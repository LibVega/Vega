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
		public enum BaseType : byte
		{
			Void     = 0,
			Boolean  = 1,
			Signed   = 2,
			Unsigned = 3,
			Float    = 4,
			Sampler  = 5,
			Image    = 6,
			ROBuffer = 7,
			RWBuffer = 8,
			ROTexels = 9,
			RWTexels = 10
		}

		// Texel-type ranks
		public enum TexelRank : byte
		{
			E1D      = 0,
			E2D      = 1,
			E3D      = 2,
			E1DArray = 3,
			E2DArray = 4,
			Cube     = 5,
			Buffer   = 6
		}

		// Texel base types
		public enum TexelType : byte
		{
			Signed   = 0,
			Unsigned = 1,
			Float    = 2,
			UNorm    = 3,
			SNorm    = 4
		}

		// Interface variable type (binary compatible)
		[StructLayout(LayoutKind.Explicit, Size = 8)]
		public struct InterfaceVariable
		{
			[FieldOffset(0)] public byte Location;
			[FieldOffset(1)] public BaseType BaseType;
			[FieldOffset(2)] public fixed byte Dims[2];
			[FieldOffset(4)] public byte ArraySize;
			[FieldOffset(5)] public fixed byte _Padding_[3];
		}

		// Binding variable type (binary compatible)
		[StructLayout(LayoutKind.Explicit, Size = 8)]
		public struct BindingVariable
		{
			// Shared fields
			[FieldOffset(0)] public byte Slot;
			[FieldOffset(1)] public BaseType BaseType;
			[FieldOffset(2)] public ushort StageMask;
			// Buffer fields
			[FieldOffset(4)] public ushort BufferSize;
			// Texel fields
			[FieldOffset(4)] public TexelRank Rank;
			[FieldOffset(5)] public TexelType TexelType;
			[FieldOffset(6)] public byte TexelSize;
			[FieldOffset(7)] public byte TexelComponents;
		}

		// Subpass input description (binary compatible)
		[StructLayout(LayoutKind.Explicit, Size = 4)]
		public struct SubpassInput
		{
			[FieldOffset(0)] public TexelType ComponentType;
			[FieldOffset(1)] public byte ComponentCount;
			[FieldOffset(2)] public fixed byte _Padding_[2];
		}

		// Struct member (binary compatible, without the variable length name)
		[StructLayout(LayoutKind.Explicit, Size = 6)]
		public struct StructMember
		{
			[FieldOffset(0)] public ushort Offset;
			[FieldOffset(2)] public BaseType BaseType;
			[FieldOffset(3)] public fixed byte Dims[2];
			[FieldOffset(5)] public byte ArraySize;
		}

		// Parses a vertex format from a given base type and dimensions
		private static VertexFormat? ParseVertexFormat(BaseType baseType, uint dim0, uint dim1) => baseType switch {
			BaseType.Signed => 
				(dim0 == 1) ? VertexFormat.Int : (dim0 == 2) ? VertexFormat.Int2 : 
				(dim0 == 3) ? VertexFormat.Int3 : VertexFormat.Int4,
			BaseType.Unsigned => 
				(dim0 == 1) ? VertexFormat.UInt : (dim0 == 2) ? VertexFormat.UInt2 : 
				(dim0 == 3) ? VertexFormat.UInt3 : VertexFormat.UInt4,
			BaseType.Float => dim1 switch {
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
		private static TexelFormat? ParseTexelFormat(TexelType baseType, uint size, uint dim) => baseType switch {
			TexelType.Signed =>
				(dim == 1) ? TexelFormat.Int : (dim == 2) ? TexelFormat.Int2 : TexelFormat.Int4,
			TexelType.Unsigned =>
				(dim == 1) ? TexelFormat.UInt : (dim == 2) ? TexelFormat.UInt2 : TexelFormat.UInt4,
			TexelType.Float =>
				(dim == 1) ? TexelFormat.Float : (dim == 2) ? TexelFormat.Float2 : TexelFormat.Float4,
			TexelType.UNorm => 
				(size == 1)
					? ((dim == 1) ? TexelFormat.UNorm : (dim == 2) ? TexelFormat.UNorm2 : TexelFormat.UNorm4)
				: (size == 2)
					? ((dim == 1) ? TexelFormat.U16Norm : (dim == 2) ? TexelFormat.U16Norm2 : TexelFormat.U16Norm4)
				: null,
			TexelType.SNorm => 
				(size == 1)
					? ((dim == 1) ? TexelFormat.SNorm : (dim == 2) ? TexelFormat.SNorm2 : TexelFormat.SNorm4)
				: (size == 2)
					? ((dim == 1) ? TexelFormat.S16Norm : (dim == 2) ? TexelFormat.S16Norm2 : TexelFormat.S16Norm4)
				: null,
			_ => null
		};

		// Parses a BindingType for a binding variable
		private static BindingType? ParseBindingType(in BindingVariable bvar) => bvar.BaseType switch {
			BaseType.Sampler => bvar.Rank switch {
				TexelRank.E1D => BindingType.Sampler1D,
				TexelRank.E2D => BindingType.Sampler2D,
				TexelRank.E3D => BindingType.Sampler3D,
				TexelRank.E1DArray => BindingType.Sampler1DArray,
				TexelRank.E2DArray => BindingType.Sampler2DArray,
				TexelRank.Cube => BindingType.SamplerCube,
				_ => null
			},
			BaseType.Image => bvar.Rank switch {
				TexelRank.E1D => BindingType.Image1D,
				TexelRank.E2D => BindingType.Image2D,
				TexelRank.E3D => BindingType.Image3D,
				TexelRank.E1DArray => BindingType.Image1DArray,
				TexelRank.E2DArray => BindingType.Image2DArray,
				_ => null
			},
			BaseType.ROBuffer => BindingType.ROBuffer,
			BaseType.RWBuffer => BindingType.RWBuffer,
			BaseType.ROTexels => BindingType.ROTexels,
			BaseType.RWTexels => BindingType.RWTexels,
			_ => null
		};

		// Parses a texel type for a binding variable
		private static TexelFormat? ParseBindingTexelFormat(in BindingVariable bvar) => bvar.BaseType switch {
			BaseType.Sampler => bvar.TexelType switch {
				TexelType.Signed => TexelFormat.Int4,
				TexelType.Unsigned => TexelFormat.UInt4,
				TexelType.Float => TexelFormat.Float4,
				TexelType.UNorm => 
					(bvar.TexelSize == 1) ? TexelFormat.UNorm4 : (bvar.TexelSize == 2) ? TexelFormat.U16Norm4 : null,
				TexelType.SNorm =>
					(bvar.TexelSize == 1) ? TexelFormat.SNorm4 : (bvar.TexelSize == 2) ? TexelFormat.S16Norm4 : null,
				_ => null
			},
			BaseType.Image => ParseTexelFormat(bvar.TexelType, bvar.TexelSize, bvar.TexelComponents),
			BaseType.ROTexels => bvar.TexelType switch {
				TexelType.Signed => TexelFormat.Int4,
				TexelType.Unsigned => TexelFormat.UInt4,
				TexelType.Float => TexelFormat.Float4,
				TexelType.UNorm =>
					(bvar.TexelSize == 1) ? TexelFormat.UNorm4 : (bvar.TexelSize == 2) ? TexelFormat.U16Norm4 : null,
				TexelType.SNorm =>
					(bvar.TexelSize == 1) ? TexelFormat.SNorm4 : (bvar.TexelSize == 2) ? TexelFormat.S16Norm4 : null,
				_ => null
			},
			BaseType.RWTexels => ParseTexelFormat(bvar.TexelType, bvar.TexelSize, bvar.TexelComponents),
			_ => null
		};
	}
}
