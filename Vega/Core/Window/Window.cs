/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega
{
	/// <summary>
	/// Manages a single application window.
	/// </summary>
	public class Window : IDisposable
	{
		#region Fields
		// GLFW window handle
		internal IntPtr Handle { get; private set; } = IntPtr.Zero;

		/// <summary>
		/// Gets if this window has been requested to close (either from user interaction or system message).
		/// </summary>
		public bool CloseRequested =>
			!IsDisposed ? Glfw.WindowShouldClose(Handle) : throw new ObjectDisposedException(nameof(Window));

		#region Properties
		/// <summary>
		/// The window title (text in the menu bar).
		/// </summary>
		public string Title
		{
			get => _title;
			set {
				if (IsDisposed)
					throw new ObjectDisposedException(nameof(Window));
				Glfw.SetWindowTitle(Handle, value);
				_title = value;
			}
		}
		private string _title;
		#endregion // Properties

		/// <summary>
		/// Gets if the window has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		internal Window(string title, uint width, uint height)
		{
			// Create window handle
			Handle = Glfw.CreateWindow((int)width, (int)height, title, null);
			if (Handle == IntPtr.Zero) {
				var err = Glfw.LastError;
				throw new Exception($"Failed to create window (code {err.code}): {err.desc}");
			}

			// Center the window
			var msize = Monitor.Primary.Size;
			uint winx = (msize.Width / 2) - (width / 2), winy = (msize.Height / 2) - (height / 2);
			Glfw.SetWindowPos(Handle, (int)winx, (int)winy);

			// Setup initial properties
			_title = title;
		}
		~Window()
		{
			dispose(false);
		}

		#region Frame
		internal void BeginFrame()
		{

		}

		internal void EndFrame()
		{

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
				// Cleanup native window
				Glfw.DestroyWindow(Handle);

				Core.Instance?.RemoveWindow(this);
			}

			Handle = IntPtr.Zero;
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
