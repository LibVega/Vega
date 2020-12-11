/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using Vulkan;

namespace Vega.Graphics
{
	// Performs management and tracking for memory, per-thread, and per-frame Vulkan resources
	internal unsafe sealed class ResourceManager : IDisposable
	{
		public const int MAX_THREADS = sizeof(uint) * 8;

		#region Fields
		// The service using this resource manager
		public readonly GraphicsDevice Graphics;

		// Memory info
		private readonly MemoryIndex MemoryDevice;
		private readonly MemoryIndex MemoryHost;
		private readonly MemoryIndex MemoryUpload;
		private readonly MemoryIndex MemoryDynamic;
		private readonly MemoryIndex? MemoryTransient;
		// Gets if the system supports transient memory
		public bool HasTransientMemory => MemoryTransient.HasValue;

		// Pipeline cache
		internal readonly VkPipelineCache PipelineCache;

		// Resource delayed destroy queues
		private readonly (FastMutex Mutex, List<ResourceBase> List)[] _destroyQueues;
		private uint _destroyIndex = 0;

		#region Thread Local Resources
		// Per-thread management values
		[ThreadStatic]
		private static uint? _ThreadIndex = null; // Index of current thread into global resource lists
		private static uint _IndexMask = UInt32.MaxValue; // Mask of available thread ids (bit=1 is available)
		private static uint _ThreadCount = 0;
		private static readonly object _ThreadObjectLock = new();
		public bool IsThreadRegistered => _ThreadIndex.HasValue;
		public bool IsMainThread => _ThreadIndex.HasValue && (_ThreadIndex.Value == 0);

		// Command pools
		private readonly CommandPool?[] _commandPools = new CommandPool?[MAX_THREADS];

		// Transfer managers
		private readonly TransferManager?[] _transferManagers = new TransferManager?[MAX_THREADS];
		public TransferManager TransferManager { 
			get {
				if (!IsThreadRegistered) {
					throw new InvalidOperationException("Attempt to perform transfer operations on non-graphics thread");
				}
				return _transferManagers[_ThreadIndex!.Value]!;
			}
		}
		#endregion // Thread Local Resources

		// If this manager is disposed
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		public ResourceManager(GraphicsDevice gs)
		{
			Graphics = gs;

			// Load memory types
			{
				var mdev = gs.VkDeviceInfo.FindMemoryType(UInt32.MaxValue, 
					VkMemoryPropertyFlags.DeviceLocal,
					VkMemoryPropertyFlags.NoFlags, 
					VkMemoryPropertyFlags.HostVisible);
				var mhos = gs.VkDeviceInfo.FindMemoryType(UInt32.MaxValue,
					VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCached,
					VkMemoryPropertyFlags.HostCoherent,
					VkMemoryPropertyFlags.DeviceLocal);
				var mupl = gs.VkDeviceInfo.FindMemoryType(UInt32.MaxValue,
					VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
					VkMemoryPropertyFlags.NoFlags,
					VkMemoryPropertyFlags.HostCached);
				var mdyn = gs.VkDeviceInfo.FindMemoryType(UInt32.MaxValue,
					VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
					VkMemoryPropertyFlags.DeviceLocal,
					VkMemoryPropertyFlags.HostCached);
				var mtra = gs.VkDeviceInfo.FindMemoryType(UInt32.MaxValue,
					VkMemoryPropertyFlags.LazilyAllocated | VkMemoryPropertyFlags.DeviceLocal,
					VkMemoryPropertyFlags.NoFlags,
					VkMemoryPropertyFlags.HostVisible);

				MemoryDevice = new(
					mdev.HasValue ? mdev.Value : throw new PlatformNotSupportedException("Device does not report VRAM"),
					gs.VkDeviceInfo.MemoryTypes[(int)mdev.Value].HeapIndex);
				MemoryHost = new(
					mhos.HasValue ? mhos.Value : throw new PlatformNotSupportedException("Device does not report DRAM"),
					gs.VkDeviceInfo.MemoryTypes[(int)mhos.Value].HeapIndex);
				MemoryUpload = new(
					mupl.HasValue ? mupl.Value : MemoryHost.Type,
					gs.VkDeviceInfo.MemoryTypes[(int)(mupl.HasValue ? mupl.Value : MemoryHost.Type)].HeapIndex);
				MemoryDynamic = new(
					mdyn.HasValue ? mdyn.Value : MemoryUpload.Type,
					gs.VkDeviceInfo.MemoryTypes[(int)(mdyn.HasValue ? mdyn.Value : MemoryUpload.Type)].HeapIndex);
				MemoryTransient = !mtra.HasValue ? null : new(
					mtra.Value,
					gs.VkDeviceInfo.MemoryTypes[(int)mtra.Value].HeapIndex);
			}

			// Create the pipeline cache
			VkPipelineCacheCreateInfo pcci = new(
				flags: VkPipelineCacheCreateFlags.NoFlags, // TODO: Look into VK_EXT_pipeline_creation_cache_control
				initialDataSize: 0, // TODO: Allow save/load of pipeline cache data
				initialData: null
			);
			VulkanHandle<VkPipelineCache> cacheHandle;
			gs.VkDevice.CreatePipelineCache(&pcci, null, &cacheHandle)
				.Throw("Failed to create core pipeline cache");
			PipelineCache = new(cacheHandle, gs.VkDevice);

			// Destroy queues
			_destroyQueues = new (FastMutex, List<ResourceBase>)[GraphicsDevice.MAX_PARALLEL_FRAMES];
			for (int i = 0; i < GraphicsDevice.MAX_PARALLEL_FRAMES; ++i) {
				_destroyQueues[i].Mutex = new();
				_destroyQueues[i].List = new(32);
			}
		}
		~ResourceManager()
		{
			dispose(false);
		}

