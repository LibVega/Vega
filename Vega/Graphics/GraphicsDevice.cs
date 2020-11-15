/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using static Vega.InternalLog;

namespace Vega.Graphics
{
	/// <summary>
	/// Manages the top-level execution and resource management of the graphics system.
	/// </summary>
	public unsafe sealed partial class GraphicsDevice
	{
		#region Fields
		/// <summary>
		/// The core instance controlling this graphics service.
		/// </summary>
		public readonly Core Core;

		#region Vulkan-Like Objects
		internal readonly Vk.Instance VkInstance;
		internal readonly Vk.InstanceData VkInstanceData;
		internal readonly Vk.EXT.DebugUtilsMessenger? VkDebugUtils;
		internal readonly Vk.PhysicalDevice VkPhysicalDevice;
		internal readonly Vk.PhysicalDeviceData VkDeviceData;
		internal readonly Vk.Device VkDevice;
		internal Vk.Version ApiVersion => VkInstance.Functions.CoreVersion;

		internal readonly DeviceQueue GraphicsQueue;
		#endregion // Vulkan-Like Objects

		/// <summary>
		/// The limits for the selected graphics device and driver.
		/// </summary>
		public readonly GraphicsLimits Limits;

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
			InitializeVulkanInstance(validation, out VkInstanceData, out VkDebugUtils, out VkDeviceData);
			VkInstance = VkInstanceData.Instance;
			VkPhysicalDevice = VkDeviceData.PhysicalDevice;
			LINFO($"Selected device '{VkDeviceData.DeviceName}'");

			// Create the device and queue objects
			CreateVulkanDevice(VkDeviceData, out VkDevice, out var graphicsQueue, out var graphicsQueueIndex);
			GraphicsQueue = new(this, graphicsQueue, graphicsQueueIndex);
			LINFO("Created Vulkan device instance");
			Limits = new(VkDeviceData);

			// Prepare resources
			Resources = new(this);
			Resources.RegisterThread();
		}
		~GraphicsDevice()
		{
			dispose(false);
		}

		// Called once per application frame to perform global resource tracking and cleanup
		internal void Update()
		{
			GraphicsQueue.UpdateContexts();
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
