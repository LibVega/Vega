/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vulkan;

namespace Vega.Graphics
{
	// Manages a descriptor pool that binding sets can be allocated from
	// Allocation is done with the following steps:
	//   * Attempt to allocate from `_currentPool`, move to end of `_usedPools` on failure
	//   * Check if start of `_usedPools` can be set to `_currentPool`, otherwise allocate new pool for `_currentPool`
	internal unsafe sealed class BindingPool : IDisposable
	{
		// Default counts for sets and resource types
		public const ushort SET_COUNT = 128;
		public const ushort TYPE_COUNT = 256;
		public const ushort INPUT_ATTACHMENT_COUNT = 128;

		#region Fields
		// The graphics device (Core.Instance not available when created)
		public readonly GraphicsDevice Graphics;
		// The binding group represented by this pool
		public readonly BindingGroup Group;

		// The default binding counts for the pool
		public readonly BindingCounts DefaultCounts;

		// The pools
		private PoolNode _currentPool; // The current allocation pool
		private readonly Queue<PoolNode> _usedPools = new(); // The used allocation pools

		// Allocation lock
		private readonly FastMutex _allocateMutex = new();

		// Disposed flag
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		public BindingPool(GraphicsDevice device, BindingGroup group)
		{
			Graphics = device;
			Group = group;

			// Create the counts based on the group type
			DefaultCounts = group switch {
				BindingGroup.Buffers => new(),
				BindingGroup.Samplers => new(sampler: TYPE_COUNT, boundSampler: TYPE_COUNT),
				BindingGroup.Textures => new(texture: TYPE_COUNT),
				BindingGroup.InputAttachments => new(inputAttachment: INPUT_ATTACHMENT_COUNT),
				_ => throw new Exception("LIBRARY BUG - Invalid group for binding pool")
			};

			// Create the initial pool
			_currentPool = new(this);
		}
		~BindingPool()
		{
			dispose(false);
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public BindingSet Allocate(in BindingCounts counts, VkDescriptorSetLayout layout)
		{
			const uint RESET_AGE = GraphicsDevice.MAX_PARALLEL_FRAMES - 1;

			var frame = AppTime.FrameCount;

			// Lock on allocate
			// A fast lock should be okay for _currentPool and reset allocations, but will be less than ideal for
			//   new-pool allocations - but these should happen infrequently for consistent rendering tasks
			using (var _ = _allocateMutex.AcquireUNSAFE()) {
				// Try to allocate from the current pool
				if (_currentPool.Check(counts)) {
					var set = AllocateSet(Graphics, _currentPool, counts, layout);
					_currentPool.LastUse = frame;
					return new(set, frame);
				}

				// Check if the next used pool is available
				_usedPools.Enqueue(_currentPool);
				var age = frame - _usedPools.Peek().LastUse;
				if (age >= RESET_AGE) {
					// Dequeue from used and reset
					_currentPool = _usedPools.Dequeue();
					_currentPool.SetCount = SET_COUNT;
					_currentPool.Counts = DefaultCounts;
					_currentPool.CacheIndex += 1;
					_currentPool.Handle.ResetDescriptorPool(VkDescriptorPoolResetFlags.NoFlags);
				}
				else { // Otherwise create a new pool
					_currentPool = new(this);
				}

				// Allocate from the new current pool
				{
					var set = AllocateSet(Graphics, _currentPool, counts, layout);
					_currentPool.LastUse = frame;
					return new(set, frame);
				}
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
				_currentPool?.Handle?.DestroyDescriptorPool(null);
				foreach (var pool in _usedPools) {
					pool?.Handle?.DestroyDescriptorPool(null);
				}
				_usedPools.Clear();
			}
			IsDisposed = true;
		}
		#endregion // IDisposable

		// Perform a set allocation for a given pool
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static VulkanHandle<VkDescriptorSet> AllocateSet(GraphicsDevice device, PoolNode pool, 
			in BindingCounts counts, VkDescriptorSetLayout layout)
		{
			// Update pool counts
			pool.Counts.Remove(counts);
			pool.SetCount -= 1;

			// Allocate the set
			var handle = layout.Handle;
			VkDescriptorSetAllocateInfo dsai = new(
				descriptorPool: pool.Handle,
				descriptorSetCount: 1,
				setLayouts: &handle
			);
			VulkanHandle<VkDescriptorSet> setHandle;
			device.VkDevice.AllocateDescriptorSets(&dsai, &setHandle);
			return setHandle;
		}

		// Allocate a new pool with the given set and resource counts
		private static VkDescriptorPool AllocateNewPool(GraphicsDevice device, uint sets, in BindingCounts counts)
		{
			// Prepare type counts
			var sizes = stackalloc VkDescriptorPoolSize[BindingCounts.TYPE_COUNT];
			counts.PopulatePoolSizes(sizes, out var sizeCounts);

			// Create pool
			VkDescriptorPoolCreateInfo dpci = new(
				flags: VkDescriptorPoolCreateFlags.NoFlags,
				maxSets: sets,
				poolSizeCount: (uint)sizeCounts,
				poolSizes: sizes
			);
			VulkanHandle<VkDescriptorPool> handle;
			device.VkDevice.CreateDescriptorPool(&dpci, null, &handle).Throw("Failed to allocate descriptor pool");
			return new(handle, device.VkDevice);
		}

		// Represents a specific pool and associated info 
		private class PoolNode
		{
			#region Fields
			// The parent pool
			public readonly BindingPool Parent;
			// The current counts
			public ushort SetCount;
			public BindingCounts Counts;
			// The pool handle
			public readonly VkDescriptorPool Handle;
			// The last use frame index for detecting when a pool can be reused
			public ulong LastUse;
			// Incremented on reset, may allow future optimizations such as set caching
			public uint CacheIndex;
			#endregion // Fields

			public PoolNode(BindingPool parent)
			{
				// Set values
				Parent = parent;
				SetCount = SET_COUNT;
				Counts = parent.DefaultCounts;
				LastUse = 0;
				CacheIndex = 0;

				// Create pool
				Handle = AllocateNewPool(parent.Graphics, SetCount, Counts);
			}

			public bool Check(in BindingCounts counts) => (SetCount > 0) && Counts.Check(counts);
		}
	}
}
