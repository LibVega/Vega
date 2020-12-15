/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;

namespace Vega.Graphics.Reflection
{
	/// <summary>
	/// Represents a collection of <see cref="BindingInfo"/> objects within a specific <see cref="BindingGroup"/>.
	/// </summary>
	public sealed class BindingSet
	{
		/// <summary>
		/// The maximum number of slots available in any binding set.
		/// </summary>
		public const uint MAX_SLOT_COUNT = 8;

		#region Fields
		/// <summary>
		/// The binding group/namespace that this set describes.
		/// </summary>
		public readonly BindingGroup Group;

		/// <summary>
		/// The collection of bindings that belong to this set, indexed by slot number.
		/// </summary>
		public IReadOnlyList<BindingInfo?> Bindings => _bindings;
		private readonly BindingInfo?[] _bindings = new BindingInfo?[MAX_SLOT_COUNT];

		/// <summary>
		/// The number of binding slots that are filled in this set.
		/// </summary>
		public uint BindingCount { get; private set; } = 0;
		#endregion // Fields

		internal BindingSet(BindingGroup group)
		{
			Group = group;
		}

		/// <summary>
		/// Gets the binding info for the given slot index.
		/// </summary>
		/// <param name="index">The slot index to get the binding info for.</param>
		public BindingInfo? this[uint index] =>
			(index < MAX_SLOT_COUNT) ? _bindings[index] : throw new ArgumentOutOfRangeException(nameof(index));

		// Attempts to add the binding, success if not yet set, or matches existing binding
		internal bool TryAdd(BindingInfo info)
		{
			if (_bindings[info.Slot] is not null) {
				var ex = _bindings[info.Slot]!;
				return ex.IsCompatible(info);
			}
			else {
				_bindings[info.Slot] = info;
				BindingCount += 1;
				return true;
			}
		}
	}
}
