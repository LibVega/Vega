/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Audio
{
	/// <summary>
	/// Represents a specific set of audio data that is fully loaded into memory. This acts as a shared source for one
	/// or more <see cref="SoundInstance"/> instances.
	/// </summary>
	public sealed class Sound : IDisposable
	{
		#region Fields
		// The underlying buffer providing audio data for this sound.
		internal readonly AudioBuffer Buffer;

		/// <summary>
		/// If the buffer data is stereo.
		/// </summary>
		public bool Stereo => Buffer.Stereo;
		/// <summary>
		/// The number of samples in the buffer data.
		/// </summary>
		public uint SampleCount => Buffer.SampleCount;
		/// <summary>
		/// The natural playback frequency for the data.
		/// </summary>
		public uint Frequency => Buffer.Frequency;
		/// <summary>
		/// The playback duration of the data at the natural playback rate.
		/// </summary>
		public TimeSpan Duration => Buffer.Duration;

		/// <summary>
		/// Gets if the sound instance has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		/// <summary>
		/// Constructs a new sound with the given audio sample data.
		/// </summary>
		/// <param name="data">The audio samples to use as the sound data.</param>
		/// <param name="stereo">If the data is stereo interleaved samples.</param>
		/// <param name="hz">The natural playback rate for the data, between 8,000 and 48,000.</param>
		public Sound(ReadOnlySpan<short> data, bool stereo, uint hz)
		{
			Buffer = new AudioBuffer();
			Buffer.SetData(data, stereo, hz);
		}
		internal Sound(AudioBuffer buffer)
		{
			Buffer = buffer;
		}
		~Sound()
		{
			dispose(false);
		}

		#region Instance
		/// <summary>
		/// Creates a new controllable instance of this sound. The calling code must manage the instance lifetime.
		/// </summary>
		public SoundInstance CreateInstance() => new SoundInstance(this, false);

		/// <summary>
		/// Plays the sound in a "fire-and-forget" fashion. The created instance is internally managed and released.
		/// </summary>
		/// <returns>If the sound could be played.</returns>
		public bool Play()
		{
			try {
				var si = new SoundInstance(this, true);
				si.Play();
				return true;
			}
			catch {
				return false;
			}
		}
		#endregion // Instance

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed) {
				Core.Instance?.AudioDriver.StopInstances(this);
				if (disposing) {
					Buffer.Dispose();
				}
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
