/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
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
			if (Lib.TryLoadFunction<T>(fn, out var func)) {
				return func!;
			}
			throw new InvalidOperationException($"Function '{fn}' not found in OpenAL library");
		}

		#region Handles
		private readonly static Delegates.alGetError _GetError;
		private readonly static Delegates.alGetString _GetString;

		private readonly static Delegates.alGenSources _GenSources;
		private readonly static Delegates.alDeleteSources _DeleteSources;
		private readonly static Delegates.alGenBuffers _GenBuffers;
		private readonly static Delegates.alDeleteBuffers _DeleteBuffers;
		private readonly static Delegates.alBufferData _BufferData;

		private readonly static Delegates.alSourcei _Sourcei;
		private readonly static Delegates.alSourcef _Sourcef;
		private readonly static Delegates.alGetSourcei _GetSourcei;
		private readonly static Delegates.alGetBufferi _GetBufferi;

		private readonly static Delegates.alSourcePlay _SourcePlay;
		private readonly static Delegates.alSourcePause _SourcePause;
		private readonly static Delegates.alSourceStop _SourceStop;
		#endregion // Handles

		public static class Delegates
		{
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int alGetError();
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr alGetString(int param);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void alGenSources(int n, IntPtr sources);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void alDeleteSources(int n, IntPtr sources);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void alGenBuffers(int n, IntPtr buffers);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void alDeleteBuffers(int n, IntPtr buffers);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void alBufferData(uint buffer, int format, IntPtr data, uint size, uint freq);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void alSourcei(uint source, int param, int value);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void alSourcef(uint source, int param, float value);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void alGetSourcei(uint source, int param, out int value);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void alGetBufferi(uint buffer, int param, out int value);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void alSourcePlay(uint source);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void alSourcePause(uint source);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void alSourceStop(uint source);
		}
	}


	internal static partial class ALC
	{
		#region Handles
		private readonly static Delegates.alcGetError _GetError;
		private readonly static Delegates.alcGetString _GetString;

		private readonly static Delegates.alcOpenDevice _OpenDevice;
		private readonly static Delegates.alcCloseDevice _CloseDevice;
		private readonly static Delegates.alcCreateContext _CreateContext;
		private readonly static Delegates.alcMakeContextCurrent _MakeContextCurrent;
		private readonly static Delegates.alcDestroyContext _DestroyContext;
		#endregion // Handles

		public static class Delegates
		{
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int alcGetError(IntPtr device);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr alcGetString(IntPtr device, int param);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr alcOpenDevice(IntPtr devicename);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate byte alcCloseDevice(IntPtr device);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr alcCreateContext(IntPtr device, IntPtr attrlist);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate byte alcMakeContextCurrent(IntPtr context);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void alcDestroyContext(IntPtr context);
		}
	}
}
