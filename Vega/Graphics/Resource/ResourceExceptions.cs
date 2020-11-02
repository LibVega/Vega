/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// An exception that is generated when an operation does not have enough host memory to complete.
	/// </summary>
	public sealed class OutOfHostMemoryException : Exception
	{
		/// <summary>
		/// The size of the memory operation that failed, if known.
		/// </summary>
		public readonly DataSize Size;

		/// <summary>
		/// Create a new exception with a known memory size.
		/// </summary>
		public OutOfHostMemoryException(DataSize size)
			: base($"Out of host memory (size={size})")
		{
			Size = size;
		}

		/// <summary>
		/// Create a new exception with an unknown memory size.
		/// </summary>
		public OutOfHostMemoryException()
			: base("Out of host memory")
		{
			Size = DataSize.Zero;
		}
	}

	/// <summary>
	/// An exception that is generated when an operation does not have enough device memory to complete.
	/// </summary>
	public sealed class OutOfDeviceMemoryException : Exception
	{
		/// <summary>
		/// The size of the memory operation that failed, if known.
		/// </summary>
		public readonly DataSize Size;

		/// <summary>
		/// Create a new exception with a known memory size.
		/// </summary>
		public OutOfDeviceMemoryException(DataSize size)
			: base($"Out of device memory (size={size})")
		{
			Size = size;
		}

		/// <summary>
		/// Create a new exception with an unknown memory size.
		/// </summary>
		public OutOfDeviceMemoryException()
			: base("Out of device memory")
		{
			Size = DataSize.Zero;
		}
	}
}
