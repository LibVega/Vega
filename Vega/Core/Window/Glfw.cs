/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.InteropServices;
using System.Text;
using Vega.Util;

namespace Vega
{
	internal static partial class Glfw
	{
		#region Fields
		// The library handle
		private static readonly EmbeddedLibrary Lib;

		// Error reporting
		private static int _LastErrorCode = Glfw.NO_ERROR;
		private static string _LastErrorDesc = String.Empty;
		public static (int code, string desc) LastError
		{
			get {
				var last = (_LastErrorCode, _LastErrorDesc);
				_LastErrorCode = Glfw.NO_ERROR;
				_LastErrorDesc = String.Empty;
				return last;
			}
		}
		public static bool HasError => _LastErrorCode != Glfw.NO_ERROR;
		#endregion // Fields

		#region Passthrough API
		public static void WindowHint(int hint, int value) => _GlfwWindowHint(hint, value);
		public static void DestroyWindow(IntPtr window) => _GlfwDestroyWindow(window);
		public static bool WindowShouldClose(IntPtr window) => (_GlfwWindowShouldClose(window) == Glfw.TRUE);
		public static void SetWindowShouldClose(IntPtr window, bool value) => 
			_GlfwSetWindowShouldClose(window, value ? Glfw.TRUE : Glfw.FALSE);
		public static void HideWindow(IntPtr window) => _GlfwHideWindow(window);
		public static void ShowWindow(IntPtr window) => _GlfwShowWindow(window);
		public static void FocusWindow(IntPtr window) => _GlfwFocusWindow(window);
		public static void RequestWindowAttention(IntPtr window) => _GlfwRequestWindowAttention(window);
		public static void IconifyWindow(IntPtr window) => _GlfwIconifyWindow(window);
		public static void RestoreWindow(IntPtr window) => _GlfwRestoreWindow(window);
		public static void PollEvents() => _GlfwPollEvents();
		public static bool VulkanSupported() => (_GlfwVulkanSupported() == Glfw.TRUE);
		public static int GetWindowAttrib(IntPtr window, int attrib) => _GlfwGetWindowAttrib(window, attrib);
		public static void SetWindowAttrib(IntPtr window, int attrib, int value) => 
			_GlfwSetWindowAttrib(window, attrib, value);
		public static void GetWindowSize(IntPtr window, out int w, out int h) => 
			_GlfwGetWindowSize(window, out w, out h);
		public static void SetWindowSize(IntPtr window, int w, int h) => _GlfwSetWindowSize(window, w, h);
		public static void GetWindowPos(IntPtr window, out int x, out int y) => 
			_GlfwGetWindowPos(window, out x, out y);
		public static void SetWindowPos(IntPtr window, int x, int y) => _GlfwSetWindowPos(window, x, y);
		public static void SetWindowMonitor(IntPtr window, IntPtr monitor, int x, int y, int w, int h, int refresh) =>
			_GlfwSetWindowMonitor(window, monitor, x, y, w, h, refresh);
		public static int GetInputMode(IntPtr window, int mode) => _GlfwGetInputMode(window, mode);
		public static void SetInputMode(IntPtr window, int mode, int value) => _GlfwSetInputMode(window, mode, value);
		public static IntPtr GetPrimaryMonitor() => _GlfwGetPrimaryMonitor();
		public static void GetMonitorPos(IntPtr monitor, out int x, out int y) => 
			_GlfwGetMonitorPos(monitor, out x, out y);
		public static void GetMonitorPhysicalSize(IntPtr monitor, out int w, out int h) =>
			_GlfwGetMonitorPhysicalSize(monitor, out w, out h);
		public static void GetMonitorContentScale(IntPtr monitor, out float x, out float y) =>
			_GlfwGetMonitorContentScale(monitor, out x, out y);
		public static void GetMonitorWorkarea(IntPtr monitor, out int x, out int y, out int w, out int h) =>
			_GlfwGetMonitorWorkarea(monitor, out x, out y, out w, out h);
		public static void SetMouseButtonCallback(IntPtr window, GLFWmousebuttonfun mouse_button_callback)
			=> _GlfwSetMouseButtonCallback(window, mouse_button_callback);
		public static void SetScrollCallback(IntPtr window, GLFWscrollfun scroll_callback)
			=> _GlfwSetScrollCallback(window, scroll_callback);
		public static void SetKeyCallback(IntPtr window, GLFWkeyfun key_callback) => 
			_GlfwSetKeyCallback(window, key_callback);
		public static void GetCursorPos(IntPtr window, out double x, out double y) => 
			_GlfwGetCursorPos(window, out x, out y);
		public static void SetCursorEnterCallback(IntPtr window, Glfwcursorenterfun func) => 
			_GlfwSetCursorEnterCallback(window, func);
		public static void SetWindowPosCallback(IntPtr window, GLFWwindowposfun func) => 
			_GlfwSetWindowPosCallback(window, func);
		public static void SetWindowSizeCallback(IntPtr window, GLFWwindowsizefun func) => 
			_GlfwSetWindowSizeCallback(window, func);
		public static void SetWindowFocusCallback(IntPtr window, GLFWwindowfocusfun func) => 
			_GlfwSetWindowFocusCallback(window, func);
		public static void SetWindowIconifyCallback(IntPtr window, GLFWwindowiconifyfun func) => 
			_GlfwSetWindowIconifyCallback(window, func);
		#endregion // Passthrough API

