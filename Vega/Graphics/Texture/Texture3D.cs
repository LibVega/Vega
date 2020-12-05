/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// Represents a sampled texture with 3 dimensions.
	/// </summary>
	public unsafe sealed class Texture3D : TextureBase
	{
		#region Fields
		/// <summary>
		/// The width of the texture (x-axis).
		/// </summary>
		public uint Width => Dimensions.Width;
		/// <summary>
		/// The height of the texture (y-axis).
		/// </summary>
		public uint Height => Dimensions.Height;
		/// <summary>
		/// The depth of the texture (z-axis).
		/// </summary>
		public uint Depth => Dimensions.Depth;
		#endregion // Fields

		/// <summary>
		/// Create a new blank texture with the given dimensions and format.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="height">The height of the texture.</param>
		/// <param name="depth">The depth of the texture.</param>
		/// <param name="format">The texel format.</param>
		public Texture3D(uint width, uint height, uint depth, TexelFormat format)
			: base(width, height, depth, 1, 1, format, TextureUsage.Static, ResourceType.Texture3D)
		{

		}
	}
}
