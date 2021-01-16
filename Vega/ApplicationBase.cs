/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega
{
	/// <summary>
	/// Provides a runtime management framework over <see cref="Core"/> and some top-level objects. This is designed
	/// to make development of standard Update/Draw loop applications easier.
	/// </summary>
	public abstract class ApplicationBase : IDisposable
	{
		/// <summary>
		/// The active instance of the application. Only one instance can be constructed at once.
		/// </summary>
		public static ApplicationBase? Instance { get; private set; } = null;

		#region Fields
		/// <summary>
		/// The core library object. Will be the same instance as <see cref="Core.Instance"/>.
		/// </summary>
		public readonly Core Core;

		/// <summary>
		/// Gets a flag indicating if the application should exit. This can be triggered either through
		/// </summary>
		public bool ShouldExit => Core.ShouldExit;

		/// <summary>
		/// Flag telling if this application instance has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		/// <summary>
		/// Performs initial startup construction and initialization of the base application objects.
		/// </summary>
		/// <param name="appName">The name of the application.</param>
		/// <param name="appVersion">The application version.</param>
		protected ApplicationBase(string appName, Version appVersion)
		{
			// Check instance
			if (Instance is not null) {
				throw new InvalidOperationException("Cannot construct more than one instance of ApplicationBase");
			}
			Instance = this;

			// Create the core object
			Core = new(appName, appVersion);
		}
		~ApplicationBase()
		{
			dispose(false);
		}

		#region Execution Control
		/// <summary>
		/// Performs final initialization and resource loading, then launches the main application loop.
		/// <para>
		/// Note that this function blocks until the application is exited.
		/// </para>
		/// </summary>
		public void Run()
		{
			mainLoop();
		}

		/// <summary>
		/// Marks that the application should exit. This will happen automatically at the end of the current frame.
		/// <para>
		/// This is equivalent to calling <see cref="Core.Exit"/>.
		/// </para>
		/// </summary>
		public void Exit() => Core.Exit();
		#endregion // Execution Control

		#region Core Loop
		// Implements the core main loop of the application, including calling frame functions and other frame logic
		// This function is blocking
		private void mainLoop()
		{
			while (!ShouldExit) {
				Core.NextFrame();

				Update();
				Render();
			}
		}

		/// <summary>
		/// Called once per frame to perform core application update logic.
		/// </summary>
		public abstract void Update();

		/// <summary>
		/// Called once per frame to perform render command preparation, recording, and submission.
		/// </summary>
		public abstract void Render();
		#endregion // Core Loop

		#region IDisposable
		/// <summary>
		/// Performs cleanup of the application and associated managed objects.
		/// </summary>
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed) {
				if (disposing) {
					Core?.Dispose();
				}
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
