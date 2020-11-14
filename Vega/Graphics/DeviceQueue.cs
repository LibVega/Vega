/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using Vk.Extras;

namespace Vega.Graphics
{
	// Maps to a VkQueue object, and handles submission and synchronization
	//    "raw" submissions are not tracked with submission contexts, usually for internal resources and operations
	//    "tracked" submissions are tracked with contexts, and are used for general recycled command buffers
	internal unsafe sealed class DeviceQueue : IDisposable
	{
		private const int GROW_SIZE = 16;

		#region Fields
		// The associated graphics service
		public readonly GraphicsService Graphics;
		// The queue object
		public readonly Vk.Queue Queue;
		// The queue family index for the queue
		public readonly uint FamilyIndex;

		// Sync objects
		private readonly object _submitLock = new();

		// Submit context objects
		private readonly Stack<SubmitContext> _available = new(100);
		private readonly Queue<SubmitContext> _pending = new(100);
		private readonly FastMutex _poolMutex = new();

		#region Info
		// The number of Submit*() calls over the queue lifetime
		public ulong SubmitCount { get; private set; } = 0;
		// The number of command buffers submitted for execution over the queue lifetime
		public ulong BufferCount { get; private set; } = 0;
		#endregion // Info

		// Disposed flag
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		public DeviceQueue(GraphicsService gs, Vk.Queue queue, uint index)
		{
			Graphics = gs;
			Queue = queue;
			FamilyIndex = index;

			growPool();
		}
		~DeviceQueue()
		{
			dispose(false);
		}

		#region Context Management
		// Gets a free submit context, automatically moving it to the pending queue
		private SubmitContext allocateContext()
		{
			using (var _ = _poolMutex.AcquireUNSAFE()) {
				if (_available.TryPop(out var ctx)) {
					_pending.Enqueue(ctx);
					return ctx;
				}
				else {
					growPool();
					_pending.Enqueue(ctx = _available.Pop());
					return ctx;
				}
			}
		}

		// Called at the end of the frame to find and return completed command buffers
		public void UpdateContexts()
		{
			if (_pending.Count == 0) {
				return;
			}

			using (var _ = _poolMutex.AcquireUNSAFE()) {
				// Exit once we find an unfinished context, as the future ones likely aren't done either
				while (_pending.TryPeek(out var ctx) && ctx.TryRelease()) {
					_available.Push(_pending.Dequeue());
				}
			}
		}

		// Allocates more submission contexts (REQUIRES EXTERNAL SYNCHRONIZATION)
		private void growPool()
		{
			for (int i = 0; i < GROW_SIZE; ++i) {
				_available.Push(new(this));
			}
		}
		#endregion // Context Management

		#region Tracked Submits
		// Submit a single command buffer with optional signal semaphore
		public Vk.Fence Submit(CommandBuffer cmd, Vk.Semaphore? signalSem = null)
		{
			var ctx = allocateContext();
			ctx.Prepare(cmd);

			SubmitCount += 1;
			BufferCount += 1;
			var cmdHandle = cmd.Cmd.Handle;
			var semHandle = signalSem ? signalSem!.Handle : Vk.Handle<Vk.Semaphore>.Null;
			Vk.SubmitInfo si = new(
				waitSemaphoreCount: 0,
				waitSemaphores: null,
				waitDstStageMask: null,
				commandBufferCount: 1,
				commandBuffers: &cmdHandle,
				signalSemaphoreCount: signalSem ? 1 : 0,
				signalSemaphores: &semHandle
			);
			SubmitRaw(&si, ctx.Fence);
			return ctx.Fence;
		}

		// Submit a single command buffer with wait semaphore, and optional signal semaphore
		public Vk.Fence Submit(CommandBuffer cmd, Vk.Semaphore waitSem, Vk.PipelineStageFlags waitStages, 
			Vk.Semaphore? signalSem = null)
		{
			var ctx = allocateContext();
			ctx.Prepare(cmd);

			SubmitCount += 1;
			BufferCount += 1;
			var cmdHandle = cmd.Cmd.Handle;
			var waitHandle = waitSem ? waitSem.Handle : Vk.Handle<Vk.Semaphore>.Null;
			var sigHandle = signalSem ? signalSem!.Handle : Vk.Handle<Vk.Semaphore>.Null;
			Vk.SubmitInfo si = new(
				waitSemaphoreCount: waitSem ? 1 : 0,
				waitSemaphores: &waitHandle,
				waitDstStageMask: &waitStages,
				commandBufferCount: 1,
				commandBuffers: &cmdHandle,
				signalSemaphoreCount: signalSem ? 1 : 0,
				signalSemaphores: &sigHandle
			);
			SubmitRaw(&si, ctx.Fence);
			return ctx.Fence;
		}
		#endregion // Tracked Submits

		#region Raw Submits
		public Vk.Result SubmitRaw(in Vk.SubmitInfo si, Vk.Handle<Vk.Fence> fence)
		{
			SubmitCount += 1;
			BufferCount += si.CommandBufferCount;
			lock (_submitLock) {
				fixed (Vk.SubmitInfo* siptr = &si) {
					return Queue.QueueSubmit(1, siptr, fence);
				}
			}
		}

		public Vk.Result SubmitRaw(Vk.SubmitInfo* si, Vk.Handle<Vk.Fence> fence)
		{
			SubmitCount += 1;
			BufferCount += si->CommandBufferCount;
			lock (_submitLock) {
				return Queue.QueueSubmit(1, si, fence);
			}
		}

		public Vk.Result Present(Vk.KHR.PresentInfo* pi)
		{
			lock (_submitLock) {
				return Queue.QueuePresentKHR(pi);
			}
		}
		#endregion // Raw Submits

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
					Graphics.Device.DeviceWaitIdle();
				}
				foreach (var ctx in _available) {
					ctx.Destroy();
				}
				foreach (var ctx in _pending) {
					ctx.Destroy();
				}
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
