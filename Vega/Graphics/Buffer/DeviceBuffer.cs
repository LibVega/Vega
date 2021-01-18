/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Base type for all device-accessible graphics data buffers, providing common functionality.
	/// </summary>
	public unsafe abstract class DeviceBuffer : ResourceBase
	{
		#region Fields
		/// <summary>
		/// The size of the buffer data, in bytes.
		/// </summary>
		public readonly ulong DataSize;
		/// <summary>
		/// The buffer usage policy.
		/// </summary>
		public readonly BufferUsage Usage;

		// The vulkan buffer object
		internal readonly VkBuffer Handle;
		// The memory allocation object
		internal readonly MemoryAllocation Memory;

		// Last frame in which a dynamic buffer was updates
		internal ulong _lastDynamicUpdate = 0;
		#endregion // Fields

		private protected DeviceBuffer(ulong size, ResourceType type, BufferUsage usage, void* initialData)
			: base(type)
		{
			// Validate data
			if (size == 0) {
				throw new ArgumentOutOfRangeException(nameof(size), "Buffer size cannot be zero");
			}
			if ((usage == BufferUsage.Static) && (initialData == null)) {
				throw new InvalidOperationException("Cannot create a static buffer without supplying initial data");
			}

			// Set fields
			DataSize = size;
			Usage = usage;
			CreateBuffer(size, type, out Handle, out Memory);

			// Set the initial data
			if (initialData != null) {
				Graphics.Resources.TransferManager.SetBufferData(Handle, 0, initialData, size, null);
			}
		}

		private protected DeviceBuffer(ulong size, ResourceType type, BufferUsage usage, HostBuffer initialData)
			: base(type)
		{
			// Validate data
			if (size == 0) {
				throw new ArgumentOutOfRangeException(nameof(size), "Buffer size cannot be zero");
			}
			if (size > initialData.DataSize) {
				throw new InvalidOperationException("Host buffer is not large enough to supply device buffer data");
			}

			// Set fields
			DataSize = size;
			Usage = usage;
			CreateBuffer(size, type, out Handle, out Memory);

			// Set initial data
			Graphics.Resources.TransferManager.SetBufferData(Handle, 0, initialData, 0, size, null);
		}

		private protected DeviceBuffer(ulong size, ResourceType type, BufferUsage usage, ReadOnlySpan<byte> initialData)
			: base(type)
		{
			// Validate data
			if (size == 0) {
				throw new ArgumentOutOfRangeException(nameof(size), "Buffer size cannot be zero");
			}
			if (size > (ulong)initialData.Length) {
				throw new InvalidOperationException("Source data is not large enough to supply device buffer data");
			}

			// Set fields
			DataSize = size;
			Usage = usage;
			CreateBuffer(size, type, out Handle, out Memory);

			// Set initial data
			fixed (byte* dataPtr = initialData) {
				Graphics.Resources.TransferManager.SetBufferData(Handle, 0, dataPtr, size, null);
			}
		}

		#region Data
		// Update the data (non-static buffers only) from a raw pointer
		protected void SetDataImpl(void* data, ulong size, ulong offset)
		{
			// Validate
			ThrowIfDisposed();
			if (Usage == BufferUsage.Static) {
				throw new InvalidOperationException("Cannot update data for static-usage buffers");
			}
			if ((size + offset) > DataSize) {
				throw new InvalidOperationException("Cannot update data outside of buffer range");
			}
			if (data == null) {
				throw new InvalidOperationException("Cannot update buffer data from a null pointer");
			}
			if (size == 0) {
				throw new InvalidOperationException("Cannot update buffer from data of length 0");
			}
			
			// Check dynamic frame
			if (Usage == BufferUsage.Dynamic) {
				if (AppTime.FrameCount == _lastDynamicUpdate) {
					throw new InvalidOperationException("Dynamic buffers can only be updated once per frame");
				}
				_lastDynamicUpdate = AppTime.FrameCount;
			}

			// Perform async update
			Graphics.Resources.TransferManager.UpdateBufferAsync(
				ResourceType, Handle, offset, data, size
			);
		}

		// Update the data (non-static buffers only) from a span of data
		protected void SetDataImpl(ReadOnlySpan<byte> data, ulong offset)
		{
			// Validate
			ThrowIfDisposed();
			if (Usage == BufferUsage.Static) {
				throw new InvalidOperationException("Cannot update data for static-usage buffers");
			}
			if (((ulong)data.Length + offset) > DataSize) {
				throw new InvalidOperationException("Cannot update data outside of buffer range");
			}
			if (data.Length == 0) {
				throw new InvalidOperationException("Cannot update buffer from an empty span");
			}

			// Check dynamic frame
			if (Usage == BufferUsage.Dynamic) {
				if (AppTime.FrameCount == _lastDynamicUpdate) {
					throw new InvalidOperationException("Dynamic buffers can only be updated once per frame");
				}
				_lastDynamicUpdate = AppTime.FrameCount;
			}

			// Perform async update
			fixed (byte* dataPtr = data) {
				Graphics.Resources.TransferManager.UpdateBufferAsync(
					ResourceType, Handle, offset, dataPtr, (ulong)data.Length
				);
			}
		}

		// Update the data (non-static buffers only) from an existing host buffer
		protected void SetDataImpl(HostBuffer data, ulong size, ulong srcOffset, ulong dstOffset)
		{
			// Validate
			ThrowIfDisposed();
			if (Usage == BufferUsage.Static) {
				throw new InvalidOperationException("Cannot update data for static-usage buffers");
			}
			if ((size + dstOffset) > DataSize) {
				throw new InvalidOperationException("Cannot update data outside of buffer range");
			}
			if ((size + srcOffset) > data.DataSize) {
				throw new InvalidOperationException("Cannot update from data outside of host buffer range");
			}
			if (size == 0) {
				throw new InvalidOperationException("Cannot update buffer from data of length 0");
			}

			// Check dynamic frame
			if (Usage == BufferUsage.Dynamic) {
				if (AppTime.FrameCount == _lastDynamicUpdate) {
					throw new InvalidOperationException("Dynamic buffers can only be updated once per frame");
				}
				_lastDynamicUpdate = AppTime.FrameCount;
			}

			// Perform async update
			Graphics.Resources.TransferManager.UpdateBufferAsync(
				ResourceType, Handle, dstOffset, data.Buffer, srcOffset, size
			);
		}
		#endregion // Data

		#region ResourceBase
		protected override void OnDispose(bool disposing)
		{
			if (Core.Instance is not null) {
				Graphics.Resources.QueueDestroy(this);
			}
			else {
				Destroy();
			}
		}

		protected internal override void Destroy()
		{
			Handle?.DestroyBuffer(null);
			Memory?.Free();
		}
		#endregion // ResourceBase

		private static VkBufferUsageFlags GetUsageFlags(ResourceType type)
		{
			var flags = VkBufferUsageFlags.TransferDst;
			flags |= type switch {
				ResourceType.IndexBuffer => VkBufferUsageFlags.IndexBuffer,
				ResourceType.VertexBuffer => VkBufferUsageFlags.VertexBuffer,
				_ => throw new ArgumentOutOfRangeException(nameof(type), "Invalid resource type")
			};
			return flags;
		}

		private static void CreateBuffer(ulong size, ResourceType type, out VkBuffer buffer, out MemoryAllocation memory)
		{
			var gd = Core.Instance!.Graphics;

			// Create buffer handle
			VkBufferCreateInfo bci = new(
				flags: VkBufferCreateFlags.NoFlags,
				size: size,
				usage: GetUsageFlags(type),
				sharingMode: VkSharingMode.Exclusive
			);
			VulkanHandle<VkBuffer> handle;
			gd.VkDevice.CreateBuffer(&bci, null, &handle)
				.Throw("Failed to create buffer handle");
			buffer = new(handle, gd.VkDevice);

			// Allocate and bind buffer memory
			VkMemoryRequirements memreq;
			buffer.GetBufferMemoryRequirements(&memreq);
			memory = gd.Resources.AllocateMemoryDevice(memreq) ??
				throw new Exception("Failed to allocate memory for buffer");
			buffer.BindBufferMemory(memory.Handle, memory.Offset);
		}
	}
}
