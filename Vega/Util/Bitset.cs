/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.Intrinsics.X86;

namespace Vega.Util
{
	/// <summary>
	/// Manages a compact set of boolean flags stored as bits.
	/// </summary>
	public sealed class Bitset
	{
		#region Fields
		/// <summary>
		/// The number of bits available in the bitset.
		/// </summary>
		public readonly uint Count;

		// Contains the bit data as an array of ulongs
		private readonly ulong[] _data;
		#endregion // Fields

		/// <summary>
		/// Create a new bitset that can store the given number of flags.
		/// </summary>
		/// <param name="count">The number of flags to support in the bitset.</param>
		public Bitset(uint count)
		{
			Count = count;
			_data = new ulong[RoundCount(count) / 64];
		}

		#region Overrides
		public override int GetHashCode()
		{
			if (Count == 0) {
				return 0;
			}
			var running = _data[0];
			for (int i = 1; i < _data.Length; ++i) {
				running ^= _data[i];
			}
			return (int)((running & 0xFFFFFFFF) ^ (running >> 32));
		}

		public override bool Equals(object? obj) => (obj is Bitset bs) && (bs == this);
		#endregion // Overrides

		#region Bit Access
		/// <summary>
		/// Gets or sets the bit at the given index
		/// </summary>
		public bool this[uint index]
		{
			get => GetBit(index);
			set {
				if (value) SetBit(index);
				else ClearBit(index);
			}
		}

		/// <summary>
		/// Get the value of the flag at the given index.
		/// </summary>
		/// <param name="index">The bit index to get the value of.</param>
		/// <returns><c>true</c> if the bit is set, <c>false</c> otherwise.</returns>
		public bool GetBit(uint index) => (index < Count)
			? (_data[index / 64] & (1u << (int)(index % 64))) > 0
			: throw new ArgumentOutOfRangeException(nameof(index));

		/// <summary>
		/// Set the bit at the given index to true (1).
		/// </summary>
		/// <param name="index">The index of the bit to set.</param>
		public void SetBit(uint index)
		{
			if (index >= Count) {
				throw new ArgumentOutOfRangeException(nameof(index));
			}
			_data[index / 64] |= (1u << (int)(index % 64));
		}

		/// <summary>
		/// Clears the bit at the given index to false (0).
		/// </summary>
		/// <param name="index">The index of the bit to clear.</param>
		public void ClearBit(uint index)
		{
			if (index >= Count) {
				throw new ArgumentOutOfRangeException(nameof(index));
			}
			_data[index / 64] &= ~(1u << (int)(index % 64));
		}
		#endregion // Bit Access

		#region Bulk Ops
		/// <summary>
		/// Sets all bits in the bitset to true (1).
		/// </summary>
		public void SetAll()
		{
			if (Count == 0) {
				return;
			}

			for (int i = 0; i < (_data.Length - 1); ++i) {
				_data[i] = UInt64.MaxValue;
			}
			// Setting all to 1 ruins the popcnt instruction, so we need this mask on the last part of data
			_data[^1] = ((Count % 64) != 0) ? ~(UInt64.MaxValue << (int)(Count % 64)) : UInt64.MaxValue;
		}

		/// <summary>
		/// Clears all bits in the bitset to false (0).
		/// </summary>
		public void ClearAll()
		{
			for (int i = 0; i < _data.Length; ++i) {
				_data[i] = 0;
			}
		}
		#endregion // Bulk Ops

		#region Bit Counting
		/// <summary>
		/// Counts the number of bits that are set in the bitset.
		/// </summary>
		/// <returns>The number of set (1, true) bits in the bitset.</returns>
		public uint CountSet()
		{
			uint total = 0;
			for (int i = 0; i < _data.Length; ++i) {
				total += (uint)Popcnt.X64.PopCount(_data[i]);
			}
			return total;
		}

		/// <summary>
		/// Counts the number of bits that are clear in the bitset.
		/// </summary>
		/// <returns>The number of clear (0, false) bits in the bitset.</returns>
		public uint CountClear() => Count - CountSet();

		/// <summary>
		/// Gets the index of the first bit that is set (1, true) in the bitset.
		/// </summary>
		/// <returns>The index of the first set bit, or <c>null</c> if no bits are set.</returns>
		public uint? FirstSet()
		{
			if (Count == 0) {
				return null;
			}

			for (int i = 0; i < _data.Length; ++i) {
				ulong data = _data[i];
				if (data != 0) { // Skip empty
					var cnt = Lzcnt.X64.LeadingZeroCount(data);
					return (uint)(((uint)i * 64) + cnt);
				}
			}
			return null;
		}

		/// <summary>
		/// Gets the index of the first bit that is clear (0, false) in the bitset.
		/// </summary>
		/// <returns>The index of the first clear bit, or <c>null</c> if no bits are clear.</returns>
		public uint? FirstClear()
		{
			if (Count == 0) {
				return null;
			}

			for (int i = 0; i < _data.Length; ++i) {
				ulong data = ~_data[i];
				if (data != 0) { // Skip empty
					var cnt = Lzcnt.X64.LeadingZeroCount(data);
					return (uint)(((uint)i * 64) + cnt);
				}
			}
			return null;
		}
		#endregion // Bit Counting

		#region Operators
		public static bool operator == (Bitset l, Bitset r)
		{
			if (ReferenceEquals(l, r)) {
				return true;
			}
			if (l.Count != r.Count) {
				return false;
			}
			for (int i = 0; i < l._data.Length; ++i) {
				if (l._data[i] != r._data[i]) {
					return false;
				}
			}
			return true;
		}

		public static bool operator != (Bitset l, Bitset r)
		{
			if (ReferenceEquals(l, r)) {
				return false;
			}
			if (l.Count != r.Count) {
				return true;
			}
			for (int i = 0; i < l._data.Length; ++i) {
				if (l._data[i] != r._data[i]) {
					return true;
				}
			}
			return false;
		}
		#endregion // Operators

		#region Utilities
		// Round the given count to the nearest multiple of 64
		private static uint RoundCount(uint count) => ((count % 64) == 0) ? count : (count + 64 - (count % 64));
		#endregion // Utilities
	}
}
