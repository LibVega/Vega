/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Threading;

namespace Vega.Graphics
{
	// Encodes both a unique ID and type in a 4-byte int for graphics resources ("Resource Unique Idenifier")
	internal struct RUID : IEquatable<RUID>
	{
		private static uint _NextId = 0;

		#region Fields
		// The raw ID value
		public readonly uint Value;
		// The type for the resource
		public readonly ResourceType Type => (ResourceType)(Value >> 24);
		#endregion // Fields

		public RUID(ResourceType type) => Value = ((uint)type << 24) | Interlocked.Increment(ref _NextId);

		#region Overrides
		public readonly override int GetHashCode() => Value.GetHashCode();

		public readonly override string ToString() => $"[{Value & 0x00FFFFFF}:{Type}]";

		public readonly override bool Equals(object? obj) => (obj is RUID rid) && (rid.Value == Value);

		readonly bool IEquatable<RUID>.Equals(RUID other) => (other.Value == Value);
		#endregion // Overrides

		#region Operators
		public static bool operator == (RUID l, RUID r) => l.Value == r.Value;

		public static bool operator != (RUID l, RUID r) => l.Value != r.Value;
		#endregion // Operators
	}
}
