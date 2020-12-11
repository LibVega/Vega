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
	// It defines either an msaa or non-msaa layout, so renderers with both will need two copies
	internal unsafe sealed class RenderLayout
	{
		private static readonly DependencyComparer COMPARER = new();

		#region Fields
		// Support flags
		public readonly bool HasMSAA;
		public readonly bool HasDepth;
		public readonly uint SubpassCount;
		public readonly uint? ResolveIndex;
		public readonly uint NonResolveCount;
		public readonly uint ColorAttachmentCount;

		// The objects that fully describe the layout
		public readonly Attachment[] Attachments;
		public readonly VkAttachmentDescription[] Descriptions;
		public readonly Subpass[] Subpasses;
		public readonly VkAttachmentReference[] References;
		public readonly uint[] Preserves;
		public readonly VkSubpassDependency[] Dependencies;
		#endregion // Fields

		public RenderLayout(RendererDescription desc, bool window, bool msaa)
		{
			// Populate simple values
			HasMSAA = desc.SupportsMSAA && msaa;
			HasDepth = desc.HasDepthAttachment;
			SubpassCount = desc.SubpassCount;
			ResolveIndex = desc.ResolveSubpass;
			NonResolveCount = (uint)desc.Attachments.Count;
			ColorAttachmentCount = 0;

			// For MSAA, calculate which attachments use MSAA and if they need resolves
			(uint idx, bool msaa, uint? resolve)[]? msaaState = null;
			uint extraAtt = 0;
			if (msaa) {
				msaaState = new (uint, bool, uint?)[desc.Attachments.Count];
				uint ai = 0;
				uint attidx = (uint)desc.Attachments.Count;
				foreach (var att in desc.Attachments) {
					var needmsaa = att.Uses[(int)ResolveIndex!.Value] == AttachmentUse.Output; // Output in resolve pass
					var lateuse = (att.Preserve || (att.LastUse.Index > ResolveIndex.Value)); // Preserve or post-resolve use
					msaaState[ai] = (ai, needmsaa, (needmsaa && lateuse) ? attidx++ : null);
					if (needmsaa && lateuse) {
						extraAtt += 1;
					}
					++ai;
					if (att.Format.IsColorFormat()) {
						ColorAttachmentCount += 1;
					}
				}
			}

			// Create the attachments
			if (msaa) {
				Attachments = new Attachment[desc.Attachments.Count + extraAtt];
				for (uint ai = 0; ai < desc.Attachments.Count; ++ai) {
					var att = desc.Attachments[(int)ai];
					(_, var ismsaa, var restarg) = msaaState![ai];
					Attachments[ai] = new(
						ai, restarg.GetValueOrDefault(0), att.Format, 
						att.Preserve && !restarg.HasValue, // Only preserve if intended, and not msaa
						ismsaa, false,
						att.Uses.Select((use, ai) => (ai <= ResolveIndex!.Value) ? (byte)use : (byte)AttachmentUse.Unused)
					);
				}
				uint ei = 0;
				foreach (var extra in msaaState!.Where(st => st.resolve.HasValue)) {
					var att = desc.Attachments[(int)extra.idx];
					var idx = desc.Attachments.Count + (ei++);
					Attachments[idx] = new(
						(uint)idx, 0, att.Format, att.Preserve, false, true,
						att.Uses.Select((use, ai) => (ai < ResolveIndex!.Value)
							? (byte)AttachmentUse.Unused
							: (ai == ResolveIndex.Value) ? Attachment.RESOLVE : (byte)use
						)
					);
				}
			}
			else {
				Attachments = desc.Attachments.Select((att, idx) => new Attachment(
					(uint)idx, (uint)idx, att.Format, att.Preserve, false, false, att.Uses.Select(use => (byte)use)
				)).ToArray();
			}

			// Create the descriptions
			Descriptions = Attachments.Select(att => {
				var isWindow = window && (att.Index == (msaa ? desc.Attachments.Count : 0));
				return new VkAttachmentDescription(
					flags: VkAttachmentDescriptionFlags.NoFlags,
					format: (VkFormat)att.Format,
					samples: VkSampleCountFlags.E1, // This is changed on build for MSAA renderers
					loadOp: att.Resolve ? VkAttachmentLoadOp.DontCare : VkAttachmentLoadOp.Clear,
					storeOp: att.Preserve ? VkAttachmentStoreOp.Store : VkAttachmentStoreOp.DontCare,
					stencilLoadOp: (att.Format.HasStencilComponent() && att.Resolve)
						? VkAttachmentLoadOp.DontCare : VkAttachmentLoadOp.Clear,
					stencilStoreOp: (att.Format.HasStencilComponent() && att.Preserve)
						? VkAttachmentStoreOp.Store : VkAttachmentStoreOp.DontCare,
					initialLayout: VkImageLayout.Undefined,
					finalLayout: att.Preserve
						? (isWindow ? VkImageLayout.PresentSrcKhr : VkImageLayout.ShaderReadOnlyOptimal)
						: att.LastUse switch {
							(byte)AttachmentUse.Input => VkImageLayout.ShaderReadOnlyOptimal,
							(byte)AttachmentUse.Output => att.Format.IsColorFormat()
								? VkImageLayout.ColorAttachmentOptimal
								: VkImageLayout.DepthStencilAttachmentOptimal,
							Attachment.RESOLVE => VkImageLayout.ColorAttachmentOptimal,
							_ => throw new Exception("Bad State")
						}
				);
			}).ToArray();

			// Create the subpasses, with reference and unused entries
			Subpasses = new Subpass[SubpassCount];
			var vkRefs = stackalloc VkAttachmentReference[(int)SubpassCount * Attachments.Length];
			var vkPres = stackalloc uint[(int)SubpassCount * Attachments.Length];
			uint refOff = 0, preOff = 0;
			for (uint si = 0; si < SubpassCount; ++si) {
				var inputs = Attachments.Where(att => att.Uses[si] == (byte)AttachmentUse.Input);
				var outputs = Attachments.Where(att => att.Uses[si] == (byte)AttachmentUse.Output);
				var unused = Attachments.Where(att => att.Uses[si] == (byte)AttachmentUse.Unused);

				// Inputs
				uint io = refOff;
				foreach (var att in inputs) {
					vkRefs[refOff++] = new(att.Index, VkImageLayout.ShaderReadOnlyOptimal);
				}

				// Outputs
				uint co = refOff;
				uint? dsidx = null;
				foreach (var att in outputs) {
					if (att.Format.IsColorFormat()) {
						vkRefs[refOff++] = new(att.Index, VkImageLayout.ColorAttachmentOptimal);
					}
					else {
						dsidx = att.Index;
					}
				}
				if (dsidx.HasValue) {
					vkRefs[refOff++] = new(dsidx.Value, VkImageLayout.DepthStencilAttachmentOptimal);
					dsidx = refOff - 1;
				}

				// Resolve
				uint? ro = null;
				if (msaa && (ResolveIndex!.Value == si)) {
					ro = refOff;
					foreach (var att in outputs) {
						bool dores = att.OutputIndex != 0;
						vkRefs[refOff++] = 
							new(dores ? att.OutputIndex : VkConstants.ATTACHMENT_UNUSED, VkImageLayout.ColorAttachmentOptimal);
					}
				}

				// Unused (todo: optimization? - don't preserve attachments that are done being used)
				uint po = preOff;
				foreach (var att in unused) {
					vkPres[preOff++] = att.Index;
				}

				// Describe subpass
				Subpasses[si] = new(
					io, co - io,
					co, (ro ?? refOff) - co - (dsidx.HasValue ? 1u : 0),
					dsidx,
					ro,
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
			// TODO: Optimize - refine this heuristic for creating dependencies, can be a major slowdown in render pass
			var deps = new HashSet<VkSubpassDependency>(COMPARER);
			foreach (var att in Attachments) {
				uint lastPass = VkConstants.SUBPASS_EXTERNAL;
				var lastMask = VkPipelineStageFlags.BottomOfPipe; // Less than ideal
				for (uint si = 0; si < RendererDescription.MAX_SUBPASSES; ++si) {
					var use = att.Uses[si];
					if (use == (byte)AttachmentUse.Unused) {
						continue;
					}

					var stageMask = use switch { 
						(byte)AttachmentUse.Input => VkPipelineStageFlags.FragmentShader,
						(byte)AttachmentUse.Output => att.Format.IsDepthFormat()
							? VkPipelineStageFlags.EarlyFragmentTests
							: VkPipelineStageFlags.ColorAttachmentOutput,
						Attachment.RESOLVE => VkPipelineStageFlags.ColorAttachmentOutput,
						_ => throw new Exception("Bad state")
					};
					deps.Add(new(
						srcSubpass: lastPass,
						dstSubpass: si,
						srcStageMask: lastMask,
						dstStageMask: stageMask,
						srcAccessMask: VkAccessFlags.MemoryWrite,
						dstAccessMask: VkAccessFlags.MemoryRead,
						dependencyFlags: VkDependencyFlags.ByRegion
					));
					lastPass = si;
					lastMask = use switch { 
						(byte)AttachmentUse.Input => VkPipelineStageFlags.FragmentShader,
						(byte)AttachmentUse.Output => att.Format.IsDepthFormat()
							? VkPipelineStageFlags.LateFragmentTests
							: VkPipelineStageFlags.ColorAttachmentOutput,
						Attachment.RESOLVE => VkPipelineStageFlags.ColorAttachmentOutput,
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
		public VkRenderPass CreateRenderpass(GraphicsDevice device, MSAA msaa)
		{
			// Check
			if ((msaa != MSAA.X1) && !HasMSAA) {
				throw new InvalidOperationException("LIBRARY ERROR - cannot build msaa render pass with non-msaa layout");
			}

			// Fix all required object arrays
			fixed (VkAttachmentDescription* descPtr = Descriptions)
			fixed (VkAttachmentReference* refPtr = References)
			fixed (uint* prePtr = Preserves)
			fixed (VkSubpassDependency* depPtr = Dependencies) {
				// Update the attachment description sample counts
				if (msaa != MSAA.X1) {
					for (int i = 0; i < Descriptions.Length; ++i) {
						bool isMsaa = Attachments[i].MSAA;
						descPtr[i].Samples = isMsaa ? (VkSampleCountFlags)msaa : VkSampleCountFlags.E1;
					}
				}

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
						resolveAttachments: sp.ResolveOffset.HasValue ? (refPtr + sp.ResolveOffset.Value) : null,
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
			public const byte RESOLVE = 255;

			public readonly uint Index;         // Attachment index
			public readonly uint OutputIndex;   // Used by MSAA attachments to point to their resolve attachment
			public readonly TexelFormat Format; // The format
			public readonly bool Preserve;      // If the attachment is preserved (mutually exclusive with MSAA)
			public readonly bool MSAA;          // If the attachment is MSAA (mutually exclusive with Preserve)
			public readonly bool Resolve;       // If the attachment is specificually used as a resolve attachment
			public fixed byte Uses[(int)RendererDescription.MAX_SUBPASSES]; // The attachment uses
			public readonly bool InputUse;
			public readonly bool OutputUse;

			// The last non-unused use of the attachment
			public byte LastUse {
				get {
					for (int i = (int)RendererDescription.MAX_SUBPASSES - 1; i >= 0; --i) {
						if (Uses[i] != (byte)AttachmentUse.Unused) {
							return Uses[i];
						}
					}
					throw new Exception("Bad State");
				}
			}

			public Attachment(uint index, uint outIdx, TexelFormat format, bool preserve, bool msaa, bool resolve, IEnumerable<byte> uses)
			{
				Index = index;
				OutputIndex = outIdx;
				Format = format;
				Preserve = preserve;
				MSAA = msaa;
				Resolve = resolve;
				int uidx = 0;
				InputUse = false;
				OutputUse = false;
				foreach (var u in uses) {
					Uses[uidx++] = u;
					if (u == (byte)AttachmentUse.Input) {
						InputUse = true;
					}
					if ((u == (byte)AttachmentUse.Output) || (u == RESOLVE)) {
						OutputUse = true;
					}
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
			public readonly uint? ResolveOffset;
			public readonly uint PreserveOffset;
			public readonly uint PreserveCount;

			public Subpass(
				uint iOff, uint iCnt,
				uint cOff, uint cCnt,
				uint? dOff,
				uint? rOff,
				uint pOff, uint pCnt
			)
			{
				InputOffset = iOff; InputCount = iCnt;
				ColorOffset = cOff; ColorCount = cCnt;
				DepthOffset = dOff;
				ResolveOffset = rOff;
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
