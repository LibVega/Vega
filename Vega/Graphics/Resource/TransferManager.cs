/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	// Handles the transfer of data to/from the GPU, mostly for buffer and texture uploads
	// Defines it's own HostBuffer for internal use and API calls that do not use HostBuffers
	internal unsafe sealed class TransferManager : IDisposable
	{
		// 16MB internal host buffer (2k x 2k texture at 4bpp for reference)
		public static readonly DataSize HOST_SIZE = DataSize.FromMega(16);

		#region Fields
		// Graphics device
		public readonly GraphicsDevice Graphics;

		// The host buffer used for transfers
		public readonly HostBuffer Buffer;

		// The command objects used for upload
		// Don't pull from threaded pools, as transfers may happen rapidly and disconnected from the frame sequence,
		//    which can cause the pools to grow uncontrollably
		private readonly VkCommandPool _pool;
		private readonly VkCommandBuffer _cmd;
		private readonly VkFence _fence;

		// Disposed flag
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		public TransferManager(GraphicsDevice graphics)
		{
			Graphics = graphics;
			Buffer = new((ulong)HOST_SIZE.B);

			// Create command objects
			VkCommandPoolCreateInfo cpci = new(VkCommandPoolCreateFlags.Transient, Graphics.GraphicsQueue.FamilyIndex);
			VulkanHandle<VkCommandPool> poolHandle;
			Graphics.VkDevice.CreateCommandPool(&cpci, null, &poolHandle)
				.Throw("Failed to create command pool for transfer");
			_pool = new(poolHandle, Graphics.VkDevice);
			VkCommandBufferAllocateInfo cbai = new(_pool, VkCommandBufferLevel.Primary, 1);
			VulkanHandle<VkCommandBuffer> cmdHandle;
			Graphics.VkDevice.AllocateCommandBuffers(&cbai, &cmdHandle)
				.Throw("Failed to allocate command buffer for transfer");
			_cmd = new(cmdHandle, _pool);
			VkFenceCreateInfo fci = new(VkFenceCreateFlags.NoFlags);
			VulkanHandle<VkFence> fenceHandle;
			Graphics.VkDevice.CreateFence(&fci, null, &fenceHandle)
				.Throw("Failed to create fence for transfer");
			_fence = new(fenceHandle, Graphics.VkDevice);
		}
		~TransferManager()
		{
			dispose(false);
		}

		#region Buffers
		// Sets the buffer data by copying from a prepared host buffer
		// Pass null as bufferType to signal a first-time copy that does not need pipeline barriers
		public void SetBufferData(VkBuffer dstBuffer, ulong dstOff, HostBuffer srcBuffer, ulong srcOff, ulong count,
			ResourceType? bufferType)
		{
			VkPipelineStageFlags srcStage = 0, dstStage = 0;
			VkAccessFlags srcAccess = 0, dstAccess = 0;
			if (bufferType.HasValue) {
				GetBarrierStages(bufferType!.Value, out srcStage, out dstStage);
				GetAccessFlags(bufferType!.Value, out srcAccess, out dstAccess);
			}

			// Start command and barrier
			VkCommandBufferBeginInfo cbbi = new(VkCommandBufferUsageFlags.OneTimeSubmit);
			_cmd.BeginCommandBuffer(&cbbi);
			if (bufferType.HasValue) {
				VkBufferMemoryBarrier srcBarrier = new(
					srcAccessMask: srcAccess,
					dstAccessMask: VkAccessFlags.TransferWrite,
					srcQueueFamilyIndex: VkConstants.QUEUE_FAMILY_IGNORED,
					dstQueueFamilyIndex: VkConstants.QUEUE_FAMILY_IGNORED,
					buffer: dstBuffer,
					offset: dstOff,
					size: count
				);
				_cmd.CmdPipelineBarrier(
					srcStage,
					VkPipelineStageFlags.Transfer,
					VkDependencyFlags.ByRegion,
					0, null,
					1, &srcBarrier,
					0, null
				); 
			}

			// Create copy command
			VkBufferCopy bc = new(srcOff, dstOff, count);
			_cmd.CmdCopyBuffer(srcBuffer.Buffer, dstBuffer, 1, &bc);

			// Last barrier and end
			if (bufferType.HasValue) {
				VkBufferMemoryBarrier dstBarrier = new(
					srcAccessMask: VkAccessFlags.TransferWrite,
					dstAccessMask: dstAccess,
					srcQueueFamilyIndex: VkConstants.QUEUE_FAMILY_IGNORED,
					dstQueueFamilyIndex: VkConstants.QUEUE_FAMILY_IGNORED,
					buffer: dstBuffer,
					offset: dstOff,
					size: count
				);
				_cmd.CmdPipelineBarrier(
					VkPipelineStageFlags.Transfer,
					dstStage,
					VkDependencyFlags.ByRegion,
					0, null,
					1, &dstBarrier,
					0, null
				); 
			}
			_cmd.EndCommandBuffer().Throw("Failed to record buffer upload commands");

			// Submit and wait
			Graphics.GraphicsQueue.SubmitRaw(_cmd, _fence);
			var waitHandle = _fence.Handle;
			Graphics.VkDevice.WaitForFences(1, &waitHandle, VkBool32.True, UInt64.MaxValue);

			// Reset pool
			_pool.ResetCommandPool(VkCommandPoolResetFlags.ReleaseResources);
			Graphics.VkDevice.ResetFences(1, &waitHandle);
		}

		// Sets buffer data using the internal host buffer
		public void SetBufferData(VkBuffer dstBuffer, ulong dstOff, void* srcData, ulong count, ResourceType? bufferType)
		{
			ulong remain = count;
			while (remain > 0) {
				ulong thisCount = Math.Min(remain, Buffer.DataSize);
				ulong thisOffset = count - remain;

				// Copy data into host buffer
				System.Buffer.MemoryCopy((byte*)srcData + thisOffset, Buffer.DataPtr, Buffer.DataSize, thisCount);

				// Copy buffer
				SetBufferData(dstBuffer, thisOffset, Buffer, 0, thisCount, bufferType);

				remain -= thisCount;
			}
		}
		#endregion // Buffers

		private static void GetBarrierStages(ResourceType type, out VkPipelineStageFlags srcStage, 
			out VkPipelineStageFlags dstStage)
		{
			(srcStage, dstStage) = type switch {
				ResourceType.IndexBuffer => (VkPipelineStageFlags.VertexInput, VkPipelineStageFlags.VertexInput),
				ResourceType.VertexBuffer => (VkPipelineStageFlags.VertexShader, VkPipelineStageFlags.VertexInput),
				_ => throw new ArgumentException("LIBRARY BUG - Invalid resource type in transfer buffer")
			};
		}

		private static void GetAccessFlags(ResourceType type, out VkAccessFlags srcFlags, out VkAccessFlags dstFlags)
		{
			(srcFlags, dstFlags) = type switch { 
				ResourceType.IndexBuffer => (VkAccessFlags.IndexRead, VkAccessFlags.IndexRead),
				ResourceType.VertexBuffer => (VkAccessFlags.VertexAttributeRead, VkAccessFlags.VertexAttributeRead),
				_ => throw new ArgumentException("LIBRARY BUG - Invalid resource type in transfer buffer")
			};
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
					Buffer.Dispose();
				}

				_fence.DestroyFence(null);
				_pool.DestroyCommandPool(null); // Also frees _cmd
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
