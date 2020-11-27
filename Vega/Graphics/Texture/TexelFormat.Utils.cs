/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Contains utility functionality for <see cref="TexelFormat"/> values.
	/// </summary>
	public static class TexelFormatUtils
	{
		private static readonly Dictionary<TexelFormat, (uint s, uint c)> FORMAT_DATA = new() {
			{ TexelFormat.UNorm,       (1, 1) }, { TexelFormat.UNorm2,   (2, 2) }, { TexelFormat.UNorm4, (4, 4) },
			{ TexelFormat.UNormBgra,   (4, 4) },
			{ TexelFormat.UByte,       (1, 1) }, { TexelFormat.UByte2,   (2, 2) }, { TexelFormat.UByte4,    (4, 4) },
			{ TexelFormat.Byte,        (1, 1) }, { TexelFormat.Byte2,    (2, 2) }, { TexelFormat.Byte4,     (4, 4) },
			{ TexelFormat.UShort,      (2, 1) }, { TexelFormat.UShort2,  (4, 2) }, { TexelFormat.UShort4,   (8, 4) },
			{ TexelFormat.Short,       (2, 1) }, { TexelFormat.Short2,   (4, 2) }, { TexelFormat.Short4,    (8, 4) },
			{ TexelFormat.UInt,        (4, 1) }, { TexelFormat.UInt2,    (8, 2) }, { TexelFormat.UInt4,    (16, 4) },
			{ TexelFormat.Int,         (4, 1) }, { TexelFormat.Int2,     (8, 2) }, { TexelFormat.Int4,     (16, 4) },
			{ TexelFormat.Float,       (4, 1) }, { TexelFormat.Float2,   (8, 2) }, { TexelFormat.Float4,   (16, 4) },
			{ TexelFormat.Argb1555,    (2, 4) }, { TexelFormat.Bgra5551, (2, 4) }, { TexelFormat.Rgba5551,  (2, 4) },
			{ TexelFormat.Argb2101010, (4, 4) },
			{ TexelFormat.Rgb565,      (2, 3) }, { TexelFormat.Bgr565,   (2, 3) },
			{ TexelFormat.Bgra4444,    (2, 4) }, { TexelFormat.Rgba4444, (2, 4) },
			{ TexelFormat.Depth16,     (2, 1) }, { TexelFormat.Depth32,  (4, 1) }, { TexelFormat.Depth24Stencil8, (4, 2) },
		};

		/// <summary>
		/// Gets if the format is a color format.
		/// </summary>
		/// <param name="format">The format to check.</param>
		public static bool IsColorFormat(this TexelFormat format) =>
			(format != TexelFormat.Depth16) && (format != TexelFormat.Depth32) && 
			(format != TexelFormat.Depth24Stencil8);

		/// <summary>
		/// Gets if the format is a depth or depth/stencil format.
		/// </summary>
		/// <param name="format">The format to check.</param>
		public static bool IsDepthFormat(this TexelFormat format) =>
			(format == TexelFormat.Depth16) || (format == TexelFormat.Depth32) || 
			(format == TexelFormat.Depth24Stencil8);

		/// <summary>
		/// Gets if the format has a stencil component.
		/// </summary>
		/// <param name="format">The format to check.</param>
		public static bool HasStencilComponent(this TexelFormat format) => format == TexelFormat.Depth24Stencil8;

		/// <summary>
		/// Gets the size of a single texel for the given format, in bytes.
		/// </summary>
		/// <param name="format">The format to get the size for.</param>
		public static uint GetSize(this TexelFormat format) => FORMAT_DATA[format].s;

		/// <summary>
		/// Gets the number of components per texel for the format.
		/// </summary>
		/// <param name="format">The format to get the component count for.</param>
		public static uint GetComponentCount(this TexelFormat format) => FORMAT_DATA[format].c;

		/// <summary>
		/// Gets if the format is a "packed" format, where components do not fall on byte boundaries.
		/// </summary>
		/// <param name="format">The format to check.</param>
		public static bool IsPacked(this TexelFormat format) =>
			(format == TexelFormat.Argb1555) || (format == TexelFormat.Bgra5551) || (format == TexelFormat.Rgba5551) ||
			(format == TexelFormat.Argb2101010) ||
			(format == TexelFormat.Rgb565) || (format == TexelFormat.Bgr565) ||
			(format == TexelFormat.Bgra4444) || (format == TexelFormat.Rgba4444);

		/// <summary>
		/// Gets if the format is valid for use as an input attachment.
		/// </summary>
		/// <param name="format">The format to check.</param>
		public static bool IsValidAsInput(this TexelFormat format) =>
			(format != TexelFormat.Bgr565) && (format != TexelFormat.Rgb565);

		// Gets the vulkan aspect flags for the format
		internal static VkImageAspectFlags GetAspectFlags(this TexelFormat format) => format switch {
			TexelFormat.Depth16 => VkImageAspectFlags.Depth,
			TexelFormat.Depth32 => VkImageAspectFlags.Depth,
			TexelFormat.Depth24Stencil8 => VkImageAspectFlags.Depth | VkImageAspectFlags.Stencil,
			_ => VkImageAspectFlags.Color
		};
	}
}
