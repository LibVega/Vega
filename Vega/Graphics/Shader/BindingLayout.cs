/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using Vega.Content;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Represents the layout of bindings within a binding group for a particular <see cref="Shader"/>.
	/// </summary>
	public unsafe sealed class BindingLayout
	{
		/// <summary>
		/// The number of slots available in a binding layout.
		/// </summary>
		public const uint SLOT_COUNT = 8;

		#region Fields
		/// <summary>
		/// The binding group that this layout represents.
		/// </summary>
		public readonly BindingGroup Group;

		/// <summary>
		/// The slots for the binding layout, indexed by their shader binding index (slot index). This list contains
		/// all slots, both filled and empty, and is always of length <see cref="SLOT_COUNT"/>.
		/// </summary>
		public IReadOnlyList<Slot> Slots => _slots;
		private readonly Slot[] _slots = new Slot[SLOT_COUNT];

		/// <summary>
		/// Gets the number of slots that are filled in this layout.
		/// </summary>
		public uint SlotCount { get; private set; } = 0;
		#endregion // Fields

		internal BindingLayout(BindingGroup group)
		{
			Group = group;
		}

		// Attempts to merge the other binding layout into this one, throws an exception if a slot is incompatible
		internal void Merge(BindingLayout other, ShaderStages stage)
		{
			for (int i = 0; i < SLOT_COUNT; ++i) {
				// Get slots
				ref var newSlot = ref other._slots[i];
				if (!newSlot.Enabled) {
					continue;
				}
				ref var oldSlot = ref _slots[i];

				// If no old slot, just replace
				if (!oldSlot.Enabled) {
					_slots[i] = newSlot;
					SlotCount += 1; // Increment for new slot
				}
				else { // Check merge validity
					if (newSlot.Type != oldSlot.Type) {
						throw new IncompatibleModuleException(stage, $"mismatch for binding {Group}:{i} type");
					}
					if (newSlot.Count != oldSlot.Count) {
						throw new IncompatibleModuleException(stage, $"mismatch for binding {Group}:{i} array size");
					}
					if (newSlot.BlockSize != oldSlot.BlockSize) {
						throw new IncompatibleModuleException(stage, $"mismatch for binding {Group}:{i} block size");
					}
					if (newSlot.Dims != oldSlot.Dims) {
						throw new IncompatibleModuleException(stage, $"mismatch for binding {Group}:{i} dims");
					}
					oldSlot.Stages |= stage; // Simply update the existing slot with the new stage flag
				}
			}
		}

		// Set a slot based on reflected binding info
		internal void SetSlot(NativeContent.BindingInfo* info, ShaderStages stage)
		{
			// Validate
			var type = info->Type.ToPublicType();
			if (!type.HasValue) {
				throw new InvalidBindingException((uint)Group, info->Slot, $"unsupported binding type {info->Type}");
			}
			if (info->ArraySize == UInt32.MaxValue) {
				throw new InvalidBindingException((uint)Group, info->Slot, "arrays must have a constant size");
			}
			var dims = info->ImageDims.ToPublicType();
			if (!dims.HasValue) {
				throw new InvalidBindingException((uint)Group, info->Slot, "unsupported or invalid binding dims");
			}

			// Create slot
			if (!_slots[info->Slot].Enabled) {
				SlotCount += 1; // Increment for new slot
			}
			_slots[info->Slot] = new(type!.Value, Math.Max(info->ArraySize, 1), info->BlockSize, dims!.Value, stage);
		}

		// Create a descriptor set layout from the current slots, returns null if there are no slots
		internal VkDescriptorSetLayout? CreateDescriptorSetLayout()
		{
			if (SlotCount == 0) {
				return null;
			}

			// Create the binding info
			var bindings = stackalloc VkDescriptorSetLayoutBinding[(int)SlotCount];
			for (int si = 0, bi = 0; si < SLOT_COUNT; ++si) {
				if (!_slots[si].Enabled) {
					continue;
				}

				ref var slot = ref _slots[si];
				bindings[bi++] = new(
					binding: (uint)si,
					descriptorType: (VkDescriptorType)slot.Type,
					descriptorCount: slot.Count,
					stageFlags: (VkShaderStageFlags)slot.Stages,
					immutableSamplers: null
				);
			}

			// Create the set layout
			VkDescriptorSetLayoutCreateInfo dslci = new(
				flags: VkDescriptorSetLayoutCreateFlags.NoFlags, // TODO: Look into push descriptors and template updates
				bindingCount: SlotCount,
				bindings: bindings
			);
			VulkanHandle<VkDescriptorSetLayout> handle;
			Core.Instance!.Graphics.VkDevice.CreateDescriptorSetLayout(&dslci, null, &handle)
				.Throw("Failed to create binding layout object");
			return new(handle, Core.Instance!.Graphics.VkDevice);
		}

		/// <summary>
		/// Contains information about a single binding slot within a layout.
		/// </summary>
		public struct Slot
		{
			#region Fields
			/// <summary>
			/// The resource type for the slot.
			/// </summary>
			public readonly BindingType Type;
			/// <summary>
			/// The number of resources at the slot (>1 implies a binding array).
			/// </summary>
			public readonly uint Count;
			/// <summary>
			/// The size of the block binding, or zero for non-block binding types.
			/// </summary>
			public readonly uint BlockSize;
			/// <summary>
			/// The dimensions of the texture or sampler resource.
			/// </summary>
			public BindingDims Dims;
			/// <summary>
			/// The shader stages in which the slot binding is accessed.
			/// </summary>
			public ShaderStages Stages { get; internal set; }

			/// <summary>
			/// Gets if the slot is enabled (count > 0).
			/// </summary>
			public readonly bool Enabled => Count != 0;
			#endregion // Fields

			internal Slot(BindingType type, uint count, uint bsize, BindingDims dims, ShaderStages stage)
			{
				Type = type;
				Count = count;
				BlockSize = bsize;
				Dims = dims;
				Stages = stage;
			}
		}
	}
}
