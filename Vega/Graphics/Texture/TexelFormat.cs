/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// Provides the set of texel formats that are supported for texture resources.
	/// </summary>
	public enum TexelFormat
	{
		// ================================== STANDARD COLOR INTEGER FORMATS ==========================================
		/// <summary>
		/// An unsigned 8-bit integer representing a value between 0 and 1. Channels: R.
		/// </summary>
		UNorm = Vk.Format.R8Unorm,
		/// <summary>
		/// Two unsigned 8-bit integers representing values between 0 and 1. Channels: RG.
		/// </summary>
		UNorm2 = Vk.Format.R8g8Unorm,
		/// <summary>
		/// Four unsigned 8-bit integers representing values between 0 and 1. Channels: RGBA.
		/// </summary>
		UNorm4 = Vk.Format.R8g8b8a8Unorm,
		/// <summary>
		/// Four unsigned 8-bit integers representing values between 0 and 1, with color channels reversed. Channels: BGRA.
		/// </summary>
		UNormBgra = Vk.Format.B8g8r8a8Unorm,
		/// <summary>
		/// A single unsigned 8-bit integer. Channels: R.
		/// </summary>
		UByte = Vk.Format.R8Uint,
		/// <summary>
		/// Two unsigned 8-bit integers. Channels: RG.
		/// </summary>
		UByte2 = Vk.Format.R8g8Uint,
		/// <summary>
		/// Four unsigned 8-bit integers. Channels: RGBA.
		/// </summary>
		UByte4 = Vk.Format.R8g8b8a8Uint,
		/// <summary>
		/// A single signed 8-bit integer. Channels: R.
		/// </summary>
		Byte = Vk.Format.R8Sint,
		/// <summary>
		/// Two signed 8-bit integers. Channels: RG.
		/// </summary>
		Byte2 = Vk.Format.R8g8Sint,
		/// <summary>
		/// Four signed 8-bit integers. Channels: RGBA.
		/// </summary>
		Byte4 = Vk.Format.R8g8b8a8Sint,
		/// <summary>
		/// An unsigned 16-bit integer. Channels: R.
		/// </summary>
		UShort = Vk.Format.R16Uint,
		/// <summary>
		/// Two unsigned 16-bit integers. Channels: RG.
		/// </summary>
		UShort2 = Vk.Format.R16g16Uint,
		/// <summary>
		/// Four unsigned 16-bit integers. Channels: RGBA.
		/// </summary>
		UShort4 = Vk.Format.R16g16b16a16Uint,
		/// <summary>
		/// A signed 16-bit integer. Channels: R.
		/// </summary>
		Short = Vk.Format.R16Sint,
		/// <summary>
		/// Two signed 16-bit integers. Channels: RG.
		/// </summary>
		Short2 = Vk.Format.R16g16Sint,
		/// <summary>
		/// Four signed 16-bit integers. Channels: RGBA.
		/// </summary>
		Short4 = Vk.Format.R16g16b16a16Sint,
		/// <summary>
		/// An unsigned 32-bit integer. Channels: R.
		/// </summary>
		UInt = Vk.Format.R32Uint,
		/// <summary>
		/// Two unsigned 32-bit integers. Channels: RG.
		/// </summary>
		UInt2 = Vk.Format.R32g32Uint,
		/// <summary>
		/// Four unsigned 32-bit integers. Channels: RGBA.
		/// </summary>
		UInt4 = Vk.Format.R32g32b32a32Uint,
		/// <summary>
		/// A signed 32-bit integer. Channels: R.
		/// </summary>
		Int = Vk.Format.R32Sint,
		/// <summary>
		/// Two signed 32-bit integers. Channels: RG.
		/// </summary>
		Int2 = Vk.Format.R32g32Sint,
		/// <summary>
		/// Four signed 32-bit integers. Channels: RGBA.
		/// </summary>
		Int4 = Vk.Format.R32g32b32a32Sint,
		/// <summary>
		/// Identical to <see cref="UNorm4"/>.
		/// </summary>
		Color = Vk.Format.R8g8b8a8Unorm,

		// =============================== STANDARD COLOR FLOATING POINT FORMATS ======================================
		/// <summary>
		/// A single-precision 32-bit floating point number. Channels: R.
		/// </summary>
		Float = Vk.Format.R32Sfloat,
		/// <summary>
		/// Two single-precision 32-bit floating point numbers. Channels: RG.
		/// </summary>
		Float2 = Vk.Format.R32g32Sfloat,
		/// <summary>
		/// Four single-precision 32-bit floating point numbers. Channels: RGBA.
		/// </summary>
		Float4 = Vk.Format.R32g32b32a32Sfloat,

		// ========================================= PACKED FORMATS ===================================================
		/// <summary>
		/// A 16-bit UNorm packed format with a 1-bit alpha in LSB, and 5-bit RGB color channels.
		/// </summary>
		Argb1555 = Vk.Format.A1r5g5b5UnormPack16,
		/// <summary>
		/// A 16-bit UNorm packed format with 5-bit BGR color channels, and a 1-bit alpha in MSB.
		/// </summary>
		Bgra5551 = Vk.Format.B5g5r5a1UnormPack16,
		/// <summary>
		/// A 16-bit UNorm packed format with 5-bit RGB color channels, and a 1-bit alpha in MSB.
		/// </summary>
		Rgba5551 = Vk.Format.R5g5b5a1UnormPack16,
		/// <summary>
		/// A 32-bit UNorm packed format with a 2-bit alpha in LSB, and 10-bit RGB color channels.
		/// </summary>
		Argb2101010 = Vk.Format.A2r10g10b10UnormPack32,
		/// <summary>
		/// A 16-bit UNorm packed format with 5 red and blue bits, and 6 green bits.
		/// </summary>
		Rgb565 = Vk.Format.R5g6b5UnormPack16,
		/// <summary>
		/// A 16-bit UNorm packed format with 5 red and blue bits, and 6 green bits, with revered order.
		/// </summary>
		Bgr565 = Vk.Format.B5g6r5UnormPack16,
		/// <summary>
		/// A 16-bit UNorm packed format with 4-bits per channel, reversed color channel order.
		/// </summary>
		Bgra4444 = Vk.Format.B4g4r4a4UnormPack16,
		/// <summary>
		/// A 16-bit UNorm packed format with 4-bits per channel.
		/// </summary>
		Rgba4444 = Vk.Format.R4g4b4a4UnormPack16,

		// ====================================== DEPTH/STENCIL FORMATS ===============================================
		/// <summary>
		/// A single 16-bit unsigned integer representing a depth value between 0 and 1.
		/// </summary>
		Depth16 = Vk.Format.D16Unorm,
		/// <summary>
		/// A single-precision 32-bit floating point number.
		/// </summary>
		Depth32 = Vk.Format.D32Sfloat,
		/// <summary>
		/// A packed 24-bit normalized integer representing a depth value between 0 and 1, and an 8-bit unsigned integer 
		/// representing stencil data.
		/// </summary>
		Depth24Stencil8 = Vk.Format.D24UnormS8Uint
	}
}
