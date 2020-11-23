/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Diagnostics;
using System.Linq;
using Vulkan;
using static Vega.InternalLog;

namespace Vega.Graphics
{
	// Represents/manages the swapchain of presentation images for a window
	internal unsafe sealed class Swapchain : IDisposable
	{
		// The number of virtual frames to use for the swapchain
		public const uint MAX_IMAGE_COUNT = 3;
		// The preferred surface formats
		private readonly VkSurfaceFormatKHR[] PREFERRED_FORMATS = {
			new() { Format = VkFormat.B8g8r8a8Unorm, ColorSpace = VkColorSpaceKHR.SrgbNonlinear },
			new() { Format = VkFormat.R8g8b8a8Unorm, ColorSpace = VkColorSpaceKHR.SrgbNonlinear }
		};
		// Subresource values
		private readonly VkComponentMapping SWAPCHAIN_MAPPING = new(); // Identity mapping
		private readonly VkImageSubresourceRange SWAPCHAIN_RANGE = new() {
			AspectMask = VkImageAspectFlags.Color, BaseArrayLayer = 0, BaseMipLevel = 0, LayerCount = 1, LevelCount = 1
		};

		#region Fields
		// The window owning the swapchain
		public readonly Window Window;
		// Reference to the graphics device
		public readonly GraphicsDevice Graphics;

		// Surface objects
		public readonly VkSurfaceKHR Surface;
		private SurfaceInfo _surfaceInfo;
		public bool Vsync => _surfaceInfo.Mode == VkPresentModeKHR.Fifo;
		public bool VsyncOnly => !_surfaceInfo.HasImmediate && !_surfaceInfo.HasMailbox;
		public VkFormat SurfaceFormat => _surfaceInfo.Format.Format;

		// Swapchain objects
		public VkSwapchainKHR Handle { get; private set; }
		private SwapchainInfo _swapchainInfo;
		private VkImage[] _images;
		private VkImageView[] _views;

		// Sync Objects
		private readonly SyncObjects _syncObjects;

