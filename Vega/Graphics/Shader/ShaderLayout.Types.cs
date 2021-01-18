/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.InteropServices;

namespace Vega.Graphics
{
	// The subtypes used in shader reflection
	public sealed partial class ShaderLayout
	{
		/// <summary>
		/// Contains information about a vertex inputs attribute for a shader.
		/// </summary>
		public struct VertexInput
		{
			#region Fields
			/// <summary>
			/// The index of the base binding location.
			/// </summary>
			public uint Location;
			/// <summary>
			/// The data format of the vertex attribute.
			/// </summary>
			public VertexFormat Format;
			/// <summary>
			/// The size of the array, or 1 if the attribute is not an array.
			/// </summary>
			public uint ArraySize;
			#endregion // Fields

			public VertexInput(uint loc, VertexFormat fmt, uint asize)
			{
				Location = loc;
				Format = fmt;
				ArraySize = asize;
			}
		}

		/// <summary>
		/// Contains information about a fragment attachment output for a shader.
		/// </summary>
		public struct FragmentOutput
		{
			#region Fields
			/// <summary>
			/// The index of the fragment attachment for this output.
			/// </summary>
			public uint Location;
			/// <summary>
			/// The texel format expected by the fragment output.
			/// </summary>
			public TexelFormat Format;
			#endregion // Fields

			public FragmentOutput(uint loc, TexelFormat fmt)
			{
				Location = loc;
				Format = fmt;
			}
		}

		/// <summary>
		/// Contains information about a binding for a shader.
		/// </summary>
		[StructLayout(LayoutKind.Explicit)]
		public struct Binding
		{
			#region Fields
			/// <summary>
			/// The index of the binding slot for this location.
			/// </summary>
			[FieldOffset(0)] public uint Slot;
			/// <summary>
			/// The mask of shader stages that access the binding.
			/// </summary>
			[FieldOffset(4)] public ShaderStages StageMask;
			/// <summary>
			/// The type of the binding.
			/// </summary>
			[FieldOffset(8)] public BindingType Type;

			#region Struct
			/// <summary>
			/// For <see cref="BindingType.ROBuffer"/> and <see cref="BindingType.RWBuffer"/>, this is the size of
			/// the buffer struct type in bytes.
			/// </summary>
			[FieldOffset(12)] public uint StructSize;
			#endregion // Struct

			#region Texel
			/// <summary>
			/// For binding types that involve texel data, this is the format of the texels managed by the resource.
			/// </summary>
			[FieldOffset(12)] public TexelFormat TexelFormat;
			#endregion // Texel
			#endregion // Fields

			public Binding(uint slot, ShaderStages smask, BindingType type, uint ssize)
			{
				Slot = slot;
				StageMask = smask;
				Type = type;
				TexelFormat = default; // Overridden by StructSize
				StructSize = ssize;
			}

			public Binding(uint slot, ShaderStages smask, BindingType type, TexelFormat tfmt)
			{
				Slot = slot;
				StageMask = smask;
				Type = type;
				StructSize = 0; // Overridden by TexelFormat
				TexelFormat = tfmt;
			}
		}

		/// <summary>
		/// Contains information about a member within a shader uniform struct.
		/// </summary>
		public sealed record UniformMember
		{
			#region Fields
			/// <summary>
			/// The name of the uniform member.
			/// </summary>
			public string Name { get; init; }
			/// <summary>
			/// The offset of the member into the struct data, in bytes.
			/// </summary>
			public uint Offset { get; init; }
			/// <summary>
			/// The data format of the member.
			/// </summary>
			public VertexFormat Format { get; init; }
			/// <summary>
			/// The member array size, or 1 if the member is not an array.
			/// </summary>
			public uint ArraySize { get; init; }
			#endregion // Fields

			public UniformMember(string name, uint off, VertexFormat fmt, uint asize)
			{
				Name = name;
				Offset = off;
				Format = fmt;
				ArraySize = asize;
			}
		}

		/// <summary>
		/// Contains information about a subpass input within a shader.
		/// </summary>
		public struct SubpassInput
		{
			#region Fields
			/// <summary>
			/// The subpass input index.
			/// </summary>
			public uint Index;
			/// <summary>
			/// The texel format of the input data.
			/// </summary>
			public TexelFormat Format;
			#endregion // Fields

			public SubpassInput(uint idx, TexelFormat fmt)
			{
				Index = idx;
				Format = fmt;
			}
		}
	}
}
