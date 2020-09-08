/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega
{
	/// <summary>
	/// Cursor/window interaction modes.
	/// </summary>
	public enum CursorMode
	{
		/// <summary>
		/// The system cursor appears over the window, and can move freely around and off of the window.
		/// </summary>
		Normal,
		/// <summary>
		/// The cursor can move freely, but is hidden while over the window.
		/// </summary>
		Hidden,
		/// <summary>
		/// The cursor is invisible and is locked in the window, and virtual mouse inputs are provided to maintain
		/// full logical mouse movements.
		/// </summary>
		Locked
	}


	/// <summary>
	/// Manages a single application window.
	/// </summary>
	public class Window : IDisposable
	{
		#region Fields
		// GLFW window handle
		internal IntPtr Handle { get; private set; } = IntPtr.Zero;

		#region Properties
		/// <summary>
		/// The window title (text in the menu bar).
		/// </summary>
		public string Title
		{
			get => _title;
			set {
				if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
				Glfw.SetWindowTitle(Handle, value);
				_title = value;
			}
		}
		private string _title;
		/// <summary>
		/// If the window is resizeable by the user by dragging on the window. This does not affect programatic
		/// resizing of the window.
		/// </summary>
		public bool Resizeable
		{
			get => _resizeable;
			set {
				if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
				Glfw.SetWindowAttrib(Handle, Glfw.RESIZABLE, value ? Glfw.TRUE : Glfw.FALSE);
				_resizeable = value;
			}
		}
		private bool _resizeable;
		/// <summary>
		/// If the window is decorated with a title bar and surrounding frame.
		/// </summary>
		public bool Decorated
		{
			get => _decorated;
			set {
				if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
				Glfw.SetWindowAttrib(Handle, Glfw.DECORATED, value ? Glfw.TRUE : Glfw.FALSE);
				_decorated = value;
			}
		}
		private bool _decorated;
		/// <summary>
		/// If the window is floating (always on top of other windows). Use this sparingly, as it can be annoying to
		/// users.
		/// </summary>
		public bool Floating
		{
			get => _floating;
			set {
				if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
				Glfw.SetWindowAttrib(Handle, Glfw.FLOATING, value ? Glfw.TRUE : Glfw.FALSE);
				_floating = value;
			}
		}
		private bool _floating;
		/// <summary>
		/// The interation mode for the cursor within this window.
		/// </summary>
		public CursorMode CursorMode
		{
			get {
				if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
				return Glfw.GetInputMode(Handle, Glfw.CURSOR) switch {
					Glfw.CURSOR_DISABLED => CursorMode.Locked,
					Glfw.CURSOR_HIDDEN => CursorMode.Hidden,
					_ => CursorMode.Normal
				};
			}
			set {
				if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
				var mode = value switch { 
					CursorMode.Hidden => Glfw.CURSOR_HIDDEN,
					CursorMode.Locked => Glfw.CURSOR_DISABLED,
					_ => Glfw.CURSOR_NORMAL
				};
				Glfw.SetInputMode(Handle, Glfw.CURSOR, mode);
			}
		}
		#endregion // Properties

		#region Status
		/// <summary>
		/// Gets if this window has been requested to close (either from user interaction or system message).
		/// </summary>
		public bool CloseRequested =>
			!IsDisposed ? Glfw.WindowShouldClose(Handle) : throw new ObjectDisposedException(nameof(Window));
		/// <summary>
		/// Gets if the window is currently visible (not hidden, and not minimized).
		/// </summary>
		public bool Visible => !IsDisposed 
			? Glfw.GetWindowAttrib(Handle, Glfw.VISIBLE) == Glfw.TRUE 
			: throw new ObjectDisposedException(nameof(Window));
		/// <summary>
		/// Gets if the window currently has active focus.
		/// </summary>
		public bool Focused => !IsDisposed
			? Glfw.GetWindowAttrib(Handle, Glfw.FOCUSED) == Glfw.TRUE
			: throw new ObjectDisposedException(nameof(Window));
		/// <summary>
		/// Gets if the window is currently iconified (minimized to the system taskbar).
		/// </summary>
		public bool Iconified => !IsDisposed
			? Glfw.GetWindowAttrib(Handle, Glfw.ICONIFIED) == Glfw.TRUE
			: throw new ObjectDisposedException(nameof(Window));
		#endregion // Status

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
			Resizeable = true;
			Decorated = true;
			Floating = false;
			CursorMode = CursorMode.Normal;
		}
		~Window()
		{
			dispose(false);
		}

		#region Window Actions
		/// <summary>
		/// Marks the window as requested to be closed. User code will still need to check this value and react
		/// appropriately. To force close a window immediately, call <see cref="Dispose"/>.
		/// </summary>
		public void RequestClose()
		{
			if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
			Glfw.SetWindowShouldClose(Handle, true);
		}

		/// <summary>
		/// Hides the window (not iconification). Has no effect on fullscreen windows.
		/// </summary>
		public void Hide()
		{
			if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
			Glfw.HideWindow(Handle);
		}

		/// <summary>
		/// Shows a window that is hidden. Has no effect on fullscreen windows.
		/// </summary>
		public void Show()
		{
			if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
			Glfw.ShowWindow(Handle);
		}

		/// <summary>
		/// Sets the window to have active focus. Use sparingly, as this can annoy the user.
		/// </summary>
		public void Focus()
		{
			if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
			Glfw.FocusWindow(Handle);
		}

		/// <summary>
		/// Causes the window to request attention from the user.
		/// </summary>
		public void RequestAttention()
		{
			if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
			Glfw.RequestWindowAttention(Handle);
		}

		/// <summary>
		/// Minimizes the window to the system taskbar.
		/// </summary>
		public void Iconify()
		{
			if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
			Glfw.IconifyWindow(Handle);
		}

		/// <summary>
		/// Restores the window from the system taskbar.
		/// </summary>
		public void Restore()
		{
			if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
			Glfw.RestoreWindow(Handle);
		}
		#endregion // Window Actions

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
				Glfw.DestroyWindow(Handle);
				Core.Instance?.RemoveWindow(this);
			}

			Handle = IntPtr.Zero;
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
