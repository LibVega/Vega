/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Vega
{
	/// <summary>
	/// Represents one of the monitors available for video output.
	/// </summary>
	public sealed class Monitor
	{
		#region Fields
		/// <summary>
		/// The reported name of the monitor.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The physical size of the monitor, in millimeters.
		/// </summary>
		public readonly Extent2D PhysicalSize;

		/// <summary>
		/// Gets if this monitor instance is considered the primary monitor by the system.
		/// </summary>
		public bool IsPrimary => Handle == Glfw.GetPrimaryMonitor();
		/// <summary>
		/// Gets the position of the monitor (upper-left corner) within the global monitor space.
		/// </summary>
		public Point2 Position
		{
			get {
				Glfw.GetMonitorPos(Handle, out var x, out var y);
				return new Point2(x, y);
			}
		}
		/// <summary>
		/// Gets the monitor work area - the full monitor display area minus the system toolbar.
		/// </summary>
		public Rect WorkArea
		{
			get {
				Glfw.GetMonitorWorkarea(Handle, out var x, out var y, out var w, out var h);
				return new Rect(x, y, (uint)w, (uint)h);
			}
		}
		/// <summary>
		/// Gets the current size of the monitor.
		/// </summary>
		public Extent2D Size => CurrentMode.Size;
		/// <summary>
		/// Gets the display area of the monitor within global monitor space.
		/// </summary>
		public Rect DisplayArea => new Rect(Position, CurrentMode.Size);
		/// <summary>
		/// Gets the content scale of the monitor - the multiplier between screen coordinates and pixels.
		/// </summary>
		public Vec2 ContentScale
		{
			get {
				Glfw.GetMonitorContentScale(Handle, out var x, out var y);
				return new Vec2(x, y);
			}
		}

		/// <summary>
		/// A list of all video modes supported by this monitor.
		/// </summary>
		public IReadOnlyList<VideoMode> VideoModes => _videoModes;
		private readonly List<VideoMode> _videoModes;
		/// <summary>
		/// The current video mode of the monitor.
		/// </summary>
		public VideoMode CurrentMode => new VideoMode(Glfw.GetVideoMode(Handle));
		/// <summary>
		/// Gets the default mode for the monitor (highest color depth, and highest resolution by area).
		/// </summary>
		public VideoMode DefaultMode => _videoModes[^1];

		// GLFW monitor handle
		internal readonly IntPtr Handle;
		#endregion // Fields

		private Monitor(IntPtr handle)
		{
			Handle = handle;

			// Get static info
			Name = Glfw.GetMonitorName(handle);
			Glfw.GetMonitorPhysicalSize(handle, out var px, out var py);
			PhysicalSize = new Extent2D((uint)px, (uint)py);

			// Get video modes
			_videoModes = Glfw.GetVideoModes(handle).Select(vm => new VideoMode(vm)).ToList();
		}

		#region Overrides
		public override bool Equals(object? obj) => (obj is Monitor m) && (m.Handle == Handle);

		public override int GetHashCode() => Handle.ToInt32();
		#endregion // Overrides

		#region Operators
		public static bool operator == (Monitor? l, Monitor? r) => l?.Handle == r?.Handle;
		public static bool operator != (Monitor? l, Monitor? r) => l?.Handle != r?.Handle;
		#endregion // Operators

		#region Static Values
		/// <summary>
		/// A list of all monitors currently connected to the system.
		/// </summary>
		public static IReadOnlyList<Monitor> Monitors
		{
			get {
				if (Core.Instance == null) 
					throw new InvalidOperationException("Cannot get Monitors without active Core");
				return _Monitors ?? (_Monitors = Glfw.GetMonitors().Select(mptr => new Monitor(mptr)).ToList());
			}
		}
		internal static List<Monitor>? _Monitors = null;
		/// <summary>
		/// Gets the monitor that the system considers as the primary monitor.
		/// </summary>
		public static Monitor Primary
		{
			get {
				var prim = Glfw.GetPrimaryMonitor();
				foreach (var mon in Monitors) {
					if (mon.Handle == prim)
						return mon;
				}
				throw new NotImplementedException("This should not be reached (library bug)");
			}
		}

		// Callback for system monitor updates
		internal static void MonitorUpdate(IntPtr monitor, bool @new)
		{
			if (@new) {
				_Monitors?.Add(new Monitor(monitor));
			}
			else {
				_Monitors?.RemoveAll(mon => mon.Handle == monitor);
			}
		}
		#endregion // Static Values
	}
}