		// Runs a frame update on the resources that are tracked per-frame
		public void NextFrame()
		{
			// Lock on thread objects, but should have a *very* low contention rate
			lock (_ThreadObjectLock) {
				// Perform command pool updates
				uint count = _ThreadCount;
				foreach (var pool in _commandPools) {
					if (pool is not null) {
						pool.NextFrame();
						if (--count == 0) break; // Dont search the rest, they will be null
					}
				} 
			}

			// Destroy delayed objects that are no longer in use
			_destroyIndex = (_destroyIndex + 1) % GraphicsDevice.MAX_PARALLEL_FRAMES;
			using (var _ = _destroyQueues[_destroyIndex].Mutex.AcquireUNSAFE()) {
				foreach (var res in _destroyQueues[_destroyIndex].List) {
					res.Destroy();
				}
				_destroyQueues[_destroyIndex].List.Clear();
			}
		}

		#region Commands
		// Get a free managed command buffer for the current thread
		public CommandBuffer AllocateManagedCommandBuffer(VkCommandBufferLevel level)
		{
			if (!IsThreadRegistered) {
				throw new InvalidOperationException("Attempt to allocate command buffer on non-graphics thread");
			}
			return _commandPools[_ThreadIndex!.Value]!.AllocateManaged(level);
		}

		// Get a free transient command buffer for the current thread
		public CommandBuffer AllocateTransientCommandBuffer(VkCommandBufferLevel level)
		{
			if (!IsThreadRegistered) {
				throw new InvalidOperationException("Attempt to allocate command buffer on non-graphics thread");
			}
			return _commandPools[_ThreadIndex!.Value]!.AllocateTransient(level);
		}
		#endregion // Commands

