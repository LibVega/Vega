/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// Represents a target (either an offscreen image or a window) for rendering commands. Manages the rendering
	/// state and command submission for the target.
	/// </summary>
	public sealed class Renderer : IDisposable
	{
		#region Fields
		// The renderpass and framebuffers for this renderer
		internal readonly RenderPass RenderPass;

		/// <summary>
		/// The current size of the renderer targets.
		/// </summary>
		public Extent2D Size { get; private set; }
		/// <summary>
		/// The current MSAA setting for the renderer. Only applies to attachments that support MSAA.
		/// </summary>
		public MSAA MSAA { get; private set; }

		/// <summary>
		/// Gets if the renderer has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		/// <summary>
		/// Creates a new Renderer for submitting draw commands to an offscreen buffer. Offscreen Renderer instances
		/// must be manually rebuilt to change the size or anti-aliasing attributes.
		/// </summary>
		/// <param name="desc">The valid description of the renderer.</param>
		/// <param name="size">The initial size of the renderer buffer, cannot be (0, 0).</param>
		/// <param name="msaa">The initial MSAA of the renderer, if supported.</param>
		public Renderer(RendererDescription desc, Extent2D size, MSAA msaa = MSAA.X1)
		{
			// Validate size
			if (size.Area == 0) {
				throw new ArgumentException("Cannot use a zero size for a renderer", nameof(size));
			}

			// Validate description
			if (!desc.TryValidate(out var descErr, null)) {
				throw new ArgumentException("Invalid renderer description: " + descErr!, nameof(desc));
			}

			// Create framebuffer
			RenderPass = new RenderPass(this, desc, null);
			RenderPass.Rebuild(size, msaa);
			Size = size;
			MSAA = msaa;
		}
		/// <summary>
		/// Creates a new Renderer for submitting draw commands to a window surface. Window Renderer instances will be
		/// automatically rebuilt if the window size changes. 
		/// <para>
		/// Attachment 0 of the description must be compatible with
		/// the window surface (same format, and MSAA == 1 or is properly resolved at the end of the renderer).
		/// </para>
		/// </summary>
		/// <param name="desc">The valid description of the renderer.</param>
		/// <param name="window">The window to use as the render surface.</param>
		/// <param name="msaa">The initial MSAA of the renderer, if supported.</param>
		public Renderer(RendererDescription desc, Window window, MSAA msaa = MSAA.X1)
		{
			// Validate description
			if (!desc.TryValidate(out var descErr, window)) {
				throw new ArgumentException("Invalid renderer description: " + descErr!, nameof(desc));
			}

			// Create framebuffer
			RenderPass = new RenderPass(this, desc, window);
			RenderPass.Rebuild(window.Size, msaa);
			Size = window.Size;
			MSAA = msaa;
		}
		~Renderer()
		{
			dispose(false);
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed) {
				if (disposing) {
					RenderPass.Dispose();
				}
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
