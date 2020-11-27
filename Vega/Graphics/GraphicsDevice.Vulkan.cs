/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Vulkan;
using Vulkan.VVK;
using static Vega.InternalLog;

namespace Vega.Graphics
{
	public unsafe sealed partial class GraphicsDevice
	{
		// The version of the library as a Vulkan version
		private static readonly VkVersion ENGINE_VERSION = new(typeof(GraphicsDevice).Assembly.GetName().Version!);

		// Create the instance, debug utils (if requested), and select the physical device to use
		private static void InitializeVulkanInstance(
			bool validation, out InstanceInfo instanceInfo, 
			out VkDebugUtilsMessengerEXT? debug, out DeviceInfo deviceInfo,
			out GraphicsFeatures features)
		{
			// Get the required layers and extensions
			var reqExts = Glfw.GetRequiredInstanceExtensions().ToList();
			List<string> reqLayers = new();
			if (validation) {
				if (!InstanceInfo.ExtensionNames.Contains(VkConstants.EXT_DEBUG_UTILS_EXTENSION_NAME)) {
					LWARN("Required extension not present for graphics validation");
					validation = false;
				}
				else if (!InstanceInfo.LayerNames.Contains("VK_LAYER_KHRONOS_validation") &&
						 !InstanceInfo.LayerNames.Contains("VK_LAYER_LUNARG_validation")) {
					LWARN("Required layer not present for graphics validation");
					validation = false;
				}
				else {
					reqExts.Add(VkConstants.EXT_DEBUG_UTILS_EXTENSION_NAME);
					reqLayers.Add(InstanceInfo.LayerNames.Contains("VK_LAYER_KHRONOS_validation")
						? "VK_LAYER_KHRONOS_validation" : "VK_LAYER_LUNARG_validation");
				}
			}

			// Create application info
			using var appName = new NativeString(Core.Instance!.AppName);
			using var engName = new NativeString("Vega");
			VkApplicationInfo appInfo = new(
				applicationName: appName.Data,
				applicationVersion: new VkVersion(Core.Instance!.AppVersion),
				engineName: engName.Data,
				engineVersion: ENGINE_VERSION,
				apiVersion: InstanceInfo.GetApiVersion() // Select highest supported version by default
			);

			// Create instance
			using var extList = new NativeStringList(reqExts);
			using var layList = new NativeStringList(reqLayers);
			VkInstanceCreateInfo ici = new(
				applicationInfo: &appInfo,
				enabledLayerCount: layList.Count,
				enabledLayerNames: layList.Data,
				enabledExtensionCount: extList.Count,
				enabledExtensionNames: extList.Data
			);
			VulkanHandle<VkInstance> instHandle;
			VkInstance.CreateInstance(&ici, null, &instHandle).Throw("Failed to create Vulkan instance");
			instanceInfo = new(new(instHandle, InstanceInfo.GetApiVersion()));
			if (instanceInfo.PhysicalDevices.Count == 0) {
				throw new PlatformNotSupportedException("No graphics devices on the system support Vulkan");
			}

			// Register with debug reports
			if (validation) {
				VkDebugUtilsMessengerCreateInfoEXT dumci = new(
					flags: VkDebugUtilsMessengerCreateFlagsEXT.NoFlags,
					messageSeverity: 
						VkDebugUtilsMessageSeverityFlagsEXT.Info |
						VkDebugUtilsMessageSeverityFlagsEXT.Warning |
						VkDebugUtilsMessageSeverityFlagsEXT.Error,
					messageType:
						VkDebugUtilsMessageTypeFlagsEXT.General |
						VkDebugUtilsMessageTypeFlagsEXT.Performance |
						VkDebugUtilsMessageTypeFlagsEXT.Validation,
					userCallback: &DebugMessageCallback
				);
				VulkanHandle<VkDebugUtilsMessengerEXT> msgHandle;
				instanceInfo.Instance.CreateDebugUtilsMessengerEXT(&dumci, null, &msgHandle)
						.Throw("Failed to create Vulkan debug messenger");
				debug = new(msgHandle, instanceInfo.Instance);
			}
			else {
				debug = null;
			}

			// Select physical device, first by events, then first discrete, then any
			VkPhysicalDevice? pdev = null;
			GraphicsFeatures? feats = null;
			foreach (var device in instanceInfo.PhysicalDevices) {
				var info = new DeviceInfo(device);

				var evt = new DeviceDiscoveryEvent {
					DeviceName = info.DeviceName,
					IsDiscrete = info.IsDiscrete,
					MemorySize = new DataSize((long)info.TotalLocalMemory),
					Features = new(),
					Use = false,
					RequestedFeatures = null
				};
				evt.Features.Populate(info);
				Core.Events.Publish(evt);
				if (evt.Use) {
					feats = evt.RequestedFeatures;
					pdev = device;
					deviceInfo = info;
				}
			}
			if (pdev is null) {
				pdev = instanceInfo.PhysicalDevices.FirstOrDefault(dev => {
					dev.GetPhysicalDeviceProperties(out var props);
					return props.DeviceType == VkPhysicalDeviceType.DiscreteGpu;
				})
				?? instanceInfo.PhysicalDevices[0];
				deviceInfo = new(pdev);
			}
			else {
				deviceInfo = new(instanceInfo.PhysicalDevices[0]); // NEVER REACHED
			}
			features = feats ?? new();
		}