		#region Memory
		public MemoryAllocation? AllocateMemoryDevice(in VkMemoryRequirements req)
		{
			if ((req.MemoryTypeBits & (1u << (int)MemoryDevice.Type)) == 0) {
				return null;
			}

			VkMemoryAllocateInfo.New(out var mai);
			mai.AllocationSize = req.Size;
			mai.MemoryTypeIndex = MemoryDevice.Type;
			VulkanHandle<VkDeviceMemory> memHandle;
			var res = Graphics.VkDevice.AllocateMemory(&mai, null, &memHandle);
			if (res == VkResult.ErrorOutOfHostMemory) throw new OutOfHostMemoryException(new DataSize((long)req.Size));
			if (res == VkResult.ErrorOutOfDeviceMemory) throw new OutOfDeviceMemoryException(new DataSize((long)req.Size));
			VkDeviceMemory mem = new(memHandle, Graphics.VkDevice);
			return (res == VkResult.Success)
				? new(mem, 0, req.Size, Graphics.VkDeviceInfo.MemoryTypes[(int)MemoryDevice.Type].PropertyFlags)
				: null;
		}

		public MemoryAllocation? AllocateMemoryHost(in VkMemoryRequirements req)
		{
			if ((req.MemoryTypeBits & (1u << (int)MemoryHost.Type)) == 0) {
				return null;
			}

			VkMemoryAllocateInfo.New(out var mai);
			mai.AllocationSize = req.Size;
			mai.MemoryTypeIndex = MemoryHost.Type;
			VulkanHandle<VkDeviceMemory> memHandle;
			var res = Graphics.VkDevice.AllocateMemory(&mai, null, &memHandle);
			if (res == VkResult.ErrorOutOfHostMemory) throw new OutOfHostMemoryException(new DataSize((long)req.Size));
			if (res == VkResult.ErrorOutOfDeviceMemory) throw new OutOfDeviceMemoryException(new DataSize((long)req.Size));
			VkDeviceMemory mem = new(memHandle, Graphics.VkDevice);
			return (res == VkResult.Success)
				? new(mem, 0, req.Size, Graphics.VkDeviceInfo.MemoryTypes[(int)MemoryHost.Type].PropertyFlags)
				: null;
		}

		public MemoryAllocation? AllocateMemoryUpload(in VkMemoryRequirements req)
		{
			if ((req.MemoryTypeBits & (1u << (int)MemoryUpload.Type)) == 0) {
				return null;
			}

			VkMemoryAllocateInfo.New(out var mai);
			mai.AllocationSize = req.Size;
			mai.MemoryTypeIndex = MemoryUpload.Type;
			VulkanHandle<VkDeviceMemory> memHandle;
			var res = Graphics.VkDevice.AllocateMemory(&mai, null, &memHandle);
			if (res == VkResult.ErrorOutOfHostMemory) throw new OutOfHostMemoryException(new DataSize((long)req.Size));
			if (res == VkResult.ErrorOutOfDeviceMemory) throw new OutOfDeviceMemoryException(new DataSize((long)req.Size));
			VkDeviceMemory mem = new(memHandle, Graphics.VkDevice);
			return (res == VkResult.Success)
				? new(mem, 0, req.Size, Graphics.VkDeviceInfo.MemoryTypes[(int)MemoryUpload.Type].PropertyFlags)
				: null;
		}

		public MemoryAllocation? AllocateMemoryDynamic(in VkMemoryRequirements req)
		{
			if ((req.MemoryTypeBits & (1u << (int)MemoryDynamic.Type)) == 0) {
				return null;
			}

			VkMemoryAllocateInfo.New(out var mai);
			mai.AllocationSize = req.Size;
			mai.MemoryTypeIndex = MemoryDynamic.Type;
			VulkanHandle<VkDeviceMemory> memHandle;
			var res = Graphics.VkDevice.AllocateMemory(&mai, null, &memHandle);
			if (res == VkResult.ErrorOutOfHostMemory) throw new OutOfHostMemoryException(new DataSize((long)req.Size));
			if (res == VkResult.ErrorOutOfDeviceMemory) throw new OutOfDeviceMemoryException(new DataSize((long)req.Size));
			VkDeviceMemory mem = new(memHandle, Graphics.VkDevice);
			return (res == VkResult.Success)
				? new(mem, 0, req.Size, Graphics.VkDeviceInfo.MemoryTypes[(int)MemoryDynamic.Type].PropertyFlags)
				: null;
		}

