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
	/// Describes a rectangular area with integer dimensions.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size=2*sizeof(uint))]
	public struct Extent2 : IEquatable<Extent2>
	{
		/// <summary>
		/// An area of zero dimension.
		/// </summary>
		public static readonly Extent2 Zero = new(0, 0);

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
		public Extent2(uint w, uint h)
		{
			Width = w;
			Height = h;
		}

		#region Overrides
		public readonly override bool Equals(object? obj) => (obj is Extent2 e) && (e == this);

		public readonly override int GetHashCode() => HashCode.Combine(Width, Height);

		public readonly override string ToString() => $"{{{Width} {Height}}}";

		readonly bool IEquatable<Extent2>.Equals(Extent2 other) => other == this;
		#endregion // Overrides

		#region Standard Math
		/// <summary>
		/// Finds the component-wise minimum of the two extents.
		/// </summary>
		/// <param name="l">The first extent.</param>
		/// <param name="r">The second extent.</param>
		/// <returns>The component-wise mimimum.</returns>
		public static Extent2 Min(in Extent2 l, in Extent2 r)
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
		public static void Min(in Extent2 l, in Extent2 r, out Extent2 o) =>
			o = new Extent2(Math.Min(l.Width, r.Width), Math.Min(l.Height, r.Height));

		/// <summary>
		/// Finds the component-wise maximum of the two extents.
		/// </summary>
		/// <param name="l">The first extent.</param>
		/// <param name="r">The second extent.</param>
		/// <returns>The component-wise maximum.</returns>
		public static Extent2 Max(in Extent2 l, in Extent2 r)
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
		public static void Max(in Extent2 l, in Extent2 r, out Extent2 o) =>
			o = new Extent2(Math.Max(l.Width, r.Width), Math.Max(l.Height, r.Height));

		/// <summary>
		/// Component-wise clamp the extent between two limiting extents.
		/// </summary>
		/// <param name="e">The extent to clamp.</param>
		/// <param name="min">The minimum extent.</param>
		/// <param name="max">The maximum extent.</param>
		/// <returns>The component-wise clamp.</returns>
		public static Extent2 Clamp(in Extent2 e, in Extent2 min, in Extent2 max)
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
		public static void Clamp(in Extent2 e, in Extent2 min, in Extent2 max, out Extent2 o) =>
			o = new Extent2(Math.Clamp(e.Width, min.Width, max.Width), Math.Clamp(e.Height, min.Height, max.Height));
		#endregion // Standard Math

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Extent2 l, in Extent2 r) => (l.Width == r.Width) && (l.Height == r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Extent2 l, in Extent2 r) => (l.Width != r.Width) || (l.Height != r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extent2 operator * (in Extent2 l, uint r) => new Extent2(l.Width * r, l.Height * r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extent2 operator * (uint l, in Extent2 r) => new Extent2(l * r.Width, l * r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extent2 operator / (in Extent2 l, uint r) => new Extent2(l.Width / r, l.Height / r);
		#endregion // Operators

		#region Tuples
		public readonly void Deconstruct(out uint w, out uint h)
		{
			w = Width;
			h = Height;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Extent2 (in (uint w, uint h) tup) =>
			new Extent2(tup.w, tup.h);
		#endregion // Tuples
	}
}
