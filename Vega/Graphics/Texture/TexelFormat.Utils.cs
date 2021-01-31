/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
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
		private static readonly Dictionary<TexelFormat, (VSL.TexelType typ, byte sz, byte cnt)> FORMAT_DATA = new() {
			{ TexelFormat.UNorm,  (VSL.TexelType.UNorm, 1, 1) }, { TexelFormat.UNorm2, (VSL.TexelType.UNorm, 2, 2) },
			{ TexelFormat.UNorm4, (VSL.TexelType.UNorm, 4, 4) },
			{ TexelFormat.UNormBgra, (VSL.TexelType.UNorm, 4, 4) },
			{ TexelFormat.SNorm,  (VSL.TexelType.SNorm, 1, 1) }, { TexelFormat.SNorm2, (VSL.TexelType.SNorm, 2, 2) },
			{ TexelFormat.SNorm4, (VSL.TexelType.SNorm, 4, 4) },
			{ TexelFormat.U16Norm,  (VSL.TexelType.UNorm, 2, 1) }, { TexelFormat.U16Norm2, (VSL.TexelType.UNorm, 4, 2) },
			{ TexelFormat.U16Norm4, (VSL.TexelType.UNorm, 8, 4) },
			{ TexelFormat.S16Norm,  (VSL.TexelType.SNorm, 2, 1) }, { TexelFormat.S16Norm2, (VSL.TexelType.SNorm, 4, 2) },
			{ TexelFormat.S16Norm4, (VSL.TexelType.SNorm, 8, 4) },
			{ TexelFormat.UByte,  (VSL.TexelType.Unsigned, 1, 1) }, { TexelFormat.UByte2, (VSL.TexelType.Unsigned, 2, 2) },
			{ TexelFormat.UByte4, (VSL.TexelType.Unsigned, 4, 4) },
			{ TexelFormat.Byte,  (VSL.TexelType.Signed, 1, 1) }, { TexelFormat.Byte2, (VSL.TexelType.Signed, 2, 2) },
			{ TexelFormat.Byte4, (VSL.TexelType.Signed, 4, 4) },
			{ TexelFormat.UShort,  (VSL.TexelType.Unsigned, 2, 1) }, { TexelFormat.UShort2, (VSL.TexelType.Unsigned, 4, 2) },
			{ TexelFormat.UShort4, (VSL.TexelType.Unsigned, 8, 4) },
			{ TexelFormat.Short,  (VSL.TexelType.Signed, 2, 1) }, { TexelFormat.Short2, (VSL.TexelType.Signed, 4, 2) },
			{ TexelFormat.Short4, (VSL.TexelType.Signed, 8, 4) },
			{ TexelFormat.UInt,  (VSL.TexelType.Unsigned, 4, 1) }, { TexelFormat.UInt2, (VSL.TexelType.Unsigned, 8, 2) },
			{ TexelFormat.UInt4, (VSL.TexelType.Unsigned, 16, 4) },
			{ TexelFormat.Int,  (VSL.TexelType.Signed, 4, 1) }, { TexelFormat.Int2, (VSL.TexelType.Signed, 8, 2) },
			{ TexelFormat.Int4, (VSL.TexelType.Signed, 16, 4) },
			{ TexelFormat.Float,  (VSL.TexelType.Float, 4, 1) }, { TexelFormat.Float2, (VSL.TexelType.Float, 8, 2) },
			{ TexelFormat.Float4, (VSL.TexelType.Float, 16, 4) },
			{ TexelFormat.Argb1555, (VSL.TexelType.UNorm, 2, 4) }, { TexelFormat.Bgra5551, (VSL.TexelType.UNorm, 2, 4) },
			{ TexelFormat.Rgba5551, (VSL.TexelType.UNorm, 2, 4) },
			{ TexelFormat.Argb2101010, (VSL.TexelType.UNorm, 4, 4) },
			{ TexelFormat.Rgb565, (VSL.TexelType.UNorm, 2, 3) }, { TexelFormat.Bgr565, (VSL.TexelType.UNorm, 2, 3) },
			{ TexelFormat.Bgra4444, (VSL.TexelType.UNorm, 2, 4) }, { TexelFormat.Rgba4444, (VSL.TexelType.UNorm, 2, 4) },
			// Texel type is less important for depth/stencil formats
			{ TexelFormat.Depth16, (VSL.TexelType.Float, 2, 1) }, { TexelFormat.Depth32, (VSL.TexelType.Float, 4, 1) },
			{ TexelFormat.Depth24Stencil8, (VSL.TexelType.Float, 4, 2) }
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
		public static uint GetSize(this TexelFormat format) => FORMAT_DATA[format].sz;

		/// <summary>
		/// Gets the number of components per texel for the format.
		/// </summary>
		/// <param name="format">The format to get the component count for.</param>
		public static uint GetComponentCount(this TexelFormat format) => FORMAT_DATA[format].cnt;

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

		/// <summary>
		/// Gets if the source format is implicitly convertible to the destination, such as in color attachment stores.
		/// </summary>
		/// <param name="srcFormat">The source data format.</param>
		/// <param name="dstFormat">The destination data format.</param>
		public static bool IsConvertible(this TexelFormat srcFormat, TexelFormat dstFormat)
		{
			var sdata = FORMAT_DATA[srcFormat];
			var ddata = FORMAT_DATA[dstFormat];
			var sbase = sdata.typ switch { 
				VSL.TexelType.Signed => 0,
				VSL.TexelType.Unsigned => 1,
				_ => 2
			};
			var dbase = ddata.typ switch {
				VSL.TexelType.Signed => 0,
				VSL.TexelType.Unsigned => 1,
				_ => 2
			};

			return (sbase == dbase) || (sdata.cnt == ddata.cnt);
		}

		// Gets the vulkan aspect flags for the format
		internal static VkImageAspectFlags GetAspectFlags(this TexelFormat format) => format switch {
			TexelFormat.Depth16 => VkImageAspectFlags.Depth,
			TexelFormat.Depth32 => VkImageAspectFlags.Depth,
			TexelFormat.Depth24Stencil8 => VkImageAspectFlags.Depth | VkImageAspectFlags.Stencil,
			_ => VkImageAspectFlags.Color
		};
	}
}
