/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vega.Graphics;

namespace Vega.Render
{
	/// <summary>
	/// Vertex type representing a position and texture coordinate.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 20)]
	public struct VertexPT : IEquatable<VertexPT>
	{
		/// <summary>
		/// The elements that make up this vertex type.
		/// </summary>
		public static readonly IReadOnlyList<VertexElement> Elements = new VertexElement[] {
			new(VertexFormat.Float3, 0, 1),
			new(VertexFormat.Float2, 12, 1)
		};
		/// <summary>
		/// The default vertex description for this vertex type.
		/// </summary>
		public static readonly VertexDescription Description = new(Elements);

		#region Fields
		/// <summary>
		/// The vertex position.
		/// </summary>
		[FieldOffset(0)] public Vec3 Position;
		/// <summary>
		/// The vertex texture coordinate.
		/// </summary>
		[FieldOffset(12)] public Vec2 TexCoord;
		#endregion // Fields

		/// <summary>
		/// Construct a new vertex from a position and texture coordinate.
		/// </summary>
		public VertexPT(in Vec3 pos, in Vec2 uv)
		{
			Position = pos;
			TexCoord = uv;
		}

		#region Overrides
		public readonly override int GetHashCode() => Position.GetHashCode() ^ TexCoord.GetHashCode();

		public readonly override string ToString() => $"[P:{Position},T:{TexCoord}]";

		public readonly override bool Equals(object? obj) => (obj is VertexPT vert) && (vert == this);

		readonly bool IEquatable<VertexPT>.Equals(VertexPT other) => other == this;
		#endregion // Overrides

		#region Operators
		public static bool operator == (in VertexPT l, in VertexPT r) =>
			(l.Position == r.Position) && (l.TexCoord == r.TexCoord);
		public static bool operator != (in VertexPT l, in VertexPT r) =>
			(l.Position != r.Position) || (l.TexCoord != r.TexCoord);
		#endregion // Operators
	}
}
