/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using VVK;
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

		// Vulkan objects
		internal readonly VulkanInstance Instance;
		internal readonly VulkanPhysicalDevice PhysicalDevice;
		internal readonly VulkanDevice Device;
		internal readonly VulkanQueue GraphicsQueue;
		internal Vk.Version ApiVersion => Instance.ApiVersion;

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
			InitializeVulkanInstance(this, validation, out Instance, out PhysicalDevice);
			LINFO($"Selected device '{PhysicalDevice.Name}'");
			CreateVulkanDevice(this, out Device, out GraphicsQueue);
			LINFO($"Created Vulkan device instance (GQ=[{GraphicsQueue.FamilyIndex}:{GraphicsQueue.QueueIndex}])");
		}
		~GraphicsService()
		{
			dispose(false);
		}

		#region Frames
		// Performs graphics operations for the start of the frame
		internal void BeginFrame()
		{
			InFrame = true;
		}

		// Performs graphics operations for the end of the frame
		internal void EndFrame()
		{
			// Advance frame
			InFrame = false;
			FrameIndex = (FrameIndex + 1) % MAX_FRAMES;
		}
		#endregion // Frames

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
					Device.Dispose();
					LINFO("Destroyed Vulkan device");
					Instance.Dispose();
					LINFO("Destroyed Vulkan instance");
				}
			}
			IsDisposed = true;
		}
		#endregion // Disposable
	}
}
