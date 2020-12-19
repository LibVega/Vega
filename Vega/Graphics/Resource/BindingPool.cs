/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	// Manages a descriptor pool that binding sets can be allocated from
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
		private PoolNode CurrentPool;

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
			CurrentPool = new(this);
		}
		~BindingPool()
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
				CurrentPool?.Handle?.DestroyDescriptorPool(null);
			}
			IsDisposed = true;
		}
		#endregion // IDisposable

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
			#endregion // Fields

			public PoolNode(BindingPool parent)
			{
				// Set values
				Parent = parent;
				SetCount = SET_COUNT;
				Counts = parent.DefaultCounts;
				LastUse = 0;

				// Create pool
				Handle = AllocateNewPool(parent.Graphics, SetCount, Counts);
			}
		}
	}
}
