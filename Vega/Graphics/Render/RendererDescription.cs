/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Vega.Graphics
{
	/// <summary>
	/// Describes the render targets and subpass layout required to create a <see cref="Renderer"/> instance.
	/// Instances are immutable once <see cref="Build(out string?)"/> is called.
	/// </summary>
	public sealed class RendererDescription
	{
		#region Fields
		/// <summary>
		/// The render target attachments that will be used in the renderer.
		/// </summary>
		public IReadOnlyList<AttachmentDescription> Attachments => _attachments;
		private readonly List<AttachmentDescription> _attachments;

		/// <summary>
		/// Gets if this description has been built (validated). Once built, a description cannot be
		/// changed. An unbuilt description cannot be used to create a <see cref="Renderer"/>.
		/// </summary>
		public bool IsBuilt { get; private set; } = false;
		#endregion // Fields

		/// <summary>
		/// Start a new renderer description with the given attachments.
		/// </summary>
		/// <param name="attachments">The renderer attachments.</param>
		public RendererDescription(params AttachmentDescription[] attachments)
		{
			_attachments = new(attachments);
		}

		/// <summary>
		/// Add an additional attachment to the renderer description.
		/// </summary>
		/// <param name="attachment">The description of the new attachment.</param>
		/// <returns>The same description to facilitate chaning.</returns>
		public RendererDescription AddAttachment(AttachmentDescription attachment)
		{
			_attachments.Add(attachment);
			return this;
		}

		/// <summary>
		/// Attempts to build the description, which validates the contents and creates some additional internal
		/// values so it can be used by <see cref="Renderer"/> instances.
		/// </summary>
		/// <param name="error">A human-readable message of the build error, or <c>null</c> on success.</param>
		/// <param name="window">The optional window against which validity checks are performed.</param>
		/// <returns>
		/// If the description was build successfully, <c>false</c> will provide an error in <paramref name="error"/>.
		/// </returns>
		public bool Build(out string? error, Window? window)
		{
			if (IsBuilt) {
				error = null;
				return true;
			}

			// Update and Return
			IsBuilt = true;
			error = null;
			return true;
		}
	}
}
