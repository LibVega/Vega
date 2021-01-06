/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.CompilerServices;

namespace Vega
{
	/// <summary>
	/// Extension functions for checking spatial relationships in 2D space.
	/// </summary>
	public static class Space2D
	{
		#region BoundingCircle
		/// <summary>
		/// Checks if the coordinates are within the circle.
		/// </summary>
		/// <param name="bc">The circle to check within.</param>
		/// <param name="x">The x-coordinate.</param>
		/// <param name="y">The y-coordinate.</param>
		public static bool Contains(this in BoundingCircle bc, int x, int y)
		{
			float dx = x - bc.Center.X, dy = y - bc.Center.Y;
			return ((dx * dx) + (dy * dy)) <= (bc.Radius * bc.Radius);
		}
		/// <inheritdoc cref="Contains(in BoundingCircle, int, int)"/>
		public static bool Contains(this in BoundingCircle bc, float x, float y)
		{
			float dx = x - bc.Center.X, dy = y - bc.Center.Y;
			return ((dx * dx) + (dy * dy)) <= (bc.Radius * bc.Radius);
		}
		/// <summary>
		/// Checks if the point is within in the circle.
		/// </summary>
		/// <param name="bc">The circle to check within.</param>
		/// <param name="p">The point to check.</param>
		public static bool Contains(this in BoundingCircle bc, in Point2 p)
		{
			float dx = p.X - bc.Center.X, dy = p.Y - bc.Center.Y;
			return ((dx * dx) + (dy * dy)) <= (bc.Radius * bc.Radius);
		}
		/// <inheritdoc cref="Contains(in BoundingCircle, in Point2)"/>
		public static bool Contains(this in BoundingCircle bc, in Vec2 p)
		{
			float dx = p.X - bc.Center.X, dy = p.Y - bc.Center.Y;
			return ((dx * dx) + (dy * dy)) <= (bc.Radius * bc.Radius);
		}

		/// <summary>
		/// Checks if the second circle is completely contained within the first.
		/// </summary>
		/// <param name="bc">The containing circle.</param>
		/// <param name="o">The circle to check containment of.</param>
		public static bool Contains(this in BoundingCircle bc, in BoundingCircle o)
		{
			var diff = (o.Center - bc.Center).Length;
			return diff <= (bc.Radius - o.Radius);
		}
		/// <summary>
		/// Checks if the line is completely contained within the circle.
		/// </summary>
		/// <param name="bc">The containing circle.</param>
		/// <param name="l">The line to check completely containment of.</param>
		public static bool Contains(this in BoundingCircle bc, in Line2D l)
		{
			float d1 = (l.P1 - bc.Center).LengthSquared,
				d2 = (l.P2 - bc.Center).LengthSquared;
			float r2 = bc.Radius * bc.Radius;
			return (d1 <= r2) && (d2 <= r2);
		}
		/// <summary>
		/// Checks if the rectangle is completely contained within the circle.
		/// </summary>
		/// <param name="bc">The containing circle.</param>
		/// <param name="r">The rectangle to check containment of.</param>
		public static bool Contains(this in BoundingCircle bc, in Rect r)
		{
			float tld = (bc.Center - (Vec2)r.TopLeft).LengthSquared,
				trd = (bc.Center - (Vec2)r.TopRight).LengthSquared,
				bld = (bc.Center - (Vec2)r.BottomLeft).LengthSquared,
				brd = (bc.Center - (Vec2)r.BottomRight).LengthSquared;
			float r2 = bc.Radius * bc.Radius;
			return (tld <= r2) && (trd <= r2) && (bld <= r2) && (brd <= r2);
		}
		/// <inheritdoc cref="Contains(in BoundingCircle, in Rect)"/>
		public static bool Contains(this in BoundingCircle bc, in RectF r)
		{
			float tld = (bc.Center - r.TopLeft).LengthSquared,
				trd = (bc.Center - r.TopRight).LengthSquared,
				bld = (bc.Center - r.BottomLeft).LengthSquared,
				brd = (bc.Center - r.BottomRight).LengthSquared;
			float r2 = bc.Radius * bc.Radius;
			return (tld <= r2) && (trd <= r2) && (bld <= r2) && (brd <= r2);
		}

