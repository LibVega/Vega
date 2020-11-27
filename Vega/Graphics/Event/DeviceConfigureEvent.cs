/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// Event that is raised one time after the graphics device has been selected, in order to configure the device
	/// for runtime use.
	/// </summary>
	public sealed class DeviceConfigureEvent
	{
		#region Fields
		/// <summary>
		/// The graphics features that are available on the device.
		/// </summary>
		public readonly GraphicsFeatures AvailableFeatures;
		/// <summary>
		/// The mutable set of features to request from the device.
		/// </summary>
		public GraphicsFeatures EnabledFeatures;
		#endregion // Fields

		internal DeviceConfigureEvent(in GraphicsFeatures afeats)
		{
			AvailableFeatures = afeats;
		}
	}
}
