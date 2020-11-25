/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Vega.Graphics
{
	/// <summary>
	/// Contains the information required to describe the attachments and subpasses for a <see cref="Renderer"/>.
	/// <para>
	/// MSAA support is enabled by setting <see cref="ResolveSubpass"/> to a non-null value. Resolve attachments
	/// are handled automatically, and do not need to be explitly specified.
	/// </para>
	/// </summary>
	public sealed class RendererDescription
	{
		#region Fields
		/// <summary>
		/// The set of attachments to be used by the renderer.
		/// </summary>
		public IReadOnlyList<AttachmentDescription> Attachments => _attachments;
		private readonly List<AttachmentDescription> _attachments;

		/// <summary>
		/// Gets the number of subpasses for the renderer, dictated by the subpass count of the first attachment.
		/// </summary>
		public uint SubpassCount => (Attachments.Count > 0) ? Attachments[0].SubpassCount : 0;

		/// <summary>
		/// Gets if the renderer has one or more depth/stencil attachments.
		/// </summary>
		public bool HasDepthAttachment => _attachments.Any(att => att.IsDepth);

		/// <summary>
		/// The subpass index, if any, where the MSAA resolve operation occurs. This enables (but does not require) 
		/// MSAA capabilities for the renderer. Set with <see cref="SetResolveSubpass(uint?)"/>.
		/// </summary>
		public uint? ResolveSubpass { get; private set; } = null;
		/// <summary>
		/// Gets if the description supports MSAA attachments (has a resolve subpass).
		/// </summary>
		public bool SupportsMSAA => ResolveSubpass.HasValue;
		#endregion // Fields

		/// <summary>
		/// Starts a new renderer description with the given attachments.
		/// </summary>
		/// <param name="attachment">The first attachment of the description, which sets the subpass count.</param>
		/// <param name="otherAttachments">The additional attachment descriptions.</param>
		public RendererDescription(AttachmentDescription attachment, params AttachmentDescription[] otherAttachments)
		{
			_attachments = new(otherAttachments.Length + 1);
			AddAttachment(attachment);
			foreach (var att in otherAttachments) {
				AddAttachment(att);
			}
		}

		/// <summary>
		/// Adds an attachment to the renderer description.
		/// </summary>
		/// <param name="attachment">The attachment description to add.</param>
		/// <returns>The same object, to faciliate chaining.</returns>
		public RendererDescription AddAttachment(AttachmentDescription attachment)
		{
			// Self-validation
			if (!attachment.TryValidate(out var error)) {
				throw new ArgumentException($"Bad attachment description - {error}");
			}
			
			// Validate against other attachments
			if (_attachments.Count > 0) {
				if (attachment.SubpassCount != _attachments[^1].SubpassCount) {
					throw new ArgumentException("Bad attachment description - wrong subpass count");
				}
				int aidx = 0;
				foreach (var att in _attachments) {
					if (attachment.IsDepth && att.IsDepth) {
						var dupOut = att.Uses.Where((use, idx) => (use == AttachmentUse.Output) && (use == attachment.Uses[idx]));
						if (dupOut.Any()) {
							throw new ArgumentException("Bad attachment description - duplicate depth/stencil output");
						}
					}
					++aidx;
				}
			}

			// Add description and return
			_attachments.Add(attachment);
			return this;
		}

		/// <summary>
		/// Sets the subpass index in which MSAA resolves occur. All color attachments used as outputs in this pass
		/// will be resolved, if they are preserved or otherwise used later in the renderer.
		/// <para>
		/// Renderers without MSAA attachments will ignore resolve operations.
		/// </para>
		/// </summary>
		/// <param name="passes">The subpass index(es), if any, where resolve operations happen.</param>
		/// <returns>The same object, to facilitate chaining.</returns>
		public RendererDescription SetResolveSubpass(uint? index)
		{
			// Set new indices
			ResolveSubpass = index;
			if (!index.HasValue) {
				return this;
			}

			// Validate against existing attachments
			if (index.Value >= SubpassCount) {
				throw new ArgumentException("Bad resolve subpass - index is out of bounds");
			}
			if (_attachments.Where(att => att.Uses[(int)index.Value] == AttachmentUse.Output).Count() == 0) {
				throw new ArgumentException("Bad resolve subpass - no output attachments for subpass");
			}
			int aidx = 0;
			foreach (var att in _attachments) {
				++aidx;
			}

			// Return
			return this;
		}
	}
}
