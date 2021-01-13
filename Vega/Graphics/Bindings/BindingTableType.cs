/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	// The the binding types for shader resource tables
	internal enum BindingTableType : byte
	{
		// Combined image/sampler
		Sampler = 0,
		// Storage image
		Image = 1,
		// Storage buffer
		Buffer = 2,
		// readonly texel buffer
		ROTexels = 3,
		// readwrite texel buffer
		RWTexels = 4
	}
}
