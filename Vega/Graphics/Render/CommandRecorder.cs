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
	/// Manages the process of recording graphics commands to be submitted to a <see cref="Renderer"/> instance.
	/// <para>
	/// An instance of this class cannot be shared between threads while recording.
	/// </para>
	/// <para>
	/// While this type implements <see cref="IDisposable"/>, it can be reused after <c>Dispose()</c>, because the
	/// disposal call will only discard the current rendering process, instead of the entire object.
	/// </para>
	/// </summary>
	public unsafe sealed partial class CommandRecorder : IDisposable
	{
		#region Fields
		/// <summary>
		/// The pipeline currently bound to the renderer.
		/// </summary>
		public Pipeline? BoundPipeline { get; private set; }
		/// <summary>
		/// The renderer currently bound to this recorder.
		/// </summary>
		public Renderer? BoundRenderer => BoundPipeline?.Renderer;
		/// <summary>
		/// The subpass index currently bound to this recorder.
		/// </summary>
		public uint? BoundSubpass => BoundPipeline?.Subpass;
		/// <summary>
		/// Gets the value of <see cref="AppTime.FrameCount"/> when the current recording process started.
		/// </summary>
		public ulong? RecordingFrame { get; private set; }

		/// <summary>
		/// Gets if commands are currently being recorded into the recorder.
		/// </summary>
		public bool IsRecording => BoundPipeline is not null;

		#region Binding State
		// The sets of currently bound resources (input attachments are managed internally and not present here)
		private readonly BoundResources _bufferResources = new();
		private readonly BoundResources _samplerResources = new();
		private readonly BoundResources _textureResources = new();
		#endregion // Binding State

		// The current command buffer for recording
		private CommandBuffer? _cmd = null;
		#endregion // Fields

		/// <summary>
		/// Creates a new command recorder
		/// </summary>
		public CommandRecorder()
		{

		}
		~CommandRecorder()
		{
			dispose(false);
		}

		#region Recording State
		/// <summary>
		/// Begins recording a new set of commands for submission to the renderer and subpass matching the passed
		/// pipeline object.
		/// </summary>
		/// <param name="pipeline">The pipeline to bind first for the subsequent rendering commands.</param>
		public void Begin(Pipeline pipeline)
		{
			// Validate
			if (IsRecording) {
				throw new InvalidOperationException("Cannot begin a command recorder that is already recording");
			}

			// Grab available secondard command buffer
			_cmd = Core.Instance!.Graphics.Resources.AllocateTransientCommandBuffer(VkCommandBufferLevel.Secondary);

			// Start a new secondary command buffer
			VkCommandBufferInheritanceInfo cbii = new(
				renderPass: pipeline.Renderer.RenderPass,
				subpass: pipeline.Subpass,
				framebuffer: pipeline.Renderer.RenderTarget.CurrentFramebuffer,
				occlusionQueryEnable: VkBool32.False,
				queryFlags: VkQueryControlFlags.NoFlags,
				pipelineStatistics: VkQueryPipelineStatisticFlags.NoFlags
			);
			VkCommandBufferBeginInfo cbbi = new(
				VkCommandBufferUsageFlags.RenderPassContinue | VkCommandBufferUsageFlags.OneTimeSubmit, &cbii
			);
			_cmd.Cmd.BeginCommandBuffer(&cbbi).Throw("Failed to start recording commands");

			// Bind the pipeline
			_cmd.Cmd.CmdBindPipeline(VkPipelineBindPoint.Graphics, pipeline.Handle);

			// Set values
			BoundPipeline = pipeline;
			RecordingFrame = AppTime.FrameCount;

			// Reset cached recording state
			_bufferResources.Reset();
			_samplerResources.Reset();
			_textureResources.Reset();
		}

		/// <summary>
		/// Completes the current recording process and prepares the recorded commands for submission to
		/// <see cref="BoundRenderer"/>.
		/// </summary>
		/// <returns>The set of recorded commands to submit to the renderer.</returns>
		public RenderTask End()
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot end a command recorder that is not recording");
			}
			if (AppTime.FrameCount != RecordingFrame) {
				// This will not be an issue for non-transient command buffers
				throw new InvalidOperationException("CommandRecorder crossed frame boundary, and is no longer valid");
			}

			// End buffer
			_cmd!.Cmd.EndCommandBuffer().Throw("Failed to record commands");
			RenderTask task = new(BoundRenderer!, BoundSubpass!.Value, _cmd);

			// Set values
			BoundPipeline = null;
			RecordingFrame = null;
			_cmd = null;

			// Return list
			return task;
		}

		/// <summary>
		/// Discards the running list of recorded commands, and returns the recorder to a non-recording state.
		/// </summary>
		public void Discard()
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot discard a command recorder that is not recording");
			}

			// Immediately return non-transient buffers
			if (!_cmd!.Transient) {
				_cmd.SourcePool.Return(_cmd);
			}

			// Set values
			BoundPipeline = null;
			RecordingFrame = null;
			_cmd = null;
		}
		#endregion // Recording State

		#region Resources
		/// <summary>
		/// Sets the pipeline object to use for future rendering commands. The new pipeline must match the renderer
		/// and subpass of the pipeline that <see cref="Begin(Pipeline)"/> was called with.
		/// </summary>
		/// <param name="pipeline">The pipeline to bind for future rendering commands.</param>
		public void BindPipeline(Pipeline pipeline)
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot bind a pipeline to an inactive command recorder");
			}
			if (BoundPipeline!.RUID == pipeline.RUID) {
				return; // Same pipeline object, dont re-bind
			}
			if (!ReferenceEquals(BoundRenderer!, pipeline.Renderer) || (BoundSubpass != pipeline.Subpass)) {
				throw new ArgumentException("Cannot bind incompatible pipeline", nameof(pipeline));
			}

			// Bind the new pipeline
			_cmd!.Cmd.CmdBindPipeline(VkPipelineBindPoint.Graphics, pipeline.Handle);
			BoundPipeline = pipeline;

			// Reset cached recording state
			_bufferResources.Reset();
			_samplerResources.Reset();
			_textureResources.Reset();
		}
		#endregion // Resources

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
		}

		private void dispose(bool disposing)
		{
			if (IsRecording) {
				Discard();
			}
		}
		#endregion // IDisposable
	}
}
