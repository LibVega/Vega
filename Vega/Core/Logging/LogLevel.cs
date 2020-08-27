/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega
{
	/// <summary>
	/// Represents the level of importance of a logged message. Can be used as a mask.
	/// </summary>
	[Flags]
	public enum LogLevel
	{
		/// <summary>
		/// Represents a mask of no logging levels.
		/// </summary>
		None      = 0x00,
		/// <summary>
		/// Very low importance debug information messages.
		/// </summary>
		Debug     = 0x01,
		/// <summary>
		/// Standard importance nominal information messages.
		/// </summary>
		Info      = 0x02,
		/// <summary>
		/// Elevated importance off-nominal messages - the application can continue operating normally.
		/// </summary>
		Warning   = 0x04,
		/// <summary>
		/// High importance off-nominal messages - the application may need to make changes to continue operation.
		/// </summary>
		Error     = 0x08,
		/// <summary>
		/// Very high importance error messages - the application must exit and cannot recover from the error state.
		/// </summary>
		Fatal     = 0x10,
		/// <summary>
		/// Special error message that is reporting a caught exception.
		/// </summary>
		Exception = 0x20,
		/// <summary>
		/// Represents a mask of all messages of <see cref="Warning"/> importance or higher.
		/// </summary>
		Important = (Warning | Error | Fatal | Exception),
		/// <summary>
		/// Represents a mask of all non-<see cref="Debug"/> messages.
		/// </summary>
		All       = (Info | Warning | Error | Fatal | Exception),
		/// <summary>
		/// Represents a mask of all message levels, including <see cref="Debug"/>.
		/// </summary>
		Verbose   = (Debug | Info | Warning | Error | Fatal | Exception)
	}
}
