/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
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

		// The binding type
		internal override BindingType BindingType => BindingType.Sampler3D;
		#endregion // Fields

		/// <summary>
		/// Create a new blank texture with the given dimensions and format.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="height">The height of the texture.</param>
		/// <param name="depth">The depth of the texture.</param>
		/// <param name="format">The texel format.</param>
		/// <param name="usage">The texture usage policy.</param>
		public Texture3D(uint width, uint height, uint depth, TexelFormat format, 
				TextureUsage usage = TextureUsage.Static)
			: base(width, height, depth, 1, 1, format, usage, ResourceType.Texture3D)
		{

		}

		/// <summary>
		/// Create a filled texture with the given dimensions and format.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="height">The height of the texture.</param>
		/// <param name="depth">The depth of the texture.</param>
		/// <param name="format">The texel format.</param>
		/// <param name="data">The initial texture data. Must be large enough to fill entire texture.</param>
		/// <param name="usage">The texture usage policy.</param>
		public Texture3D(uint width, uint height, uint depth, TexelFormat format, void* data, 
				TextureUsage usage = TextureUsage.Static)
			: base(width, height, depth, 1, 1, format, usage, ResourceType.Texture3D)
		{
			if (data == null) {
				throw new ArgumentException("Initial texture data pointer cannot be null", nameof(data));
			}
			SetDataImpl(new(0, 0, 0, width, height, depth, 0, 1), data);
		}

		/// <summary>
		/// Create a filled texture with the given dimensions and format.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="height">The height of the texture.</param>
		/// <param name="depth">The depth of the texture.</param>
		/// <param name="format">The texel format.</param>
		/// <param name="data">The initial texture data. Must be large enough to fill entire texture.</param>
		/// <param name="usage">The texture usage policy.</param>
		public Texture3D(uint width, uint height, uint depth, TexelFormat format, ReadOnlySpan<byte> data, 
				TextureUsage usage = TextureUsage.Static)
			: base(width, height, depth, 1, 1, format, usage, ResourceType.Texture3D)
		{
			SetDataImpl(new(0, 0, 0, width, height, depth, 0, 1), data);
		}

		/// <summary>
		/// Create a filled texture with the given dimensions and format.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="height">The height of the texture.</param>
		/// <param name="depth">The depth of the texture.</param>
		/// <param name="format">The texel format.</param>
		/// <param name="data">The initial texture data. Must be large enough to fill entire texture.</param>
		/// <param name="dataOff">The offset into the buffered data to upload.</param>
		/// <param name="usage">The texture usage policy.</param>
		public Texture3D(uint width, uint height, uint depth, TexelFormat format, HostBuffer data, ulong dataOff = 0, 
				TextureUsage usage = TextureUsage.Static)
			: base(width, height, depth, 1, 1, format, usage, ResourceType.Texture3D)
		{
			SetDataImpl(new(0, 0, 0, width, height, depth, 0, 1), data, dataOff);
		}

		#region Data
		/// <summary>
		/// Updates the image data with the passed data. Only works for non-Static or uninialized textures.
		/// </summary>
		/// <param name="data">The data to update the texture with.</param>
		/// <param name="x">The texel x-coordinate to begin updating at.</param>
		/// <param name="y">The texel y-coordinate to begin updating at.</param>
		/// <param name="z">The texel z-coordinate to begin updating at.</param>
		/// <param name="width">The width of the texel region to update.</param>
		/// <param name="height">The height of the texel region to update.</param>
		/// <param name="depth">The depth of the texel region to update.</param>
		public void SetData(void* data, uint x, uint y, uint z, uint width, uint height, uint depth) =>
			SetDataImpl(new(x, y, z, width, height, depth), data);

		/// <summary>
		/// Updates the image data with the passed data. Only works for non-Static or uninialized textures.
		/// </summary>
		/// <param name="data">The data to update the texture with.</param>
		/// <param name="x">The texel x-coordinate to begin updating at.</param>
		/// <param name="y">The texel y-coordinate to begin updating at.</param>
		/// <param name="z">The texel z-coordinate to begin updating at.</param>
		/// <param name="width">The width of the texel region to update.</param>
		/// <param name="height">The height of the texel region to update.</param>
		/// <param name="depth">The depth of the texel region to update.</param>
		public void SetData(ReadOnlySpan<byte> data, uint x, uint y, uint z, uint width, uint height, uint depth) =>
			SetDataImpl(new(x, y, z, width, height, depth), data);

		/// <summary>
		/// Updates the image data with the passed data. Only works for non-Static or uninialized textures.
		/// </summary>
		/// <param name="data">The host buffer to update the texture with.</param>
		/// <param name="x">The texel x-coordinate to begin updating at.</param>
		/// <param name="y">The texel y-coordinate to begin updating at.</param>
		/// <param name="z">The texel z-coordinate to begin updating at.</param>
		/// <param name="width">The width of the texel region to update.</param>
		/// <param name="height">The height of the texel region to update.</param>
		/// <param name="depth">The depth of the texel region to update.</param>
		/// <param name="dataOffset">The offset into <paramref name="data"/> to copy data from.</param>
		public void SetData(HostBuffer data, uint x, uint y, uint z, uint width, uint height, uint depth,
				uint dataOffset = 0) =>
			SetDataImpl(new(x, y, z, width, height, depth), data, dataOffset);
		#endregion // Data
	}
}
