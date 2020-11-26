/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
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

		// The framebuffers for the render target
		private readonly VkFramebuffer[] _framebuffers;
		// The current framebuffer
		public VkFramebuffer CurrentFramebuffer => _framebuffers[Swapchain?.CurrentImageIndex ?? 0];

		// Disposal flag
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		public RenderTarget(Renderer renderer, Window window)
		{
			Renderer = renderer;
			Window = window;
			Window.Swapchain.Renderer = renderer;

			_framebuffers = new VkFramebuffer[Swapchain.MAX_IMAGE_COUNT];
			Rebuild();
		}
		~RenderTarget()
		{
			dispose(false);
		}

		// Rebuilds the additional images and framebuffers
		public void Rebuild()
		{
			// Destroy old framebuffers
			foreach (var fb in _framebuffers) {
				fb?.DestroyFramebuffer(null);
			}
			Array.Clear(_framebuffers, 0, _framebuffers.Length);

			// Create a new framebuffer for each swapchain iamge
			int fbidx = 0;
			foreach (var view in Swapchain.ImageViews) {
				var vhandle = view.Handle;
				VkFramebufferCreateInfo fbci = new(
					flags: VkFramebufferCreateFlags.NoFlags,
					renderPass: Renderer.RenderPass,
					attachmentCount: 1,
					attachments: &vhandle,
					width: Size.Width,
					height: Size.Height,
					layers: 1
				);
				VulkanHandle<VkFramebuffer> fbhandle;
				Renderer.Graphics.VkDevice.CreateFramebuffer(&fbci, null, &fbhandle)
					.Throw("Failed to create framebuffer");
				_framebuffers[fbidx++] = new(fbhandle, Renderer.Graphics.VkDevice);
			}
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
				foreach (var fb in _framebuffers) {
					fb?.DestroyFramebuffer(null);
				}
				Array.Clear(_framebuffers, 0, _framebuffers.Length);
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
