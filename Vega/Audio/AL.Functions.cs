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

		private readonly static Delegates.GenSources _GenSources;
		private readonly static Delegates.DeleteSources _DeleteSources;
		private readonly static Delegates.GenBuffers _GenBuffers;
		private readonly static Delegates.DeleteBuffers _DeleteBuffers;
		private readonly static Delegates.BufferData _BufferData;

		private readonly static Delegates.Sourcei _Sourcei;
		private readonly static Delegates.Sourcef _Sourcef;
		private readonly static Delegates.GetSourcei _GetSourcei;
		private readonly static Delegates.GetBufferi _GetBufferi;

		private readonly static Delegates.SourcePlay _SourcePlay;
		private readonly static Delegates.SourcePause _SourcePause;
		private readonly static Delegates.SourceStop _SourceStop;
		#endregion // Handles

		public static class Delegates
		{
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int GetError();
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr GetString(int param);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void GenSources(int n, IntPtr sources);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void DeleteSources(int n, IntPtr sources);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void GenBuffers(int n, IntPtr buffers);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void DeleteBuffers(int n, IntPtr buffers);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void BufferData(uint buffer, int format, IntPtr data, uint size, uint freq);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void Sourcei(uint source, int param, int value);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void Sourcef(uint source, int param, float value);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void GetSourcei(uint source, int param, out int value);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void GetBufferi(uint buffer, int param, out int value);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void SourcePlay(uint source);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void SourcePause(uint source);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void SourceStop(uint source);
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
