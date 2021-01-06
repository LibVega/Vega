/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Diagnostics;
using System.Linq;

namespace Vega
{
	/// <summary>
	/// Callback for notifying about the value of <see cref="AppTime.Scale"/> changing.
	/// </summary>
	/// <param name="oldScale">The old time scale.</param>
	/// <param name="newScale">The new time scale.</param>
	public delegate void TimeScaleChangedCallback(float oldScale, float newScale);

	/// <summary>
	/// Provides information about time within the application.
	/// </summary>
	public static class AppTime
	{
		#region Fields
		/// <summary>
		/// The time elapsed between this frame and last. Affected by the value of <see cref="Scale"/>.
		/// </summary>
		public static TimeSpan Delta { get; private set; } = TimeSpan.Zero;
		/// <summary>
		/// The unscaled version of <see cref="Delta"/>, that is unaffected by <see cref="Scale"/>.
		/// </summary>
		public static TimeSpan RealDelta { get; private set; } = TimeSpan.Zero;
		/// <summary>
		/// The total amount of elapsed wall time since the application started.
		/// </summary>
		public static TimeSpan Elapsed { get; private set; } = TimeSpan.Zero;
		/// <summary>
		/// The total amount of elapsed wall time in the last frame.
		/// </summary>
		public static TimeSpan LastElapsed { get; private set; } = TimeSpan.Zero;

		/// <summary>
		/// The total number of frames run by the application.
		/// </summary>
		public static ulong FrameCount { get; private set; } = 0;

		private const uint FPS_HISTORY_SIZE = 10;
		private static readonly float[] _FpsHistory = new float[FPS_HISTORY_SIZE];
		private static uint _FpsIndex = 0;
		/// <summary>
		/// The FPS of the application, calculated as a running average of the last 10 frames.
		/// </summary>
		public static float FPS { get; private set; } = 0;
		/// <summary>
		/// The raw FPS of the application in the last frame.
		/// </summary>
		public static float RawFPS { get; private set; } = 0;

		private static float _Scale = 1;
		private static float? _NewScale = null;
		/// <summary>
		/// Gets/sets the scaling factor for the scaled <see cref="Delta"/> measurement. Value will be clamped
		/// above or at zero. Changes to the scale will take effect at the start of the next frame.
		/// </summary>
		public static float Scale
		{
			get => _Scale;
			set => _NewScale = Math.Max(value, 0);
		}
		/// <summary>
		/// Callback for changes to the value of <see cref="Scale"/>, will be called at the beginning of the frame that
		/// the scale change takes effect.
		/// </summary>
		public static event TimeScaleChangedCallback? TimeScaleChanged;

		/// <summary>
		/// The resolution (minimum measureable difference) of the timer, in nanoseconds.
		/// </summary>
		public static readonly uint Resolution;
		/// <summary>
		/// Gets if the timer is considered high resolution. This limit is given as a resolution of 10 microseconds 
		/// (<see cref="Resolution"/> value of &lt;= 10,000).
		/// </summary>
		public static readonly bool IsHighResolution;

		private static Stopwatch _Timer;
		/// <summary>
		/// The most current application runtime with sub-frame accuracy. This value is independent of 
		/// <see cref="Elapsed"/>.
		/// </summary>
		public static TimeSpan Now => _Timer.Elapsed;
		/// <summary>
		///	The timestamp for when the application time began measurement.
		/// </summary>
		public static readonly DateTime Start;
		#endregion // Fields

		static AppTime()
		{
			Array.Fill(_FpsHistory, 0f);
			Resolution = Math.Max((uint)Math.Ceiling(1e9 / Stopwatch.Frequency), 1);
			IsHighResolution = (Resolution <= 10_000);
			_Timer = Stopwatch.StartNew();
			Start = DateTime.Now;
		}

		internal static void Frame()
		{
			FrameCount++;

			// Check new time scale
			if (_NewScale.HasValue) {
				var old = _Scale;
				_Scale = _NewScale.Value;
				TimeScaleChanged?.Invoke(old, _Scale);
				_NewScale = null;
			}

			// Update timing
			LastElapsed = Elapsed;
			Elapsed = _Timer.Elapsed;
			RealDelta = Elapsed - LastElapsed;
			Delta = TimeSpan.FromTicks((long)(RealDelta.Ticks * _Scale));

			// Update FPS
			RawFPS = _FpsHistory[_FpsIndex] = 1000f / ((float)RealDelta.TotalMilliseconds + 1e-5f); // no div by 0
			FPS = _FpsHistory.Sum() / ((FrameCount < FPS_HISTORY_SIZE) ? FrameCount : FPS_HISTORY_SIZE);
			_FpsIndex = (_FpsIndex + 1) % FPS_HISTORY_SIZE;
		}

		/// <summary>
		/// Checks if the application run time is newly at or later than the passed time in the current frame.
		/// </summary>
		/// <param name="time">The time to check for.</param>
		/// <returns>If the application is at the passed time.</returns>
		public static bool IsTime(TimeSpan time) => (LastElapsed < time) && (Elapsed >= time);

		/// <summary>
		/// Checks if the application runtime is at a multiple of the passed time.
		/// </summary>
		/// <param name="time">The time multiple to check.</param>
		/// <returns>If the application is at a multiple of the passed time.</returns>
		public static bool IsTimeMultiple(TimeSpan time) 
			=> (Elapsed.TotalSeconds % time.TotalSeconds) < (LastElapsed.TotalSeconds % time.TotalSeconds);
	}
}
