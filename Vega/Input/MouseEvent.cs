﻿/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Input
{
	/// <summary>
	/// Represents the different possible input events for mouse buttons.
	/// </summary>
	public enum ButtonEventType : byte
	{
		/// <summary>
		/// The button was pressed down in the last frame.
		/// </summary>
		Pressed,
		/// <summary>
		/// The button was released in the last frame.
		/// </summary>
		Released,
		/// <summary>
		/// The button was clicked, which is a press followed by a release within a certain amount of time and within
		/// a certain range of screen movement.
		/// </summary>
		Clicked,
		/// <summary>
		/// The button was double clicked, which are two Clicked events within a certain amount of time and within a
		/// certain range of screen movement.
		/// </summary>
		DoubleClicked
	}


	/// <summary>
	/// Data describing an input event of a mouse button.
	/// </summary>
	public readonly struct MouseButtonEventData
	{
		#region Fields
		/// <summary>
		/// The type of event.
		/// </summary>
		public readonly ButtonEventType Type;
		/// <summary>
		/// The button that generated this event.
		/// </summary>
		public readonly MouseButton Button;
		/// <summary>
		/// The time (in seconds) associated with the event. This field takes on different meanings based on the event type:
		/// <list type="bullet">
		/// <item><term>Pressed</term> The time since the button was last released.</item>
		/// <item><term>Released</term> The time since the button was last pressed.</item>
		/// <item><term>Clicked</term> The time between the press and release of the event.</item>
		/// <item><term>DoubleClicked</term> The time between the component click events.</item>
		/// </list>
		/// </summary>
		public readonly float EventTime;
		/// <summary>
		/// The application time (in seconds) at which this event was generated.
		/// </summary>
		public readonly float Timestamp;

		#region Helpers
		/// <summary>
		/// Gets if the event button is <see cref="MouseButton.Left"/>.
		/// </summary>
		public readonly bool Left => (Button == MouseButton.Left);
		/// <summary>
		/// Gets if the event button is <see cref="MouseButton.Middle"/>.
		/// </summary>
		public readonly bool Middle => (Button == MouseButton.Middle);
		/// <summary>
		/// Gets if the event button is <see cref="MouseButton.Right"/>.
		/// </summary>
		public readonly bool Right => (Button == MouseButton.Right);
		/// <summary>
		/// Gets if the event button is <see cref="MouseButton.X1"/>.
		/// </summary>
		public readonly bool X1 => (Button == MouseButton.X1);
		/// <summary>
		/// Gets if the event button is <see cref="MouseButton.X2"/>.
		/// </summary>
		public readonly bool X2 => (Button == MouseButton.X2);
		/// <summary>
		/// Gets if the event button is <see cref="MouseButton.X3"/>.
		/// </summary>
		public readonly bool X3 => (Button == MouseButton.X3);
		/// <summary>
		/// Gets if the event button is <see cref="MouseButton.X4"/>.
		/// </summary>
		public readonly bool X4 => (Button == MouseButton.X4);
		/// <summary>
		/// Gets if the event button is <see cref="MouseButton.X5"/>.
		/// </summary>
		public readonly bool X5 => (Button == MouseButton.X5);

		/// <summary>
		/// Gets if the event is a <see cref="ButtonEventType.Pressed"/> event.
		/// </summary>
		public readonly bool Press => (Type == ButtonEventType.Pressed);
		/// <summary>
		/// Gets if the event is a <see cref="ButtonEventType.Released"/> event.
		/// </summary>
		public readonly bool Release => (Type == ButtonEventType.Released);
		/// <summary>
		/// Gets if the event is a <see cref="ButtonEventType.Clicked"/> event.
		/// </summary>
		public readonly bool Click => (Type == ButtonEventType.Clicked);
		/// <summary>
		/// Gets if the event is a <see cref="ButtonEventType.DoubleClicked"/> event.
		/// </summary>
		public readonly bool DoubleClick => (Type == ButtonEventType.DoubleClicked);
		#endregion // Helpers
		#endregion // Fields

		internal MouseButtonEventData(ButtonEventType type, MouseButton button, float time)
		{
			Type = type;
			Button = button;
			EventTime = time;
			Timestamp = (float)AppTime.Elapsed.TotalSeconds;
		}
	}


	/// <summary>
	/// Data describing an input event of the mouse moving.
	/// </summary>
	public readonly struct MouseMoveEventData
	{
		#region Fields
		/// <summary>
		/// The current position of the mouse, when the event was fired.
		/// </summary>
		public readonly Point2 Current;
		/// <summary>
		/// The position of the mouse in the frame before the event was fired.
		/// </summary>
		public readonly Point2 Last;
		/// <summary>
		/// The change in position of this move event.
		/// </summary>
		public readonly Point2 Delta => Current - Last;
		/// <summary>
		/// The mask of buttons that were down during the move event.
		/// </summary>
		public readonly MouseButtonMask Buttons;
		/// <summary>
		/// The application time (in seconds) at which this event was generated.
		/// </summary>
		public readonly float Timestamp;
		#endregion // Fields

		internal MouseMoveEventData(in Point2 curr, in Point2 last, in MouseButtonMask buttons)
		{
			Current = curr;
			Last = last;
			Buttons = buttons;
			Timestamp = (float)AppTime.Elapsed.TotalSeconds;
		}
	}


	/// <summary>
	/// Data describing an input event of the mouse wheel.
	/// </summary>
	public readonly struct MouseWheelEventData
	{
		#region Fields
		/// <summary>
		/// The change in the mouse wheel value in both dimensions.
		/// </summary>
		public readonly Point2 Delta;
		/// <summary>
		/// The x-value delta of the mouse wheel. This is the standard up/down scroll direction for mice.
		/// </summary>
		public readonly int X => Delta.X;
		/// <summary>
		/// The y-value delta of the mouse wheel. This is horizonal change only supported by some mice.
		/// </summary>
		public readonly int Y => Delta.Y;
		/// <summary>
		/// The application time (in seconds) at which this event was generated.
		/// </summary>
		public readonly float Timestamp;
		#endregion // Fields

		internal MouseWheelEventData(in Point2 delta)
		{
			Delta = delta;
			Timestamp = (float)AppTime.Elapsed.TotalSeconds;
		}
	}


	/// <summary>
	/// Callback for a mouse button event.
	/// </summary>
	/// <param name="mouse">The mouse that generated the event.</param>
	/// <param name="data">The data describing the event.</param>
	public delegate void MouseButtonEvent(Mouse mouse, MouseButtonEventData data);

	/// <summary>
	/// Callback for a mouse move event.
	/// </summary>
	/// <param name="mouse">The mouse that generated the event.</param>
	/// <param name="data">The data describing the event.</param>
	public delegate void MouseMoveEvent(Mouse mouse, MouseMoveEventData data);

	/// <summary>
	/// Callback for a mouse wheel event.
	/// </summary>
	/// <param name="mouse">The mouse that generated the event.</param>
	/// <param name="data">The data describing the event.</param>
	public delegate void MouseWheelEvent(Mouse mouse, MouseWheelEventData data);

	/// <summary>
	/// Callback for when the cursor mode for the mouse is changed.
	/// </summary>
	/// <param name="mouse">The mouse that generated the event.</param>
	/// <param name="oldMode">The old mouse cursor mode.</param>
	/// <param name="newMode">The new mouse curosr mode.</param>
	public delegate void CursorModeChangedEvent(Mouse mouse, CursorMode oldMode, CursorMode newMode);

	/// <summary>
	/// Callback for when the mouse cursor enters or leaves the window.
	/// </summary>
	/// <param name="mouse">The mouse that generated the event.</param>
	/// <param name="entered"><c>true</c> if the cursor entered the window, <c>false</c> if it left the window.</param>
	public delegate void CursorEnteredEvent(Mouse mouse, bool entered);
}
