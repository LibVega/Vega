/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
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
				Core.Instance!.Graphics.Resources.TransferManager.SetBufferData(Handle, 0, initialData, size, null);
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
			Core.Instance!.Graphics.Resources.TransferManager.SetBufferData(Handle, 0, initialData, 0, size, null);
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
				Core.Instance!.Graphics.Resources.TransferManager.SetBufferData(Handle, 0, dataPtr, size, null);
			}
		}

		protected override void OnDispose(bool disposing)
		{
			if (Core.Instance is not null) {
				Core.Instance.Graphics.Resources.QueueDestroy(this);
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
