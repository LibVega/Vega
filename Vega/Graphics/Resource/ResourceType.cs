/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	// The different graphics resource types.
	internal enum ResourceType : byte
	{
		Invalid = 0,
		HostBuffer,
		IndexBuffer,
		VertexBuffer,
		Texture1D,
		Texture2D,
		Texture3D,
		Texture1DArray,
		Texture2DArray,
		Shader
	}
}
