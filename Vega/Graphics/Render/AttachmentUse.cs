/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// Describes how an attachment is used within a <see cref="Renderer"/> subpass.
	/// </summary>
	public enum AttachmentUse
	{
		/// <summary>
		/// The attachment is not used by the subpass.
		/// </summary>
		Unused,
		/// <summary>
		/// The attachment is write-only in the subpass, either as color or depth/stecil based on format.
		/// </summary>
		Output,
		/// <summary>
		/// The attachment is read-only in the subpass, as an input attachment.
		/// </summary>
		Input
	}
}
