/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Render
{
	/// <summary>
	/// The possible primitives that a raw vertex buffer can be interpreted as.
	/// </summary>
	public enum Topology : sbyte
	{
		/// <summary>
		/// Interpret as individual disconnected points.
		/// </summary>
		PointList = VkPrimitiveTopology.PointList,
		/// <summary>
		/// Interpret as pairs of points that create individual, disconnected line segments.
		/// </summary>
		LineList = VkPrimitiveTopology.LineList,
		/// <summary>
		/// Interpret as a set of points creating a continunous connected line.
		/// </summary>
		LineStrip = VkPrimitiveTopology.LineStrip,
		/// <summary>
		/// Interpret as point triples that create individual, disconnected triangles.
		/// </summary>
		TriangleList = VkPrimitiveTopology.TriangleList,
		/// <summary>
		/// Interpret as a collection of connected triangles, each sharing a side with the next.
		/// </summary>
		TriangleStrip = VkPrimitiveTopology.TriangleStrip,
		/// <summary>
		/// Interpret as a collection of connected triangles, each sharing the first point in the vertex stream.
		/// </summary>
		TriangleFan = VkPrimitiveTopology.TriangleFan
	}

	/// <summary>
	/// Polygon interior fragment fill mode.
	/// </summary>
	public enum FillMode : sbyte
	{
		/// <summary>
		/// Polygons are filled completely in the interior.
		/// </summary>
		Solid = VkPolygonMode.Fill,
		/// <summary>
		/// Polygons are filled along their edge lines. Requires <see cref="GraphicsFeatures.FillModeNonSolid"/>.
		/// </summary>
		Line = VkPolygonMode.Line,
		/// <summary>
		/// Polygons are only highlighted at their vertices. Requires <see cref="GraphicsFeatures.FillModeNonSolid"/>.
		/// </summary>
		Point = VkPolygonMode.Point
	}

	/// <summary>
	/// Face culling modes.
	/// </summary>
	public enum CullMode : byte
	{
		/// <summary>
		/// No faces are culled.
		/// </summary>
		None = (byte)VkCullModeFlags.None,
		/// <summary>
		/// The front face is culled.
		/// </summary>
		Front = (byte)VkCullModeFlags.Front,
		/// <summary>
		/// The back face is culled.
		/// </summary>
		Back = (byte)VkCullModeFlags.Back,
		/// <summary>
		/// All faces are culled.
		/// </summary>
		Both = (byte)VkCullModeFlags.FrontAndBack
	}

	/// <summary>
	/// The vertex winding directions to define a front face.
	/// </summary>
	public enum Winding : sbyte
	{
		/// <summary>
		/// Counter-clockwise winding defines a front face.
		/// </summary>
		CCW = VkFrontFace.CounterClockwise,
		/// <summary>
		/// Clockwise winding defines a front face.
		/// </summary>
		CW = VkFrontFace.Clockwise
	}

	/// <summary>
	/// The graphics operations available for comparing values.
	/// </summary>
	public enum CompareOp : sbyte
	{
		/// <summary>
		/// The comparison is never true.
		/// </summary>
		Never = VkCompareOp.Never,
		/// <summary>
		/// The comparison is true if <c>first < second</c>.
		/// </summary>
		Less = VkCompareOp.Less,
		/// <summary>
		/// The comparison is true if <c>first == second</c>.
		/// </summary>
		Equal = VkCompareOp.Equal,
		/// <summary>
		/// The comparison is true if <c>first <= second</c>.
		/// </summary>
		LessOrEqual = VkCompareOp.LessOrEqual,
		/// <summary>
		/// The comparison is true if <c>first > second</c>.
		/// </summary>
		Greater = VkCompareOp.Greater,
		/// <summary>
		/// The comparison is true if <c>first != second</c>.
		/// </summary>
		NotEqual = VkCompareOp.NotEqual,
		/// <summary>
		/// The comparison is true if <c>first >= second</c>.
		/// </summary>
		GreaterOrEqual = VkCompareOp.GreaterOrEqual,
		/// <summary>
		/// The comparison is always true.
		/// </summary>
		Always = VkCompareOp.Always
	}

	/// <summary>
	/// Operations available on the stencil buffer.
	/// </summary>
	public enum StencilOp : sbyte
	{
		/// <summary>
		/// The stencil value is untouched by the operation.
		/// </summary>
		Keep = VkStencilOp.Keep,
		/// <summary>
		/// The stencil value is set to zero.
		/// </summary>
		Zero = VkStencilOp.Zero,
		/// <summary>
		/// The stencil value is replaced with a reference constant.
		/// </summary>
		Replace = VkStencilOp.Replace,
		/// <summary>
		/// The stencil value is incremented with clamping at max value.
		/// </summary>
		IncClamp = VkStencilOp.IncrementAndClamp,
		/// <summary>
		/// The stencil value is decremented with clamping at zero.
		/// </summary>
		DecClamp = VkStencilOp.DecrementAndClamp,
		/// <summary>
		/// The stencil value is bitwise inverted.
		/// </summary>
		Invert = VkStencilOp.Invert,
		/// <summary>
		/// The stencil value is incremented with wrapping at max value.
		/// </summary>
		IncWrap = VkStencilOp.IncrementAndWrap,
		/// <summary>
		/// The stencil value is decremented with wrapping at zero.
		/// </summary>
		DecWrap = VkStencilOp.DecrementAndWrap
	}

	/// <summary>
	/// Available depth buffer operation states.
	/// </summary>
	public enum DepthMode : sbyte
	{
		/// <summary>
		/// No depth buffer reads or writes are performed.
		/// </summary>
		None = 0,
		/// <summary>
		/// Depth testing is enabled, but no values are modified or written back to the depth buffer.
		/// </summary>
		TestOnly = 1,
		/// <summary>
		/// Depth testing is disabled, and all depth buffer writes occur regardless of existing content.
		/// </summary>
		Overwrite = 2,
		/// <summary>
		/// Depth testing is enabled, and fragments that pass overwrite their value into the depth buffer.
		/// </summary>
		Default = 3
	}

	/// <summary>
	/// Available blending operation input factors. "Inv*" factors perform color inversion operations, which are
	/// <c>(1, 1, 1) - color</c>, or <c>1 - alpha</c>.
	/// </summary>
	public enum BlendFactor : sbyte
	{
		/// <summary>
		/// Input factor is zero (black/transparent).
		/// </summary>
		Zero = VkBlendFactor.Zero,
		/// <summary>
		/// Input factor is one (white/opaque).
		/// </summary>
		One = VkBlendFactor.One,
		/// <summary>
		/// Input factor is the unchanged incoming color.
		/// </summary>
		SrcColor = VkBlendFactor.SrcColor,
		/// <summary>
		/// Input factor is the inverse of the incoming color.
		/// </summary>
		InvSrcColor = VkBlendFactor.OneMinusSrcColor,
		/// <summary>
		/// Input factor is the unchanged existing color.
		/// </summary>
		DstColor = VkBlendFactor.DstColor,
		/// <summary>
		/// Input factor is the inverse of the existing color.
		/// </summary>
		InvDstColor = VkBlendFactor.OneMinusDstColor,
		/// <summary>
		/// Input factor is the unchanged incoming alpha.
		/// </summary>
		SrcAlpha = VkBlendFactor.SrcAlpha,
		/// <summary>
		/// Input factor is the inverse of the incoming alpha.
		/// </summary>
		InvSrcAlpha = VkBlendFactor.OneMinusSrcAlpha,
		/// <summary>
		/// Input factor is the unchanged existing alpha.
		/// </summary>
		DstAlpha = VkBlendFactor.DstAlpha,
		/// <summary>
		/// Input factor is the inverse of the existing alpha.
		/// </summary>
		InvDstAlpha = VkBlendFactor.OneMinusDstAlpha,
		/// <summary>
		/// Input factor is a constant reference color.
		/// </summary>
		RefColor = VkBlendFactor.ConstantColor,
		/// <summary>
		/// Input factor is the inverse of a constant reference color.
		/// </summary>
		InvRefColor = VkBlendFactor.OneMinusConstantColor,
		/// <summary>
		/// Input factor is a constant reference alpha.
		/// </summary>
		RefAlpha = VkBlendFactor.ConstantAlpha,
		/// <summary>
		/// Input factor is the inverse of a constant reference alpha.
		/// </summary>
		InvRefAlpha = VkBlendFactor.OneMinusConstantAlpha,
		/// <summary>
		/// Input factor is the minimum of the incoming and existing alphas.
		/// </summary>
		SrcAlphaSaturate = VkBlendFactor.SrcAlphaSaturate
	}

	/// <summary>
	/// Available operations for combining the inputs during color buffer blending.
	/// </summary>
	public enum BlendOp : sbyte
	{
		/// <summary>
		/// The inputs are added (<c>src + dst</c>).
		/// </summary>
		Add = VkBlendOp.Add,
		/// <summary>
		/// The inputs are subtracted, with src first (<c>src - dst</c>).
		/// </summary>
		Subtract = VkBlendOp.Subtract,
		/// <summary>
		/// The inputs are subtracted, with dst first (<c>dst - src</c>).
		/// </summary>
		InvSubtract = VkBlendOp.ReverseSubtract,
		/// <summary>
		/// The component-wise minimum of the inputs is taken (<c>min(src, dst)</c>).
		/// </summary>
		Min = VkBlendOp.Min,
		/// <summary>
		/// The component-wise maximum of the inputs is taken (<c>max(src, dst)</c>).
		/// </summary>
		Max = VkBlendOp.Max
	}

	/// <summary>
	/// Represents a mask of different color channels in RGBA color space.
	/// </summary>
	[Flags]
	public enum ColorChannels : byte
	{
		/// <summary>
		/// Mask of no color channels.
		/// </summary>
		None = 0,
		/// <summary>
		/// Mask of the red color channel.
		/// </summary>
		R = (byte)VkColorComponentFlags.R,
		/// <summary>
		/// Mask of the green color channel.
		/// </summary>
		G = (byte)VkColorComponentFlags.G,
		/// <summary>
		/// Mask of the blue color channel.
		/// </summary>
		B = (byte)VkColorComponentFlags.B,
		/// <summary>
		/// Mask of the alpha channel.
		/// </summary>
		A = (byte)VkColorComponentFlags.A,
		/// <summary>
		/// Mask of the color channels (R, G, B).
		/// </summary>
		RGB = (R | G | B),
		/// <summary>
		/// Mask of all channels (R, G, B, A).
		/// </summary>
		RGBA = (R | G | B | A)
	}
}