		// Dispose flag
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		public Swapchain(Window window)
		{
			Window = window;
			Graphics = Core.Instance!.Graphics;

			// Create and check surface
			Glfw.CreateWindowSurface(Graphics.VkInstance, window.Handle, out var surface)
				.Throw("Failed to create window surface");
			Surface = new(surface, Graphics.VkInstance);
			Graphics.VkPhysicalDevice
				.GetPhysicalDeviceSurfaceSupportKHR(Graphics.GraphicsQueue.FamilyIndex, surface, out var presentSupport);
			if (!presentSupport) {
				throw new PlatformNotSupportedException("Selected device does not support window presentation");
			}

			// Get surface info
			VkSurfaceFormatKHR[] sFmts;
			VkPresentModeKHR[] sModes;
			{
				uint count = 0;
				Graphics.VkPhysicalDevice.GetPhysicalDeviceSurfaceFormatsKHR(Surface, &count, null);
				sFmts = new VkSurfaceFormatKHR[count];
				fixed (VkSurfaceFormatKHR* fmtPtr = sFmts) {
					Graphics.VkPhysicalDevice.GetPhysicalDeviceSurfaceFormatsKHR(Surface, &count, fmtPtr);
				}
				Graphics.VkPhysicalDevice.GetPhysicalDeviceSurfacePresentModesKHR(Surface, &count, null);
				sModes = new VkPresentModeKHR[count];
				fixed (VkPresentModeKHR* modePtr = sModes) {
					Graphics.VkPhysicalDevice.GetPhysicalDeviceSurfacePresentModesKHR(Surface, &count, modePtr);
				}
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
			_surfaceInfo.HasImmediate = sModes.Contains(VkPresentModeKHR.Immediate);
			_surfaceInfo.HasMailbox = sModes.Contains(VkPresentModeKHR.Mailbox);
			_surfaceInfo.Mode = VkPresentModeKHR.Fifo;
			LINFO($"Created window surface (format={_surfaceInfo.Format.Format}) " +
				$"(imm={_surfaceInfo.HasImmediate}) (mb={_surfaceInfo.HasMailbox})");

			// Create sync objects
			_syncObjects = new() {
				RenderSemaphores = new VkSemaphore[MAX_IMAGE_COUNT],
				AcquireSemaphores = new VkSemaphore[MAX_IMAGE_COUNT],
				RenderFences = new VkFence[MAX_IMAGE_COUNT],
				MappedFences = new VkFence[MAX_IMAGE_COUNT]
			};
			for (uint i = 0; i < MAX_IMAGE_COUNT; ++i) {
				VkSemaphoreCreateInfo sci = new(VkSemaphoreCreateFlags.NoFlags);
				VkFenceCreateInfo fci = new(VkFenceCreateFlags.Signaled);
				VulkanHandle<VkSemaphore> rHandle, aHandle;
				VulkanHandle<VkFence> fHandle;
				Graphics.VkDevice.CreateSemaphore(&sci, null, &rHandle).Throw("Swapchain render semaphore");
				Graphics.VkDevice.CreateSemaphore(&sci, null, &aHandle).Throw("Swapchain acquire semaphore");
				Graphics.VkDevice.CreateFence(&fci, null, &fHandle).Throw("Swapchain render fence");
				_syncObjects.RenderSemaphores[i] = new(rHandle, Graphics.VkDevice);
				_syncObjects.AcquireSemaphores[i] = new(aHandle, Graphics.VkDevice);
				_syncObjects.RenderFences[i] = new(fHandle, Graphics.VkDevice);
				_syncObjects.MappedFences[i] = null;
			}

			// Perform initial build
			Handle = new(VulkanHandle<VkSwapchainKHR>.Null, Graphics.VkDevice);
			_swapchainInfo = new();
			_images = new VkImage[0];
			_views = new VkImageView[0];
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
				(vsync || VsyncOnly) ? VkPresentModeKHR.Fifo :
				_surfaceInfo.HasMailbox ? VkPresentModeKHR.Mailbox : VkPresentModeKHR.Immediate;

			_swapchainInfo.Dirty = _surfaceInfo.Mode != old;
		}

		// Performs the swapchain present and swap, using the passed buffer for synchronization
		public void Present(CommandBuffer renderBuffer)
		{
			// Submit the buffer to be rendered, additionally performing necessary synchronization
			var rsem = _syncObjects.RenderSemaphores[_swapchainInfo.SyncIndex].Handle;
			{
				var asem = _syncObjects.AcquireSemaphores[_swapchainInfo.SyncIndex].Handle;
				var WAIT_STAGE = VkPipelineStageFlags.ColorAttachmentOutput;
				var cmd = renderBuffer.Cmd.Handle;

				VkSubmitInfo si = new(
					waitSemaphoreCount: 1,
					waitSemaphores: &asem,
					waitDstStageMask: &WAIT_STAGE,
					commandBufferCount: 1,
					commandBuffers: &cmd,
					signalSemaphoreCount: 1,
					signalSemaphores: &rsem
				);
				Graphics.GraphicsQueue.SubmitRaw(&si, _syncObjects.RenderFences[_swapchainInfo.SyncIndex])
					.Throw("Failed to submit window render commands");
			}

			// Submit for presentation
			VkResult res;
			{
				var sc = Handle.Handle;
				var iidx = _swapchainInfo.ImageIndex;
				VkPresentInfoKHR pi = new(
					waitSemaphoreCount: 1,
					waitSemaphores: &rsem,
					swapchainCount: 1,
					swapchains: &sc,
					imageIndices: &iidx
				);
				res = Graphics.GraphicsQueue.Present(&pi);
			}
			_swapchainInfo.SyncIndex = (_swapchainInfo.SyncIndex + 1) % MAX_IMAGE_COUNT;
			if (_swapchainInfo.Dirty || (res == VkResult.SuboptimalKhr) || (res == VkResult.ErrorOutOfDateKhr)) {
				rebuild(); // Also acquires
			}
			else if (res != VkResult.Success) {
				throw new InvalidOperationException($"Failed to present swapchain to surface ({res})");
			}
			else if (!acquire()) {
				throw new InvalidOperationException($"Failed to acquire window surface after presentation");
			}
		}

		private bool acquire()
		{
			// Wait for in-flight render fence
			var currFence = _syncObjects.RenderFences[_swapchainInfo.SyncIndex].Handle;
			Graphics.VkDevice.WaitForFences(1, &currFence, true, UInt64.MaxValue);

			// Try to acquire the next image
			uint iidx = 0;
			var asem = _syncObjects.AcquireSemaphores[_swapchainInfo.SyncIndex];
			var res = Handle.AcquireNextImageKHR(UInt64.MaxValue, asem, null, &iidx);
			if ((res == VkResult.SuboptimalKhr) || (res == VkResult.ErrorOutOfDateKhr)) {
				_swapchainInfo.Dirty = true;
				return false;
			}
			else if (res != VkResult.Success) {
				throw new InvalidOperationException($"Failed to acquire window surface ({res})");
			}
			_swapchainInfo.ImageIndex = iidx;

			// Wait for out-of-order fences and reset the fence for the new image
			if (_syncObjects.MappedFences[iidx] is not null) {
				var mapFence = _syncObjects.MappedFences[iidx]!.Handle;
				Graphics.VkDevice.WaitForFences(1, &mapFence, true, UInt64.MaxValue);
			}
			_syncObjects.MappedFences[iidx] = _syncObjects.RenderFences[_swapchainInfo.SyncIndex];
			var resFence = _syncObjects.MappedFences[iidx]!.Handle;
			Graphics.VkDevice.ResetFences(1, &resFence);
			return true;
		}

		private void rebuild()
		{
			var timer = Stopwatch.StartNew();

			// Wait for processing to finish before rebuilding
			Graphics.VkDevice.DeviceWaitIdle();
			var waitTime = timer.Elapsed;

			// New new extent and image count
			VkSurfaceCapabilitiesKHR caps;
			Graphics.VkPhysicalDevice.GetPhysicalDeviceSurfaceCapabilitiesKHR(Surface, &caps)
				.Throw("Failed to get surface capabilities");
			Extent2D newSize;
			if (caps.CurrentExtent.Width != UInt32.MaxValue) {
				newSize = new(caps.CurrentExtent.Width, caps.CurrentExtent.Height);
			}
			else {
				newSize = Extent2D.Clamp(Window.Size,
					new(caps.MinImageExtent.Width, caps.MinImageExtent.Height),
					new(caps.MaxImageExtent.Width, caps.MaxImageExtent.Height));
			}
			uint icnt = Math.Min(caps.MinImageCount + 1, MAX_IMAGE_COUNT);
			if ((caps.MaxImageCount != 0) && (icnt > caps.MaxImageCount)) {
				icnt = caps.MaxImageCount;
			}

			// Cancel rebuild on minimized window
			if (newSize == Extent2D.Zero) {
				return;
			}

			// Prepare swapchain info
			var oldHandle = Handle;
			VkSwapchainCreateInfoKHR scci = new(
				surface: Surface,
				minImageCount: MAX_IMAGE_COUNT,
				imageFormat: _surfaceInfo.Format.Format,
				imageColorSpace: _surfaceInfo.Format.ColorSpace,
				imageExtent: new(newSize.Width, newSize.Height),
				imageArrayLayers: 1,
				imageUsage: VkImageUsageFlags.ColorAttachment,
				imageSharingMode: VkSharingMode.Exclusive,
				preTransform: caps.CurrentTransform,
				compositeAlpha: VkCompositeAlphaFlagsKHR.Opaque,
				presentMode: _surfaceInfo.Mode,
				oldSwapchain: Handle
			);

			// Create swapchain and get images
			VulkanHandle<VkSwapchainKHR> newHandle;
			Graphics.VkDevice.CreateSwapchainKHR(&scci, null, &newHandle).Throw("Failed to create swapchain");
			Handle = new(newHandle, Handle.Parent);
			uint imgCount;
			{
				Handle.GetSwapchainImagesKHR(&imgCount, null);
				var imgHandles = stackalloc VulkanHandle<VkImage>[(int)imgCount];
				_images = new VkImage[imgCount];
				Handle.GetSwapchainImagesKHR(&imgCount, imgHandles);
				for (uint i = 0; i < imgCount; ++i) {
					_images[i] = new(imgHandles[i], Handle.Parent);
				}
			}

			// Destroy old objects
			foreach (var view in _views) {
				view.DestroyImageView(null);
			}
			if (oldHandle) {
				oldHandle.DestroySwapchainKHR(null);
			}

			// Create new views
			VkImageViewCreateInfo ivci = new(
				viewType: VkImageViewType.E2D,
				format: _surfaceInfo.Format.Format,
				components: SWAPCHAIN_MAPPING,
				subresourceRange: SWAPCHAIN_RANGE
			);
			_views = new VkImageView[_images.Length];
			for (int i = 0; i < _images.Length; ++i) {
				ivci.Image = _images[i];
				VulkanHandle<VkImageView> viewHandle;
				Graphics.VkDevice.CreateImageView(&ivci, null, &viewHandle).Throw("Failed to create swapchain view");
				_views[i] = new(viewHandle, Graphics.VkDevice);
			}

			// Update swapchain objects
			var oldSize = _swapchainInfo.Extent;
			_swapchainInfo.ImageIndex = 0;
			_swapchainInfo.SyncIndex = 0;
			_swapchainInfo.Dirty = false;
			_swapchainInfo.Extent = newSize;

			// Acquire
			if (!acquire()) {
				throw new InvalidOperationException("Failed to acquire swapchain after rebuild");
			}

			// Report
			LINFO($"Rebuilt swapchain (old={oldSize}) (new={newSize}) (time={timer.Elapsed.TotalMilliseconds}ms) " +
				$"(wait={waitTime.TotalMilliseconds}ms) (mode={_surfaceInfo.Mode}) (count={imgCount})");

			// TODO: Inform attached renderer of resize
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
				Graphics.VkDevice.DeviceWaitIdle();

				// Sync Objects
				for (uint i = 0; i < MAX_IMAGE_COUNT; ++i) {
					_syncObjects.RenderSemaphores[i].DestroySemaphore(null);
					_syncObjects.AcquireSemaphores[i].DestroySemaphore(null);
					_syncObjects.RenderFences[i].DestroyFence(null);
				}

				// Swapchain Objects
				foreach (var view in _views) {
					view.DestroyImageView(null);
				}
				Handle.DestroySwapchainKHR(null);

				Surface?.DestroySurfaceKHR(null);
				LINFO("Destroyed window surface");
			}
			IsDisposed = true;
		}
		#endregion // IDisposable

		// Values for swapchain surface
		private struct SurfaceInfo
		{
			public VkSurfaceFormatKHR Format;
			public VkPresentModeKHR Mode;
			public bool HasImmediate;
			public bool HasMailbox;
		}

		// Info about the swapchain
		private struct SwapchainInfo
		{
			public Extent2D Extent;
			public uint SyncIndex;
			public uint ImageIndex;
			public bool Dirty;
		}

		// Objects for syncing
		private struct SyncObjects
		{
			public VkSemaphore[] RenderSemaphores;
			public VkSemaphore[] AcquireSemaphores;
			public VkFence[] RenderFences;
			public VkFence?[] MappedFences;
		}
	}
}
