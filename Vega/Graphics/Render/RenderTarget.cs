/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	// Represents a set of images that can be rendered to, either in a swapchain or offscreen
	// Swaps a set of VkFramebuffer objects
	internal unsafe sealed class RenderTarget : IDisposable
	{
		#region Fields
		// The renderer using this target
		public readonly Renderer Renderer;
		// This window managed by the render target
		public readonly Window Window;
		// The swapchain of the attached window
		public Swapchain Swapchain => Window.Swapchain;

		// The size of the images in the render target
		public Extent2D Size => Swapchain.Size;
		// The current render target MSAA
		public MSAA MSAA { get; private set; } = MSAA.X1;

		// The image objects
		private readonly VkImage[] _images;
		private readonly MemoryAllocation[] _memorys;
		internal readonly VkImageView[] Views;

		// The framebuffers for the render target
		private readonly VkFramebuffer[] _framebuffers;
		// The current framebuffer
		public VkFramebuffer CurrentFramebuffer => _framebuffers[Swapchain?.CurrentImageIndex ?? 0];

		// Disposal flag
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		public RenderTarget(Renderer renderer, Window window, MSAA msaa)
		{
			Renderer = renderer;
			Window = window;
			Window.Swapchain.Renderer = renderer;

			// Do initial build
			var maxAtt = (renderer?.MSAALayout ?? renderer!.Layout).Attachments.Length;
			_images = new VkImage[maxAtt];
			_memorys = new MemoryAllocation[maxAtt];
			Views = new VkImageView[maxAtt];
			_framebuffers = new VkFramebuffer[Swapchain.MAX_IMAGE_COUNT];
			Rebuild(msaa);
		}
		~RenderTarget()
		{
			dispose(false);
		}

		// Rebuilds the additional images and framebuffers
		public void Rebuild(MSAA msaa)
		{
			// Destroy old framebuffers
			foreach (var fb in _framebuffers) {
				fb?.DestroyFramebuffer(null);
			}
			Array.Clear(_framebuffers, 0, _framebuffers.Length);

			// Destroy old images
			foreach (var view in Views) {
				view?.DestroyImageView(null);
			}
			Array.Clear(Views, 0, Views.Length);
			foreach (var image in _images) {
				image?.DestroyImage(null);
			}
			Array.Clear(_images, 0, _images.Length);
			foreach (var mem in _memorys) {
				mem?.Free();
			}
			Array.Clear(_memorys, 0, _memorys.Length);

			// Create new images
			var layout = (msaa != MSAA.X1) ? Renderer.MSAALayout! : Renderer.Layout;
			var windowIdx = (layout.HasMSAA ? layout.NonResolveCount : 0);
			uint idx = 0;
			foreach (var att in layout.Attachments) {
				if (idx != windowIdx) { // Skip attachment for window
					CreateImage(Renderer.Graphics, att, msaa, Size, out _images[idx], out _memorys[idx], out Views[idx]);
				}
				idx++;
			}

			// Create a new framebuffer for each swapchain iamge
			var viewHandles = stackalloc VulkanHandle<VkImageView>[Views.Length];
			int vidx = 0;
			foreach (var view in Views) {
				viewHandles[vidx++] = view?.Handle ?? VulkanHandle<VkImageView>.Null;
			}
			VkFramebufferCreateInfo fbci = new(
				flags: VkFramebufferCreateFlags.NoFlags,
				renderPass: Renderer.RenderPass,
				attachmentCount: (uint)layout.Attachments.Length,
				attachments: viewHandles,
				width: Size.Width,
				height: Size.Height,
				layers: 1
			);
			int fbidx = 0;
			foreach (var view in Swapchain.ImageViews) {
				viewHandles[windowIdx] = view.Handle;
				VulkanHandle<VkFramebuffer> fbhandle;
				Renderer.Graphics.VkDevice.CreateFramebuffer(&fbci, null, &fbhandle)
					.Throw("Failed to create framebuffer");
				_framebuffers[fbidx++] = new(fbhandle, Renderer.Graphics.VkDevice);
			}

			// Update values
			MSAA = msaa;
		}

		// Performs a buffer swap, either presenting the swapchain or swapping offscreen target
		// The buffer is the primary command buffer containing the renderer commands
		public void Swap(CommandBuffer buffer)
		{
			Swapchain.Present(buffer);
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
				// The renderer using this target should have already waited for device idle
				foreach (var view in Views) {
					view?.DestroyImageView(null);
				}
				foreach (var image in _images) {
					image?.DestroyImage(null);
				}
				foreach (var mem in _memorys) {
					mem?.Free();
				}
				foreach (var fb in _framebuffers) {
					fb?.DestroyFramebuffer(null);
				}
			}
			IsDisposed = true;
		}
		#endregion // IDisposable

		private static void CreateImage(GraphicsDevice graphics, in RenderLayout.Attachment att, MSAA msaa, Extent2D size,
			out VkImage image, out MemoryAllocation memory, out VkImageView view)
		{
			// Calculate usage
			var usage = VkImageUsageFlags.NoFlags;
			if (att.InputUse) {
				usage |= VkImageUsageFlags.InputAttachment;
			}
			if (att.OutputUse || att.Resolve) {
				usage |= att.Format.IsDepthFormat() 
					? VkImageUsageFlags.DepthStencilAttachment 
					: VkImageUsageFlags.ColorAttachment;
			}
			if (att.Preserve) {
				usage |= VkImageUsageFlags.Sampled;
			}

			// Create image
			VkImageCreateInfo ici = new(
				flags: VkImageCreateFlags.NoFlags,
				imageType: VkImageType.E2D,
				format: (VkFormat)att.Format,
				extent: new(size.Width, size.Height, 1),
				mipLevels: 1,
				arrayLayers: 1,
				samples: att.MSAA ? (VkSampleCountFlags)msaa : VkSampleCountFlags.E1,
				tiling: VkImageTiling.Optimal,
				usage: usage,
				sharingMode: VkSharingMode.Exclusive,
				queueFamilyIndexCount: 0,
				queueFamilyIndices: null,
				initialLayout: VkImageLayout.Undefined
			);
			VulkanHandle<VkImage> imageHandle;
			graphics.VkDevice.CreateImage(&ici, null, &imageHandle)
				.Throw("Failed to create framebuffer image");
			image = new(imageHandle, graphics.VkDevice);

			// Allocate and bind memory
			VkMemoryRequirements memreq;
			image.GetImageMemoryRequirements(&memreq);
			memory = graphics.Resources.AllocateMemoryDevice(memreq) ?? 
				throw new Exception("Failed to allocate framebuffer memory");
			image.BindImageMemory(memory.Handle, memory.Offset);

			// Create view
			VkImageViewCreateInfo ivci = new(
				flags: VkImageViewCreateFlags.NoFlags,
				image: imageHandle,
				viewType: VkImageViewType.E2D,
				format: ici.Format,
				components: new(), // Identity mapping
				subresourceRange: new(att.Format.GetAspectFlags(), 0, 1, 0, 1)
			);
			VulkanHandle<VkImageView> viewHandle;
			graphics.VkDevice.CreateImageView(&ivci, null, &viewHandle)
				.Throw("Failed to create framebuffer image view");
			view = new(viewHandle, graphics.VkDevice);
		}
	}
}
