/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega
{
	/// <summary>
	/// Represents a message published to an <see cref="EventHub"/> from the logging system, such as with 
	/// <see cref="Log"/>.
	/// </summary>
	public sealed class LogEvent
	{
		#region Fields
		/// <summary>
		/// The log message string.
		/// </summary>
		public string Message { get; internal set; } = "";
		/// <summary>
		/// The importance level of the logged message.
		/// </summary>
		public LogLevel Level { get; internal set; }
		/// <summary>
		/// Gets if this log message was generated internally by the library, instead of user code.
		/// </summary>
		public bool Internal { get; internal set; }
		/// <summary>
		/// Optional exception corresponding to the message (will only be populated if <see cref="Level"/> is
		/// <see cref="LogLevel.Exception"/>).
		/// </summary>
		public Exception? Exception { get; internal set; }
		#endregion // Fields
	}
}