		#region API Function Wrappers
		public static unsafe IntPtr CreateWindow(int width, int height, string title, Monitor? monitor)
		{
			byte[] tstr = Encoding.UTF8.GetBytes(title + '\0');

			fixed (byte* tptr = tstr) {
				return _GlfwCreateWindow(width, height, (IntPtr)tptr, monitor?.Handle ?? IntPtr.Zero, IntPtr.Zero);
			}
		}

		public static IntPtr[] GetMonitors()
		{
			IntPtr mptr = _GlfwGetMonitors(out int count);

			IntPtr[] mons = new IntPtr[count];
			for (int i = 0; i < count; ++i, mptr += IntPtr.Size)
				mons[i] = Marshal.ReadIntPtr(mptr);
			return mons;
		}

		public static VidMode[] GetVideoModes(IntPtr monitor)
		{
			IntPtr mptr = _GlfwGetVideoModes(monitor, out int count);

			VidMode[] modes = new VidMode[count];
			for (int i = 0; i < count; ++i, mptr += (6 * sizeof(int)))
				modes[i] = Marshal.PtrToStructure<VidMode>(mptr);
			return modes;
		}

		public static VidMode GetVideoMode(IntPtr monitor)
		{
			IntPtr mptr = _GlfwGetVideoMode(monitor);
			return Marshal.PtrToStructure<VidMode>(mptr);
		}

		public static string GetMonitorName(IntPtr monitor)
		{
			IntPtr sptr = _GlfwGetMonitorName(monitor);
			return Marshal.PtrToStringAnsi(sptr) ?? "<UNKNOWN>";
		}

		public static unsafe void SetWindowTitle(IntPtr window, string title)
		{
			byte[] tstr = Encoding.UTF8.GetBytes(title + '\0');
			fixed (byte* tptr = tstr) {
				_GlfwSetWindowTitle(window, (IntPtr)tptr);
			}
		}
		#endregion // API Function Wrappers

		#region Init/Term
		public static void Init()
		{
			// Set error callback
			_GlfwSetErrorCallback((code, desc) => {
				_LastErrorCode = code;
				_LastErrorDesc = desc;
			});

			// Init
			if (_GlfwInit() != Glfw.TRUE) {
				var err = LastError;
				throw new Exception($"Failed to initialize GLFW (code {err.code}): {err.desc}");
			}

			// Install other callbacks
			_GlfwSetMonitorCallback((mon, @event) => Monitor.MonitorUpdate(mon, @event == Glfw.CONNECTED));

			// Setup window conditions
			Glfw.WindowHint(Glfw.CLIENT_API, Glfw.NO_API);
		}

		public static void Terminate()
		{
			Monitor._Monitors = null;

			_GlfwTerminate();
		}
		#endregion Init/Term

