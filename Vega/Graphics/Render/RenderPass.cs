/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Vk.Extras;

namespace Vega.Graphics
{
	// Wraps a Vulkan framebuffer object for a Renderer, manages framebuffer resources and rebuilds
	internal unsafe sealed class RenderPass : IDisposable
	{
		private static readonly SubpassDependencyComparer COMPARER = new();

		#region Fields
		// The renderer using this renderpass
		public readonly Renderer Renderer;
		// The window that this renderpass is associated with
		public readonly Window? Window;

		// Info
		// If at least one attachment supports msaa
		public readonly bool HasMSAA;
		// The number of non-resolve attachments
		public readonly int NonResolveCount;
		// The msaa last built against
		public MSAA LastMSAA { get; private set; }

		// Framebuffer data/objects
		// The attachment objects
		public readonly List<Attachment> Attachments;
		// The framebuffer object(s) - multiple for windows
		public readonly List<Vk.Framebuffer> Framebuffers;
		// The current framebuffer object (per-frame)
		public Vk.Framebuffer CurrentFramebuffer => (Window is not null) 
			? Framebuffers[(int)Window.Swapchain.ImageIndex]
			: Framebuffers[0];

		// Renderpass data/objects
		// Non-MSAA renderpass
		public readonly Vk.RenderPass Handle;
		// MSAA renderpass
		public Vk.RenderPass MSAAHandle { get; private set; }

		// If the renderpass is disposed
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		// Create the attachment descriptions and render pass, but not the attachment objects
		public RenderPass(Renderer renderer, RendererDescription desc, Window? window)
		{
			Renderer = renderer;
			Window = window;

			// Generate the info
			HasMSAA = desc.Attachments.Any(att => att.MSAA);
			NonResolveCount = desc.AttachmentCount;
			LastMSAA = MSAA.X1;

			// Generate the attachment infos
			// The non-msaa and non-resolve attachments first, then the resolve attachments as needed
			Attachments = new();
			uint aidx = 0;
			foreach (var att in desc.Attachments) {
				Attachments.Add(new(aidx++, att.Format, att.MSAA, att.Preserve, att.Timeline.Any(use => use == AttachmentUse.Input)));
			}
			foreach (var att in desc.Attachments.Select((att, i) => (att, i))
												.Where(pair => pair.att.MSAA && pair.att.ResolveSubpass.HasValue)) {
				Attachments.Add(new(aidx++, att.att.Format, false, att.att.Preserve, att.att.Timeline.Any(use => use == AttachmentUse.Input)));
				Attachments[att.i].ResolveAttachment = (uint)Attachments.Count - 1;
				Attachments.Last().MSAAAttachment = (uint)att.i;
			}

			// Generate uses
			for (int i = 0; i < Attachments.Count; ++i) {
				var att = Attachments[i];
				if (i < NonResolveCount) { // Non-MSAA or non-resolve targets
					att.Uses.AddRange(desc.Attachments[i].Timeline.Select(use => (Attachment.Use)use));
					att.MSAAUses.AddRange(desc.Attachments[i].Timeline.Select((use, si) => {
						if (!desc.Attachments[i].MSAA) return (Attachment.Use)use; // No difference for non-MSAA
						var respass = desc.Attachments[i].ResolveSubpass;
						if (respass.HasValue && (si > respass.Value)) {
							return Attachment.Use.None; // No use after resolve
						}
						return (Attachment.Use)use; // Never resolved, or before/at the resolve point
					}));
				}
				else { // Special MSAA targets
					var matt = desc.Attachments[(int)att.MSAAAttachment!.Value];
					att.MSAAUses.AddRange(matt.Timeline.Select((use, si) => {
						var respass = matt.ResolveSubpass!.Value;
						if (si > respass) return (Attachment.Use)use; // Normal use after resolve
						if (si == respass) return Attachment.Use.Resolve; // Resolve on resolve subpass index
						else return Attachment.Use.None; // No use before resolve
					}));
				}
			}

			// Create normal renderpass, since that will not be rebuilt
			// MSAA renderpass will need to be rebuild any time the MSAA changes
			MakeRenderpass(Attachments, Window is not null, MSAA.X1, out Handle);
			MSAAHandle = Vk.RenderPass.Null;

			// No framebuffers yet
			Framebuffers = new();
		}
		~RenderPass()
		{
			dispose(false);
		}

