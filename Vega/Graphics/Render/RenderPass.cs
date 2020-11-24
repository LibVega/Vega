/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	// Manages the set of subpasses and transitions for a renderer object
	// Wraps a VkRenderPass object and associated data
	internal unsafe sealed class RenderPass : IDisposable
	{
		#region Fields
		// The attached renderer
		public readonly Renderer Renderer;

		// The render pass object
		public VkRenderPass Handle { get; private set; }

		// Disposal flag
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		public RenderPass(Renderer renderer, TexelFormat format)
		{
			Renderer = renderer;

			Handle = CreateRenderPass(renderer.Graphics.VkDevice, format);
		}
		~RenderPass()
		{
			dispose(false);
		}

		// Creates a render pass with the given information
		private static VkRenderPass CreateRenderPass(VkDevice device, TexelFormat format)
		{
			// Describe attachments
			VkAttachmentDescription attDesc = new(
				flags: VkAttachmentDescriptionFlags.NoFlags,
				format: (VkFormat)format,
				samples: VkSampleCountFlags.E1,
				loadOp: VkAttachmentLoadOp.Clear,
				storeOp: VkAttachmentStoreOp.Store,
				stencilLoadOp: VkAttachmentLoadOp.DontCare,
				stencilStoreOp: VkAttachmentStoreOp.DontCare,
				initialLayout: VkImageLayout.Undefined,
				finalLayout: VkImageLayout.PresentSrcKhr
			);

			// Describe subpasses
			VkAttachmentReference attRef = new(
				attachment: 0,
				layout: VkImageLayout.ColorAttachmentOptimal
			);
			VkSubpassDescription spass = new(
				flags: VkSubpassDescriptionFlags.NoFlags,
				pipelineBindPoint: VkPipelineBindPoint.Graphics,
				inputAttachmentCount: 0,
				inputAttachments: null,
				colorAttachmentCount: 1,
				colorAttachments: &attRef,
				resolveAttachments: null,
				depthStencilAttachment: null,
				preserveAttachmentCount: 0,
				preserveAttachments: null
			);

			// Describe dependencies
			VkSubpassDependency spDep = new(
				srcSubpass: VkConstants.SUBPASS_EXTERNAL,
				dstSubpass: 0,
				srcStageMask: VkPipelineStageFlags.ColorAttachmentOutput,
				dstStageMask: VkPipelineStageFlags.ColorAttachmentOutput,
				srcAccessMask: VkAccessFlags.NoFlags,
				dstAccessMask: VkAccessFlags.ColorAttachmentWrite,
				dependencyFlags: VkDependencyFlags.NoFlags
			);

			// Create the renderpass
			VkRenderPassCreateInfo rpci = new(
				flags: VkRenderPassCreateFlags.NoFlags,
				attachmentCount: 1,
				attachments: &attDesc,
				subpassCount: 1,
				subpasses: &spass,
				dependencyCount: 1,
				dependencies: &spDep
			);
			VulkanHandle<VkRenderPass> handle;
			device.CreateRenderPass(&rpci, null, &handle).Throw("Failed to create render pass");
			return new(handle, device);
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
				// The renderer using this pass should have already waited for device idle
				if (Handle) {
					Handle.DestroyRenderPass(null);
				}
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
