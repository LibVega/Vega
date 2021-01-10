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
	/// A flags-like type of supported shader stages.
	/// </summary>
	[Flags]
	public enum ShaderStages : uint
	{
		/// <summary>
		/// Mask representing no shader stages.
		/// </summary>
		None = 0,
		/// <summary>
		/// A single bit representing the graphics pipeline vertex stage.
		/// </summary>
		Vertex = VkShaderStageFlags.Vertex,
		/// <summary>
		/// A single bit representing the graphics pipeline fragment stage.
		/// </summary>
		Fragment = VkShaderStageFlags.Fragment,
		/// <summary>
		/// Mask representing all supported shader stages.
		/// </summary>
		AllGraphics = Vertex | Fragment
	}
}
