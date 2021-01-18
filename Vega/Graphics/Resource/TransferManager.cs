/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
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
		public static readonly DataSize MAX_HOST_SIZE = DataSize.FromMega(16);
		public static readonly DataSize INITIAL_HOST_SIZE = DataSize.FromMega(1);

		#region Fields
		// Graphics device
		public readonly GraphicsDevice Graphics;

		// The default host buffer used for transfers under a certain size, otherwise a temp buffer is allocated and
		//    used instead of this
		public HostBuffer Buffer { get; private set; }

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
			Buffer = new((ulong)INITIAL_HOST_SIZE.B);
			Buffer.CanDestroyImmediately = true; // Safe to do since all operations with this are synchronous

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

		// Attempts to resize the host buffer to the next size, up to a certain maximum
		// Returns the new size of the host buffer (or the same size, if the maximum is reached)
		public ulong RequestNextHostSize(ulong targetSize)
		{
			if (Buffer.DataSize >= (ulong)MAX_HOST_SIZE.B) {
				return Buffer.DataSize;
			}
			else {
				var newSize = Buffer.DataSize * 2;
				while ((newSize < targetSize) && (newSize < (ulong)MAX_HOST_SIZE.B)) {
					newSize *= 2;
				}
				Buffer.Dispose();
				Buffer = new(newSize);
				return newSize;
			}
		}

		#region Buffers
		// Sets the buffer data by copying from a prepared host buffer
		// Pass null as bufferType to signal a first-time copy that does not need pipeline barriers
		public void SetBufferData(VkBuffer dstBuffer, ulong dstOff, HostBuffer srcBuffer, ulong srcOff, ulong count,
			ResourceType? bufferType)
		{
			// Record the copy command
			RecordBufferCopy(_cmd, bufferType, srcBuffer.Buffer, srcOff, dstBuffer, dstOff, count);

			// Submit and wait
			Graphics.GraphicsQueue.SubmitRaw(_cmd, _fence);
			var waitHandle = _fence.Handle;
			Graphics.VkDevice.WaitForFences(1, &waitHandle, VkBool32.True, UInt64.MaxValue);

			// Reset pool
			_pool.ResetCommandPool(VkCommandPoolResetFlags.ReleaseResources);
			Graphics.VkDevice.ResetFences(1, &waitHandle);
		}

		// Sets buffer data from raw data
		public void SetBufferData(VkBuffer dstBuffer, ulong dstOff, void* srcData, ulong count, ResourceType? bufferType)
		{
			// Select which host buffer to use
			bool useTmp = (count > Buffer.DataSize) && (count > RequestNextHostSize(count));
			var srcBuffer = useTmp ? new HostBuffer(count) : Buffer;

			// Copy and transfer
			System.Buffer.MemoryCopy(srcData, srcBuffer.DataPtr, srcBuffer.DataSize, count);
			if (useTmp) {
				srcBuffer.CanDestroyImmediately = true;
				// Need Dispose safety to not leak the tmp buffer
				using (srcBuffer) {
					SetBufferData(dstBuffer, dstOff, srcBuffer, 0, count, bufferType);
				}
			}
			else {
				SetBufferData(dstBuffer, dstOff, srcBuffer, 0, count, bufferType);
			}
		}

		// Performs an asynchronous update of a buffer
		public void UpdateBufferAsync(ResourceType bufferType,
			VkBuffer dstBuffer, ulong dstOffset, VkBuffer srcBuffer, ulong srcOffset, ulong count)
		{
			// Allocate a transient command buffer and record
			var cmd = Graphics.Resources.AllocateTransientCommandBuffer(VkCommandBufferLevel.Primary);
			RecordBufferCopy(cmd.Cmd, bufferType, srcBuffer, srcOffset, dstBuffer, dstOffset, count);

			// Submit (no wait for async)
			var _ = Graphics.GraphicsQueue.Submit(cmd);
		}

		// Performs an asynchronout update of a buffer from raw data
		public void UpdateBufferAsync(ResourceType bufferType,
			VkBuffer dstBuffer, ulong dstOffset, void* srcData, ulong count)
		{
			// Allocate a host buffer for the data update
			using HostBuffer srcBuffer = new(count);
			System.Buffer.MemoryCopy(srcData, srcBuffer.DataPtr, count, count);

			// Allocate a transient command buffer and record
			var cmd = Graphics.Resources.AllocateTransientCommandBuffer(VkCommandBufferLevel.Primary);
			RecordBufferCopy(cmd.Cmd, bufferType, srcBuffer.Buffer, 0, dstBuffer, dstOffset, count);

			// Submit (no wait for async)
			var _ = Graphics.GraphicsQueue.Submit(cmd);
		}

		// Record a buffer copy operation into the command buffer
		public void RecordBufferCopy(VkCommandBuffer cmd, ResourceType? dstBufferType,
			VkBuffer srcBuffer, ulong srcOffset, VkBuffer dstBuffer, ulong dstOffset, ulong count)
		{
			// Get pipeline barrier values
			VkPipelineStageFlags srcStage = 0, dstStage = 0;
			VkAccessFlags srcAccess = 0, dstAccess = 0;
			if (dstBufferType.HasValue) {
				GetBarrierStages(dstBufferType.Value, out srcStage, out dstStage);
				GetAccessFlags(dstBufferType.Value, out srcAccess, out dstAccess);
			}

			// Start command
			VkCommandBufferBeginInfo cbbi = new(VkCommandBufferUsageFlags.OneTimeSubmit, null);
			cmd.BeginCommandBuffer(&cbbi);

			// Src barrier
			if (dstBufferType.HasValue) {
				VkBufferMemoryBarrier srcBarrier = new(
					srcAccessMask: srcAccess,
					dstAccessMask: VkAccessFlags.TransferWrite,
					srcQueueFamilyIndex: VkConstants.QUEUE_FAMILY_IGNORED,
					dstQueueFamilyIndex: VkConstants.QUEUE_FAMILY_IGNORED,
					buffer: dstBuffer,
					offset: dstOffset,
					size: count
				);
				cmd.CmdPipelineBarrier(
					srcStage,
					VkPipelineStageFlags.Transfer,
					VkDependencyFlags.ByRegion,
					0, null,
					1, &srcBarrier,
					0, null
				);
			}

			// Create copy command
			VkBufferCopy bc = new(srcOffset, dstOffset, count);
			cmd.CmdCopyBuffer(srcBuffer, dstBuffer, 1, &bc);

			// Last barrier
			if (dstBufferType.HasValue) {
				VkBufferMemoryBarrier dstBarrier = new(
					srcAccessMask: VkAccessFlags.TransferWrite,
					dstAccessMask: dstAccess,
					srcQueueFamilyIndex: VkConstants.QUEUE_FAMILY_IGNORED,
					dstQueueFamilyIndex: VkConstants.QUEUE_FAMILY_IGNORED,
					buffer: dstBuffer,
					offset: dstOffset,
					size: count
				);
				cmd.CmdPipelineBarrier(
					VkPipelineStageFlags.Transfer,
					dstStage,
					VkDependencyFlags.ByRegion,
					0, null,
					1, &dstBarrier,
					0, null
				);
			}

			// End
			cmd.EndCommandBuffer().Throw("Failed to record buffer upload commands");
		}
		#endregion // Buffers

		#region Images
		// Sets the texture data by copying from a prepared host buffer
		// Pass null as imageType to signal a first-time copy that does not need pipeline barriers
		public void SetImageData(VkImage dstImage, TexelFormat fmt, in TextureRegion region, HostBuffer srcBuffer, 
			ulong srcOff, ResourceType imageType, bool discard)
		{
			// Record copy commands
			RecordBufferImageCopy(_cmd, imageType, discard, srcBuffer.Buffer, srcOff, dstImage, fmt, region);

			// Submit and wait
			Graphics.GraphicsQueue.SubmitRaw(_cmd, _fence);
			var waitHandle = _fence.Handle;
			Graphics.VkDevice.WaitForFences(1, &waitHandle, VkBool32.True, UInt64.MaxValue);

			// Reset pool
			_pool.ResetCommandPool(VkCommandPoolResetFlags.ReleaseResources);
			Graphics.VkDevice.ResetFences(1, &waitHandle);
		}

		// Sets image data from raw data
		public void SetImageData(VkImage dstImage, TexelFormat fmt, in TextureRegion region, void* srcData, 
			ResourceType imageType, bool discard)
		{
			// Select which host buffer to use
			ulong count = region.GetDataSize(fmt);
			bool useTmp = (count > Buffer.DataSize) && (count > RequestNextHostSize(count));
			var srcBuffer = useTmp ? new HostBuffer(count) : Buffer;

			// Copy and transfer
			System.Buffer.MemoryCopy(srcData, srcBuffer.DataPtr, srcBuffer.DataSize, count);
			if (useTmp) {
				srcBuffer.CanDestroyImmediately = true;
				// Need Dispose safety to not leak the tmp buffer
				using (srcBuffer) {
					SetImageData(dstImage, fmt, region, srcBuffer, 0, imageType, discard);
				}
			}
			else {
				SetImageData(dstImage, fmt, region, srcBuffer, 0, imageType, discard);
			}
		}

		// Perform asynchronous update of image
		public void UpdateImageAsync(ResourceType imageType, TexelFormat format, bool discard,
			HostBuffer srcBuffer, ulong srcOff, VkImage dstImage, in TextureRegion dstRegion)
		{
			// Allocate transient command buffer and record
			var cmd = Graphics.Resources.AllocateTransientCommandBuffer(VkCommandBufferLevel.Primary);
			RecordBufferImageCopy(cmd.Cmd, imageType, discard, srcBuffer.Buffer, srcOff, dstImage, format, dstRegion);

			// Submit (no wait for async)
			var _ = Graphics.GraphicsQueue.Submit(cmd);
		}

		// Perform asynchronous update of image with raw data
		public void UpdateImageAsync(ResourceType imageType, TexelFormat format, bool discard,
			void* srcData, VkImage dstImage, in TextureRegion dstRegion)
		{
			// Allocate a host buffer for the data update
			var dataSize = dstRegion.GetDataSize(format);
			using HostBuffer srcBuffer = new(dataSize);
			System.Buffer.MemoryCopy(srcData, srcBuffer.DataPtr, dataSize, dataSize);

			// Allocate transient command buffer and record
			var cmd = Graphics.Resources.AllocateTransientCommandBuffer(VkCommandBufferLevel.Primary);
			RecordBufferImageCopy(cmd.Cmd, imageType, discard, srcBuffer.Buffer, 0, dstImage, format, dstRegion);

			// Submit (no wait for async)
			var _ = Graphics.GraphicsQueue.Submit(cmd);
		}

		// Record an image copy operation into the command buffer
		public void RecordBufferImageCopy(VkCommandBuffer cmd, ResourceType dstImageType, bool discardOld,
			VkBuffer srcBuffer, ulong srcOffset, 
			VkImage dstImage, TexelFormat format, in TextureRegion dstRegion)
		{
			// Get barrier values
			VkPipelineStageFlags srcStage = 0, dstStage = 0;
			VkAccessFlags srcAccess = 0, dstAccess = 0;
			GetBarrierStages(dstImageType, out srcStage, out dstStage);
			GetAccessFlags(dstImageType, out srcAccess, out dstAccess);
			GetLayouts(dstImageType, out var srcLayout, out var dstLayout);
			if (discardOld) {
				srcLayout = VkImageLayout.Undefined;
			}

			// Start command and barrier
			VkCommandBufferBeginInfo cbbi = new(VkCommandBufferUsageFlags.OneTimeSubmit);
			cmd.BeginCommandBuffer(&cbbi);
			VkImageMemoryBarrier srcBarrier = new(
				srcAccessMask: srcAccess,
				dstAccessMask: VkAccessFlags.TransferWrite,
				oldLayout: srcLayout,
				newLayout: VkImageLayout.TransferDstOptimal,
				srcQueueFamilyIndex: VkConstants.QUEUE_FAMILY_IGNORED,
				dstQueueFamilyIndex: VkConstants.QUEUE_FAMILY_IGNORED,
				image: dstImage,
				subresourceRange: new(format.GetAspectFlags(), 0, 1, dstRegion.LayerStart, dstRegion.LayerCount)
			);
			cmd.CmdPipelineBarrier(
				srcStage,
				VkPipelineStageFlags.Transfer,
				VkDependencyFlags.ByRegion,
				0, null,
				0, null,
				1, &srcBarrier
			);

			// Copy
			VkBufferImageCopy bic = new(
				srcOffset, 0, 0,
				new(format.GetAspectFlags(), 0, dstRegion.LayerStart, dstRegion.LayerCount),
				dstRegion.Offset,
				dstRegion.Extent
			);
			cmd.CmdCopyBufferToImage(srcBuffer, dstImage, VkImageLayout.TransferDstOptimal, 1, &bic);

			// Transfer back
			VkImageMemoryBarrier dstBarrier = new(
				srcAccessMask: VkAccessFlags.TransferWrite,
				dstAccessMask: dstAccess,
				oldLayout: VkImageLayout.TransferDstOptimal,
				newLayout: dstLayout,
				srcQueueFamilyIndex: VkConstants.QUEUE_FAMILY_IGNORED,
				dstQueueFamilyIndex: VkConstants.QUEUE_FAMILY_IGNORED,
				image: dstImage,
				subresourceRange: new(format.GetAspectFlags(), 0, 1, dstRegion.LayerStart, dstRegion.LayerCount)
			);
			cmd.CmdPipelineBarrier(
				VkPipelineStageFlags.Transfer,
				dstStage,
				VkDependencyFlags.ByRegion,
				0, null,
				0, null,
				1, &dstBarrier
			);

			// End
			cmd.EndCommandBuffer().Throw("Failed to record image upload commands");
		}
		#endregion // Images

		#region Barriers
		private static void GetBarrierStages(ResourceType type, 
			out VkPipelineStageFlags srcStage, out VkPipelineStageFlags dstStage)
		{
			(srcStage, dstStage) = type switch {
				ResourceType.IndexBuffer => (VkPipelineStageFlags.VertexInput, VkPipelineStageFlags.VertexInput),
				ResourceType.VertexBuffer => (VkPipelineStageFlags.VertexShader, VkPipelineStageFlags.VertexInput),
				// Until we (maybe) enforce that sampled textures must be in fragment shaders only, assume the worst
				ResourceType.Texture1D => (VkPipelineStageFlags.FragmentShader, VkPipelineStageFlags.VertexShader),
				ResourceType.Texture2D => (VkPipelineStageFlags.FragmentShader, VkPipelineStageFlags.VertexShader),
				ResourceType.Texture3D => (VkPipelineStageFlags.FragmentShader, VkPipelineStageFlags.VertexShader),
				ResourceType.Texture1DArray => (VkPipelineStageFlags.FragmentShader, VkPipelineStageFlags.VertexShader),
				ResourceType.Texture2DArray => (VkPipelineStageFlags.FragmentShader, VkPipelineStageFlags.VertexShader),
				_ => throw new ArgumentException("LIBRARY BUG - Invalid resource type in transfer buffer")
			};
		}

		private static void GetAccessFlags(ResourceType type, 
			out VkAccessFlags srcFlags, out VkAccessFlags dstFlags)
		{
			(srcFlags, dstFlags) = type switch { 
				ResourceType.IndexBuffer => (VkAccessFlags.IndexRead, VkAccessFlags.IndexRead),
				ResourceType.VertexBuffer => (VkAccessFlags.VertexAttributeRead, VkAccessFlags.VertexAttributeRead),
				ResourceType.Texture1D => (VkAccessFlags.ShaderRead, VkAccessFlags.ShaderRead),
				ResourceType.Texture2D => (VkAccessFlags.ShaderRead, VkAccessFlags.ShaderRead),
				ResourceType.Texture3D => (VkAccessFlags.ShaderRead, VkAccessFlags.ShaderRead),
				ResourceType.Texture1DArray => (VkAccessFlags.ShaderRead, VkAccessFlags.ShaderRead),
				ResourceType.Texture2DArray => (VkAccessFlags.ShaderRead, VkAccessFlags.ShaderRead),
				_ => throw new Exception("LIBRARY BUG - Invalid resource type in transfer buffer")
			};
		}

		private static void GetLayouts(ResourceType? type,
			out VkImageLayout srcLayout, out VkImageLayout dstLayout)
		{
			if (type.HasValue) {
				(srcLayout, dstLayout) = type.Value switch {
					ResourceType.Texture1D => (VkImageLayout.ShaderReadOnlyOptimal, VkImageLayout.ShaderReadOnlyOptimal),
					ResourceType.Texture2D => (VkImageLayout.ShaderReadOnlyOptimal, VkImageLayout.ShaderReadOnlyOptimal),
					ResourceType.Texture3D => (VkImageLayout.ShaderReadOnlyOptimal, VkImageLayout.ShaderReadOnlyOptimal),
					ResourceType.Texture1DArray => (VkImageLayout.ShaderReadOnlyOptimal, VkImageLayout.ShaderReadOnlyOptimal),
					ResourceType.Texture2DArray => (VkImageLayout.ShaderReadOnlyOptimal, VkImageLayout.ShaderReadOnlyOptimal),
					_ => throw new Exception("LIBRARY BUG - Invalid resource type in image layout")
				};
			}
			else {
				// Will not work for storage images if implemented
				srcLayout = VkImageLayout.Undefined;
				dstLayout = VkImageLayout.ShaderReadOnlyOptimal;
			}
		}
		#endregion // Barriers

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
