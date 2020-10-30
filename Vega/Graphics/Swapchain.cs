/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Linq;
using Vk;
using static Vega.InternalLog;

namespace Vega.Graphics
{
	// Manages the swapchain objects and operations for a single open application window
	internal unsafe sealed class Swapchain : IDisposable
	{
		// The preferred surface formats
		private readonly Vk.KHR.SurfaceFormat[] PREFERRED_FORMATS = { 
			new() { Format = Vk.Format.B8g8r8a8Unorm, ColorSpace = Vk.KHR.ColorSpace.SrgbNonlinearKHR },
			new() { Format = Vk.Format.R8g8b8a8Unorm, ColorSpace = Vk.KHR.ColorSpace.SrgbNonlinearKHR }
		};

		#region Fields
		// The window using this swapchain
		public readonly Window Window;

		// Vulkan objects
		private readonly Vk.PhysicalDevice _physicalDevice;
		private readonly Vk.Device _device;

		// Surface objects
		public readonly Vk.KHR.Surface Surface;
		private SurfaceInfo _surfaceInfo;

		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		public Swapchain(Window window)
		{
			var gs = Core.Instance!.Graphics;
			Window = window;
			_physicalDevice = gs.PhysicalDevice;
			_device = gs.Device;

			// Create the surface
			Glfw.CreateWindowSurface(gs.Instance, window.Handle, out var surfaceHandle)
				.Throw("Failed to create window surface");
			Surface = new(gs.Instance, surfaceHandle);
			_physicalDevice.GetPhysicalDeviceSurfaceSupportKHR(gs.GraphicsQueueIndex, Surface, out var presentSupport);
			if (!presentSupport) {
				throw new PlatformNotSupportedException("Selected device does not support window presentation");
			}

			// Get surface info
			Vk.KHR.SurfaceFormat[] sFmts = { };
			Vk.KHR.PresentMode[] sModes = { };
			{
				uint count = 0;
				_physicalDevice.GetPhysicalDeviceSurfaceFormatsKHR(Surface, &count, null);
				sFmts = new Vk.KHR.SurfaceFormat[count];
				_physicalDevice.GetPhysicalDeviceSurfaceFormatsKHR(Surface, out count, sFmts);
				_physicalDevice.GetPhysicalDeviceSurfacePresentModesKHR(Surface, &count, null);
				sModes = new Vk.KHR.PresentMode[count];
				_physicalDevice.GetPhysicalDeviceSurfacePresentModesKHR(Surface, out count, sModes);
			}
			if (sFmts.Length == 0 || sModes.Length == 0) {
				throw new PlatformNotSupportedException("Window context does not support presentation operations");
			}

			// Select surface info
			foreach (var prefFmt in PREFERRED_FORMATS) {
				if (sFmts.Contains(prefFmt)) {
					_surfaceInfo.Format = prefFmt;
				}
			}
			if (_surfaceInfo.Format == default) {
				_surfaceInfo.Format = sFmts[0];
			}
			_surfaceInfo.HasImmediate = sModes.Contains(Vk.KHR.PresentMode.ImmediateKHR);
			_surfaceInfo.HasMailbox = sModes.Contains(Vk.KHR.PresentMode.MailboxKHR);
			_surfaceInfo.Mode = Vk.KHR.PresentMode.FifoKHR;
			LINFO($"Created window surface (format={_surfaceInfo.Format.Format}) " +
				$"(imm={_surfaceInfo.HasImmediate}) (mb={_surfaceInfo.HasMailbox})");
		}
		~Swapchain()
		{
			dispose(false);
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed) {
				_device.DeviceWaitIdle();

				Surface?.DestroySurfaceKHR(null);
				LINFO("Destroyed window surface");
			}
			IsDisposed = true;
		}
		#endregion // IDisposable

		// Contains handles and values for a swapchain surface
		private struct SurfaceInfo
		{
			public Vk.KHR.SurfaceFormat Format;
			public Vk.KHR.PresentMode Mode;
			public bool HasImmediate;
			public bool HasMailbox;
		}
	}
}