		// Create the logical device, and the graphics queue
		private static void CreateVulkanDevice(DeviceInfo pdev, GraphicsFeatures features,
			out VkDevice device, out VkQueue gQueue, out uint gQueueIndex)
		{
			// Populate the features and extensions for the device
			List<string> extensions = new();
			if (!features.TryBuild(pdev.Features, pdev.ExtensionNames, out var feats, extensions, out var missing)) {
				throw new InvalidOperationException($"Cannot create device - required feature '{missing}' is not present");
			}

			// Check and populate extensions
			using var extList = new NativeStringList(extensions);
			if (!pdev.ExtensionNames.Contains(VkConstants.KHR_SWAPCHAIN_EXTENSION_NAME)) {
				throw new PlatformNotSupportedException("Selected device does not support swapchain operations");
			}
			extList.Add(VkConstants.KHR_SWAPCHAIN_EXTENSION_NAME);

			// Create the queues
			float QUEUE_PRIORITIES = 1;
			VkDeviceQueueCreateInfo gQueueInfo = new(
				queueFamilyIndex: pdev.FindQueueFamily(VkQueueFlags.Graphics)!.Value,
				queueCount: 1,
				queuePriorities: &QUEUE_PRIORITIES
			);

			// Create device
			VkDeviceCreateInfo dci = new(
				queueCreateInfoCount: 1,
				queueCreateInfos: &gQueueInfo,
				enabledExtensionCount: extList.Count,
				enabledExtensionNames: extList.Data,
				enabledFeatures: &feats
			);
			VulkanHandle<VkDevice> devHandle;
			pdev.PhysicalDevice.CreateDevice(&dci, null, &devHandle).Throw("Failed to create Vulkan device");
			device = new(devHandle, pdev.PhysicalDevice, pdev.PhysicalDevice.Functions.CoreVersion);

			// Get the queue
			VulkanHandle<VkQueue> queueHandle;
			device.GetDeviceQueue(gQueueInfo.QueueFamilyIndex, 0, &queueHandle);
			gQueue = new(queueHandle, device);
			gQueueIndex = gQueueInfo.QueueFamilyIndex;
		}

		// The callback (FROM UNMANAGED CODE) for Vulkan debug messaging
		private static VkBool32 DebugMessageCallback(
			VkDebugUtilsMessageSeverityFlagsEXT severity,
			VkDebugUtilsMessageTypeFlagsEXT type,
			VkDebugUtilsMessengerCallbackDataEXT* data,
			void* userData
		)
		{
			var evt = new DebugMessageEvent {
				Severity = (DebugMessageSeverity)severity,
				Type = (DebugMessageType)type,
				Message = Marshal.PtrToStringAnsi(new IntPtr(data->Message)) ?? String.Empty, // ANSI, not UTF
				MessageId = data->MessageIdNumber
			};
			VkDebugUtilsObjectNameInfoEXT* next = data->Objects;
			while (next != null) {
				evt.ObjectNames.Add(Marshal.PtrToStringAnsi(new IntPtr(next->ObjectName)) ?? String.Empty); // ANSI, not UTF
				next = (VkDebugUtilsObjectNameInfoEXT*)next->pNext;
			}
			Core.Events.Publish(Core.Instance?.Graphics, evt);
			return VkBool32.False;
		}
	}
}
