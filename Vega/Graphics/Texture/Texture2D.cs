/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.InteropServices;
using Vega.Content;

namespace Vega.Graphics
{
	/// <summary>
	/// Represents a sampled texture with 2 dimensions.
	/// </summary>
	public unsafe sealed class Texture2D : TextureBase
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

		// The binding type
		internal override BindingType BindingType => BindingType.Sampler2D;
		#endregion // Fields

		/// <summary>
		/// Create a new blank texture with the given dimensions and format.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="height">The height of the texture.</param>
		/// <param name="format">The texel format.</param>
		/// <param name="usage">The texture usage policy.</param>
		public Texture2D(uint width, uint height, TexelFormat format, TextureUsage usage = TextureUsage.Static)
			: base(width, height, 1, 1, 1, format, usage, ResourceType.Texture2D)
		{

		}

		/// <summary>
		/// Create a filled texture with the given dimensions and format.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="height">The height of the texture.</param>
		/// <param name="format">The texel format.</param>
		/// <param name="data">The initial texture data. Must be large enough to fill entire texture.</param>
		/// <param name="usage">The texture usage policy.</param>
		public Texture2D(uint width, uint height, TexelFormat format, void* data, 
				TextureUsage usage = TextureUsage.Static)
			: base(width, height, 1, 1, 1, format, usage, ResourceType.Texture2D)
		{
			if (data == null) {
				throw new ArgumentException("Initial texture data pointer cannot be null", nameof(data));
			}
			SetDataImpl(new(0, 0, 0, width, height, 1, 0, 1), data);
		}

		/// <summary>
		/// Create a filled texture with the given dimensions and format.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="height">The height of the texture.</param>
		/// <param name="format">The texel format.</param>
		/// <param name="data">The initial texture data. Must be large enough to fill entire texture.</param>
		/// <param name="usage">The texture usage policy.</param>
		public Texture2D(uint width, uint height, TexelFormat format, ReadOnlySpan<byte> data, 
				TextureUsage usage = TextureUsage.Static)
			: base(width, height, 1, 1, 1, format, usage, ResourceType.Texture2D)
		{
			SetDataImpl(new(0, 0, 0, width, height, 1, 0, 1), data);
		}

		/// <summary>
		/// Create a filled texture with the given dimensions and format.
		/// </summary>
		/// <param name="width">The width of the texture.</param>
		/// <param name="height">The height of the texture.</param>
		/// <param name="format">The texel format.</param>
		/// <param name="data">The initial texture data. Must be large enough to fill entire texture.</param>
		/// <param name="dataOff">The offset into the buffered data to upload.</param>
		/// <param name="usage">The texture usage policy.</param>
		public Texture2D(uint width, uint height, TexelFormat format, HostBuffer data, ulong dataOff = 0, 
				TextureUsage usage = TextureUsage.Static)
			: base(width, height, 1, 1, 1, format, usage, ResourceType.Texture2D)
		{
			SetDataImpl(new(0, 0, 0, width, height, 1, 0, 1), data, dataOff);
		}

		#region Data
		/// <summary>
		/// Updates the image data with the passed data. Only works for non-Static or uninialized textures.
		/// </summary>
		/// <param name="data">The data to update the texture with.</param>
		/// <param name="x">The texel x-coordinate to begin updating at.</param>
		/// <param name="y">The texel y-coordinate to begin updating at.</param>
		/// <param name="width">The width of the texel region to update.</param>
		/// <param name="height">The height of the texel region to update.</param>
		public void SetData(void* data, uint x, uint y, uint width, uint height) =>
			SetDataImpl(new(x, y, 0, width, height, 1), data);

		/// <summary>
		/// Updates the image data with the passed data. Only works for non-Static or uninialized textures.
		/// </summary>
		/// <param name="data">The data to update the texture with.</param>
		/// <param name="x">The texel x-coordinate to begin updating at.</param>
		/// <param name="y">The texel y-coordinate to begin updating at.</param>
		/// <param name="width">The width of the texel region to update.</param>
		/// <param name="height">The height of the texel region to update.</param>
		public void SetData(ReadOnlySpan<byte> data, uint x, uint y, uint width, uint height) =>
			SetDataImpl(new(x, y, 0, width, height, 1), data);

		/// <summary>
		/// Updates the image data with the passed data. Only works for non-Static or uninialized textures.
		/// </summary>
		/// <param name="data">The host buffer to update the texture with.</param>
		/// <param name="x">The texel x-coordinate to begin updating at.</param>
		/// <param name="y">The texel y-coordinate to begin updating at.</param>
		/// <param name="width">The width of the texel region to update.</param>
		/// <param name="height">The height of the texel region to update.</param>
		/// <param name="dataOffset">The offset into <paramref name="data"/> to copy data from.</param>
		public void SetData(HostBuffer data, uint x, uint y, uint width, uint height, uint dataOffset = 0) =>
			SetDataImpl(new(x, y, 0, width, height, 1), data, dataOffset);

		/// <summary>
		/// Updates the image data with the passed data. Only works for non-Static or uninialized textures.
		/// </summary>
		/// <param name="data">The data to update the texture with.</param>
		/// <param name="region">The texture region to update.</param>
		public void SetData(void* data, in Rect region) =>
			SetDataImpl(new((uint)region.X, (uint)region.Y, 0, region.Width, region.Height, 1), data);

		/// <summary>
		/// Updates the image data with the passed data. Only works for non-Static or uninialized textures.
		/// </summary>
		/// <param name="data">The data to update the texture with.</param>
		/// <param name="region">The texture region to update.</param>
		public void SetData(ReadOnlySpan<byte> data, in Rect region) =>
			SetDataImpl(new((uint)region.X, (uint)region.Y, 0, region.Width, region.Height, 1), data);

		/// <summary>
		/// Updates the image data with the passed data. Only works for non-Static or uninialized textures.
		/// </summary>
		/// <param name="data">The host buffer to update the texture with.</param>
		/// <param name="region">The texture region to update.</param>
		/// <param name="dataOffset">The offset into <paramref name="data"/> to copy data from.</param>
		public void SetData(HostBuffer data, in Rect region, uint dataOffset = 0) =>
			SetDataImpl(new((uint)region.X, (uint)region.Y, 0, region.Width, region.Height, 1), data, dataOffset);
		#endregion // Data

		#region Content Load
		/// <summary>
		/// Attempts to open and load the image file at the given path. Supported image types are JPEG (.jpg/.jpeg),
		/// PNG (.png), TGA (.tga), and BMP (.bmp).
		/// </summary>
		/// <param name="path">The path to the image file to load.</param>
		/// <param name="usage">The usage policy for the resulting loaded texture.</param>
		/// <returns>The loaded image data as a Texture2D.</returns>
		public static Texture2D LoadFile(string path, TextureUsage usage = TextureUsage.Static)
		{
			using (var file = new ImageFile(path)) {
				// Load data
				var data = file.LoadDataRGBA();

				// Create image
				return new Texture2D(file.Width, file.Height, TexelFormat.Color, MemoryMarshal.AsBytes(data), usage);
			}
		}
		#endregion // Content Load
	}
}
