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
		public static bool Init() => (_GlfwInit() == Glfw.TRUE);
		public static void Terminate() => _GlfwTerminate();
		public static void WindowHint(int hint, int value) => _GlfwWindowHint(hint, value);
		public static void DestroyWindow(IntPtr window) => _GlfwDestroyWindow(window);
		public static bool WindowShouldClose(IntPtr window) => (_GlfwWindowShouldClose(window) == Glfw.TRUE);
		public static void PollEvents() => _GlfwPollEvents();
		public static void ShowWindow(IntPtr window) => _GlfwShowWindow(window);
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
		public static IntPtr GetPrimaryMonitor() => _GlfwGetPrimaryMonitor();
		public static void GetMonitorPos(IntPtr monitor, out int x, out int y) => 
			_GlfwGetMonitorPos(monitor, out x, out y);
		public static void SetMouseButtonCallback(IntPtr window, GLFWmousebuttonfun mouse_button_callback)
			=> _GlfwSetMouseButtonCallback(window, mouse_button_callback);
		public static void SetScrollCallback(IntPtr window, GLFWscrollfun scroll_callback)
			=> _GlfwSetScrollCallback(window, scroll_callback);
		public static void SetKeyCallback(IntPtr window, GLFWkeyfun key_callback) => 
			_GlfwSetKeyCallback(window, key_callback);
		public static void GetCursorPos(IntPtr window, out double x, out double y) => 
			_GlfwGetCursorPos(window, out x, out y);
		public static void SetInputMode(IntPtr window, int mode, int value) => 
			_GlfwSetInputMode(window, mode, value);
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
		public static unsafe IntPtr CreateWindow(int width, int height, string title)
		{
			byte[] tstr = Encoding.UTF8.GetBytes(title + '\0');

			fixed (byte* tptr = tstr) {
				return _GlfwCreateWindow(width, height, (IntPtr)tptr, IntPtr.Zero, IntPtr.Zero);
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

		public static unsafe void SetWindowTitle(IntPtr window, string title)
		{
			byte[] tstr = Encoding.UTF8.GetBytes(title + '\0');
			fixed (byte* tptr = tstr) {
				_GlfwSetWindowTitle(window, (IntPtr)tptr);
			}
		}
		#endregion // API Function Wrappers

		static Glfw()
		{
			Lib = new EmbeddedLibrary(typeof(Glfw).Assembly, "glfw3", "glfw3");
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
			_GlfwPollEvents = LoadFunc<Delegates.glfwPollEvents>();
			_GlfwShowWindow = LoadFunc<Delegates.glfwShowWindow>();
			_GlfwVulkanSupported = LoadFunc<Delegates.glfwVulkanSupported>();
			_GlfwGetWindowAttrib = LoadFunc<Delegates.glfwGetWindowAttrib>();
			_GlfwSetWindowAttrib = LoadFunc<Delegates.glfwSetWindowAttrib>();
			_GlfwGetWindowSize = LoadFunc<Delegates.glfwGetWindowSize>();
			_GlfwSetWindowSize = LoadFunc<Delegates.glfwSetWindowSize>();
			_GlfwGetWindowPos = LoadFunc<Delegates.glfwGetWindowPos>();
			_GlfwSetWindowPos = LoadFunc<Delegates.glfwSetWindowPos>();
			_GlfwGetPrimaryMonitor = LoadFunc<Delegates.glfwGetPrimaryMonitor>();
			_GlfwGetMonitors = LoadFunc<Delegates.glfwGetMonitors>();
			_GlfwGetMonitorPos = LoadFunc<Delegates.glfwGetMonitorPos>();
			_GlfwGetVideoModes = LoadFunc<Delegates.glfwGetVideoModes>();
			_GlfwGetVideoMode = LoadFunc<Delegates.glfwGetVideoMode>();
			_GlfwSetWindowTitle = LoadFunc<Delegates.glfwSetWindowTitle>();
			_GlfwSetMouseButtonCallback = LoadFunc<Delegates.glfwSetMouseButtonCallback>();
			_GlfwSetScrollCallback = LoadFunc<Delegates.glfwSetScrollCallback>();
			_GlfwSetKeyCallback = LoadFunc<Delegates.glfwSetKeyCallback>();
			_GlfwGetCursorPos = LoadFunc<Delegates.glfwGetCursorPos>();
			_GlfwSetInputMode = LoadFunc<Delegates.glfwSetInputMode>();
			_GlfwSetCursorEnterCallback = LoadFunc<Delegates.glfwSetCursorEnterCallback>();
			_GlfwSetWindowPosCallback = LoadFunc<Delegates.glfwSetWindowPosCallback>();
			_GlfwSetWindowSizeCallback = LoadFunc<Delegates.glfwSetWindowSizeCallback>();
			_GlfwSetWindowFocusCallback = LoadFunc<Delegates.glfwSetWindowFocusCallback>();
			_GlfwSetWindowIconifyCallback = LoadFunc<Delegates.glfwSetWindowIconifyCallback>();
			_GlfwGetPhysicalDevicePresentationSupport = LoadFunc<Delegates.glfwGetPhysicalDevicePresentationSupport>();
			_GlfwCreateWindowSurface = LoadFunc<Delegates.glfwCreateWindowSurface>();

			// Set error callback
			_GlfwSetErrorCallback((code, desc) => {
				_LastErrorCode = code;
				_LastErrorDesc = desc;
			});
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
