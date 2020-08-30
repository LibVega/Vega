/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
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
		#region Rect
		/// <summary>
		/// Checks if the coordinates are inside of the rectangle.
		/// </summary>
		/// <param name="r">The bounding rectangle.</param>
		/// <param name="x">The x-coordinate to check.</param>
		/// <param name="y">The y-coordinate to check.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in Rect r, int x, int y) => (x >= r.Left) && (x <= r.Right) && (y >= r.Bottom) && (y <= r.Top);
		/// <inheritdoc cref="Contains(in Rect, int, int)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in Rect r, float x, float y) => (x >= r.Left) && (x <= r.Right) && (y >= r.Bottom) && (y <= r.Top);

		/// <summary>
		/// Checks if the position is inside of the rectangle.
		/// </summary>
		/// <param name="r">The bounding rectangle.</param>
		/// <param name="p">The point to check.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in Rect r, in Point2 p) => (p.X >= r.Left) && (p.X <= r.Right) && (p.Y >= r.Bottom) && (p.Y <= r.Top);
		/// <inheritdoc cref="Contains(in Rect, in Point2)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in Rect r, in Vec2 p) => (p.X >= r.Left) && (p.X <= r.Right) && (p.Y >= r.Bottom) && (p.Y <= r.Top);

		/// <summary>
		/// Checks if the second rectangle is completely contained by the first.
		/// </summary>
		/// <param name="r">The bounding rectangle.</param>
		/// <param name="o">The rectangle to check.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in Rect r, in Rect o) => (r.Left <= o.Left) && (r.Right >= o.Right) && (r.Bottom <= o.Bottom) && (r.Top >= o.Top);
		/// <inheritdoc cref="Contains(in Rect, in Rect)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in Rect r, in RectF o) => (r.Left <= o.Left) && (r.Right >= o.Right) && (r.Bottom <= o.Bottom) && (r.Top >= o.Top);

		/// <summary>
		/// Checks if the two rectangles share any overlap in their area.
		/// </summary>
		/// <param name="r1">The first rectangle.</param>
		/// <param name="r2">The second rectangle.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in Rect r1, in Rect r2) => (r2.Left < r1.Right) && (r1.Left < r2.Right) && (r2.Top > r1.Bottom) && (r1.Top > r2.Bottom);
		/// <inheritdoc cref="Intersects(in Rect, in Rect)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in Rect r1, in RectF r2) => (r2.Left < r1.Right) && (r1.Left < r2.Right) && (r2.Top > r1.Bottom) && (r1.Top > r2.Bottom);
		#endregion // Rect

		#region RectF
		/// <inheritdoc cref="Contains(in Rect, int, int)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in RectF r, int x, int y) => (x >= r.Left) && (x <= r.Right) && (y >= r.Bottom) && (y <= r.Top);
		/// <inheritdoc cref="Contains(in Rect, int, int)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in RectF r, float x, float y) => (x >= r.Left) && (x <= r.Right) && (y >= r.Bottom) && (y <= r.Top);

		/// <inheritdoc cref="Contains(in Rect, in Point2)/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in RectF r, in Point2 p) => (p.X >= r.Left) && (p.X <= r.Right) && (p.Y >= r.Bottom) && (p.Y <= r.Top);
		/// <inheritdoc cref="Contains(in Rect, in Point2)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in RectF r, in Vec2 p) => (p.X >= r.Left) && (p.X <= r.Right) && (p.Y >= r.Bottom) && (p.Y <= r.Top);

		/// <inheritdoc cref="Contains(in Rect, in Rect)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in RectF r, in Rect o) => (r.Left <= o.Left) && (r.Right >= o.Right) && (r.Bottom <= o.Bottom) && (r.Top >= o.Top);
		/// <inheritdoc cref="Contains(in Rect, in Rect)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(this in RectF r, in RectF o) => (r.Left <= o.Left) && (r.Right >= o.Right) && (r.Bottom <= o.Bottom) && (r.Top >= o.Top);

		/// <inheritdoc cref="Intersects(in Rect, in Rect)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in RectF r1, in Rect r2) => (r2.Left < r1.Right) && (r1.Left < r2.Right) && (r2.Top > r1.Bottom) && (r1.Top > r2.Bottom);
		/// <inheritdoc cref="Intersects(in Rect, in Rect)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this in RectF r1, in RectF r2) => (r2.Left < r1.Right) && (r1.Left < r2.Right) && (r2.Top > r1.Bottom) && (r1.Top > r2.Bottom);
		#endregion // RectF
	}
}
