/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.InteropServices;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Represents a union of color and depth/stencil clear values, used to dictate how to clear <see cref="Renderer"/>
	/// attachments.
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public struct ClearValue : IEquatable<ClearValue>
	{
		#region Fields
		/// <summary>
		/// The clear values for float-type color formats.
		/// </summary>
		[FieldOffset(0)]
		public (float R, float G, float B, float A) ColorF;

		/// <summary>
		/// The clear values for signed integer-type color formats.
		/// </summary>
		[FieldOffset(0)]
		public (int R, int G, int B, int A) ColorI;
		
		/// <summary>
		/// The clear values for unsigned integer-type color formats.
		/// </summary>
		[FieldOffset(0)]
		public (uint R, uint G, uint B, uint A) ColorU;

		/// <summary>
		/// The clear values for depth and depth/stencil formats.
		/// </summary>
		[FieldOffset(0)]
		public (float D, uint S) DepthStencil;
		#endregion // Fields

		/// <summary>
		/// Create a new clear value for float-type color formats.
		/// </summary>
		public ClearValue(float r, float g, float b, float a) : this() => ColorF = (r, g, b, a);

		/// <summary>
		/// Create a new clear value for signed integer-type color formats.
		/// </summary>
		public ClearValue(int r, int g, int b, int a) : this() => ColorI = (r, g, b, a);

		/// <summary>
		/// Create a new clear value for unsigned integer-type color formats.
		/// </summary>
		public ClearValue(uint r, uint g, uint b, uint a) : this() => ColorU = (r, g, b, a);

		/// <summary>
		/// Create a new clear value for a depth or depth/stencil format.
		/// </summary>
		public ClearValue(float depth, uint stencil) : this() => DepthStencil = (depth, stencil);

		#region Overrides
		public readonly override int GetHashCode() => HashCode.Combine(ColorF.R, ColorF.G, ColorF.B, ColorF.A);

		public readonly override string ToString() => 
			$"[R:{ColorF.R} G:{ColorF.G} B:{ColorF.B} A:{ColorF.A} D:{DepthStencil.D} S:{DepthStencil.S}]";

		public readonly override bool Equals(object? obj) => (obj is ClearValue cv) && (ColorI == cv.ColorI);

		readonly bool IEquatable<ClearValue>.Equals(ClearValue other) => ColorI == other.ColorI;
		#endregion // Overrides

		internal VkClearValue ToVk() => new(new VkClearColorValue(ColorF.R, ColorF.G, ColorF.B, ColorF.A));

		#region Operators
		public static bool operator == (in ClearValue l, in ClearValue r) => l.ColorI == r.ColorI;
		public static bool operator != (in ClearValue l, in ClearValue r) => l.ColorI != r.ColorI;
		#endregion // Operators
	}
}
