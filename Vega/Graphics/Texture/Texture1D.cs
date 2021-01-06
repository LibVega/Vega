/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// Represents a sampled texture with 1 dimension.
	/// </summary>
	public unsafe sealed class Texture1D : TextureBase
	{
		#region Fields
		/// <summary>
		/// The width of the texture (x-axis).
		/// </summary>
		public uint Width => Dimensions.Width;
		#endregion // Fields

		/// <summary>
		/// Create a new blank texture with the given dimensions and format.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="format">The texel format.</param>
		public Texture1D(uint width, TexelFormat format)
			: base(width, 1, 1, 1, 1, format, TextureUsage.Static, ResourceType.Texture1D)
		{

		}

		/// <summary>
		/// Create a new filled texture with the given dimensions and format.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="format">The texel format.</param>
		/// <param name="data">The initial texture data. Must be large enough to fill entire texture.</param>
		public Texture1D(uint width, TexelFormat format, void* data)
			: base(width, 1, 1, 1, 1, format, TextureUsage.Static, ResourceType.Texture1D)
		{
			if (data == null) {
				throw new ArgumentException("Initial texture data pointer cannot be null", nameof(data));
			}
			SetDataImpl(new(0, 0, 0, width, 1, 1, 0, 1), data);
		}

		/// <summary>
		/// Create a new filled texture with the given dimensions and format.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="format">The texel format.</param>
		/// <param name="data">The initial texture data. Must be large enough to fill entire texture.</param>
		public Texture1D(uint width, TexelFormat format, ReadOnlySpan<byte> data)
			: base(width, 1, 1, 1, 1, format, TextureUsage.Static, ResourceType.Texture1D)
		{
			SetDataImpl(new(0, 0, 0, width, 1, 1, 0, 1), data);
		}

		/// <summary>
		/// Create a new filled texture with the given dimensions and format.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="format">The texel format.</param>
		/// <param name="data">The initial texture data. Must be large enough to fill entire texture.</param>
		/// <param name="dataOff">The offset into the buffered data to upload.</param>
		public Texture1D(uint width, TexelFormat format, HostBuffer data, ulong dataOff = 0)
			: base(width, 1, 1, 1, 1, format, TextureUsage.Static, ResourceType.Texture1D)
		{
			SetDataImpl(new(0, 0, 0, width, 1, 1, 0, 1), data, dataOff);
		}
	}
}
