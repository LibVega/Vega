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
	/// Defines the set of graphics shader stages as a mask.
	/// </summary>
	[Flags]
	public enum ShaderStages : uint
	{
		/// <summary>
		/// Bitmask of no shader stages.
		/// </summary>
		None = 0,
		/// <summary>
		/// The vertex stage.
		/// </summary>
		Vertex = VkShaderStageFlags.Vertex,
		/// <summary>
		/// The tessellation control stage.
		/// </summary>
		TessControl = VkShaderStageFlags.TessellationControl,
		/// <summary>
		/// The tessellation evaluation stage.
		/// </summary>
		TessEval = VkShaderStageFlags.TessellationEvaluation,
		/// <summary>
		/// The geometry stage.
		/// </summary>
		Geometry = VkShaderStageFlags.Geometry,
		/// <summary>
		/// The fragment stage.
		/// </summary>
		Fragment = VkShaderStageFlags.Fragment,
		/// <summary>
		/// Bitmask of all stages.
		/// </summary>
		All = VkShaderStageFlags.AllGraphics
	}
}
