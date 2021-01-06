/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;

namespace Vega.Graphics
{
	/// <summary>
	/// Contains utility functionality for <see cref="VertexFormat"/> values.
	/// </summary>
	public static class VertexFormatUtils
	{
		private static readonly Dictionary<VertexFormat, (uint cs, uint c)> FORMAT_DATA = new() {
			{ VertexFormat.Byte,    (1, 1) }, { VertexFormat.Byte2,   (1, 2) }, { VertexFormat.Byte4,   (1, 4) },
			{ VertexFormat.SByte,   (1, 1) }, { VertexFormat.SByte2,  (1, 2) }, { VertexFormat.SByte4,  (1, 4) },
			{ VertexFormat.UShort,  (2, 1) }, { VertexFormat.UShort2, (2, 2) }, { VertexFormat.UShort4, (2, 4) },
			{ VertexFormat.Short,   (2, 1) }, { VertexFormat.Short2,  (2, 2) }, { VertexFormat.Short4,  (2, 4) },
			{ VertexFormat.UInt,    (4, 1) }, { VertexFormat.UInt2,   (4, 2) }, { VertexFormat.UInt3,   (4, 3) }, 
			{ VertexFormat.UInt4,   (4, 4) },
			{ VertexFormat.Int,     (4, 1) }, { VertexFormat.Int2,    (4, 2) }, { VertexFormat.Int3,    (4, 3) },
			{ VertexFormat.Int4,    (4, 4) },
			{ VertexFormat.ULong,   (8, 1) }, { VertexFormat.ULong2,  (8, 2) }, { VertexFormat.ULong3,  (8, 3) },
			{ VertexFormat.ULong4,  (8, 4) },
			{ VertexFormat.Long,    (8, 1) }, { VertexFormat.Long2,   (8, 2) }, { VertexFormat.Long3,   (8, 3) },
			{ VertexFormat.Long4,   (8, 4) },

			{ VertexFormat.Half,    (2, 1) }, { VertexFormat.Half2,   (2, 2) }, { VertexFormat.Half4,   (2, 4) },
			{ VertexFormat.Float,   (4, 1) }, { VertexFormat.Float2,  (4, 2) }, { VertexFormat.Float3,  (4, 3) },
			{ VertexFormat.Float4,  (4, 4) },
			{ VertexFormat.Double,  (8, 1) }, { VertexFormat.Double2, (8, 2) }, { VertexFormat.Double3, (8, 3) },
			{ VertexFormat.Double4, (8, 4) },

			{ VertexFormat.FloatUnorm8,  (1, 1) }, { VertexFormat.Float2Unorm8,  (1, 2) }, { VertexFormat.Float4Unorm8,  (1, 4) },
			{ VertexFormat.FloatUnorm16, (2, 1) }, { VertexFormat.Float2Unorm16, (2, 2) }, { VertexFormat.Float4Unorm16, (2, 4) }
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
	}
}
