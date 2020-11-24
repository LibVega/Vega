﻿/*
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
	// Locking is minimized by using a separate pull stack (allocation) and return stack (return), then swaping
	//    atomically when the pull stack is empty. If both are empty, then additional buffers are allocated into
	//    the pull stack.
	internal unsafe sealed class CommandPool : IDisposable
	{
		// The pool grow size
		public const int GROW_SIZE = 10;

		#region Fields
		public readonly GraphicsDevice Graphics;

		// Pool and buffers
		private readonly VkCommandPool _pool;
		private Stack<CommandBuffer> _priAlloc = new(100);
		private Stack<CommandBuffer> _secAlloc = new(100);
		private Stack<CommandBuffer> _priReuse = new(100);
		private Stack<CommandBuffer> _secReuse = new(100);

		// Stack swap locks
		private readonly FastMutex _priLock = new();
		private readonly FastMutex _secLock = new();

		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		public CommandPool(GraphicsDevice gs)
		{
			Graphics = gs;

			// Create the pools
			VkCommandPoolCreateInfo cpci = new(
				flags: VkCommandPoolCreateFlags.ResetCommandBuffer, 
				queueFamilyIndex: gs.GraphicsQueue.FamilyIndex
			);
			VulkanHandle<VkCommandPool> poolHandle;
			gs.VkDevice.CreateCommandPool(&cpci, null, &poolHandle)
				.Throw("Failed to create transient command pool");
			_pool = new(poolHandle, gs.VkDevice);

			// Initial allocations
			grow(VkCommandBufferLevel.Primary);
			grow(VkCommandBufferLevel.Secondary);
		}
		~CommandPool()
		{
			dispose(false);
		}

		#region Allocate
		public CommandBuffer AllocatePrimary()
		{
			// Check for swap or allocate
			if (_priAlloc.Count == 0) {
				if (_priReuse.Count > 0) { // New stack available, do a swap
					using (var _ = _priLock.AcquireUNSAFE()) {
						var newAlloc = _priReuse;
						_priReuse = _priAlloc;
						_priAlloc = newAlloc;
					}
				}
				else { // None are available, allocate new buffers
					grow(VkCommandBufferLevel.Primary);
				}
			}
			return _priAlloc.Pop();
		}

		public CommandBuffer AllocateSecondary()
		{
			// Check for swap or allocate
			if (_secAlloc.Count == 0) {
				if (_secReuse.Count > 0) { // New stack available, do a swap
					using (var _ = _secLock.AcquireUNSAFE()) {
						var newAlloc = _secReuse;
						_secReuse = _secAlloc;
						_secAlloc = newAlloc;
					}
				}
				else { // None are available, allocate new buffers
					grow(VkCommandBufferLevel.Secondary);
				}
			}
			return _secAlloc.Pop();
		}

		private void grow(VkCommandBufferLevel level)
		{
			VkCommandBufferAllocateInfo cbai = new(
				commandPool: _pool,
				level: level,
				commandBufferCount: GROW_SIZE
			);
			var handles = stackalloc VulkanHandle<VkCommandBuffer>[GROW_SIZE];
			Graphics.VkDevice.AllocateCommandBuffers(&cbai, handles)
				.Throw("Failed to allocate more command buffers in thread pool");
			var stack = (level == VkCommandBufferLevel.Primary) ? _priAlloc : _secAlloc;
			for (int i = 0; i < GROW_SIZE; ++i) {
				stack.Push(new(new(handles[i], _pool), level, this));
			}
		}
		#endregion // Allocate

		public void Return(CommandBuffer buf)
		{
			if (buf.Level == VkCommandBufferLevel.Primary) {
				using (var _ = _priLock.AcquireUNSAFE()) {
					_priReuse.Push(buf);
				}
			}
			else {
				using (var _ = _secLock.AcquireUNSAFE()) {
					_secReuse.Push(buf);
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
				// Destroy the pool
				_pool.DestroyCommandPool(null);
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}