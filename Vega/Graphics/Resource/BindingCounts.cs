/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	// Contains a set of binding type counts for use with binding layouts and pools
	// This needs to be kept up to date with `enum BindingType`
	internal struct BindingCounts
	{
		// The total number of binding types
		public static readonly int TYPE_COUNT = Enum.GetValues<BindingType>().Length;

		#region Fields
		public ushort Sampler;
		public ushort BoundSampler;
		public ushort Texture;
		public ushort InputAttachment;
		#endregion // Fields

		public BindingCounts(
			ushort sampler = 0,
			ushort boundSampler = 0,
			ushort texture = 0,
			ushort inputAttachment = 0
		)
		{
			Sampler = sampler;
			BoundSampler = boundSampler;
			Texture = texture;
			InputAttachment = inputAttachment;
		}

		// Checks if this set of counts is >= all counts in another set
		public bool Check(in BindingCounts other) =>
			(Sampler >= other.Sampler) &&
			(BoundSampler >= other.BoundSampler) &&
			(Texture >= other.Texture) &&
			(InputAttachment >= other.InputAttachment);

		// Subtracts another count set from this one
		public void Remove(in BindingCounts other)
		{
			Sampler -= other.Sampler;
			BoundSampler -= other.BoundSampler;
			Texture -= other.Texture;
			InputAttachment -= other.InputAttachment;
		}

		// Adds another count set to this one
		public void Add(in BindingCounts other)
		{
			Sampler += other.Sampler;
			BoundSampler += other.BoundSampler;
			Texture += other.Texture;
			InputAttachment += other.InputAttachment;
		}

		// Adds a binding type to this set of counts
		public void Add(BindingType type)
		{
			switch (type) {
				case BindingType.Sampler: Sampler += 1; break;
				case BindingType.BoundSampler: BoundSampler += 1; break;
				case BindingType.Texture: Texture += 1; break;
				case BindingType.InputAttachment: InputAttachment += 1; break;
				default: throw new ArgumentException($"LIBRARY BUG - Invalid binding counts type argument");
			}
		}

		// Populates an array of vk pool sizes
		public unsafe void PopulatePoolSizes(VkDescriptorPoolSize* sizes, out int sizeCount)
		{
			sizeCount = 0;
			if (Sampler > 0) {
				sizes[sizeCount++] = new(VkDescriptorType.Sampler, Sampler);
			}
			if (BoundSampler > 0) {
				sizes[sizeCount++] = new(VkDescriptorType.CombinedImageSampler, BoundSampler);
			}
			if (Texture > 0) {
				sizes[sizeCount++] = new(VkDescriptorType.SampledImage, Texture);
			}
			if (InputAttachment > 0) {
				sizes[sizeCount++] = new(VkDescriptorType.InputAttachment, InputAttachment);
			}
		}
	}
}
