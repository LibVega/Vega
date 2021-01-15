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
	/// Vertex type representing a position.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 12)]
	public struct VertexPosition : IEquatable<VertexPosition>
	{
		/// <summary>
		/// The elements that make up this vertex type.
		/// </summary>
		public static readonly IReadOnlyList<VertexElement> Elements = new VertexElement[] { 
			new(VertexFormat.Float3, 0, 1)
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
		#endregion // Fields

		/// <summary>
		/// Construct a new vertex from a position.
		/// </summary>
		public VertexPosition(in Vec3 pos)
		{
			Position = pos;
		}

		#region Overrides
		public readonly override int GetHashCode() => Position.GetHashCode();

		public readonly override string ToString() => $"[P:{Position}]";

		public readonly override bool Equals(object? obj) => (obj is VertexPosition vert) && (vert == this);

		readonly bool IEquatable<VertexPosition>.Equals(VertexPosition other) => other == this;
		#endregion // Overrides

		#region Operators
		public static bool operator == (in VertexPosition l, in VertexPosition r) =>
			(l.Position == r.Position);
		public static bool operator != (in VertexPosition l, in VertexPosition r) =>
			(l.Position != r.Position);
		#endregion // Operators
	}
}
