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
		internal readonly Vk.Queue GraphicsQueue;
		internal readonly uint GraphicsQueueIndex;
		private readonly FastMutex _graphicsQueueLock = new();
		internal Vk.Version ApiVersion => Instance.Functions.CoreVersion;

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
		// If the graphics service is in the frame
		internal bool InFrame { get; private set; } = false;

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
			CreateVulkanDevice(this, out Device, out GraphicsQueue, out GraphicsQueueIndex);
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

		#region Frames
		// Performs graphics operations for the start of the frame
		internal void BeginFrame()
		{
			Resources.BeginFrame();
			InFrame = true;
		}

		// Performs graphics operations for the end of the frame
		internal void EndFrame()
		{
			// Advance frame
			InFrame = false;
			FrameIndex = (FrameIndex + 1) % MAX_FRAMES;

			Resources.EndFrame();
		}
		#endregion // Frames

		#region Commands
		// Submits a set of commands to the graphics queue
		internal Vk.Result SubmitToGraphicsQueue(in Vk.SubmitInfo si, Vk.Handle<Vk.Fence> fence)
		{
			using (var _ = _graphicsQueueLock.AcquireUNSAFE()) {
				fixed (Vk.SubmitInfo* siptr = &si) {
					return GraphicsQueue.QueueSubmit(1, siptr, fence);
				}
			}
		}

		// Submits a set of commands to the graphics queue
		internal Vk.Result SubmitToGraphicsQueue(Vk.SubmitInfo* si, Vk.Handle<Vk.Fence> fence)
		{
			using (var _ = _graphicsQueueLock.AcquireUNSAFE()) {
				return GraphicsQueue.QueueSubmit(1, si, fence);
			}
		}

		// Presents a swapchain
		internal Vk.Result SubmitToGraphicsQueue(Vk.KHR.PresentInfo* pi)
		{
			using (var _ = _graphicsQueueLock.AcquireUNSAFE()) {
				return GraphicsQueue.QueuePresentKHR(pi);
			}
		}
		#endregion // Commands

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
