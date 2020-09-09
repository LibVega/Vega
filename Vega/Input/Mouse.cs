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
	/// Cursor/window interaction modes.
	/// </summary>
	public enum CursorMode
	{
		/// <summary>
		/// The system cursor appears over the window, and can move freely around and off of the window.
		/// </summary>
		Normal,
		/// <summary>
		/// The cursor can move freely, but is hidden while over the window.
		/// </summary>
		Hidden,
		/// <summary>
		/// The cursor is invisible and is locked in the window, and virtual mouse inputs are provided to maintain
		/// full logical mouse movements.
		/// </summary>
		Locked
	}

	/// <summary>
	/// Reports mouse input for a specific open <see cref="Window"/>. Supports both polling and event-based 
	/// operations.
	/// </summary>
	public sealed class Mouse
	{
		#region Fields
		// Event tracking
		private MouseButtonMask _lastMouse = MouseButtonMask.None;
		private MouseButtonMask _currMouse = MouseButtonMask.None;
		private readonly float[] _lastPress = new float[MouseButtonUtils.MAX_BUTTON_INDEX + 1];
		private readonly float[] _lastRelease = new float[MouseButtonUtils.MAX_BUTTON_INDEX + 1];
		private readonly Point2[] _lastPressPos = new Point2[MouseButtonUtils.MAX_BUTTON_INDEX + 1];
		private readonly Point2[] _lastReleasePos = new Point2[MouseButtonUtils.MAX_BUTTON_INDEX + 1];

		// Click event tracking
		private readonly float[] _lastClick = new float[MouseButtonUtils.MAX_BUTTON_INDEX + 1]; // Time of last click
		private readonly Point2[] _lastClickPos = new Point2[MouseButtonUtils.MAX_BUTTON_INDEX + 1]; // Pos of last click
		private readonly bool[] _lastClickFrame = new bool[MouseButtonUtils.MAX_BUTTON_INDEX + 1]; // Click event in last frame
		private readonly bool[] _nextDouble = new bool[MouseButtonUtils.MAX_BUTTON_INDEX + 1]; // Next click is double click

		// Position tracking
		private Point2 _lastPos = Point2.Zero;
		private Point2 _currPos = Point2.Zero;
		private Point2 _deltaWheel = Point2.Zero;

		// Enter/Leave tracking
		private bool _enterEvent = false;
		private bool _enterData = false;

		#region Input Settings
		/// <summary>
		/// A mask of the buttons that can generate double click events. Defaults to the primary buttons.
		/// Max amount of time between a press and release event to generate a click event. Defaults to half a second.
		/// </summary>
		public float ClickTime = 0.5f;

		/// <summary>
		/// Max amount of time between two click events to generated a double click event. Defaults to half a second.
		/// </summary>
		public float DoubleClickTime = 0.5f;

		/// <summary>
		/// Max distance (in pixels) between a two events to generate a click or double click event. Set to zero to
		/// disable this check. Defaults to 25 pixels.
		/// </summary>
		public uint ClickDistance = 25;

		/// <summary>
		/// The input mode used by the mouse cursor, see the CursorMode values for more information. Fires the
		/// <see cref="CursorModeChanged"/> event when changed.
		/// </summary>
		public CursorMode CursorMode
		{
			get => Glfw.GetInputMode(Window.Handle, Glfw.CURSOR) switch {
				Glfw.CURSOR_DISABLED => CursorMode.Locked,
				Glfw.CURSOR_HIDDEN => CursorMode.Hidden,
				_ => CursorMode.Normal
			};
			set {
				var old = CursorMode;
				Glfw.SetInputMode(Window.Handle, Glfw.CURSOR, value switch {
					CursorMode.Locked => Glfw.CURSOR_DISABLED,
					CursorMode.Hidden => Glfw.CURSOR_HIDDEN,
					_ => Glfw.CURSOR_NORMAL
				});
				CursorModeChanged?.Invoke(this, old, value);
			}
		}
		#endregion // Input Settings

		#region Events
		/// <summary>
		/// Event that is raised whenever a mouse button is pressed.
		/// </summary>
		public event MouseButtonEvent? ButtonPressed;
		/// <summary>
		/// Event that is raised whenever a mouse button is released.
		/// </summary>
		public event MouseButtonEvent? ButtonReleased;
		/// <summary>
		/// Event that is raised whenever a mouse button is clicked or double clicked.
		/// </summary>
		public event MouseButtonEvent? ButtonClicked;
		/// <summary>
		/// Event that can be used to subscribe or unsubscribe from all mouse button events.
		/// </summary>
		public event MouseButtonEvent? AllButtonEvents
		{
			add { ButtonPressed += value; ButtonReleased += value; ButtonClicked += value; }
			remove { ButtonPressed -= value; ButtonReleased -= value; ButtonClicked -= value; }
		}
		/// <summary>
		/// Event that is raised when the mouse is moved.
		/// </summary>
		public event MouseMoveEvent? Moved;
		/// <summary>
		/// Event that is raised when the mouse wheel is moved.
		/// </summary>
		public event MouseWheelEvent? WheelChanged;

		/// <summary>
		/// Event that is raised when the <see cref="CursorMode"/> of the mouse is changed.
		/// </summary>
		public event CursorModeChangedEvent? CursorModeChanged;

		/// <summary>
		/// Event that is raised when the mouse cursor either enters or leaves the window.
		/// </summary>
		public event CursorEnteredEvent? CursorEntered;
		#endregion // Events

		/// <summary>
		/// The window that this mouse instance is managing mouse input for.
		/// </summary>
		public readonly Window Window;

		// Function registers, needed to keep managed delegates around for unmanaged calls
		private Glfw.GLFWmousebuttonfun _buttonfunc;
		private Glfw.GLFWscrollfun _scrollfunc;
		private Glfw.Glfwcursorenterfun _cursorenterfunc;
		#endregion // Fields

		internal Mouse(Window window)
		{
			Window = window;

			for (int i = 0; i < MouseButtonUtils.MAX_BUTTON_INDEX; ++i) {
				_lastPress[i] = _lastRelease[i] = 0;
				_lastPressPos[i] = _lastReleasePos[i] = _lastClickPos[i] = Point2.Zero;
				_lastClick[i] = 0;
				_lastClickFrame[i] = _nextDouble[i] = false;
			}

			_buttonfunc = (window, button, action, mods) => {
				var but = MouseButtonUtils.Translate(button);
				handleButton(but, action == Glfw.PRESS);
			};
			Glfw.SetMouseButtonCallback(Window.Handle, _buttonfunc);
			_scrollfunc = (window, xoff, yoff) => {
				_deltaWheel = new Point2((int)xoff, (int)yoff);
			};
			Glfw.SetScrollCallback(Window.Handle, _scrollfunc);
			_cursorenterfunc = (window, entered) => {
				_enterEvent = true;
				_enterData = (entered == Glfw.TRUE);
			};
			Glfw.SetCursorEnterCallback(Window.Handle, _cursorenterfunc);
		}

		internal void NewFrame()
		{
			_lastMouse = _currMouse;
			_lastPos = _currPos;
			_deltaWheel = Point2.Zero;
			Glfw.GetCursorPos(Window.Handle, out var xpos, out var ypos);
			_currPos = new Point2((int)xpos, (int)ypos);
			for (int i = 0; i < MouseButtonUtils.MAX_BUTTON_INDEX; ++i)
				_lastClickFrame[i] = false;
			_enterEvent = false;
		}

		internal void HandleEvents()
		{
			if (_currPos != _lastPos) {
				Moved?.Invoke(this, new MouseMoveEventData(_currPos, _lastPos, _currMouse));
			}
			if (_deltaWheel != Point2.Zero) {
				WheelChanged?.Invoke(this, new MouseWheelEventData(_deltaWheel));
			}
			if (_enterEvent) {
				CursorEntered?.Invoke(this, _enterData);
			}
		}

		#region Polling
		/// <summary>
		/// Gets if the mouse button is currently pressed.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public bool IsButtonDown(MouseButton mb) => _currMouse.GetButton(mb);
		/// <summary>
		/// Gets if the mouse button is currently released.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public bool IsButtonUp(MouseButton mb) => !_currMouse.GetButton(mb);
		/// <summary>
		/// Gets if the mouse button was pressed in the previous frame.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public bool IsButtonPreviouslyDown(MouseButton mb) => _lastMouse.GetButton(mb);
		/// <summary>
		/// Gets if the mouse button was released in the previous frame.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public bool IsButtonPreviouslyUp(MouseButton mb) => !_lastMouse.GetButton(mb);
		/// <summary>
		/// Gets if the button was just pressed in this frame.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public bool IsButtonPressed(MouseButton mb) => _currMouse.GetButton(mb) && !_lastMouse.GetButton(mb);
		/// <summary>
		/// Gets if the button was just released in this frame.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public bool IsButtonReleased(MouseButton mb) => !_currMouse.GetButton(mb) && _lastMouse.GetButton(mb);
		/// <summary>
		/// Gets if the button was clicked or double clicked in this frame.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public bool IsButtonClicked(MouseButton mb) => _lastClickFrame[(int)mb];
		/// <summary>
		/// Gets if the button was double clicked in this frame. Returns false for clicks that are not double.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public bool IsButtonDoubleClicked(MouseButton mb) => _lastClickFrame[(int)mb] && !_nextDouble[(int)mb];

		/// <summary>
		/// Gets a mask of all of the mouse buttons that are currently pressed down.
		/// </summary>
		public MouseButtonMask GetCurrentButtons() => _currMouse;
		/// <summary>
		/// Enumerator for all of the keys that are currently pressed down.
		/// </summary>
		public IEnumerator<MouseButton> EnumerateCurrentButtons()
		{
			if (_currMouse.Left) yield return MouseButton.Left;
			if (_currMouse.Right) yield return MouseButton.Right;
			if (_currMouse.Middle) yield return MouseButton.Middle;
			if (_currMouse.X1) yield return MouseButton.X1;
			if (_currMouse.X2) yield return MouseButton.X2;
			if (_currMouse.X3) yield return MouseButton.X3;
			if (_currMouse.X4) yield return MouseButton.X4;
			if (_currMouse.X5) yield return MouseButton.X5;
		}

		/// <summary>
		/// Gets the value of <see cref="Time.Elapsed"/> when the mouse button was last pressed.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public float GetLastPressTime(MouseButton mb) => _lastPress[(int)mb];
		/// <summary>
		/// Gets the value of <see cref="Time.Elapsed"/> when the mouse button was last released.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public float GetLastReleaseTime(MouseButton mb) => _lastRelease[(int)mb];
		/// <summary>
		/// Gets the value of <see cref="Time.Elapsed"/> when the mouse button was last clicked.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public float GetLastClickTime(MouseButton mb) => _lastClick[(int)mb];
		/// <summary>
		/// Gets the screen position where the mouse cursor was when the button was last pressed.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public Point2 GetLastPressPos(MouseButton mb) => _lastPressPos[(int)mb];
		/// <summary>
		/// Gets the screen position where the mouse cursor was when the button was last released.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public Point2 GetLastReleasePos(MouseButton mb) => _lastReleasePos[(int)mb];
		/// <summary>
		/// Gets the screen position where the mouse cursor was when the button was last clicked.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public Point2 GetLastClickPos(MouseButton mb) => _lastClickPos[(int)mb];

		/// <summary>
		/// Gets the current position of the mouse, in pixels, relative to the top-left corner of the window client area.
		/// </summary>
		public Point2 GetPosition() => _currPos;
		/// <summary>
		/// Gets if the position of the mouse in the last frame.
		/// </summary>
		public Point2 GetLastPosition() => _lastPos;
		/// <summary>
		/// Gets the change in mouse position between this frame and the last.
		/// </summary>
		public Point2 GetDelta() => _currPos - _lastPos;
		/// <summary>
		/// Gets if the mouse moved between this frame and the last.
		/// </summary>
		public bool GetMoved() => (_currPos - _lastPos) != Point2.Zero;

		/// <summary>
		/// Gets the change in the mouse wheel between this frame and the last. The X-component of the return value is
		/// the change along the primary wheel axis. The Y-component will only be non-zero on mice that support a
		/// second wheel axis.
		/// </summary>
		public Point2 GetWheelDelta() => _deltaWheel;

		/// <summary>
		/// Gets if the cursor entered the window area in this frame.
		/// </summary>
		public bool GetCursorEntered() => _enterEvent && _enterData;
		/// <summary>
		/// Gets if the cursor exited the window area in this frame.
		/// </summary>
		public bool GetCursorExited() => _enterEvent && !_enterData;
		/// <summary>
		/// Gets if the mouse cursor is currently in the window area. Note that this value may be inaccurate if the
		/// window does not have focus, or if the application has just started up.
		/// </summary>
		public bool IsInWindow() => _enterData;
		#endregion // Polling

		private void handleButton(MouseButton button, bool pressed)
		{
			var index = (int)button;
			var time = (float)AppTime.Elapsed.TotalSeconds;

			if (pressed) {
				_currMouse.SetButton(button);
				_lastPress[index] = time;
				_lastPressPos[index] = _currPos;
				ButtonPressed?.Invoke(this,
					new MouseButtonEventData(ButtonEventType.Pressed, button, time - _lastRelease[index]));
			}
			else { // released
				_currMouse.ClearButton(button);
				_lastRelease[index] = time;
				var lastClick = _lastReleasePos[index];
				_lastReleasePos[index] = _currPos;
				var diff = time - _lastPress[index];
				ButtonReleased?.Invoke(this,
					new MouseButtonEventData(ButtonEventType.Released, button, diff));

				if ((diff < ClickTime) && 
					(ClickDistance == 0 || Point2.Distance(_currPos, _lastPressPos[index]) <= ClickDistance)) {
					ButtonClicked?.Invoke(this,
						new MouseButtonEventData(ButtonEventType.Clicked, button, diff));

					if (_nextDouble[index]) {
						if (((time - _lastClick[index]) < DoubleClickTime) &&
							(ClickDistance == 0 || Point2.Distance(_currPos, lastClick) <= ClickDistance)) {
							ButtonClicked?.Invoke(this,
								new MouseButtonEventData(ButtonEventType.DoubleClicked, button, time - _lastClick[index]));
							_nextDouble[index] = false;
						}
					}
					else {
						_nextDouble[index] = true;
					}

					_lastClickFrame[index] = true;
					_lastClick[index] = time;
					_lastClickPos[index] = _currPos;
				}
				else {
					_nextDouble[index] = false;
				}
			}
		}
	}
}
