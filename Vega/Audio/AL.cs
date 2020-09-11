/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vega.Util;

namespace Vega.Audio
{
	internal static partial class AL
	{
		#region Fields
		// OpenAL library handle
		public static readonly EmbeddedLibrary Lib;

		// Last library error
		public static int LastError { get; private set; } = AL.NO_ERROR;
		#endregion // Fields

		#region API
		public unsafe static uint[] GenSources(int count)
		{
			var arr = new uint[count];
			fixed (uint* aptr = arr) {
				_GenSources(count, new IntPtr(aptr));
			}
			return arr;
		}

		public unsafe static void DeleteSources(uint[] srcs)
		{
			fixed (uint* sptr = srcs) {
				_DeleteSources(srcs.Length, new IntPtr(sptr));
			}
		}

		public unsafe static uint[] GenBuffers(int count)
		{
			var arr = new uint[count];
			fixed (uint* aptr = arr) {
				_GenBuffers(count, new IntPtr(aptr));
			}
			return arr;
		}

		public unsafe static void DeleteBuffers(uint[] bufs)
		{
			fixed (uint* bptr = bufs) {
				_DeleteBuffers(bufs.Length, new IntPtr(bptr));
			}
		}

		public static void BufferData(uint buffer, int format, IntPtr data, uint size, uint freq) =>
			_BufferData(buffer, format, data, size, freq);

		public static void Sourcei(uint src, int param, int value) => _Sourcei(src, param, value);

		public static void Sourcef(uint src, int param, float value) => _Sourcef(src, param, value);

		public static void GetSourcei(uint buffer, int param, out int value) => _GetSourcei(buffer, param, out value);

		public static void GetBufferi(uint buffer, int param, out int value) => _GetBufferi(buffer, param, out value);

		public static void SourcePlay(uint source) => _SourcePlay(source);

		public static void SourcePause(uint source) => _SourcePause(source);

		public static void SourceStop(uint source) => _SourceStop(source);
		#endregion // API

		#region Errors
		[Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void CheckError(
			string? message = null,
			[CallerMemberName] string name = "",
			[CallerLineNumber] int line = 0
		)
		{
			LastError = _GetError();
			if (LastError != AL.NO_ERROR) {
				var estr = GetErrorString(LastError);
				throw new Exception($"AL ERROR ({LastError} | {estr}) at [{name}:{line}] - {message ?? "\"\""}");
			}
		}

		public static void ClearError() => _GetError();

		public static string GetErrorString(int code) => code switch {
			AL.NO_ERROR => "No error",
			AL.INVALID_NAME => "Invalid name",
			AL.INVALID_ENUM => "Invalid enum",
			AL.INVALID_VALUE => "Invalid value",
			AL.INVALID_OPERATION => "Invalid operation",
			AL.OUT_OF_MEMORY => "Out of memory",
			_ => $"Invalid error code ({code})"
		};
		#endregion // Errors

		public static string GetString(int param)
		{
			var str = _GetString(param);
			CheckError($"Could not get AL string {param}");
			return Marshal.PtrToStringAnsi(str) ?? throw new Exception("Bad ANSI string in AL.GetString()");
		}

		static AL()
		{
			Lib = new EmbeddedLibrary(typeof(AL).Assembly, "Vega.Lib.openal", "openal");
			var _ = Lib.Handle; // Load

			_GetError = LoadFunc<Delegates.GetError>();
			_GetString = LoadFunc<Delegates.GetString>();

			_GenSources = LoadFunc<Delegates.GenSources>();
			_DeleteSources = LoadFunc<Delegates.DeleteSources>();
			_GenBuffers = LoadFunc<Delegates.GenBuffers>();
			_DeleteBuffers = LoadFunc<Delegates.DeleteBuffers>();
			_BufferData = LoadFunc<Delegates.BufferData>();

			_Sourcei = LoadFunc<Delegates.Sourcei>();
			_Sourcef = LoadFunc<Delegates.Sourcef>();
			_GetSourcei = LoadFunc<Delegates.GetSourcei>();
			_GetBufferi = LoadFunc<Delegates.GetBufferi>();

			_SourcePlay = LoadFunc<Delegates.SourcePlay>();
			_SourcePause = LoadFunc<Delegates.SourcePause>();
			_SourceStop = LoadFunc<Delegates.SourceStop>();
		}
	}


