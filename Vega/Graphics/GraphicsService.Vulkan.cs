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
	public unsafe sealed partial class GraphicsService
	{
		private static void InitializeVulkanInstance(
			GraphicsService service, bool validation, out VulkanInstance instance, out VulkanPhysicalDevice physDevice)
		{
			// Get the required layers and extensions
			var reqExts = Glfw.GetRequiredInstanceExtensions().ToList();
			List<string> reqLayers = new();
			if (validation) {
				if (!VulkanInstance.Extensions.Contains(Vk.Constants.EXT_DEBUG_UTILS_EXTENSION_NAME)) {
					LWARN("Required extension not present for graphics validation");
					validation = false;
				}
				else if (!VulkanInstance.Layers.Contains("VK_LAYER_KHRONOS_validation") &&
						 !VulkanInstance.Layers.Contains("VK_LAYER_LUNARG_validation")) {
					LWARN("Required layer not present for graphics validation");
					validation = false;
				}
				else {
					reqExts.Add(Vk.Constants.EXT_DEBUG_UTILS_EXTENSION_NAME);
					reqLayers.Add(VulkanInstance.Layers.Contains("VK_LAYER_KHRONOS_validation")
						? "VK_LAYER_KHRONOS_validation" : "VK_LAYER_LUNARG_validation");
				}
			}

			// Create Vulkan Instance
			instance = VulkanInstance.Create(
				service.Core.AppName, service.Core.AppVersion,
				"Vega", typeof(GraphicsService).Assembly.GetName().Version!,
				Vk.Version.VK_VERSION_1_0, reqExts, reqLayers);
			if (instance.PhysicalDevices.Count == 0) {
				throw new PlatformNotSupportedException("No devices found that support Vulkan");
			}

			// Register with debug reports
			if (validation) {
				instance.OnDebugUtilMessage += (severity, type, data) => {
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
					Core.Events.Publish(service, evt);
				};
			}

			// Select physical device, first by events, then first discrete, then any
			VulkanPhysicalDevice? pdev = null;
			foreach (var device in instance.PhysicalDevices) {
				var evt = new DeviceDiscoveryEvent {
					DeviceName = device.Name,
					IsDiscrete = device.Properties.DeviceType == Vk.PhysicalDeviceType.DiscreteGpu,
					MemorySize = new DataSize((long)device.TotalDeviceMemory),
					Use = false
				};
				Core.Events.Publish(evt);
				if (evt.Use) {
					pdev = device;
				}
			}
			physDevice = pdev
				?? instance.PhysicalDevices.FirstOrDefault(
					dev => dev.Properties.DeviceType == Vk.PhysicalDeviceType.DiscreteGpu)
				?? instance.PhysicalDevices[0];
		}

		private static void CreateVulkanDevice(GraphicsService service, out VulkanDevice device, out VulkanQueue gQueue)
		{
			// Populate the features
			Vk.PhysicalDeviceFeatures feats = new();

			// Create the queues
			float QUEUE_PRIORITIES = 1;
			Vk.DeviceQueueCreateInfo.New(out var gQueueInfo);
			gQueueInfo.QueueCount = 1;
			gQueueInfo.QueuePriorities = &QUEUE_PRIORITIES;
			foreach (var props in service.PhysicalDevice.QueueFamilies) {
				if ((props.QueueFlags & Vk.QueueFlags.Graphics) > 0) {
					break;
				}
				++gQueueInfo.QueueFamilyIndex;
			}
			if (gQueueInfo.QueueFamilyIndex == service.PhysicalDevice.QueueFamilies.Count) {
				// Shouldn't happen per spec, but still check
				throw new PlatformNotSupportedException("Selected device does not support graphics operations.");
			}

			// Create the device and get the queue
			device = service.PhysicalDevice.CreateDevice(feats, new[] { gQueueInfo });
			gQueue = device.GetQueue(gQueueInfo.QueueFamilyIndex, 0);
		}
	}
}
