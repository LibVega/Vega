/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Threading;

namespace Vega
{
	/// <summary>
	/// Implements a fast, FIFO-guaranteed critical section locking mechanism. Uses spin-locks, so use should be
	/// limited to instances where the wait time will be very short.
	/// </summary>
	public sealed class FifoLock
	{
		#region Fields
		private ulong _acquire;
		private ulong _release;

		/// <summary>
		/// Gets the number of successful lock attempts on the lock.
		/// </summary>
		public ulong LockCount => Interlocked.Read(ref _acquire);
		#endregion // Fields

		/// <summary>
		/// Constructs a new lock object.
		/// </summary>
		public FifoLock() => _acquire = _release = 0;

		/// <summary>
		/// Acquires the lock, or blocks until it is able to do so.
		/// </summary>
		public void Lock()
		{
			var ticket = Interlocked.Increment(ref _acquire);
			while (ticket != Interlocked.Read(ref _release)) { ; }
		}

		/// <summary>
		/// Attempts to acquire the lock, returns immediately regardless.
		/// </summary>
		/// <returns>If the lock was successfully acquired.</returns>
		public bool TryLock()
		{
			var ticket = Interlocked.Read(ref _release);
			return Interlocked.CompareExchange(ref _acquire, ticket + 1, ticket) == ticket;
		}

		/// <summary>
		/// Releases the lock to allow the next waiting lock, if any, to acquire. Calling this function on a lock that
		/// is not acquired in the current thread will put the lock into an indeterminate state.
		/// </summary>
		public void Unlock()
		{
			Interlocked.Increment(ref _release);
		}
	}
}
