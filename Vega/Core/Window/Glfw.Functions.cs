/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Vega
{
	internal static partial class Glfw
	{
		#region Delegate Fields
		private static readonly Delegates.glfwInit _GlfwInit;
		private static readonly Delegates.glfwTerminate _GlfwTerminate;
		private static readonly Delegates.glfwGetVersion _GlfwGetVersion;
		private static readonly Delegates.glfwSetErrorCallback _GlfwSetErrorCallback;
		private static readonly Delegates.glfwWindowHint _GlfwWindowHint;
		private static readonly Delegates.glfwCreateWindow _GlfwCreateWindow;
		private static readonly Delegates.glfwDestroyWindow _GlfwDestroyWindow;
		private static readonly Delegates.glfwWindowShouldClose _GlfwWindowShouldClose;
		private static readonly Delegates.glfwSetWindowShouldClose _GlfwSetWindowShouldClose;
		private static readonly Delegates.glfwHideWindow _GlfwHideWindow;
		private static readonly Delegates.glfwShowWindow _GlfwShowWindow;
		private static readonly Delegates.glfwFocusWindow _GlfwFocusWindow;
		private static readonly Delegates.glfwRequestWindowAttention _GlfwRequestWindowAttention;
		private static readonly Delegates.glfwIconifyWindow _GlfwIconifyWindow;
		private static readonly Delegates.glfwRestoreWindow _GlfwRestoreWindow;
		private static readonly Delegates.glfwPollEvents _GlfwPollEvents;
		private static readonly Delegates.glfwVulkanSupported _GlfwVulkanSupported;
		private static readonly Delegates.glfwGetWindowAttrib _GlfwGetWindowAttrib;
		private static readonly Delegates.glfwSetWindowAttrib _GlfwSetWindowAttrib;
		private static readonly Delegates.glfwGetWindowSize _GlfwGetWindowSize;
		private static readonly Delegates.glfwSetWindowSize _GlfwSetWindowSize;
		private static readonly Delegates.glfwGetWindowPos _GlfwGetWindowPos;
		private static readonly Delegates.glfwSetWindowPos _GlfwSetWindowPos;
		private static readonly Delegates.glfwSetWindowMonitor _GlfwSetWindowMonitor;
		private static readonly Delegates.glfwGetInputMode _GlfwGetInputMode;
		private static readonly Delegates.glfwSetInputMode _GlfwSetInputMode;
		private static readonly Delegates.glfwGetPrimaryMonitor _GlfwGetPrimaryMonitor;
		private static readonly Delegates.glfwGetMonitors _GlfwGetMonitors;
		private static readonly Delegates.glfwGetMonitorPos _GlfwGetMonitorPos;
		private static readonly Delegates.glfwGetVideoModes _GlfwGetVideoModes;
		private static readonly Delegates.glfwGetVideoMode _GlfwGetVideoMode;
		private static readonly Delegates.glfwGetMonitorPhysicalSize _GlfwGetMonitorPhysicalSize;
		private static readonly Delegates.glfwGetMonitorContentScale _GlfwGetMonitorContentScale;
		private static readonly Delegates.glfwGetMonitorWorkarea _GlfwGetMonitorWorkarea;
		private static readonly Delegates.glfwGetMonitorName _GlfwGetMonitorName;
		private static readonly Delegates.glfwSetMonitorCallback _GlfwSetMonitorCallback;
		private static readonly Delegates.glfwSetWindowTitle _GlfwSetWindowTitle;
		private static readonly Delegates.glfwSetMouseButtonCallback _GlfwSetMouseButtonCallback;
		private static readonly Delegates.glfwSetScrollCallback _GlfwSetScrollCallback;
		private static readonly Delegates.glfwSetKeyCallback _GlfwSetKeyCallback;
		private static readonly Delegates.glfwGetCursorPos _GlfwGetCursorPos;
		private static readonly Delegates.glfwSetCursorEnterCallback _GlfwSetCursorEnterCallback;
		private static readonly Delegates.glfwSetWindowPosCallback _GlfwSetWindowPosCallback;
		private static readonly Delegates.glfwSetWindowSizeCallback _GlfwSetWindowSizeCallback;
		private static readonly Delegates.glfwSetWindowFocusCallback _GlfwSetWindowFocusCallback;
		private static readonly Delegates.glfwSetWindowIconifyCallback _GlfwSetWindowIconifyCallback;
		private static readonly Delegates.glfwGetPhysicalDevicePresentationSupport _GlfwGetPhysicalDevicePresentationSupport;
		private static readonly Delegates.glfwCreateWindowSurface _GlfwCreateWindowSurface;
		#endregion // Delegate Fields

		#region Public Delegate Types
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWerrorfun(int error, string desc);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWmousebuttonfun(IntPtr window, int button, int action, int mods);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWscrollfun(IntPtr window, double xoffset, double yoffset);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWkeyfun(IntPtr window, int key, int scancode, int action, int mods);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void Glfwcursorenterfun(IntPtr window, int entered);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWwindowposfun(IntPtr window, int xpos, int ypos);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWwindowsizefun(IntPtr window, int width, int height);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWwindowfocusfun(IntPtr window, int focused);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWwindowiconifyfun(IntPtr window, int iconified);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWmonitorfun(IntPtr monitor, int @event);
		#endregion // Public Delegate Types

		#region API Delegate Types
		public static class Delegates
		{
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwInit();
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwTerminate();
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetVersion(out int maj, out int min, out int rev);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetErrorCallback(GLFWerrorfun cbfun);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwWindowHint(int hint, int value);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwCreateWindow(int width, int height, IntPtr title, IntPtr monitor, IntPtr share);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwDestroyWindow(IntPtr window);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwWindowShouldClose(IntPtr window);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowShouldClose(IntPtr window, int value);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwHideWindow(IntPtr window);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwShowWindow(IntPtr window);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwFocusWindow(IntPtr window);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwRequestWindowAttention(IntPtr window);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwIconifyWindow(IntPtr window);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwRestoreWindow(IntPtr window);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwPollEvents();
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwVulkanSupported();
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwGetWindowAttrib(IntPtr window, int attrib);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowAttrib(IntPtr window, int attrib, int value);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetWindowSize(IntPtr window, out int width, out int height);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowSize(IntPtr window, int width, int height);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetWindowPos(IntPtr window, out int x, out int y);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowPos(IntPtr window, int w, int y);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowMonitor(IntPtr window, IntPtr monitor, int x, int y, int w, int h, int refresh);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwGetInputMode(IntPtr window, int mode);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetInputMode(IntPtr window, int mode, int value);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetPrimaryMonitor();
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetMonitors(out int count);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetMonitorPos(IntPtr monitor, out int x, out int y);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetVideoModes(IntPtr monitor, out int count);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetVideoMode(IntPtr monitor);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetMonitorPhysicalSize(IntPtr monitor, out int w, out int h);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetMonitorContentScale(IntPtr monitor, out float x, out float y);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetMonitorWorkarea(IntPtr monitor, out int x, out int y, out int w, out int h);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetMonitorName(IntPtr monitor);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWmonitorfun glfwSetMonitorCallback(GLFWmonitorfun callback);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowTitle(IntPtr window, IntPtr title);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWmousebuttonfun glfwSetMouseButtonCallback(IntPtr window, GLFWmousebuttonfun mouse_button_callback);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWscrollfun glfwSetScrollCallback(IntPtr window, GLFWscrollfun scroll_callback);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWkeyfun glfwSetKeyCallback(IntPtr window, GLFWkeyfun key_callback);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetCursorPos(IntPtr window, out double xpos, out double ypos);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetCursorEnterCallback(IntPtr window, Glfwcursorenterfun cursor_enter_callback);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowPosCallback(IntPtr window, GLFWwindowposfun window_pos_callback);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowSizeCallback(IntPtr window, GLFWwindowsizefun window_size_callback);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowFocusCallback(IntPtr window, GLFWwindowfocusfun focus_callback);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowIconifyCallback(IntPtr window, GLFWwindowiconifyfun iconify_callback);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwGetPhysicalDevicePresentationSupport(IntPtr instance, IntPtr device, uint family);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwCreateWindowSurface(IntPtr instance, IntPtr window, IntPtr alloc, out IntPtr surface);
		}
		#endregion // API Delegate Types
	}
}
