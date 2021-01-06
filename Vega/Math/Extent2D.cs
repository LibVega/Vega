/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Vega
{
	/// <summary>
	/// Describes a rectangular area with integer dimensions.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size=2*sizeof(uint))]
	public struct Extent2D : IEquatable<Extent2D>
	{
		/// <summary>
		/// An area of zero dimension.
		/// </summary>
		public static readonly Extent2D Zero = new(0, 0);

		#region Fields
		/// <summary>
		/// The width of the area (x-axis dimension).
		/// </summary>
		[FieldOffset(0)]
		public uint Width;
		/// <summary>
		/// The height of the area (y-axis dimension).
		/// </summary>
		[FieldOffset(sizeof(uint))]
		public uint Height;

		/// <summary>
		/// The total area of the described dimensions.
		/// </summary>
		public readonly uint Area => Width * Height;
		#endregion // Fields

		/// <summary>
		/// Constructs a new size.
		/// </summary>
		/// <param name="w">The width of the new area.</param>
		/// <param name="h">The height of the new area.</param>
		public Extent2D(uint w, uint h)
		{
			Width = w;
			Height = h;
		}

		#region Overrides
		public readonly override bool Equals(object? obj) => (obj is Extent2D e) && (e == this);

		public readonly override int GetHashCode() => HashCode.Combine(Width, Height);

		public readonly override string ToString() => $"{{{Width} {Height}}}";

		readonly bool IEquatable<Extent2D>.Equals(Extent2D other) => other == this;
		#endregion // Overrides

		#region Standard Math
		/// <summary>
		/// Finds the component-wise minimum of the two extents.
		/// </summary>
		/// <param name="l">The first extent.</param>
		/// <param name="r">The second extent.</param>
		/// <returns>The component-wise mimimum.</returns>
		public static Extent2D Min(in Extent2D l, in Extent2D r)
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
		public static void Min(in Extent2D l, in Extent2D r, out Extent2D o) =>
			o = new Extent2D(Math.Min(l.Width, r.Width), Math.Min(l.Height, r.Height));

		/// <summary>
		/// Finds the component-wise maximum of the two extents.
		/// </summary>
		/// <param name="l">The first extent.</param>
		/// <param name="r">The second extent.</param>
		/// <returns>The component-wise maximum.</returns>
		public static Extent2D Max(in Extent2D l, in Extent2D r)
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
		public static void Max(in Extent2D l, in Extent2D r, out Extent2D o) =>
			o = new Extent2D(Math.Max(l.Width, r.Width), Math.Max(l.Height, r.Height));

		/// <summary>
		/// Component-wise clamp the extent between two limiting extents.
		/// </summary>
		/// <param name="e">The extent to clamp.</param>
		/// <param name="min">The minimum extent.</param>
		/// <param name="max">The maximum extent.</param>
		/// <returns>The component-wise clamp.</returns>
		public static Extent2D Clamp(in Extent2D e, in Extent2D min, in Extent2D max)
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
		public static void Clamp(in Extent2D e, in Extent2D min, in Extent2D max, out Extent2D o) =>
			o = new Extent2D(Math.Clamp(e.Width, min.Width, max.Width), Math.Clamp(e.Height, min.Height, max.Height));
		#endregion // Standard Math

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Extent2D l, in Extent2D r) => (l.Width == r.Width) && (l.Height == r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Extent2D l, in Extent2D r) => (l.Width != r.Width) || (l.Height != r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extent2D operator * (in Extent2D l, uint r) => new Extent2D(l.Width * r, l.Height * r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extent2D operator * (uint l, in Extent2D r) => new Extent2D(l * r.Width, l * r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extent2D operator / (in Extent2D l, uint r) => new Extent2D(l.Width / r, l.Height / r);
		#endregion // Operators

		#region Tuples
		public readonly void Deconstruct(out uint w, out uint h)
		{
			w = Width;
			h = Height;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Extent2D (in (uint w, uint h) tup) =>
			new Extent2D(tup.w, tup.h);
		#endregion // Tuples
	}
}
