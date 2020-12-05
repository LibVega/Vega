/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	// A region in 3D texture space
	internal struct TextureRegion
	{
		#region Fields
		public uint X;
		public uint Y;
		public uint Z;
		public uint Width;
		public uint Height;
		public uint Depth;
		public uint LayerStart;
		public uint LayerCount;

		public VkOffset3D Offset => new((int)X, (int)Y, (int)Z);
		public VkExtent3D Extent => new(Width, Height, Depth);
		#endregion // Fields

		public TextureRegion(uint x, uint y, uint z, uint w, uint h, uint d)
		{
			X = x;
			Y = y;
			Z = z;
			Width = w;
			Height = h;
			Depth = d;
			LayerStart = 0;
			LayerCount = 1;
		}

		public TextureRegion(uint x, uint y, uint z, uint w, uint h, uint d, uint ls, uint lc)
		{
			X = x;
			Y = y;
			Z = z;
			Width = w;
			Height = h;
			Depth = d;
			LayerStart = ls;
			LayerCount = lc;
		}

		public ulong GetDataSize(TexelFormat fmt) => Width * Height * Depth * LayerCount * fmt.GetSize();
	}
}