	internal static partial class ALC
	{
		#region Fields
		// OpenAL library handle
		public static EmbeddedLibrary Lib => AL.Lib;

		// Last library error
		public static int LastError { get; private set; } = ALC.NO_ERROR;
		#endregion // Fields

		#region API
		public static IntPtr OpenDevice(string devicename)
		{
			var sptr = Marshal.StringToHGlobalAnsi(devicename);
			try {
				return _OpenDevice(sptr);
			}
			finally {
				Marshal.FreeHGlobal(sptr);
			}
		}

		public static bool CloseDevice(IntPtr device) => _CloseDevice(device) == ALC.TRUE;

		public unsafe static IntPtr CreateContext(IntPtr device, int[] attrlist)
		{
			fixed (int* aptr = attrlist) {
				return _CreateContext(device, new IntPtr(aptr));
			}
		}

		public static bool MakeContextCurrent(IntPtr context) => _MakeContextCurrent(context) == ALC.TRUE;

		public static void DestroyContext(IntPtr context) => _DestroyContext(context);
		#endregion // API

		#region Errors
		[Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void CheckError(
			IntPtr device,
			string? message = null,
			[CallerMemberName] string name = "",
			[CallerLineNumber] int line = 0
		)
		{
			LastError = _GetError(device);
			if (LastError != ALC.NO_ERROR) {
				var estr = GetErrorString(LastError);
				throw new Exception($"ALC ERROR ({LastError} | {estr}) at [{name}:{line}] - {message ?? "\"\""}");
			}
		}

		public static void ClearError(IntPtr device) => _GetError(device);

		public static string GetErrorString(int code) => code switch {
			ALC.NO_ERROR => "No error",
			ALC.INVALID_DEVICE => "Invalid device",
			ALC.INVALID_CONTEXT => "Invalid context",
			ALC.INVALID_ENUM => "Invalid enum",
			ALC.INVALID_VALUE => "Invalid value",
			ALC.OUT_OF_MEMORY => "Out of memory",
			_ => $"Invalid error code ({code})"
		};
		#endregion // Errors

		public static string GetString(IntPtr device, int param)
		{
			var sptr = _GetString(device, param);
			CheckError(device, $"Could not get ALC string {param}");
			if (sptr == IntPtr.Zero) throw new Exception("Null string pointer in ALC.GetString");

			// ALC can return string lists, which cant be used by PtrToStringAnsi
			// Individual strings are null-terminated, and the full list has two nulls at the end
			if ((device == IntPtr.Zero) &&
				(param == ALC.DEVICE_SPECIFIER || param == ALC.CAPTURE_DEVICE_SPECIFIER || param == ALC.ALL_DEVICES_SPECIFIER)) {
				byte[] chars = new byte[GetStringListPtrLength(sptr)];
				Marshal.Copy(sptr, chars, 0, chars.Length);

				var splits = chars.Select((b, i) => b == 0 ? i : -1).Where(i => i != -1).ToList();
				splits.Insert(0, -1); // Add start of first string
				splits.RemoveAt(splits.Count - 1); // Remove second null terminator

				var slist = new List<string>();
				for (int i = 0; i < (splits.Count - 1); ++i) {
					slist.Add(Marshal.PtrToStringAnsi(sptr + (splits[i] + 1), splits[i + 1] - splits[i] - 1));
				}
				return String.Join("\n", slist);
			}
			else {
				return Marshal.PtrToStringAnsi(sptr) ?? throw new Exception("Bad string pointer in ALC.GetString");
			}
		}

		private unsafe static int GetStringListPtrLength(IntPtr sptr)
		{
			var ptr = (byte*)sptr.ToPointer();
			int length = 0;
			while ((*(ptr++) != 0) || (*ptr != 0)) ++length;
			return length + 2;
		}

		static ALC()
		{
			_GetError = LoadFunc<Delegates.GetError>();
			_GetString = LoadFunc<Delegates.GetString>();

			_OpenDevice = LoadFunc<Delegates.OpenDevice>();
			_CloseDevice = LoadFunc<Delegates.CloseDevice>();
			_CreateContext = LoadFunc<Delegates.CreateContext>();
			_MakeContextCurrent = LoadFunc<Delegates.MakeContextCurrent>();
			_DestroyContext = LoadFunc<Delegates.DestroyContext>();
		}
	}
}
