/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// Represents a set of rendering commands recorded with a <see cref="CommandRecorder"/> instance, and available
	/// for submission to <see cref="Renderer"/>.
	/// <para>
	/// A command list can only be submitted within a single render pass. Once <see cref="Renderer.End"/> is called, 
	/// all command lists for that renderer are invalidated and must be re-recorded.
	/// </para>
	/// </summary>
	public sealed class CommandList
	{
		#region Fields
		/// <summary>
		/// The renderer instance that the commands were recorded for. The commands must be submitted to this renderer.
		/// </summary>
		public readonly Renderer Renderer;
		/// <summary>
		/// The renderer subpass that the commands were recorded for.
		/// </summary>
		public readonly uint Subpass;

		// The buffer holding the commands
		internal CommandBuffer? Buffer { get; private set; } = null;
		/// <summary>
		/// Gets if the command list is still valid for submission.
		/// </summary>
		public bool IsValid => Buffer is not null;
		#endregion // Fields

		internal CommandList(Renderer renderer, uint subpass, CommandBuffer buffer)
		{
			Renderer = renderer;
			Subpass = subpass;
			Buffer = buffer;

			Renderer.TrackList(this);
		}

		// Perform command list invalidation
		internal void Invalidate() => Buffer = null;
	}
}
