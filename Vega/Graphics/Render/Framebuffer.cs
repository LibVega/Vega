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
	internal unsafe sealed class Framebuffer : IDisposable
	{
		#region Fields
		// The renderer using this framebuffer
		public readonly Renderer Renderer;
		// The window that this framebuffer is associated with
		public readonly Window? Window;

		// Info
		// If at least one attachment supports msaa
		public readonly bool HasMSAA;
		// The number of non-resolve attachments
		public readonly int NonResolveCount;

		// The attachment objects
		public readonly List<Attachment> Attachments;
		// The framebuffer object(s) - multiple for windows
		public readonly List<Vk.Framebuffer> Handles;
		// The current framebuffer object (per-frame)
		public Vk.Framebuffer CurrentHandle => Handles[(int)Core.Instance!.Graphics.FrameIndex];

		// If the framebuffer is disposed
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		// The attachments need to be validated first
		public Framebuffer(Renderer renderer, RendererDescription desc, Window? window)
		{
			Renderer = renderer;
			Window = window;

			// Generate the info
			HasMSAA = desc.Attachments.Any(att => att.MSAA);
			NonResolveCount = desc.AttachmentCount;

			// Generate the attachment infos
			// The non-msaa and non-resolve attachments first, then the resolve attachments as needed
			Attachments = new();
			foreach (var att in desc.Attachments) {
				Attachments.Add(new(att.Format, att.MSAA, att.Preserve, att.Timeline.Any(use => use == AttachmentUse.Input)));
			}
			foreach (var att in desc.Attachments.Select((att, i) => (att, i))
												.Where(pair => pair.att.MSAA && pair.att.ResolveSubpass.HasValue)) {
				Attachments.Add(new(att.att.Format, false, false, att.att.Timeline.Any(use => use == AttachmentUse.Input)));
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

			// No handles yet
			Handles = new();
		}
		~Framebuffer()
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
			if (Window is null) { // Offscreen buffer
				var att = Attachments[0];
				MakeImage(att.Format, size, att.MSAA ? msaa : MSAA.X1, att.Preserve, att.Input, 
					out att.Image, out att.View, out att.Memory);
			}
			var buildCount = (msaa != MSAA.X1) ? Attachments.Count : NonResolveCount;
			for (int i = 1; i < buildCount; ++i) {
				var att = Attachments[i];
				MakeImage(att.Format, size, att.MSAA ? msaa : MSAA.X1, att.Preserve, att.Input,
					out att.Image, out att.View, out att.Memory);
			}
		}

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

		// Destroys and frees the resources for the framebuffer
		private void freeResources()
		{
			foreach (var fb in Handles) {
				if (fb) {
					fb.DestroyFramebuffer(null);
				}
			}
			Handles.Clear();
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
				freeResources();
			}
			IsDisposed = true;
		}
		#endregion // IDisposable

		// Contains information and objects for a framebuffer attachment
		public class Attachment
		{
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

			public Attachment(TexelFormat format, bool msaa, bool preserve, bool input)
			{
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
	}
}