		/// <summary>
		/// Checks if the two circles overlap at all.
		/// </summary>
		/// <param name="bc">The bounding circle.</param>
		/// <param name="o">The other circle.</param>
		public static bool Intersects(this in BoundingCircle bc, in BoundingCircle o)
		{
			var diff = (o.Center - bc.Center).Length;
			return diff < (bc.Radius + o.Radius);
		}
		/// <summary>
		/// Checks if the circle overlaps with any part of the line segment.
		/// </summary>
		/// <param name="bc">The bounding circle.</param>
		/// <param name="l">The line segment to check.</param>
		public static bool Intersects(this in BoundingCircle bc, in Line2D l)
		{
			float x1 = l.P1.X - bc.Center.X, x2 = l.P2.X - bc.Center.X, 
				y1 = l.P1.Y - bc.Center.Y, y2 = l.P2.Y - bc.Center.Y;
			float dx = x2 - x1, dy = y2 - y1;
			float d2 = (dx * dx) + (dy * dy);
			float dot = (x1 * y2) - (x2 * y1);
			return ((bc.Radius * bc.Radius) * d2) > (dot * dot);
		}
		/// <summary>
		/// Checks if the circle is intersected by the plane.
		/// </summary>
		/// <param name="bc">The bounding circle.</param>
		/// <param name="p">The plane to check the intersection of.</param>
		public static bool Intersects(this in BoundingCircle bc, in Plane2D p) => 
			Plane2D.Distance(p, bc.Center) < bc.Radius;
		/// <summary>
		/// Checks if the circle overlaps with the rectangle to any amount.
		/// </summary>
		/// <param name="bc">The bounding circle.</param>
		/// <param name="r">The rectangle to check.</param>
		public static bool Intersects(this in BoundingCircle bc, in Rect r)
		{
			float tx = Math.Clamp(bc.Center.X, r.Left, r.Right);
			float ty = Math.Clamp(bc.Center.Y, r.Bottom, r.Top);
			float dx = bc.Center.X - tx, dy = bc.Center.Y - ty;
			float d2 = (dx * dx) + (dy * dy);
			return d2 <= (bc.Radius * bc.Radius);
		}
		/// <inheritdoc cref="Intersects(in BoundingCircle, in Rect)"/>
		public static bool Intersects(this in BoundingCircle bc, in RectF r)
		{
			float tx = Math.Clamp(bc.Center.X, r.Left, r.Right);
			float ty = Math.Clamp(bc.Center.Y, r.Bottom, r.Top);
			float dx = bc.Center.X - tx, dy = bc.Center.Y - ty;
			float d2 = (dx * dx) + (dy * dy);
			return d2 <= (bc.Radius * bc.Radius);
		}
		#endregion // BoundingCircle

		#region Line2D
		/// <inheritdoc cref="Intersects(in BoundingCircle, in Line2D)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in Line2D l, in BoundingCircle bc) => Intersects(bc, l);
		/// <summary>
		/// Checks if the two lines intersect at all.
		/// </summary>
		/// <param name="l">The first line to check.</param>
		/// <param name="o">The second line to check.</param>
		public static bool Intersects(this in Line2D l, in Line2D o)
		{
			float a = ((o.P2.X - o.P1.X) * (l.P1.Y - o.P1.Y) - (o.P2.Y - o.P1.Y) * (l.P1.X - o.P1.X)) / 
				((o.P2.Y - o.P1.Y) * (l.P2.X - l.P1.X) - (o.P2.X - o.P1.X) * (l.P2.Y - l.P1.Y));
			float b = ((l.P2.X - l.P1.X) * (l.P1.Y - o.P1.Y) - (l.P2.Y - l.P1.Y) * (l.P1.X - o.P1.X)) /
				((o.P2.Y - o.P1.Y) * (l.P2.X - l.P1.X) - (o.P2.X - o.P1.X) * (l.P2.Y - l.P1.Y));
			return (a >= 0) && (a <= 1) && (b >= 0) && (b <= 1);
		}
		/// <summary>
		/// Checks if the line intersects with the plane.
		/// </summary>
		/// <param name="l">The line to check.</param>
		/// <param name="p">The plane to check.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in Line2D l, in Plane2D p) => 
			(Plane2D.Dot(p, l.P1) < 0) != (Plane2D.Dot(p, l.P2) < 0);
		/// <inheritdoc cref="Intersects(in Rect, in Line2D)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in Line2D l, in Rect r) => Intersects(r, l);
		/// <inheritdoc cref="Intersects(in Rect, in Line2D)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in Line2D l, in RectF r) => Intersects(r, l);
		#endregion // Line2D