		// Performs a rebuild of the framebuffer images
		public void Rebuild(Extent2D size, MSAA msaa)
		{
			var gs = Core.Instance!.Graphics;
			if (!gs.Limits.IsMSAASupported(msaa)) {
				throw new ArgumentException($"Unsupported MSAA level {msaa}", nameof(msaa));
			}
			if ((msaa != MSAA.X1) && !HasMSAA) {
				throw new ArgumentException("Framebuffer does not support MSAA", nameof(msaa));
			}
			if (Window is not null) {
				size = Window.Size;
			}
			if ((size.Width > gs.Limits.MaxFramebufferSize.Width) || 
					(size.Height > gs.Limits.MaxFramebufferSize.Height)) {
				throw new ArgumentException(
					$"Framebuffer size is not supported ({size} > {gs.Limits.MaxFramebufferSize})", nameof(size));
			}

			// Free old resources
			gs.Device.DeviceWaitIdle();
			freeResources();

			// Build new images
			var buildCount = (msaa != MSAA.X1) ? Attachments.Count : NonResolveCount;
			for (int i = 0; i < buildCount; ++i) {
				var isWindow = (Window is not null) && (((msaa == MSAA.X1) && (i == 0)) || ((msaa != MSAA.X1) && (i == NonResolveCount)));
				if (!isWindow) {
					var att = Attachments[i];
					MakeImage(att.Format, size, att.MSAA ? msaa : MSAA.X1, att.Preserve, att.Input,
						out att.Image, out att.View, out att.Memory);
				}
			}

			// Recreate the MSAA renderpass, if needed
			if (HasMSAA && (LastMSAA != msaa) && (msaa != MSAA.X1)) {
				if (MSAAHandle) {
					MSAAHandle.DestroyRenderPass(null);
				}
				MakeRenderpass(Attachments, Window is not null, msaa, out var newHandle);
				MSAAHandle = newHandle;
				LastMSAA = msaa;
			}

			// Recreate the framebuffer(s)
			if (Window is not null) {
				var attptr = stackalloc Vk.Handle<Vk.ImageView>[buildCount];
				for (int i = 0; i < buildCount; ++i) {
					attptr[i] = Attachments[i].View;
				}
				Vk.FramebufferCreateInfo fbci = new(
					flags: Vk.FramebufferCreateFlags.NoFlags,
					renderPass: (msaa != MSAA.X1) ? MSAAHandle : Handle,
					attachmentCount: (uint)buildCount,
					attachments: attptr,
					width: size.Width,
					height: size.Height,
					layers: 1
				);

				foreach (var view in Window.Swapchain.ImageViews) {
					if (!view) continue;
					attptr[(msaa == MSAA.X1) ? 0 : NonResolveCount] = view;
					gs.Device.CreateFramebuffer(&fbci, null, out var fbhandle)
						.Throw("Failed to create window surface frambuffer");
					Framebuffers.Add(fbhandle);
				}
			}
			else {
				var attptr = stackalloc Vk.Handle<Vk.ImageView>[buildCount];
				for (int i = 0; i < buildCount; ++i) {
					attptr[i] = Attachments[i].View;
				}
				Vk.FramebufferCreateInfo fbci = new(
					flags: Vk.FramebufferCreateFlags.NoFlags,
					renderPass: (msaa != MSAA.X1) ? MSAAHandle : Handle,
					attachmentCount: (uint)buildCount,
					attachments: attptr,
					width: size.Width,
					height: size.Height,
					layers: 1
				);
				gs.Device.CreateFramebuffer(&fbci, null, out var fbhandle)
					.Throw("Failed to create offscreen framebuffer");
				Framebuffers.Add(fbhandle);
			}
		}

		// Destroys and frees the resources for the framebuffer
		private void freeResources()
		{
			foreach (var fb in Framebuffers) {
				if (fb) {
					fb.DestroyFramebuffer(null);
				}
			}
			Framebuffers.Clear();
			foreach (var att in Attachments) {
				if (att.View) {
					att.View.DestroyImageView(null);
					att.View = Vk.ImageView.Null;
				}
				if (att.Image) {
					att.Image.DestroyImage(null);
					att.Image = Vk.Image.Null;
				}
				if (att.Memory is not null) {
					att.Memory.Free();
					att.Memory = null;
				}
			}
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
				Core.Instance?.Graphics.Device.DeviceWaitIdle();
				Handle.DestroyRenderPass(null);
				if (MSAAHandle) {
					MSAAHandle.DestroyRenderPass(null);
				}
				freeResources();
			}
			IsDisposed = true;
		}
		#endregion // IDisposable

