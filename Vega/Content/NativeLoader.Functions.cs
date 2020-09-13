/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Vega.Content
{
	// Functions for the ContentLoader library
	internal static partial class NativeLoader
	{
		private static T LoadFunc<T>() where T : Delegate
		{
			var fn = typeof(T).Name;
			if (Lib.TryGetFunction<T>(fn, out var func)) {
				return func!;
			}
			throw new InvalidOperationException($"Function '{fn}' not found in OpenAL library");
		}

		#region Functions
		// AUDIO
		private static readonly Delegates.vegaAudioOpenFile _VegaAudioOpenFile;
		private static readonly Delegates.vegaAudioCloseFile _VegaAudioCloseFile;
		private static readonly Delegates.vegaAudioGetError _VegaAudioGetError;
		private static readonly Delegates.vegaAudioGetFrameCount _VegaAudioGetFrameCount;
		private static readonly Delegates.vegaAudioGetSampleRate _VegaAudioGetSampleRate;
		private static readonly Delegates.vegaAudioGetChannelCount _VegaAudioGetChannelCount;
		private static readonly Delegates.vegaAudioGetInfo _VegaAudioGetInfo;
		private static readonly Delegates.vegaAudioGetRemainingFrames _VegaAudioGetRemainingFrames;
		private static readonly Delegates.vegaAudioReadFrames _VegaAudioReadFrames;
		#endregion // Functions

		public static class Delegates
		{
			// AUDIO
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr vegaAudioOpenFile(IntPtr path, out AudioError error);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void vegaAudioCloseFile(IntPtr handle);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate AudioError vegaAudioGetError(IntPtr handle);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate ulong vegaAudioGetFrameCount(IntPtr handle);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate uint vegaAudioGetSampleRate(IntPtr handle);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate uint vegaAudioGetChannelCount(IntPtr handle);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void vegaAudioGetInfo(IntPtr handle, out ulong frames, out uint rate, out uint channels);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate ulong vegaAudioGetRemainingFrames(IntPtr handle);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate ulong vegaAudioReadFrames(IntPtr handle, ulong frameCount, IntPtr buffer);
		}
	}
}
