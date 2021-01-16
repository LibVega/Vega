/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.InteropServices;

namespace Vega
{
	/// <summary>
	/// Represents a 32-bit RGBA color. This matches the <see cref="Graphics.TexelFormat.Color"/> format.
	/// <para>
	/// The red value is in the least significant bits, making the layout <c>0xAABBGGRR</c>.
	/// </para>
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 4)]
	public unsafe struct Color : IEquatable<Color>
	{
		private static readonly Random _Random = new();
		/// <summary>
		/// Returns a random opaque color.
		/// </summary>
		public static Color Random => new((uint)_Random.Next() | 0xFF000000);

		#region Fields
		// The backing integer value
		[FieldOffset(0)]
		private uint _value;

		/// <summary>
		/// The value of the red channel (range [0, 255]).
		/// </summary>
		[FieldOffset(0)]
		public byte R;
		/// <summary>
		/// The value of the green channel (range [0, 255]).
		/// </summary>
		[FieldOffset(1)]
		public byte G;
		/// <summary>
		/// The value of the blue channel (range [0, 255]).
		/// </summary>
		[FieldOffset(2)]
		public byte B;
		/// <summary>
		/// The value of the alpha channel (range [0, 255]).
		/// </summary>
		[FieldOffset(3)]
		public byte A;

		/// <summary>
		/// The normalized [0, 1] value of the red channel.
		/// </summary>
		public float RFloat
		{
			readonly get => R / 255f;
			set => R = (byte)(value * 255);
		}
		/// <summary>
		/// The normalized [0, 1] value of the green channel.
		/// </summary>
		public float GFloat
		{
			readonly get => G / 255f;
			set => G = (byte)(value * 255);
		}
		/// <summary>
		/// The normalized [0, 1] value of the blue channel.
		/// </summary>
		public float BFloat
		{
			readonly get => B / 255f;
			set => B = (byte)(value * 255);
		}
		/// <summary>
		/// The normalized [0, 1] value of the alpha channel.
		/// </summary>
		public float AFloat
		{
			readonly get => A / 255f;
			set => A = (byte)(value * 255);
		}

		/// <summary>
		/// Gets the inverse of the color.
		/// </summary>
		public readonly Color Inverse => new((byte)(255 - R), (byte)(255 - G), (byte)(255 - B), A);
		#endregion // Fields

		/// <summary>
		/// Create a color from a packed integer.
		/// </summary>
		public Color(uint packed) : this() => _value = packed;
		/// <summary>
		/// Create a color from integer color channel values.
		/// </summary>
		public Color(byte r, byte g, byte b, byte a = 255)
			: this()
		{
			R = r; G = g; B = b; A = a;
		}
		/// <summary>
		/// Create a color from floating point channel values.
		/// </summary>
		public Color(float r, float g, float b, float a = 1)
			: this()
		{
			RFloat = r; GFloat = g; BFloat = b; AFloat = a;
		}
		/// <summary>
		/// Create a grayscale color.
		/// </summary>
		public Color(byte val, byte a = 255) : this(val, val, val, a) { }
		/// <summary>
		/// Create a grayscale color.
		/// </summary>
		public Color(float val, float a = 1) : this(val, val, val, a) { }

		#region Overrides
		public readonly override string ToString() => $"[{R},{G},{B},{A}]";

		public readonly override int GetHashCode() => _value.GetHashCode();

		public readonly override bool Equals(object? obj) => (obj is Color c) && (c._value == _value);

		readonly bool IEquatable<Color>.Equals(Color other) => other._value == _value;
		#endregion // Overrides

		#region Operators
		public static bool operator == (Color l, Color r) => l._value == r._value;

		public static bool operator != (Color l, Color r) => l._value != r._value;

		public static explicit operator Vec3 (Color c) => new Vec3(c.RFloat, c.GFloat, c.BFloat);

		public static explicit operator Vec4 (Color c) => new Vec4(c.RFloat, c.GFloat, c.BFloat, c.AFloat);
		#endregion // Operators

		#region Constants
		// Standard colors
		public static readonly Color Black = new Color(0xFF000000);
		public static readonly Color TransparentBlack = new Color(0x00000000);
		public static readonly Color White = new Color(0xFFFFFFFF);
		public static readonly Color TransparentWhite = new Color(0x00FFFFFF);
		public static readonly Color Red = new Color(0xFF0000FF);
		public static readonly Color Green = new Color(0xFF00FF00);
		public static readonly Color Blue = new Color(0xFFFF0000);
		public static readonly Color Yellow = new Color(0xFF00FFFF);
		public static readonly Color Magenta = new Color(0xFFFF00FF);
		public static readonly Color Cyan = new Color(0xFFFFFF00);
		// Shades of gray
		public static readonly Color DarkGray = new Color(0xFF222222);
		public static readonly Color Gray = new Color(0xFF555555);
		public static readonly Color LightGray = new Color(0xFF999999);
		#endregion // Constants
	}
}