		// Performs allocation and binding of image resources and memory
		private static void MakeImage(TexelFormat fmt, Extent2D size, MSAA msaa, bool preserve, bool input,
			out Vk.Image image, out Vk.ImageView view, out MemoryAllocation memory)
		{
			// Calculate usage
			var gs = Core.Instance!.Graphics;
			var usage = fmt.IsDepthFormat() ? Vk.ImageUsageFlags.DepthStencilAttachment : Vk.ImageUsageFlags.ColorAttachment;
			if (preserve) {
				usage |= Vk.ImageUsageFlags.Sampled;
			}
			if (input) {
				usage |= Vk.ImageUsageFlags.InputAttachment;
			}
			if (!preserve && gs.Resources.HasTransientMemory) {
				usage |= Vk.ImageUsageFlags.TransientAttachment;
			}
			var aspect = fmt.IsDepthFormat() ? Vk.ImageAspectFlags.Depth : Vk.ImageAspectFlags.Color;
			if (fmt.HasStencilComponent()) {
				aspect |= Vk.ImageAspectFlags.Stencil;
			}

			// Create image
			Vk.ImageCreateInfo ici = new(
				imageType: Vk.ImageType.E2D,
				format: (Vk.Format)fmt,
				extent: new(size.Width, size.Height, 1),
				mipLevels: 1,
				arrayLayers: 1,
				samples: (Vk.SampleCountFlags)msaa,
				tiling: Vk.ImageTiling.Optimal,
				usage: usage,
				sharingMode: Vk.SharingMode.Exclusive,
				initialLayout: Vk.ImageLayout.Undefined
			);
			gs.Device.CreateImage(&ici, null, out image).Throw("Failed to create framebuffer image");

			// Allocate memory
			image.GetImageMemoryRequirements(out var memReq);
			if (gs.Resources.HasTransientMemory) {
				memory = gs.Resources.AllocateMemoryTransient(memReq)
					?? throw new Exception("Failed to allocate transient memory for framebuffer image");
			}
			else {
				memory = gs.Resources.AllocateMemoryDevice(memReq)
					?? throw new Exception("Failed to allocate memory for framebuffer image");
			}
			image.BindImageMemory(memory.Handle, memory.Offset).Throw("Failed to bind framebuffer image memory");

			// Create view
			Vk.ImageViewCreateInfo ivci = new(
				image: image,
				viewType: Vk.ImageViewType.E2D,
				format: ici.Format,
				components: new(),
				subresourceRange: new(aspect, 0, 1, 0, 1)
			);
			gs.Device.CreateImageView(&ivci, null, out view).Throw("Failed to create framebuffer image view");
		}