		#region Plane2D
		/// <inheritdoc cref="Intersects(in BoundingCircle, in Plane2D)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in Plane2D p, in BoundingCircle bc) => Intersects(bc, p);
		/// <inheritdoc cref="Intersects(in Line2D, in Plane2D)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in Plane2D p, in Line2D l) => Intersects(l, p);
		/// <summary>
		/// Checks if the two planes intersect eachother.
		/// </summary>
		/// <param name="p">The first plane to check.</param>
		/// <param name="o">The second plane to check.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in Plane2D p, in Plane2D o) => 
			(p.Normal == o.Normal) || (p.Normal == -o.Normal);
		/// <inheritdoc cref="Intersects(in Rect, in Plane2D)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in Plane2D p, in Rect r) => Intersects(r, p);
		/// <inheritdoc cref="Intersects(in RectF, in Plane2D)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in Plane2D p, in RectF r) => Intersects(r, p);
		#endregion // Plane2D

		#region Ray2D
		#endregion // Ray2D

		#region Rect
		/// <summary>
		/// Checks if the coordinates are inside of the rectangle.
		/// </summary>
		/// <param name="r">The bounding rectangle.</param>
		/// <param name="x">The x-coordinate to check.</param>
		/// <param name="y">The y-coordinate to check.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in Rect r, int x, int y) => 
			(x >= r.Left) && (x <= r.Right) && (y >= r.Bottom) && (y <= r.Top);
		/// <inheritdoc cref="Contains(in Rect, int, int)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in Rect r, float x, float y) => 
			(x >= r.Left) && (x <= r.Right) && (y >= r.Bottom) && (y <= r.Top);
		/// <summary>
		/// Checks if the position is inside of the rectangle.
		/// </summary>
		/// <param name="r">The bounding rectangle.</param>
		/// <param name="p">The point to check.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in Rect r, in Point2 p) => 
			(p.X >= r.Left) && (p.X <= r.Right) && (p.Y >= r.Bottom) && (p.Y <= r.Top);
		/// <inheritdoc cref="Contains(in Rect, in Point2)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in Rect r, in Vec2 p) => 
			(p.X >= r.Left) && (p.X <= r.Right) && (p.Y >= r.Bottom) && (p.Y <= r.Top);

		/// <summary>
		/// Checks if the circle is contained completely within the rectangle.
		/// </summary>
		/// <param name="r">The bounding rectangle.</param>
		/// <param name="bc">The circle to check.</param>
		public static bool Contains(this in Rect r, in BoundingCircle bc)
		{
			float ct = bc.Center.Y + bc.Radius,
				cb = bc.Center.Y - bc.Radius,
				cl = bc.Center.X - bc.Radius,
				cr = bc.Center.X + bc.Radius;
			return (ct <= r.Top) && (cb >= r.Bottom) && (cl >= r.Left) && (cr <= r.Right);
		}
		/// <summary>
		/// Checks if the line is completely contained within the rectangle.
		/// </summary>
		/// <param name="r">The bounding rectangle.</param>
		/// <param name="l">The line to check.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in Rect r, in Line2D l) => Contains(r, l.P1) && Contains(r, l.P2);
		/// <summary>
		/// Checks if the second rectangle is completely contained by the first.
		/// </summary>
		/// <param name="r">The bounding rectangle.</param>
		/// <param name="o">The rectangle to check.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in Rect r, in Rect o) => 
			(r.Left <= o.Left) && (r.Right >= o.Right) && (r.Bottom <= o.Bottom) && (r.Top >= o.Top);
		/// <inheritdoc cref="Contains(in Rect, in Rect)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in Rect r, in RectF o) => 
			(r.Left <= o.Left) && (r.Right >= o.Right) && (r.Bottom <= o.Bottom) && (r.Top >= o.Top);

