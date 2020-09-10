/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Audio
{
	// Manages a specific audio context
	internal sealed class AudioDriver : IDisposable
	{
		#region Fields
		// Core using this driver
		public readonly Core Core;

		// Device/Context handles
		public readonly IntPtr Device;
		public readonly IntPtr Context;
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
		}
		~AudioDriver()
		{
			dispose(false);
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			// Destroy the context and device
			ALC.MakeContextCurrent(IntPtr.Zero);
			ALC.CheckError(Device, "deactivate context");
			ALC.DestroyContext(Context);
			ALC.CheckError(Device, "destroy context");
			ALC.CloseDevice(Device);
		}
		#endregion // IDisposable
	}
}
