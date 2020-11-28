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
	/// Describes a stencil operation that is performed in a pipeline.
	/// </summary>
	public struct StencilState : IEquatable<StencilState>
	{
		/// <summary>
		/// Stencil operation that always passes, and does not affect the stencil buffer.
		/// </summary>
		public static readonly StencilState Always = new(StencilOp.Keep, StencilOp.Keep, CompareOp.Always, 0);
		/// <summary>
		/// Stencil operation that always writes a mask of 1 to the stencil buffer.
		/// </summary>
		public static readonly StencilState WriteMask = new(StencilOp.Replace, StencilOp.Keep, CompareOp.Always, 1);
		/// <summary>
		/// Stencil operation that passes on non-zero values, and does not affect the stencil buffer.
		/// </summary>
		public static readonly StencilState TestMask = new(StencilOp.Keep, StencilOp.Keep, CompareOp.NotEqual, 0);
		/// <summary>
		/// Stencil operation that always passes, and increments the buffer value by 1 with clamping.
		/// </summary>
		public static readonly StencilState Accumulate = new(StencilOp.IncClamp, StencilOp.Keep, CompareOp.Always, 0);

		#region Fields
		/// <summary>
		/// The operation to perform when the stencil test is true.
		/// </summary>
		public StencilOp Pass;
		/// <summary>
		/// The operation to perform when the stencil test is false.
		/// </summary>
		public StencilOp Fail;
		/// <summary>
		/// The operation to perform when the stencil test is true, but the depth test is false. If this has no value,
		/// then it defaults to <see cref="Fail"/>.
		/// </summary>
		public StencilOp? DepthFail;
		/// <summary>
		/// The comparison operation to perform as the stencil test.
		/// </summary>
		public CompareOp Compare;
		/// <summary>
		/// The reference constant value to use for comparisons, and as the replacement in 
		/// <see cref="StencilOp.Replace"/>.
		/// </summary>
		public byte Reference;
		#endregion // Fields

		/// <summary>
		/// Describes a new stencil operation state.
		/// </summary>
		public StencilState(StencilOp pass, StencilOp fail, CompareOp compare, byte reference, 
			StencilOp? depthFail = null)
		{
			Pass = pass;
			Fail = fail;
			DepthFail = depthFail;
			Compare = compare;
			Reference = reference;
		}

		// Fill the vulkan info object
		internal void ToVk(out VkStencilOpState vk) => vk = new(
			failOp: (VkStencilOp)Fail,
			passOp: (VkStencilOp)Pass,
			depthFailOp: (VkStencilOp)DepthFail.GetValueOrDefault(Fail),
			compareOp: (VkCompareOp)Compare,
			compareMask: 0xFFFFFFFF,
			writeMask: 0xFFFFFFFF,
			reference: Reference
		);

		#region Overrides
		public readonly override int GetHashCode() =>
			Pass.GetHashCode() ^ Fail.GetHashCode() ^ DepthFail.GetHashCode() ^ Compare.GetHashCode() ^ 
			Reference.GetHashCode();

		public readonly override string ToString() => 
			$"[{Pass}:{Fail}:{DepthFail.GetValueOrDefault(Fail)}:{Compare}:{Reference}]";

		public readonly override bool Equals(object? obj) => (obj is StencilState rs) && (rs == this);

		readonly bool IEquatable<StencilState>.Equals(StencilState other) => other == this;
		#endregion // Overrides

		#region Operators
		public static bool operator == (in StencilState l, in StencilState r) =>
			(l.Pass == r.Pass) && (l.Fail == r.Fail) && (l.DepthFail == r.DepthFail) &&
			(l.Compare == r.Compare) && (l.Reference == r.Reference);

		public static bool operator != (in StencilState l, in StencilState r) =>
			(l.Pass != r.Pass) || (l.Fail != r.Fail) || (l.DepthFail != r.DepthFail) ||
			(l.Compare != r.Compare) || (l.Reference != r.Reference);
		#endregion // Operators
	}
}
