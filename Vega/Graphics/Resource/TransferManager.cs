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
		public static readonly DataSize HOST_SIZE = DataSize.FromMega(16);

		#region Fields
		// Graphics device
		public readonly GraphicsDevice Graphics;

		// The default host buffer used for transfers under a certain size, otherwise a temp buffer is allocated and
		//    used instead of this
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

		// Sets buffer data from raw data
		public void SetBufferData(VkBuffer dstBuffer, ulong dstOff, void* srcData, ulong count, ResourceType? bufferType)
		{
			// Select which host buffer to use
			bool useTmp = count > (ulong)HOST_SIZE.B;
			var srcBuffer = useTmp ? new HostBuffer(count) : Buffer;

			// Copy and transfer
			System.Buffer.MemoryCopy(srcData, srcBuffer.DataPtr, srcBuffer.DataSize, count);
			if (useTmp) {
				// Need Dispose safety to not leak the tmp buffer
				using (srcBuffer) {
					SetBufferData(dstBuffer, dstOff, srcBuffer, 0, count, bufferType);
				}
			}
			else {
				SetBufferData(dstBuffer, dstOff, srcBuffer, 0, count, bufferType);
			}
		}
		#endregion // Buffers

		#region Images
		// Sets the texture data by copying from a prepared host buffer
		// Pass null as imageType to signal a first-time copy that does not need pipeline barriers
		public void SetImageData(VkImage dstImage, TexelFormat fmt, in TextureRegion region, HostBuffer srcBuffer, 
			ulong srcOff, ResourceType? imageType)
		{
			VkPipelineStageFlags srcStage = 0, dstStage = 0;
			VkAccessFlags srcAccess = 0, dstAccess = 0;
			if (imageType.HasValue) {
				GetBarrierStages(imageType!.Value, out srcStage, out dstStage);
				GetAccessFlags(imageType!.Value, out srcAccess, out dstAccess);
			}
			GetLayouts(imageType, out var srcLayout, out var dstLayout);

			// Start command and barrier
			VkCommandBufferBeginInfo cbbi = new(VkCommandBufferUsageFlags.OneTimeSubmit);
			_cmd.BeginCommandBuffer(&cbbi);
			VkImageMemoryBarrier srcBarrier = new(
				srcAccessMask: srcAccess,
				dstAccessMask: VkAccessFlags.TransferWrite,
				oldLayout: srcLayout,
				newLayout: VkImageLayout.TransferDstOptimal,
				srcQueueFamilyIndex: VkConstants.QUEUE_FAMILY_IGNORED,
				dstQueueFamilyIndex: VkConstants.QUEUE_FAMILY_IGNORED,
				image: dstImage,
				subresourceRange: new(fmt.GetAspectFlags(), 0, 1, region.LayerStart, region.LayerCount)
			);
			_cmd.CmdPipelineBarrier(
				srcStage,
				VkPipelineStageFlags.Transfer,
				VkDependencyFlags.ByRegion,
				0, null,
				0, null,
				1, &srcBarrier
			);

			// Copy
			VkBufferImageCopy bic = new(
				srcOff, 0, 0,
				new(fmt.GetAspectFlags(), 0, region.LayerStart, region.LayerCount),
				region.Offset,
				region.Extent
			);
			_cmd.CmdCopyBufferToImage(srcBuffer.Buffer, dstImage, VkImageLayout.TransferDstOptimal, 1, &bic);

			// Transfer back and end
			VkImageMemoryBarrier dstBarrier = new(
				srcAccessMask: VkAccessFlags.TransferWrite,
				dstAccessMask: dstAccess,
				oldLayout: VkImageLayout.TransferDstOptimal,
				newLayout: dstLayout,
				srcQueueFamilyIndex: VkConstants.QUEUE_FAMILY_IGNORED,
				dstQueueFamilyIndex: VkConstants.QUEUE_FAMILY_IGNORED,
				image: dstImage,
				subresourceRange: new(fmt.GetAspectFlags(), 0, 1, region.LayerStart, region.LayerCount)
			);
			_cmd.CmdPipelineBarrier(
				VkPipelineStageFlags.Transfer,
				dstStage,
				VkDependencyFlags.ByRegion,
				0, null,
				0, null,
				1, &dstBarrier
			);
			_cmd.EndCommandBuffer().Throw("Failed to record image upload commands");

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
			ResourceType? imageType)
		{
			// Select which host buffer to use
			ulong count = region.GetDataSize(fmt);
			bool useTmp = count > (ulong)HOST_SIZE.B;
			var srcBuffer = useTmp ? new HostBuffer(count) : Buffer;

			// Copy and transfer
			System.Buffer.MemoryCopy(srcData, srcBuffer.DataPtr, srcBuffer.DataSize, count);
			if (useTmp) {
				// Need Dispose safety to not leak the tmp buffer
				using (srcBuffer) {
					SetImageData(dstImage, fmt, region, srcBuffer, 0, imageType);
				}
			}
			else {
				SetImageData(dstImage, fmt, region, srcBuffer, 0, imageType);
			}
		}
		#endregion // Images

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
