/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Vega
{
	/// <summary>
	/// Describes a location in 2D cartesian integer space.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size=2*sizeof(int))]
	public struct Point2 : IEquatable<Point2>
	{
		/// <summary>
		/// Represents the origin of the representable space, at coordinates (0, 0).
		/// </summary>
		public static readonly Point2 Zero = new(0);

		#region Fields
		/// <summary>
		/// The x-coordinate of the point.
		/// </summary>
		[FieldOffset(0)]
		public int X;
		/// <summary>
		/// The y-coordinate of the point.
		/// </summary>
		[FieldOffset(sizeof(int))]
		public int Y;
		#endregion // Fields

		#region Ctor
		/// <summary>
		/// Constructs a point with all components equal to the value.
		/// </summary>
		/// <param name="i">The coordinate value.</param>
		public Point2(int i) => X = Y = i;

		/// <summary>
		/// Constructs a point with the given component values.
		/// </summary>
		/// <param name="x">The x-coordinate value.</param>
		/// <param name="y">The y-coordinate value.</param>
		public Point2(int x, int y)
		{
			X = x;
			Y = y;
		}
		#endregion // Ctor

		#region Overrides
		public readonly override bool Equals(object? obj) => (obj is Point2 p) && (p == this);

		public readonly override int GetHashCode() => HashCode.Combine(X, Y);

		public readonly override string ToString() => $"{{{X} {Y}}}";

		readonly bool IEquatable<Point2>.Equals(Point2 other) => other == this;
		#endregion // Overrides

		#region Distance
		/// <summary>
		/// Gets the distance between two points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Distance(in Point2 l, in Point2 r)
		{
			int dx = l.X - r.X, dy = l.Y - r.Y;
			return MathF.Sqrt(dx * dx + dy * dy);
		}

		/// <summary>
		/// Gets the distance squared between two points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float DistanceSquared(in Point2 l, in Point2 r)
		{
			int dx = l.X - r.X, dy = l.Y - r.Y;
			return dx * dx + dy * dy;
		}
		#endregion // Distance

		#region Standard Math
		/// <summary>
		/// Component-wise maximum of the two input points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		/// <returns>The output value for the minimized point.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point2 Min(in Point2 l, in Point2 r) =>
			new Point2(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y);

		/// <summary>
		/// Component-wise maximum of the two input points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		/// <param name="p">The output value for the minimized point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Min(in Point2 l, in Point2 r, out Point2 p) => (p.X, p.Y) =
			(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y);

		/// <summary>
		/// Component-wise minimum of the two input points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		/// <returns>The output value for the maximized point.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point2 Max(in Point2 l, in Point2 r) =>
			new Point2(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y);

		/// <summary>
		/// Component-wise minimum of the two input points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		/// <param name="p">The output value for the maximized point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Max(in Point2 l, in Point2 r, out Point2 p) => (p.X, p.Y) =
			(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y);

		/// <summary>
		/// Component-wise clamp between of the two limiting points.
		/// </summary>
		/// <param name="val">The point to clamp.</param>
		/// <param name="min">The minimum bounding point.</param>
		/// <param name="max">The maximum bounding point.</param>
		/// <returns>The output clamped point.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point2 Clamp(in Point2 val, in Point2 min, in Point2 max) =>
			new Point2(Math.Clamp(val.X, min.X, max.X), Math.Clamp(val.Y, min.Y, max.Y));

		/// <summary>
		/// Component-wise clamp between of the two limiting points.
		/// </summary>
		/// <param name="val">The point to clamp.</param>
		/// <param name="min">The minimum bounding point.</param>
		/// <param name="max">The maximum bounding point.</param>
		/// <param name="p">The output clamped point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clamp(in Point2 val, in Point2 min, in Point2 max, out Point2 p) => (p.X, p.Y) =
			(Math.Clamp(val.X, min.X, max.X), Math.Clamp(val.Y, min.Y, max.Y));
		#endregion // Standard Math

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Point2 l, in Point2 r) => (l.X == r.X) && (l.Y == r.Y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Point2 l, in Point2 r) => (l.X != r.X) || (l.Y != r.Y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point2 operator + (in Point2 l, in Point2 r) => new Point2(l.X + r.X, l.Y + r.Y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point2 operator - (in Point2 l, in Point2 r) => new Point2(l.X - r.X, l.Y - r.Y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point2 operator * (in Point2 l, int r) => new Point2(l.X * r, l.Y * r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point2 operator * (int l, in Point2 r) => new Point2(l * r.X, l * r.Y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point2 operator / (in Point2 l, int r) => new Point2(l.X / r, l.Y / r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Point2 (in Point3 p) => new Point2(p.X, p.Y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Point2 (in Extent2 e) => new Point2((int)e.Width, (int)e.Height);
		#endregion // Operators

		#region Tuples
		public readonly void Deconstruct(out int x, out int y)
		{
			x = X;
			y = Y;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Point2 (in (int x, int y) tup) =>
			new Point2(tup.x, tup.y);
		#endregion // Tuples
	}
}
