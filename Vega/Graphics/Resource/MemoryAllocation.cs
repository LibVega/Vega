/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	// Holds information about a graphics memory allocation
	internal unsafe sealed class MemoryAllocation
	{
		#region Fields
		public readonly VkDeviceMemory Handle;
		public readonly ulong Offset;
		public readonly ulong Size;
		public readonly VkMemoryPropertyFlags Flags;

		public bool IsDeviceLocal => (Flags & VkMemoryPropertyFlags.DeviceLocal) > 0;
		public bool IsHostVisible => (Flags & VkMemoryPropertyFlags.HostVisible) > 0;
		public bool IsLazyAllocated => (Flags & VkMemoryPropertyFlags.LazilyAllocated) > 0;

		public bool IsMapped => DataPtr != null;
		public void* DataPtr { get; private set; } = null;

		public bool IsValid { get; private set; } = true;
		#endregion // Fields

		public MemoryAllocation(VkDeviceMemory handle, ulong off, ulong size, 
			VkMemoryPropertyFlags flags)
		{
			Handle = handle;
			Offset = off;
			Size = size;
			Flags = flags;
		}
		
		public void Free()
		{
			if (IsValid) {
				if (IsMapped) {
					throw new InvalidOperationException("LIBRARY BUG - Attempt to free mapped memory");
				}
				Handle.FreeMemory(null);
				IsValid = false;
			}
			else {
				throw new InvalidOperationException("LIBRARY BUG - Double-free device memory allocation");
			}
		}

		public void* Map()
		{
			if (!IsHostVisible) {
				throw new InvalidOperationException("LIBRARY BUG - Cannot map non-host-visible memory");
			}
			if (IsMapped) {
				return DataPtr;
			}

			// Map memory
			void* memptr;
			Handle.MapMemory(Offset, VkConstants.WHOLE_SIZE, VkMemoryMapFlags.NoFlags, &memptr)
				.Throw("Failed to map memory to host address space");
			DataPtr = memptr;

			return DataPtr;
		}

		public void Unmap()
		{
			if (!IsMapped) {
				throw new InvalidOperationException("LIBRARY BUG - Cannot unmap memory that is not mapped");
			}

			Handle.UnmapMemory();
			DataPtr = null;
		}
	}
}
