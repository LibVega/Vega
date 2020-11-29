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
	/// Base type for all graphics data buffer types, providing common functionality.
	/// </summary>
	public unsafe abstract class BufferBase : ResourceBase
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

		private protected BufferBase(ulong size, ResourceType type, BufferUsage usage)
			: base(type)
		{
			// Set fields
			DataSize = size;
			Usage = usage;
			
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
			Handle = new(handle, gd.VkDevice);

			// Allocate and bind buffer memory
			VkMemoryRequirements memreq;
			Handle.GetBufferMemoryRequirements(&memreq);
			Memory = gd.Resources.AllocateMemoryDevice(memreq) ??
				throw new Exception("Failed to allocate memory for buffer");
			Handle.BindBufferMemory(Memory.Handle, Memory.Offset);
		}

		protected override void OnDispose(bool disposing)
		{
			// TODO: Submit to delayed destroy queue
		}

		private static VkBufferUsageFlags GetUsageFlags(ResourceType type)
		{
			var flags = VkBufferUsageFlags.TransferDst;
			flags |= type switch {
				_ => throw new ArgumentOutOfRangeException(nameof(type), "Invalid resource type")
			};
			return flags;
		}
	}
}
