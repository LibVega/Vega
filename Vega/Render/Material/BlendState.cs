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
	/// Controls how <see cref="Renderer"/> attachment data is updated by graphics pipeline writes.
	/// </summary>
	public sealed record BlendState
	{
		/// <summary>
		/// State that performs no color blending, the incoming color and alpha always replace the buffer values.
		/// </summary>
		public static readonly BlendState Opaque = new();
		/// <summary>
		/// State that performs simple addition blending of the colors, also known as "plus lighter".
		/// </summary>
		public static readonly BlendState Additive =
			new(BlendFactor.SrcAlpha, BlendFactor.One, BlendOp.Add, BlendFactor.One, BlendFactor.One, BlendOp.Add);
		/// <summary>
		/// State that performs multiplicitive blending.
		/// </summary>
		public static readonly BlendState Multiplicative =
			new(BlendFactor.DstColor, BlendFactor.Zero, BlendOp.Add, BlendFactor.DstAlpha, BlendFactor.Zero, BlendOp.Add);
		/// <summary>
		/// State that performs standard alpha-based color blending using the incoming alpha value.
		/// </summary>
		public static readonly BlendState Alpha =
			new(BlendFactor.SrcAlpha, BlendFactor.InvSrcAlpha, BlendOp.Add, BlendFactor.One, BlendFactor.Zero, BlendOp.Add);

		#region Fields
		/// <summary>
		/// The factor for the incoming color.
		/// </summary>
		public BlendFactor SrcColor { get; init; }
		/// <summary>
		/// The factor for the existing color.
		/// </summary>
		public BlendFactor DstColor { get; init; }
		/// <summary>
		/// The operation to perform on the colors, or <c>null</c> to disable color blending.
		/// </summary>
		public BlendOp? ColorOp { get; init; }
		/// <summary>
		/// The factor for the incoming alpha.
		/// </summary>
		public BlendFactor SrcAlpha { get; init; }
		/// <summary>
		/// The factor for the existing alpha.
		/// </summary>
		public BlendFactor DstAlpha { get; init; }
		/// <summary>
		/// The operation to perform on the alphas, or <c>null</c> to disable alpha blending.
		/// </summary>
		public BlendOp? AlphaOp { get; init; }
		/// <summary>
		/// A mask of the channels that are written into the color buffer. This is used even if blending is disabled.
		/// </summary>
		public ColorChannels WriteMask { get; init; }

		/// <summary>
		/// The hash value for the set of blending states.
		/// </summary>
		public int Hash => _hash ?? (_hash = buildHash()).Value;
		private int? _hash = null;
		#endregion // Fields

		/// <summary>
		/// Describes a new color blend state.
		/// </summary>
		/// <param name="srcColor">The blending factor for the source color.</param>
		/// <param name="dstColor">The blending factor for the destination color.</param>
		/// <param name="colorOp">The blending operation for the colors.</param>
		/// <param name="srcAlpha">The blending factor for the source alpha.</param>
		/// <param name="dstAlpha">The blending factor for the destination alpha.</param>
		/// <param name="alphaOp">The blending operation for the alphas.</param>
		/// <param name="mask">The channel mask for writes.</param>
		public BlendState(BlendFactor srcColor, BlendFactor dstColor, BlendOp? colorOp, BlendFactor srcAlpha,
			BlendFactor dstAlpha, BlendOp? alphaOp, ColorChannels mask = ColorChannels.RGBA)
		{
			SrcColor = srcColor;
			DstColor = dstColor;
			ColorOp = colorOp;
			SrcAlpha = srcAlpha;
			DstAlpha = dstAlpha;
			AlphaOp = alphaOp;
			WriteMask = mask;
		}
		/// <summary>
		/// Describes a new color blend state with no blending (opaque), and a specified write mask.
		/// </summary>
		public BlendState(ColorChannels mask) 
			: this(BlendFactor.One, BlendFactor.Zero, null, BlendFactor.One, BlendFactor.Zero, null, mask)
		{ }
		/// <summary>
		/// Describes a default opaque blend mode with all color channels.
		/// </summary>
		public BlendState()
			: this(BlendFactor.One, BlendFactor.Zero, null, BlendFactor.One, BlendFactor.Zero, null, ColorChannels.RGBA)
		{ }

		public override int GetHashCode() => Hash;

		public bool Equals(BlendState? other) => other?.CompareStates(this) ?? false;

		private int buildHash()
		{
			unchecked {
				int hash = SrcColor.GetHashCode();
				hash = ((hash << 5) + hash) ^ DstColor.GetHashCode();
				hash = ((hash << 5) + hash) ^ ColorOp.GetHashCode();
				hash = ((hash << 5) + hash) ^ SrcAlpha.GetHashCode();
				hash = ((hash << 5) + hash) ^ DstAlpha.GetHashCode();
				hash = ((hash << 5) + hash) ^ AlphaOp.GetHashCode();
				hash = ((hash << 5) + hash) ^ WriteMask.GetHashCode();
				return hash;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		internal bool CompareStates(BlendState state) =>
			(Hash == state.Hash) && (SrcColor == state.SrcColor) && (DstColor == state.DstColor) &&
			(ColorOp == state.ColorOp) && (SrcAlpha == state.SrcAlpha) && (DstAlpha == state.DstAlpha) &&
			(AlphaOp == state.AlphaOp) && (WriteMask == state.WriteMask);
	}
}
