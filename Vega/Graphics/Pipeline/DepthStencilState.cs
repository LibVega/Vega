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
	/// Describes how the depth and stencil buffers are used and affected in a Pipeline.
	/// </summary>
	public struct DepthStencilState : IEquatable<DepthStencilState>
	{
		/// <summary>
		/// Pipeline state where all depth and stencil testing is disabled.
		/// </summary>
		public static readonly DepthStencilState Disabled = new(DepthMode.None, CompareOp.Never, null, null);
		/// <summary>
		/// Pipeline state with test-only less-than depth, and no stencil testing.
		/// </summary>
		public static readonly DepthStencilState TestOnly = new(DepthMode.TestOnly, CompareOp.Less, null, null);
		/// <summary>
		/// Pipeline state with standard less-than depth testing and depth writeback, with no stencil testing.
		/// </summary>
		public static readonly DepthStencilState Default = new(DepthMode.Default, CompareOp.Less, null, null);

		#region Fields
		/// <summary>
		/// The depth buffer mode.
		/// </summary>
		public DepthMode DepthMode;
		/// <summary>
		/// The depth value comparison operation.
		/// </summary>
		public CompareOp DepthOp;
		/// <summary>
		/// The optional stencil buffer operations on front faces.
		/// </summary>
		public StencilState? FrontStencil;
		/// <summary>
		/// The optional stencil buffer operations on back faces.
		/// </summary>
		public StencilState? BackStencil;
		#endregion // Fields

		/// <summary>
		/// Describes a new depth/stencil state.
		/// </summary>
		public DepthStencilState(DepthMode mode, CompareOp op, StencilState? frontStencil = null,
			StencilState? backStencil = null)
		{
			DepthMode = mode;
			DepthOp = op;
			FrontStencil = frontStencil;
			BackStencil = backStencil;
		}

		// Fill vulkan info object
		internal void ToVk(out VkPipelineDepthStencilStateCreateInfo vk)
		{
			vk = new(
				flags: VkPipelineDepthStencilStateCreateFlags.NoFlags,
				depthTestEnable: (DepthMode == DepthMode.TestOnly) || (DepthMode == DepthMode.Default),
				depthWriteEnable: (DepthMode == DepthMode.Overwrite) || (DepthMode == DepthMode.Default),
				depthCompareOp: (VkCompareOp)DepthOp,
				depthBoundsTestEnable: false,
				stencilTestEnable: FrontStencil.HasValue || BackStencil.HasValue
				// front: new(),
				// back: new(),
			);
			FrontStencil?.ToVk(out vk.Front);
			BackStencil?.ToVk(out vk.Back);
		}

		#region Overrides
		public readonly override int GetHashCode() =>
			DepthMode.GetHashCode() ^ DepthOp.GetHashCode() ^ FrontStencil.GetHashCode() ^ BackStencil.GetHashCode();

		public readonly override string ToString() => 
			$"[{DepthMode}:{DepthOp}:{FrontStencil?.ToString() ?? "Disabled"}:{BackStencil?.ToString() ?? "Disabled"}]";

		public readonly override bool Equals(object? obj) => (obj is DepthStencilState rs) && (rs == this);

		readonly bool IEquatable<DepthStencilState>.Equals(DepthStencilState other) => other == this;
		#endregion // Overrides

		#region Operators
		public static bool operator == (in DepthStencilState l, in DepthStencilState r) =>
			(l.DepthMode == r.DepthMode) && (l.DepthOp == r.DepthOp) && (l.FrontStencil == r.FrontStencil) &&
			(l.BackStencil == r.BackStencil);

		public static bool operator != (in DepthStencilState l, in DepthStencilState r) =>
			(l.DepthMode != r.DepthMode) || (l.DepthOp != r.DepthOp) || (l.FrontStencil != r.FrontStencil) ||
			(l.BackStencil != r.BackStencil);
		#endregion // Operators
	}
}
