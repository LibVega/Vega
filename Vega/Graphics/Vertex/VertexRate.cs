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
	/// Describes the rate at which vertex data is read from a vertex buffer.
	/// </summary>
	public enum VertexRate : int
	{
		/// <summary>
		/// The vertex data is read and updated for each vertex.
		/// </summary>
		Vertex = VkVertexInputRate.Vertex,
		/// <summary>
		/// The vertex data is read and updated for each instance in instanced drawing.
		/// </summary>
		Instance = VkVertexInputRate.Instance
	}
}
