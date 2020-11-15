/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Vk.Extras;
using static Vega.InternalLog;

namespace Vega.Graphics
{
	public unsafe sealed partial class GraphicsDevice
	{
		// The version of the library as a Vulkan version
		private static readonly Vk.Version ENGINE_VERSION = new(typeof(GraphicsDevice).Assembly.GetName().Version!);

		// Create the instance, debug utils (if requested), and select the physical device to use
		private static void InitializeVulkanInstance(
			bool validation, out Vk.InstanceData instanceData, 
			out Vk.EXT.DebugUtilsMessenger? debug, out Vk.PhysicalDeviceData deviceData)
		{
			// Get the required layers and extensions
			var reqExts = Glfw.GetRequiredInstanceExtensions().ToList();
			List<string> reqLayers = new();
			if (validation) {
				if (!Vk.InstanceData.ExtensionNames.Contains(Vk.Constants.EXT_DEBUG_UTILS_EXTENSION_NAME)) {
					LWARN("Required extension not present for graphics validation");
					validation = false;
				}
				else if (!Vk.InstanceData.LayerNames.Contains("VK_LAYER_KHRONOS_validation") &&
						 !Vk.InstanceData.LayerNames.Contains("VK_LAYER_LUNARG_validation")) {
					LWARN("Required layer not present for graphics validation");
					validation = false;
				}
				else {
					reqExts.Add(Vk.Constants.EXT_DEBUG_UTILS_EXTENSION_NAME);
					reqLayers.Add(Vk.InstanceData.LayerNames.Contains("VK_LAYER_KHRONOS_validation")
						? "VK_LAYER_KHRONOS_validation" : "VK_LAYER_LUNARG_validation");
				}
			}

			// Create application info
			using var appName = new Vk.NativeString(Core.Instance!.AppName);
			using var engName = new Vk.NativeString("Vega");
			Vk.ApplicationInfo appInfo = new(
				applicationName: appName.Data,
				applicationVersion: new Vk.Version(Core.Instance!.AppVersion),
				engineName: engName.Data,
				engineVersion: ENGINE_VERSION,
				apiVersion: Vk.InstanceData.GetApiVersion() // Select highest supported version by default
			);

			// Create instance
			using var extList = new Vk.NativeStringList(reqExts);
			using var layList = new Vk.NativeStringList(reqLayers);
			Vk.InstanceCreateInfo ici = new(
				applicationInfo: &appInfo,
				enabledLayerCount: layList.Count,
				enabledLayerNames: layList.Data,
				enabledExtensionCount: extList.Count,
				enabledExtensionNames: extList.Data
			);
			Vk.Instance.CreateInstance(&ici, null, out var instance).Throw("Failed to create Vulkan instance");
			instanceData = new(instance);
			if (instanceData.PhysicalDevices.Count == 0) {
				throw new PlatformNotSupportedException("No graphics devices on the system support Vulkan");
			}

			// Register with debug reports
			if (validation) {
				Vk.EXT.DebugUtilsMessengerCreateInfo dumci = new(
					flags: Vk.EXT.DebugUtilsMessengerCreateFlags.NoFlags,
					messageSeverity: 
						Vk.EXT.DebugUtilsMessageSeverityFlags.InfoEXT |
						Vk.EXT.DebugUtilsMessageSeverityFlags.WarningEXT |
						Vk.EXT.DebugUtilsMessageSeverityFlags.ErrorEXT,
					messageType:
						Vk.EXT.DebugUtilsMessageTypeFlags.GeneralEXT |
						Vk.EXT.DebugUtilsMessageTypeFlags.PerformanceEXT |
						Vk.EXT.DebugUtilsMessageTypeFlags.ValidationEXT,
					userCallback: &DebugMessageCallback
				);
				instance.CreateDebugUtilsMessengerEXT(&dumci, null, out debug)
						.Throw("Failed to create Vulkan debug messenger");
			}
			else {
				debug = null;
			}

			// Select physical device, first by events, then first discrete, then any
			Vk.PhysicalDevice pdev = Vk.PhysicalDevice.Null;
			foreach (var device in instanceData.PhysicalDevices) {
				var data = new Vk.PhysicalDeviceData(device);

				var evt = new DeviceDiscoveryEvent {
					DeviceName = data.DeviceName,
					IsDiscrete = data.IsDiscrete,
					MemorySize = new DataSize((long)data.TotalLocalMemory.Value),
					Use = false
				};
				Core.Events.Publish(evt);
				if (evt.Use) {
					pdev = device;
					deviceData = data;
				}
			}
			if (!pdev) {
				pdev = instanceData.PhysicalDevices.FirstOrDefault(dev => {
					dev.GetPhysicalDeviceProperties(out var props);
					return props.DeviceType == Vk.PhysicalDeviceType.DiscreteGpu;
				})
				?? instanceData.PhysicalDevices[0];
				deviceData = new(pdev);
			}
			else {
				deviceData = new(Vk.PhysicalDevice.Null); // Never Reached
			}
		}

