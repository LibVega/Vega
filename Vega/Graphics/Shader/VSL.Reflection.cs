/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.InteropServices;

namespace Vega.Graphics
{
	internal unsafe static partial class VSL
	{
		// Shader base types
		public enum ShaderBaseType : byte
		{
			Void = 0,
			Bool = 1,
			Signed = 2,
			Unsigned = 3,
			Float = 4,
			Sampler = 5,
			Image = 6,
			Uniform = 7,
			ROBuffer = 8,
			RWBuffer = 9,
			ROTexels = 10,
			RWTexels = 11,
			Input = 12,
			Struct = 13
		}

		// Image dimension types
		public enum ImageDims : byte
		{
			Error = 0,
			E1D = 1,
			E2D = 2,
			E3D = 3,
			E1DArray = 4,
			E2DArray = 5,
			Cube = 6,
			Buffer = 7
		}

		// Interface variable type (binary compatible)
		[StructLayout(LayoutKind.Explicit)]
		public struct InterfaceVariable
		{
			[FieldOffset(0)] public byte Location;
			[FieldOffset(1)] public ShaderBaseType BaseType;
			[FieldOffset(2)] public fixed byte Dims[2];
			[FieldOffset(4)] public byte ArraySize;
			[FieldOffset(5)] public fixed byte Padding[3];
		}

		// Binding variable type (binary compatible)
		[StructLayout(LayoutKind.Explicit)]
		public struct BindingVariable
		{
			// Shared fields
			[FieldOffset(0)] public byte Slot;
			[FieldOffset(1)] public ShaderBaseType BaseType;
			[FieldOffset(2)] public ushort StageMask;
			// Buffer fields
			[FieldOffset(4)] public ushort BufferSize;
			// Texel fields
			[FieldOffset(4)] public ImageDims Dimensions;
			[FieldOffset(5)] public ShaderBaseType TexelType;
			[FieldOffset(6)] public byte TexelSize;
			[FieldOffset(7)] public byte TexelComponents;
		}

		// Uniform member (NOT binary compatible)
		public struct UniformMember
		{
			public uint Offset;
			public string Name;
		}

		// Subpass input description (binary compatible)
		[StructLayout(LayoutKind.Explicit)]
		public struct InputDescription
		{
			[FieldOffset(0)] public ShaderBaseType ComponentType;
			[FieldOffset(1)] public byte ComponentCount;
			[FieldOffset(2)] public fixed byte Padding[2];
		}
	}
}
