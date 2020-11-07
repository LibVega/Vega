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
		/// If the target should be cleared at the start of the render process.
		/// </summary>
		public readonly bool Clear;
		/// <summary>
		/// If the target should be preserved past the end of the render process.
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
		public uint TimelineLength => (uint)_timeline.Count;
		#endregion // Fields

		/// <summary>
		/// Create a new attachment description.
		/// </summary>
		/// <param name="format">The format of the attachment.</param>
		/// <param name="clear">If the attachment is cleared at the start of the render process.</param>
		/// <param name="preserve">If the attachment is preserved at the end of the render process.</param>
		/// <param name="uses">Optional description of the usage timeline.</param>
		public AttachmentDescription(TexelFormat format, bool clear, bool preserve, params AttachmentUse[] uses)
		{
			Format = format;
			Clear = clear;
			Preserve = preserve;
			_timeline = new(uses);
		}
	}
}
