/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Audio
{
	/// <summary>
	/// Represents an active instance of playable data from a <see cref="Sound"/> object.
	/// </summary>
	public sealed class SoundInstance : IDisposable
	{
		#region Fields
		/// <summary>
		/// The <see cref="Sound"/> object that this instance is playing from.
		/// </summary>
		public readonly Sound Sound;

		// If this instance is transient (fire-and-forget)
		internal readonly bool IsTransient;

		// OpenAL source handle
		internal uint Handle = 0;
		internal bool HasHandle => Handle != 0;

		#region State
		/// <summary>
		/// The current playback state of the source instance.
		/// </summary>
		public PlaybackState State
		{
			get {
				if (IsDisposed) throw new ObjectDisposedException(nameof(SoundInstance));
				if (!HasHandle) {
					return PlaybackState.Stopped;
				}

				AL.GetSourcei(Handle, AL.SOURCE_STATE, out var state);
				AL.CheckError("source state");
				return state switch { 
					AL.INITIAL => PlaybackState.Stopped,
					AL.STOPPED => PlaybackState.Stopped,
					AL.PAUSED => PlaybackState.Paused,
					AL.PLAYING => PlaybackState.Playing,
					_ => throw new Exception("Invalid audio source state")
				};
			}
		}

		/// <summary>
		/// Gets if the sound is currently stopped.
		/// </summary>
		public bool IsStopped => State == PlaybackState.Stopped;
		/// <summary>
		/// Gets if the sound is currently paused.
		/// </summary>
		public bool IsPaused => State == PlaybackState.Paused;
		/// <summary>
		/// Gets if the sound is currently playing. 
		/// </summary>
		public bool IsPlaying => State == PlaybackState.Playing;
		#endregion // State

		/// <summary>
		/// Gets if the instance has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		internal SoundInstance(Sound sound, bool transient)
		{
			Sound = sound;
			IsTransient = transient;
		}
		~SoundInstance()
		{
			dispose(false);
		}

		#region State Control
		/// <summary>
		/// Begins or resumes playback of the sound instance, if not already playing.
		/// </summary>
		public void Play()
		{
			if (IsDisposed) throw new ObjectDisposedException(nameof(SoundInstance));

			// Same-state
			var curr = State;
			if (curr == PlaybackState.Playing) {
				return;
			}

			// Reserve new source if required
			if (!HasHandle) {
				Handle = Core.Instance!.AudioDriver.ReserveSource();
				AL.Sourcei(Handle, AL.BUFFER, (int)Sound.Buffer.Handle);
				AL.CheckError("set source buffer");
			}

			// Play the sound
			AL.SourcePlay(Handle);
			AL.CheckError("source play");

			// Register the instance
			if (curr == PlaybackState.Stopped) {
				Core.Instance!.AudioDriver.TrackInstance(this);
			}
		}

		/// <summary>
		/// Pauses sound playback, maintaining the playback position.
		/// </summary>
		public void Pause()
		{
			if (IsDisposed) throw new ObjectDisposedException(nameof(SoundInstance));

			// Same-state
			if (!IsPlaying) {
				return;
			}

			AL.SourcePause(Handle);
			AL.CheckError("source pause");
		}

		/// <summary>
		/// Stops the playback, releasing the audio source and resetting the playback position to be beginning.
		/// </summary>
		public void Stop()
		{
			if (IsDisposed) throw new ObjectDisposedException(nameof(SoundInstance));

			// Stop playback if not stopped
			if (!IsStopped) {
				AL.SourceStop(Handle);
				AL.CheckError("source stop");
			}

			// Release handle if reserved
			if (HasHandle) {
				releaseSource();
				Core.Instance?.AudioDriver.RemoveInstance(this);
			}
		}
		#endregion // State Control

		private void releaseSource()
		{
			if (HasHandle) {
				AL.Sourcei(Handle, AL.BUFFER, 0);
				AL.CheckError("unbind source buffer");
				Core.Instance?.AudioDriver.ReleaseSource(Handle);
				Handle = 0;
			}
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed && HasHandle) {
				Stop();
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
