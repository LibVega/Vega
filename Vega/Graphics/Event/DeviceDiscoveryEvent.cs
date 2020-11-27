/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan.VVK;

namespace Vega.Graphics
{
	/// <summary>
	/// Event that is raised on <see cref="Core.Events"/> during GPU selection containing the information for one of
	/// the phyiscal graphics devices on the system. These events are used to select which graphics device to use for
	/// graphics operations.
	/// <para>
	/// <em>Note: this interface is likely to be replaced in the future with somthing that allows much more robust
	/// introspection of the devices for better selection options.</em>
	/// </para>
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
		/// The status of the optional features on this device.
		/// </summary>
		public DeviceFeatures Features { get; internal set; }

		// Flag for using this device
		internal bool Use = false;
		// The device features to enable, if used
		internal GraphicsFeatures? RequestedFeatures = null;
		#endregion // Fields

		/// <summary>
		/// Marks the device described by this event as the device to use for graphics operations. Note that calling
		/// this function does not stop device discovery, it only marks this device as the one to use if future devices
		/// are not selected instead.
		/// </summary>
		/// <param name="features">The features to enable on the device.</param>
		public void UseDevice(GraphicsFeatures? features = null)
		{
			Use = true;
			RequestedFeatures = features;
		}

		/// <summary>
		/// The set of graphics features that might be available on the device.
		/// </summary>
		public struct DeviceFeatures
		{
			#region Fields
			#endregion // Fields

			// Populates the fields from a device info
			internal void Populate(DeviceInfo info)
			{

			}
		}
	}
}
