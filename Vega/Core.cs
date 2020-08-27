/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega
{
	/// <summary>
	/// Central type for managing the runtime and resources of the whole application. Only one instance of this class
	/// can exist at a time.
	/// </summary>
	public sealed class Core : IDisposable
	{
		/// <summary>
		/// The active core object, if any.
		/// </summary>
		public static Core? Instance { get; private set; } = null;

		#region Members
		/// <summary>
		/// The message/event bus for the application.
		/// </summary>
		public MessageHub Messages { get; private set; }

		/// <summary>
		/// Reports if the application is in a frame (between <see cref="BeginFrame"/> and <see cref="EndFrame"/>).
		/// </summary>
		public bool InFrame { get; private set; } = false;
		/// <summary>
		/// Reports if the application has been requested to close. The main application loop should check this.
		/// </summary>
		public bool ShouldExit { get; private set; } = false;

		/// <summary>
		/// Reports if the core object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;
		#endregion // Members

		/// <summary>
		/// Constructs a new core object instance, which initializes the global resources for the application.
		/// </summary>
		/// <param name="name">The name of the application, cannot be empty or only whitespace.</param>
		public Core(string name)
		{
			if (String.IsNullOrWhiteSpace(name)) {
				throw new ArgumentException("Application name cannot be empty", nameof(name));
			}
			if (Instance != null) {
				throw new InvalidOperationException("Cannot create more than one Core instance");
			}
			Instance = this;

			// Initialize the message bus and logging
			Messages = new();

			// Attach to exit events
			Console.CancelKeyPress += (_, e) => {
				e.Cancel = true;
				this.ShouldExit = true;
			};
		}
		~Core()
		{
			dispose(false);
		}

		/// <summary>
		/// Marks that the application should exit. The code managing the runtime loop should check
		/// <see cref="ShouldExit"/> to properly handle this.
		/// </summary>
		public void Exit() => ShouldExit = true;

		#region Frame
		/// <summary>
		/// Begins the frame for the main application loop. This performs updates for the windowing and input, and
		/// prepares the graphics system for a new frame. This cannot be called if a frame is already active.
		/// </summary>
		public void BeginFrame()
		{
			if (InFrame) {
				throw new InvalidOperationException("Cannot call BeginFrame() if a frame is already active");
			}
			InFrame = true;

			AppTime.Frame();
		}

		/// <summary>
		/// Ends the current frame for the main application loop. This presents the window contents, and performs
		/// per-frame cleanup operations. This cannot be called if a frame is not currently active.
		/// </summary>
		public void EndFrame()
		{
			if (!InFrame) {
				throw new InvalidOperationException("Cannot call EndFrame() if a frame is not active");
			}
			InFrame = false;
		}
		#endregion // Frame

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed) {

			}

			IsDisposed = true;
			Instance = null;
		}
		#endregion // IDisposable
	}
}
