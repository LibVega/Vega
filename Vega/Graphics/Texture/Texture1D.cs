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

		// The binding type
		internal override BindingType BindingType => BindingType.Sampler1D;
		#endregion // Fields

		/// <summary>
		/// Create a new blank texture with the given dimensions and format.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="format">The texel format.</param>
		/// <param name="usage">The texture usage policy.</param>
		public Texture1D(uint width, TexelFormat format, TextureUsage usage = TextureUsage.Static)
			: base(width, 1, 1, 1, 1, format, usage, ResourceType.Texture1D)
		{

		}

		/// <summary>
		/// Create a new filled texture with the given dimensions and format.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="format">The texel format.</param>
		/// <param name="data">The initial texture data. Must be large enough to fill entire texture.</param>
		/// <param name="usage">The texture usage policy.</param>
		public Texture1D(uint width, TexelFormat format, void* data, TextureUsage usage = TextureUsage.Static)
			: base(width, 1, 1, 1, 1, format, usage, ResourceType.Texture1D)
		{
			if (data == null) {
				throw new ArgumentException("Initial texture data pointer cannot be null", nameof(data));
			}
			SetDataImpl(data, new(0, 0, 0, width, 1, 1, 0, 1));
		}

		/// <summary>
		/// Create a new filled texture with the given dimensions and format.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="format">The texel format.</param>
		/// <param name="data">The initial texture data. Must be large enough to fill entire texture.</param>
		/// <param name="usage">The texture usage policy.</param>
		public Texture1D(uint width, TexelFormat format, ReadOnlySpan<byte> data, 
				TextureUsage usage = TextureUsage.Static)
			: base(width, 1, 1, 1, 1, format, usage, ResourceType.Texture1D)
		{
			SetDataImpl(data, new(0, 0, 0, width, 1, 1, 0, 1));
		}

		/// <summary>
		/// Create a new filled texture with the given dimensions and format.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="format">The texel format.</param>
		/// <param name="data">The initial texture data. Must be large enough to fill entire texture.</param>
		/// <param name="dataOff">The offset into the buffered data to upload.</param>
		/// <param name="usage">The texture usage policy.</param>
		public Texture1D(uint width, TexelFormat format, HostBuffer data, ulong dataOff = 0, 
				TextureUsage usage = TextureUsage.Static)
			: base(width, 1, 1, 1, 1, format, usage, ResourceType.Texture1D)
		{
			SetDataImpl(data, dataOff, new(0, 0, 0, width, 1, 1, 0, 1));
		}

		#region Data
		/// <summary>
		/// Updates the image data with the passed data. Only works for non-Static or uninialized textures.
		/// </summary>
		/// <param name="data">The data to update the texture with.</param>
		/// <param name="x">The texel coordinate to begin updating at.</param>
		/// <param name="width">The number of texels to update.</param>
		public void SetData(void* data, uint x, uint width) =>
			SetDataImpl(data, new(x, 0, 0, width, 1, 1));

		/// <summary>
		/// Updates the image data with the passed data. Only works for non-Static or uninialized textures.
		/// </summary>
		/// <param name="data">The data to update the texture with.</param>
		/// <param name="x">The texel coordinate to begin updating at.</param>
		/// <param name="width">The number of texels to update.</param>
		public void SetData(ReadOnlySpan<byte> data, uint x, uint width) =>
			SetDataImpl(data, new(x, 0, 0, width, 1, 1));

		/// <summary>
		/// Updates the image data with the passed data. Only works for non-Static or uninialized textures.
		/// </summary>
		/// <param name="data">The host buffer to update the texture with.</param>
		/// <param name="x">The texel coordinate to begin updating at.</param>
		/// <param name="width">The number of texels to update.</param>
		/// <param name="dataOffset">The offset into <paramref name="data"/> to copy data from.</param>
		public void SetData(HostBuffer data, uint x, uint width, uint dataOffset = 0) =>
			SetDataImpl(data, dataOffset, new(x, 0, 0, width, 1, 1));
		#endregion // Data
	}
}
