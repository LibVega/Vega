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
	/// Describes how primitives are rasterized into fragments in a Pipeline.
	/// </summary>
	public struct RasterizerState : IEquatable<RasterizerState>
	{
		/// <summary>
		/// Solid-fill rasterization with no face culling.
		/// </summary>
		public static readonly RasterizerState CullNone = new(FillMode.Solid, CullMode.None, Winding.CCW);
		/// <summary>
		/// Solid-fill rasterization with culling, counter-clockwise front.
		/// </summary>
		public static readonly RasterizerState CullCW = new(FillMode.Solid, CullMode.Back, Winding.CCW);
		/// <summary>
		/// Solid-fill rasterization with culling, clockwise front.
		/// </summary>
		public static readonly RasterizerState CullCCW = new(FillMode.Solid, CullMode.Back, Winding.CW);
		/// <summary>
		/// Wireframe fill rasterization with no face culling. Requires <see cref="GraphicsFeatures.WideLines"/>.
		/// </summary>
		public static readonly RasterizerState Wireframe = new(FillMode.Line, CullMode.None, Winding.CCW);

		#region Fields
		/// <summary>
		/// The primitive face fill mode.
		/// </summary>
		public FillMode FillMode;
		/// <summary>
		/// Culling setting for polygon faces.
		/// </summary>
		public CullMode CullMode;
		/// <summary>
		/// The winding direction that defines the front face of the polygons.
		/// </summary>
		public Winding FrontFace;
		/// <summary>
		/// The width of lines, in pixels. A value other than one requires <see cref="GraphicsFeatures.WideLines"/>.
		/// </summary>
		public float? LineWidth;
		/// <summary>
		/// If depth samples are clamped instead of discarded when out of range. Requires
		/// <see cref="GraphicsFeatures.DepthClamp"/>.
		/// </summary>
		public bool DepthClamp;
		#endregion // Fields

		/// <summary>
		/// Describe a new rasterizer state.
		/// </summary>
		public RasterizerState(FillMode fill, CullMode cull, Winding winding, float? lineWidth = null, 
			bool depthClamp = false)
		{
			FillMode = fill;
			CullMode = cull;
			FrontFace = winding;
			LineWidth = lineWidth;
			DepthClamp = depthClamp;
		}

		// Fill the vulkan info object
		internal readonly void ToVk(out VkPipelineRasterizationStateCreateInfo vk) => vk = new(
			flags: VkPipelineRasterizationStateCreateFlags.NoFlags,
			depthClampEnable: DepthClamp,
			rasterizerDiscardEnable: false,
			polygonMode: (VkPolygonMode)FillMode,
			cullMode: (VkCullModeFlags)CullMode,
			frontFace: (VkFrontFace)FrontFace,
			depthBiasEnable: false,
			depthBiasConstantFactor: 0,
			depthBiasClamp: 0,
			depthBiasSlopeFactor: 0,
			lineWidth: LineWidth.GetValueOrDefault(1.0f)
		);

		#region Overrides
		public readonly override int GetHashCode() =>
			FillMode.GetHashCode() ^ CullMode.GetHashCode() ^ FrontFace.GetHashCode() ^
			LineWidth.GetHashCode() ^ DepthClamp.GetHashCode();

		public readonly override string ToString() => 
			$"[{FillMode}:{CullMode}:{FrontFace}:{LineWidth.GetValueOrDefault(1)}:{DepthClamp}]";

		public readonly override bool Equals(object? obj) => (obj is RasterizerState rs) && (rs == this);

		readonly bool IEquatable<RasterizerState>.Equals(RasterizerState other) => other == this;
		#endregion // Overrides

		#region Operators
		public static bool operator == (in RasterizerState l, in RasterizerState r) =>
			(l.FillMode == r.FillMode) && (l.CullMode == r.CullMode) && (l.FrontFace == r.FrontFace) &&
			(l.LineWidth == r.LineWidth) && (l.DepthClamp == r.DepthClamp);

		public static bool operator != (in RasterizerState l, in RasterizerState r) =>
			(l.FillMode != r.FillMode) || (l.CullMode != r.CullMode) || (l.FrontFace != r.FrontFace) ||
			(l.LineWidth != r.LineWidth) || (l.DepthClamp != r.DepthClamp);
		#endregion // Operators
	}
}