		// Create the logical device, and the graphics queue
		private static void CreateVulkanDevice(Vk.PhysicalDeviceData pdev, out Vk.Device device, out Vk.Queue gQueue,
			out uint gQueueIndex)
		{
			// Populate the features (TODO: add some feature selection to the public API)
			Vk.PhysicalDeviceFeatures feats = new();

			// Check and populate extensions
			using var extList = new Vk.NativeStringList();
			if (!pdev.ExtensionNames.Contains(Vk.Constants.KHR_SWAPCHAIN_EXTENSION_NAME)) {
				throw new PlatformNotSupportedException("Selected device does not support swapchain operations");
			}
			extList.Add(Vk.Constants.KHR_SWAPCHAIN_EXTENSION_NAME);

			// Create the queues
			float QUEUE_PRIORITIES = 1;
			Vk.DeviceQueueCreateInfo gQueueInfo = new(
				queueFamilyIndex: 0,
				queueCount: 1,
				queuePriorities: &QUEUE_PRIORITIES
			);
			foreach (var props in pdev.QueueFamilies) {
				if ((props.QueueFlags & Vk.QueueFlags.Graphics) > 0) {
					break;
				}
				++gQueueInfo.QueueFamilyIndex;
			}
			if (gQueueInfo.QueueFamilyIndex == pdev.QueueFamilyCount) {
				// Shouldn't happen per spec, but still check
				throw new PlatformNotSupportedException("Selected device does not support graphics operations.");
			}

			// Create device
			Vk.DeviceCreateInfo dci = new(
				queueCreateInfoCount: 1,
				queueCreateInfos: &gQueueInfo,
				enabledExtensionCount: extList.Count,
				enabledExtensionNames: extList.Data,
				enabledFeatures: &feats
			);
			pdev.PhysicalDevice.CreateDevice(&dci, null, out device!).Throw("Failed to create Vulkan device");

			// Get the queue
			device.GetDeviceQueue(gQueueInfo.QueueFamilyIndex, 0, out gQueue!);
			gQueueIndex = gQueueInfo.QueueFamilyIndex;
		}

		// The callback (FROM UNMANAGED CODE) for Vulkan debug messaging
		private static Vk.Bool32 DebugMessageCallback(
			Vk.EXT.DebugUtilsMessageSeverityFlags severity,
			Vk.EXT.DebugUtilsMessageTypeFlags type,
			Vk.EXT.DebugUtilsMessengerCallbackData* data,
			void* userData
		)
		{
			var evt = new DebugMessageEvent {
				Severity = (DebugMessageSeverity)severity,
				Type = (DebugMessageType)type,
				Message = Marshal.PtrToStringAnsi(new IntPtr(data->Message)) ?? String.Empty, // ANSI, not UTF
				MessageId = data->MessageIdNumber
			};
			Vk.EXT.DebugUtilsObjectNameInfo* next = data->Objects;
			while (next != null) {
				evt.ObjectNames.Add(Marshal.PtrToStringAnsi(new IntPtr(next->ObjectName)) ?? String.Empty); // ANSI, not UTF
				next = (Vk.EXT.DebugUtilsObjectNameInfo*)next->pNext;
			}
			Core.Events.Publish(Core.Instance?.Graphics, evt);
			return Vk.Bool32.False;
		}
	}
}
