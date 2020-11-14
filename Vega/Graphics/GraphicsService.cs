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
	public unsafe sealed partial class GraphicsService
	{
		/// <summary>
		/// The maximum number of concurrently executed graphics frames. This gives the number of frames that resources
		/// must be kept alive to ensure they are not destroyed while in use.
		/// </summary>
		public const uint MAX_FRAMES = 3;

		#region Fields
		/// <summary>
		/// The core instance controlling this graphics service.
		/// </summary>
		public readonly Core Core;

		// Vulkan objects/values
		internal readonly Vk.Instance Instance;
		internal readonly Vk.InstanceData InstanceData;
		internal readonly Vk.EXT.DebugUtilsMessenger? DebugUtils;
		internal readonly Vk.PhysicalDevice PhysicalDevice;
		internal readonly Vk.PhysicalDeviceData DeviceData;
		internal readonly Vk.Device Device;
		internal Vk.Version ApiVersion => Instance.Functions.CoreVersion;

		// Queue objects
		internal readonly DeviceQueue GraphicsQueue;

		/// <summary>
		/// The limits for the selected graphics device and driver.
		/// </summary>
		public readonly GraphicsLimits Limits;

		// Resources
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

		/// <summary>
		/// The frame index used for resource synchronization.
		/// </summary>
		public uint FrameIndex { get; private set; } = 0;

		internal bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		internal GraphicsService(Core core, bool validation)
		{
			if (!Glfw.VulkanSupported()) {
				throw new PlatformNotSupportedException("The Vulkan runtime is not available on this platform");
			}
			Core = core;

			// Create the instance and select the device to use
			InitializeVulkanInstance(this, validation, out InstanceData, out DebugUtils, out DeviceData);
			Instance = InstanceData.Instance;
			PhysicalDevice = DeviceData.PhysicalDevice;
			LINFO($"Selected device '{DeviceData.DeviceName}'");
			CreateVulkanDevice(this, out Device, out var graphicsQueue, out var graphicsQueueIndex);
			GraphicsQueue = new(this, graphicsQueue, graphicsQueueIndex);
			LINFO("Created Vulkan device instance");
			Limits = new(DeviceData);

			// Prepare resources
			Resources = new(this);
			Resources.RegisterThread();
		}
		~GraphicsService()
		{
			dispose(false);
		}

		// Performs end-frame graphics operations, such as advancing the graphics frame index and managing resources
		internal void EndFrame()
		{
			// Advance frame
			FrameIndex = (FrameIndex + 1) % MAX_FRAMES;

			// Run resource processing for the frame
			GraphicsQueue.UpdateContexts();
			Resources.EndFrame();
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
					Device.DeviceWaitIdle();

					Resources.UnregisterThread();
					Resources.Dispose();

					GraphicsQueue.Dispose();

					Device.DestroyDevice(null);
					LINFO("Destroyed Vulkan device");
					DebugUtils?.DestroyDebugUtilsMessengerEXT(null);
					Instance.DestroyInstance(null);
					LINFO("Destroyed Vulkan instance");
				}
			}
			IsDisposed = true;
		}
		#endregion // Disposable
	}
}
