/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Vulkan;

namespace Vega.Graphics
{
	// Acts as a "compiled" version of a validated RendererDescription for quick render pass builds
	internal unsafe sealed class RenderLayout
	{
		private static readonly DependencyComparer COMPARER = new();

		#region Fields
		// Support flags
		public readonly bool HasMSAA;
		public readonly bool HasDepth;
		public readonly uint SubpassCount;
		public readonly uint? ResolveIndex;

		// The objects that fully describe the layout
		public readonly Attachment[] Attachments;
		public readonly VkAttachmentDescription[] Descriptions;
		public readonly Subpass[] Subpasses;
		public readonly VkAttachmentReference[] References;
		public readonly uint[] Preserves;
		public readonly VkSubpassDependency[] Dependencies;
		#endregion // Fields

		public RenderLayout(RendererDescription desc, bool window)
		{
			// Populate simple values
			HasMSAA = desc.SupportsMSAA;
			HasDepth = desc.HasDepthAttachment;
			SubpassCount = desc.SubpassCount;
			ResolveIndex = desc.ResolveSubpass;

			// Create the attachments
			Attachments = desc.Attachments.Select((att, idx) => new Attachment(
				(uint)idx, att.Format, att.Preserve, false, att.Uses.Select(use => (byte)use)
			)).ToArray();

			// Create the descriptions
			Descriptions = desc.Attachments.Select((att, idx) => new VkAttachmentDescription(
				flags: VkAttachmentDescriptionFlags.NoFlags,
				format: (VkFormat)att.Format,
				samples: VkSampleCountFlags.E1,
				loadOp: VkAttachmentLoadOp.Clear,
				storeOp: att.Preserve ? VkAttachmentStoreOp.Store : VkAttachmentStoreOp.DontCare,
				stencilLoadOp: att.Format.HasStencilComponent()
					? VkAttachmentLoadOp.Clear : VkAttachmentLoadOp.DontCare,
				stencilStoreOp: (att.Format.HasStencilComponent() && att.Preserve)
					? VkAttachmentStoreOp.Store : VkAttachmentStoreOp.DontCare,
				initialLayout: VkImageLayout.Undefined,
				finalLayout: att.Preserve 
					? ((window && idx == 0) ? VkImageLayout.PresentSrcKhr : VkImageLayout.ShaderReadOnlyOptimal)
					: att.LastUse.Use switch {
						AttachmentUse.Input => VkImageLayout.ShaderReadOnlyOptimal,
						AttachmentUse.Output => att.IsColor
							? VkImageLayout.ColorAttachmentOptimal
							: VkImageLayout.DepthStencilAttachmentOptimal,
						_ => throw new Exception("Bad state")
					}
			)).ToArray();

			// Create the subpasses, with reference and unused entries
			Subpasses = new Subpass[SubpassCount];
			var vkRefs = stackalloc VkAttachmentReference[(int)SubpassCount * desc.Attachments.Count];
			var vkPres = stackalloc uint[(int)SubpassCount * desc.Attachments.Count];
			uint refOff = 0, preOff = 0;
			for (uint si = 0; si < SubpassCount; ++si) {
				var groups = desc.Attachments.Select((att, idx) => (att, idx: (uint)idx)).GroupBy(pair => pair.att.Uses[(int)si]);
				var inputs = groups.FirstOrDefault(grp => grp.Key == AttachmentUse.Input);
				var outputs = groups.FirstOrDefault(grp => grp.Key == AttachmentUse.Output);
				var unused = groups.FirstOrDefault(grp => grp.Key == AttachmentUse.Unused);
				uint io = refOff;
				if (inputs is not null) { // Build inputs
					foreach (var att in inputs) {
						vkRefs[refOff++] = new(att.idx, VkImageLayout.ShaderReadOnlyOptimal);
					}
				}
				uint co = refOff;
				uint? dsidx = null;
				if (outputs is not null) { // Build outputs
					foreach (var att in outputs) {
						if (att.att.IsColor) {
							vkRefs[refOff++] = new(att.idx, VkImageLayout.ColorAttachmentOptimal);
						}
						else {
							dsidx = att.idx;
						}
					}
					if (dsidx.HasValue) {
						vkRefs[refOff++] = new(dsidx.Value, VkImageLayout.DepthStencilAttachmentOptimal);
						dsidx = refOff - 1;
					}
				}
				uint po = preOff;
				if (unused is not null) {
					foreach (var att in unused) {
						vkPres[preOff++] = att.idx;
					}
				}

				// Describe subpass
				Subpasses[si] = new(
					io, co - io,
					co, refOff - co - (dsidx.HasValue ? 1u : 0),
					dsidx,
					po, preOff - po
				);
			}

			// Save references
			References = new VkAttachmentReference[refOff];
			Preserves = new uint[preOff];
			for (uint i = 0; i < refOff; ++i) {
				References[i] = vkRefs[i];
			}
			for (uint i = 0; i < preOff; ++i) {
				Preserves[i] = vkPres[i];
			}

			// Create subpass dependencies
			var deps = new HashSet<VkSubpassDependency>(COMPARER);
			foreach (var att in desc.Attachments) {
				var uses = att.Uses
					.Select((use, idx) => (use, idx: (uint)idx))
					.Where(use => use.use != AttachmentUse.Unused)
					.ToArray();
				uint lastPass = VkConstants.SUBPASS_EXTERNAL;
				var lastMask = VkPipelineStageFlags.BottomOfPipe; // Less than ideal
				foreach (var use in uses) {
					var stageMask = use.use switch { 
						AttachmentUse.Input => VkPipelineStageFlags.FragmentShader,
						AttachmentUse.Output => att.IsDepth
							? VkPipelineStageFlags.EarlyFragmentTests
							: VkPipelineStageFlags.ColorAttachmentOutput,
						_ => throw new Exception("Bad state")
					};
					deps.Add(new(
						srcSubpass: lastPass,
						dstSubpass: use.idx,
						srcStageMask: lastMask,
						dstStageMask: stageMask,
						srcAccessMask: VkAccessFlags.MemoryWrite,
						dstAccessMask: VkAccessFlags.MemoryRead,
						dependencyFlags: VkDependencyFlags.ByRegion
					));
					lastPass = use.idx;
					lastMask = use.use switch { 
						AttachmentUse.Input => VkPipelineStageFlags.FragmentShader,
						AttachmentUse.Output => att.IsDepth
							? VkPipelineStageFlags.LateFragmentTests
							: VkPipelineStageFlags.ColorAttachmentOutput,
						_ => throw new Exception("Bad state")
					};
				}
				if (att.Preserve) {
					deps.Add(new(
						srcSubpass: lastPass,
						dstSubpass: VkConstants.SUBPASS_EXTERNAL,
						srcStageMask: lastMask,
						dstStageMask: VkPipelineStageFlags.FragmentShader | VkPipelineStageFlags.Transfer,
						srcAccessMask: VkAccessFlags.MemoryWrite,
						dstAccessMask: VkAccessFlags.MemoryRead,
						dependencyFlags: VkDependencyFlags.ByRegion
					));
				}
			}
			Dependencies = deps.ToArray();
		}

