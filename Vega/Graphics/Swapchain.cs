/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Vk.Extras;
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
		// Subresource values
		private readonly Vk.ComponentMapping SWAPCHAIN_MAPPING = new(); // Identity mapping
		private readonly Vk.ImageSubresourceRange SWAPCHAIN_RANGE = new() {
			AspectMask = Vk.ImageAspectFlags.Color, BaseArrayLayer = 0, BaseMipLevel = 0,
			LayerCount = 1, LevelCount = 1
		};
		// Default clear color value
		private readonly Vk.ClearColorValue CLEAR_COLOR = new(0.1f, 0.1f, 0.1f, 1.0f);

		#region Fields
		// The window using this swapchain
		public readonly Window Window;

		// Vulkan objects
		private readonly Vk.PhysicalDevice _physicalDevice;
		private readonly Vk.Device _device;

		// Surface objects
		public readonly Vk.KHR.Surface Surface;
		private SurfaceInfo _surfaceInfo;
		public bool Vsync => _surfaceInfo.Mode == Vk.KHR.PresentMode.FifoKHR;
		public bool VsyncOnly => !_surfaceInfo.HasImmediate && !_surfaceInfo.HasMailbox;
		public Vk.Format SurfaceFormat => _surfaceInfo.Format.Format;

		// Swapchain objects
		public Vk.KHR.Swapchain Handle { get; private set; }
		private SwapchainInfo _swapchainInfo;
		private readonly Vk.Image[] _images;
		private readonly Vk.ImageView[] _imageViews;
		private readonly Vk.Semaphore[] _acquireSemaphores;
		private readonly Vk.Fence?[] _mappedFences;
		public uint ImageIndex => _swapchainInfo.ImageIndex;
		public uint ImageCount => _swapchainInfo.ImageCount;
		public IReadOnlyList<Vk.ImageView> ImageViews => _imageViews;

		// Sync objects
		private CommandObjects _cmd;

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
			Vk.KHR.SurfaceFormat[] sFmts;
			Vk.KHR.PresentMode[] sModes;
			{
				uint count = 0;
				_physicalDevice.GetPhysicalDeviceSurfaceFormatsKHR(Surface, &count, null);
				sFmts = new Vk.KHR.SurfaceFormat[count];
				_physicalDevice.GetPhysicalDeviceSurfaceFormatsKHR(Surface, sFmts);
				_physicalDevice.GetPhysicalDeviceSurfacePresentModesKHR(Surface, &count, null);
				sModes = new Vk.KHR.PresentMode[count];
				_physicalDevice.GetPhysicalDeviceSurfacePresentModesKHR(Surface, sModes);
			}
			if (sFmts.Length == 0 || sModes.Length == 0) {
				throw new PlatformNotSupportedException("Window context does not support presentation operations");
			}

			// Select surface info
			foreach (var prefFmt in PREFERRED_FORMATS) {
				if (sFmts.Contains(prefFmt)) {
					_surfaceInfo.Format = prefFmt;
					break;
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

			// Build sync objects
			_acquireSemaphores = new Vk.Semaphore[GraphicsService.MAX_FRAMES];
			_mappedFences = new Vk.Fence[GraphicsService.MAX_FRAMES];
			for (uint i = 0; i < GraphicsService.MAX_FRAMES; ++i) {
				Vk.SemaphoreCreateInfo.New(out var sci);
				_device.CreateSemaphore(&sci, null, out _acquireSemaphores[i]!).Throw("Swapchain acquire semaphore");
				_mappedFences[i] = null;
			}

			// Create command objects
			Vk.CommandPoolCreateInfo.New(out var cpci);
			cpci.QueueFamilyIndex = gs.GraphicsQueueIndex;
			_device.CreateCommandPool(&cpci, null, out _cmd.Pool!).Throw("Swapchain command pool");
			_cmd.ClearSemaphores = new Vk.Semaphore[GraphicsService.MAX_FRAMES];
			_cmd.RenderFences = new Vk.Fence[GraphicsService.MAX_FRAMES];
			for (uint i = 0; i < GraphicsService.MAX_FRAMES; ++i) {
				Vk.SemaphoreCreateInfo.New(out var sci);
				_device.CreateSemaphore(&sci, null, out _cmd.ClearSemaphores[i]!).Throw("Swapchain clear semaphore");
				Vk.FenceCreateInfo.New(out var fci);
				fci.Flags = Vk.FenceCreateFlags.Signaled;
				_device.CreateFence(&fci, null, out _cmd.RenderFences[i]!).Throw("Swapchain render fence");
			}
			{
				Vk.CommandBufferAllocateInfo.New(out var cbai);
				cbai.CommandPool = _cmd.Pool;
				cbai.Level = Vk.CommandBufferLevel.Primary;
				cbai.CommandBufferCount = GraphicsService.MAX_FRAMES;
				_device.AllocateCommandBuffers(cbai, out _cmd.Cmds).Throw("Swapchain command buffers");
			}

			// Do initial build
			Handle = Vk.KHR.Swapchain.Null;
			_swapchainInfo = new();
			_images = new Vk.Image[GraphicsService.MAX_FRAMES];
			_imageViews = new Vk.ImageView[GraphicsService.MAX_FRAMES];
			rebuild();
		}
		~Swapchain()
		{
			dispose(false);
		}

		public void SetVsync(bool vsync)
		{
			if (vsync == Vsync) return;

			var old = _surfaceInfo.Mode;
			_surfaceInfo.Mode =
				(vsync || VsyncOnly) ? Vk.KHR.PresentMode.FifoKHR :
				_surfaceInfo.HasMailbox ? Vk.KHR.PresentMode.MailboxKHR : Vk.KHR.PresentMode.ImmediateKHR;

			_swapchainInfo.Dirty = _surfaceInfo.Mode != old;
		}

		public void Present()
		{
			// Check attached renderer, or just clear for no attached renderer
			Vk.Semaphore renderSem = Vk.Semaphore.Null;
			object? renderer = null;
			if (renderer is not null) {
				// TODO
			}
			else {
				renderSem = _cmd.ClearSemaphores[_swapchainInfo.ImageIndex];
				var ssem = renderSem.Handle;
				var wsem = _acquireSemaphores[_swapchainInfo.ImageIndex].Handle;
				Vk.PipelineStageFlags WAIT_STAGE = Vk.PipelineStageFlags.Transfer;
				var cmd = _cmd.Cmds[_swapchainInfo.ImageIndex].Handle;

				Vk.SubmitInfo.New(out var si);
				si.WaitSemaphoreCount = 1;
				si.WaitSemaphores = &wsem;
				si.WaitDstStageMask = &WAIT_STAGE;
				si.CommandBufferCount = 1;
				si.CommandBuffers = &cmd;
				si.SignalSemaphoreCount = 1;
				si.SignalSemaphores = &ssem;
				Core.Instance!.Graphics.SubmitToGraphicsQueue(&si, _cmd.RenderFences[_swapchainInfo.SyncIndex])
					.Throw("Failed to submit window clear commands");
			}

			// Submit for presentation
			Vk.Result res;
			{
				var rsem = renderSem.Handle;
				var sc = Handle.Handle;
				var iidx = _swapchainInfo.ImageIndex;
				Vk.KHR.PresentInfo.New(out var pi);
				pi.WaitSemaphoreCount = 1;
				pi.WaitSemaphores = &rsem;
				pi.SwapchainCount = 1;
				pi.Swapchains = &sc;
				pi.ImageIndices = &iidx;
				res = Core.Instance!.Graphics.SubmitToGraphicsQueue(&pi);
			}
			_swapchainInfo.SyncIndex = (_swapchainInfo.SyncIndex + 1) % GraphicsService.MAX_FRAMES;
			if (_swapchainInfo.Dirty || (res == Vk.Result.SuboptimalKhr) || (res == Vk.Result.OutOfDateKhr)) {
				rebuild(); // Also acquires next image
			}
			else if (res != Vk.Result.Success) {
				throw new Vk.Extras.ResultException(res, "Failed to present to window");
			}
			else if (!acquire()) {
				throw new Exception("Failed to acquire window surface after presentation");
			}
		}

		private bool acquire()
		{
			// Wait for in-flight clear fence
			var currFence = _cmd.RenderFences[_swapchainInfo.SyncIndex].Handle;
			_device.WaitForFences(1, &currFence, true, UInt64.MaxValue);

			// Try to acquire the next image
			uint iidx = 0;
			var asem = _acquireSemaphores[_swapchainInfo.SyncIndex];
			var res = Handle.AcquireNextImageKHR(UInt64.MaxValue, asem, null, &iidx);
			if ((res == Vk.Result.SuboptimalKhr) || (res == Vk.Result.OutOfDateKhr)) {
				_swapchainInfo.Dirty = true;
				return false;
			}
			else if (res != Vk.Result.Success) {
				throw new Vk.Extras.ResultException(res, "Failed to acquire window surface");
			}
			_swapchainInfo.ImageIndex = iidx;

			// Wait for out-of-order fences and reset the fence for the new image
			if (_mappedFences[iidx]) {
				var mapFence = _mappedFences[iidx]!.Handle;
				_device.WaitForFences(1, &mapFence, true, UInt64.MaxValue);
			}
			_mappedFences[iidx] = _cmd.RenderFences[_swapchainInfo.SyncIndex];
			var resFence = _mappedFences[iidx]!.Handle;
			_device.ResetFences(1, &resFence);
			return true;
		}

		private void rebuild()
		{
			Stopwatch timer = Stopwatch.StartNew();

			_device.DeviceWaitIdle();
			var waitTime = timer.Elapsed;

			// Choose new extent and image count
			_physicalDevice.GetPhysicalDeviceSurfaceCapabilitiesKHR(Surface, out var caps).Throw("Surface caps");
			Extent2D newSize;
			if (caps.CurrentExtent.Width != UInt32.MaxValue) {
				newSize = new(caps.CurrentExtent.Width, caps.CurrentExtent.Height);
			}
			else {
				newSize = Extent2D.Clamp(Window.Size,
					new(caps.MinImageExtent.Width, caps.MinImageExtent.Height),
					new(caps.MaxImageExtent.Width, caps.MaxImageExtent.Height));
			}
			uint icnt = Math.Min(caps.MinImageCount + 1, GraphicsService.MAX_FRAMES);
			if ((caps.MaxImageCount != 0) && (icnt > caps.MaxImageCount)) {
				icnt = caps.MaxImageCount;
			}

			// Cancel rebuild on minimized window
			if (newSize == Extent2D.Zero) {
				return;
			}

			// Prepare swapchain info
			Vk.KHR.SwapchainCreateInfo.New(out var sci);
			sci.Surface = Surface;
			sci.MinImageCount = icnt;
			sci.ImageFormat = _surfaceInfo.Format.Format;
			sci.ImageColorSpace = _surfaceInfo.Format.ColorSpace;
			sci.ImageExtent = new() { Width = newSize.Width, Height = newSize.Height };
			sci.ImageArrayLayers = 1;
			sci.ImageUsage = Vk.ImageUsageFlags.ColorAttachment | Vk.ImageUsageFlags.TransferDst;
			sci.ImageSharingMode = Vk.SharingMode.Exclusive;
			sci.PreTransform = caps.CurrentTransform;
			sci.CompositeAlpha = Vk.KHR.CompositeAlphaFlags.OpaqueKHR;
			sci.PresentMode = _surfaceInfo.Mode;
			sci.OldSwapchain = Handle ? Handle : Vk.Handle<Vk.KHR.Swapchain>.Null;

			// Create swapchain and get images
			_device.CreateSwapchainKHR(&sci, null, out var nsc).Throw("Failed to create window swapchain");
			Array.Clear(_images, 0, _images.Length);
			uint imgCount = 0;
			{
				nsc!.GetSwapchainImagesKHR(&imgCount, null).Throw("Swapchain Images");
				var imgptr = stackalloc Vk.Handle<Vk.Image>[(int)imgCount];
				nsc.GetSwapchainImagesKHR(&imgCount, imgptr).Throw("Swapchain Images");
				for (uint i = 0; i < imgCount; ++i) {
					_images[i] = new(_device, imgptr[i]);
				}
			}

			// Free old image views
			foreach (var view in _imageViews) {
				view?.DestroyImageView(null);
			}
			Array.Clear(_imageViews, 0, _imageViews.Length);

			// Create new image views
			int vidx = 0;
			foreach (var img in _images) {
				if (img is null) break;
				Vk.ImageViewCreateInfo.New(out var ivci);
				ivci.Image = img;
				ivci.ViewType = Vk.ImageViewType.E2D;
				ivci.Format = _surfaceInfo.Format.Format;
				ivci.Components = SWAPCHAIN_MAPPING;
				ivci.SubresourceRange = SWAPCHAIN_RANGE;
				_device.CreateImageView(&ivci, null, out _imageViews[vidx++]!).Throw("Swapchain image view");
			}

			// Destroy the old swapchain
			if (Handle) {
				Handle.DestroySwapchainKHR(null);
			}

			// Update swapchain objects
			Handle = nsc;
			var oldSize = _swapchainInfo.Extent;
			_swapchainInfo.ImageIndex = 0;
			_swapchainInfo.SyncIndex = 0;
			_swapchainInfo.Dirty = false;
			_swapchainInfo.Extent = newSize;
			_swapchainInfo.ImageCount = imgCount;

			// Build the new clear commands
			_cmd.Pool.ResetCommandPool(Vk.CommandPoolResetFlags.NoFlags);
			uint iidx = 0;
			foreach (var cmd in _cmd.Cmds) {
				Vk.ImageMemoryBarrier.New(out var srcimb);
				srcimb.DstAccessMask = Vk.AccessFlags.MemoryWrite;
				srcimb.OldLayout = Vk.ImageLayout.Undefined;
				srcimb.NewLayout = Vk.ImageLayout.TransferDstOptimal;
				srcimb.SrcQueueFamilyIndex = Vk.Constants.QUEUE_FAMILY_IGNORED;
				srcimb.DstQueueFamilyIndex = Vk.Constants.QUEUE_FAMILY_IGNORED;
				srcimb.Image = _images[iidx];
				srcimb.SubresourceRange = SWAPCHAIN_RANGE;
				Vk.ImageMemoryBarrier dstimb = srcimb;
				dstimb.SrcAccessMask = Vk.AccessFlags.MemoryWrite;
				dstimb.DstAccessMask = Vk.AccessFlags.NoFlags;
				dstimb.OldLayout = Vk.ImageLayout.TransferDstOptimal;
				dstimb.NewLayout = Vk.ImageLayout.PresentSrcKHR;

				Vk.CommandBufferBeginInfo.New(out var cbbi);
				cmd.BeginCommandBuffer(&cbbi);
				cmd.PipelineBarrier(Vk.PipelineStageFlags.TopOfPipe, Vk.PipelineStageFlags.Transfer, 
					Vk.DependencyFlags.NoFlags, 0, null, 0, null, 1, &srcimb);
				cmd.ClearColorImage(_images[iidx], Vk.ImageLayout.TransferDstOptimal, CLEAR_COLOR, 
					new[] { SWAPCHAIN_RANGE });
				cmd.PipelineBarrier(Vk.PipelineStageFlags.Transfer, Vk.PipelineStageFlags.BottomOfPipe,
					Vk.DependencyFlags.NoFlags, 0, null, 0, null, 1, &dstimb);
				cmd.EndCommandBuffer();

				if (++iidx == imgCount) {
					break;
				}
			}

			// Acquire after rebuild
			if (!acquire()) {
				throw new Exception("Failed to acquire swapchain after rebuild");
			}

			LINFO($"Rebuilt swapchain (old={oldSize}) (new={newSize}) (time={timer.Elapsed.TotalMilliseconds}ms) " +
				$"(wait={waitTime.TotalMilliseconds}ms) (mode={_surfaceInfo.Mode}) (count={imgCount})");

			// Inform attached renderer of swapchain resize
			Window.Renderer?.OnSwapchainResize(newSize);
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

				// Swapchain Objects
				foreach (var view in _imageViews) {
					view?.DestroyImageView(null);
				}
				foreach (var sem in _acquireSemaphores) {
					sem.DestroySemaphore(null);
				}
				Handle?.DestroySwapchainKHR(null);
				LINFO("Destroyed window swapchain");

				// Sync/Command objects
				foreach (var sem in _cmd.ClearSemaphores) {
					sem.DestroySemaphore(null);
				}
				foreach (var fence in _cmd.RenderFences) {
					fence.DestroyFence(null);
				}
				_cmd.Pool.DestroyCommandPool(null);

				Surface?.DestroySurfaceKHR(null);
				LINFO("Destroyed window surface");
			}
			IsDisposed = true;
		}
		#endregion // IDisposable

		// Contains values for a swapchain surface
		private struct SurfaceInfo
		{
			public Vk.KHR.SurfaceFormat Format;
			public Vk.KHR.PresentMode Mode;
			public bool HasImmediate;
			public bool HasMailbox;
		}

		// Contains values for the swapchain object
		private struct SwapchainInfo
		{
			public Extent2D Extent;
			public uint ImageCount;
			public uint SyncIndex;
			public uint ImageIndex;
			public bool Dirty;
		}

		// Contains objects for syncronization
		private struct CommandObjects
		{
			public Vk.CommandPool Pool;
			public Vk.CommandBuffer[] Cmds;
			public Vk.Semaphore[] ClearSemaphores;
			public Vk.Fence[] RenderFences;
		}
	}
}
