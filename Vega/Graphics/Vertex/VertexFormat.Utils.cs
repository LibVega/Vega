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
	/// Contains utility functionality for <see cref="VertexFormat"/> values.
	/// </summary>
	public static class VertexFormatUtils
	{
		private static readonly Dictionary<VertexFormat, (uint cs, uint c)> FORMAT_DATA = new() {
			//{ VertexFormat.Byte,    (1, 1) }, { VertexFormat.Byte2,   (1, 2) }, { VertexFormat.Byte4,   (1, 4) },
			//{ VertexFormat.SByte,   (1, 1) }, { VertexFormat.SByte2,  (1, 2) }, { VertexFormat.SByte4,  (1, 4) },
			//{ VertexFormat.UShort,  (2, 1) }, { VertexFormat.UShort2, (2, 2) }, { VertexFormat.UShort4, (2, 4) },
			//{ VertexFormat.Short,   (2, 1) }, { VertexFormat.Short2,  (2, 2) }, { VertexFormat.Short4,  (2, 4) },
			{ VertexFormat.UInt,    (4, 1) }, { VertexFormat.UInt2,   (4, 2) }, { VertexFormat.UInt3,   (4, 3) }, 
			{ VertexFormat.UInt4,   (4, 4) },
			{ VertexFormat.Int,     (4, 1) }, { VertexFormat.Int2,    (4, 2) }, { VertexFormat.Int3,    (4, 3) },
			{ VertexFormat.Int4,    (4, 4) },
			//{ VertexFormat.ULong,   (8, 1) }, { VertexFormat.ULong2,  (8, 2) }, { VertexFormat.ULong3,  (8, 3) },
			//{ VertexFormat.ULong4,  (8, 4) },
			//{ VertexFormat.Long,    (8, 1) }, { VertexFormat.Long2,   (8, 2) }, { VertexFormat.Long3,   (8, 3) },
			//{ VertexFormat.Long4,   (8, 4) },

			//{ VertexFormat.Half,    (2, 1) }, { VertexFormat.Half2,   (2, 2) }, { VertexFormat.Half4,   (2, 4) },
			{ VertexFormat.Float,   (4, 1) }, { VertexFormat.Float2,  (4, 2) }, { VertexFormat.Float3,  (4, 3) },
			{ VertexFormat.Float4,  (4, 4) },
			//{ VertexFormat.Double,  (8, 1) }, { VertexFormat.Double2, (8, 2) }, { VertexFormat.Double3, (8, 3) },
			//{ VertexFormat.Double4, (8, 4) },

			{ VertexFormat.FloatUnorm8,  (1, 1) }, { VertexFormat.Float2Unorm8,  (1, 2) }, { VertexFormat.Float4Unorm8,  (1, 4) },
			{ VertexFormat.FloatUnorm16, (2, 1) }, { VertexFormat.Float2Unorm16, (2, 2) }, { VertexFormat.Float4Unorm16, (2, 4) },

			{ VertexFormat.Float2x2, (4, 4) }, { VertexFormat.Float2x3,  (4, 6) }, { VertexFormat.Float2x4,  (4, 8) },
			{ VertexFormat.Float3x2, (4, 6) }, { VertexFormat.Float3x3,  (4, 9) }, { VertexFormat.Float3x4, (4, 12) },
			{ VertexFormat.Float4x2, (4, 8) }, { VertexFormat.Float4x3, (4, 12) }, { VertexFormat.Float4x4, (4, 16) }
		};

		/// <summary>
		/// Gets the size of the given format, in bytes.
		/// </summary>
		/// <param name="format">The format to get the size for.</param>
		public static uint GetSize(this VertexFormat format)
		{
			var info = FORMAT_DATA[format];
			return info.cs * info.c;
		}

		/// <summary>
		/// Gets the number of vector components for the the format.
		/// </summary>
		/// <param name="format">The format to get the component count for.</param>
		public static uint GetComponentCount(this VertexFormat format) => FORMAT_DATA[format].c;

		/// <summary>
		/// Gets the size of the format scalar component, in bytes.
		/// </summary>
		/// <param name="format">The format to get the component size for.</param>
		public static uint GetComponentSize(this VertexFormat format) => FORMAT_DATA[format].cs;

		/// <summary>
		/// Gets if the format represents a scalar type.
		/// </summary>
		/// <param name="format">The format to check.</param>
		public static bool IsScalarType(this VertexFormat format) => FORMAT_DATA[format].c == 1;

		/// <summary>
		/// Gets if the format represents a vector type.
		/// </summary>
		/// <param name="format">The format to check.</param>
		public static bool IsVectorType(this VertexFormat format) => 
			(FORMAT_DATA[format].c != 1) && !IsMatrixType(format);

		/// <summary>
		/// Gets if the format represents a matrix type.
		/// </summary>
		/// <param name="format">The format to check.</param>
		public static bool IsMatrixType(this VertexFormat format) =>
			(format >= VertexFormat.Float2x2) && (format <= VertexFormat.Float4x4);

		/// <summary>
		/// Gets the number of vertex attribute binding slots the format takes up.
		/// </summary>
		/// <param name="format">The format to get the slot count for.</param>
		public static uint GetBindingCount(this VertexFormat format)
		{
			var size = GetSize(format);
			var baseCount = (size > 16) ? 2u : 1u;
			return IsMatrixType(format) ? GetDimensions(format).col * baseCount : baseCount;
		}

		/// <summary>
		/// Gets the dimensionality of the vertex format, as a scalar, vertex size, or matrix size.
		/// </summary>
		/// <param name="format">The format to get the dimensionality of.</param>
		/// <returns>
		/// A (row, column) pair. Matrix of size MxN is (N, M). Vector of size N is (N, 1). Scalars are (1, 1).
		/// </returns>
		public static (uint row, uint col) GetDimensions(this VertexFormat format)
		{
			if (IsMatrixType(format)) {
				return format switch {
					VertexFormat.Float2x2 => (2, 2),
					VertexFormat.Float2x3 => (3, 2),
					VertexFormat.Float2x4 => (4, 2),
					VertexFormat.Float3x2 => (2, 3),
					VertexFormat.Float3x3 => (3, 3),
					VertexFormat.Float3x4 => (4, 3),
					VertexFormat.Float4x2 => (2, 4),
					VertexFormat.Float4x3 => (3, 4),
					VertexFormat.Float4x4 => (4, 4),
					_ => (0, 0) // Never reached
				};
			}
			else return (FORMAT_DATA[format].c, 1);
		}

		// Get the underlying Vulkan format
		internal static VkFormat GetVulkanFormat(this VertexFormat format)
		{
			if (IsMatrixType(format)) {
				return format switch {
					VertexFormat.Float2x2 => VkFormat.R32g32Sfloat,
					VertexFormat.Float2x3 => VkFormat.R32g32b32Sfloat,
					VertexFormat.Float2x4 => VkFormat.R32g32b32a32Sfloat,
					VertexFormat.Float3x2 => VkFormat.R32g32Sfloat,
					VertexFormat.Float3x3 => VkFormat.R32g32b32Sfloat,
					VertexFormat.Float3x4 => VkFormat.R32g32b32a32Sfloat,
					VertexFormat.Float4x2 => VkFormat.R32g32Sfloat,
					VertexFormat.Float4x3 => VkFormat.R32g32b32Sfloat,
					VertexFormat.Float4x4 => VkFormat.R32g32b32a32Sfloat,
					_ => default // Never reached
				};
			}
			else return (VkFormat)format;
		}
	}
}
