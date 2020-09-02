/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega
{
	/// <summary>
	/// Represents a ray in 2D space, with an origin point and direction. <see cref="Ray2D.Direction"/> is assumed
	/// to be normalized, and ray instances produced by the library will always have normalized directions.
	/// </summary>
	public struct Ray2D : IEquatable<Ray2D>
	{
		#region Fields
		/// <summary>
		/// The origin point of the ray.
		/// </summary>
		public Vec2 Origin;
		/// <summary>
		/// The direction of the ray, assumed to be normalized in ray operations.
		/// </summary>
		public Vec2 Direction;
		#endregion // Fields

		/// <summary>
		/// Constructs a new ray.
		/// </summary>
		/// <param name="origin">The ray origin.</param>
		/// <param name="direction">The ray direction, will be normalized.</param>
		public Ray2D(in Vec2 origin, in Vec2 direction)
		{
			Origin = origin;
			Direction = direction.Normalized;
		}

		#region Overrides
		readonly bool IEquatable<Ray2D>.Equals(Ray2D other) => other == this;

		public readonly override bool Equals(object? obj) => (obj is Ray2D r) && (r == this);

		public readonly override int GetHashCode() => HashCode.Combine(Origin, Direction);

		public readonly override string ToString() => $"{{{Origin}->{Direction}}}";
		#endregion // Overrides

		/// <summary>
		/// Calculates a new position from a distance along the ray.
		/// </summary>
		/// <param name="distance">The distance along the ray to calculate the position from.</param>
		/// <returns>The position along the ray.</returns>
		public readonly Vec2 GetPosition(float distance) => Origin + (Direction * distance);

		/// <summary>
		/// Constructs a line segment going from the ray origin to the given distance along the ray.
		/// </summary>
		/// <param name="distance">The distance along the ray, or the length of the final line segment.</param>
		public readonly Line2D GetLine(float distance) => new Line2D(Origin, Origin + (Direction * distance));

		#region Operators
		public static bool operator == (in Ray2D l, in Ray2D r) => l.Origin == r.Origin && l.Direction == r.Direction;
		public static bool operator != (in Ray2D l, in Ray2D r) => l.Origin != r.Origin || l.Direction != r.Direction;
		#endregion // Operators

		#region Tuples
		public readonly void Deconstruct(out Vec2 origin, out Vec2 direction)
		{
			origin = Origin;
			direction = Direction;
		}

		public static implicit operator Ray2D (in (Vec2 origin, Vec2 direction) tup) =>
			new Ray2D(tup.origin, tup.direction);
		#endregion // Tuples
	}
}
