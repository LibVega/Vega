/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Audio
{
	/// <summary>
	/// The states of audio playback objects.
	/// </summary>
	public enum PlaybackState
	{
		/// <summary>
		/// The audio source is inactive and not producing sound.
		/// </summary>
		Stopped,
		/// <summary>
		/// The audio source is active, but is not currently progressing.
		/// </summary>
		Paused,
		/// <summary>
		/// The audio source is active, and is currently progressing and contributing to sound output.
		/// </summary>
		Playing
	}
}
