/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega
{
	/// <summary>
	/// Callback for a change to <see cref="Window.Position"/>.
	/// </summary>
	/// <param name="window">The window whose position changed.</param>
	/// <param name="position">The new position of the window.</param>
	public delegate void WindowPositionCallback(Window window, in Point2 position);

	/// <summary>
	/// Callback for a change to <see cref="Window.Size"/>.
	/// </summary>
	/// <param name="window">The window whose size changed.</param>
	/// <param name="size">The new size of the window.</param>
	public delegate void WindowSizeCallback(Window window, in Extent2D size);

	/// <summary>
	/// Callback for a window gaining or losing focus.
	/// </summary>
	/// <param name="window">The window whose focus status changed.</param>
	/// <param name="focused"><c>true</c> if the window gained focus, <c>false</c> if focus was lost.</param>
	public delegate void WindowFocusCallback(Window window, bool focused);

	/// <summary>
	/// Callback for a window being iconified or restored.
	/// </summary>
	/// <param name="window">The window that was iconified or restored.</param>
	/// <param name="iconified"><c>true</c> if the window was minimized, <c>false</c> for maximized.</param>
	public delegate void WindowIconifyCallback(Window window, bool iconified);

	/// <summary>
	/// Callback for a window mode change.
	/// </summary>
	/// <param name="window">The window whose mode changed.</param>
	/// <param name="oldMode">The old window mode.</param>
	/// <param name="newMode">The new window mode.</param>
	public delegate void WindowModeCallback(Window window, WindowMode oldMode, WindowMode newMode);
}
