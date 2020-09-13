/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vega.Util;

namespace Vega.Content
{
	// API mapping to the embedded native ContentLoader library
	internal static partial class NativeLoader
	{
		// Library handle
		public static readonly EmbeddedLibrary Lib;

		#region Audio API
		public unsafe static IntPtr AudioOpenFile(string path, out AudioError error)
		{
			fixed (char* sptr = path) {
				return _VegaAudioOpenFile(new IntPtr(sptr), out error);
			}
		}

		public static void AudioCloseFile(IntPtr handle) => _VegaAudioCloseFile(handle);

		public static AudioError AudioGetError(IntPtr handle) => _VegaAudioGetError(handle);

		public static ulong AudioGetFrameCount(IntPtr handle) => _VegaAudioGetFrameCount(handle);

		public static uint AudioGetSampleRate(IntPtr handle) => _VegaAudioGetSampleRate(handle);

		public static uint AudioGetChannelCount(IntPtr handle) => _VegaAudioGetChannelCount(handle);

		public static void AudioGetInfo(IntPtr handle, out ulong frames, out uint rate, out uint channels) =>
			_VegaAudioGetInfo(handle, out frames, out rate, out channels);

		public static ulong AudioGetRemainingFrames(IntPtr handle) => _VegaAudioGetRemainingFrames(handle);

		public unsafe static ulong AudioReadFrames(IntPtr handle, ulong frameCount, ReadOnlySpan<short> buffer)
		{
			fixed (short* bptr = buffer) {
				return _VegaAudioReadFrames(handle, frameCount, new IntPtr(bptr));
			}
		}
		#endregion // Audio API

		static NativeLoader()
		{
			// Load library
			Lib = new EmbeddedLibrary(typeof(NativeLoader).Assembly, "Vega.Lib.content", "content");
			var _ = Lib.Handle; // Force load handle

			// Load audio
			_VegaAudioOpenFile = LoadFunc<Delegates.vegaAudioOpenFile>();
			_VegaAudioCloseFile = LoadFunc<Delegates.vegaAudioCloseFile>();
			_VegaAudioGetError = LoadFunc<Delegates.vegaAudioGetError>();
			_VegaAudioGetFrameCount = LoadFunc<Delegates.vegaAudioGetFrameCount>();
			_VegaAudioGetSampleRate = LoadFunc<Delegates.vegaAudioGetSampleRate>();
			_VegaAudioGetChannelCount = LoadFunc<Delegates.vegaAudioGetChannelCount>();
			_VegaAudioGetInfo = LoadFunc<Delegates.vegaAudioGetInfo>();
			_VegaAudioGetRemainingFrames = LoadFunc<Delegates.vegaAudioGetRemainingFrames>();
			_VegaAudioReadFrames = LoadFunc<Delegates.vegaAudioReadFrames>();
		}
	}
}
