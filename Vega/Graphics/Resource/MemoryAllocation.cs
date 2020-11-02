/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	// Holds information about a graphics memory allocation
	internal sealed class MemoryAllocation
	{
		#region Fields
		public readonly Vk.DeviceMemory Handle;
		public readonly Vk.DeviceSize Offset;
		public readonly Vk.DeviceSize Size;
		public readonly Vk.MemoryPropertyFlags Flags;

		public bool IsDeviceLocal => (Flags & Vk.MemoryPropertyFlags.DeviceLocal) > 0;
		public bool IsHostVisible => (Flags & Vk.MemoryPropertyFlags.HostVisible) > 0;
		public bool IsLazyAllocated => (Flags & Vk.MemoryPropertyFlags.LazilyAllocated) > 0;

		public bool IsValid { get; private set; } = true;
		#endregion // Fields

		public MemoryAllocation(Vk.DeviceMemory handle, Vk.DeviceSize off, Vk.DeviceSize size, 
			Vk.MemoryPropertyFlags flags)
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
