/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;

namespace Vega.Input
{
	/// <summary>
	/// Reports keyboard input for a specific open <see cref="Window"/>. Supports both polling and event-based 
	/// operations.
	/// </summary>
	public sealed class Keyboard
	{
		/// <summary>
		/// The default quantity for the <see cref="HoldTime"/> field.
		/// </summary>
		public const float DEFAULT_HOLD_TIME = 0.5f;
		/// <summary>
		/// The default quantity for the <see cref="TapTime"/> field.
		/// </summary>
		public const float DEFAULT_TAP_TIME = 0.25f;

		#region Fields
		// Track states and times
		private readonly bool[] _lastKeys = new bool[KeysUtils.MAX_KEY_INDEX + 1];
		private readonly bool[] _currKeys = new bool[KeysUtils.MAX_KEY_INDEX + 1];
		private readonly float[] _lastPress = new float[KeysUtils.MAX_KEY_INDEX + 1];
		private readonly float[] _lastRelease = new float[KeysUtils.MAX_KEY_INDEX + 1];
		private readonly float[] _lastTap = new float[KeysUtils.MAX_KEY_INDEX + 1];
		private readonly List<Keys> _pressed = new List<Keys>(16);

		/// <summary>
		/// The window that this keyboard instance is managing keyboard input for.
		/// </summary>
		public readonly Window Window;

		#region Input Settings
		/// <summary>
		/// The amount of time (in seconds) that a key must be held down for to start generating hold events.
		/// </summary>
		public float HoldTime = DEFAULT_HOLD_TIME;

		/// <summary>
		/// The maximum amount of time (in seconds) between a press and release event to be considered a tap event.
		/// </summary>
		public float TapTime = DEFAULT_TAP_TIME;

		/// <summary>
		/// Gets or sets if <see cref="KeyEventType.Tapped"/> events are generated. Defaults to true.
		/// </summary>
		public bool TapEventsEnabled = true;
		#endregion // Input Settings

		/// <summary>
		/// The mask of modifier keys (shift, control, alt) currently pressed down.
		/// </summary>
		public ModKeyMask ModifierMask => _modMask;
		private ModKeyMask _modMask = new ModKeyMask(0);

		#region Events
		/// <summary>
		/// Event that is raised every time a key is pressed.
		/// </summary>
		public event KeyEvent? KeyPressed;
		/// <summary>
		/// Event that is raised every time a key is released.
		/// </summary>
		public event KeyEvent? KeyReleased;
		/// <summary>
		/// Events that is raised every time a key is released within a certain time after being pressed.
		/// </summary>
		public event KeyEvent? KeyTapped;
		/// <summary>
		/// Event that is raised while a key is being held down.
		/// </summary>
		public event KeyEvent? KeyHeld;
		/// <summary>
		/// Event used to subscribe or unsubscribe from all input events.
		/// </summary>
		public event KeyEvent AllKeyEvents
		{
			add { KeyPressed += value; KeyReleased += value; KeyTapped += value; KeyHeld += value; }
			remove { KeyPressed -= value; KeyReleased -= value; KeyTapped -= value; KeyHeld -= value; }
		}
		#endregion // Events

		// Function register, needed to keep managed delegates around for unmanaged calls
		private Glfw.GLFWkeyfun _keyfunc;
		#endregion // Fields

		internal Keyboard(Window window)
		{
			Window = window;

			for (int i = 0; i < KeysUtils.MAX_KEY_INDEX; ++i) {
				_lastKeys[i] = _currKeys[i] = false;
				_lastPress[i] = _lastRelease[i] = _lastTap[i] = 0;
			}

			_keyfunc = (window, key, scancode, action, mods) => {
				if (action == Glfw.REPEAT) return;
				var keys = KeysUtils.Translate(key);
				if (keys == Keys.Unknown) return;
				handleKey(keys, scancode, action == Glfw.PRESS);
			};
			Glfw.SetKeyCallback(window.Handle, _keyfunc);
		}

		internal void NewFrame()
		{
			Array.Copy(_currKeys, _lastKeys, _currKeys.Length);
		}

