/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.InteropServices;
using System.Text;
using Vega.Util;
using Vulkan;

namespace Vega
{
	internal static partial class Glfw
	{
		#region Fields
		// The library handle
		private static readonly NativeLibraryHandle Lib;

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

		public static unsafe string[] GetRequiredInstanceExtensions()
		{
			var exts = (byte**)_GlfwGetRequiredInstanceExtensions(out var count).ToPointer();
			var ret = new string[count];
			for (uint i = 0; i < count; ++i) {
				ret[i] = Marshal.PtrToStringAnsi(new IntPtr(exts[i]))!;
			}
			return ret;
		}

		public static VkResult CreateWindowSurface(VkInstance instance, IntPtr window, out VulkanHandle<VkSurfaceKHR> surf)
		{
			var res = _GlfwCreateWindowSurface(instance.Handle.PtrHandle, window, IntPtr.Zero, out var HANDLE);
			surf = new((res == (int)VkResult.Success) ? HANDLE : IntPtr.Zero);
			return (VkResult)res;
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
			Lib = NativeLibraryHandle.FromEmbedded(typeof(Glfw).Assembly, "Vega.Lib.glfw3", "glfw3");
			var _ = Lib.Handle; // Causes the library to be loaded

			// Check the version
			_GlfwGetVersion = Lib.LoadFunction<Delegates.glfwGetVersion>();
			_GlfwGetVersion(out var vmaj, out var vmin, out var vrev);
			if (vmaj < 3 || vmin < 3)
				throw new PlatformNotSupportedException($"Vega requires GLFW 3.3 or later (found {vmaj}.{vmin}.{vrev})");

			// Load the functions
			_GlfwInit = Lib.LoadFunction<Delegates.glfwInit>();
			_GlfwTerminate = Lib.LoadFunction<Delegates.glfwTerminate>();
			_GlfwSetErrorCallback = Lib.LoadFunction<Delegates.glfwSetErrorCallback>();
			_GlfwWindowHint = Lib.LoadFunction<Delegates.glfwWindowHint>();
			_GlfwCreateWindow = Lib.LoadFunction<Delegates.glfwCreateWindow>();
			_GlfwDestroyWindow = Lib.LoadFunction<Delegates.glfwDestroyWindow>();
			_GlfwWindowShouldClose = Lib.LoadFunction<Delegates.glfwWindowShouldClose>();
			_GlfwSetWindowShouldClose = Lib.LoadFunction<Delegates.glfwSetWindowShouldClose>();
			_GlfwHideWindow = Lib.LoadFunction<Delegates.glfwHideWindow>();
			_GlfwShowWindow = Lib.LoadFunction<Delegates.glfwShowWindow>();
			_GlfwFocusWindow = Lib.LoadFunction<Delegates.glfwFocusWindow>();
			_GlfwRequestWindowAttention = Lib.LoadFunction<Delegates.glfwRequestWindowAttention>();
			_GlfwIconifyWindow = Lib.LoadFunction<Delegates.glfwIconifyWindow>();
			_GlfwRestoreWindow = Lib.LoadFunction<Delegates.glfwRestoreWindow>();
			_GlfwPollEvents = Lib.LoadFunction<Delegates.glfwPollEvents>();
			_GlfwVulkanSupported = Lib.LoadFunction<Delegates.glfwVulkanSupported>();
			_GlfwGetWindowAttrib = Lib.LoadFunction<Delegates.glfwGetWindowAttrib>();
			_GlfwSetWindowAttrib = Lib.LoadFunction<Delegates.glfwSetWindowAttrib>();
			_GlfwGetWindowSize = Lib.LoadFunction<Delegates.glfwGetWindowSize>();
			_GlfwSetWindowSize = Lib.LoadFunction<Delegates.glfwSetWindowSize>();
			_GlfwGetWindowPos = Lib.LoadFunction<Delegates.glfwGetWindowPos>();
			_GlfwSetWindowPos = Lib.LoadFunction<Delegates.glfwSetWindowPos>();
			_GlfwSetWindowMonitor = Lib.LoadFunction<Delegates.glfwSetWindowMonitor>();
			_GlfwGetInputMode = Lib.LoadFunction<Delegates.glfwGetInputMode>();
			_GlfwSetInputMode = Lib.LoadFunction<Delegates.glfwSetInputMode>();
			_GlfwGetPrimaryMonitor = Lib.LoadFunction<Delegates.glfwGetPrimaryMonitor>();
			_GlfwGetMonitors = Lib.LoadFunction<Delegates.glfwGetMonitors>();
			_GlfwGetMonitorPos = Lib.LoadFunction<Delegates.glfwGetMonitorPos>();
			_GlfwGetVideoModes = Lib.LoadFunction<Delegates.glfwGetVideoModes>();
			_GlfwGetVideoMode = Lib.LoadFunction<Delegates.glfwGetVideoMode>();
			_GlfwGetMonitorPhysicalSize = Lib.LoadFunction<Delegates.glfwGetMonitorPhysicalSize>();
			_GlfwGetMonitorContentScale = Lib.LoadFunction<Delegates.glfwGetMonitorContentScale>();
			_GlfwGetMonitorWorkarea = Lib.LoadFunction<Delegates.glfwGetMonitorWorkarea>();
			_GlfwGetMonitorName = Lib.LoadFunction<Delegates.glfwGetMonitorName>();
			_GlfwSetMonitorCallback = Lib.LoadFunction<Delegates.glfwSetMonitorCallback>();
			_GlfwSetWindowTitle = Lib.LoadFunction<Delegates.glfwSetWindowTitle>();
			_GlfwSetMouseButtonCallback = Lib.LoadFunction<Delegates.glfwSetMouseButtonCallback>();
			_GlfwSetScrollCallback = Lib.LoadFunction<Delegates.glfwSetScrollCallback>();
			_GlfwSetKeyCallback = Lib.LoadFunction<Delegates.glfwSetKeyCallback>();
			_GlfwGetCursorPos = Lib.LoadFunction<Delegates.glfwGetCursorPos>();
			_GlfwSetCursorEnterCallback = Lib.LoadFunction<Delegates.glfwSetCursorEnterCallback>();
			_GlfwSetWindowPosCallback = Lib.LoadFunction<Delegates.glfwSetWindowPosCallback>();
			_GlfwSetWindowSizeCallback = Lib.LoadFunction<Delegates.glfwSetWindowSizeCallback>();
			_GlfwSetWindowFocusCallback = Lib.LoadFunction<Delegates.glfwSetWindowFocusCallback>();
			_GlfwSetWindowIconifyCallback = Lib.LoadFunction<Delegates.glfwSetWindowIconifyCallback>();
			_GlfwGetPhysicalDevicePresentationSupport = Lib.LoadFunction<Delegates.glfwGetPhysicalDevicePresentationSupport>();
			_GlfwCreateWindowSurface = Lib.LoadFunction<Delegates.glfwCreateWindowSurface>();
			_GlfwGetRequiredInstanceExtensions = Lib.LoadFunction<Delegates.glfwGetRequiredInstanceExtensions>();
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
