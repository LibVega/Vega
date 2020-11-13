/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	// Maps to a VkQueue object, and handles submission and synchronization
	//    "raw" submissions are not tracked with submission contexts, usually for internal resources and operations
	//    "tracked" submissions are tracked with contexts, and are used for general recycled command buffers
	internal unsafe sealed class DeviceQueue : IDisposable
	{
		#region Fields
		// The associated graphics service
		public readonly GraphicsService Graphics;
		// The queue object
		public readonly Vk.Queue Queue;
		// The queue family index for the queue
		public readonly uint FamilyIndex;

		// Sync objects
		private readonly object _submitLock = new();

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
		}
		~DeviceQueue()
		{
			dispose(false);
		}

		#region Raw Submits
		// Raw, pre-prepared submission
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

		// Raw, pre-prepared submission
		public Vk.Result SubmitRaw(Vk.SubmitInfo* si, Vk.Handle<Vk.Fence> fence)
		{
			SubmitCount += 1;
			BufferCount += si->CommandBufferCount;
			lock (_submitLock) {
				return Queue.QueueSubmit(1, si, fence);
			}
		}

		// Surface presentation
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

			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
