/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	// The subtypes used in shader reflection
	public sealed partial class ShaderInfo
	{
		/// <summary>
		/// Contains information about a vertex inputs attribute for a shader.
		/// </summary>
		public sealed record VertexInput(uint Location, VertexFormat Format, uint ArraySize);

		/// <summary>
		/// Contains information about a fragment attachment output for a shader.
		/// </summary>
		public sealed record FragmentOutput(uint Location, TexelFormat Format);

		/// <summary>
		/// Contains information about a member within a shader uniform struct.
		/// </summary>
		public sealed record UniformMember(string Name, uint Offset, VertexFormat Format, uint ArraySize);
	}
}
