﻿/*
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
		public Extent2D Size => RenderTarget.Size;

		/// <summary>
		/// The color used to clear the renderer target image.
		/// </summary>
		public ClearValue ClearValue = new(0f, 0f, 0f, 1f);

		// The render pass objects
		internal readonly RenderLayout Layout;
		internal readonly VkRenderPass RenderPass;
		// The render target
		internal readonly RenderTarget RenderTarget;

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
		/// <param name="description">A description of the pass layout and attachments for the renderer.</param>
		public Renderer(Window window, RendererDescription description)
		{
			// Validate
			if (window.HasRenderer) {
				throw new InvalidOperationException("Cannot create more than one renderer for a single window");
			}

			// Set objects
			Graphics = Core.Instance!.Graphics;
			Window = window;

			// Validate and create layout and pass
			if (description.Attachments[0].Format != window.SurfaceFormat) {
				throw new ArgumentException("The given renderer description does not match the window surface format");
			}
			if (!description.Attachments[0].Preserve) {
				throw new ArgumentException("Attachment 0 must be preserved in window renderers");
			}
			Layout = new(description, true);
			RenderPass = Layout.CreateRenderpass(Graphics);

			// Create render target
			RenderTarget = new RenderTarget(this, window);
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

			// Get a new command buffer (transient works because these can't cross frame boundaries)
			Cmd = Graphics.Resources.AllocateTransientCommandBuffer(VkCommandBufferLevel.Primary);
			VkCommandBufferBeginInfo cbbi = new(
				flags: VkCommandBufferUsageFlags.OneTimeSubmit,
				inheritanceInfo: null
			);
			Cmd.Cmd.BeginCommandBuffer(&cbbi).Throw("Failed to start renderer command recording");

			// Start the render pass
			VkClearValue clear = ClearValue.ToVk();
			VkRenderPassBeginInfo rpbi = new(
				renderPass: RenderPass,
				framebuffer: RenderTarget.CurrentFramebuffer,
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
			RenderTarget.Swap(Cmd);

			// End objects
			Cmd = null;
		}
		#endregion // Recording

		// Called by the connected swapchain (if any) when it resizes
		// The swapchain will have already waited for device idle at this point
		internal void OnSwapchainResize()
		{
			RenderTarget.Rebuild();
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
					RenderTarget.Dispose();
				}
				RenderPass.DestroyRenderPass(null);
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
