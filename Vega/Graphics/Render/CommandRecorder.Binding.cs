/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.InteropServices;
using Vulkan;

namespace Vega.Graphics
{
	// Implements the types and operations for managing bindings in a command recorder
	public unsafe sealed partial class CommandRecorder
	{
		// Represents a bound resource and associated information
		[StructLayout(LayoutKind.Explicit)]
		private struct Binding
		{
			#region Fields
			// The handle of the primary binding resource
			[FieldOffset(0)] public ulong PrimaryHandle;
			
			// The handle of the secondary resource (the texture handle for BoundSamplers)
			[FieldOffset(sizeof(ulong))] public ulong SecondaryHandle;
			// The buffer offset for buffer resources
			[FieldOffset(sizeof(ulong))] public ulong BufferOffset;

			// If the binding is valid (non-null primary handle)
			public readonly bool IsValid => PrimaryHandle != 0;
			#endregion // Fields

			public Binding(ulong primary, ulong secondary) 
			{
				PrimaryHandle = primary;
				SecondaryHandle = BufferOffset = secondary;
			}

			public static implicit operator bool (in Binding b) => b.IsValid;
		}


		// Represents the set of bound resources for a specific binding group
		private class BoundResources
		{
			#region Fields
			// The bound resources
			private readonly Binding[] _bindings = new Binding[BindingLayout.SLOT_COUNT];

			// If the bindings are dirty (a new set is required)
			public bool Dirty { get; private set; } = false;
			// If the offsets are dirty (no new set required, but offsets need to be updated)
			public bool OffsetsDirty { get; private set; } = false;
			#endregion // Fields

			// Update the binding for a single-reference binding type
			public void Set<T>(uint slot, VulkanHandle<T> handle)
				where T : class, IVulkanHandle<T>
			{
				var ptr = handle.LongHandle;
				var changed = (ptr != _bindings[slot].PrimaryHandle);
				if (changed) {
					_bindings[slot].PrimaryHandle = ptr;
					Dirty = true;
				}
			}

			// Update the binding for a combined image sampler
			public void Set(uint slot, VulkanHandle<VkSampler> sampler, VulkanHandle<VkImageView> image)
			{
				var sptr = sampler.LongHandle;
				var iptr = image.LongHandle;
				var changed = (_bindings[slot].PrimaryHandle != sptr) || (_bindings[slot].SecondaryHandle != iptr);
				if (changed) {
					_bindings[slot] = new(sptr, iptr);
					Dirty = true;
				}
			}

			// Update the binding for a buffer with and offset
			public void Set(uint slot, VulkanHandle<VkBuffer> buffer, ulong offset)
			{
				var bptr = buffer.LongHandle;
				var changed = bptr != _bindings[slot].PrimaryHandle;
				var offchanged = changed || (offset != _bindings[slot].BufferOffset);
				if (changed) {
					_bindings[slot] = new(bptr, offset);
					Dirty = true;
				}
				if (offchanged) {
					OffsetsDirty = true;
				}
			}

			// Resets all bindings in the set to be unbound
			public void Reset()
			{
				for (int i = 0; i < BindingLayout.SLOT_COUNT; ++i) {
					_bindings[i].PrimaryHandle = 0;
				}
			}

			// Marks the binding set as clean
			public void MarkClean() => Dirty = OffsetsDirty = false;

			// Populates write descriptor objects to update descriptor sets with the binding contents
			public void PopulateDescriptorWrites(VkWriteDescriptorSet* writes, VkDescriptorImageInfo* iinfos, 
				in BindingSet set, BindingLayout layout)
			{
				for (int i = 0, rem = (int)layout.SlotCount; (i < BindingLayout.SLOT_COUNT) && (rem > 0); ++i) {
					// Skip disabled slots
					if (!layout.Slots[i].Enabled) {
						continue;
					}
					ref var binding = ref _bindings[i];

					// Update image/buffer info as needed (TODO: Buffer Info)
					switch (layout.Slots[i].Type) {
						case BindingType.Sampler: 
							*(iinfos++) = new(sampler: new(binding.PrimaryHandle));
							break;
						case BindingType.BoundSampler:
							*(iinfos++) = new(
								sampler: new(binding.PrimaryHandle), 
								imageView: new(binding.SecondaryHandle), 
								imageLayout: VkImageLayout.ShaderReadOnlyOptimal);
							break;
						case BindingType.Texture:
							*(iinfos++) = new(
								imageView: new(binding.PrimaryHandle), 
								imageLayout: VkImageLayout.ShaderReadOnlyOptimal);
							break;
					}

					// Create the write values
					*writes = new(
						dstSet: set.Handle,
						dstBinding: (uint)i,
						dstArrayElement: 0,
						descriptorCount: 1,
						descriptorType: (VkDescriptorType)layout.Slots[i].Type,
						imageInfo: iinfos - 1,
						bufferInfo: null, // TODO: Buffer info
						texelBufferView: null
					);

					// Loop values
					writes += 1;
					rem -= 1;
				}
			}
		}
	}
}
