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
	/// Contains additional math utilities not found in the standard library math classes.
	/// </summary>
	public static class MathHelper
	{
		// Limit for absolute/relative epsilon switch for NearlyEqualFast() functions.
		private const float ABS_REL_EPS_LIMIT = 0.015625f; // 1/64
		/// <summary>
		/// Bit mask for the sign bit for 32-bit integers.
		/// </summary>
		public const uint INT_SIGN_BIT = 0x80000000;
		/// <summary>
		/// Bit mask for the sign bit for 64-bit integers.
		/// </summary>
		public const ulong LONG_SIGN_BIT = 0x8000000000000000;
		/// <summary>
		/// Default value for <c>float</c> epsilon comparisons.
		/// </summary>
		public const float FLOAT_EPSILON = Single.Epsilon * 10;
		/// <summary>
		/// Default value for <c>double</c> epsilon comparisons.
		/// </summary>
		public const double DOUBLE_EPSILON = Double.Epsilon * 10;

		#region Floating Point
		/// <summary>
		/// Gets the ULP representation of the floating point value.
		/// </summary>
		/// <param name="f">The floating point value to convert.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe int AsULP(float f)
		{
			int ulp = *(int*)&f;
			return ulp < 0 ? (int)(INT_SIGN_BIT - (uint)ulp) : ulp;
		}
		/// <inheritdoc cref="AsULP(float)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe long AsULP(double f)
		{
			long ulp = *(long*)&f;
			return ulp < 0 ? (long)(LONG_SIGN_BIT - (ulong)ulp) : ulp;
		}

		/// <summary>
		/// Gets the units of least precision (ULP) distance between the two values.
		/// </summary>
		/// <param name="a">The first value.</param>
		/// <param name="b">The second value.</param>
		/// <returns>ULP distance, or <see cref="Int64.MaxValue"/> if either value is NaN or Inf.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static long ULPDistance(float a, float b) 
			=> (Single.IsNaN(a) || Single.IsNaN(b) || Single.IsInfinity(a) || Single.IsInfinity(b)) 
				? Int64.MaxValue : AsULP(a) - AsULP(b);
		/// <inheritdoc cref="ULPDistance(float, float)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static long ULPDistance(double a, double b)
			=> (Double.IsNaN(a) || Double.IsNaN(b) || Double.IsInfinity(a) || Double.IsInfinity(b))
				? Int64.MaxValue : AsULP(a) - AsULP(b);

		/// <summary>
		/// Calculates if the two floating point values are nearly equal, using epsilon and ULP differences.
		/// </summary>
		/// <param name="a">The first value to compare.</param>
		/// <param name="b">The second value to compare.</param>
		/// <param name="eps">The absolute epsilon used to compare values close to zero.</param>
		/// <param name="ulpEps">The ULP epsilon used to compare values not close to zero.</param>
		public static bool NearlyEqual(float a, float b, float eps = FLOAT_EPSILON, ulong ulpEps = 10)
		{
			float diff = MathF.Abs(a - b);
			if (diff <= eps) return true;
			if ((a < 0) != (b < 0)) return false;
			return (ulong)Math.Abs(ULPDistance(a, b)) <= ulpEps;
		}
		/// <inheritdoc cref="NearlyEqual(float, float, float, ulong)"/>
		public static bool NearlyEqual(double a, double b, double eps = DOUBLE_EPSILON, ulong ulpEps = 10)
		{
			double diff = Math.Abs(a - b);
			if (diff <= eps) return true;
			if ((a < 0) != (b < 0)) return false;
			return (ulong)Math.Abs(ULPDistance(a, b)) <= ulpEps;
		}

		/// <summary>
		/// A less-accurate, but faster way to compare near-equality with floating point numbers.
		/// </summary>
		/// <param name="a">The first value to compare.</param>
		/// <param name="b">The second value to compare.</param>
		/// <param name="eps">The epsilon used absolutely for small numbers, and relatively for large numbers.</param>
		public static bool NearlyEqualFast(float a, float b, float eps = FLOAT_EPSILON)
		{
			float diff = MathF.Abs(a - b);
			return MathF.Max(MathF.Abs(a), MathF.Abs(b)) <= ABS_REL_EPS_LIMIT ? diff <= eps : diff <= (eps * diff);
		}
		/// <inheritdoc cref="NearlyEqualFast(float, float, float)"/>
		public static bool NearlyEqualFast(double a, double b, double eps = DOUBLE_EPSILON)
		{
			double diff = Math.Abs(a - b);
			return Math.Max(Math.Abs(a), Math.Abs(b)) <= ABS_REL_EPS_LIMIT ? diff <= eps : diff <= (eps * diff);
		}
		#endregion // Floating Point

		#region Powers
		/// <summary>
		/// Checks if the integer value is positive, non-zero, and a power of two.
		/// </summary>
		/// <param name="l">The value to check.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPowerOfTwo(long l) => (l >= 1) && (l & (l - 1)) == 0;
		/// <summary>
		/// Checks if the integer value is non-zero and a power of two.
		/// </summary>
		/// <param name="l">The value to check.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPowerOfTwo(ulong l) => (l != 0) && (l & (l - 1)) == 0;

		/// <summary>
		/// Checks if the first argument is an integer power of the second.
		/// </summary>
		/// <param name="l">The value to check the power of.</param>
		/// <param name="power">The power to check for.</param>
		public static bool IsPower(long l, ulong power)
		{
			if (power == 0 || (ulong)l < power) return false;
			if (power == 1) return l == 1;
			if (power == 2) return IsPowerOfTwo((ulong)l);

			double log = Math.Log10((ulong)l) / Math.Log10(power); // No fractional part for perfect powers
			return ULPDistance(log, Math.Floor(log)) <= 1;
		}
		/// <inheritdoc cref="IsPower(long, ulong)"/>
		public static bool IsPower(ulong l, ulong power)
		{
			if (power == 0 || l < power) return false;
			if (power == 1) return l == 1;
			if (power == 2) return IsPowerOfTwo(l);

			double log = Math.Log10(l) / Math.Log10(power); // No fractional part for perfect powers
			return ULPDistance(log, Math.Floor(log)) <= 1;
		}
		#endregion // Powers
	}
}
