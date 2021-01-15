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
	/// Vertex type representing a position, normal, color, and texture coordinate.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 36)]
	public struct VertexPositionNormalColorTexture : IEquatable<VertexPositionNormalColorTexture>
	{
		/// <summary>
		/// The elements that make up this vertex type.
		/// </summary>
		public static readonly IReadOnlyList<VertexElement> Elements = new VertexElement[] {
			new(VertexFormat.Float3, 0, 1),
			new(VertexFormat.Float3, 12, 1),
			new(VertexFormat.Float4Unorm8, 24, 1),
			new(VertexFormat.Float2, 28, 1)
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
		/// The vertex normal direction.
		/// </summary>
		[FieldOffset(12)] public Vec3 Normal;
		/// <summary>
		/// The vertex color.
		/// </summary>
		[FieldOffset(24)] public Color Color;
		/// <summary>
		/// The vertex texture coordinate.
		/// </summary>
		[FieldOffset(28)] public Vec2 TexCoord;
		#endregion // Fields

		/// <summary>
		/// Construct a new vertex from a position, normal, color, and texture coordinate.
		/// </summary>
		public VertexPositionNormalColorTexture(in Vec3 pos, in Vec3 normal, in Color color, in Vec2 uv)
		{
			Position = pos;
			Normal = normal;
			Color = color;
			TexCoord = uv;
		}

		#region Overrides
		public readonly override int GetHashCode() =>
			Position.GetHashCode() ^ Normal.GetHashCode() ^ Color.GetHashCode() ^ TexCoord.GetHashCode();

		public readonly override string ToString() => $"[P:{Position},N:{Normal},C:{Color},T:{TexCoord}]";

		public readonly override bool Equals(object? obj) =>
			(obj is VertexPositionNormalColorTexture vert) && (vert == this);

		readonly bool IEquatable<VertexPositionNormalColorTexture>.Equals(VertexPositionNormalColorTexture other) =>
			other == this;
		#endregion // Overrides

		#region Operators
		public static bool operator == (in VertexPositionNormalColorTexture l, in VertexPositionNormalColorTexture r) =>
			(l.Position == r.Position) && (l.Normal == r.Normal) && (l.Color == r.Color) && (l.TexCoord == r.TexCoord);
		public static bool operator != (in VertexPositionNormalColorTexture l, in VertexPositionNormalColorTexture r) =>
			(l.Position != r.Position) || (l.Normal != r.Normal) || (l.Color != r.Color) || (l.TexCoord != r.TexCoord);
		#endregion // Operators
	}
}
