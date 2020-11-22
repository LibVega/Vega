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
	internal sealed class MemoryAllocation
	{
		#region Fields
		public readonly VkDeviceMemory Handle;
		public readonly ulong Offset;
		public readonly ulong Size;
		public readonly VkMemoryPropertyFlags Flags;

		public bool IsDeviceLocal => (Flags & VkMemoryPropertyFlags.DeviceLocal) > 0;
		public bool IsHostVisible => (Flags & VkMemoryPropertyFlags.HostVisible) > 0;
		public bool IsLazyAllocated => (Flags & VkMemoryPropertyFlags.LazilyAllocated) > 0;

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
		
		public unsafe void Free()
		{
			if (IsValid) {
				Handle.FreeMemory(null);
				IsValid = false;
			}
			else {
				throw new InvalidOperationException("Double-free device memory allocation");
			}
		}
	}
}
