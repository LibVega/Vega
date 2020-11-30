/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.IO;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Represents a contiguous block of memory available in host RAM, that can be used as the data source for transfers
	/// to the graphics device.
	/// </summary>
	public unsafe sealed class HostBuffer : ResourceBase
	{
		#region Fields
		/// <summary>
		/// The size of the buffer in bytes.
		/// </summary>
		public readonly ulong DataSize;
		/// <summary>
		/// Gets the pointer to the mapped host memory where data can be copied to/from.
		/// </summary>
		public void* DataPtr {
			get {
				ThrowIfDisposed();
				return _memory.DataPtr;
			}
		}
		/// <summary>
		/// Gets the mapped host memory as a byte span.
		/// </summary>
		public Span<byte> DataSpan {
			get {
				ThrowIfDisposed();
				return new(_memory.DataPtr, (int)DataSize);
			}
		}
		/// <summary>
		/// Gets a write-only stream object for the unmanaged memory in the buffer.
		/// </summary>
		public UnmanagedMemoryStream WriteStream { 
			get {
				ThrowIfDisposed();
				return _stream;
			}
		}
		private readonly UnmanagedMemoryStream _stream;

		// Vulkan buffer and memory
		private readonly VkBuffer _buffer;
		private readonly MemoryAllocation _memory;
		#endregion // Fields

		/// <summary>
		/// Create and map a new host buffer with the given size in bytes.
		/// </summary>
		/// <param name="size">The buffer size, in bytes.</param>
		public HostBuffer(ulong size)
			: base(ResourceType.HostBuffer)
		{
			DataSize = size;

			var gd = Core.Instance!.Graphics;

			// Create buffer
			VkBufferCreateInfo bci = new(
				flags: VkBufferCreateFlags.NoFlags,
				size: size,
				usage: VkBufferUsageFlags.TransferDst | VkBufferUsageFlags.TransferSrc,
				sharingMode: VkSharingMode.Exclusive
			);
			VulkanHandle<VkBuffer> handle;
			gd.VkDevice.CreateBuffer(&bci, null, &handle).Throw("Failed to create host buffer");
			_buffer = new(handle, gd.VkDevice);

			// Allocate/bind memory
			VkMemoryRequirements memreq;
			_buffer.GetBufferMemoryRequirements(&memreq);
			_memory = gd.Resources.AllocateMemoryHost(memreq) ?? 
				throw new Exception("Failed to allocate host buffer memory");
			_buffer.BindBufferMemory(_memory.Handle, _memory.Offset);

			// Map memory
			_memory.Map();
			_stream = new((byte*)_memory.DataPtr, 0, (long)DataSize, FileAccess.Write);
		}

		protected override void OnDispose(bool disposing)
		{
			if (disposing) {
				_stream.Dispose();
			}
			// No delayed destroy, this type cannot be used in async graphics ops
			Destroy();
		}

		protected internal override void Destroy()
		{
			_memory.Unmap();
			_buffer.DestroyBuffer(null);
			_memory.Free();
		}
	}
}
