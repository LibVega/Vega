/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega
{
	/// <summary>
	/// Represents a video mode supported by a monitor in hardware, given as a resolution, refresh rate, and color 
	/// depth. The "screen coordinate" unit will be 1:1 with pixels on most screens, but HDPI screens (such as Mac
	/// Retina) will likely have a different ratio to pixels.
	/// </summary>
	public struct VideoMode : IEquatable<VideoMode>
	{
		#region Fields
		/// <summary>
		/// The width of the video mode output, in screen coordinates.
		/// </summary>
		public uint Width;
		/// <summary>
		/// The height of the video mode output, in screen coordinates.
		/// </summary>
		public uint Height;
		/// <summary>
		/// The refresh rate of the video mode.
		/// </summary>
		public uint RefreshRate;
		/// <summary>
		/// The number of color bits per pixel. Full color will be 24 BPP.
		/// </summary>
		public uint BPP;

		/// <summary>
		/// The total display area of the video mode, in pixels.
		/// </summary>
		public readonly uint Area => Width * Height;
		/// <summary>
		/// Gets the video mode size as an extent object.
		/// </summary>
		public readonly Extent2D Size => new Extent2D(Width, Height);
		#endregion // Fields

		internal VideoMode(in Glfw.VidMode mode)
		{
			Width = (uint)mode.Width;
			Height = (uint)mode.Height;
			RefreshRate = (uint)mode.RefreshRate;
			BPP = (uint)(mode.RedBits + mode.GreenBits + mode.BlueBits);
		}

		#region Overrides
		readonly bool IEquatable<VideoMode>.Equals(VideoMode other) => (other == this);

		public readonly override bool Equals(object? obj) => (obj is VideoMode vm) && (vm == this);

		public readonly override int GetHashCode() => HashCode.Combine(Width, Height, RefreshRate, BPP);

		public readonly override string ToString() => $"{{{Width}x{Height}@{RefreshRate}}}";
		#endregion // Overrides

		#region Operators
		public static bool operator == (in VideoMode l, in VideoMode r) =>
			(l.Width == r.Width) && (l.Height == r.Height) && (l.RefreshRate == r.RefreshRate) && (l.BPP == r.BPP);
		public static bool operator != (in VideoMode l, in VideoMode r) =>
			(l.Width != r.Width) || (l.Height != r.Height) || (l.RefreshRate != r.RefreshRate) || (l.BPP != r.BPP);
		#endregion // Operators
	}
}
