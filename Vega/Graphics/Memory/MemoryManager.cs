/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	// Manages memory allocations and operations
	internal unsafe sealed class MemoryManager : IDisposable
	{
		#region Fields
		// The graphics device that this manager is for
		public readonly GraphicsDevice Device;

		// Memory info
		private readonly MemoryIndex MemoryDevice;
		private readonly MemoryIndex MemoryHost;
		private readonly MemoryIndex MemoryUpload;
		private readonly MemoryIndex MemoryDynamic;
		private readonly MemoryIndex? MemoryTransient;
		// Gets if the system supports transient memory
		public bool HasTransientMemory => MemoryTransient.HasValue;

		// Disposal flag
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		public MemoryManager(GraphicsDevice device)
		{
			Device = device;
			var info = device.VkDeviceInfo;

			var mdev = info.FindMemoryType(UInt32.MaxValue,
				VkMemoryPropertyFlags.DeviceLocal,
				VkMemoryPropertyFlags.NoFlags,
				VkMemoryPropertyFlags.HostVisible);
			var mhos = info.FindMemoryType(UInt32.MaxValue,
				VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCached,
				VkMemoryPropertyFlags.HostCoherent,
				VkMemoryPropertyFlags.DeviceLocal);
			var mupl = info.FindMemoryType(UInt32.MaxValue,
				VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
				VkMemoryPropertyFlags.NoFlags,
				VkMemoryPropertyFlags.HostCached);
			var mdyn = info.FindMemoryType(UInt32.MaxValue,
				VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
				VkMemoryPropertyFlags.DeviceLocal,
				VkMemoryPropertyFlags.HostCached);
			var mtra = info.FindMemoryType(UInt32.MaxValue,
				VkMemoryPropertyFlags.LazilyAllocated | VkMemoryPropertyFlags.DeviceLocal,
				VkMemoryPropertyFlags.NoFlags,
				VkMemoryPropertyFlags.HostVisible);

			MemoryDevice = new(
				mdev ?? throw new PlatformNotSupportedException("Device does not report graphics memory"),
				info.MemoryTypes[(int)mdev.Value]);
			MemoryHost = new(
				mhos ?? throw new PlatformNotSupportedException("Device does not report host memory"),
				info.MemoryTypes[(int)mhos.Value]);
			MemoryUpload = new(
				mupl.HasValue ? mupl.Value : MemoryHost.Index,
				info.MemoryTypes[mupl.HasValue ? (int)mupl.Value : (int)MemoryHost.Index]);
			MemoryDynamic = new(
				mdyn.HasValue ? mdyn.Value : MemoryUpload.Index,
				info.MemoryTypes[mdyn.HasValue ? (int)mdyn.Value : (int)MemoryUpload.Index]);
			MemoryTransient = !mtra.HasValue ? null : new(
				mtra.Value,
				info.MemoryTypes[(int)mtra.Value]);
		}

		#region Allocation
		public MemoryAllocation? AllocateDevice(in VkMemoryRequirements req)
		{
			if ((req.MemoryTypeBits & (1u << (int)MemoryDevice.Index)) == 0) {
				return null;
			}

			return allocateMemory(req.Size, MemoryDevice);
		}

		public MemoryAllocation? AllocateHost(in VkMemoryRequirements req)
		{
			if ((req.MemoryTypeBits & (1u << (int)MemoryHost.Index)) == 0) {
				return null;
			}

			return allocateMemory(req.Size, MemoryHost);
		}

		public MemoryAllocation? AllocateUpload(in VkMemoryRequirements req)
		{
			if ((req.MemoryTypeBits & (1u << (int)MemoryUpload.Index)) == 0) {
				return null;
			}

			return allocateMemory(req.Size, MemoryUpload);
		}

		public MemoryAllocation? AllocateDynamic(in VkMemoryRequirements req)
		{
			if ((req.MemoryTypeBits & (1u << (int)MemoryDynamic.Index)) == 0) {
				return null;
			}

			return allocateMemory(req.Size, MemoryDynamic);
		}

		public MemoryAllocation? AllocateTransient(in VkMemoryRequirements req)
		{
			if (!MemoryTransient.HasValue) {
				return null;
			}
			if ((req.MemoryTypeBits & (1u << (int)MemoryTransient.Value.Index)) == 0) {
				return null;
			}

			return allocateMemory(req.Size, MemoryTransient.Value);
		}

		private MemoryAllocation? allocateMemory(ulong size, in MemoryIndex memType)
		{
			VkMemoryAllocateInfo mai = new(size, memType.Index);
			VulkanHandle<VkDeviceMemory> memHandle;
			var res = Device.VkDevice.AllocateMemory(&mai, null, &memHandle);
			if (res == VkResult.ErrorOutOfHostMemory) {
				throw new OutOfHostMemoryException(new DataSize((long)size));
			}
			if (res == VkResult.ErrorOutOfDeviceMemory) {
				throw new OutOfDeviceMemoryException(new DataSize((long)size));
			}
			return (res == VkResult.Success) 
				? new(new(memHandle, Device.VkDevice), 0, size, memType.Type.PropertyFlags) 
				: null;
		}
		#endregion // Allocation

		#region IDisposable
		public void Dispose()
		{
			if (!IsDisposed) {

			}
			IsDisposed = true;
		}
		#endregion // IDisposable

		// Holds information for a memory type
		private struct MemoryIndex
		{
			public uint Index;
			public VkMemoryType Type;

			public MemoryIndex(uint idx, in VkMemoryType type) => (Index, Type) = (idx, type);
		}
	}
}
