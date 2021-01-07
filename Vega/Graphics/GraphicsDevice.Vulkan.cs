/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
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
		// Library requires Vulkan 1.2
		private static readonly VkVersion REQUIRED_API = VkVersion.VK_VERSION_1_2;

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

			// Check for required Vulkan 1.2
			var topVersion = InstanceInfo.GetApiVersion();
			if (topVersion < REQUIRED_API) {
				throw new PlatformNotSupportedException("Vega requires Vulkan 1.2 support, please update drivers");
			}

			// Create application info
			using var appName = new NativeString(Core.Instance!.AppName);
			using var engName = new NativeString("Vega");
			VkApplicationInfo appInfo = new(
				applicationName: appName.Data,
				applicationVersion: new VkVersion(Core.Instance!.AppVersion),
				engineName: engName.Data,
				engineVersion: ENGINE_VERSION,
				apiVersion: REQUIRED_API
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

			// Filter the physical devices
			var goodDevices = instanceInfo.PhysicalDevices
				.Select(dev => ValidatePhysicalDevice(dev))
				.Where(pair => pair.info is not null)
				.ToArray();
			if (goodDevices.Length == 0) {
				throw new PlatformNotSupportedException("No devices on system support required features");
			}

			// Select physical device, first by events, then first discrete, then any
			VkPhysicalDevice? pdev = null;
			if (Core.Events.GetSubscriptionCount<DeviceDiscoveryEvent>() > 0) {
				foreach (var device in goodDevices) {
					var evt = new DeviceDiscoveryEvent(
						new(device.info!.Features, device.info.ExtensionNames), new(device.info))
					{
						DeviceName = device.info.DeviceName,
						IsDiscrete = device.info.IsDiscrete,
						MemorySize = new DataSize((long)device.info.TotalLocalMemory),
						Use = false
					};
					Core.Events.Publish(evt);
					if (evt.Use) {
						pdev = device.dev;
						deviceInfo = device.info;
					}
				} 
			}
			if (pdev is null) {
				pdev = goodDevices.FirstOrDefault(dev => {
					dev.dev.GetPhysicalDeviceProperties(out var props);
					return props.DeviceType == VkPhysicalDeviceType.DiscreteGpu;
				}).info?.PhysicalDevice
				?? goodDevices[0].dev;
				deviceInfo = new(pdev);
			}
			else {
				deviceInfo = goodDevices[0].info!; // NEVER REACHED
			}

			// Configure the device
			features = new();
			if (Core.Events.GetSubscriptionCount<DeviceConfigureEvent>() > 0) {
				DeviceConfigureEvent evt = new(new(deviceInfo.Features, deviceInfo.ExtensionNames));
				Core.Events.Publish(evt);
				features = evt.EnabledFeatures;
			}
		}

		// Create the logical device, and the graphics queue
		private static void CreateVulkanDevice(DeviceInfo pdev, in GraphicsFeatures features,
			out VkDevice device, out VkQueue gQueue, out uint gQueueIndex)
		{
			// Populate the features and extensions for the device
			List<string> extensions = new();
			VkPhysicalDeviceFeatures2.New(out var baseFeats);
			if (!features.TryBuild(pdev.Features, pdev.ExtensionNames, out baseFeats.Features, extensions, out var missing)) {
				throw new InvalidOperationException($"Cannot create device - required feature '{missing}' is not present");
			}
			baseFeats.Features.IndependentBlend = true;
			VkPhysicalDeviceVulkan11Features.New(out var feats11);
			baseFeats.pNext = &feats11;
			VkPhysicalDeviceVulkan12Features.New(out var feats12);
			feats11.pNext = &feats12;
			feats12.ScalarBlockLayout = true;
			feats12.DescriptorIndexing = true;
			feats12.DescriptorBindingPartiallyBound = true;
			feats12.DescriptorBindingUpdateUnusedWhilePending = true;
			feats12.DescriptorBindingSampledImageUpdateAfterBind = true;
			feats12.DescriptorBindingStorageImageUpdateAfterBind = true;
			feats12.DescriptorBindingStorageBufferUpdateAfterBind = true;
			feats12.DescriptorBindingUniformTexelBufferUpdateAfterBind = true;
			feats12.DescriptorBindingStorageTexelBufferUpdateAfterBind = true;

			// Populate the extensions
			using var extList = new NativeStringList(extensions);
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
				enabledFeatures: null
			);
			dci.pNext = &baseFeats; // Specify features through the pNext pointer
			VulkanHandle<VkDevice> devHandle;
			pdev.PhysicalDevice.CreateDevice(&dci, null, &devHandle).Throw("Failed to create Vulkan device");
			device = new(devHandle, pdev.PhysicalDevice, pdev.PhysicalDevice.Functions.CoreVersion);

			// Get the queue
			VulkanHandle<VkQueue> queueHandle;
			device.GetDeviceQueue(gQueueInfo.QueueFamilyIndex, 0, &queueHandle);
			gQueue = new(queueHandle, device);
			gQueueIndex = gQueueInfo.QueueFamilyIndex;
		}

		// Checks the physical device for required features
		// Note that this is the central location at which required feature checks should be added
		// Make sure to also enable the required features when creating the device above
		private static (VkPhysicalDevice dev, DeviceInfo? info) ValidatePhysicalDevice(VkPhysicalDevice dev)
		{
			var info = new DeviceInfo(dev);

			// API version check
			if (info.ApiVersion < REQUIRED_API) {
				return (dev, null);
			}

			// Feature check
			if (!info.Features12.DescriptorIndexing) {
				return (dev, null);
			}
			if (!info.Features12.ScalarBlockLayout) {
				return (dev, null);
			}

			// Extension check
			if (!info.ExtensionNames.Contains(VkConstants.KHR_SWAPCHAIN_EXTENSION_NAME)) {
				return (dev, null);
			}

			return (dev, info);
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
