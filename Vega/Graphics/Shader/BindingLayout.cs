/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using Vega.Graphics.Reflection;

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
		public const uint SLOT_COUNT = BindingSet.MAX_SLOT_COUNT;

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
		#endregion // Fields

		internal BindingLayout(BindingGroup group)
		{
			Group = group;
		}

		// Attempts to add a slot, throws an exception if the slot is incompatible
		internal void Add(BindingInfo info, ShaderStages stage)
		{
			// Validate
			var slot = info.Slot;
			if (slot >= SLOT_COUNT) {
				throw new ArgumentOutOfRangeException(nameof(info), "Invalid slot index");
			}

			// Check or add slot
			var asize = info.ArraySize.GetValueOrDefault(1);
			var bsize = info.BlockSize.GetValueOrDefault(0);
			var tdims = info.TextureDims.GetValueOrDefault(TextureDims.E1D);
			if (_slots[info.Slot].Enabled) {
				ref var exslot = ref _slots[slot];
				if (info.Type != exslot.Type) {
					throw new IncompatibleModuleException(stage, $"mismatch for binding {Group}:{slot} type");
				}
				if (asize != exslot.Count) {
					throw new IncompatibleModuleException(stage, $"mismatch for binding {Group}:{slot} array size");
				}
				if (bsize != exslot.BlockSize) {
					throw new IncompatibleModuleException(stage, $"mismatch for binding {Group}:{slot} block size");
				}
				if (tdims != exslot.TextureDims) {
					throw new IncompatibleModuleException(stage, $"mismatch for binding {Group}:{slot} texture dims");
				}
				exslot.Stages |= stage; // Update access stage flags
			}
			else {
				_slots[info.Slot] = new(info.Type, asize, bsize, tdims, stage);
			}
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
			public TextureDims TextureDims;
			/// <summary>
			/// The shader stages in which the slot binding is accessed.
			/// </summary>
			public ShaderStages Stages { get; internal set; }

			/// <summary>
			/// Gets if the slot is enabled (count > 0).
			/// </summary>
			public readonly bool Enabled => Count != 0;
			#endregion // Fields

			internal Slot(BindingType type, uint count, uint bsize, TextureDims dims, ShaderStages stage)
			{
				Type = type;
				Count = count;
				BlockSize = bsize;
				TextureDims = dims;
				Stages = stage;
			}
		}
	}
}
