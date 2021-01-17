/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega
{
	/// <summary>
	/// Stored by <see cref="ApplicationBase"/> when internal code generates an unhandled exception.
	/// <para>Captures runtime state about the exception for help with debugging.</para>
	/// </summary>
	public sealed class RuntimeExceptionInfo
	{
		#region Fields
		/// <summary>
		/// The application phase in which the exception occured.
		/// </summary>
		public readonly ApplicationPhase Phase;

		/// <summary>
		/// The unhandled exception that was caught.
		/// </summary>
		public readonly Exception Exception;
		/// <summary>
		/// The type of exception that was caught.
		/// </summary>
		public Type ExceptionType => Exception.GetType();
		/// <summary>
		/// The exception message.
		/// </summary>
		public string Message => Exception.Message;
		/// <summary>
		/// The (potential) inner exception.
		/// </summary>
		public Exception? InnerException => Exception.InnerException;
		#endregion // Fields

		internal RuntimeExceptionInfo(ApplicationPhase phase, Exception exception)
		{
			Phase = phase;
			Exception = exception;
		}
	}
}
