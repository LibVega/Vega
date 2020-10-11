/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using VVK;
using static Vega.InternalLog;

namespace Vega.Graphics
{
	public unsafe sealed class GraphicsService
	{
		#region Fields
		/// <summary>
		/// The core instance controlling this graphics service.
		/// </summary>
		public readonly Core Core;

		// Vulkan objects
		internal readonly VulkanInstance Instance;
		internal readonly VulkanPhysicalDevice PhysicalDevice;
		internal Vk.Version ApiVersion => Instance.ApiVersion;

		internal bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		internal GraphicsService(Core core, bool validation)
		{
			if (!Glfw.VulkanSupported()) {
				throw new PlatformNotSupportedException("The Vulkan runtime is not available on this platform");
			}
			Core = core;

			// Get the required layers and extensions
			var reqExts = Glfw.GetRequiredInstanceExtensions().ToList();
			List<string> reqLayers = new();
			if (validation) {
				if (!VulkanInstance.Extensions.Contains(Vk.Constants.EXT_DEBUG_UTILS_EXTENSION_NAME)) {
					LWARN("Required extension not present for graphics validation", this);
					validation = false;
				}
				else if (!VulkanInstance.Layers.Contains("VK_LAYER_KHRONOS_validation") &&
						 !VulkanInstance.Layers.Contains("VK_LAYER_LUNARG_validation")) {
					LWARN("Required layer not present for graphics validation", this);
					validation = false;
				}
				else {
					reqExts.Add(Vk.Constants.EXT_DEBUG_UTILS_EXTENSION_NAME);
					reqLayers.Add(VulkanInstance.Layers.Contains("VK_LAYER_KHRONOS_validation")
						? "VK_LAYER_KHRONOS_validation" : "VK_LAYER_LUNARG_validation");
				}
			}

			// Create Vulkan Instance
			Instance = VulkanInstance.Create(
				core.AppName, core.AppVersion,
				"Vega", GetType().Assembly.GetName().Version!,
				Vk.Version.VK_VERSION_1_0, reqExts, reqLayers);
			if (Instance.PhysicalDevices.Count == 0) {
				throw new PlatformNotSupportedException("No devices found that support Vulkan");
			}

			// Register with debug reports
			if (validation) {
				Instance.OnDebugUtilMessage += (severity, type, data) => {
					var evt = new DebugMessageEvent { 
						Severity = (DebugMessageSeverity)severity,
						Type = (DebugMessageType)type,
						Message = Marshal.PtrToStringAnsi(new IntPtr(data->Message)) ?? String.Empty,
						MessageId = data->MessageIdNumber
					};
					Vk.EXT.DebugUtilsObjectNameInfo* next = data->Objects;
					for (uint i = 0; i < data->ObjectCount; ++i) {
						evt.ObjectNames.Add(Marshal.PtrToStringAnsi(new IntPtr(next->ObjectName)) ?? String.Empty);
						next = (Vk.EXT.DebugUtilsObjectNameInfo*)next->pNext;
					}
					Core.Events.Publish(this, evt);
				};
			}

			// Select physical device, first by events, then first discrete, then any
			VulkanPhysicalDevice? pdev = null;
			foreach (var device in Instance.PhysicalDevices) {
				var evt = new DeviceDiscoveryEvent {
					DeviceName = device.Name,
					IsDiscrete = device.Properties.DeviceType == Vk.PhysicalDeviceType.DiscreteGpu,
					MemorySize = new DataSize((long)device.TotalDeviceMemory),
					Use = false
				};
				Core.Events.Publish(this, evt);
				if (evt.Use) {
					pdev = device;
				}
			}
			PhysicalDevice = pdev 
				?? Instance.PhysicalDevices.FirstOrDefault(
					dev => dev.Properties.DeviceType == Vk.PhysicalDeviceType.DiscreteGpu)
				?? Instance.PhysicalDevices[0];
			LINFO($"Selected device '{PhysicalDevice.Name}'");
		}
		~GraphicsService()
		{
			dispose(false);
		}

		#region Disposable
		internal void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed) {
				if (disposing) {
					Instance.Dispose();
				}
			}
			IsDisposed = true;
		}
		#endregion // Disposable
	}
}
