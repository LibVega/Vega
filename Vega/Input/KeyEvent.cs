/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Input
{
	/// <summary>
	/// The different event types for keyboard input events.
	/// </summary>
	public enum KeyEventType
	{
		/// <summary>
		/// The key was pressed down in the last frame.
		/// </summary>
		Pressed,
		/// <summary>
		/// The key was released in the last frame.
		/// </summary>
		Released,
		/// <summary>
		/// The key has been held long enough to start generating hold events.
		/// </summary>
		Held,
		/// <summary>
		/// The key was pressed and released quickly enough to register as a "tap".
		/// </summary>
		Tapped
	}


	/// <summary>
	/// Data describing a keyboard input event.
	/// </summary>
	public struct KeyEventData
	{
		#region Fields
		/// <summary>
		/// The event type.
		/// </summary>
		public KeyEventType Type;
		/// <summary>
		/// The key that generated the event.
		/// </summary>
		public Keys Key;
		/// <summary>
		/// The modifier keys for the key event.
		/// </summary>
		public ModKeyMask Mods;
		/// <summary>
		/// The time (in seconds) associated with the event, with different meanings for different event types:
		/// <list type="bullet">
		/// <item><term>Pressed</term> - The time since the key was last released (total release time).</item>
		/// <item><term>Released</term> - The time since the key was last pressed (total hold time).</item>
		/// <item><term>Held</term> - The running amount of time the key has been held for.</item>
		/// <item><term>Tapped</term> - The time since the last tap event.</item>
		/// </list>
		/// </summary>
		public float EventTime;
		/// <summary>
		/// The value of <see cref="AppTime.Elapsed"/> in seconds when the event was generated.
		/// </summary>
		public float Timestamp;
		#endregion // Fields

		internal KeyEventData(KeyEventType type, Keys key, ModKeyMask mask, float time)
		{
			Type = type;
			Key = key;
			Mods = mask;
			EventTime = time;
			Timestamp = (float)AppTime.Elapsed.TotalSeconds;
		}
	}


	/// <summary>
	/// Callback for a keyboard input event.
	/// </summary>
	/// <param name="keyboard">The keyboard instance generating the event.</param>
	/// <param name="data">Data about the event.</param>
	public delegate void KeyEvent(Keyboard keyboard, in KeyEventData data);
}
