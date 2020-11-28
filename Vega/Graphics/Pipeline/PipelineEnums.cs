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
}
