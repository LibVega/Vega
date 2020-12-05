/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// The different usage policies for texture resources. These dictate how and when texture data can be updated.
	/// </summary>
	public enum TextureUsage : byte
	{
		/// <summary>
		/// The texture data is set before first use and cannot be updated at any point.
		/// </summary>
		Static
	}
}
