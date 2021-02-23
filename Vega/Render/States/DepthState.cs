/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.CompilerServices;

namespace Vega.Render
{
	/// <summary>
	/// Describes a depth operation that is performed in a graphics pipeline.
	/// </summary>
	public struct DepthState : IEquatable<DepthState>
	{
		/// <summary>
		/// All depth testing and writing is disabled, fragments always pass the depth test.
		/// </summary>
		public static readonly DepthState None = new(DepthMode.None, CompareOp.Always, false);
		/// <summary>
		/// Depth state is test only, and new passing fragments to not overwrite the depth buffer.
		/// </summary>
		public static readonly DepthState TestOnly = new(DepthMode.TestOnly, CompareOp.Less, false);
		/// <summary>
		/// Standard closer-passes depth testing where new fragments write to the depth buffer.
		/// </summary>
		public static readonly DepthState Default = new(DepthMode.Default, CompareOp.Less, false);

		#region Fields
		/// <summary>
		/// The depth buffer mode.
		/// </summary>
		public DepthMode Mode;
		/// <summary>
		/// The depth value comparison operation.
		/// </summary>
		public CompareOp Op;
		/// <summary>
		/// If depth clamping is enabled (clamps out of range depth values, instead of discarding). Requires
		/// <see cref="Graphics.GraphicsFeatures.DepthClamp"/>.
		/// </summary>
		public bool Clamp;
		#endregion // Fields

		/// <summary>
		/// Describe a new depth state.
		/// </summary>
		/// <param name="mode">The depth buffering mode.</param>
		/// <param name="op">The comparison operation to use to define passing depth samples.</param>
		/// <param name="clamp">The depth clamping is enabled</param>
		public DepthState(DepthMode mode, CompareOp op = CompareOp.Less, bool clamp = false)
		{
			Mode = mode;
			Op = op;
			Clamp = clamp;
		}

		#region Overrides
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public readonly override int GetHashCode() => Mode.GetHashCode() ^ Op.GetHashCode() ^ Clamp.GetHashCode();

		public readonly override string ToString() => $"[{Mode}:{Op}:{Clamp}]";

		public readonly override bool Equals(object? obj) => (obj is DepthState ds) && (ds == this);

		readonly bool IEquatable<DepthState>.Equals(DepthState other) => other == this;
		#endregion // Overrides

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static bool operator == (in DepthState l, in DepthState r) =>
			(l.Mode == r.Mode) && (l.Op == r.Op) && (l.Clamp == r.Clamp);

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static bool operator != (in DepthState l, in DepthState r) =>
			(l.Mode != r.Mode) || (l.Op != r.Op) || (l.Clamp != r.Clamp);
		#endregion // Operators
	}
}
