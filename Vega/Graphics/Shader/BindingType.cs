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
	/// Represents the resource types that can be used as bound resources for shader uniforms.
	/// </summary>
	public enum BindingType : uint
	{
		/// <summary>
		/// A <see cref="Graphics.Sampler"/> object used to load sampled texels from a raw texture.
		/// <para>
		/// Part of <see cref="BindingGroup.Samplers"/>.
		/// </para>
		/// </summary>
		Sampler = VkDescriptorType.Sampler,
		/// <summary>
		/// A <see cref="Graphics.Sampler"/> object bound to a specific texture for direct texel loads.
		/// <para>
		/// This type is preferred over separate sampler and texture objects, since it may be more performant on
		/// some implementations.
		/// </para>
		/// <para>
		/// Part of <see cref="BindingGroup.Samplers"/>.
		/// </para>
		/// </summary>
		BoundSampler = VkDescriptorType.CombinedImageSampler,
		/// <summary>
		/// A texture object (subclassed from <see cref="TextureBase"/>), not associated with other objects.
		/// <para>
		/// Part of <see cref="BindingGroup.Textures"/>.
		/// </para>
		/// </summary>
		Texture = VkDescriptorType.SampledImage,
		/// <summary>
		/// An input attachment reference, which are handled internally by the library.
		/// <para>
		/// Part of <see cref="BindingGroup.InputAttachments"/>.
		/// </para>
		/// </summary>
		InputAttachment = VkDescriptorType.InputAttachment
	}
}
