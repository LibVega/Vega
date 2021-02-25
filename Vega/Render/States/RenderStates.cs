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
	/// <item><see cref="Winding"/> - <see cref="Winding.CW"/></item>
	/// <item><see cref="RestartEnabled"/> - <c>false</c></item>
	/// <item><see cref="LineWidth"/> - <c>1.0f</c></item>
	/// <item><see cref="StencilState"/> - <c>null</c></item>
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
		/// The vertex winding order that specifies the "front face".
		/// </summary>
		public Winding Winding { get; init; } = Winding.CW;

		/// <summary>
		/// If primitive stream resetting is enabled using <see cref="UInt16.MaxValue"/> or <see cref="UInt32.MaxValue"/>.
		/// </summary>
		public bool RestartEnabled { get; init; } = false;

		/// <summary>
		/// The width of line primitives, or <c>null</c> to use the default of <c>1.0f</c>. Values other than 
		/// <c>null</c> requires <see cref="Graphics.GraphicsFeatures.WideLines"/>.
		/// </summary>
		public float LineWidth { get; init; } = 1.0f;

		/// <summary>
		/// If the current renderer has a stencil buffer, this describes what stencil operations to perform.
		/// </summary>
		public StencilState? StencilState { get; init; } = null;

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
		/// <param name="winding">The primitive winding that defines a front face.</param>
		/// <param name="restartEnabled">If primitive stream restart is supported.</param>
		/// <param name="lineWidth">The width of line primitives.</param>
		/// <param name="stencilState">The stencil buffer state.</param>
		public RenderStates(
			FillMode fillMode = FillMode.Solid,
			CullMode cullMode = CullMode.None,
			Winding winding = Winding.CW,
			bool restartEnabled = false,
			float lineWidth = 1.0f,
			in StencilState? stencilState = null
		)
		{
			FillMode = fillMode;
			CullMode = cullMode;
			Winding = winding;
			RestartEnabled = restartEnabled;
			LineWidth = lineWidth;
			StencilState = stencilState;
		}
		/// <summary>
		/// Create a set of default render states.
		/// </summary>
		public RenderStates()
		{ }

		#region Overrides
		public override int GetHashCode() => Hash;

		public bool Equals(RenderStates? obj) => obj?.CompareStates(this) ?? false;
		#endregion // Overrides

		private int buildHash()
		{
			unchecked {
				int hash = FillMode.GetHashCode();
				hash = ((hash << 5) + hash) ^ CullMode.GetHashCode();
				hash = ((hash << 5) + hash) ^ Winding.GetHashCode();
				hash = ((hash << 5) + hash) ^ RestartEnabled.GetHashCode();
				hash = ((hash << 5) + hash) ^ LineWidth.GetHashCode();
				hash = ((hash << 5) + hash) ^ StencilState.GetHashCode();
				return hash;
			}
		}

		// Compare the state values
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		internal bool CompareStates(RenderStates states) =>
			(Hash == states.Hash) && (FillMode == states.FillMode) && (CullMode == states.CullMode) &&
			(Winding == states.Winding) && (RestartEnabled == states.RestartEnabled) &&
			(LineWidth == states.LineWidth) && (StencilState == states.StencilState);
	}
}
