/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vk.Extras;
using static Vega.InternalLog;

namespace Vega.Graphics
{
	/// <summary>
	/// Represents a target (either an offscreen image or a window) for rendering commands. Manages the rendering
	/// state and command submission for the target. 
	/// <para>
	/// A renderer can only be used at most once per frame, and window renderers <em>must</em> be used <em>exactly 
	/// once</em> per frame.
	/// </para>
	/// </summary>
	public unsafe sealed class Renderer : IDisposable
	{
		#region Fields
		// The renderpass and framebuffers for this renderer
		internal readonly RenderPass RenderPass;
		/// <summary>
		/// The number of subpasses for the renderer.
		/// </summary>
		public int SubpassCount => RenderPass.SubpassCount;
		// Gets the current msaa-aware render pass object
		internal Vk.RenderPass CurrentRenderPassHandle =>
			(MSAA != MSAA.X1) ? RenderPass.MSAAHandle : RenderPass.Handle;

		/// <summary>
		/// The associated graphics service.
		/// </summary>
		public readonly GraphicsService Graphics;
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
		/// The values used to clear the renderer attachments.
		/// </summary>
		public readonly ClearValue[] ClearValues;

		#region Render Pass Data
		/// <summary>
		/// The current renderer subpass index, or <c>null</c> if not recording.
		/// </summary>
		public uint? PassIndex { get; private set; } = null;
		/// <summary>
		/// Gets if the renderer is currently recording.
		/// </summary>
		public bool IsRecording => PassIndex.HasValue;
		/// <summary>
		/// The value of <see cref="AppTime.FrameCount"/> when the renderer was last submitted.
		/// </summary>
		public ulong LastEndFrame { get; private set; } = 0;
		// The frame index for the last Start() call
		private ulong _startFrame = 0;

		// Command buffer for current recording
		private CommandBuffer? _cmd = null;
		// The fence used to submit the last render command
		internal Vk.Fence LastRenderFence = Vk.Fence.Null;

		// The running set of command lists to invalidate and submit at End()
		private readonly List<CommandList> _ownedLists = new();

		// The running set of secondary command buffers to submit at End()
		private readonly List<CommandBuffer> _secondaryBuffers = new();
		#endregion // Render Pass Data

		/// <summary>
		/// Gets if the renderer has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		/// <summary>
		/// <para>
		/// <em>WARNING: NOT YET IMPLEMENTED</em>
		/// </para>
		/// Creates a new Renderer for submitting draw commands to an offscreen buffer. Offscreen Renderer instances
		/// must be manually rebuilt to change the size or anti-aliasing attributes.
		/// </summary>
		/// <param name="desc">The valid description of the renderer.</param>
		/// <param name="size">The initial size of the renderer buffer, cannot be (0, 0).</param>
		/// <param name="msaa">The initial MSAA of the renderer, if supported.</param>
		public Renderer(RendererDescription desc, Extent2D size, MSAA msaa = MSAA.X1)
		{
			throw new NotImplementedException("Offscreen renderers are not yet fully implemented");

			// Validate size
#pragma warning disable CS0162 // Unreachable code detected
			if (size.Area == 0) {
				throw new ArgumentException("Cannot use a zero size for a renderer", nameof(size));
			}
			Graphics = Core.Instance!.Graphics;
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
			ClearValues = new ClearValue[RenderPass.NonResolveCount];
#pragma warning restore CS0162 // Unreachable code detected
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
			Graphics = Core.Instance!.Graphics;
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
			ClearValues = new ClearValue[RenderPass.NonResolveCount];

			// Assign as official window renderer
			Window.Renderer = this;
		}
		~Renderer()
		{
			dispose(false);
		}

