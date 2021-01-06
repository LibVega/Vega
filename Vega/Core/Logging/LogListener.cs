/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Vega
{
	/// <summary>
	/// Implements the logic to listen for <see cref="LogEvent"/> messages and handle them.
	/// </summary>
	public abstract class LogListener : IDisposable
	{
		#region Fields
		/// <summary>
		/// The subscription token to the <see cref="EventHub"/> for the listener.
		/// </summary>
		public EventSubscription<LogEvent> Subscription { get; private set; }

		/// <summary>
		/// Gets if the listener is disposed.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		protected LogListener(EventHub hub)
		{
			Subscription = hub.Subscribe<LogEvent>(HandleLogEvent);
		}
		~LogListener()
		{
			OnDispose(false);
		}

		/// <summary>
		/// Used as the callback for handling logging events.
		/// </summary>
		/// <param name="sender">The object that sent the log message.</param>
		/// <param name="time">The time of the log message.</param>
		/// <param name="event">The log message event.</param>
		public abstract void HandleLogEvent(object? sender, TimeSpan time, LogEvent? @event);

		public void Dispose()
		{
			if (!IsDisposed) {
				OnDispose(true);
				Subscription.Dispose();
			}

			IsDisposed = true;
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Virtual function for disposing the log listener.
		/// </summary>
		/// <param name="disposing">If <c>Dispose()</c> was called to dispose the listener.</param>
		protected virtual void OnDispose(bool disposing) { }
	}

	/// <summary>
	/// Default <see cref="ILogListener"/> implementation that formats <see cref="LogEvent"/> messages, and writes them
	/// to a log file.
	/// </summary>
	public sealed class FileLogger : LogListener
	{
		#region Fields
		private readonly StreamWriter _writer;

		/// <summary>
		/// The absolute path of the log file being written to.
		/// </summary>
		public readonly string FilePath;
		#endregion // Fields

		/// <summary>
		/// Create and open a new file logger for the given hub.
		/// </summary>
		/// <param name="hub">The hub to log messages from.</param>
		/// <param name="path">The path to the folder to place log files in.</param>
		/// <param name="saveOld">If old log files should be kept.</param>
		public FileLogger(EventHub hub, string path = "./logs", bool saveOld = true)
			: base(hub)
		{
			string filename = Path.Combine(path, "latest.log");
			var fileInfo = new FileInfo(filename);

			// Create directory, if needed
			if (fileInfo == null || !fileInfo.Exists) {
				Directory.CreateDirectory(Path.GetDirectoryName(filename) ?? path);
				fileInfo = new FileInfo(filename);
			}

			// Save old file, if needed
			if (saveOld && fileInfo.Exists) {
				fileInfo.MoveTo(fileInfo.FullName.Replace("latest", fileInfo.CreationTime.ToString("yyMMdd_HHmmss")));
			}

			// Open the file
			_writer = new StreamWriter(File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Read), 
				encoding: Encoding.UTF8, leaveOpen: false);
			FilePath = (_writer.BaseStream as FileStream)!.Name;

			// Write initial file contents
			_writer.Write($"Log opened - {DateTime.Now.ToString("G")}\n\n");
			_writer.Flush();
		}

		public unsafe override void HandleLogEvent(object? sender, TimeSpan time, LogEvent? @event)
		{
			// Build tag
			var tag = stackalloc char[19] { 
				'[', 'H', 'H', ':', 'M', 'M', ':', 'S', 'S', '.', 's', 's', ']', '[', 'X', ']', ':', ' ', ' '
			};
			tag[1]  = (char)(((time.Hours % 100) / 10) + '0');
			tag[2]  = (char)((time.Hours % 10) + '0');
			tag[4]  = (char)((time.Minutes / 10) + '0');
			tag[5]  = (char)((time.Minutes % 10) + '0');
			tag[7]  = (char)((time.Seconds / 10) + '0');
			tag[8]  = (char)((time.Seconds % 10) + '0');
			tag[10] = (char)((time.Milliseconds / 100) + '0');
			tag[11] = (char)(((time.Milliseconds % 100) / 10) + '0');
			tag[14] = GetLevelTag(@event?.Level);
			ReadOnlySpan<char> tagSpan = new(tag, 19);

			// Write message
			if (@event != null) {
				if (@event.Level == LogLevel.Exception) {
					_writer.Write(tagSpan);
					_writer.Write($"({@event.Exception?.GetType().Name ?? ""})  ");
					_writer.WriteLine(@event.Message);
				}
				else {
					_writer.Write(tagSpan);
					_writer.WriteLine(@event.Message);
				}
			}
			else {
				_writer.Write(tagSpan);
				_writer.WriteLine("null");
			}
			_writer.Flush();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static char GetLevelTag(LogLevel? level) => level.GetValueOrDefault(LogLevel.None) switch {
			LogLevel.Debug => 'D',
			LogLevel.Info => 'I',
			LogLevel.Warning => 'W',
			LogLevel.Error => 'E',
			LogLevel.Fatal => '!',
			LogLevel.Exception => 'X',
			_ => '?'
		};

		protected override void OnDispose(bool disposing)
		{
			if (disposing) {
				_writer.Write($"\nLog closed - {DateTime.Now.ToString("G")}");
				_writer.Flush();
				_writer.Close();
				_writer.Dispose();
			}
		}
	}
}