		public MemoryAllocation? AllocateMemoryTransient(in VkMemoryRequirements req)
		{
			if (!MemoryTransient.HasValue) {
				return null;
			}
			if ((req.MemoryTypeBits & (1u << (int)MemoryTransient.Value.Type)) == 0) {
				return null;
			}

			VkMemoryAllocateInfo.New(out var mai);
			mai.AllocationSize = req.Size;
			mai.MemoryTypeIndex = MemoryTransient.Value.Type;
			VulkanHandle<VkDeviceMemory> memHandle;
			var res = Graphics.VkDevice.AllocateMemory(&mai, null, &memHandle);
			if (res == VkResult.ErrorOutOfHostMemory) throw new OutOfHostMemoryException(new DataSize((long)req.Size));
			if (res == VkResult.ErrorOutOfDeviceMemory) throw new OutOfDeviceMemoryException(new DataSize((long)req.Size));
			VkDeviceMemory mem = new(memHandle, Graphics.VkDevice);
			return (res == VkResult.Success)
				? new(mem, 0, req.Size, Graphics.VkDeviceInfo.MemoryTypes[(int)MemoryTransient.Value.Type].PropertyFlags)
				: null;
		}
		#endregion // Memory

		#region Threading
		public void RegisterThread()
		{
			const uint THREAD_INDEX_OFFSET = (sizeof(ulong) - sizeof(uint)) * 8; // Needed b/c Lzcnt promotes to ulong

			if (_ThreadIndex.HasValue) {
				throw new InvalidOperationException("Cannot double register a thread for graphics operations");
			}

			// Prepare thread
			lock (_ThreadObjectLock) {
				// Get the next thread index
				if (_IndexMask == 0) {
					throw new InvalidOperationException("No available slots for a new graphics thread");
				}
				_ThreadIndex = (uint)Lzcnt.X64.LeadingZeroCount(_IndexMask) - THREAD_INDEX_OFFSET;
				_IndexMask &= ~(1u << (int)_ThreadIndex.Value); // Clear the bit (mark as used)
				_ThreadCount += 1;

				// Create thread resources
				_commandPools[_ThreadIndex.Value] = new(Graphics);
				_transferManagers[_ThreadIndex.Value] = new(Graphics);
			}
		}

		public void UnregisterThread()
		{
			if (!_ThreadIndex.HasValue) {
				throw new InvalidOperationException("Cannot unregister a thread that is not registered for graphics operations");
			}

			// Release the thread id
			lock (_ThreadObjectLock) {
				// Destroy thread resources
				_transferManagers[_ThreadIndex.Value]!.Dispose();
				_transferManagers[_ThreadIndex.Value] = null;
				_commandPools[_ThreadIndex.Value]!.Dispose();
				_commandPools[_ThreadIndex.Value] = null;

				// Clear thread index
				_IndexMask |= (1u << (int)_ThreadIndex.Value); // Set the bit (mark as unused)
				_ThreadIndex = null;
				_ThreadCount -= 1;
			}

			// This should happen relatively infrequently, and after this call there will be a good amount to release
			GC.Collect();
		}
		#endregion // Threading

		#region Destroy Queue
		// Queues the resource for delayed destruction
		public void QueueDestroy(ResourceBase res)
		{
			var queue = _destroyQueues[_destroyIndex];
			using (var _ = queue.Mutex.AcquireUNSAFE()) {
				queue.List.Add(res);
			}
		}
		#endregion // Destroy Queue

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed) {
				if (!disposing) {
					Graphics.VkDevice?.DeviceWaitIdle(); // We may not be coming from GraphicsDevice.Dispose()
				}

				// Destroy all delayed resoures
				foreach (var queue in _destroyQueues) {
					foreach (var res in queue.List) {
						res.Destroy();
					}
					queue.List.Clear();
				}

				// Destroy pipeline cache
				PipelineCache?.DestroyPipelineCache(null);

				// Destroy threaded resources
				foreach (var pool in _commandPools) {
					pool?.Dispose();
				}
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
