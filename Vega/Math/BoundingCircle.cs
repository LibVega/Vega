/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega
{
	/// <summary>
	/// Represents a circular area of 2D space defined by a center point and radius.
	/// </summary>
	public struct BoundingCircle : IEquatable<BoundingCircle>
	{
		#region Fields
		/// <summary>
		/// The center point of the circular area.
		/// </summary>
		public Vec2 Center;
		/// <summary>
		/// The radius of the circular area.
		/// </summary>
		public float Radius;

		/// <summary>
		/// Gets the area contained in the circle.
		/// </summary>
		public readonly float Area => MathF.PI * Radius * Radius;
		#endregion // Fields

		/// <summary>
		/// Constructs a new circular area description.
		/// </summary>
		/// <param name="center">The center point of the circle.</param>
		/// <param name="radius">The radius of the circle.</param>
		public BoundingCircle(Vec2 center, float radius)
		{
			Center = center;
			Radius = radius;
		}

		#region Overrides
		readonly bool IEquatable<BoundingCircle>.Equals(BoundingCircle other) => other == this;

		public readonly override bool Equals(object? obj) => (obj is BoundingCircle bc) && (bc == this);

		public readonly override int GetHashCode() => HashCode.Combine(Center, Radius);

		public readonly override string ToString() => $"{{{Center}, {Radius}}}";
		#endregion // Overrides

		/// <summary>
		/// Gets the point on the circle edge corresponding to the given angle, starting from (Radius, 0).
		/// </summary>
		/// <param name="radians">The angle around the circle, in radians.</param>
		public readonly Vec2 GetPoint(float radians) => 
			Center + new Vec2(Radius * MathF.Cos(radians), Radius * MathF.Sin(radians));

		#region Operators
		public static bool operator == (in BoundingCircle l, in BoundingCircle r) => 
			l.Center == r.Center && l.Radius == r.Radius;
		public static bool operator != (in BoundingCircle l, in BoundingCircle r) =>
			l.Center != r.Center || l.Radius != r.Radius;
		#endregion // Operators

		#region Tuples
		public readonly void Deconstruct(out Vec2 center, out float radius)
		{
			center = Center;
			radius = Radius;
		}

		public static implicit operator BoundingCircle (in (Vec2 center, float radius) tup) =>
			new BoundingCircle(tup.center, tup.radius);
		#endregion // Tuples
	}
}
