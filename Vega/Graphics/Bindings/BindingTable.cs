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
		public const ushort DEFAULT_SIZE_SAMPLER = 8192;
		public const ushort DEFAULT_SIZE_IMAGE = 128;
		public const ushort DEFAULT_SIZE_BUFFER = 512;
		public const ushort DEFAULT_SIZE_ROTEXELS = 128;
		public const ushort DEFAULT_SIZE_RWTEXELS = 128;
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

		// The shared layout handle for set 1 uniform buffers shared by all shaders (maybe put in better place?)
		public readonly VkDescriptorSetLayout UniformLayoutHandle;
		// The shared layout handle for no-binding sets (used when shaders have gaps in binding sets)
		public readonly VkDescriptorSetLayout BlankLayoutHandle;

		// Bitsets for tracking table utilization
		private readonly Bitset _samplerMask;
		private readonly Bitset _imageMask;
		private readonly Bitset _bufferMask;
		private readonly Bitset _rotexelMask;
		private readonly Bitset _rwtexelMask;

		// Locks for table bitsets
		private readonly FastMutex _samplerMutex = new();
		private readonly FastMutex _imageMutex = new();
		private readonly FastMutex _bufferMutex = new();
		private readonly FastMutex _rotexelsMutex = new();
		private readonly FastMutex _rwtexelsMutex = new();

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

			// Create the uniform binding layout
			VkDescriptorSetLayoutBinding uniformLayout = new(
				0, VkDescriptorType.UniformBufferDynamic, 1, VkShaderStageFlags.AllGraphics, null
			);
			dslci = new(
				flags: VkDescriptorSetLayoutCreateFlags.NoFlags,
				bindingCount: 1,
				bindings: &uniformLayout
			);
			gd.VkDevice.CreateDescriptorSetLayout(&dslci, null, &layoutHandle)
				.Throw("Fauled to create layout for uniform buffer binding");
			UniformLayoutHandle = new(layoutHandle, gd.VkDevice);

			// Create the blank binding layout
			dslci = new(VkDescriptorSetLayoutCreateFlags.NoFlags, 0, null);
			gd.VkDevice.CreateDescriptorSetLayout(&dslci, null, &layoutHandle)
				.Throw("Failed to create blank descriptor layout");
			BlankLayoutHandle = new(layoutHandle, gd.VkDevice);
		}
		~BindingTable()
		{
			dispose(false);
		}

		// Adds a new combined image/sampler entry to the table, returning the index of the new table slot
		public ushort Reserve(TextureBase tex, Sampler sampler)
		{
			// Get the next available index
			ushort index = 0;
			using (var _ = _samplerMutex.AcquireUNSAFE()) {
				index = (ushort)(_samplerMask.FirstClear() ?? throw new InvalidOperationException(
					$"Max number of texture bindings reached ({_samplerMask.Count})"));
				_samplerMask.SetBit(index);
			}

			// Get the handles
			var viewHandle = tex.View.Handle;
			var sampHandle = SamplerPool.Get(sampler).Handle;

			// Update the table
			VkDescriptorImageInfo info = new(
				sampler: sampHandle,
				imageView: viewHandle,
				imageLayout: VkImageLayout.ShaderReadOnlyOptimal
			);
			VkWriteDescriptorSet write = new(
				dstSet: SetHandle,
				dstBinding: 0,
				dstArrayElement: index,
				descriptorCount: 1,
				descriptorType: VkDescriptorType.CombinedImageSampler,
				imageInfo: &info,
				bufferInfo: null,
				texelBufferView: null
			);
			GraphicsDevice.VkDevice.UpdateDescriptorSets(1, &write, 0, null);

			// Return new index
			return index;
		}

		// Releases the given index from the table of the given binding type
		public void Release(BindingTableType type, ushort index)
		{
			var (bitset, mutex) = type switch {
				BindingTableType.Sampler => (_samplerMask, _samplerMutex),
				BindingTableType.Image => (_imageMask, _imageMutex),
				BindingTableType.Buffer => (_bufferMask, _bufferMutex),
				BindingTableType.ROTexels => (_rotexelMask, _rotexelsMutex),
				BindingTableType.RWTexels => (_rwtexelMask, _rwtexelsMutex),
				_ => throw new Exception("LIBRARY BUG - invalid binding type")
			};

			using (var _ = mutex.AcquireUNSAFE()) {
				bitset.ClearBit(index);
			}
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
					UniformLayoutHandle?.DestroyDescriptorSetLayout(null);
					BlankLayoutHandle?.DestroyDescriptorSetLayout(null);
				}
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
