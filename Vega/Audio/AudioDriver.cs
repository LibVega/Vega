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
		public const int SOURCE_COUNT = 32;

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

		public uint ReserveSource()
		{
			lock (_sourceLock) {
				if (_availableSources.Count == 0) {
					throw new AudioPlayLimitException();
				}

				var src = _availableSources.Pop();
				_usedSources.Add(src);
				ResetSoundEffects(src);
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

		public void ResetSoundEffects(uint src)
		{
			// No looping, default volume and pitch
			AL.Sourcei(src, AL.LOOPING, 0);
			AL.Sourcef(src, AL.GAIN, 1);
			AL.Sourcef(src, AL.PITCH, 1);
		}

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
