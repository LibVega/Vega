/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// Provides the set of queryable limits for the current graphics device and driver.
	/// </summary>
	public struct GraphicsLimits
	{
		#region Fields
		// The actual Vulkan limits
		internal readonly Vk.PhysicalDeviceData Data;

		/// <summary>
		/// Gets the maximum MSAA level supported by the current platform.
		/// </summary>
		public MSAA MaxMSAA
		{
			get {
				var sup = Data.Properties.Limits.FramebufferColorSampleCounts &
					Data.Properties.Limits.FramebufferDepthSampleCounts;
				if ((sup & Vk.SampleCountFlags.E16) > 0) return MSAA.X16;
				if ((sup & Vk.SampleCountFlags.E8) > 0) return MSAA.X8;
				if ((sup & Vk.SampleCountFlags.E4) > 0) return MSAA.X4;
				if ((sup & Vk.SampleCountFlags.E2) > 0) return MSAA.X2;
				return MSAA.X1;
			}
		}

		/// <summary>
		/// The maximum supported size for a Renderer or Window.
		/// </summary>
		public Extent2D MaxFramebufferSize =>
			new(Data.Properties.Limits.MaxFramebufferWidth, Data.Properties.Limits.MaxFramebufferHeight);
		#endregion // Fields

		internal GraphicsLimits(Vk.PhysicalDeviceData data) => Data = data;

		/// <summary>
		/// Checks if the given MSAA level is supported by the current platform.
		/// </summary>
		/// <param name="msaa">The MSAA level to check.</param>
		public bool IsMSAASupported(MSAA msaa) =>
			(Data.Properties.Limits.FramebufferColorSampleCounts &
			 Data.Properties.Limits.FramebufferDepthSampleCounts &
			 (Vk.SampleCountFlags)msaa) > 0;
	}
}
