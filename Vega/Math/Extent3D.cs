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
	/// Describes a rectangular volume with integer dimensions.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size=3*sizeof(uint))]
	public struct Extent3D : IEquatable<Extent3D>
	{
		/// <summary>
		/// An volume of zero dimension.
		/// </summary>
		public static readonly Extent3D Zero = new(0, 0, 0);

		#region Fields
		/// <summary>
		/// The width of the volume (x-axis dimension).
		/// </summary>
		[FieldOffset(0)]
		public uint Width;
		/// <summary>
		/// The height of the volume (y-axis dimension).
		/// </summary>
		[FieldOffset(sizeof(uint))]
		public uint Height;
		/// <summary>
		/// The depth of the volume (z-axis dimension).
		/// </summary>
		[FieldOffset(2 * sizeof(uint))]
		public uint Depth;

		/// <summary>
		/// The total volume of the described dimensions.
		/// </summary>
		public readonly uint Volume => Width * Height * Depth;
		#endregion // Fields

		/// <summary>
		/// Constructs a new size.
		/// </summary>
		/// <param name="w">The width of the new volume.</param>
		/// <param name="h">The height of the new volume.</param>
		/// <param name="d">The depth of the new volume.</param>
		public Extent3D(uint w, uint h, uint d)
		{
			Width = w;
			Height = h;
			Depth = d;
		}

		#region Overrides
		public readonly override bool Equals(object? obj) => (obj is Extent3D e) && (e == this);

		public readonly override int GetHashCode() => HashCode.Combine(Width, Height, Depth);

		public readonly override string ToString() => $"{{{Width} {Height} {Depth}}}";

		readonly bool IEquatable<Extent3D>.Equals(Extent3D other) => other == this;
		#endregion // Overrides

		#region Standard Math
		/// <summary>
		/// Finds the component-wise minimum of the two extents.
		/// </summary>
		/// <param name="l">The first extent.</param>
		/// <param name="r">The second extent.</param>
		/// <returns>The component-wise mimimum.</returns>
		public static Extent3D Min(in Extent3D l, in Extent3D r)
		{
			Min(l, r, out var o);
			return o;
		}

		/// <summary>
		/// Finds the component-wise minimum of the two extents.
		/// </summary>
		/// <param name="l">The first extent.</param>
		/// <param name="r">The second extent.</param>
		/// <param name="o">The output extent.</param>
		public static void Min(in Extent3D l, in Extent3D r, out Extent3D o) =>
			o = new Extent3D(Math.Min(l.Width, r.Width), Math.Min(l.Height, r.Height), Math.Min(l.Depth, r.Depth));

		/// <summary>
		/// Finds the component-wise maximum of the two extents.
		/// </summary>
		/// <param name="l">The first extent.</param>
		/// <param name="r">The second extent.</param>
		/// <returns>The component-wise maximum.</returns>
		public static Extent3D Max(in Extent3D l, in Extent3D r)
		{
			Max(l, r, out var o);
			return o;
		}

		/// <summary>
		/// Finds the component-wise maximum of the two extents.
		/// </summary>
		/// <param name="l">The first extent.</param>
		/// <param name="r">The second extent.</param>
		/// <param name="o">The output extent.</param>
		public static void Max(in Extent3D l, in Extent3D r, out Extent3D o) =>
			o = new Extent3D(Math.Max(l.Width, r.Width), Math.Max(l.Height, r.Height), Math.Max(l.Depth, r.Depth));

		/// <summary>
		/// Component-wise clamp the extent between two limiting extents.
		/// </summary>
		/// <param name="e">The extent to clamp.</param>
		/// <param name="min">The minimum extent.</param>
		/// <param name="max">The maximum extent.</param>
		/// <returns>The component-wise clamp.</returns>
		public static Extent3D Clamp(in Extent3D e, in Extent3D min, in Extent3D max)
		{
			Clamp(e, min, max, out var o);
			return o;
		}

		/// <summary>
		/// Component-wise clamp the extent between two limiting extents.
		/// </summary>
		/// <param name="e">The extent to clamp.</param>
		/// <param name="min">The minimum extent.</param>
		/// <param name="max">The maximum extent.</param>
		/// <param name="o">The component-wise clamp.</param>
		public static void Clamp(in Extent3D e, in Extent3D min, in Extent3D max, out Extent3D o) =>
			o = new Extent3D(Math.Clamp(e.Width, min.Width, max.Width), Math.Clamp(e.Height, min.Height, max.Height), Math.Clamp(e.Depth, min.Depth, max.Depth));
		#endregion // Standard Math

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Extent3D l, in Extent3D r) => 
			(l.Width == r.Width) && (l.Height == r.Height) && (l.Depth == r.Depth);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Extent3D l, in Extent3D r) => 
			(l.Width != r.Width) || (l.Height != r.Height) || (l.Depth != r.Depth);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extent3D operator * (in Extent3D l, uint r) => new Extent3D(l.Width * r, l.Height * r, l.Depth * r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extent3D operator * (uint l, in Extent3D r) => new Extent3D(l * r.Width, l * r.Height, l * r.Depth);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extent3D operator / (in Extent3D l, uint r) => new Extent3D(l.Width / r, l.Height / r, l.Depth / r);
		#endregion // Operators

		#region Tuples
		public readonly void Deconstruct(out uint w, out uint h, out uint d)
		{
			w = Width;
			h = Height;
			d = Depth;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Extent3D (in (uint w, uint h, uint d) tup) =>
			new Extent3D(tup.w, tup.h, tup.d);
		#endregion // Tuples
	}
}
