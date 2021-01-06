/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;
using static Vega.InternalLog;

namespace Vega.Graphics
{
	/// <summary>
	/// Manages the top-level execution and resource management of the graphics system.
	/// </summary>
	public unsafe sealed partial class GraphicsDevice
	{
		// The maximum number of graphics frames that may be processing in parallel
		// This is not necessarily tied to the swapchain buffer count, but cannot be larger than it
		internal const uint MAX_PARALLEL_FRAMES = 3;

		#region Fields
		/// <summary>
		/// The core instance controlling this graphics service.
		/// </summary>
		public readonly Core Core;

		#region Vulkan Objects
		internal readonly VkInstance VkInstance;
		internal readonly Vulkan.VVK.InstanceInfo VkInstanceInfo;
		internal readonly VkDebugUtilsMessengerEXT? VkDebugUtils;
		internal readonly VkPhysicalDevice VkPhysicalDevice;
		internal readonly Vulkan.VVK.DeviceInfo VkDeviceInfo;
		internal readonly VkDevice VkDevice;
		internal VkVersion ApiVersion => VkInstance.Functions.CoreVersion;

		internal readonly DeviceQueue GraphicsQueue;
		#endregion // Vulkan Objects

		/// <summary>
		/// The limits for the selected graphics device and driver.
		/// </summary>
		public readonly GraphicsLimits Limits;
		/// <summary>
		/// The features that are enabled on the device.
		/// </summary>
		public readonly GraphicsFeatures Features;

		#region Resources
		internal readonly ResourceManager Resources;
		/// <summary>
		/// Gets if the calling thread is registered with the graphics service, and is able to perform graphics
		/// operations.
		/// </summary>
		public bool IsThreadRegistered => Resources.IsThreadRegistered;
		/// <summary>
		/// Gets if the calling thread is the main graphics thread.
		/// </summary>
		public bool IsMainThread => Resources.IsMainThread;
		#endregion // Resources

		internal bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		internal GraphicsDevice(Core core, bool validation)
		{
			if (!Glfw.VulkanSupported()) {
				throw new PlatformNotSupportedException("The Vulkan runtime is not available on this platform");
			}
			Core = core;

			// Create the instance and select the device to use
			InitializeVulkanInstance(validation, out VkInstanceInfo, out VkDebugUtils, out VkDeviceInfo, out Features);
			VkInstance = VkInstanceInfo.Instance;
			VkPhysicalDevice = VkDeviceInfo.PhysicalDevice;
			LINFO($"Selected device '{VkDeviceInfo.DeviceName}'");

			// Create the device and queue objects
			CreateVulkanDevice(VkDeviceInfo, Features, out VkDevice, out var graphicsQueue, out var graphicsQueueIndex);
			GraphicsQueue = new(this, graphicsQueue, graphicsQueueIndex);
			LINFO("Created Vulkan device instance");
			Limits = new(VkDeviceInfo);

			// Prepare resources
			Resources = new(this);
		}
		~GraphicsDevice()
		{
			dispose(false);
		}

		// Called once per application frame to perform global resource tracking and cleanup
		internal void Update()
		{
			// Validate no window renderers are crossing frame boundaries
			foreach (var window in Core.Windows) {
				if (window.Renderer?.IsRecording ?? false) {
					throw new InvalidOperationException("Window renderers cannot be recorded across frame boundaries");
				}
			}

			// Update per-frame graphics objects
			GraphicsQueue.UpdateContexts();

			// Run per-frame resource updates
			Resources.NextFrame();
		}

		#region Threading
		/// <summary>
		/// Performs initialization required for the calling thread to perform graphics operations. Attempting to
		/// perform graphics operations on unregistered threads will cause an exception. The main thread is always
		/// registered.
		/// </summary>
		public void RegisterThread() => Resources.RegisterThread();

		/// <summary>
		/// Unregisters a threads previously called with <see cref="RegisterThread"/>, performing cleanup and releasing
		/// graphics resources for the thread. After this call, the calling thread must not perform graphics operations.
		/// </summary>
		public void UnregisterThread() => Resources.UnregisterThread();
		#endregion // Threading

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
					VkDevice.DeviceWaitIdle();

					Resources.UnregisterThread();
					Resources.Dispose();

					GraphicsQueue.Dispose();

					VkDevice.DestroyDevice(null);
					LINFO("Destroyed Vulkan device");
					VkDebugUtils?.DestroyDebugUtilsMessengerEXT(null);
					VkInstance.DestroyInstance(null);
					LINFO("Destroyed Vulkan instance");
				}
			}
			IsDisposed = true;
		}
		#endregion // Disposable
	}
}
