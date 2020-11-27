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
}
