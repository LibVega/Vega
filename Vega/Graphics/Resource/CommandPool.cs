/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using Vulkan;

namespace Vega.Graphics
{
	// Manages command buffer resources for a single thread
	// There are two pools of command buffers available to each thread: transient and managed
	//   * Transient - These are per-frame, and can only be used within the frame they are allocated in. Their pools
	//                 are bulk reset at the start of each frame, making them very cheap, and they should be used as
	//                 often as possible.
	//   * Managed - These are allocated from a single pool that exists across all frames. Their lifetimes need to be
	//               carefully tracked, and they need to be manually returned if allocated, but not submitted. If
	//               submitted, they are tracked by the receiving DeviceQueue instance, and are returned to the pool
	//               once execution is complete. Because these may contend with the main thread returning them to the
	//               pool, there is a fastlock mechanism protecting the stack of buffers for both allocations and
	//               returns.
	internal unsafe sealed class CommandPool : IDisposable
	{
		// The pool grow size
		public const int GROW_SIZE = 16;

		#region Fields
		public readonly GraphicsDevice Graphics;

		// Pool and buffers for managed (frame-crossing) commands
		private readonly VkCommandPool _managedPool;
		private readonly Stack<CommandBuffer> _managedPrimaries = new(32);
		private readonly Stack<CommandBuffer> _managedSecondaries = new(32);
		private readonly FastMutex _lockPrimary = new();
		private readonly FastMutex _lockSecondary = new();

		// Pools and buffers for transient (per-frame) commands
		private readonly VkCommandPool[] _transientPools;
		private readonly List<CommandBuffer>[] _transientPrimaries;
		private readonly uint[] _transientPrimaryOffsets;
		private readonly List<CommandBuffer>[] _transientSecondaries;
		private readonly uint[] _transientSecondaryOffsets;
		private uint _frameIndex = 0;

		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		public CommandPool(GraphicsDevice gs)
		{
			Graphics = gs;

			// Create the managed pool
			VkCommandPoolCreateInfo cpci = new(
				flags: VkCommandPoolCreateFlags.ResetCommandBuffer, 
				queueFamilyIndex: gs.GraphicsQueue.FamilyIndex
			);
			VulkanHandle<VkCommandPool> poolHandle;
			gs.VkDevice.CreateCommandPool(&cpci, null, &poolHandle)
				.Throw("Failed to create thread command pool");
			_managedPool = new(poolHandle, gs.VkDevice);

			// Create the transient pools
			_transientPools = new VkCommandPool[GraphicsDevice.MAX_PARALLEL_FRAMES];
			_transientPrimaries = new List<CommandBuffer>[GraphicsDevice.MAX_PARALLEL_FRAMES];
			_transientPrimaryOffsets = new uint[GraphicsDevice.MAX_PARALLEL_FRAMES];
			_transientSecondaries = new List<CommandBuffer>[GraphicsDevice.MAX_PARALLEL_FRAMES];
			_transientSecondaryOffsets = new uint[GraphicsDevice.MAX_PARALLEL_FRAMES];
			cpci.Flags = VkCommandPoolCreateFlags.Transient;
			for (int i = 0; i < _transientPools.Length; ++i) {
				gs.VkDevice.CreateCommandPool(&cpci, null, &poolHandle)
					.Throw("Failed to create per-frame thread command pool");
				_transientPools[i] = new(poolHandle, gs.VkDevice);
				_transientPrimaries[i] = new(32);
				_transientPrimaryOffsets[i] = 0;
				_transientSecondaries[i] = new(32);
				_transientSecondaryOffsets[i] = 0;
			}

			// Initial allocations
			grow(VkCommandBufferLevel.Primary, null);
			grow(VkCommandBufferLevel.Secondary, null);
			for (int i = 0; i < _transientPools.Length; ++i) {
				grow(VkCommandBufferLevel.Primary, (uint)i);
				grow(VkCommandBufferLevel.Secondary, (uint)i);
			}
		}
		~CommandPool()
		{
			dispose(false);
		}

		// Moves/prepares the command pool to point at the next frame
		public void NextFrame()
		{
			// Advance frame
			_frameIndex = (_frameIndex + 1) % (uint)_transientPools.Length;

			// Reset frame transient pool
			_transientPools[_frameIndex].ResetCommandPool(VkCommandPoolResetFlags.NoFlags);
		}

		#region Allocate
		public CommandBuffer AllocateManaged(VkCommandBufferLevel level)
		{
			// Get correct objects
			var prim = level == VkCommandBufferLevel.Primary;
			var stack = (prim ? _managedPrimaries : _managedSecondaries);
			var mutex = (prim ? _lockPrimary : _lockSecondary);

			// Check for needed allocation
			if (stack.Count == 0) {
				using (var _ = mutex.AcquireUNSAFE()) {
					grow(level, null);
				}
			}

			// Pop next buffer and return
			using (var _ = mutex.AcquireUNSAFE()) {
				return stack.Pop();
			}
		}

		public CommandBuffer AllocateTransient(VkCommandBufferLevel level)
		{
			// Get correct objects
			var prim = level == VkCommandBufferLevel.Primary;
			var list = (prim ? _transientPrimaries : _transientSecondaries)[_frameIndex];
			var offs = (prim ? _transientPrimaryOffsets : _transientSecondaryOffsets);

			// Check for needed allocation
			if (offs[_frameIndex] == list.Count) {
				grow(level, _frameIndex);
			}

			// Return next buffer
			return list[(int)(offs[_frameIndex]++)];
		}

		// Requires external synchronization
		private void grow(VkCommandBufferLevel level, uint? frameIndex)
		{
			// Allocate new handles
			var pool = frameIndex.HasValue ? _transientPools[frameIndex.Value] : _managedPool;
			VkCommandBufferAllocateInfo cbai = new(
				commandPool: pool,
				level: level,
				commandBufferCount: GROW_SIZE
			);
			var handles = stackalloc VulkanHandle<VkCommandBuffer>[GROW_SIZE];
			Graphics.VkDevice.AllocateCommandBuffers(&cbai, handles)
				.Throw("Failed to allocate more command buffers in thread pool");

			// Add to correct buffer set
			if (frameIndex.HasValue) {
				var list = ((level == VkCommandBufferLevel.Primary) ? _transientPrimaries : _transientSecondaries)[_frameIndex];
				for (int i = 0; i < GROW_SIZE; ++i) {
					list.Add(new(new(handles[i], pool), level, this, true));
				}
			}
			else {
				var list = (level == VkCommandBufferLevel.Primary) ? _managedPrimaries : _managedSecondaries;
				for (int i = 0; i < GROW_SIZE; ++i) {
					list.Push(new(new(handles[i], pool), level, this, false));
				}
			}
		}
		#endregion // Allocate

		public void Return(CommandBuffer buf)
		{
			// Shouldn't happen, this would be an internal library error
			if (buf.Transient) {
				throw new InvalidOperationException("Attempt to return transient command buffer - THIS IS A LIBRARY BUG");
			}

			// Return to the correct stack
			if (buf.Level == VkCommandBufferLevel.Primary) {
				using (var _ = _lockPrimary.AcquireUNSAFE()) {
					_managedPrimaries.Push(buf);
				}
			}
			else {
				using (var _ = _lockSecondary.AcquireUNSAFE()) {
					_managedSecondaries.Push(buf);
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
				// Destroy the pools
				_managedPool.DestroyCommandPool(null);
				foreach (var tpool in _transientPools) {
					tpool.DestroyCommandPool(null);
				}
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
