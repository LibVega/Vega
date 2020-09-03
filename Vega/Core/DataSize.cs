/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega
{
	/// <summary>
	/// A lightweight wrapper around a specific amount of data. Note that this uses the traditional binary definition 
	/// (powers of 1024) for conversions. Negative sizes are supported.
	/// </summary>
	public struct DataSize : IEquatable<DataSize>
	{
		#region Conversion
		/// <summary>
		/// Number of bytes in a kilobyte.
		/// </summary>
		public const double KB_RATIO = 1_024.0;
		/// <summary>
		/// Number of bytes in a megabyte.
		/// </summary>
		public const double MB_RATIO = 1_048_576.0;
		/// <summary>
		/// Number of bytes in a gigabyte.
		/// </summary>
		public const double GB_RATIO = 1_073_741_842.0;
		/// <summary>
		/// Number of bytes in a petabyte.
		/// </summary>
		public const double PB_RATIO = 1_099_511_627_776.0;
		#endregion // Conversion

		/// <summary>
		/// Represents zero bytes of data.
		/// </summary>
		public static readonly DataSize Zero = new();

		#region Fields
		private long _bytes;

		/// <summary>
		/// The data size in bytes.
		/// </summary>
		public readonly long B => _bytes;
		/// <summary>
		/// The data size in kilobytes.
		/// </summary>
		public readonly double KB => _bytes / KB_RATIO;
		/// <summary>
		/// The data size in megabytes.
		/// </summary>
		public readonly double MB => _bytes / MB_RATIO;
		/// <summary>
		/// The data size in gigabytes.
		/// </summary>
		public readonly double GB => _bytes / GB_RATIO;
		/// <summary>
		/// The data size in petabytes.
		/// </summary>
		public readonly double PB => _bytes / PB_RATIO;
		#endregion // Fields

		/// <summary>
		/// Constructs a new size object with the given byte count.
		/// </summary>
		/// <param name="bytes">The data size, in bytes.</param>
		public DataSize(long bytes) => _bytes = bytes;

		#region Overrides
		readonly bool IEquatable<DataSize>.Equals(DataSize other) => other == this;

		public readonly override bool Equals(object? obj) => (obj is DataSize ds) && (ds == this);

		public readonly override int GetHashCode() => (int)(_bytes & 0xFFFFFF) ^ (int)(_bytes >> 32);

		public override string ToString() => $"{{{_bytes} B}}";
		#endregion // Overrides

		#region Construction
		/// <summary>
		/// Constructs a new size object with the given size in bytes.
		/// </summary>
		/// <param name="bytes">The size in bytes.</param>
		public static DataSize FromBytes(long bytes) => new DataSize(bytes);
		/// <summary>
		/// Constructs a new size object with the given size in kilobytes.
		/// </summary>
		/// <param name="kilobytes">The size in bytes.</param>
		public static DataSize FromKilo(double kilobytes) => new DataSize((long)(kilobytes * KB_RATIO));
		/// <summary>
		/// Constructs a new size object with the given size in megabytes.
		/// </summary>
		/// <param name="megabytes">The size in bytes.</param>
		public static DataSize FromMega(double megabytes) => new DataSize((long)(megabytes * MB_RATIO));
		/// <summary>
		/// Constructs a new size object with the given size in gigabytes.
		/// </summary>
		/// <param name="gigabytes">The size in bytes.</param>
		public static DataSize FromGiga(double gigabytes) => new DataSize((long)(gigabytes * GB_RATIO));
		/// <summary>
		/// Constructs a new size object with the given size in petabytes.
		/// </summary>
		/// <param name="petabytes">The size in bytes.</param>
		public static DataSize FromPeta(double petabytes) => new DataSize((long)(petabytes * PB_RATIO));
		#endregion // Construction

		#region Operators
		public static bool operator == (in DataSize l, in DataSize r) => l._bytes == r._bytes;
		public static bool operator != (in DataSize l, in DataSize r) => l._bytes != r._bytes;
		public static bool operator <  (in DataSize l, in DataSize r) => l._bytes < r._bytes;
		public static bool operator <= (in DataSize l, in DataSize r) => l._bytes <= r._bytes;
		public static bool operator >  (in DataSize l, in DataSize r) => l._bytes > r._bytes;
		public static bool operator >= (in DataSize l, in DataSize r) => l._bytes >= r._bytes;

		public static DataSize operator + (in DataSize l, in DataSize r) => new DataSize(l._bytes + r._bytes);
		public static DataSize operator - (in DataSize l, in DataSize r) => new DataSize(l._bytes - r._bytes);
		public static DataSize operator * (in DataSize l, double scale) => 
			new DataSize((long)(l._bytes * Math.Max(0, scale)));
		public static DataSize operator * (double scale, in DataSize r) => 
			new DataSize((long)(r._bytes * Math.Max(0, scale)));
		public static DataSize operator / (in DataSize l, double scale) => 
			new DataSize((long)(l._bytes / Math.Max(0, scale)));
		#endregion // Operators
	}
}