		internal void ProcessHoldEvents()
		{
			var elapsed = (float)AppTime.Elapsed.TotalSeconds;
			foreach (var keys in _pressed) {
				float diff = elapsed - _lastPress[(int)keys];
				KeyHeld?.Invoke(this, new KeyEventData(KeyEventType.Held, keys, ModifierMask, diff));
			}
		}

		#region Polling
		/// <summary>
		/// Gets if the key is currently pressed.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public bool IsKeyDown(Keys key) => _currKeys[(int)key];
		/// <summary>
		/// Gets if the key is currently released.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public bool IsKeyUp(Keys key) => !_currKeys[(int)key];
		/// <summary>
		/// Gets if the key was pressed in the previous frame.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public bool IsKeyPreviouslyDown(Keys key) => _lastKeys[(int)key];
		/// <summary>
		/// Gets if the key was released in the previous frame.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public bool IsKeyPreviouslyUp(Keys key) => !_lastKeys[(int)key];
		/// <summary>
		/// Gets if the key was just pressed in this frame.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public bool IsKeyPressed(Keys key) => _currKeys[(int)key] && !_lastKeys[(int)key];
		/// <summary>
		/// Gets if the key was just released in this frame.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public bool IsKeyReleased(Keys key) => !_currKeys[(int)key] && _lastKeys[(int)key];
		/// <summary>
		/// Gets if the key is currently generating hold events.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public bool IsKeyHeld(Keys key) => 
			_currKeys[(int)key] && ((AppTime.Elapsed.TotalSeconds - _lastPress[(int)key]) >= HoldTime);
		/// <summary>
		/// Gets the running hold time for the passed key, or zero if the key is not held.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public float GetKeyHoldTime(Keys key) =>
			_currKeys[(int)key] ? (float)(AppTime.Elapsed.TotalSeconds - _lastPress[(int)key]) : 0;

		/// <summary>
		/// Gets an array of the keys that are currently pressed down.
		/// </summary>
		public Keys[] GetCurrentKeys() => _pressed.ToArray();
		/// <summary>
		/// Enumerator for all of the keys that are currently pressed down.
		/// </summary>
		public IEnumerator<Keys> EnumerateCurrentKeys()
		{
			foreach (var key in _pressed)
				yield return key;
		}

		/// <summary>
		/// Gets the value of <see cref="Time.Elapsed"/> when the key was last pressed.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public float GetLastPressTime(Keys key) => _lastPress[(int)key];
		/// <summary>
		/// Gets the value of <see cref="Time.Elapsed"/> when the key was last released.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public float GetLastReleaseTime(Keys key) => _lastRelease[(int)key];
		/// <summary>
		/// Gets the value of <see cref="Time.Elapsed"/> when the key was last tapped.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public float GetLastTapTime(Keys key) => _lastTap[(int)key];
		#endregion // Polling

		private void handleKey(Keys key, int scancode, bool press)
		{
			var index = (int)key;
			var time = (float)AppTime.Elapsed.TotalSeconds;

			// Update modifier state
			switch (key)
			{
				case Keys.LeftShift: _modMask.LeftShift = press; break;
				case Keys.LeftControl: _modMask.LeftControl = press; break;
				case Keys.LeftAlt: _modMask.LeftAlt = press; break;
				case Keys.RightShift: _modMask.RightShift = press; break;
				case Keys.RightControl: _modMask.RightControl = press; break;
				case Keys.RightAlt: _modMask.RightAlt = press; break;
			}

			// Update key state
			if (press) {
				_currKeys[index] = true;
				_lastPress[index] = time;
				_pressed.Add(key);

				KeyPressed?.Invoke(this, 
					new KeyEventData(KeyEventType.Pressed, key, ModifierMask, time - _lastRelease[index]));
			}
			else { // release
				_currKeys[index] = false;
				_lastRelease[index] = time;
				_pressed.Remove(key);
				float diff = time - _lastPress[index];

				KeyReleased?.Invoke(this,
					new KeyEventData(KeyEventType.Released, key, ModifierMask, diff));
				if (TapEventsEnabled && (diff <= TapTime)) {
					KeyTapped?.Invoke(this,
						new KeyEventData(KeyEventType.Tapped, key, ModifierMask, time - _lastTap[index]));
					_lastTap[index] = time;
				}
			}
		}
	}
}