		#region Begin/End
		/// <summary>
		/// Starts recording commands to the renderer. This cannot be called if already recording. Window renderers
		/// cannot be started twice in a single frame.
		/// </summary>
		public void Begin()
		{
			// Validate
			if (PassIndex.HasValue) {
				throw new InvalidOperationException("Cannot call Begin() on a Renderer that is already recording");
			}
			if ((Window is not null) && (LastEndFrame == AppTime.FrameCount)) {
				throw new InvalidOperationException("Cannot call Begin() on a window Renderer more than once per frame");
			}

			// Start command buffer
			_cmd = Graphics.Resources.AllocatePrimaryCommandBuffer();
			Vk.CommandBufferBeginInfo cbbi = new(Vk.CommandBufferUsageFlags.OneTimeSubmit, null);
			_cmd.Cmd.BeginCommandBuffer(&cbbi);

			// Start render pass
			var clears = stackalloc Vk.ClearValue[RenderPass.NonResolveCount];
			for (int i = 0; i < RenderPass.NonResolveCount; ++i) {
				clears[i] = ClearValues[i].ToVk();
			}
			Vk.RenderPassBeginInfo rpbi = new(
				renderPass: CurrentRenderPassHandle,
				framebuffer: RenderPass.CurrentFramebuffer,
				renderArea: new(new(0, 0), new(Size.Width, Size.Height)),
				clearValueCount: (uint)RenderPass.NonResolveCount,
				clearValues: clears
			);
			_cmd.Cmd.BeginRenderPass(&rpbi, Vk.SubpassContents.SecondaryCommandBuffers);

			// Set values
			_startFrame = AppTime.FrameCount;
			PassIndex = 0;
		}

		/// <summary>
		/// Moves the renderer to the next subpass, performing transitions and resolve operations as needed.
		/// </summary>
		public void NextSubpass()
		{
			// Validate
			if (!PassIndex.HasValue) {
				throw new InvalidOperationException("Cannot call NextSubpass() on Renderer that is not recording");
			}
			if (PassIndex.Value == (RenderPass.SubpassCount - 1)) {
				throw new InvalidOperationException("NextSubpass() called on renderer on final subpass");
			}

			// Next subpass command
			_cmd!.Cmd.NextSubpass(Vk.SubpassContents.SecondaryCommandBuffers);
			PassIndex += 1;
		}

		/// <summary>
		/// Ends command recording, and submits the recorded commands to the device for processing. For window
		/// renderers, this must be called in the same frame as <see cref="Begin"/>.
		/// </summary>
		public void End()
		{
			// Validate
			if (!PassIndex.HasValue) {
				throw new InvalidOperationException("Cannot call End() on a Renderer that is not recording");
			}
			if (PassIndex.Value != (RenderPass.SubpassCount - 1)) {
				throw new InvalidOperationException("Cannot end a renderer without moving through all subpasses");
			}

			// End command buffer
			_cmd!.Cmd.EndRenderPass();
			_cmd.Cmd.EndCommandBuffer().Throw("Failed to build renderer command buffer");

			// Wait for the last render task
			if (LastRenderFence) {
				var fhandle = LastRenderFence.Handle;
				Graphics.Device.WaitForFences(1, &fhandle, Vk.Bool32.True, UInt64.MaxValue);
			}

			// Submit
			if (Window is null) {
				LastRenderFence = Graphics.GraphicsQueue.Submit(_cmd, _secondaryBuffers);
			}
			else {
				LastRenderFence = Graphics.GraphicsQueue.Submit(_cmd, _secondaryBuffers,
					Window.Swapchain.CurrentAcquireSemaphore, Vk.PipelineStageFlags.ColorAttachmentOutput,
					Window.Swapchain.CurrentRenderSemaphore);
			}
			_secondaryBuffers.Clear();

			// Invalidate lists
			foreach (var list in _ownedLists) {
				list.Invalidate();
			}
			_ownedLists.Clear();

			// Set values
			_cmd = null;
			LastEndFrame = AppTime.FrameCount;
			PassIndex = null;
		}
		#endregion // Begin/End

		#region Commands
		// Adds the given list to the set of tracked lists
		internal void TrackList(CommandList list)
		{
			_ownedLists.Add(list);
			_secondaryBuffers.Add(list.Buffer!);
		}

		/// <summary>
		/// Submits the given command list to be executed at the current recoding location of the renderer.
		/// </summary>
		/// <param name="list">The list of commands to execute.</param>
		public void Submit(CommandList list)
		{
			// Validate state
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot submit a command list to a renderer that is not recording");
			}
			if (!list.IsValid) {
				throw new InvalidOperationException("Cannot submit an invalidated command list to a renderer");
			}
			if (!ReferenceEquals(list.Renderer, this)) {
				throw new InvalidOperationException("Cannot submit a command list to a non-matching renderer");
			}
			if (list.Subpass != PassIndex!.Value) {
				throw new InvalidOperationException("Cannot submit a command list outside of its expected subpass");
			}

			// Submit commands
			var handle = list.Buffer!.Cmd.Handle;
			_cmd!.Cmd.ExecuteCommands(1, &handle);
		}
		#endregion // Commands

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
