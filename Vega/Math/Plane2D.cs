/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega
{
	/// <summary>
	/// Represents an infinitely extending plane in 2D space (an infinite line). <see cref="Plane2D.Normal"/> is assumed
	/// to be normalized in all calculations, and all planes constructed by the library will be normalized.
	/// </summary>
	public struct Plane2D : IEquatable<Plane2D>
	{
		#region Fields
		/// <summary>
		/// The normal vector (perpendicular to the surface) describing the plane orientation.
		/// </summary>
		public Vec2 Normal;
		/// <summary>
		/// The distance of the plane from the origin, describing the plane position.
		/// </summary>
		public float D;

		/// <summary>
		/// Gets an identical plane, but with a flipped normal vector.
		/// </summary>
		public readonly Plane2D Flipped => new Plane2D(-Normal, -D);
		/// <summary>
		/// Gets the version of the plane with the normal pointing away from the origin (positive distance).
		/// </summary>
		public readonly Plane2D Positive => (D < 0) ? new Plane2D(-Normal, -D) : this;
		#endregion // Fields

		#region Ctor
		/// <summary>
		/// Constructs a new 2D plane.
		/// </summary>
		/// <param name="normal">The plane normal vector (orientation), will be normalized.</param>
		/// <param name="d">The plane distance from origin (position).</param>
		public Plane2D(in Vec2 normal, float d)
		{
			Normal = normal.Normalized;
			D = d;
		}
		
		/// <summary>
		/// Constructs a new plane passing through the given point, with the given normal.
		/// </summary>
		/// <param name="point">The point to pass the plane through.</param>
		/// <param name="normal">The plane normal, will be normalized.</param>
		public Plane2D(in Vec2 point, in Vec2 normal)
		{
			Normal = normal.Normalized;
			D = Vec2.Dot(point, Normal);
		}

		/// <summary>
		/// Constructs a new plane from normal components and distance.
		/// </summary>
		/// <param name="x">The x-component of the normal.</param>
		/// <param name="y">The y-component of the normal.</param>
		/// <param name="d">The distance from the origin.</param>
		public Plane2D(float x, float y, float d)
		{
			float len = MathF.Sqrt(x * x + y * y);
			Normal = new Vec2(x / len, y / len);
			D = d;
		}

		/// <summary>
		/// Constructs a new plane that passes through the two points. The resulting normal will always point 
		/// <em>left</em> of the vector from the first point to the second.
		/// </summary>
		/// <param name="p1">The first point to define the plane.</param>
		/// <param name="p2">The second point to define the plane.</param>
		public static Plane2D FromPoints(in Vec2 p1, in Vec2 p2)
		{
			float nx = p1.Y - p2.Y, ny = p2.X - p1.X;
			float nlen = MathF.Sqrt(nx * nx + ny * ny);
			var norm = new Vec2(nx / nlen, ny / nlen);
			var d = Vec2.Dot(p1, norm);
			return new Plane2D(norm, d);
		}
		#endregion // Ctor

		#region Overrides
		readonly bool IEquatable<Plane2D>.Equals(Plane2D other) => other == this;

		public readonly override bool Equals(object? obj) => (obj is Plane2D p) && (p == this);

		public readonly override int GetHashCode() => HashCode.Combine(Normal, D);

		public readonly override string ToString() => $"{{{Normal}, {D}}}";
		#endregion // Overrides

		#region Plane Functions
		/// <summary>
		/// Calculates the dot product of the plane and coordinate. The product sign can be used to detect if the
		/// coordinate is in front of, or behind, the plane.
		/// </summary>
		/// <param name="plane">The plane to calculate against.</param>
		/// <param name="coord">The coordinate to dot with the plane.</param>
		public static float Dot(in Plane2D plane, in Vec2 coord) => 
			(plane.Normal.X * coord.X) + (plane.Normal.Y * coord.Y) - plane.D;

		/// <summary>
		/// Calculates the dot product of the plane and a normal.
		/// </summary>
		/// <param name="plane">The plane to calculate against.</param>
		/// <param name="normal">The normal vector to dot with the plane.</param>
		public static float DotNormal(in Plane2D plane, in Vec2 normal) =>
			(plane.Normal.X * normal.X) + (plane.Normal.Y * normal.Y);

		/// <summary>
		/// Calculates the point on the plane that is closest to the given point.
		/// </summary>
		/// <param name="plane">The plane to find the point on.</param>
		/// <param name="point">The point to get closest to the plane.</param>
		public static Vec2 ClosestPoint(in Plane2D plane, in Vec2 point)
		{
			float cx = plane.Normal.Y * point.X, cy = plane.Normal.X * point.Y;
			float nx = (plane.Normal.Y * (cx - cy)) + (plane.Normal.X * plane.D);
			float ny = (plane.Normal.X * (cy - cx)) + (plane.Normal.Y * plane.D);
			return new Vec2(nx, ny);
		}

		/// <summary>
		/// Calculates the distance from the point to the closest point on the plane.
		/// </summary>
		/// <param name="plane">The plane to get the distance to.</param>
		/// <param name="point">The point to get the distance to.</param>
		public static float Distance(in Plane2D plane, in Vec2 point) =>
			MathF.Abs(plane.Normal.X * point.X + plane.Normal.Y * point.Y - plane.D);
		#endregion // Plane Functions

		#region Operators
		public static bool operator == (in Plane2D l, in Plane2D r) => l.Normal == r.Normal && l.D == r.D;
		public static bool operator != (in Plane2D l, in Plane2D r) => l.Normal != r.Normal || l.D != r.D;
		#endregion // Operators

		public readonly void Deconstruct(out Vec2 normal, out float d)
		{
			normal = Normal;
			d = D;
		}
	}
}
