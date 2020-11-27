/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega
{
	/// <summary>
	/// Implements functionality for delayed and spread-out task execution using enumerators.
	/// </summary>
	public abstract class Coroutine
	{
		/// <summary>
		/// Special return value for <see cref="Tick"/> to signal the coroutine has completed execution.
		/// </summary>
		protected internal static readonly object END = new();

		#region Fields
		// Record of wait object implementation
		internal WaitObjects WaitImpl = new(0, null);

		/// <summary>
		/// Gets if the coroutine is currently in a waiting phase.
		/// </summary>
		public bool Waiting => Running && ((WaitImpl.Time > 0) || (WaitImpl.Coroutine != null));

		/// <summary>
		/// The number of times that this coroutine has been ticked.
		/// </summary>
		public uint TickCount { get; internal set; } = 0;

		/// <summary>
		/// If the coroutine is currently running (being actively ticked).
		/// </summary>
		public bool Running { get; internal set; } = false;

		/// <summary>
		/// If the coroutine uses unscaled (<see cref="AppTime.RealDelta"/>) time for time-based pauses. Defaults to
		/// <see langword="false"/>.
		/// </summary>
		public virtual bool UseUnscaledTime => false;
		#endregion // Fields

		#region Waiting
		// Implementation of waitable objects on coroutine
		internal record WaitObjects(float Time, Coroutine? Coroutine);
		// Implementation of return value for time-waiting coroutines
		internal record WaitForTimeImpl(float Seconds);

		/// <summary>
		/// Returns an object compatible with the <see cref="Tick"/> return value to cause the coroutine to wait for
		/// the given number of seconds.
		/// </summary>
		/// <param name="seconds">The number of seconds to wait the coroutine for.</param>
		/// <returns>The opaque wait object.</returns>
		public static object WaitForSeconds(float seconds) => new WaitForTimeImpl(seconds);

		/// <summary>
		/// Returns an object compatible with the <see cref="Tick"/> return value to cause the coroutine to wait for
		/// the given time span.
		/// </summary>
		/// <param name="time">The time to wait the coroutine for.</param>
		/// <returns>The opaque wait object.</returns>
		public static object WaitForTime(TimeSpan time) => new WaitForTimeImpl((float)time.TotalSeconds);
		#endregion // Waiting

		/// <summary>
		/// Implements the ticking logic for the coroutine. The return value controls the execution of the coroutine,
		/// and can be any of:
		/// <list type="bullet">
		/// <item>
		///		<term><c>null</c></term>
		///		<description>Continue ticking the coroutine as normal.</description>
		/// </item>
		/// <item>
		///		<term><see cref="END"/></term>
		///		<description>End the execution of the coroutine.</description>
		/// </item>
		/// <item>
		///		<term><see cref="WaitForSeconds"/> or <see cref="WaitForTime"/></term>
		///		<description>Pause execution of the coroutine for the given amount of time.</description>
		/// </item>
		/// <item>
		///		<term>Non-<c>null</c> <see cref="Coroutine"/> instance</term>
		///		<description>Pause execution of the coroutine until the returned coroutine is complete.</description>
		/// </item>
		/// </list>
		/// </summary>
		/// <returns>A value to control the continued execution of the coroutine.</returns>
		protected internal abstract object? Tick();

		/// <summary>
		/// Called by code external to the coroutine to stop the execution.
		/// </summary>
		public void Stop()
		{
			if (Running) OnStop();
			Running = false;
		}

		/// <summary>
		/// Called when the coroutine is externally stopped (such as a call to <see cref="Stop"/>).
		/// </summary>
		protected virtual void OnStop() { }

		/// <summary>
		/// Called when the coroutine is removed from the list of active coroutines. Resource cleanup can be done here.
		/// </summary>
		protected internal virtual void OnRemove() { }
	}

	/// <summary>
	/// Represents a bad return value from <see cref="Coroutine.Tick"/>.
	/// </summary>
	public sealed class CoroutineTickReturnException : Exception
	{
		/// <summary>
		/// The return value that was not understood as a valid <see cref="Coroutine.Tick"/> return value.
		/// </summary>
		public readonly object Value;

		internal CoroutineTickReturnException(object value) :
			base($"Bad Coroutine return value type: {value.GetType().Name}")
		{
			Value = value;
		}
	}
}
