/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// Describes the set of available texture sampler types that can be bound as renderer resources. 
	/// <para>
	/// The names give the sample settings, in the order: min/mag filtering, mipmap filtering, border mode.
	/// </para>
	/// </summary>
	public enum Sampler
	{
		/// <summary>
		/// Min/mag = Nearest, Mip = Nearest, Border = Repeat
		/// </summary>
		NearestNearestRepeat       = 0,
		/// <summary>
		/// Min/mag = Nearest, Mip = Nearest, Border = Edge Clamp
		/// </summary>
		NearestNearestEdge         = 1,
		/// <summary>
		/// Min/mag = Nearest, Mip = Nearest, Border = Black Clamp
		/// </summary>
		NearestNearestBlack        = 2,
		/// <summary>
		/// Min/mag = Nearest, Mip = Nearest, Border = Transparent Clamp
		/// </summary>
		NearestNearestTransparent  = 3,
		/// <summary>
		/// Min/mag = Nearest, Mip = Linear, Border = Repeat
		/// </summary>
		NearestLinearRepeat        = 4,
		/// <summary>
		/// Min/mag = Nearest, Mip = Linear, Border = Edge Clamp
		/// </summary>
		NearestLinearEdge          = 5,
		/// <summary>
		/// Min/mag = Nearest, Mip = Linear, Border = Black Clamp
		/// </summary>
		NearestLinearBlack         = 6,
		/// <summary>
		/// Min/mag = Nearest, Mip = Linear, Border = Transparent Clamp
		/// </summary>
		NearestLinearTransparent   = 7,
		/// <summary>
		/// Min/mag = Linear, Mip = Nearest, Border = Repeat
		/// </summary>
		LinearNearestRepeat        = 8,
		/// <summary>
		/// Min/mag = Linear, Mip = Nearest, Border = Edge Clamp
		/// </summary>
		LinearNearestEdge          = 9,
		/// <summary>
		/// Min/mag = Linear, Mip = Nearest, Border = Black Clamp
		/// </summary>
		LinearNearestBlack         = 10,
		/// <summary>
		/// Min/mag = Linear, Mip = Nearest, Border = Transparent Clamp
		/// </summary>
		LinearNearestTransparent   = 11,
		/// <summary>
		/// Min/mag = Linear, Mip = Linear, Border = Repeat
		/// </summary>
		LinearLinearRepeat         = 12,
		/// <summary>
		/// Min/mag = Linear, Mip = Linear, Border = Edge Clamp
		/// </summary>
		LinearLinearEdge           = 13,
		/// <summary>
		/// Min/mag = Linear, Mip = Linear, Border = Black Clamp
		/// </summary>
		LinearLinearBlack          = 14,
		/// <summary>
		/// Min/mag = Linear, Mip = Linear, Border = Transparent Clamp
		/// </summary>
		LinearLinearTransparent    = 15

		// When adding to the end of this, make sure to update the MAX_SAMPLER_COUNT in SamplerPool
	}
}
