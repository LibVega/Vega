/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.InteropServices;
using System.Security;
using Vega.Util;

namespace Vega.Content
{
	// Wraps the "content" native dependency
	internal static class NativeContent
	{
		public static readonly EmbeddedLibrary Lib;

		#region Sound API
		public unsafe static IntPtr SoundOpenFile(string fileName, out int error)
		{
			fixed (char* ptr = fileName) {
				return _SoundOpenFile(new IntPtr(ptr), out error);
			}
		}

		public static void SoundCloseFile(IntPtr file) => _SoundCloseFile(file);

		public static SoundFileType SoundGetFileType(IntPtr file) => (SoundFileType)_SoundGetFileType(file);

		public static string SoundGetFileName(IntPtr file)
		{
			var sptr = _SoundGetFileName(file);
			return Marshal.PtrToStringAnsi(sptr)!;
		}

		public static SoundError SoundGetError(IntPtr file) => (SoundError)_SoundGetError(file);

		public static ulong SoundGetFrameCount(IntPtr file) => _SoundGetFrameCount(file);

		public static uint SoundGetSampleRate(IntPtr file) => _SoundGetSampleRate(file);

		public static uint SoundGetChannelCount(IntPtr file) => _SoundGetChannelCount(file);

		public static void SoundGetInfo(IntPtr file, out ulong frames, out uint rate, out uint channels) =>
			_SoundGetInfo(file, out frames, out rate, out channels);
		#endregion // Sound API

		#region Function Pointers
		private static readonly Delegates.soundOpenFile _SoundOpenFile;
		private static readonly Delegates.soundCloseFile _SoundCloseFile;
		private static readonly Delegates.soundGetFileType _SoundGetFileType;
		private static readonly Delegates.soundGetFileName _SoundGetFileName;
		private static readonly Delegates.soundGetError _SoundGetError;
		private static readonly Delegates.soundGetFrameCount _SoundGetFrameCount;
		private static readonly Delegates.soundGetSampleRate _SoundGetSampleRate;
		private static readonly Delegates.soundGetChannelCount _SoundGetChannelCount;
		private static readonly Delegates.soundGetInfo _SoundGetInfo;
		#endregion // Function Pointers

		#region Delegates
		public static class Delegates
		{
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr soundOpenFile(IntPtr fileName, out int error);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void soundCloseFile(IntPtr file);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int soundGetFileType(IntPtr file);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr soundGetFileName(IntPtr file);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int soundGetError(IntPtr file);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate ulong soundGetFrameCount(IntPtr file);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate uint soundGetSampleRate(IntPtr file);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate uint soundGetChannelCount(IntPtr file);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void soundGetInfo(IntPtr file, out ulong frames, out uint rate, out uint channels);
		}
		#endregion // Delegates

		static NativeContent()
		{
			Lib = new EmbeddedLibrary(typeof(NativeContent).Assembly, "Vega.Lib.content", "content");
			var _ = Lib.Handle; // Force load

			_SoundOpenFile = LoadFunc<Delegates.soundOpenFile>();
			_SoundCloseFile = LoadFunc<Delegates.soundCloseFile>();
			_SoundGetFileType = LoadFunc<Delegates.soundGetFileType>();
			_SoundGetFileName = LoadFunc<Delegates.soundGetFileName>();
			_SoundGetError = LoadFunc<Delegates.soundGetError>();
			_SoundGetFrameCount = LoadFunc<Delegates.soundGetFrameCount>();
			_SoundGetSampleRate = LoadFunc<Delegates.soundGetSampleRate>();
			_SoundGetChannelCount = LoadFunc<Delegates.soundGetChannelCount>();
			_SoundGetInfo = LoadFunc<Delegates.soundGetInfo>();
		}

		private static T LoadFunc<T>() where T : Delegate
		{
			if (Lib.TryGetFunction<T>(typeof(T).Name, out var func)) {
				return func!;
			}
			throw new InvalidOperationException($"Function '{typeof(T).Name}' not found in OpenAL library");
		}
	}
}
