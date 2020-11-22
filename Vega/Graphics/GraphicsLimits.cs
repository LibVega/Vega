/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Provides the set of queryable limits for the current graphics device and driver.
	/// </summary>
	public struct GraphicsLimits
	{
		#region Fields
		// The actual Vulkan limits
		internal readonly Vulkan.VVK.DeviceInfo Info;

		/// <summary>
		/// Gets the maximum MSAA level supported by the current platform.
		/// </summary>
		public MSAA MaxMSAA
		{
			get {
				var sup = Info.Properties.Limits.FramebufferColorSampleCounts &
					Info.Properties.Limits.FramebufferDepthSampleCounts;
				if ((sup & VkSampleCountFlags.E16) > 0) return MSAA.X16;
				if ((sup & VkSampleCountFlags.E8) > 0) return MSAA.X8;
				if ((sup & VkSampleCountFlags.E4) > 0) return MSAA.X4;
				if ((sup & VkSampleCountFlags.E2) > 0) return MSAA.X2;
				return MSAA.X1;
			}
		}

		/// <summary>
		/// The maximum supported size for a Renderer or Window.
		/// </summary>
		public Extent2D MaxFramebufferSize =>
			new(Info.Properties.Limits.MaxFramebufferWidth, Info.Properties.Limits.MaxFramebufferHeight);
		#endregion // Fields

		internal GraphicsLimits(Vulkan.VVK.DeviceInfo info) => Info = info;

		/// <summary>
		/// Checks if the given MSAA level is supported by the current platform.
		/// </summary>
		/// <param name="msaa">The MSAA level to check.</param>
		public bool IsMSAASupported(MSAA msaa) =>
			(Info.Properties.Limits.FramebufferColorSampleCounts &
			 Info.Properties.Limits.FramebufferDepthSampleCounts &
			 (VkSampleCountFlags)msaa) > 0;
	}
}
