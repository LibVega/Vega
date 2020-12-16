/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// Represents the different shader binding groups (resource namespaces). They have the numeric values matching 
	/// their shader set indices.
	/// </summary>
	public enum BindingGroup : byte
	{
		/// <summary>
		/// The binding set for buffer-like objects (set index = 0).
		/// <para>
		/// This set includes: uniform buffers, storage buffers, uniform texel buffers, storage texel buffers.
		/// </para>
		/// </summary>
		Buffers = 0,
		/// <summary>
		/// The binding set for samplers and sampler-like objects (set index = 1).
		/// <para>
		/// This set includes: samplers, bound samplers (combined image/samplers).
		/// </para>
		/// </summary>
		Samplers = 1,
		/// <summary>
		/// The binding set for texture and image objects (set index = 2).
		/// <para>
		/// This set includes: sampled textures, storage images.
		/// </para>
		/// </summary>
		Textures = 2,
		/// <summary>
		/// This binding set for render subpass input attachments (set index = 3). Input attachments are managed
		/// internally by the library.
		/// </summary>
		InputAttachments = 3
	}
}
