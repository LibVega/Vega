/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// Event that is raised on <see cref="Core.Events"/> during GPU selection containing the information for one of
	/// the phyiscal graphics devices on the system. These events are used to select which graphics device to use for
	/// graphics operations.
	/// </summary>
	public sealed class DeviceDiscoveryEvent
	{
		#region Fields
		/// <summary>
		/// The name of the physical device.
		/// </summary>
		public string DeviceName { get; internal set; } = String.Empty;
		/// <summary>
		/// Gets if the device represents a discrete graphics processor, <c>false</c> implies integrated or virtual.
		/// </summary>
		public bool IsDiscrete { get; internal set; } = false;
		/// <summary>
		/// The total amount of physical VRAM available on the device. Note that this might include host RAM for
		/// integrated processors.
		/// </summary>
		public DataSize MemorySize { get; internal set; } = DataSize.Zero;
		/// <summary>
		/// The graphics features that are supported on the device.
		/// </summary>
		public readonly GraphicsFeatures Features;
		/// <summary>
		/// The graphics limits for the device.
		/// </summary>
		public readonly GraphicsLimits Limits;

		// Flag for using this device
		internal bool Use = false;
		#endregion // Fields

		internal DeviceDiscoveryEvent(in GraphicsFeatures feats, in GraphicsLimits lims)
		{
			Features = feats;
			Limits = lims;
		}

		/// <summary>
		/// Marks the device described by this event as the device to use for graphics operations. Note that calling
		/// this function does not stop device discovery, it only marks this device as the one to use if future devices
		/// are not selected instead.
		/// </summary>
		public void UseDevice() => Use = true;
	}
}
