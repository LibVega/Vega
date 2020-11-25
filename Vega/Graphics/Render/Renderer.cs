/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Represents a target (either offscreen or on a window) for rendering commands. Manages rendering state and
	/// command submission.
	/// <para>
	/// Renderer instances targeting windows will cause the window surface buffer swap when ended.
	/// </para>
	/// </summary>
	public unsafe sealed class Renderer : IDisposable
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
		public Extent2D Size => Target.Size;

		/// <summary>
		/// The color used to clear the renderer target image.
		/// </summary>
		public ClearValue ClearValue = new(0f, 0f, 0f, 1f);

		// The render pass
		internal readonly RenderPass Pass;
		// The render target
		internal readonly RenderTarget Target;

		#region Recording
		/// <summary>
		/// Gets if the renderer is currently recording commands.
		/// </summary>
		public bool IsRecording => Cmd is not null;
		
		// The current primary command buffer recording render commands
		private CommandBuffer? Cmd = null;
		#endregion // Recording

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

		#region Recording
		/// <summary>
		/// Begins recording a new set of rendering commands to be submitted to the device.
		/// </summary>
		public void Begin()
		{
			// Check state
			if (IsRecording) {
				throw new InvalidOperationException("Cannot call Renderer.Begin() on a renderer that is recording");
			}

			// Get a new command buffer
			Cmd = Graphics.Resources.AllocateManagedCommandBuffer(VkCommandBufferLevel.Primary);
			VkCommandBufferBeginInfo cbbi = new(
				flags: VkCommandBufferUsageFlags.OneTimeSubmit,
				inheritanceInfo: null
			);
			Cmd.Cmd.BeginCommandBuffer(&cbbi).Throw("Failed to start renderer command recording");

			// Start the render pass
			VkClearValue clear = ClearValue.ToVk();
			VkRenderPassBeginInfo rpbi = new(
				renderPass: Pass.Handle,
				framebuffer: Target.CurrentFramebuffer,
				renderArea: new(default, new(Size.Width, Size.Height)),
				clearValueCount: 1,
				clearValues: &clear
			);
			Cmd.Cmd.CmdBeginRenderPass(&rpbi, VkSubpassContents.SecondaryCommandBuffers);
		}

		/// <summary>
		/// Ends the current command recording process, and submits the commands to be executed. If this renderer is
		/// attached to a window, this also performs a surface swap for the window.
		/// </summary>
		public void End()
		{
			// Check state
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot call Renderer.End() on a renderer that is not recording");
			}

			// End the pass and commands
			Cmd!.Cmd.CmdEndRenderPass();
			Cmd.Cmd.EndCommandBuffer().Throw("Failed to record commands for renderer");

			// Swap buffers (also submits the commands for execution)
			Target.Swap(Cmd);

			// End objects
			Cmd = null;
		}
		#endregion // Recording

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
