/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Text;

namespace Vega.Content
{
	// API mapping to the embedded native ContentLoader library
	internal unsafe static partial class NativeContent
	{
		#region Audio API
		public unsafe static (IntPtr Handle, AudioError Error) AudioOpenFile(string path)
		{
			var sdata = Encoding.ASCII.GetBytes(path);
			fixed (byte* sptr = sdata) {
				AudioError err;
				return (new(_AudioOpenFile(sptr, &err)), err);
			}
		}

		public static void AudioCloseFile(IntPtr handle) => _AudioCloseFile(handle.ToPointer());

		public static AudioType AudioGetType(IntPtr handle) => _AudioGetType(handle.ToPointer());

		public static AudioError AudioGetError(IntPtr handle) => _AudioGetError(handle.ToPointer());

		public static ulong AudioGetFrameCount(IntPtr handle) => _AudioGetFrameCount(handle.ToPointer());

		public static uint AudioGetSampleRate(IntPtr handle) => _AudioGetSampleRate(handle.ToPointer());

		public static uint AudioGetChannelCount(IntPtr handle) => _AudioGetChannelCount(handle.ToPointer());

		public static void AudioGetInfo(IntPtr handle, out ulong frames, out uint rate, out uint channels)
		{
			ulong f;
			uint r, c;
			_AudioGetInfo(handle.ToPointer(), &f, &r, &c);
			frames = f;
			rate = r;
			channels = c;
		}

		public static ulong AudioGetRemainingFrames(IntPtr handle) => _AudioGetRemainingFrames(handle.ToPointer());

		public unsafe static ulong AudioReadFrames(IntPtr handle, ulong frameCount, ReadOnlySpan<short> buffer)
		{
			fixed (short* bptr = buffer) {
				return _AudioReadFrames(handle.ToPointer(), frameCount, bptr);
			}
		}
		#endregion // Audio API
	}
}