		static Glfw()
		{
			Lib = new EmbeddedLibrary(typeof(Glfw).Assembly, "Vega.Lib.glfw3", "glfw3");
			var _ = Lib.Handle; // Causes the library to be loaded

			// Check the version
			_GlfwGetVersion = LoadFunc<Delegates.glfwGetVersion>();
			_GlfwGetVersion(out var vmaj, out var vmin, out var vrev);
			if (vmaj < 3 || vmin < 3)
				throw new PlatformNotSupportedException($"Vega requires GLFW 3.3 or later (found {vmaj}.{vmin}.{vrev})");

			// Load the functions
			_GlfwInit = LoadFunc<Delegates.glfwInit>();
			_GlfwTerminate = LoadFunc<Delegates.glfwTerminate>();
			_GlfwSetErrorCallback = LoadFunc<Delegates.glfwSetErrorCallback>();
			_GlfwWindowHint = LoadFunc<Delegates.glfwWindowHint>();
			_GlfwCreateWindow = LoadFunc<Delegates.glfwCreateWindow>();
			_GlfwDestroyWindow = LoadFunc<Delegates.glfwDestroyWindow>();
			_GlfwWindowShouldClose = LoadFunc<Delegates.glfwWindowShouldClose>();
			_GlfwSetWindowShouldClose = LoadFunc<Delegates.glfwSetWindowShouldClose>();
			_GlfwHideWindow = LoadFunc<Delegates.glfwHideWindow>();
			_GlfwShowWindow = LoadFunc<Delegates.glfwShowWindow>();
			_GlfwFocusWindow = LoadFunc<Delegates.glfwFocusWindow>();
			_GlfwRequestWindowAttention = LoadFunc<Delegates.glfwRequestWindowAttention>();
			_GlfwIconifyWindow = LoadFunc<Delegates.glfwIconifyWindow>();
			_GlfwRestoreWindow = LoadFunc<Delegates.glfwRestoreWindow>();
			_GlfwPollEvents = LoadFunc<Delegates.glfwPollEvents>();
			_GlfwVulkanSupported = LoadFunc<Delegates.glfwVulkanSupported>();
			_GlfwGetWindowAttrib = LoadFunc<Delegates.glfwGetWindowAttrib>();
			_GlfwSetWindowAttrib = LoadFunc<Delegates.glfwSetWindowAttrib>();
			_GlfwGetWindowSize = LoadFunc<Delegates.glfwGetWindowSize>();
			_GlfwSetWindowSize = LoadFunc<Delegates.glfwSetWindowSize>();
			_GlfwGetWindowPos = LoadFunc<Delegates.glfwGetWindowPos>();
			_GlfwSetWindowPos = LoadFunc<Delegates.glfwSetWindowPos>();
			_GlfwSetWindowMonitor = LoadFunc<Delegates.glfwSetWindowMonitor>();
			_GlfwGetInputMode = LoadFunc<Delegates.glfwGetInputMode>();
			_GlfwSetInputMode = LoadFunc<Delegates.glfwSetInputMode>();
			_GlfwGetPrimaryMonitor = LoadFunc<Delegates.glfwGetPrimaryMonitor>();
			_GlfwGetMonitors = LoadFunc<Delegates.glfwGetMonitors>();
			_GlfwGetMonitorPos = LoadFunc<Delegates.glfwGetMonitorPos>();
			_GlfwGetVideoModes = LoadFunc<Delegates.glfwGetVideoModes>();
			_GlfwGetVideoMode = LoadFunc<Delegates.glfwGetVideoMode>();
			_GlfwGetMonitorPhysicalSize = LoadFunc<Delegates.glfwGetMonitorPhysicalSize>();
			_GlfwGetMonitorContentScale = LoadFunc<Delegates.glfwGetMonitorContentScale>();
			_GlfwGetMonitorWorkarea = LoadFunc<Delegates.glfwGetMonitorWorkarea>();
			_GlfwGetMonitorName = LoadFunc<Delegates.glfwGetMonitorName>();
			_GlfwSetMonitorCallback = LoadFunc<Delegates.glfwSetMonitorCallback>();
			_GlfwSetWindowTitle = LoadFunc<Delegates.glfwSetWindowTitle>();
			_GlfwSetMouseButtonCallback = LoadFunc<Delegates.glfwSetMouseButtonCallback>();
			_GlfwSetScrollCallback = LoadFunc<Delegates.glfwSetScrollCallback>();
			_GlfwSetKeyCallback = LoadFunc<Delegates.glfwSetKeyCallback>();
			_GlfwGetCursorPos = LoadFunc<Delegates.glfwGetCursorPos>();
			_GlfwSetCursorEnterCallback = LoadFunc<Delegates.glfwSetCursorEnterCallback>();
			_GlfwSetWindowPosCallback = LoadFunc<Delegates.glfwSetWindowPosCallback>();
			_GlfwSetWindowSizeCallback = LoadFunc<Delegates.glfwSetWindowSizeCallback>();
			_GlfwSetWindowFocusCallback = LoadFunc<Delegates.glfwSetWindowFocusCallback>();
			_GlfwSetWindowIconifyCallback = LoadFunc<Delegates.glfwSetWindowIconifyCallback>();
			_GlfwGetPhysicalDevicePresentationSupport = LoadFunc<Delegates.glfwGetPhysicalDevicePresentationSupport>();
			_GlfwCreateWindowSurface = LoadFunc<Delegates.glfwCreateWindowSurface>();
		}

		[StructLayout(LayoutKind.Explicit, Size=6*sizeof(int))]
		public struct VidMode
		{
			[FieldOffset(0)]
			public int Width;
			[FieldOffset(1*sizeof(int))]
			public int Height;
			[FieldOffset(2*sizeof(int))]
			public int RedBits;
			[FieldOffset(3*sizeof(int))]
			public int GreenBits;
			[FieldOffset(4*sizeof(int))]
			public int BlueBits;
			[FieldOffset(5*sizeof(int))]
			public int RefreshRate;
		}
	}
}
