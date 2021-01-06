/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Defines the set of formats available for vertex component data.
	/// </summary>
	public enum VertexFormat : int
	{
		// ===================================== STANDARD INTEGER FORMATS =============================================
		/// <summary>A scalar 1-byte unsigned integer.</summary>
		Byte = VkFormat.R8Uint,
		/// <summary>A 2-component vector of 1-byte unsigned integers.</summary>
		Byte2 = VkFormat.R8g8Uint,
		/// <summary>A 4-component vector of 1-byte unsigned integers.</summary>
		Byte4 = VkFormat.R8g8b8a8Uint,
		/// <summary>A scalar 1-byte signed integer.</summary>
		SByte = VkFormat.R8Sint,
		/// <summary>A 2-component vector of 1-byte signed integers.</summary>
		SByte2 = VkFormat.R8g8Sint,
		/// <summary>A 4-component vector of 1-byte signed integers.</summary>
		SByte4 = VkFormat.R8g8b8a8Sint,
		/// <summary>A scalar 2-byte unsigned integer.</summary>
		UShort = VkFormat.R16Uint,
		/// <summary>A 2-component vector of 2-byte unsigned integers.</summary>
		UShort2 = VkFormat.R16g16Uint,
		/// <summary>A 4-component vector of 2-byte unsigned integers.</summary>
		UShort4 = VkFormat.R16g16b16a16Uint,
		/// <summary>A scalar 2-byte signed integer.</summary>
		Short = VkFormat.R16Sint,
		/// <summary>A 2-component vector of 2-byte signed integers.</summary>
		Short2 = VkFormat.R16g16Sint,
		/// <summary>A 4-component vector of 2-byte signed integers.</summary>
		Short4 = VkFormat.R16g16b16a16Sint,
		/// <summary>A scalar 4-byte unsigned integer.</summary>
		UInt = VkFormat.R32Uint,
		/// <summary>A 2-component vector of 4-byte unsigned integers.</summary>
		UInt2 = VkFormat.R32g32Uint,
		/// <summary>A 3-component vector of 4-byte unsigned integers.</summary>
		UInt3 = VkFormat.R32g32b32Uint,
		/// <summary>A 4-component vector of 4-byte unsigned integers.</summary>
		UInt4 = VkFormat.R32g32b32a32Uint,
		/// <summary>A scalar 4-byte signed integer.</summary>
		Int = VkFormat.R32Sint,
		/// <summary>A 2-component vector of 4-byte signed integers.</summary>
		Int2 = VkFormat.R32g32Sint,
		/// <summary>A 3-component vector of 4-byte signed integers.</summary>
		Int3 = VkFormat.R32g32b32Sint,
		/// <summary>A 4-component vector of 4-byte signed integers.</summary>
		Int4 = VkFormat.R32g32b32a32Sint,
		/// <summary>A scalar 8-byte unsigned integer.</summary>
		ULong = VkFormat.R64Uint,
		/// <summary>A 2-component vector of 8-byte unsigned integers.</summary>
		ULong2 = VkFormat.R64g64Uint,
		/// <summary>A 3-component vector of 8-byte unsigned integers.</summary>
		ULong3 = VkFormat.R64g64b64Uint,
		/// <summary>A 4-component vector of 8-byte unsigned integers.</summary>
		ULong4 = VkFormat.R64g64b64a64Uint,
		/// <summary>A scalar 8-byte signed integer.</summary>
		Long = VkFormat.R64Sint,
		/// <summary>A 2-component vector of 8-byte signed integers.</summary>
		Long2 = VkFormat.R64g64Sint,
		/// <summary>A 3-component vector of 8-byte signed integers.</summary>
		Long3 = VkFormat.R64g64b64Sint,
		/// <summary>A 4-component vector of 8-byte signed integers.</summary>
		Long4 = VkFormat.R64g64b64a64Sint,

		// ===================================== STANDARD FLOAT FORMATS ===============================================
		/// <summary>A scalar 2-byte floating point.</summary>
		Half = VkFormat.R16Sfloat,
		/// <summary>A 2-component vector of 2-byte floating points.</summary>
		Half2 = VkFormat.R16g16Sfloat,
		/// <summary>A 4-component vector of 2-byte floating points.</summary>
		Half4 = VkFormat.R16g16b16a16Sfloat,
		/// <summary>A scalar 4-byte floating point.</summary>
		Float = VkFormat.R32Sfloat,
		/// <summary>A 2-component vector of 4-byte floating points.</summary>
		Float2 = VkFormat.R32g32Sfloat,
		/// <summary>A 3-component vector of 4-byte floating points.</summary>
		Float3 = VkFormat.R32g32b32Sfloat,
		/// <summary>A 4-component vector of 4-byte floating points.</summary>
		Float4 = VkFormat.R32g32b32a32Sfloat,
		/// <summary>A scalar 8-byte floating point.</summary>
		Double = VkFormat.R64Sfloat,
		/// <summary>A 2-component vector of 8-byte floating points.</summary>
		Double2 = VkFormat.R64g64Sfloat,
		/// <summary>A 3-component vector of 8-byte floating points.</summary>
		Double3 = VkFormat.R64g64b64Sfloat,
		/// <summary>A 4-component vector of 8-byte floating points.</summary>
		Double4 = VkFormat.R64g64b64a64Sfloat,

		// ===================================== NORMALIZED FLOAT FORMATS =============================================
		/// <summary>A scalar floating point value backed by a 1-byte unsigned integer.</summary>
		FloatUnorm8 = VkFormat.R8Unorm,
		/// <summary>A 2-component vector of floating point values backed by 1-byte unsigned integers.</summary>
		Float2Unorm8 = VkFormat.R8g8Unorm,
		/// <summary>A 4-component vector of floating point values backed by 1-byte unsigned integers.</summary>
		Float4Unorm8 = VkFormat.R8g8b8a8Unorm,
		/// <summary>A scalar floating point value backed by a 2-byte unsigned integer.</summary>
		FloatUnorm16 = VkFormat.R16Unorm,
		/// <summary>A 2-component vector of floating point values backed by 2-byte unsigned integers.</summary>
		Float2Unorm16 = VkFormat.R16g16Unorm,
		/// <summary>A 4-component vector of floating point values backed by 2-byte unsigned integers.</summary>
		Float4Unorm16 = VkFormat.R16g16b16a16Unorm
	}
}
