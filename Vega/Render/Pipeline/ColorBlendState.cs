/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Render
{
	/// <summary>
	/// Describes how data is combined in color buffers in a Pipeline. Blending is disabled if both <see cref="ColorOp"/>
	/// and <see cref="AlphaOp"/> are <c>null</c>.
	/// </summary>
	public struct ColorBlendState : IEquatable<ColorBlendState>
	{
		/// <summary>
		/// State that performs no color blending, the incoming color and alpha always replace the buffer values.
		/// </summary>
		public static readonly ColorBlendState Opaque = new(ColorChannels.RGBA);
		/// <summary>
		/// State that performs simple addition blending of the colors, also known as "plus lighter".
		/// </summary>
		public static readonly ColorBlendState Additive = 
			new(BlendFactor.SrcAlpha, BlendFactor.One, BlendOp.Add, BlendFactor.One, BlendFactor.One, BlendOp.Add);
		/// <summary>
		/// State that performs multiplicitive blending.
		/// </summary>
		public static readonly ColorBlendState Multiplicative =
			new(BlendFactor.DstColor, BlendFactor.Zero, BlendOp.Add, BlendFactor.DstAlpha, BlendFactor.Zero, BlendOp.Add);
		/// <summary>
		/// State that performs standard alpha-based color blending using the incoming alpha value.
		/// </summary>
		public static readonly ColorBlendState Alpha =
			new(BlendFactor.SrcAlpha, BlendFactor.InvSrcAlpha, BlendOp.Add, BlendFactor.One, BlendFactor.Zero, BlendOp.Add);

		#region Fields
		/// <summary>
		/// The factor for the incoming color.
		/// </summary>
		public BlendFactor SrcColor;
		/// <summary>
		/// The factor for the existing color.
		/// </summary>
		public BlendFactor DstColor;
		/// <summary>
		/// The operation to perform on the colors, or <c>null</c> to disable color blending.
		/// </summary>
		public BlendOp? ColorOp;
		/// <summary>
		/// The factor for the incoming alpha.
		/// </summary>
		public BlendFactor SrcAlpha;
		/// <summary>
		/// The factor for the existing alpha.
		/// </summary>
		public BlendFactor DstAlpha;
		/// <summary>
		/// The operation to perform on the alphas, or <c>null</c> to disable alpha blending.
		/// </summary>
		public BlendOp? AlphaOp;
		/// <summary>
		/// A mask of the channels that are written into the color buffer. This is used even if blending is disabled.
		/// </summary>
		public ColorChannels WriteMask;
		#endregion // Fields

		/// <summary>
		/// Describes a new color blend state with duplicate factors and operations on color and alpha.
		/// </summary>
		public ColorBlendState(BlendFactor src, BlendFactor dst, BlendOp? op, ColorChannels mask = ColorChannels.RGBA)
		{
			SrcColor = SrcAlpha = src;
			DstColor = DstAlpha = dst;
			ColorOp = AlphaOp = op;
			WriteMask = mask;
		}
		/// <summary>
		/// Describes a new color blend state.
		/// </summary>
		public ColorBlendState(BlendFactor srcColor, BlendFactor dstColor, BlendOp colorOp, BlendFactor srcAlpha,
			BlendFactor dstAlpha, BlendOp alphaOp, ColorChannels mask = ColorChannels.RGBA)
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
		/// Describes a new color blend state with no blending, and a specified write mask.
		/// </summary>
		public ColorBlendState(ColorChannels mask) : this()
		{
			WriteMask = mask;
		}

		// Fill vulkan info object
		internal void ToVk(out VkPipelineColorBlendAttachmentState vk) => vk = new(
			blendEnable: ColorOp.HasValue || AlphaOp.HasValue,
			srcColorBlendFactor: ColorOp.HasValue ? (VkBlendFactor)SrcColor : VkBlendFactor.One,
			dstColorBlendFactor: ColorOp.HasValue ? (VkBlendFactor)DstColor : VkBlendFactor.Zero,
			colorBlendOp: (VkBlendOp)ColorOp.GetValueOrDefault(BlendOp.Add),
			srcAlphaBlendFactor: AlphaOp.HasValue ? (VkBlendFactor)SrcAlpha : VkBlendFactor.One,
			dstAlphaBlendFactor: AlphaOp.HasValue ? (VkBlendFactor)DstAlpha : VkBlendFactor.Zero,
			alphaBlendOp: (VkBlendOp)AlphaOp.GetValueOrDefault(BlendOp.Add),
			colorWriteMask: (VkColorComponentFlags)WriteMask
		);

		#region Overrides
		public readonly override int GetHashCode() =>
			SrcColor.GetHashCode() ^ DstColor.GetHashCode() ^ ColorOp.GetHashCode() ^ SrcAlpha.GetHashCode() ^
			DstAlpha.GetHashCode() ^ AlphaOp.GetHashCode() ^ WriteMask.GetHashCode();

		public readonly override string ToString() => 
			$"[{SrcColor}:{DstColor}:{ColorOp.GetValueOrDefault()}:{SrcAlpha}:{DstAlpha}:{AlphaOp.GetValueOrDefault()}:{WriteMask}]";

		public readonly override bool Equals(object? obj) => (obj is ColorBlendState rs) && (rs == this);

		readonly bool IEquatable<ColorBlendState>.Equals(ColorBlendState other) => other == this;
		#endregion // Overrides

		#region Operators
		public static bool operator == (in ColorBlendState l, in ColorBlendState r) =>
			(l.SrcColor == r.SrcColor) && (l.DstColor == r.DstColor) && (l.ColorOp == r.ColorOp) &&
			(l.SrcAlpha == r.SrcAlpha) && (l.DstAlpha == r.DstAlpha) && (l.AlphaOp == r.AlphaOp) &&
			(l.WriteMask == r.WriteMask);

		public static bool operator != (in ColorBlendState l, in ColorBlendState r) =>
			(l.SrcColor != r.SrcColor) || (l.DstColor != r.DstColor) || (l.ColorOp != r.ColorOp) ||
			(l.SrcAlpha != r.SrcAlpha) || (l.DstAlpha != r.DstAlpha) || (l.AlphaOp != r.AlphaOp) ||
			(l.WriteMask != r.WriteMask);
		#endregion // Operators
	}
}
