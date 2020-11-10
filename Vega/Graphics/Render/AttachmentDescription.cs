/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;

namespace Vega.Graphics
{
	/// <summary>
	/// Describes a render target attachment used within a <see cref="Renderer"/>.
	/// </summary>
	public sealed class AttachmentDescription
	{
		#region Fields
		/// <summary>
		/// The format of the attachment.
		/// </summary>
		public readonly TexelFormat Format;
		/// <summary>
		/// If the attachment supports MSAA operations.
		/// </summary>
		public readonly bool MSAA;
		/// <summary>
		/// The subpass, if any, where the attachment is resolved to a single-sample target when using MSAA.
		/// </summary>
		public readonly uint? ResolveSubpass;
		/// <summary>
		/// If the target should be preserved past the end of the render process for use in another rendering process.
		/// </summary>
		public readonly bool Preserve;
		/// <summary>
		/// The usage timeline for the attachment.
		/// </summary>
		public IReadOnlyList<AttachmentUse> Timeline => _timeline;
		private readonly List<AttachmentUse> _timeline;

		/// <summary>
		/// The length of the usage timeline (the number of subpasses in the <see cref="Renderer"/>).
		/// </summary>
		public int TimelineSize => _timeline.Count;
		#endregion // Fields

		/// <summary>
		/// Create a new non-MSAA attachment description.
		/// </summary>
		/// <param name="format">The format of the attachment.</param>
		/// <param name="preserve">If the attachment is preserved at the end of the render process.</param>
		/// <param name="use">The attachment use for the first or singular subpass.</param>
		/// <param name="uses">Optional additional uses of the attachment in multi-pass renderers.</param>
		public AttachmentDescription(TexelFormat format, bool preserve, AttachmentUse use, params AttachmentUse[] uses)
		{
			Format = format;
			MSAA = false;
			ResolveSubpass = null;
			Preserve = preserve;
			_timeline = new(uses.Length + 1);
			_timeline.Add(use);
			_timeline.AddRange(uses);
		}

		/// <summary>
		/// Create a new MSAA-enabled attachment description.
		/// </summary>
		/// <param name="format">The format of the attachment.</param>
		/// <param name="resolveSubpass">
		/// The subpass index (if any) in which to resolve the attachment when using MSAA. Ignored in renderers not
		/// using MSAA.
		/// </param>
		/// <param name="preserve">If the attachment is preserved at the end of the render process.</param>
		/// <param name="use">The attachment use for the first or singular subpass.</param>
		/// <param name="uses">Optional additional uses of the attachment in multi-pass renderers.</param>
		public AttachmentDescription(TexelFormat format, uint? resolveSubpass, bool preserve, AttachmentUse use, 
			params AttachmentUse[] uses)
		{
			Format = format;
			MSAA = true;
			ResolveSubpass = resolveSubpass;
			Preserve = preserve;
			_timeline = new(uses.Length + 1);
			_timeline.Add(use);
			_timeline.AddRange(uses);
		}

		/// <summary>
		/// Checks if the attachment description is valid and consistent.
		/// </summary>
		/// <param name="error">
		/// A human-readable message describing the invalid or inconsistent part of the description, or <c>null</c>
		/// if there is no error.
		/// </param>
		/// <returns>If the description is valid, <c>false</c> implies the error message is populated.</returns>
		public bool TryValidate(out string? error)
		{
			// Check resolve
			if (ResolveSubpass.HasValue) {
				if (ResolveSubpass.Value >= _timeline.Count) {
					error = $"resolve subpass index is too large for timeline ({ResolveSubpass.Value} >= {_timeline.Count})";
					return false;
				}
				if (_timeline[(int)ResolveSubpass.Value] != AttachmentUse.Output) {
					error = $"resolve subpass must use AttachmentUse.Output";
					return false;
				}
			}
			else if (MSAA && Preserve) {
				error = "attachment is preserved, but is never resolved";
				return false;
			}

			// Loop over uses with lookback
			bool written = false, read = false;
			for (int i = 0; i < _timeline.Count; ++i) {
				switch (_timeline[i]) {
					case AttachmentUse.Unused: {
						// N/A
					} break;
					case AttachmentUse.Input: {
						if (!written) {
							error = "attachment cannot be used as input before an output subpass";
							return false;
						}
						read = true;
					} break;
					case AttachmentUse.Output: {
						written = true;
					} break;
				}
			}

			// Check other use
			if (!written && !read) {
				error = "attachment is unused for all subpasses";
				return false;
			}

			// Good
			error = null;
			return true;
		}
	}
}
