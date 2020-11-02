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
	public unsafe sealed partial class GraphicsService
	{
		private static void InitializeVulkanInstance(
			GraphicsService service, bool validation, out Vk.InstanceData instanceData, 
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
			using var appName = new Vk.NativeString(service.Core.AppName);
			using var engName = new Vk.NativeString("Vega");
			Vk.ApplicationInfo.New(out var appInfo);
			appInfo.ApplicationName = appName.Data;
			appInfo.ApplicationVersion = new Vk.Version(service.Core.AppVersion);
			appInfo.EngineName = engName.Data;
			appInfo.EngineVersion = new Vk.Version(typeof(GraphicsService).Assembly.GetName().Version!);
			appInfo.ApiVersion = Vk.Version.VK_VERSION_1_0;

			// Create instance
			using var extList = new Vk.NativeStringList(reqExts);
			using var layList = new Vk.NativeStringList(reqLayers);
			Vk.InstanceCreateInfo.New(out var ici);
			ici.ApplicationInfo = &appInfo;
			ici.EnabledExtensionCount = extList.Count;
			ici.EnabledExtensionNames = extList.Data;
			ici.EnabledLayerCount = layList.Count;
			ici.EnabledLayerNames = layList.Data;
			Vk.Instance.CreateInstance(&ici, null, out var instance).Throw("Failed to create Vulkan instance");
			instanceData = new(instance);
			if (instanceData.PhysicalDevices.Count == 0) {
				throw new PlatformNotSupportedException("No graphics devices on the system support Vulkan");
			}

			// Register with debug reports
			if (validation) {
				Vk.EXT.DebugUtilsMessengerCreateInfo.New(out var dumci);
				dumci.MessageSeverity =
					Vk.EXT.DebugUtilsMessageSeverityFlags.InfoEXT |
					Vk.EXT.DebugUtilsMessageSeverityFlags.WarningEXT |
					Vk.EXT.DebugUtilsMessageSeverityFlags.ErrorEXT;
				dumci.MessageType = 
					Vk.EXT.DebugUtilsMessageTypeFlags.GeneralEXT | 
					Vk.EXT.DebugUtilsMessageTypeFlags.PerformanceEXT | 
					Vk.EXT.DebugUtilsMessageTypeFlags.ValidationEXT;
				dumci.UserCallback = &DebugMessageCallback;
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

		private static void CreateVulkanDevice(GraphicsService service, out Vk.Device device, out Vk.Queue gQueue,
			out uint gQueueIndex)
		{
			// Populate the features
			Vk.PhysicalDeviceFeatures feats = new();

			// Check and populate extensions
			using var extList = new Vk.NativeStringList();
			if (!service.DeviceData.ExtensionNames.Contains(Vk.Constants.KHR_SWAPCHAIN_EXTENSION_NAME)) {
				throw new PlatformNotSupportedException("Selected device does not support swapchain operations");
			}
			extList.Add(Vk.Constants.KHR_SWAPCHAIN_EXTENSION_NAME);

			// Create the queues
			float QUEUE_PRIORITIES = 1;
			Vk.DeviceQueueCreateInfo.New(out var gQueueInfo);
			gQueueInfo.QueueCount = 1;
			gQueueInfo.QueuePriorities = &QUEUE_PRIORITIES;
			foreach (var props in service.DeviceData.QueueFamilies) {
				if ((props.QueueFlags & Vk.QueueFlags.Graphics) > 0) {
					break;
				}
				++gQueueInfo.QueueFamilyIndex;
			}
			if (gQueueInfo.QueueFamilyIndex == service.DeviceData.QueueFamilyCount) {
				// Shouldn't happen per spec, but still check
				throw new PlatformNotSupportedException("Selected device does not support graphics operations.");
			}

			// Create device
			Vk.DeviceCreateInfo.New(out var dci);
			dci.EnabledFeatures = &feats;
			dci.QueueCreateInfoCount = 1;
			dci.QueueCreateInfos = &gQueueInfo;
			dci.EnabledExtensionCount = extList.Count;
			dci.EnabledExtensionNames = extList.Data;
			service.PhysicalDevice.CreateDevice(&dci, null, out device!).Throw("Failed to create Vulkan device");

			// Get the queue
			device.GetDeviceQueue(gQueueInfo.QueueFamilyIndex, 0, out gQueue!);
			gQueueIndex = gQueueInfo.QueueFamilyIndex;
		}

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
				Message = Marshal.PtrToStringAnsi(new IntPtr(data->Message)) ?? String.Empty,
				MessageId = data->MessageIdNumber
			};
			Vk.EXT.DebugUtilsObjectNameInfo* next = data->Objects;
			while (next != null) {
				evt.ObjectNames.Add(Marshal.PtrToStringAnsi(new IntPtr(next->ObjectName)) ?? String.Empty);
				next = (Vk.EXT.DebugUtilsObjectNameInfo*)next->pNext;
			}
			Core.Events.Publish(Core.Instance?.Graphics, evt);
			return Vk.Bool32.False;
		}
	}
}
