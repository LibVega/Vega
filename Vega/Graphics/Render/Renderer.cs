/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Diagnostics;
using static Vega.InternalLog;

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
		/// The window associated with the renderer, if this is not an offscreen renderer.
		/// </summary>
		public readonly Window? Window;
		/// <summary>
		/// Gets if this renderer is performing offscreen rendering operations.
		/// </summary>
		public bool IsOffscreen => Window is null;

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
			Window = null;

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
			// Check window
			if (window.Renderer is not null) {
				throw new InvalidOperationException("Cannot create a Renderer for a window that already has a renderer");
			}
			Window = window;

			// Validate description
			if (!desc.TryValidate(out var descErr, window)) {
				throw new ArgumentException("Invalid renderer description: " + descErr!, nameof(desc));
			}

			// Create framebuffer
			RenderPass = new RenderPass(this, desc, window);
			RenderPass.Rebuild(window.Size, msaa);
			Size = window.Size;
			MSAA = msaa;

			// Assign as official window renderer
			Window.Renderer = this;
		}
		~Renderer()
		{
			dispose(false);
		}

		#region Settings
		/// <summary>
		/// Sets the new size of the renderer targets. This is a no-op if the size is not changing. Calling this
		/// on a window-attached renderer will generate an exception.
		/// </summary>
		/// <param name="newSize">The new size of the offscreen render target.</param>
		public void SetSize(Extent2D newSize)
		{
			if (!IsOffscreen) {
				throw new InvalidOperationException("Cannot call SetSize on a renderer attached to a window");
			}
			if (newSize == Size) {
				return; // Skip expensive rebuild (important check)
			}

			var timer = Stopwatch.StartNew();
			RenderPass.Rebuild(newSize, MSAA);
			LINFO($"Rebuilt renderer ({Size} -> {newSize}) (elapsed = {timer.Elapsed.TotalMilliseconds}ms)", this);
			Size = newSize;
		}

		/// <summary>
		/// Sets the new MSAA setting on the renderer targets. This is a no-op if the msaa is not changing. Attempting
		/// to set a non-one MSAA on a renderer that doesn't support MSAA will generate an exception.
		/// </summary>
		/// <param name="msaa">The new msaa setting.</param>
		public void SetMSAA(MSAA msaa)
		{
			if (!RenderPass.HasMSAA) {
				throw new InvalidOperationException("Cannot call SetMSAA on non-msaa renderer instance");
			}
			if (msaa == MSAA) {
				return; // Skip expensive rebuild (important check)
			}

			var timer = Stopwatch.StartNew();
			RenderPass.Rebuild(Size, msaa);
			LINFO($"Rebuilt renderer ({MSAA} -> {msaa}) (elapsed = {timer.Elapsed.TotalMilliseconds}ms)", this);
			MSAA = msaa;
		}

		/// <summary>
		/// Sets the size and MSAA setting of the renderer in one operation. This is significantly more efficient than
		/// setting them separately in cases where both are changing at the same time.
		/// </summary>
		/// <param name="newSize">The new size of the renderer.</param>
		/// <param name="msaa">The new msaa setting for the renderer.</param>
		public void SetSizeAndMSAA(Extent2D newSize, MSAA msaa)
		{
			if (!IsOffscreen) {
				throw new InvalidOperationException("Cannot call SetSizeAndMSAA on a renderer attached to a window");
			}
			if (!RenderPass.HasMSAA) {
				throw new InvalidOperationException("Cannot call SetSizeAndMSAA on non-msaa renderer instance");
			}
			if ((Size == newSize) && (msaa == MSAA)) {
				return; // Skip expensive rebuild (important check)
			}

			var timer = Stopwatch.StartNew();
			RenderPass.Rebuild(newSize, msaa);
			LINFO($"Rebuilt renderer ({Size} -> {newSize}) ({MSAA} -> {msaa}) (elapsed = {timer.Elapsed.TotalMilliseconds}ms)", this);
			Size = newSize;
			MSAA = msaa;
		}

		// Called by the swapchain when resizing
		internal void OnSwapchainResize(Extent2D newSize)
		{
			var timer = Stopwatch.StartNew();
			RenderPass.Rebuild(newSize, MSAA);
			LINFO($"Rebuilt window renderer ({Size} -> {newSize}) (elapsed = {timer.Elapsed.TotalMilliseconds}ms)", this);
			Size = newSize;
		}
		#endregion // Settings

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
					if (Window is not null) {
						Window.Renderer = null;
					}
				}
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
