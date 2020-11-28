/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// The possible primitives that a raw vertex buffer can be interpreted as.
	/// </summary>
	public enum Topology : int
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
	public enum FillMode : int
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
	public enum CullMode : uint
	{
		/// <summary>
		/// No faces are culled.
		/// </summary>
		None = VkCullModeFlags.None,
		/// <summary>
		/// The front face is culled.
		/// </summary>
		Front = VkCullModeFlags.Front,
		/// <summary>
		/// The back face is culled.
		/// </summary>
		Back = VkCullModeFlags.Back,
		/// <summary>
		/// All faces are culled.
		/// </summary>
		Both = VkCullModeFlags.FrontAndBack
	}

	/// <summary>
	/// The vertex winding directions to define a front face.
	/// </summary>
	public enum Winding : int
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
	public enum CompareOp : int
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
	public enum StencilOp : int
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
	public enum DepthMode : int
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
}
