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
		[StructLayout(LayoutKind.Explicit, Size = 8)]
		public struct InterfaceVariable
		{
			[FieldOffset(0)] public byte Location;
			[FieldOffset(1)] public ShaderBaseType BaseType;
			[FieldOffset(2)] public fixed byte Dims[2];
			[FieldOffset(4)] public byte ArraySize;
			[FieldOffset(5)] public fixed byte Padding[3];
		}

		// Binding variable type (binary compatible)
		[StructLayout(LayoutKind.Explicit, Size = 8)]
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

		// Uniform member (binary compatible, without the variable length name)
		[StructLayout(LayoutKind.Explicit, Size = 12)]
		public struct UniformMember
		{
			[FieldOffset(0)] public uint Offset;
			[FieldOffset(4)] public ShaderBaseType BaseType;
			[FieldOffset(5)] public fixed byte Dims[2];
			[FieldOffset(7)] public byte ArraySize;
			[FieldOffset(8)] public uint NameLength;
		}

		// Subpass input description (binary compatible)
		[StructLayout(LayoutKind.Explicit, Size = 4)]
		public struct SubpassInput
		{
			[FieldOffset(0)] public ShaderBaseType ComponentType;
			[FieldOffset(1)] public byte ComponentCount;
			[FieldOffset(2)] public fixed byte Padding[2];
		}

		// Parses a vertex format from a given base type and dimensions
		private static VertexFormat? ParseVertexFormat(ShaderBaseType baseType, uint dim0, uint dim1) => baseType switch {
			ShaderBaseType.Signed => 
				(dim0 == 1) ? VertexFormat.Int : (dim0 == 2) ? VertexFormat.Int2 : 
				(dim0 == 3) ? VertexFormat.Int3 : VertexFormat.Int4,
			ShaderBaseType.Unsigned => 
				(dim0 == 1) ? VertexFormat.UInt : (dim0 == 2) ? VertexFormat.UInt2 : 
				(dim0 == 3) ? VertexFormat.UInt3 : VertexFormat.UInt4,
			ShaderBaseType.Float => dim1 switch {
				1 => 
					(dim0 == 1) ? VertexFormat.Float : (dim0 == 2) ? VertexFormat.Float2 :
					(dim0 == 3) ? VertexFormat.Float3 : VertexFormat.Float4,
				2 =>
					(dim0 == 2) ? VertexFormat.Float2x2 : (dim0 == 3) ? VertexFormat.Float2x3 : VertexFormat.Float2x4,
				3 =>
					(dim0 == 2) ? VertexFormat.Float3x2 : (dim0 == 3) ? VertexFormat.Float3x3 : VertexFormat.Float3x4,
				4 =>
					(dim0 == 2) ? VertexFormat.Float4x2 : (dim0 == 3) ? VertexFormat.Float4x3 : VertexFormat.Float4x4,
				_ => null
			},
			_ => null
		};

		// Parses a texel format from a given base type and dimensions
		private static TexelFormat? ParseTexelFormat(ShaderBaseType baseType, uint size, uint dim) => baseType switch {
			ShaderBaseType.Signed => 
				(dim == 1) ? TexelFormat.Int : (dim == 2) ? TexelFormat.Int2 : TexelFormat.Int4,
			ShaderBaseType.Unsigned => 
				(dim == 1) ? TexelFormat.UInt : (dim == 2) ? TexelFormat.UInt2 : TexelFormat.UInt4,
			ShaderBaseType.Float => size switch {
				4 => (dim == 1) ? TexelFormat.Float : (dim == 2) ? TexelFormat.Float2 : TexelFormat.Float4,
				1 => (dim == 1) ? TexelFormat.UNorm : (dim == 2) ? TexelFormat.UNorm2 : TexelFormat.UNorm4,
				_ => null
			},
			_ => null
		};

		// Parses a BindingType for a binding variable
		private static BindingType? ParseBindingType(in BindingVariable bvar) => bvar.BaseType switch {
			ShaderBaseType.Sampler => bvar.Dimensions switch {
				ImageDims.E1D => BindingType.Sampler1D,
				ImageDims.E2D => BindingType.Sampler2D,
				ImageDims.E3D => BindingType.Sampler3D,
				ImageDims.E1DArray => BindingType.Sampler1DArray,
				ImageDims.E2DArray => BindingType.Sampler2DArray,
				ImageDims.Cube => BindingType.SamplerCube,
				_ => null
			},
			ShaderBaseType.Image => bvar.Dimensions switch {
				ImageDims.E1D => BindingType.Image1D,
				ImageDims.E2D => BindingType.Image2D,
				ImageDims.E3D => BindingType.Image3D,
				ImageDims.E1DArray => BindingType.Image1DArray,
				ImageDims.E2DArray => BindingType.Image2DArray,
				_ => null
			},
			ShaderBaseType.ROBuffer => BindingType.ROBuffer,
			ShaderBaseType.RWBuffer => BindingType.RWBuffer,
			ShaderBaseType.ROTexels => BindingType.ROTexels,
			ShaderBaseType.RWTexels => BindingType.RWTexels,
			_ => null
		};

		// Parses a texel type for a binding variable
		private static TexelFormat? ParseBindingTexelFormat(in BindingVariable bvar) => bvar.BaseType switch {
			ShaderBaseType.Sampler => bvar.TexelType switch {
				ShaderBaseType.Float => TexelFormat.Float4,
				ShaderBaseType.Signed => TexelFormat.Int4,
				ShaderBaseType.Unsigned => TexelFormat.UInt4,
				_ => null
			},
			ShaderBaseType.Image => ParseTexelFormat(bvar.TexelType, bvar.TexelSize, bvar.TexelComponents),
			ShaderBaseType.ROTexels => bvar.TexelType switch {
				ShaderBaseType.Float => TexelFormat.Float4,
				ShaderBaseType.Signed => TexelFormat.Int4,
				ShaderBaseType.Unsigned => TexelFormat.UInt4,
				_ => null
			},
			ShaderBaseType.RWTexels => ParseTexelFormat(bvar.TexelType, bvar.TexelSize, bvar.TexelComponents),
			_ => null
		};
	}
}
