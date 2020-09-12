/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;

namespace Vega.Audio
{
	// Manages a specific audio context
	internal sealed class AudioDriver : IDisposable
	{
		public const int SOURCE_COUNT = 64;

		#region Fields
		// Core using this driver
		public readonly Core Core;

		// Device/Context handles
		public readonly IntPtr Device;
		public readonly IntPtr Context;

		// Audio sources
		private readonly uint[] _allSources;
		private readonly Stack<uint> _availableSources = new();
		private readonly List<uint> _usedSources = new();
		private readonly object _sourceLock = new();

		// SoundInstance runtime tracking
		private readonly List<SoundInstance> _playingInstances = new();
		private readonly object _instanceLock = new();
		private float _lastCleanTime = 0;
		#endregion // Fields

		public AudioDriver(Core core)
		{
			Core = core;

			// Open the default audio device
			var dname = ALC.GetString(IntPtr.Zero, ALC.DEFAULT_DEVICE_SPECIFIER);
			Device = ALC.OpenDevice(dname);
			ALC.CheckError(IntPtr.Zero, "open device");
			if (Device == IntPtr.Zero) {
				throw new Exception("Failed to open default audio playback device");
			}

			// Create and activate audio context
			Context = ALC.CreateContext(Device, new int[2] { 0, 0 }); // No extra attributes
			ALC.CheckError(Device, "create context");
			if (Context == IntPtr.Zero) {
				throw new Exception("Failed to create audio playback context");
			}
			ALC.MakeContextCurrent(Context);
			ALC.CheckError(Device, "activate context");

			// Generate sources
			_allSources = AL.GenSources(SOURCE_COUNT);
			AL.CheckError("generate sources");
			foreach (var src in _allSources) {
				_availableSources.Push(src);
			}
		}
		~AudioDriver()
		{
			dispose(false);
		}

		// Run at the beginning of the frame to update running audio effects
		// Clean checks happen every 1/5 second
		public void Update()
		{
			var nowTime = (float)AppTime.Elapsed.TotalSeconds;
			if ((nowTime - _lastCleanTime) < 0.2f) {
				return;
			}
			_lastCleanTime = nowTime;

			lock (_instanceLock) {
				for (int i = 0; i < _playingInstances.Count; ) {
					var inst = _playingInstances[i];
					
					if (inst.IsDisposed) {
						_playingInstances.RemoveAt(i);
						continue;
					}
					if (inst.IsStopped) { // Releases the OpenAL source and removes from _playingInstances
						if (inst.IsTransient) {
							inst.Dispose();
						}
						else {
							inst.Stop();
						}
						continue;
					}

					++i;
				}
			}
		}

		#region Sources
		public uint ReserveSource()
		{
			lock (_sourceLock) {
				if (_availableSources.Count == 0) {
					throw new AudioPlayLimitException();
				}

				var src = _availableSources.Pop();
				_usedSources.Add(src);
				ResetSource(src);
				return src;
			}
		}

		public void ReleaseSource(uint src)
		{
			lock (_sourceLock) {
				if (!_usedSources.Remove(src)) {
					throw new InvalidOperationException("Attempt to release audio source that is not in use");
				}
				_availableSources.Push(src);
			}
		}

		public void ResetSource(uint src)
		{
			// No looping, default volume and pitch
			AL.Sourcei(src, AL.LOOPING, 0);
			AL.Sourcef(src, AL.GAIN, 1);
			AL.Sourcef(src, AL.PITCH, 1);
		}
		#endregion // Sources

		#region SoundInstance
		// Adds the sound instance to the runtime tracking system
		public void TrackInstance(SoundInstance inst)
		{
			lock (_instanceLock) {
				_playingInstances.Add(inst);
			}
		}

		// Remove the sound instance from the runtime tracking
		public void RemoveInstance(SoundInstance inst)
		{
			lock (_instanceLock) {
				_playingInstances.Remove(inst);
			}
		}

		// Stop all instances associated with the sound for a good cleanup
		public void StopInstances(Sound sound)
		{
			lock (_instanceLock) {
				for (int i = 0; i < _playingInstances.Count; ) {
					var inst = _playingInstances[i];

					if (inst.IsDisposed) {
						_playingInstances.RemoveAt(i);
						continue;
					}
					if (ReferenceEquals(inst.Sound, sound)) { // Also removes from _playingInstances
						if (inst.IsTransient) {
							inst.Dispose();
						}
						else {
							inst.Stop();
						}
						continue;
					}

					++i;
				}
			}
		}
		#endregion // SoundInstance

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			// Destroy sources
			AL.DeleteSources(_allSources);
			AL.CheckError("delete sources");

			// Destroy the context and device
			ALC.MakeContextCurrent(IntPtr.Zero);
			ALC.CheckError(Device, "deactivate context");
			ALC.DestroyContext(Context);
			ALC.CheckError(Device, "destroy context");
			ALC.CloseDevice(Device);
		}
		#endregion // IDisposable
	}


	/// <summary>
	/// Exception generated when the audio source limit is reached.
	/// </summary>
	public sealed class AudioPlayLimitException : Exception
	{
		internal AudioPlayLimitException() :
			base("Audio source limit reached")
		{ }
	}
}
