/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
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
		/// The current MSAA setting for the renderer.
		/// </summary>
		public MSAA MSAA => RenderTarget.MSAA;
		/// <summary>
		/// The number of subpasses in this renderer.
		/// </summary>
		public uint SubpassCount => Layout.SubpassCount;

		/// <summary>
		/// The color used to clear the renderer target image.
		/// </summary>
		public readonly ClearValue[] ClearValues;

		// The render pass objects
		internal readonly RenderLayout Layout;
		internal readonly RenderLayout? MSAALayout;
		internal VkRenderPass RenderPass;
		// The render target
		internal readonly RenderTarget RenderTarget;

		#region Recording
		/// <summary>
		/// Gets if the renderer is currently recording commands.
		/// </summary>
		public bool IsRecording => _cmd is not null;
		/// <summary>
		/// Gets the current subpass being recorded. Returns zero if not recording.
		/// </summary>
		public uint CurrentSubpass { get; private set; } = 0;
		/// <summary>
		/// Gets the value of <see cref="AppTime.FrameCount"/> when the renderer was last ended.
		/// </summary>
		public ulong LastRenderFrame { get; private set; } = 0;
		
		// The current primary command buffer recording render commands
		private CommandBuffer? _cmd = null;
		#endregion // Recording

		#region Pipelines
		// The pipelines that have been created for this renderer
		internal IReadOnlyList<Pipeline> Pipelines => _pipelines;
		private readonly List<Pipeline> _pipelines = new();
		private readonly object _pipelineLock = new();
		#endregion // Pipelines

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
		/// <param name="initialMSAA">The initial MSAA setting for the renderer, if supported.</param>
		public Renderer(Window window, RendererDescription description, MSAA initialMSAA = MSAA.X1)
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
			Layout = new(description, true, false);
			if (description.SupportsMSAA) {
				MSAALayout = new(description, true, true);
			}
			if ((initialMSAA != MSAA.X1) && (MSAALayout is null)) {
				throw new ArgumentException($"Renderer does not support MSAA operations");
			}
			RenderPass = ((initialMSAA == MSAA.X1) ? Layout : MSAALayout!).CreateRenderpass(Graphics, initialMSAA);

			// Create render target
			ClearValues = description.Attachments.Select(att =>
				att.IsColor ? new ClearValue(0f, 0f, 0f, 1f) : new ClearValue(1f, 0)).ToArray();
			RenderTarget = new RenderTarget(this, window, initialMSAA);
		}
		~Renderer()
		{
			dispose(false);
		}

		#region Size/MSAA
		/// <summary>
		/// Sets the size of the images targeted by the renderer. Note that it is an error to do this for window
		/// renderers.
		/// </summary>
		/// <param name="newSize">The new size of the renderer.</param>
		public void SetSize(Extent2D newSize)
		{
			// Skip expensive rebuild
			if (newSize == Size) {
				return;
			}

			// Check for validity
			if (Window is not null) {
				throw new InvalidOperationException("Cannot set the size of a window renderer - it is tied to the window size");
			}

			// TODO: Rebuild at new size once offscreen renderers are implemented
		}

		/// <summary>
		/// Sets the multisample anti-aliasing level of the renderer. If the renderer does not support MSAA operations,
		/// then an exception is thrown.
		/// <para>
		/// Note that this is a very expensive operation, and should be avoided unless necessary.
		/// </para>
		/// </summary>
		/// <param name="msaa">The MSAA level to apply to the renderer.</param>
		public void SetMSAA(MSAA msaa)
		{
			// Skip expensive rebuild
			if (msaa == MSAA) {
				return;
			}

			// Validate
			if ((msaa != MSAA.X1) && (MSAALayout is null)) {
				throw new InvalidOperationException("Cannot enable MSAA operations on a non-MSAA renderer");
			}
			if (!msaa.IsSupported()) {
				throw new ArgumentException($"MSAA level {msaa} is not supported on the current system");
			}

			// Wait for rendering operations to complete before messing with a core object
			Graphics.VkDevice.DeviceWaitIdle();

			// Destroy old MSAA renderpass, then build new one
			RenderPass.DestroyRenderPass(null);
			RenderPass = ((msaa != MSAA.X1) ? MSAALayout! : Layout).CreateRenderpass(Graphics, msaa);

			// Rebuild the render target
			RenderTarget.Rebuild(msaa);

			// Rebuild all associated pipelines
			foreach (var pipeline in _pipelines) {
				pipeline.Rebuild();
			}
		}
		#endregion // Size/MSAA

		#region Recording State
		/// <summary>
		/// Begins recording a new set of rendering commands to be submitted to the device.
		/// </summary>
		public void Begin()
		{
			// Validate
			if (IsRecording) {
				throw new InvalidOperationException("Cannot call Begin() on a renderer that is recording");
			}
			if (LastRenderFrame == AppTime.FrameCount) {
				throw new InvalidOperationException("Cannot call Begin() on a window renderer in the same frame as the last submission");
			}

			// Get a new command buffer (transient works because these can't cross frame boundaries)
			_cmd = Graphics.Resources.AllocateTransientCommandBuffer(VkCommandBufferLevel.Primary);
			VkCommandBufferBeginInfo cbbi = new(
				flags: VkCommandBufferUsageFlags.OneTimeSubmit,
				inheritanceInfo: null
			);
			_cmd.Cmd.BeginCommandBuffer(&cbbi).Throw("Failed to start renderer command recording");

			// Start the render pass
			var clears = stackalloc VkClearValue[ClearValues.Length];
			for (int i = 0; i < ClearValues.Length; ++i) {
				clears[i] = ClearValues[i].ToVk();
			}
			VkRenderPassBeginInfo rpbi = new(
				renderPass: RenderPass,
				framebuffer: RenderTarget.CurrentFramebuffer,
				renderArea: new(default, new(Size.Width, Size.Height)),
				clearValueCount: (uint)ClearValues.Length,
				clearValues: clears
			);
			_cmd.Cmd.CmdBeginRenderPass(&rpbi, VkSubpassContents.SecondaryCommandBuffers);

			// Set values
			CurrentSubpass = 0;
		}

		/// <summary>
		/// Moves the renderer into recording the next subpass.
		/// </summary>
		public void NextSubpass()
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot call NextSubpass() on a renderer that is not recording");
			}
			if (CurrentSubpass == (SubpassCount - 1)) {
				throw new InvalidOperationException("Cannot call NextSubpass() on the last subpass");
			}

			// Move next
			_cmd!.Cmd.CmdNextSubpass(VkSubpassContents.SecondaryCommandBuffers);
			CurrentSubpass += 1;
		}

		/// <summary>
		/// Ends the current command recording process, and submits the commands to be executed. If this renderer is
		/// attached to a window, this also performs a surface swap for the window.
		/// </summary>
		public void End()
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot call End() on a renderer that is not recording");
			}
			if (CurrentSubpass != (SubpassCount - 1)) {
				throw new InvalidOperationException("Cannot call End() on a renderer that has not visited all subpasses");
			}

			// End the pass and commands
			_cmd!.Cmd.CmdEndRenderPass();
			_cmd.Cmd.EndCommandBuffer().Throw("Failed to record commands for renderer");

			// Swap buffers (also submits the commands for execution)
			RenderTarget.Swap(_cmd);

			// End objects
			_cmd = null;
			CurrentSubpass = 0;
			LastRenderFrame = AppTime.FrameCount;
		}
		#endregion // Recording State

		#region Commands
		/// <summary>
		/// Submits the given command list to be executed at the current recording location of the renderer. The
		/// command list is invalidated and cannot be reused after this call.
		/// </summary>
		/// <param name="task">The list of commands to execute.</param>
		public void Submit(RenderTask task)
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot call Submit() on a renderer that is not recording");
			}
			if (!task.IsValid) {
				throw new InvalidOperationException("Cannot submit an invalid RenderTask to a renderer");
			}
			if (!ReferenceEquals(task.Renderer, this)) {
				throw new InvalidOperationException("Cannot submit a RenderTask to the renderer it was not recorded for");
			}
			if (task.Subpass != CurrentSubpass) {
				throw new InvalidOperationException("Cannot submit a RenderTask in a subpass it was not recorded for");
			}

			// Submit
			var handle = task.Buffer!.Cmd.Handle;
			_cmd!.Cmd.CmdExecuteCommands(1, &handle);
			task.Invalidate();
		}

		/// <summary>
		/// Submits the given set of command lists to be executed at the current recording location of the renderer.
		/// All commands lists will be invalidated and cannot be reused after this call.
		/// <para>
		/// The submited command lists will be executed in the order they are given.
		/// </para>
		/// </summary>
		/// <param name="tasks">The set of render tasks to submit.</param>
		public void Submit(params RenderTask[] tasks)
		{
			// Validate
			if (tasks.Length == 0) {
				return;
			}
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot call Submit() on a renderer that is not recording");
			}
			foreach (var list in tasks) {
				if (!list.IsValid) {
					throw new InvalidOperationException("Cannot submit an invalid RenderTask to a renderer");
				}
				if (!ReferenceEquals(list.Renderer, this)) {
					throw new InvalidOperationException("Cannot submit a RenderTask to the renderer it was not recorded for");
				}
				if (list.Subpass != CurrentSubpass) {
					throw new InvalidOperationException("Cannot submit a RenderTask in a subpass it was not recorded for");
				}
			}

			// Submit
			var handles = stackalloc VulkanHandle<VkCommandBuffer>[tasks.Length];
			for (int i = 0; i < tasks.Length; ++i) {
				handles[i] = tasks[i].Buffer!.Cmd.Handle;
				tasks[i].Invalidate();
			}
			_cmd!.Cmd.CmdExecuteCommands((uint)tasks.Length, handles);
		}
		#endregion // Commands

		// Called by the connected swapchain (if any) when it resizes
		// The swapchain will have already waited for device idle at this point
		internal void OnSwapchainResize()
		{
			RenderTarget.Rebuild(MSAA);
		}

		#region Pipelines
		// Adds a new pipeline to be tracked by this renderer
		internal void AddPipeline(Pipeline pipeline)
		{
			if (!ReferenceEquals(this, pipeline.Renderer)) {
				throw new ArgumentException("LIBRARY BUG - renderer instance mismatch for pipeline", nameof(pipeline));
			}

			lock (_pipelineLock) {
				_pipelines.Add(pipeline);
			}
		}

		// Removes the pipeline from being tracked and managed by this renderer
		internal void RemovePipeline(Pipeline pipeline)
		{
			if (!ReferenceEquals(this, pipeline.Renderer)) {
				throw new ArgumentException("LIBRARY BUG - renderer instance mismatch for pipeline", nameof(pipeline));
			}

			lock (_pipelineLock) {
				_pipelines.Remove(pipeline);
			}
		}
		#endregion // Pipelines

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

					while (_pipelines.Count > 0) {
						_pipelines[^1].Dispose(); // Removes this pipeline from the list
					}
				}
				RenderPass.DestroyRenderPass(null);
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
