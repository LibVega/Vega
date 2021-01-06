/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vega.Graphics;
using Vega.Input;

namespace Vega
{
	/// <summary>
	/// Window moding values.
	/// </summary>
	public enum WindowMode
	{
		/// <summary>
		/// Non-fullscreen windowed.
		/// </summary>
		Window,
		/// <summary>
		/// Windowed (or borderless) fullscreen, a floating window taking up the whole screen.
		/// </summary>
		FullscreenWindow,
		/// <summary>
		/// Exclusive hardware fullscreen mode.
		/// </summary>
		FullscreenExclusive
	}


	/// <summary>
	/// Manages a single application window.
	/// </summary>
	public class Window : IDisposable
	{
		#region Fields
		// GLFW window handle
		internal IntPtr Handle { get; private set; } = IntPtr.Zero;

		// Window swapchain
		internal readonly Swapchain Swapchain;
		// Possible window renderer
		internal Renderer? Renderer => Swapchain.Renderer;
		/// <summary>
		/// Gets if there is a renderer attached to this window.
		/// </summary>
		public bool HasRenderer => Swapchain.Renderer is not null;
		/// <summary>
		/// Gets the texel format of the window surface.
		/// </summary>
		public TexelFormat SurfaceFormat => (TexelFormat)Swapchain.SurfaceFormat;

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
		/// resizing of the window. This value does not effect fullscreen windows.
		/// </summary>
		public bool Resizeable
		{
			get => (Mode == WindowMode.Window) && _resizeable;
			set {
				if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
				if (Mode != WindowMode.Window) return;
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
			get => (Mode == WindowMode.Window) && _decorated;
			set {
				if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
				if (Mode != WindowMode.Window) return;
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
			get => Mode switch { 
				WindowMode.Window => _floating,
				WindowMode.FullscreenWindow => false,
				_ => true
			};
			set {
				if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
				Glfw.SetWindowAttrib(Handle, Glfw.FLOATING, value ? Glfw.TRUE : Glfw.FALSE);
				_floating = value;
			}
		}
		private bool _floating;
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

		#region State
		/// <summary>
		/// The window position (top-left corner) within global screen space. Setting position only affects windows
		/// that are not fullscreen.
		/// </summary>
		public Point2 Position
		{
			get {
				if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
				Glfw.GetWindowPos(Handle, out var x, out var y);
				return new Point2(x, y);
			}
			set {
				if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
				if (Mode != WindowMode.Window) return;
				Glfw.SetWindowPos(Handle, value.X, value.Y);
			}
		}
		/// <summary>
		/// The size of the content area (not including the frame) of the window. Setting the size only affects windows
		/// that are not fullscreen.
		/// </summary>
		public Extent2D Size
		{
			get {
				if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
				Glfw.GetWindowSize(Handle, out var w, out var h);
				return new Extent2D((uint)w, (uint)h);
			}
			set {
				if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
				if (Mode != WindowMode.Window) return;
				Glfw.SetWindowSize(Handle, (int)value.Width, (int)value.Height);
			}
		}
		/// <summary>
		/// Gets the current rectangular window area.
		/// </summary>
		public Rect WindowArea => new Rect(Position, Size);
		/// <summary>
		/// The current window mode.
		/// </summary>
		public WindowMode Mode { get; private set; }
		/// <summary>
		/// Gets the current monitor that the center of the window is within, or which has the most overlap. Will
		/// return <c>null</c> if the window is not open, or if it is not within any monitor.
		/// </summary>
		public Monitor? CurrentMonitor
		{
			get {
				if (IsDisposed) throw new ObjectDisposedException(nameof(Window));
				if (!Visible || Iconified) return null;
				var mons = Monitor.Monitors;
				var area = WindowArea;

				// Check center
				var center = area.Center;
				foreach (var m in mons) {
					if (m.DisplayArea.Contains(center)) {
						return m;
					}
				}

				// Check overlap
				uint bestArea = 0;
				Monitor? bestMon = null;
				foreach (var m in mons) {
					Rect.Intersect(m.DisplayArea, area, out var overlap);
					if (overlap.Area > bestArea) {
						bestArea = overlap.Area;
						bestMon = m;
					}
				}
				return bestMon;
			}
		}

		/// <summary>
		/// Gets/sets if the window uses vsync. This change will not occur until the next time the window is presented.
		/// </summary>
		public bool VerticalSync
		{
			set => Swapchain.SetVsync(value);
			get => Swapchain.Vsync;
		}
		/// <summary>
		/// Gets if the window only supports vsync presentation.
		/// </summary>
		public bool VerticalSyncOnly => Swapchain.VsyncOnly;

		// State saving for fullscreen switches
		private Rect _savedWindow;
		private Rect _savedMonitor;
		#endregion // State

		#region Events
		/// <summary>
		/// Event for the window position changing.
		/// </summary>
		public event WindowPositionCallback? PositionChanged;
		/// <summary>
		/// Event for the window size changing.
		/// </summary>
		public event WindowSizeCallback? SizeChanged;
		/// <summary>
		/// Event for the window focus status changing.
		/// </summary>
		public event WindowFocusCallback? FocusChanged;
		/// <summary>
		/// Event for the window iconification changing.
		/// </summary>
		public event WindowIconifyCallback? IconifyChanged;
		/// <summary>
		/// Event for the window mode changing.
		/// </summary>
		public event WindowModeCallback? ModeChanged;
		#endregion // Events

		#region Input
		/// <summary>
		/// The keyboard input processing for this window.
		/// </summary>
		public Keyboard Keyboard => !IsDisposed ? _keyboard : throw new ObjectDisposedException(nameof(Window));
		private readonly Keyboard _keyboard;
		/// <summary>
		/// The mouse input processing for this window.
		/// </summary>
		public Mouse Mouse => !IsDisposed ? _mouse : throw new ObjectDisposedException(nameof(Window));
		private readonly Mouse _mouse;
		#endregion // Input

		/// <summary>
		/// Gets if the window has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;

		// Function registers to keep managed delegates from being disposed
		private Glfw.GLFWwindowposfun _posfunc;
		private Glfw.GLFWwindowsizefun _sizefunc;
		private Glfw.GLFWwindowfocusfun _focusfunc;
		private Glfw.GLFWwindowiconifyfun _iconifyfunc;
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
			Mode = WindowMode.Window;

			// Setup callbacks
			_posfunc = (win, x, y) => PositionChanged?.Invoke(this, new(x, y));
			_sizefunc = (win, w, h) => SizeChanged?.Invoke(this, new((uint)w, (uint)h));
			_focusfunc = (win, focus) => FocusChanged?.Invoke(this, focus == Glfw.TRUE);
			_iconifyfunc = (win, icon) => IconifyChanged?.Invoke(this, icon == Glfw.TRUE);
			Glfw.SetWindowPosCallback(Handle, _posfunc);
			Glfw.SetWindowSizeCallback(Handle, _sizefunc);
			Glfw.SetWindowFocusCallback(Handle, _focusfunc);
			Glfw.SetWindowIconifyCallback(Handle, _iconifyfunc);

			// Setup input
			_keyboard = new Keyboard(this);
			_mouse = new Mouse(this);

			// Create swapchain
			Swapchain = new(this);
		}
		~Window()
		{
			dispose(false);
		}

		#region Window Actions
		/// <summary>
		/// Sets the monitor for the window. This function is used to move between windowed and fullscreen states.
		/// </summary>
		/// <param name="monitor">
		/// The monitor to make the window fullscreen on, or <c>null</c> to put the window into windowed mode.
		/// </param>
		/// <param name="mode">
		/// The video mode for exclusive fullscreen, or <c>null</c> to use windowed (borderless) fullscreen.
		/// </param>
		public void SetMonitor(Monitor? monitor, VideoMode? mode = null)
		{
			// Check for same-state
			var targState = (monitor, mode) switch { 
				(null, _) => WindowMode.Window,
				(not null, null) => WindowMode.FullscreenWindow,
				_ => WindowMode.FullscreenExclusive
			};
			var currMon = CurrentMonitor ?? Monitor.Primary;
			if (targState == Mode && monitor == currMon) {
				return;
			}

			// * -> Window
			if (targState == WindowMode.Window) {
				if (Mode != WindowMode.FullscreenExclusive) { // (FSWindow or Window) -> Window
					Glfw.SetWindowPos(Handle, _savedWindow.X, _savedWindow.Y);
					Glfw.SetWindowSize(Handle, (int)_savedWindow.Width, (int)_savedWindow.Height);
				}
				else { // FSExclusive -> Window
					Glfw.SetWindowMonitor(Handle, IntPtr.Zero, _savedWindow.X, _savedWindow.Y,
						(int)_savedWindow.Width, (int)_savedWindow.Height, Glfw.DONT_CARE);
				}

				Glfw.SetWindowAttrib(Handle, Glfw.RESIZABLE, _resizeable ? Glfw.TRUE : Glfw.FALSE);
				Glfw.SetWindowAttrib(Handle, Glfw.DECORATED, _decorated ? Glfw.TRUE : Glfw.FALSE);
				Glfw.SetWindowAttrib(Handle, Glfw.FLOATING, _floating ? Glfw.TRUE : Glfw.FALSE);
			}
			// * -> FSWindow
			else if (targState == WindowMode.FullscreenWindow) {
				var newArea = monitor!.DisplayArea;

				if (Mode == WindowMode.Window) { // Window -> FSWindow
					_savedWindow = WindowArea;
					_savedMonitor = currMon.DisplayArea;

					Glfw.SetWindowAttrib(Handle, Glfw.RESIZABLE, Glfw.FALSE);
					Glfw.SetWindowAttrib(Handle, Glfw.DECORATED, Glfw.FALSE);
					Glfw.SetWindowAttrib(Handle, Glfw.FLOATING, Glfw.FALSE);
					Glfw.SetWindowPos(Handle, newArea.X, newArea.Y);
					Glfw.SetWindowSize(Handle, (int)newArea.Width, (int)newArea.Height);
				}
				else if (Mode == WindowMode.FullscreenWindow) { // Monitor switch, only change pos/size
					Glfw.SetWindowPos(Handle, newArea.X, newArea.Y);
					Glfw.SetWindowSize(Handle, (int)newArea.Width, (int)newArea.Height);
				}
				else { // FSExclusive -> FSWindow
					Glfw.SetWindowMonitor(Handle, IntPtr.Zero, _savedMonitor.X, _savedMonitor.Y,
						(int)_savedMonitor.Width, (int)_savedMonitor.Height, Glfw.DONT_CARE);
					Glfw.SetWindowAttrib(Handle, Glfw.RESIZABLE, Glfw.FALSE);
					Glfw.SetWindowAttrib(Handle, Glfw.DECORATED, Glfw.FALSE);
					Glfw.SetWindowAttrib(Handle, Glfw.FLOATING, Glfw.FALSE);
				}

				Glfw.FocusWindow(Handle);
			}
			// * -> FSExclusive
			else {
				if (Mode == WindowMode.Window) { // Window -> FSExclusive
					_savedWindow = WindowArea;
					_savedMonitor = currMon.DisplayArea;
				}
				else if (Mode == WindowMode.FullscreenWindow) { // FSWindow -> FSExclusive
					// Do nothing - keep the saved values from the last windowed mode
				}
				else { // Monitor change
					// Do nothing - Glfw.SetWindowMonitor will handle all changes needed
				}

				Glfw.SetWindowMonitor(Handle, monitor!.Handle, 0, 0, (int)mode!.Value.Width,
					(int)mode.Value.Height, (int)mode.Value.RefreshRate);
			}

			// Update mode
			ModeChanged?.Invoke(this, Mode, targState);
			Mode = targState;
		}

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

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed) {
				if (disposing) {
					Swapchain?.Dispose();
				}
				Glfw.DestroyWindow(Handle);
				Core.Instance?.RemoveWindow(this);
			}

			Handle = IntPtr.Zero;
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
