/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Vega.Audio
{
	internal static partial class AL
	{
		private static T LoadFunc<T>() where T : Delegate
		{
			var fn = $"al{typeof(T).Name}";
			if (Lib.TryGetFunction<T>(fn, out var func)) {
				return func!;
			}
			throw new InvalidOperationException($"Function '{fn}' not found in OpenAL library");
		}

		#region Handles
		private readonly static Delegates.GetError _GetError;
		private readonly static Delegates.GetString _GetString;
		#endregion // Handles

		public static class Delegates
		{
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int GetError();
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr GetString(int param);
		}
	}


	internal static partial class ALC
	{
		private static T LoadFunc<T>() where T : Delegate
		{
			var fn = $"alc{typeof(T).Name}";
			if (AL.Lib.TryGetFunction<T>(fn, out var func)) {
				return func!;
			}
			throw new InvalidOperationException($"Function '{fn}' not found in OpenAL library");
		}

		#region Handles
		private readonly static Delegates.GetError _GetError;
		private readonly static Delegates.GetString _GetString;

		private readonly static Delegates.OpenDevice _OpenDevice;
		private readonly static Delegates.CloseDevice _CloseDevice;
		private readonly static Delegates.CreateContext _CreateContext;
		private readonly static Delegates.MakeContextCurrent _MakeContextCurrent;
		private readonly static Delegates.DestroyContext _DestroyContext;
		#endregion // Handles

		public static class Delegates
		{
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int GetError(IntPtr device);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr GetString(IntPtr device, int param);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr OpenDevice(IntPtr devicename);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate byte CloseDevice(IntPtr device);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr CreateContext(IntPtr device, IntPtr attrlist);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate byte MakeContextCurrent(IntPtr context);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void DestroyContext(IntPtr context);
		}
	}
}
