/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;

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
		/// The hub used to manage all core event messages, can be extended to additionally handle user messages.
		/// </summary>
		public EventHub Events { get; private set; }

		/// <summary>
		/// Reports if the application is in a frame (between <see cref="BeginFrame"/> and <see cref="EndFrame"/>).
		/// </summary>
		public bool InFrame { get; private set; } = false;
		/// <summary>
		/// Reports if the application has been requested to close. The main application loop should check this.
		/// </summary>
		public bool ShouldExit { get; private set; } = false;

		/// <summary>
		/// The list of all open windows in the application.
		/// </summary>
		public IReadOnlyList<Window> Windows => _windows;
		private readonly List<Window> _windows = new();
		/// <summary>
		/// Gets the current main window (the oldest open window).
		/// </summary>
		public Window? MainWindow => (_windows.Count > 0) ? _windows[0] : null;

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

			// Initialize event hub and logging
			Events = new();

			// Attach to exit events
			Console.CancelKeyPress += (_, e) => {
				e.Cancel = true;
				this.ShouldExit = true;
			};

			// Initialize the dependencies
			Glfw.Init();
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

			// Run window frames
			foreach (var win in _windows) {
				win.BeginFrame();
			}
			Glfw.PollEvents();

			// Tick begin coroutines
			CoroutineManager.Tick(CoroutinePolicy.Beginning);
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

			// End window frames
			foreach (var win in _windows) {
				win.EndFrame();
			}

			CoroutineManager.Tick(CoroutinePolicy.End);
		}
		#endregion // Frame

		#region Coroutine
		/// <summary>
		/// Sets the passed coroutine as running and starts ticking it.
		/// </summary>
		/// <param name="coroutine">The coroutine to start.</param>
		/// <returns>The passed coroutine, for chaining.</returns>
		public Coroutine StartCoroutine(Coroutine coroutine)
		{
			if (coroutine.Running) 
				throw new InvalidOperationException("Cannot start a coroutine that is already scheduled");
			CoroutineManager.AddCoroutine(coroutine);
			return coroutine;
		}

		/// <summary>
		/// Starts the passed enumerator as a new coroutine. The enumerator should use the return values as described
		/// in <see cref="Coroutine.Tick"/>.
		/// </summary>
		/// <param name="enumerator">The enumerator to start as a coroutine.</param>
		/// <param name="unscaled">If the enumerator should use unscaled time to time-based pauses.</param>
		/// <returns>A coroutine object representing the enumerator.</returns>
		public Coroutine StartCoroutine(IEnumerator<object?> enumerator, bool unscaled = false)
		{
			var cor = new EnumeratorCoroutine(enumerator, unscaled);
			CoroutineManager.AddCoroutine(cor);
			return cor;
		}

		/// <summary>
		/// Schedules a delayed, optionally repeating action as a new coroutine.
		/// </summary>
		/// <param name="action">The action to execute as the coroutine.</param>
		/// <param name="delay">The delay (in seconds) before executing the action for the first time.</param>
		/// <param name="repeat"><see langword="null"/> to not repeat, or the repeat delay in seconds.</param>
		/// <param name="unscaled">If the delay timing uses unscaled time.</param>
		/// <returns>A coroutine object representing the action.</returns>
		public Coroutine ScheduleAction(Action action, float delay, float? repeat = null, bool unscaled = false)
		{
			var cor = new TimerCoroutine(delay, repeat, unscaled, () => { action(); return true; });
			CoroutineManager.AddCoroutine(cor);
			return cor;
		}

		/// <inheritdoc cref="ScheduleAction(Action, float, float?, bool)"/>
		public Coroutine ScheduleAction(Action action, TimeSpan delay, TimeSpan? repeat = null, bool unscaled = false)
			=> ScheduleAction(action, (float)delay.TotalSeconds, (float?)repeat?.TotalSeconds ?? null, unscaled);

		/// <summary>
		/// Schedules a delayed, optionally repeating action as a new coroutine.
		/// </summary>
		/// <param name="action">
		/// The action to execute as the coroutine, where the return value controls if the action repeats.
		/// </param>
		/// <param name="delay">The delay (in seconds) before executing the action for the first time.</param>
		/// <param name="repeat"><see langword="null"/> to not repeat, or the repeat delay in seconds.</param>
		/// <param name="unscaled">If the delay timing uses unscaled time.</param>
		/// <returns>A coroutine object representing the action.</returns>
		public Coroutine ScheduleAction(Func<bool> action, float delay, float? repeat = null, bool unscaled = false)
		{
			var cor = new TimerCoroutine(delay, repeat, unscaled, action);
			CoroutineManager.AddCoroutine(cor);
			return cor;
		}

		/// <inheritdoc cref="ScheduleAction(Func{bool}, float, float?, bool)"/>
		public Coroutine ScheduleAction(Func<bool> action, TimeSpan delay, TimeSpan? repeat = null, bool unscaled = false)
			=> ScheduleAction(action, (float)delay.TotalSeconds, (float?)repeat?.TotalSeconds ?? null, unscaled);
		#endregion // Coroutine

		#region Window
		/// <summary>
		/// Creates and opens a new window, with its own render commands and input handling.
		/// </summary>
		/// <param name="title">The initial title of the new window.</param>
		/// <param name="width">The width of the new window.</param>
		/// <param name="height">The height of the new window.</param>
		/// <returns>The new Window instance, which is added to <see cref="Windows"/>.</returns>
		public Window CreateWindow(string title, uint width, uint height)
		{
			var win = new Window(title, width, height);
			_windows.Add(win);
			return win;
		}

		// Remove the window from the internal list
		internal void RemoveWindow(Window win) => _windows.Remove(win);
		#endregion // Window

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed) {
				// Close open windows
				while (_windows.Count > 0) {
					_windows[0].Dispose();
				}

				CoroutineManager.Cleanup();
				Events.ClearAll();
			}

			// Terminate libraries
			Glfw.Terminate();

			IsDisposed = true;
			Instance = null;
		}
		#endregion // IDisposable
	}
}
