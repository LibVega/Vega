/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vega.Util;
using Vulkan;

namespace Vega.Graphics
{
	// Manages an instance of the global binding table used by VSL for all non-uniform non-subpass-input resources
	// This is an implementation of a bind-less (bind-once) design using descriptor tables to store all
	//     shader-accessible resources at once, thus reducing descriptor set bind calls to a minimum
	// All resources that could be accessible to shaders get a unique (or multiple unique) slots in the table, which is
	//     then sent to shaders in the push constants and used to index the global tables
	// The descriptor indexing features are required to get the larger descriptor table size limits, and to be able to
	//     update the descriptor set with new resources while it is being actively used
	// The descriptor table sizes are defaulted to the VSL table sizes, which are given below
	// The descriptor type indices are:
	//     0) COMBINED_IMAGE_SAMPLER  - built-in samplers paired with textures in unique indices, default 8192
	//     1) STORAGE_IMAGE  -  storage images, default 128
	//     2) STORAGE_BUFFER  -  storage buffers (both RO and RW), default 512
	//     3) UNIFORM_TEXEL_BUFFER  -  readonly texel buffers, default 128
	//     4) STORAGE_TEXEL_BUFFER  -  readwrite texel buffers, default 128
	internal unsafe sealed class BindingTable : IDisposable
	{
		// Default sizes
		private const uint DEFAULT_SIZE_SAMPLER = 8192;
		private const uint DEFAULT_SIZE_IMAGE = 128;
		private const uint DEFAULT_SIZE_BUFFER = 512;
		private const uint DEFAULT_SIZE_ROTEXELS = 128;
		private const uint DEFAULT_SIZE_RWTEXELS = 128;
		// Default flags
		private const VkDescriptorBindingFlags BINDING_FLAGS =
			VkDescriptorBindingFlags.PartiallyBound | VkDescriptorBindingFlags.UpdateUnusedWhilePending;

		#region Fields
		// The device owning this binding table
		public readonly GraphicsDevice GraphicsDevice;

		// The binding layout, pool, and global set
		public readonly VkDescriptorSetLayout LayoutHandle;
		private readonly VkDescriptorPool _pool;
		public readonly VkDescriptorSet SetHandle;

		// Bitsets for tracking table utilization
		private readonly Bitset _samplerMask;
		private readonly Bitset _imageMask;
		private readonly Bitset _bufferMask;
		private readonly Bitset _rotexelMask;
		private readonly Bitset _rwtexelMask;

		// Disposal flag
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		public BindingTable(GraphicsDevice gd)
		{
			GraphicsDevice = gd;

			// Create the table utiliziation sets
			_samplerMask = new(DEFAULT_SIZE_SAMPLER);
			_imageMask = new(DEFAULT_SIZE_IMAGE);
			_bufferMask = new(DEFAULT_SIZE_BUFFER);
			_rotexelMask = new(DEFAULT_SIZE_ROTEXELS);
			_rwtexelMask = new(DEFAULT_SIZE_RWTEXELS);

			// Create the pool sizes and layout description
			var stageFlags = VkShaderStageFlags.Vertex | VkShaderStageFlags.Fragment;
			var layouts = stackalloc VkDescriptorSetLayoutBinding[5] { 
				new(0, VkDescriptorType.CombinedImageSampler, DEFAULT_SIZE_SAMPLER, stageFlags, null),
				new(1, VkDescriptorType.StorageImage, DEFAULT_SIZE_IMAGE, stageFlags, null),
				new(2, VkDescriptorType.StorageBuffer, DEFAULT_SIZE_BUFFER, stageFlags, null),
				new(3, VkDescriptorType.UniformTexelBuffer, DEFAULT_SIZE_ROTEXELS, stageFlags, null),
				new(4, VkDescriptorType.StorageTexelBuffer, DEFAULT_SIZE_RWTEXELS, stageFlags, null)
			};
			var pools = stackalloc VkDescriptorPoolSize[5] {
				new(VkDescriptorType.CombinedImageSampler, DEFAULT_SIZE_SAMPLER),
				new(VkDescriptorType.StorageImage, DEFAULT_SIZE_IMAGE),
				new(VkDescriptorType.StorageBuffer, DEFAULT_SIZE_BUFFER),
				new(VkDescriptorType.UniformTexelBuffer, DEFAULT_SIZE_ROTEXELS),
				new(VkDescriptorType.StorageTexelBuffer, DEFAULT_SIZE_RWTEXELS)
			};

			// Create the standard binding table layout
			VkDescriptorSetLayoutCreateInfo dslci = new(
				flags: VkDescriptorSetLayoutCreateFlags.UpdateAfterBindPool,
				bindingCount: 5,
				bindings: layouts
			);
			var bindingFlags = stackalloc VkDescriptorBindingFlags[5] { 
				BINDING_FLAGS, BINDING_FLAGS, BINDING_FLAGS, BINDING_FLAGS, BINDING_FLAGS
			};
			VkDescriptorSetLayoutBindingFlagsCreateInfo dslbci = new(
				bindingCount: 5,
				bindingFlags: bindingFlags
			);
			dslci.pNext = &dslbci;
			VulkanHandle<VkDescriptorSetLayout> layoutHandle;
			gd.VkDevice.CreateDescriptorSetLayout(&dslci, null, &layoutHandle)
				.Throw("Failed to create layout for global descriptor pool");
			LayoutHandle = new(layoutHandle, gd.VkDevice);

			// Create the pool
			VkDescriptorPoolCreateInfo dpci = new(
				flags: VkDescriptorPoolCreateFlags.UpdateAfterBind,
				maxSets: 1,
				poolSizeCount: 5,
				poolSizes: pools
			);
			VulkanHandle<VkDescriptorPool> poolHandle;
			gd.VkDevice.CreateDescriptorPool(&dpci, null, &poolHandle)
				.Throw("Failed to create global descriptor pool");
			_pool = new(poolHandle, gd.VkDevice);

			// Create the global set
			VkDescriptorSetAllocateInfo dsai = new(
				descriptorPool: poolHandle,
				descriptorSetCount: 1,
				setLayouts: &layoutHandle
			);
			VulkanHandle<VkDescriptorSet> setHandle;
			gd.VkDevice.AllocateDescriptorSets(&dsai, &setHandle).Throw("Failed to allocate global descriptor table");
			SetHandle = new(setHandle, _pool);
		}
		~BindingTable()
		{
			dispose(false);
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed) {
				if (disposing) {
					_pool?.DestroyDescriptorPool(null);
					LayoutHandle?.DestroyDescriptorSetLayout(null); 
				}
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
