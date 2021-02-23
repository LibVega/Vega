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
	/// Describes a collection of rendering state values that, when combined with a <see cref="Material"/>, fully 
	/// describe a graphics pipeline.
	/// <para>
	/// Default state values are:
	/// <list type="bullet">
	/// <item><see cref="FillMode"/> - <see cref="FillMode.Solid"/></item>
	/// <item><see cref="CullMode"/> - <see cref="CullMode.None"/></item>
	/// <item><see cref="LineWidth"/> - <c>1.0f</c></item>
	/// <item><see cref="DepthState"/> - <see cref="DepthState.Default"/></item>
	/// <item><see cref="FrontStencil"/> - <c>null</c></item>
	/// <item><see cref="BackStencil"/> - <c>null</c></item>
	/// </list>
	/// </para>
	/// </summary>
	public sealed record RenderStates
	{
		#region Fields
		/// <summary>
		/// The primitive fill mode. Values other than <see cref="FillMode.Solid"/> requires
		/// <see cref="Graphics.GraphicsFeatures.FillModeNonSolid"/>.
		/// </summary>
		public FillMode FillMode { get; init; } = FillMode.Solid;

		/// <summary>
		/// The face cull mode.
		/// </summary>
		public CullMode CullMode { get; init; } = CullMode.None;

		/// <summary>
		/// The width of line primitives, or <c>null</c> to use the default of <c>1.0f</c>. Values other than 
		/// <c>null</c> requires <see cref="Graphics.GraphicsFeatures.WideLines"/>.
		/// </summary>
		public float LineWidth { get; init; } = 1.0f;

		/// <summary>
		/// The state of the depth operations to perform.
		/// </summary>
		public DepthState DepthState { get; init; } = DepthState.Default;

		/// <summary>
		/// If the current renderer has a stencil buffer, this describes what stencil operations to perform on
		/// front-facing primitives.
		/// </summary>
		public StencilState? FrontStencil { get; init; } = null;

		/// <summary>
		/// If the current renderer has a stencil buffer, this describes what stencil operations to perform on
		/// front-facing primitives.
		/// </summary>
		public StencilState? BackStencil { get; init; } = null;

		/// <summary>
		/// The hash for the collection of render states.
		/// </summary>
		public int Hash => _hash ?? (_hash = buildHash()).Value;
		private int? _hash = null;
		#endregion // Fields

		/// <summary>
		/// Create a fully-defined set of render states.
		/// </summary>
		/// <param name="fillMode">The polygon fill mode.</param>
		/// <param name="cullMode">The polygon winding cull mode.</param>
		/// <param name="lineWidth">The width of line primitives.</param>
		/// <param name="depthState">The depth buffer read/write state.</param>
		/// <param name="frontStencil">The stencil buffer state for front-facing primitives.</param>
		/// <param name="backStencil">The stencil buffer state for back-facing primitives.</param>
		public RenderStates(
			FillMode fillMode,
			CullMode cullMode,
			float lineWidth,
			in DepthState depthState,
			in StencilState? frontStencil,
			in StencilState? backStencil
		)
		{
			FillMode = fillMode;
			CullMode = cullMode;
			LineWidth = lineWidth;
			DepthState = depthState;
			FrontStencil = frontStencil;
			BackStencil = backStencil;
		}

		/// <summary>
		/// Create a default set of render states with a defined depth state and cull mode.
		/// </summary>
		/// <param name="depthState">The depth buffer read/write state.</param>
		/// <param name="cullMode">The polygon winding cull mode.</param>
		public RenderStates(
			in DepthState depthState,
			CullMode cullMode = CullMode.None
		)
		{
			DepthState = depthState;
			CullMode = cullMode;
		}

		#region Overrides
		public override int GetHashCode() => Hash;

		public bool Equals(RenderStates? obj) => obj?.CompareStates(this) ?? false;
		#endregion // Overrides

		private int buildHash()
		{
			// This is how System.Tuple calculates hash codes
			unchecked {
				int hash = FillMode.GetHashCode();
				hash = ((hash << 5) + hash) ^ CullMode.GetHashCode();
				hash = ((hash << 5) + hash) ^ LineWidth.GetHashCode();
				hash = ((hash << 5) + hash) ^ DepthState.GetHashCode();
				hash = ((hash << 5) + hash) ^ FrontStencil.GetHashCode();
				hash = ((hash << 5) + hash) ^ BackStencil.GetHashCode();
				return hash;
			}
		}

		// Compare the state values
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		internal bool CompareStates(RenderStates states) =>
			(Hash == states.Hash) && (FillMode == states.FillMode) && (CullMode == states.CullMode) && 
			(LineWidth == states.LineWidth) && (DepthState == states.DepthState) && 
			(FrontStencil == states.FrontStencil) && (BackStencil == states.BackStencil);
	}
}
