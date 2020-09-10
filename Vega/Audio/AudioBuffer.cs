/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Threading;

namespace Vega.Audio
{
	/// <summary>
	/// Represents an in-memory buffer of audio data.
	/// </summary>
	public sealed class AudioBuffer : IDisposable
	{
		#region Fields
		// OpenAL buffer handle
		internal readonly uint Handle;

		#region Info
		/// <summary>
		/// If the contained data is stereo (2-channel interleaved), <c>false</c> implies mono sound.
		/// </summary>
		public bool Stereo { get; private set; } = false;
		/// <summary>
		/// The number of samples in the stored data, stereo data counts pairs of data points as a single sample.
		/// </summary>
		public uint SampleCount { get; private set; } = 0;
		/// <summary>
		/// The natural playback frequency for the stored data.
		/// </summary>
		public uint Frequency { get; private set; } = 0;
		/// <summary>
		/// The playback duration of the stored data at the natural playback rate.
		/// </summary>
		public TimeSpan Duration { get; private set; } = TimeSpan.Zero;
		#endregion // Info

		/// <summary>
		/// The number of audio sources currently using this buffer.
		/// </summary>
		public uint UseCount => _useCount;
		internal uint _useCount;

		/// <summary>
		/// Gets if the buffer has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		/// <summary>
		/// Creates a new un-sized and empty audio data buffer.
		/// </summary>
		public AudioBuffer()
		{
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

		// Increment the use count
		internal void IncUseCount() => Interlocked.Increment(ref _useCount);
		// Decrement the use count
		internal void DecUseCount() => Interlocked.Decrement(ref _useCount);

		#region Data
		/// <summary>
		/// Sets the data in the buffer, and updates the buffer info fields.
		/// </summary>
		/// <param name="data">The audio data, interleaved if stereo.</param>
		/// <param name="stereo">If the data is stereo (2-channel).</param>
		/// <param name="hz">The sample rate for the data.</param>
		/// <exception cref="ObjectDisposedException">The buffer is disposed.</exception>
		/// <exception cref="InvalidOperationException">The buffer is currently in use by audio sources.</exception>
		public unsafe void SetData(ReadOnlySpan<short> data, bool stereo, uint hz)
		{
			if (IsDisposed) {
				throw new ObjectDisposedException(nameof(AudioBuffer));
			}
			if (UseCount != 0) {
				throw new InvalidOperationException("Cannot set AudioBuffer data while the buffer is in use");
			}

			// Set the data
			var fmt = stereo ? AL.FORMAT_STEREO16 : AL.FORMAT_MONO16;
			fixed (short* dptr = data) {
				AL.BufferData(Handle, fmt, new IntPtr(dptr), (uint)data.Length, hz);
				AL.CheckError("buffer data set");
			}

			// Update buffer info
			AL.GetBufferi(Handle, AL.BITS, out var bits);
			AL.GetBufferi(Handle, AL.CHANNELS, out var channels);
			AL.GetBufferi(Handle, AL.SIZE, out var size);
			AL.GetBufferi(Handle, AL.FREQUENCY, out var freq);
			Stereo = channels > 1;
			SampleCount = (uint)(size / ((bits / 8) * channels));
			Frequency = (uint)freq;
			Duration = TimeSpan.FromSeconds((double)SampleCount / Frequency);
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
			if (disposing) {
				if (UseCount != 0) {
					throw new InvalidOperationException("Cannot dispose AudioBuffer currently in use");
				}
			}
			if (!IsDisposed) {
				AL.DeleteBuffers(new[] { Handle });
				AL.CheckError("delete buffer");
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