		/// <inheritdoc cref="Intersects(in BoundingCircle, in Rect)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in Rect r, in BoundingCircle bc) => Intersects(bc, r);
		/// <summary>
		/// Checks if the rectangle and line segment overlap at all.
		/// </summary>
		/// <param name="r">The bounding rectangle.</param>
		/// <param name="l">The line to check.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in Rect r, in Line2D l) => Intersects(r, l.Plane);
		/// <summary>
		/// Checks if the rectangle and plane overlap.
		/// </summary>
		/// <param name="r">The bounding rectangle.</param>
		/// <param name="p">The plane to check.</param>
		public static bool Intersects(this in Rect r, in Plane2D p)
		{
			bool tl = Plane2D.Dot(p, (Vec2)r.TopLeft) < 0,
				tr = Plane2D.Dot(p, (Vec2)r.TopRight) < 0,
				bl = Plane2D.Dot(p, (Vec2)r.BottomLeft) < 0,
				br = Plane2D.Dot(p, (Vec2)r.BottomRight) < 0;
			return tl != tr || tr != bl || bl != br;
		}
		/// <summary>
		/// Checks if the two rectangles share any overlap in their area.
		/// </summary>
		/// <param name="r1">The first rectangle.</param>
		/// <param name="r2">The second rectangle.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in Rect r1, in Rect r2) => 
			(r2.Left < r1.Right) && (r1.Left < r2.Right) && (r2.Top > r1.Bottom) && (r1.Top > r2.Bottom);
		/// <inheritdoc cref="Intersects(in Rect, in Rect)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in Rect r1, in RectF r2) => 
			(r2.Left < r1.Right) && (r1.Left < r2.Right) && (r2.Top > r1.Bottom) && (r1.Top > r2.Bottom);
		#endregion // Rect

		#region RectF
		/// <inheritdoc cref="Contains(in Rect, int, int)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in RectF r, int x, int y) => 
			(x >= r.Left) && (x <= r.Right) && (y >= r.Bottom) && (y <= r.Top);
		/// <inheritdoc cref="Contains(in Rect, int, int)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in RectF r, float x, float y) => 
			(x >= r.Left) && (x <= r.Right) && (y >= r.Bottom) && (y <= r.Top);
		/// <inheritdoc cref="Contains(in Rect, in Point2)/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in RectF r, in Point2 p) => 
			(p.X >= r.Left) && (p.X <= r.Right) && (p.Y >= r.Bottom) && (p.Y <= r.Top);
		/// <inheritdoc cref="Contains(in Rect, in Point2)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in RectF r, in Vec2 p) => 
			(p.X >= r.Left) && (p.X <= r.Right) && (p.Y >= r.Bottom) && (p.Y <= r.Top);

		/// <inheritdoc cref="Contains(in Rect, in BoundingCircle)"/>
		public static bool Contains(this in RectF r, in BoundingCircle bc)
		{
			float ct = bc.Center.Y + bc.Radius,
				cb = bc.Center.Y - bc.Radius,
				cl = bc.Center.X - bc.Radius,
				cr = bc.Center.X + bc.Radius;
			return (ct <= r.Top) && (cb >= r.Bottom) && (cl >= r.Left) && (cr <= r.Right);
		}
		/// <inheritdoc cref="Contains(in Rect, in Line2D)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in RectF r, in Line2D l) => Contains(r, l.P1) && Contains(r, l.P2);
		/// <inheritdoc cref="Contains(in Rect, in Rect)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in RectF r, in Rect o) => 
			(r.Left <= o.Left) && (r.Right >= o.Right) && (r.Bottom <= o.Bottom) && (r.Top >= o.Top);
		/// <inheritdoc cref="Contains(in Rect, in Rect)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in RectF r, in RectF o) => 
			(r.Left <= o.Left) && (r.Right >= o.Right) && (r.Bottom <= o.Bottom) && (r.Top >= o.Top);

		/// <inheritdoc cref="Intersects(in BoundingCircle, in Rect)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in RectF r, in BoundingCircle bc) => Intersects(bc, r);
		/// <inheritdoc cref="Intersects(in Rect, in Line2D)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in RectF r, in Line2D l) => Intersects(r, l.Plane);
		/// <inheritdoc cref="Intersects(in Rect, in Plane2D)"/>
		public static bool Intersects(this in RectF r, in Plane2D p)
		{
			bool tl = Plane2D.Dot(p, (Vec2)r.TopLeft) < 0,
				tr = Plane2D.Dot(p, (Vec2)r.TopRight) < 0,
				bl = Plane2D.Dot(p, (Vec2)r.BottomLeft) < 0,
				br = Plane2D.Dot(p, (Vec2)r.BottomRight) < 0;
			return tl != tr || tr != bl || bl != br;
		}
		/// <inheritdoc cref="Intersects(in Rect, in Rect)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in RectF r1, in Rect r2) => 
			(r2.Left < r1.Right) && (r1.Left < r2.Right) && (r2.Top > r1.Bottom) && (r1.Top > r2.Bottom);
		/// <inheritdoc cref="Intersects(in Rect, in Rect)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in RectF r1, in RectF r2) => 
			(r2.Left < r1.Right) && (r1.Left < r2.Right) && (r2.Top > r1.Bottom) && (r1.Top > r2.Bottom);
		#endregion // RectF
	}
}