		// Creates a renderpass that matches the layout
		public VkRenderPass CreateRenderpass(GraphicsDevice device)
		{
			// Fix all required object arrays
			fixed (VkAttachmentDescription* descPtr = Descriptions)
			fixed (VkAttachmentReference* refPtr = References)
			fixed (uint* prePtr = Preserves)
			fixed (VkSubpassDependency* depPtr = Dependencies) {
				// Describe the subpasses
				var passPtr = stackalloc VkSubpassDescription[(int)SubpassCount];
				for (uint si = 0; si < SubpassCount; ++si) {
					ref Subpass sp = ref Subpasses[si];
					passPtr[si] = new(
						flags: VkSubpassDescriptionFlags.NoFlags,
						pipelineBindPoint: VkPipelineBindPoint.Graphics,
						inputAttachmentCount: sp.InputCount,
						inputAttachments: refPtr + sp.InputOffset,
						colorAttachmentCount: sp.ColorCount,
						colorAttachments: refPtr + sp.ColorOffset,
						resolveAttachments: null,
						depthStencilAttachment: sp.DepthOffset.HasValue ? (refPtr + sp.DepthOffset.Value) : null,
						preserveAttachmentCount: sp.PreserveCount,
						preserveAttachments: prePtr + sp.PreserveOffset
					);
				}

				// Create the renderpass
				VkRenderPassCreateInfo rpci = new(
					flags: VkRenderPassCreateFlags.NoFlags,
					attachmentCount: (uint)Descriptions.Length,
					attachments: descPtr,
					subpassCount: SubpassCount,
					subpasses: passPtr,
					dependencyCount: (uint)Dependencies.Length,
					dependencies: depPtr
				);
				VulkanHandle<VkRenderPass> handle;
				device.VkDevice.CreateRenderPass(&rpci, null, &handle).Throw("Failed to create render pass object");
				return new(handle, device.VkDevice);
			}
		}

		// Contains information about an attachment
		public struct Attachment
		{
			public readonly uint Index;         // Attachment index
			public readonly TexelFormat Format; // The format
			public readonly bool Preserve;      // If the attachment is preserved (mutually exclusive with MSAA)
			public readonly bool MSAA;          // If the attachment is MSAA (mutually exclusive with Preserve)
			public fixed byte Uses[(int)RendererDescription.MAX_SUBPASSES]; // The attachment uses

			public Attachment(uint index, TexelFormat format, bool preserve, bool msaa, IEnumerable<byte> uses)
			{
				Index = index;
				Format = format;
				Preserve = preserve;
				MSAA = msaa;
				int uidx = 0;
				foreach (var u in uses) {
					Uses[uidx++] = u;
				}
			}
		}

		// Contains information about a subpass
		public struct Subpass
		{
			public readonly uint InputOffset;
			public readonly uint InputCount;
			public readonly uint ColorOffset;
			public readonly uint ColorCount;
			public readonly uint? DepthOffset;
			public readonly uint PreserveOffset;
			public readonly uint PreserveCount;

			public Subpass(
				uint iOff, uint iCnt,
				uint cOff, uint cCnt,
				uint? dOff,
				uint pOff, uint pCnt
			)
			{
				InputOffset = iOff; InputCount = iCnt;
				ColorOffset = cOff; ColorCount = cCnt;
				DepthOffset = dOff;
				PreserveOffset = pOff; PreserveCount = pCnt;
			}
		}

		// Comparator for subpass dependencies
		public class DependencyComparer : IEqualityComparer<VkSubpassDependency>
		{
			bool IEqualityComparer<VkSubpassDependency>.Equals(VkSubpassDependency x, VkSubpassDependency y) => x == y;
			int IEqualityComparer<VkSubpassDependency>.GetHashCode(VkSubpassDependency obj) => obj.GetHashCode();
		}
	}
}
