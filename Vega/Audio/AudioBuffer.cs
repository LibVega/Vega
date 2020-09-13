/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Audio
{
	// Represents an in-memory buffer of audio data.
	internal sealed class AudioBuffer : IDisposable
	{
		#region Fields
		// OpenAL buffer handle
		internal readonly uint Handle;

		// If the data is stereo
		public bool Stereo { get; private set; }
		// Data sample count (pairs of samples for stereo)
		public uint FrameCount { get; private set; }
		// Natural playback frequency
		public uint Frequency { get; private set; }
		// Duration of the audio data
		public TimeSpan Duration { get; private set; }
		#endregion // Fields

		public AudioBuffer()
		{
			// Generate the buffer handle
			Handle = AL.GenBuffers(1)[0];
			AL.CheckError("generate buffer");
			if (Handle == 0) {
				throw new Exception("Failed to generate audio buffer");
			}
		}
		~AudioBuffer()
		{
			dispose(false);
		}

		#region Data
		// Sets the data in the buffer, and updates the buffer info fields.
		public unsafe void SetData(ReadOnlySpan<short> data, bool stereo, uint hz)
		{
			// Check data
			if (stereo && (data.Length & 1) > 0) {
				throw new ArgumentException("Stereo data must have an even number of samples", nameof(data));
			}

			// Set the data
			var fmt = stereo ? AL.FORMAT_STEREO16 : AL.FORMAT_MONO16;
			fixed (short* dptr = data) {
				AL.BufferData(Handle, fmt, new IntPtr(dptr), (uint)data.Length * sizeof(short), hz);
				AL.CheckError("buffer data set");
			}

			// Update buffer info
			AL.GetBufferi(Handle, AL.BITS, out var bits);
			AL.GetBufferi(Handle, AL.CHANNELS, out var channels);
			AL.GetBufferi(Handle, AL.SIZE, out var size);
			AL.GetBufferi(Handle, AL.FREQUENCY, out var freq);
			Stereo = channels > 1;
			FrameCount = (uint)(size / ((bits / 8) * channels));
			Frequency = (uint)freq;
			Duration = TimeSpan.FromSeconds((double)FrameCount / Frequency);
		}
		#endregion // Data

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			AL.DeleteBuffers(new[] { Handle });
			AL.CheckError("delete buffer");
		}
		#endregion // IDisposable
	}
}
