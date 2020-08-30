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
	/// The floating point version of the <see cref="Rect"/> type, for continuous cartesian space. Note that this type
	/// does not check for negative size.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size=4*sizeof(float))]
	public struct RectF : IEquatable<RectF>
	{
		/// <summary>
		/// Represents an empty rectangle with zero dimensions.
		/// </summary>
		public static readonly RectF Empty = new(0f, 0f, 0f, 0f);

		#region Fields
		/// <summary>
		/// The x-coordinate of the left side of the rectangle.
		/// </summary>
		[FieldOffset(0)]
		public float X;
		/// <summary>
		/// The y-coordinate of the bottom side of the rectangle.
		/// </summary>
		[FieldOffset(sizeof(float))]
		public float Y;
		/// <summary>
		/// The width of the rectangle (along x-axis).
		/// </summary>
		[FieldOffset(2*sizeof(float))]
		public float Width;
		/// <summary>
		/// The height of the rectangle (along y-axis).
		/// </summary>
		[FieldOffset(3*sizeof(float))]
		public float Height;

		/// <summary>
		/// The bottom left corner of the rectangle.
		/// </summary>
		public Vec2 Position
		{
			readonly get => new Vec2(X, Y);
			set { X = value.X; Y = value.Y; }
		}
		/// <summary>
		/// The dimensions of the rectangle.
		/// </summary>
		public Vec2 Size
		{
			readonly get => new Vec2(Width, Height);
			set { Width = value.X; Height = value.Y; }
		}

		/// <summary>
		/// The top-left corner, assuming an up/right coordinate system.
		/// </summary>
		public readonly Vec2 TopLeft => new Vec2(X, Y + Height);
		/// <summary>
		/// The top-right corner, assuming an up/right coordinate system.
		/// </summary>
		public readonly Vec2 TopRight => new Vec2(X + Width, Y + Height);
		/// <summary>
		/// The bottom-left corner, assuming an up/right coordinate system.
		/// </summary>
		public readonly Vec2 BottomLeft => new Vec2(X, Y);
		/// <summary>
		/// The bottom-right corner, assuming an up/right coordinate system.
		/// </summary>
		public readonly Vec2 BottomRight => new Vec2(X + Width, Y);

		/// <summary>
		/// The top-left corner, assuming an down/right coordinate system.
		/// </summary>
		public readonly Vec2 TopLeftInv => new Vec2(X, Y);
		/// <summary>
		/// The top-right corner, assuming an down/right coordinate system.
		/// </summary>
		public readonly Vec2 TopRightInv => new Vec2(X + Width, Y);
		/// <summary>
		/// The bottom-left corner, assuming an down/right coordinate system.
		/// </summary>
		public readonly Vec2 BottomLeftInv => new Vec2(X, Y + Height);
		/// <summary>
		/// The bottom-right corner, assuming an down/right coordinate system.
		/// </summary>
		public readonly Vec2 BottomRightInv => new Vec2(X + Width, Y + Height);

		/// <summary>
		/// The x-coordinate of the left edge.
		/// </summary>
		public readonly float Left => X;
		/// <summary>
		/// The x-coordinate of the right edge.
		/// </summary>
		public readonly float Right => X + Width;
		/// <summary>
		/// The y-coordinate of the top edge, assuming an up/right coordinate system.
		/// </summary>
		public readonly float Top => Y + Height;
		/// <summary>
		/// The y-coordinate of the bottom edge, assuming an up/right coordinate system.
		/// </summary>
		public readonly float Bottom => Y;
		/// <summary>
		/// The y-coordinate of the top edge, assuming an down/right coordinate system.
		/// </summary>
		public readonly float TopInv => Y;
		/// <summary>
		/// The y-coordinate of the bottom edge, assuming an down/right coordinate system.
		/// </summary>
		public readonly float BottomInv => Y + Height;

		/// <summary>
		/// The area of the rectangle interior.
		/// </summary>
		public readonly float Area => Width * Height;

		/// <summary>
		/// The center of the rectangle.
		/// </summary>
		public readonly Vec2 Center => new Vec2(X + (Width / 2), Y + (Height / 2));

		/// <summary>
		/// Gets if the dimensions of the rectangle are positive.
		/// </summary>
		public readonly bool IsReal => (Width >= 0) && (Height >= 0);
		#endregion // Fields

		#region Ctor
		/// <summary>
		/// Constructs a new rectangle from the given coordinates and dimensions
		/// </summary>
		/// <param name="x">The x-coordinate of the left side.</param>
		/// <param name="y">The y-coordinate of the bottom side.</param>
		/// <param name="w">The width.</param>
		/// <param name="h">The height.</param>
		public RectF(float x, float y, float w, float h)
		{
			X = x;
			Y = y;
			Width = w;
			Height = h;
		}

		/// <summary>
		/// Constructs a new rectangle from the position and size.
		/// </summary>
		/// <param name="pos">The rectangle position.</param>
		/// <param name="ex">The rectangle size.</param>
		public RectF(in Vec2 pos, in Vec2 ex)
		{
			X = pos.X;
			Y = pos.Y;
			Width = ex.X;
			Height = ex.Y;
		}
		#endregion // Ctor

		#region Overrides
		readonly bool IEquatable<RectF>.Equals(RectF other) => other == this;

		public readonly override bool Equals(object? obj) => (obj is RectF r) && (r == this);

		public readonly override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

		public readonly override string ToString() => $"{{{X} {Y} {Width} {Height}}}";
		#endregion // Overrides

		#region Creation
		/// <summary>
		/// Calculates the minimal rectangular area that completely contains both input rectangles.
		/// </summary>
		/// <param name="r1">The first rectangle.</param>
		/// <param name="r2">The second rectangle.</param>
		/// <returns>The output union rectangle.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static RectF Union(in RectF r1, in RectF r2)
		{
			Union(r1, r2, out var o);
			return o;
		}

		/// <summary>
		/// Calculates the minimal rectangular area that completely contains both input rectangles.
		/// </summary>
		/// <param name="r1">The first rectangle.</param>
		/// <param name="r2">The second rectangle.</param>
		/// <param name="o">The output union rectangle.</param>
		public static void Union(in RectF r1, in RectF r2, out RectF o)
		{
			o.X = Math.Min(r1.X, r2.X);
			o.Y = Math.Min(r1.Y, r2.Y);
			o.Width = Math.Max(r1.X + r1.Width, r2.X + r2.Width) - o.X;
			o.Height = Math.Max(r1.Y + r1.Height, r2.Y + r2.Height) - o.Y;
		}

		/// <summary>
		/// Calculates the overlap between the two rectangular areas, if any.
		/// </summary>
		/// <param name="r1">The first rectangle.</param>
		/// <param name="r2">The second rectangle.</param>
		/// <returns>The overlap area, set to <see cref="Empty"/> if there is no overlap.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static RectF Intersect(in RectF r1, in RectF r2)
		{
			Intersect(r1, r2, out var o);
			return o;
		}

		/// <summary>
		/// Calculates the overlap between the two rectangular areas, if any.
		/// </summary>
		/// <param name="r1">The first rectangle.</param>
		/// <param name="r2">The second rectangle.</param>
		/// <param name="o">The overlap area, set to <see cref="Empty"/> if there is no overlap.</param>
		public static void Intersect(in RectF r1, in RectF r2, out RectF o)
		{
			if (r1.Intersects(r2)) {
				o.X = Math.Max(r1.X, r2.X);
				o.Y = Math.Max(r1.Y, r2.Y);
				o.Width = Math.Min(r1.X + r1.Width, r2.X + r2.Width) - o.X;
				o.Height = Math.Min(r1.Y + r1.Height, r2.Y + r2.Height) - o.Y;
			}
			else
				o = RectF.Empty;
		}

		/// <summary>
		/// Constructs the minimal rectangular area that encompasses both passed points as corners.
		/// </summary>
		/// <param name="p1">The first point to contain.</param>
		/// <param name="p2">The second point to contain.</param>
		/// <returns>The output area.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static RectF FromCorners(in Vec2 p1, in Vec2 p2)
		{
			FromCorners(p1, p2, out var o);
			return o;
		}

		/// <summary>
		/// Constructs the minimal rectangular area that encompasses both passed points as corners.
		/// </summary>
		/// <param name="p1">The first point to contain.</param>
		/// <param name="p2">The second point to contain.</param>
		/// <param name="o">The output area.</param>
		public static void FromCorners(in Vec2 p1, in Vec2 p2, out RectF o)
		{
			o.X = Math.Min(p1.X, p2.X);
			o.Y = Math.Min(p1.Y, p2.Y);
			o.Width = Math.Max(p1.X, p2.X) - o.X;
			o.Height = Math.Max(p1.Y, p2.Y) - o.Y;
		}

		/// <summary>
		/// Constructs the minimal rectangular area that encompasses all of the passed points.
		/// </summary>
		/// <param name="pts">The collection of points to encompass.</param>
		/// <returns>The output area.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static RectF FromPoints(params Vec2[] pts)
		{
			FromPoints(out var o, pts);
			return o;
		}

		/// <summary>
		/// Constructs the minimal rectangular area that encompasses all of the passed points.
		/// </summary>
		/// <param name="o">The output area.</param>
		/// <param name="pts">The collection of points to encompass.</param>
		public static void FromPoints(out RectF o, params Vec2[] pts)
		{
			float minx = Single.MaxValue, miny = Single.MaxValue;
			float maxx = Single.MinValue, maxy = Single.MinValue;
			foreach (var p in pts) {
				minx = (p.X < minx) ? p.X : minx;
				miny = (p.Y < miny) ? p.Y : miny;
				maxx = (p.X > maxx) ? p.X : maxx;
				maxy = (p.Y > maxy) ? p.Y : maxy;
			}
			o = new RectF(minx, miny, maxx - minx, maxy - miny);
		}
		#endregion // Creation

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in RectF l, in RectF r) =>
			(l.X == r.X) && (l.Y == r.Y) && (l.Width == r.Width) && (l.Height == r.Height);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in RectF l, in RectF r) =>
			(l.X != r.X) || (l.Y != r.Y) || (l.Width != r.Width) || (l.Height != r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator RectF (in Rect r) => new RectF(r.X, r.Y, r.Width, r.Height);
		#endregion // Operators

		#region Tuples
		public readonly void Deconstruct(out float x, out float y, out float w, out float h)
		{
			x = X;
			y = Y;
			w = Width;
			h = Height;
		}

		public readonly void Deconstruct(out Vec2 pos, out Vec2 ext)
		{
			pos.X = X;
			pos.Y = Y;
			ext.X = Width;
			ext.Y = Height;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator RectF (in (float x, float y, float w, float h) tup) =>
			new RectF(tup.x, tup.y, tup.w, tup.h);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator RectF (in (Vec2 pos, Vec2 ext) tup) =>
			new RectF(tup.pos, tup.ext);
		#endregion // Tuples
	}
}
