/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics.Reflection
{
	/// <summary>
	/// The different dimensionality types that textures, storage images, and samplers can have.
	/// </summary>
	public enum TextureDims
	{
		/// <summary>
		/// One-dimensional texel data (matches <see cref="Texture1D"/>).
		/// </summary>
		E1D,
		/// <summary>
		/// Two-dimensional texel data (matches <see cref="Texture2D"/>).
		/// </summary>
		E2D,
		/// <summary>
		/// Three-dimensional texel data (matches <see cref="Texture3D"/>).
		/// </summary>
		E3D
	}
}
