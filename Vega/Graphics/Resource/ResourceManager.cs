/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	// Performs management and tracking for memory, per-thread, and per-frame Vulkan resources
	internal unsafe sealed class ResourceManager : IDisposable
	{
		#region Fields
		// The service using this resource manager
		public readonly GraphicsService Graphics;

		// Memory info
		private readonly MemoryIndex MemoryDevice;
		private readonly MemoryIndex MemoryHost;
		private readonly MemoryIndex MemoryUpload;
		private readonly MemoryIndex MemoryDynamic;
		private readonly MemoryIndex? MemoryTransient;

		// If this manager is disposed
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		public ResourceManager(GraphicsService gs)
		{
			Graphics = gs;

			// Load memory types
			{
				var mdev = gs.DeviceData.FindMemoryType(UInt32.MaxValue, 
					Vk.MemoryPropertyFlags.DeviceLocal,
					Vk.MemoryPropertyFlags.NoFlags, 
					Vk.MemoryPropertyFlags.HostVisible);
				var mhos = gs.DeviceData.FindMemoryType(UInt32.MaxValue,
					Vk.MemoryPropertyFlags.HostVisible | Vk.MemoryPropertyFlags.HostCached,
					Vk.MemoryPropertyFlags.HostCoherent,
					Vk.MemoryPropertyFlags.DeviceLocal);
				var mupl = gs.DeviceData.FindMemoryType(UInt32.MaxValue,
					Vk.MemoryPropertyFlags.HostVisible | Vk.MemoryPropertyFlags.HostCoherent,
					Vk.MemoryPropertyFlags.NoFlags,
					Vk.MemoryPropertyFlags.HostCached);
				var mdyn = gs.DeviceData.FindMemoryType(UInt32.MaxValue,
					Vk.MemoryPropertyFlags.HostVisible | Vk.MemoryPropertyFlags.HostCoherent,
					Vk.MemoryPropertyFlags.DeviceLocal,
					Vk.MemoryPropertyFlags.HostCached);
				var mtra = gs.DeviceData.FindMemoryType(UInt32.MaxValue,
					Vk.MemoryPropertyFlags.LazilyAllocated | Vk.MemoryPropertyFlags.DeviceLocal,
					Vk.MemoryPropertyFlags.NoFlags,
					Vk.MemoryPropertyFlags.HostVisible);

				MemoryDevice = new(
					mdev.HasValue ? mdev.Value : throw new PlatformNotSupportedException("Device does not report VRAM"),
					gs.DeviceData.MemoryTypes[(int)mdev.Value].HeapIndex);
				MemoryHost = new(
					mhos.HasValue ? mhos.Value : throw new PlatformNotSupportedException("Device does not report DRAM"),
					gs.DeviceData.MemoryTypes[(int)mhos.Value].HeapIndex);
				MemoryUpload = new(
					mupl.HasValue ? mupl.Value : MemoryHost.Type,
					gs.DeviceData.MemoryTypes[(int)(mupl.HasValue ? mupl.Value : MemoryHost.Type)].HeapIndex);
				MemoryDynamic = new(
					mdyn.HasValue ? mdyn.Value : MemoryUpload.Type,
					gs.DeviceData.MemoryTypes[(int)(mdyn.HasValue ? mdyn.Value : MemoryUpload.Type)].HeapIndex);
				MemoryTransient = !mtra.HasValue ? null : new(
					mtra.Value,
					gs.DeviceData.MemoryTypes[(int)mtra.Value].HeapIndex);
			}
		}
		~ResourceManager()
		{
			dispose(false);
		}

		#region Memory
		public MemoryAllocation? AllocateMemoryDevice(in Vk.MemoryRequirements req)
		{
			if ((req.MemoryTypeBits & (1u << (int)MemoryDevice.Type)) == 0) {
				return null;
			}

			Vk.MemoryAllocateInfo.New(out var mai);
			mai.AllocationSize = req.Size;
			mai.MemoryTypeIndex = MemoryDevice.Type;
			var res = Graphics.Device.AllocateMemory(&mai, null, out var mem);
			if (res == Vk.Result.OutOfHostMemory) throw new OutOfHostMemoryException(new DataSize((long)req.Size.Value));
			if (res == Vk.Result.OutOfDeviceMemory) throw new OutOfDeviceMemoryException(new DataSize((long)req.Size.Value));
			return (res == Vk.Result.Success)
				? new(mem, 0, req.Size, Graphics.DeviceData.MemoryTypes[(int)MemoryDevice.Type].PropertyFlags)
				: null;
		}

		public MemoryAllocation? AllocateMemoryHost(in Vk.MemoryRequirements req)
		{
			if ((req.MemoryTypeBits & (1u << (int)MemoryHost.Type)) == 0) {
				return null;
			}

			Vk.MemoryAllocateInfo.New(out var mai);
			mai.AllocationSize = req.Size;
			mai.MemoryTypeIndex = MemoryHost.Type;
			var res = Graphics.Device.AllocateMemory(&mai, null, out var mem);
			if (res == Vk.Result.OutOfHostMemory) throw new OutOfHostMemoryException(new DataSize((long)req.Size.Value));
			if (res == Vk.Result.OutOfDeviceMemory) throw new OutOfDeviceMemoryException(new DataSize((long)req.Size.Value));
			return (res == Vk.Result.Success)
				? new(mem, 0, req.Size, Graphics.DeviceData.MemoryTypes[(int)MemoryHost.Type].PropertyFlags)
				: null;
		}

		public MemoryAllocation? AllocateMemoryUpload(in Vk.MemoryRequirements req)
		{
			if ((req.MemoryTypeBits & (1u << (int)MemoryUpload.Type)) == 0) {
				return null;
			}

			Vk.MemoryAllocateInfo.New(out var mai);
			mai.AllocationSize = req.Size;
			mai.MemoryTypeIndex = MemoryUpload.Type;
			var res = Graphics.Device.AllocateMemory(&mai, null, out var mem);
			if (res == Vk.Result.OutOfHostMemory) throw new OutOfHostMemoryException(new DataSize((long)req.Size.Value));
			if (res == Vk.Result.OutOfDeviceMemory) throw new OutOfDeviceMemoryException(new DataSize((long)req.Size.Value));
			return (res == Vk.Result.Success)
				? new(mem, 0, req.Size, Graphics.DeviceData.MemoryTypes[(int)MemoryUpload.Type].PropertyFlags)
				: null;
		}

		public MemoryAllocation? AllocateMemoryDynamic(in Vk.MemoryRequirements req)
		{
			if ((req.MemoryTypeBits & (1u << (int)MemoryDynamic.Type)) == 0) {
				return null;
			}

			Vk.MemoryAllocateInfo.New(out var mai);
			mai.AllocationSize = req.Size;
			mai.MemoryTypeIndex = MemoryDynamic.Type;
			var res = Graphics.Device.AllocateMemory(&mai, null, out var mem);
			if (res == Vk.Result.OutOfHostMemory) throw new OutOfHostMemoryException(new DataSize((long)req.Size.Value));
			if (res == Vk.Result.OutOfDeviceMemory) throw new OutOfDeviceMemoryException(new DataSize((long)req.Size.Value));
			return (res == Vk.Result.Success)
				? new(mem, 0, req.Size, Graphics.DeviceData.MemoryTypes[(int)MemoryDynamic.Type].PropertyFlags)
				: null;
		}

		public MemoryAllocation? AllocateMemoryTransient(in Vk.MemoryRequirements req)
		{
			if (!MemoryTransient.HasValue) {
				return null;
			}
			if ((req.MemoryTypeBits & (1u << (int)MemoryTransient.Value.Type)) == 0) {
				return null;
			}

			Vk.MemoryAllocateInfo.New(out var mai);
			mai.AllocationSize = req.Size;
			mai.MemoryTypeIndex = MemoryTransient.Value.Type;
			var res = Graphics.Device.AllocateMemory(&mai, null, out var mem);
			if (res == Vk.Result.OutOfHostMemory) throw new OutOfHostMemoryException(new DataSize((long)req.Size.Value));
			if (res == Vk.Result.OutOfDeviceMemory) throw new OutOfDeviceMemoryException(new DataSize((long)req.Size.Value));
			return (res == Vk.Result.Success)
				? new(mem, 0, req.Size, Graphics.DeviceData.MemoryTypes[(int)MemoryTransient.Value.Type].PropertyFlags)
				: null;
		}
		#endregion // Memory

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed) {

			}
			IsDisposed = true;
		}
		#endregion // IDisposable

		// Holds information for a memory type
		private struct MemoryIndex
		{
			public uint Type;
			public uint Heap;

			public MemoryIndex(uint t, uint h) => (Type, Heap) = (t, h);
		}
	}
}
