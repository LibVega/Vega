/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// Represents a target (either offscreen or on a window) for rendering commands. Manages rendering state and
	/// command submission.
	/// <para>
	/// Renderer instances targeting windows will cause the window surface buffer swap when ended.
	/// </para>
	/// </summary>
	public sealed class Renderer : IDisposable
	{
		#region Fields

		// The graphics device 
		internal readonly GraphicsDevice Graphics;
		/// <summary>
		/// The window associated with the renderer.
		/// </summary>
		public readonly Window Window;
		/// <summary>
		/// If the renderer is targeting an offscreen image.
		/// </summary>
		public bool IsOffscreen => Window is null;

		/// <summary>
		/// The current size of the images being rendered to.
		/// </summary>
		public Extent2D Size => Window.Size;

		// The render pass
		internal readonly RenderPass Pass;
		// The render target
		internal readonly RenderTarget Target;

		/// <summary>
		/// Disposal flag.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		/// <summary>
		/// Creates a new renderer targeting the passed window. It is an error to create more than one renderer for
		/// a single window.
		/// </summary>
		/// <param name="window">The window to target with the renderer.</param>
		public Renderer(Window window)
		{
			// Validate
			if (window.HasRenderer) {
				throw new InvalidOperationException("Cannot create more than one renderer for a single window");
			}

			// Set objects
			Graphics = Core.Instance!.Graphics;
			Window = window;

			// Create pass and target
			Pass = new(this, (TexelFormat)window.Swapchain.SurfaceFormat);
			Target = new RenderTarget(this, window);
		}
		~Renderer()
		{
			dispose(false);
		}

		// Called by the connected swapchain (if any) when it resizes
		// The swapchain will have already waited for device idle at this point
		internal void OnSwapchainResize()
		{
			Target.Rebuild();
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
					Graphics.VkDevice.DeviceWaitIdle();

					Target.Dispose();
					Pass.Dispose();
				}
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
