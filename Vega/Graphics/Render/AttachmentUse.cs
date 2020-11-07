/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// Describes the ways in which a render attachment may be used within a <see cref="Renderer"/> subpass.
	/// </summary>
	public enum AttachmentUse
	{
		/// <summary>
		/// The attachment is unused within the subpass.
		/// </summary>
		Unused,
		/// <summary>
		/// The attachment is written to in the subpass.
		/// </summary>
		Output,
		/// <summary>
		/// The attachment is read from as an input attachment in the subpass.
		/// </summary>
		Input,
		/// <summary>
		/// The attachment is used as a resolve target for another output target.
		/// </summary>
		Resolve
	}
}
