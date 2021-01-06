/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega
{
	/// <summary>
	/// Represents a finite line segment in 2D space, defined by two end points.
	/// </summary>
	public struct Line2D : IEquatable<Line2D>
	{
		#region Fields
		/// <summary>
		/// The first point defining one end of the line segment.
		/// </summary>
		public Vec2 P1;
		/// <summary>
		/// The second point defining one end of the line segment.
		/// </summary>
		public Vec2 P2;

		/// <summary>
		/// The length of the line segment.
		/// </summary>
		public readonly float Length => MathF.Sqrt((P1.X - P2.X) * (P1.X - P2.X) + (P1.Y - P2.Y) * (P1.Y - P2.Y));
		/// <summary>
		/// The squared length of the line segment.
		/// </summary>
		public readonly float LenghSquared => (P1.X - P2.X) * (P1.X - P2.X) + (P1.Y - P2.Y) * (P1.Y - P2.Y);

		/// <summary>
		/// Constructs a ray starting at <see cref="P1"/>, pointing in the direction of <see cref="P2"/>.
		/// </summary>
		public readonly Ray2D Ray1 => new Ray2D(P1, P2 - P1);
		/// <summary>
		/// Constructs a ray starting at <see cref="P2"/>, pointing in the direction of <see cref="P1"/>.
		/// </summary>
		public readonly Ray2D Ray2 => new Ray2D(P2, P1 - P2);

		/// <summary>
		/// Constructs the plane that this line is contained within.
		/// </summary>
		public readonly Plane2D Plane => Plane2D.FromPoints(P1, P2);
		#endregion // Fields

		/// <summary>
		/// Constructs a new line segment.
		/// </summary>
		/// <param name="p1">The first line end-point.</param>
		/// <param name="p2">The second line end-point.</param>
		public Line2D(in Vec2 p1, in Vec2 p2)
		{
			P1 = p1;
			P2 = p2;
		}

		#region Overrides
		readonly bool IEquatable<Line2D>.Equals(Line2D other) => other == this;

		public readonly override bool Equals(object? obj) => (obj is Line2D l) && (l == this);

		public readonly override int GetHashCode() => HashCode.Combine(P1, P2);

		public readonly override string ToString() => $"{{{P1}<->{P2}}}";
		#endregion // Overrides

		/// <summary>
		/// Gets a point along the line between <see cref="P1"/> and <see cref="P2"/>.
		/// </summary>
		/// <param name="amt">The normalized [0, 1] interpolation amount between the points.</param>
		public readonly Vec2 GetPoint(float amt) => P1 + ((P2 - P1) * Math.Clamp(amt, 0, 1));

		#region Operators
		public static bool operator == (in Line2D l, in Line2D r) =>
			(l.P1 == r.P1 && l.P2 == r.P2) || (l.P1 == r.P2 && l.P2 == r.P1);
		public static bool operator != (in Line2D l, in Line2D r) =>
			(l.P1 != r.P1 || l.P2 != r.P2) && (l.P1 != r.P2 || l.P2 != r.P1);
		#endregion // Operators

		public readonly void Deconstruct(out Vec2 p1, out Vec2 p2)
		{
			p1 = P1;
			p2 = P2;
		}
	}
}
