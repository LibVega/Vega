/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Vega.Graphics
{
	/// <summary>
	/// Vertex type representing a position and color.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 16)]
	public struct VertexPositionColor : IEquatable<VertexPositionColor>
	{
		/// <summary>
		/// The elements that make up this vertex type.
		/// </summary>
		public static readonly IReadOnlyList<VertexElement> Elements = new VertexElement[] {
			new(VertexFormat.Float3, 0, 1),
			new(VertexFormat.Float4Unorm8, 12, 1)
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
		#endregion // Fields

		/// <summary>
		/// Construct a new vertex from a position and color.
		/// </summary>
		public VertexPositionColor(in Vec3 pos, in Color color)
		{
			Position = pos;
			Color = color;
		}

		#region Overrides
		public readonly override int GetHashCode() => Position.GetHashCode() ^ Color.GetHashCode();

		public readonly override string ToString() => $"[P:{Position},C:{Color}]";

		public readonly override bool Equals(object? obj) => (obj is VertexPositionColor vert) && (vert == this);

		readonly bool IEquatable<VertexPositionColor>.Equals(VertexPositionColor other) => other == this;
		#endregion // Overrides

		#region Operators
		public static bool operator == (in VertexPositionColor l, in VertexPositionColor r) =>
			(l.Position == r.Position) && (l.Color == r.Color);
		public static bool operator != (in VertexPositionColor l, in VertexPositionColor r) =>
			(l.Position != r.Position) || (l.Color != r.Color);
		#endregion // Operators
	}
}
