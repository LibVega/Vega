/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Threading;

namespace Vega
{
	/// <summary>
	/// Contains utility functionality for multi-threading and thread-related operations.
	/// </summary>
	public static class Threading
	{
		#region Fields
		/// <summary>
		/// The unique identifier for the main application thread.
		/// </summary>
		public static int MainThreadId => _MainThreadId 
			?? throw new InvalidOperationException("Cannot use Threading class before initializing Vega");
		private static int? _MainThreadId = null;

		/// <summary>
		/// Gets a flag indicating if the calling thread is the main application thread.
		/// </summary>
		public static bool IsMainThread => MainThreadId == Thread.CurrentThread.ManagedThreadId;
		#endregion // Fields

		#region Thread Checks
		/// <summary>
		/// Throws an exception if the calling thread is not the main application thread.
		/// </summary>
		/// <param name="message">The optional message to use in the exception.</param>
		public static void EnsureMainThread(string? message = null)
		{
			if (!IsMainThread) {
				throw new InvalidOperationException(message ?? "Operation not called on main thread");
			}
		}

		// Called to set the calling thread as the main application thread
		internal static void UpdateMainThread() => _MainThreadId = Thread.CurrentThread.ManagedThreadId;
		#endregion // Thread Checks

		static Threading()
		{
			
		}
	}
}
