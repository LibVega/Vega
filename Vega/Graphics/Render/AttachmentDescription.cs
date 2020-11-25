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
	/// Describes an image that is used as the target of render commands in <see cref="Renderer"/> instances.
	/// </summary>
	public sealed class AttachmentDescription
	{
		#region Fields
		/// <summary>
		/// The format of the attachment.
		/// </summary>
		public readonly TexelFormat Format;
		/// <summary>
		/// If the target should be preserved past the end of the render process for use in another rendering process.
		/// </summary>
		public readonly bool Preserve;
		/// <summary>
		/// The usage timeline for the attachment.
		/// </summary>
		public IReadOnlyList<AttachmentUse> Uses => _uses;
		private readonly List<AttachmentUse> _uses;

		/// <summary>
		/// Gets if the attachment is a color attachment.
		/// </summary>
		public bool IsColor => Format.IsColorFormat();
		/// <summary>
		/// Gets if the attachment is a depth attachment.
		/// </summary>
		public bool IsDepth => Format.IsDepthFormat();

		/// <summary>
		/// The number of subpasses expected by the attachment.
		/// </summary>
		public uint SubpassCount => (uint)_uses.Count;
		/// <summary>
		/// Gets the use and subpass index of the first use that is not <see cref="AttachmentUse.Unused"/>. Throws an
		/// exception if the attachment is not valid.
		/// </summary>
		public (AttachmentUse Use, uint Index) FirstUse {
			get {
				var idx = _uses.FindIndex(use => use != AttachmentUse.Unused);
				return (idx >= 0) ? (_uses[idx], (uint)idx) : 
					throw new InvalidOperationException("Attachment is never used");
			}
		}
		/// <summary>
		/// Gets the use and subpass index of the last use that is not <see cref="AttachmentUse.Unused"/>. Throws an
		/// exception if the attachment is not valid.
		/// </summary>
		public (AttachmentUse Use, uint Index) LastUse {
			get {
				var idx = _uses.FindLastIndex(use => use != AttachmentUse.Unused);
				return (idx >= 0) ? (_uses[idx], (uint)idx) :
					throw new InvalidOperationException("Attachment is never used");
			}
		}
		#endregion // Fields

		/// <summary>
		/// Create a new attachment description.
		/// </summary>
		/// <param name="format">The format of the attachment.</param>
		/// <param name="preserve">If the attachment is preserved at the end of the render process.</param>
		/// <param name="use">The attachment use for the first or singular subpass.</param>
		/// <param name="uses">Optional additional uses of the attachment in multi-pass renderers.</param>
		public AttachmentDescription(TexelFormat format, bool preserve, AttachmentUse use, params AttachmentUse[] uses)
		{
			Format = format;
			Preserve = preserve;
			_uses = new(uses.Length + 1);
			_uses.Add(use);
			_uses.AddRange(uses);
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
			// Loop over uses with lookback
			bool written = false, read = false;
			for (int i = 0; i < _uses.Count; ++i) {
				switch (_uses[i]) {
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
