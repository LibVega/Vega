/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	// Represents a uniform buffer that uses a thread-safe push allocator for dynamic uniform data
	// These are *not* circular, and each buffer will throw an exception after running out of space
	// These are triple buffered, each taking a certain amount of space in a single buffer object
	// Prefers the dynamic memory type, will fall back on upload memory, then host memory.
	// We may eventually switch to a bulk-upload of host memory at render submission time for non-dynamic buffers
	internal unsafe sealed class UniformPushBuffer : IDisposable
	{
		private const ulong PADDING = 1024; // Padding at the end of the buffer, since some layouts could potentially
											// bind past the normal end, which causes an error and blocks rendering

		#region Fields
		// Size info
		public readonly ulong TotalSize; // Total buffer size
		public readonly ulong FrameSize; // Per-frame buffer size

		// Uniform alignment requirement
		public readonly ulong Alignment;
		public readonly ulong FrameAlignment;

		// Buffer object
		internal readonly VkBuffer Handle;
		private readonly MemoryAllocation Memory;

		// Disposal flag
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		public UniformPushBuffer(ulong size)
		{
			var gd = Core.Instance!.Graphics;
			Alignment = gd.VkDeviceInfo.Properties.Limits.MinUniformBufferOffsetAlignment;

			// Calculate size requirements
			FrameSize = MathHelper.RoundUp(size, Alignment);
			FrameAlignment = FrameSize + PADDING;
			TotalSize = FrameAlignment * GraphicsDevice.MAX_PARALLEL_FRAMES;

			// Create the buffer
			VkBufferCreateInfo bci = new(
				flags: VkBufferCreateFlags.NoFlags,
				size: TotalSize,
				usage: VkBufferUsageFlags.UniformBuffer,
				sharingMode: VkSharingMode.Exclusive,
				queueFamilyIndexCount: 0,
				queueFamilyIndices: null
			);
			VulkanHandle<VkBuffer> handle;
			gd.VkDevice.CreateBuffer(&bci, null, &handle).Throw("Failed to create uniform push buffer");
			Handle = new(handle, gd.VkDevice);

			// Allocate and bind buffer memory
			VkMemoryRequirements memreq;
			Handle.GetBufferMemoryRequirements(&memreq);
			Memory = gd.Resources.AllocateMemoryDynamic(memreq) ?? 
				throw new Exception("Failed to allocate uniform push buffer memory");
			Handle.BindBufferMemory(Memory.Handle, Memory.Offset)
				.Throw("Failed to bind uniform push buffer memory");
		}
		~UniformPushBuffer()
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
				if (disposing) {
					Handle?.DestroyBuffer(null);
					Memory?.Free();
				}
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
