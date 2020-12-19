﻿/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// Represents a set of graphics commands recorded with <see cref="CommandRecorder"/>, which are ready to be
	/// submitted to <see cref="Renderer"/>.
	/// <para>
	/// A command list can only be submitted once, and then becomes invalid. Additionally, once the associated
	/// Renderer is ended, all command lists for the current frame (even if not submitted) become invalid.
	/// </para>
	/// </summary>
	public sealed class RenderTask
	{
		#region Fields
		/// <summary>
		/// The Renderer instance that the commands were generated for.
		/// </summary>
		public readonly Renderer Renderer;
		/// <summary>
		/// The subpass that the commands were generated for.
		/// </summary>
		public readonly uint Subpass;

		// The command buffer
		internal CommandBuffer? Buffer { get; private set; }
		/// <summary>
		/// Gets if the command list is valid (has not yet been submitted).
		/// </summary>
		public bool IsValid => Buffer is not null;
		#endregion // Fields

		internal RenderTask(Renderer renderer, uint subpass, CommandBuffer cmd)
		{
			Renderer = renderer;
			Subpass = subpass;
			Buffer = cmd;

			// TODO: For non-transient commands (not yet used), tracking might need to start here
		}

		// Perform submit-time invalidation
		internal void Invalidate() => Buffer = null;
	}
}