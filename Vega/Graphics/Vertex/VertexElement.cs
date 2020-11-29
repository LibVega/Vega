/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// Describes a single data element of a vertex, and how it is accessed from a vertex buffer.
	/// </summary>
	public struct VertexElement : IEquatable<VertexElement>, IComparable<VertexElement>
	{
		#region Fields
		/// <summary>
		/// The format of the element data.
		/// </summary>
		public readonly VertexFormat Format;
		/// <summary>
		/// The offset of the element data within a vertex buffer, from the beginning.
		/// </summary>
		public readonly uint Offset;
		#endregion // Fields

		/// <summary>
		/// Describes a new vertex element.
		/// </summary>
		public VertexElement(VertexFormat format, uint offset)
		{
			Format = format;
			Offset = offset;
		}

		#region Overrides
		public readonly override int GetHashCode() => Format.GetHashCode() ^ Offset.GetHashCode();

		public readonly override string ToString() => $"[{Format}:{Offset}]";

		public readonly override bool Equals(object? obj) => (obj is VertexElement e) && (e == this);

		readonly bool IEquatable<VertexElement>.Equals(VertexElement other) => other == this;

		readonly int IComparable<VertexElement>.CompareTo(VertexElement other) => Offset.CompareTo(other.Offset);
		#endregion // Overrides

		#region Operators
		public static bool operator == (VertexElement l, VertexElement r) =>
			(l.Format == r.Format) && (l.Offset == r.Offset);

		public static bool operator != (VertexElement l, VertexElement r) =>
			(l.Format != r.Format) || (l.Offset != r.Offset);
		#endregion // Operators
	}
}
