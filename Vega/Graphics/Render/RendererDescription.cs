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
	/// Describes the render targets and subpass layout required to create a <see cref="Renderer"/> instance.
	/// </summary>
	public sealed class RendererDescription
	{
		#region Fields
		/// <summary>
		/// The render target attachments that will be used in the renderer.
		/// </summary>
		public IReadOnlyList<AttachmentDescription> Attachments => _attachments;
		private readonly List<AttachmentDescription> _attachments;

		/// <summary>
		/// The number of attachments in the description.
		/// </summary>
		public int AttachmentCount => _attachments.Count;
		/// <summary>
		/// The number of subpasses in the renderer (taken from attachment 0).
		/// </summary>
		public int SubpassCount => (_attachments.Count > 0) ? _attachments[0].TimelineSize : 0;
		#endregion // Fields

		/// <summary>
		/// Start a new renderer description with the given attachments.
		/// </summary>
		/// <param name="attachments">The renderer attachments.</param>
		public RendererDescription(params AttachmentDescription[] attachments)
		{
			_attachments = new(attachments);
		}

		/// <summary>
		/// Add an additional attachment to the renderer description.
		/// </summary>
		/// <param name="attachment">The description of the new attachment.</param>
		/// <returns>The same description to facilitate chaning.</returns>
		public RendererDescription AddAttachment(AttachmentDescription attachment)
		{
			_attachments.Add(attachment);
			return this;
		}

		/// <summary>
		/// Checks if the renderer description is valid and consistent.
		/// </summary>
		/// <param name="error">A human-readable message of the build error, or <c>null</c> on success.</param>
		/// <param name="window">The optional window against which validity checks are performed.</param>
		/// <returns>If the description was built successfully.</returns>
		public bool TryValidate(out string? error, Window? window)
		{
			// Check counts
			if (_attachments.Count == 0) {
				error = "no attachments";
				return false;
			}
			if (_attachments.Skip(1).Any(a => a.TimelineSize != _attachments[0].TimelineSize)) {
				error = $"subpass count mismatch (counts=[{String.Join(", ", _attachments.Select(a => a.TimelineSize))}])";
				return false;
			}

			// Check individual attachments
			for (int i = 0; i < _attachments.Count; ++i) {
				if (!_attachments[i].TryValidate(out var aerr)) {
					error = $"invalid attachment {i} - {aerr}";
					return false;
				}
			}

			// Subpass checks
			for (int si = 0; si < SubpassCount; ++si) {
				// Check MSAA outputs
				var msaaOut = _attachments
					.Where(att => att.Timeline[si] == AttachmentUse.Output)
					.Select(att => att.MSAA && (si <= att.ResolveSubpass.GetValueOrDefault(UInt32.MaxValue)))
					.Distinct();
				if (msaaOut.Count() != 1) {
					error = $"cannot mix MSAA and non-MSAA output attachments in subpass {si}";
					return false;
				}

				// Check depth attachments
				var depthOut = _attachments
					.Where(att => att.Timeline[si] == AttachmentUse.Output)
					.Where(att => att.Format.IsDepthFormat());
				if (depthOut.Count() > 1) {
					error = $"cannot have multiple depth/stencil attachments in subpass {si}";
					return false;
				}
			}

			// Window validation
			if (window is not null) {
				if (_attachments[0].Format != window.SurfaceFormat) {
					error = "attachment 0 and window surface format mismatch";
					return false;
				}
				if (!_attachments[0].Preserve) {
					error = "attachment 0 must be preserved if used as the window surface";
					return false;
				}
				if (_attachments[0].MSAA && !_attachments[0].ResolveSubpass.HasValue) {
					error = "attachment 0 must be resolved if used as the window surface";
					return false;
				}
			}

			// Update and Return
			error = null;
			return true;
		}
	}
}
