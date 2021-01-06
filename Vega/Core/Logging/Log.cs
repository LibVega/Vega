/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Vega
{
	/// <summary>
	/// Provides core message logging functionality, by publishing <see cref="LogEvent"/> instances to the
	/// <see cref="Core.Events"/> event bus. The functions are designed to be used statically with <c>static using
	/// Vega.Log</c>.
	/// </summary>
	public static class Log
	{
		private static readonly ThreadLocal<LogEvent> _ThreadEvent = new(() => {
			var evt = new LogEvent();
			evt.Internal = false;
			evt.Exception = null;
			return evt;
		});

		/// <summary>
		/// Logs a message at the <see cref="LogLevel.Debug"/> level.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LDEBUG(string msg, object? sender = null)
		{
			var evt = _ThreadEvent.Value!;
			evt.Message = msg;
			evt.Level = LogLevel.Debug;
			Core.Events.Publish(sender, evt);
		}

		/// <summary>
		/// Logs a message at the <see cref="LogLevel.Info"/> level.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LINFO(string msg, object? sender = null)
		{
			var evt = _ThreadEvent.Value!;
			evt.Message = msg;
			evt.Level = LogLevel.Info;
			Core.Events.Publish(sender, evt);
		}

		/// <summary>
		/// Logs a message at the <see cref="LogLevel.Warn"/> level.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LWARN(string msg, object? sender = null)
		{
			var evt = _ThreadEvent.Value!;
			evt.Message = msg;
			evt.Level = LogLevel.Warning;
			Core.Events.Publish(sender, evt);
		}

		/// <summary>
		/// Logs a message at the <see cref="LogLevel.Debug"/> level.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LERROR(string msg, object? sender = null)
		{
			var evt = _ThreadEvent.Value!;
			evt.Message = msg;
			evt.Level = LogLevel.Error;
			Core.Events.Publish(sender, evt);
		}

		/// <summary>
		/// Logs a message at the <see cref="LogLevel.Debug"/> level.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LFATAL(string msg, object? sender = null)
		{
			var evt = _ThreadEvent.Value!;
			evt.Message = msg;
			evt.Level = LogLevel.Fatal;
			Core.Events.Publish(sender, evt);
		}

		/// <summary>
		/// Logs an exception as a log message event.
		/// </summary>
		/// <param name="ex">The exception to log.</param>
		/// <param name="msg">The optional message to include with the exception.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LEXCEPTION(Exception ex, string? msg = null, object? sender = null)
		{
			var evt = _ThreadEvent.Value!;
			evt.Message = msg ?? ex.Message;
			evt.Level = LogLevel.Exception;
			evt.Exception = ex;
			Core.Events.Publish(sender, evt);
			evt.Exception = null;
		}
	}

	// Identical to Log class, but for the internal messages
	internal static class InternalLog
	{
		private static readonly ThreadLocal<LogEvent> _ThreadEvent = new(() => {
			var evt = new LogEvent();
			evt.Internal = true;
			evt.Exception = null;
			return evt;
		});
		public static LogLevel LevelMask = LogLevel.Verbose;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LDEBUG(string msg, object? sender = null)
		{
			if ((LevelMask & LogLevel.Debug) != 0) {
				var evt = _ThreadEvent.Value!;
				evt.Message = msg;
				evt.Level = LogLevel.Debug;
				Core.Events.Publish(sender, evt); 
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LINFO(string msg, object? sender = null)
		{
			if ((LevelMask & LogLevel.Info) != 0) {
				var evt = _ThreadEvent.Value!;
				evt.Message = msg;
				evt.Level = LogLevel.Info;
				Core.Events.Publish(sender, evt); 
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LWARN(string msg, object? sender = null)
		{
			if ((LevelMask & LogLevel.Warning) != 0) {
				var evt = _ThreadEvent.Value!;
				evt.Message = msg;
				evt.Level = LogLevel.Warning;
				Core.Events.Publish(sender, evt); 
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LERROR(string msg, object? sender = null)
		{
			if ((LevelMask & LogLevel.Error) != 0) {
				var evt = _ThreadEvent.Value!;
				evt.Message = msg;
				evt.Level = LogLevel.Error;
				Core.Events.Publish(sender, evt); 
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LFATAL(string msg, object? sender = null)
		{
			if ((LevelMask & LogLevel.Fatal) != 0) {
				var evt = _ThreadEvent.Value!;
				evt.Message = msg;
				evt.Level = LogLevel.Fatal;
				Core.Events.Publish(sender, evt); 
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LEXCEPTION(Exception ex, string? msg = null, object? sender = null)
		{
			if ((LevelMask & LogLevel.Exception) != 0) {
				var evt = _ThreadEvent.Value!;
				evt.Message = msg ?? ex.Message;
				evt.Level = LogLevel.Exception;
				evt.Exception = ex;
				Core.Events.Publish(sender, evt);
				evt.Exception = null; 
			}
		}
	}
}