		// Creates a renderpass for the given attachments and msaa flag
		private static void MakeRenderpass(List<Attachment> attachments, bool window, MSAA msaa, out Vk.RenderPass renderPass)
		{
			bool useMsaa = msaa != MSAA.X1;

			// Describe the attachments
			var acount = useMsaa ? attachments.Count : attachments.Count(att => !att.MSAAAttachment.HasValue);
			var adesc = stackalloc Vk.AttachmentDescription[acount];
			for (int ai = 0; ai < acount; ++ai) {
				var att = attachments[ai];
				var preserve = att.Preserve && (!useMsaa || !att.ResolveAttachment.HasValue);
				var lastUse = (useMsaa ? att.MSAAUses : att.Uses).Where(use => use != Attachment.Use.None).Last();
				var isWindow = window && ((!useMsaa && (ai == 0)) || (useMsaa && (ai == attachments[0].ResolveAttachment!.Value)));
				var tmp = adesc[ai] = new(
					flags: Vk.AttachmentDescriptionFlags.NoFlags,
					format: (Vk.Format)att.Format,
					samples: att.MSAA ? (Vk.SampleCountFlags)msaa : Vk.SampleCountFlags.E1,
					loadOp: Vk.AttachmentLoadOp.Clear,
					storeOp: preserve ? Vk.AttachmentStoreOp.Store : Vk.AttachmentStoreOp.DontCare,
					stencilLoadOp: Vk.AttachmentLoadOp.Clear,
					stencilStoreOp: (att.Format.HasStencilComponent() && preserve)
						? Vk.AttachmentStoreOp.Store : Vk.AttachmentStoreOp.DontCare,
					initialLayout: Vk.ImageLayout.Undefined,
					finalLayout: preserve
						? (isWindow ? Vk.ImageLayout.PresentSrcKHR : Vk.ImageLayout.ShaderReadOnlyOptimal)
						: lastUse switch { 
							Attachment.Use.Input => Vk.ImageLayout.ShaderReadOnlyOptimal,
							Attachment.Use.Output => att.Format.IsColorFormat()
								? Vk.ImageLayout.ColorAttachmentOptimal
								: att.Format.HasStencilComponent()
									? Vk.ImageLayout.DepthStencilAttachmentOptimal 
									: Vk.ImageLayout.DepthAttachmentOptimal,
							Attachment.Use.Resolve => Vk.ImageLayout.ColorAttachmentOptimal,
							_ => throw new Exception("Never reached")
						}
				);
			}

			// Describe the subpasses
			var scount = attachments[0].Uses.Count;
			var sdesc = stackalloc Vk.SubpassDescription[scount];
			var aref = stackalloc Vk.AttachmentReference[scount * acount];
			var uidx = stackalloc uint[scount * acount];
			for (int si = 0; si < scount; ++si) {
				uint baseIdx = (uint)(si * acount);
				uint icnt = 0, ocnt = 0, rcnt = 0, ucnt = 0;
				IEnumerable<Attachment>
					iatts = attachments.Take(acount).Where(att => (useMsaa ? att.MSAAUses : att.Uses)[si] == Attachment.Use.Input),
					oatts = attachments.Take(acount).Where(att => (useMsaa ? att.MSAAUses : att.Uses)[si] == Attachment.Use.Output)
													.Where(att => !att.Format.IsDepthFormat()),
					uatts = attachments.Take(acount).Where(att => (useMsaa ? att.MSAAUses : att.Uses)[si] == Attachment.Use.None);
				foreach (var att in iatts) {
					var tmp2 = aref[baseIdx + icnt++] = new(att.Index, Vk.ImageLayout.ShaderReadOnlyOptimal);
				}
				foreach (var att in oatts) {
					var tmp2 = aref[baseIdx + icnt + ocnt++] = new(att.Index, Vk.ImageLayout.ColorAttachmentOptimal);
				}
				var rany = false;
				if (useMsaa) {
					foreach (var att in oatts) {
						var resolved = att.ResolveAttachment.HasValue &&
							(attachments[(int)att.ResolveAttachment.Value].MSAAUses[si] == Attachment.Use.Resolve);
						var tmp2 = aref[baseIdx + icnt + ocnt + rcnt++] =
							new(resolved ? att.ResolveAttachment!.Value : Vk.Constants.ATTACHMENT_UNUSED, Vk.ImageLayout.ColorAttachmentOptimal);
						rany = rany || resolved;
					}
				}
				foreach (var att in uatts) {
					var tmp2 = uidx[baseIdx + ucnt++] = att.Index;
				}
				var datt = attachments
					.Take(acount)
					.Where(att => att.Format.IsDepthFormat() && ((useMsaa ? att.MSAAUses : att.Uses)[si] == Attachment.Use.Output))
					.FirstOrDefault();
				uint? doff = null;
				if (datt is not null) {
					var layout = datt.Format.HasStencilComponent()
						? Vk.ImageLayout.DepthStencilAttachmentOptimal
						: Vk.ImageLayout.DepthAttachmentOptimal;
					doff = icnt + ocnt + rcnt;
					var tmp2 = aref[baseIdx + doff.Value] = new(datt.Index, layout);
				}
				var tmp = sdesc[si] = new(
					flags: Vk.SubpassDescriptionFlags.NoFlags,
					pipelineBindPoint: Vk.PipelineBindPoint.Graphics,
					inputAttachmentCount: icnt,
					inputAttachments: aref + baseIdx,
					colorAttachmentCount: ocnt,
					colorAttachments: aref + baseIdx + icnt,
					resolveAttachments: rany ? (aref + baseIdx + icnt + ocnt) : null,
					depthStencilAttachment: doff.HasValue ? (aref + doff.Value) : null,
					preserveAttachmentCount: ucnt,
					preserveAttachments: uidx + baseIdx
				);
			}

			// Build the dependencies
			// TODO: This is likely creating more dependencies than necessary (slow down point)
			//       We need a better heuristic for creating dependencies
			var sdeps = new HashSet<Vk.SubpassDependency>(COMPARER);
			foreach (var att in attachments.Take(acount)) {
				var realuses = (useMsaa ? att.MSAAUses : att.Uses)
					.Select((use, ui) => (use, idx: (uint)ui))
					.Where(use => use.use != Attachment.Use.None)
					.ToArray();
				uint lastPass = Vk.Constants.SUBPASS_EXTERNAL;
				var lastStageMask = Vk.PipelineStageFlags.BottomOfPipe; // Less than ideal
				foreach (var use in realuses) {
					var stageMask = use.use switch { 
						Attachment.Use.Input => Vk.PipelineStageFlags.FragmentShader,
						Attachment.Use.Output => att.Format.IsDepthFormat() 
							? Vk.PipelineStageFlags.EarlyFragmentTests 
							: Vk.PipelineStageFlags.ColorAttachmentOutput,
						Attachment.Use.Resolve => Vk.PipelineStageFlags.ColorAttachmentOutput,
						_ => throw new Exception("Invalid use for subpass dependency")
					};
					sdeps.Add(new(
						srcSubpass: lastPass,
						dstSubpass: use.idx,
						srcStageMask: lastStageMask,
						dstStageMask: stageMask,
						srcAccessMask: Vk.AccessFlags.MemoryWrite,
						dstAccessMask: Vk.AccessFlags.MemoryRead,
						dependencyFlags: Vk.DependencyFlags.ByRegion
					));
					lastPass = use.idx;
					lastStageMask = use.use switch { 
						Attachment.Use.Input => Vk.PipelineStageFlags.FragmentShader,
						Attachment.Use.Output => att.Format.IsDepthFormat()
							? Vk.PipelineStageFlags.LateFragmentTests
							: Vk.PipelineStageFlags.ColorAttachmentOutput,
						Attachment.Use.Resolve => Vk.PipelineStageFlags.ColorAttachmentOutput,
						_ => throw new Exception("Invalid use for subpass dependency")
					};
				}
				if (att.Preserve) {
					sdeps.Add(new(
						srcSubpass: lastPass,
						dstSubpass: Vk.Constants.SUBPASS_EXTERNAL,
						srcStageMask: lastStageMask,
						dstStageMask: Vk.PipelineStageFlags.FragmentShader,
						srcAccessMask: Vk.AccessFlags.MemoryWrite,
						dstAccessMask: Vk.AccessFlags.MemoryRead,
						dependencyFlags: Vk.DependencyFlags.ByRegion
					));
				}
			}
			var depptr = stackalloc Vk.SubpassDependency[sdeps.Count];
			int depidx = 0;
			foreach (var dep in sdeps) {
				depptr[depidx++] = dep;
			}

			// Create the renderpass
			Vk.RenderPassCreateInfo rpci = new(
				flags: Vk.RenderPassCreateFlags.NoFlags,
				attachmentCount: (uint)acount,
				attachments: adesc,
				subpassCount: (uint)scount,
				subpasses: sdesc,
				dependencyCount: (uint)sdeps.Count,
				dependencies: depptr
			);
			Core.Instance!.Graphics.Device.CreateRenderPass(&rpci, null, out renderPass)
				.Throw("Failed to create renderpass");
		}

