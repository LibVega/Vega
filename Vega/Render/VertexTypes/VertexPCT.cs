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
	/// Vertex type representing a position, color, and texture coordinate.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 24)]
	public struct VertexPCT : IEquatable<VertexPCT>
	{
		/// <summary>
		/// The elements that make up this vertex type.
		/// </summary>
		public static readonly IReadOnlyList<VertexElement> Elements = new VertexElement[] {
			new(VertexFormat.Float3, 0, 1),
			new(VertexFormat.Float4Unorm8, 12, 1),
			new(VertexFormat.Float2, 16, 1)
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
		/// The vertex color.
		/// </summary>
		[FieldOffset(12)] public Color Color;
		/// <summary>
		/// The vertex texture coordinate.
		/// </summary>
		[FieldOffset(16)] public Vec2 TexCoord;
		#endregion // Fields

		/// <summary>
		/// Construct a new vertex from a position, color, and texture coordinate.
		/// </summary>
		public VertexPCT(in Vec3 pos, in Color color, in Vec2 uv)
		{
			Position = pos;
			Color = color;
			TexCoord = uv;
		}

		#region Overrides
		public readonly override int GetHashCode() => 
			Position.GetHashCode() ^ Color.GetHashCode() ^ TexCoord.GetHashCode();

		public readonly override string ToString() => $"[P:{Position},C:{Color},T:{TexCoord}]";

		public readonly override bool Equals(object? obj) => 
			(obj is VertexPCT vert) && (vert == this);

		readonly bool IEquatable<VertexPCT>.Equals(VertexPCT other) => 
			other == this;
		#endregion // Overrides

		#region Operators
		public static bool operator == (in VertexPCT l, in VertexPCT r) =>
			(l.Position == r.Position) && (l.Color == r.Color) && (l.TexCoord == r.TexCoord);
		public static bool operator != (in VertexPCT l, in VertexPCT r) =>
			(l.Position != r.Position) || (l.Color != r.Color) || (l.TexCoord != r.TexCoord);
		#endregion // Operators
	}
}
