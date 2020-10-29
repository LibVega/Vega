/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Vk;
using static Vega.InternalLog;

namespace Vega.Graphics
{
	public unsafe sealed partial class GraphicsService
	{
		private static void InitializeVulkanInstance(
			GraphicsService service, bool validation, out Vk.Instance instance, out Vk.EXT.DebugUtilsMessenger? debug, 
			out Vk.PhysicalDevice physDevice)
		{
			// Get instance extensions and layers
			List<string> instExts = new(), instLayers = new();
			{
				uint instCount = 0;
				Vk.Instance.EnumerateInstanceExtensionProperties(null, &instCount, null).Throw();
				var extPtr = stackalloc Vk.ExtensionProperties[(int)instCount];
				Vk.Instance.EnumerateInstanceExtensionProperties(null, &instCount, extPtr).Throw();
				for (uint i = 0; i < instCount; ++i) {
					instExts.Add(extPtr[i].ExtensionName);
				}
				Vk.Instance.EnumerateInstanceLayerProperties(&instCount, null).Throw();
				var layPtr = stackalloc Vk.LayerProperties[(int)instCount];
				Vk.Instance.EnumerateInstanceLayerProperties(&instCount, layPtr).Throw();
				for (uint i = 0; i < instCount; ++i) {
					instLayers.Add(layPtr[i].LayerName);
				}
			}

			// Get the required layers and extensions
			var reqExts = Glfw.GetRequiredInstanceExtensions().ToList();
			List<string> reqLayers = new();
			if (validation) {
				if (!instExts.Contains(Vk.Constants.EXT_DEBUG_UTILS_EXTENSION_NAME)) {
					LWARN("Required extension not present for graphics validation");
					validation = false;
				}
				else if (!instLayers.Contains("VK_LAYER_KHRONOS_validation") &&
						 !instLayers.Contains("VK_LAYER_LUNARG_validation")) {
					LWARN("Required layer not present for graphics validation");
					validation = false;
				}
				else {
					reqExts.Add(Vk.Constants.EXT_DEBUG_UTILS_EXTENSION_NAME);
					reqLayers.Add(instLayers.Contains("VK_LAYER_KHRONOS_validation")
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
			Vk.Instance.CreateInstance(&ici, null, out instance!).Throw("Failed to create Vulkan instance");

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

			// Get the physical devices
			List<Vk.PhysicalDevice> pdevs = new();
			{
				uint pdevCount = 0;
				instance.EnumeratePhysicalDevices(&pdevCount, null).Throw();
				var pdevPtr = stackalloc Vk.Handle<Vk.PhysicalDevice>[(int)pdevCount];
				instance.EnumeratePhysicalDevices(&pdevCount, pdevPtr).Throw();
				for (uint i = 0; i < pdevCount; ++i) {
					pdevs.Add(new(instance, pdevPtr[i]));
				}
			}
			if (pdevs.Count == 0) {
				throw new PlatformNotSupportedException("No graphics devices on the system support Vulkan");
			}

			// Select physical device, first by events, then first discrete, then any
			Vk.PhysicalDevice? pdev = null;
			foreach (var device in pdevs) {
				device.GetPhysicalDeviceProperties(out var props);
				device.GetPhysicalDeviceMemoryProperties(out var memProps);
				ulong totalMem = 0;
				for (uint mi = 0; mi < memProps.MemoryHeapCount; ++mi) {
					totalMem += (&memProps.MemoryHeaps_0)[mi].Size;
				}

				var evt = new DeviceDiscoveryEvent {
					DeviceName = props.DeviceName,
					IsDiscrete = props.DeviceType == Vk.PhysicalDeviceType.DiscreteGpu,
					MemorySize = new DataSize((long)totalMem),
					Use = false
				};
				Core.Events.Publish(evt);
				if (evt.Use) {
					pdev = device;
				}
			}
			physDevice = pdev
				?? pdevs.FirstOrDefault(dev => {
					dev.GetPhysicalDeviceProperties(out var props);
					return props.DeviceType == PhysicalDeviceType.DiscreteGpu;
				})
				?? pdevs[0];
		}

		private static void CreateVulkanDevice(GraphicsService service, out Vk.Device device, out Vk.Queue gQueue)
		{
			// Populate the features
			Vk.PhysicalDeviceFeatures feats = new();

			// Enumerate queue families
			List<Vk.QueueFamilyProperties> queueFams = new();
			{
				uint qcount = 0;
				service.PhysicalDevice.GetPhysicalDeviceQueueFamilyProperties(&qcount, null);
				var qptr = stackalloc Vk.QueueFamilyProperties[(int)qcount];
				service.PhysicalDevice.GetPhysicalDeviceQueueFamilyProperties(&qcount, qptr);
				for (uint i = 0; i < qcount; ++i) {
					queueFams.Add(qptr[i]);
				}
			}

			// Create the queues
			float QUEUE_PRIORITIES = 1;
			Vk.DeviceQueueCreateInfo.New(out var gQueueInfo);
			gQueueInfo.QueueCount = 1;
			gQueueInfo.QueuePriorities = &QUEUE_PRIORITIES;
			foreach (var props in queueFams) {
				if ((props.QueueFlags & Vk.QueueFlags.Graphics) > 0) {
					break;
				}
				++gQueueInfo.QueueFamilyIndex;
			}
			if (gQueueInfo.QueueFamilyIndex == queueFams.Count) {
				// Shouldn't happen per spec, but still check
				throw new PlatformNotSupportedException("Selected device does not support graphics operations.");
			}

			// Create device
			Vk.DeviceCreateInfo.New(out var dci);
			dci.EnabledFeatures = &feats;
			dci.QueueCreateInfoCount = 1;
			dci.QueueCreateInfos = &gQueueInfo;
			service.PhysicalDevice.CreateDevice(&dci, null, out device!).Throw("Failed to create Vulkan device");

			// Get the queue
			device.GetDeviceQueue(gQueueInfo.QueueFamilyIndex, 0, out gQueue!);
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
			for (uint i = 0; i < data->ObjectCount; ++i) {
				evt.ObjectNames.Add(Marshal.PtrToStringAnsi(new IntPtr(next->ObjectName)) ?? String.Empty);
				next = (Vk.EXT.DebugUtilsObjectNameInfo*)next->pNext;
			}
			Core.Events.Publish(Core.Instance?.Graphics, evt);
			return Vk.Bool32.False;
		}
	}
}