		// Contains information and objects for a framebuffer attachment
		public class Attachment
		{
			public readonly uint Index;
			public readonly TexelFormat Format;
			public readonly bool MSAA;
			public readonly bool Preserve;
			public readonly bool Input;
			public uint? MSAAAttachment;
			public uint? ResolveAttachment;
			public Vk.Image Image;
			public Vk.ImageView View;
			public MemoryAllocation? Memory;
			public readonly List<Use> Uses;
			public readonly List<Use> MSAAUses; // Only used in MSAA != 1

			public Attachment(uint index, TexelFormat format, bool msaa, bool preserve, bool input)
			{
				Index = index;
				Format = format;
				MSAA = msaa;
				Preserve = preserve;
				Input = input;
				MSAAAttachment = null;
				ResolveAttachment = null;
				Image = Vk.Image.Null;
				View = Vk.ImageView.Null;
				Memory = null;
				Uses = new();
				MSAAUses = new();
			}

			public enum Use : byte { None, Output, Input, Resolve }
		}

		public class SubpassDependencyComparer : IEqualityComparer<Vk.SubpassDependency>
		{
			public bool Equals(Vk.SubpassDependency x, Vk.SubpassDependency y) => x == y;
			public int GetHashCode(Vk.SubpassDependency obj) => obj.GetHashCode();
		}
	}
}
