/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.InteropServices;

namespace Vega.Render
{
	/// <summary>
	/// Constants used for pipeline <see cref="BlendFactor"/> values that reference constant values.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct BlendConstants : IEquatable<BlendConstants>
	{
		#region Fields
		/// <summary>
		/// The red channel blend constant.
		/// </summary>
		public float R;
		/// <summary>
		/// The green channel blend constant.
		/// </summary>
		public float G;
		/// <summary>
		/// The blue channel blend constant.
		/// </summary>
		public float B;
		/// <summary>
		/// The alpha channel blend constant.
		/// </summary>
		public float A;
		#endregion // Fields

		/// <summary>
		/// Create blend constants with all channels the same value.
		/// </summary>
		/// <param name="f"></param>
		public BlendConstants(float f) => (R, G, B, A) = (f, f, f, f);
		/// <summary>
		/// Create blend constants with identical color channels, and a separate alpha channel/
		/// </summary>
		public BlendConstants(float f, float a) => (R, G, B, A) = (f, f, f, a);
		/// <summary>
		/// Create blend constants with all specified values.
		/// </summary>
		public BlendConstants(float r, float g, float b, float a = 1) => (R, G, B, A) = (r, g, b, a);
		/// <summary>
		/// Create blend constants from a color value.
		/// </summary>
		public BlendConstants(in Color c) => (R, G, B, A) = (c.RFloat, c.GFloat, c.BFloat, c.AFloat);

		#region Overrides
		public readonly override int GetHashCode() =>
			R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode() ^ A.GetHashCode();

		public readonly override string ToString() => $"[{R}:{G}:{B}:{A}]";

		public readonly override bool Equals(object? obj) => (obj is BlendConstants bc) && (bc == this);

		readonly bool IEquatable<BlendConstants>.Equals(BlendConstants other) => (other == this);
		#endregion // Overrides

		#region Operators
		public static bool operator == (in BlendConstants l, in BlendConstants r) =>
			(l.R == r.R) && (l.G == r.G) && (l.B == r.B) && (l.A == r.A);

		public static bool operator != (in BlendConstants l, in BlendConstants r) =>
			(l.R != r.R) || (l.G != r.G) || (l.B != r.B) || (l.A != r.A);

		public static implicit operator BlendConstants (in Color c) => new(c);
		#endregion // Operators
	}
}
