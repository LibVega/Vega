/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.CompilerServices;
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
		/// The shader program current bound to this recorder from the pipeline.
		/// </summary>
		public ShaderProgram? BoundShader => BoundPipeline?.Shader;
		/// <summary>
		/// Gets the value of <see cref="AppTime.FrameCount"/> when the current recording process started.
		/// </summary>
		public ulong? RecordingFrame { get; private set; }

		/// <summary>
		/// Gets if commands are currently being recorded into the recorder.
		/// </summary>
		public bool IsRecording => BoundPipeline is not null;

		// The current command buffer for recording
		private CommandBuffer? _cmd = null;

		// The table indices of the bound resources
		private readonly ushort[] _bindingIndices = new ushort[VSL.MAX_BINDING_COUNT];
		private bool _bindingsDirty = false;
		private uint _bindingSize = 0;
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

			// Prepare for new commands
			resetRenderState();

			// Bind renderer-specific descriptors
			// TODO: Uniform buffer descriptor
			if (pipeline.Renderer.SubpassLayouts[pipeline.Subpass] is not null) { // Subpass inputs
				var setHandle = pipeline.Renderer.SubpassDescriptors[pipeline.Subpass]!.Handle;
				_cmd.Cmd.CmdBindDescriptorSets(VkPipelineBindPoint.Graphics,
					pipeline.Shader.PipelineLayout, 2, 1, &setHandle, 0, null);
			}
		}

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

			// Prepare for new commands
			resetRenderState();
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
		#region Texture Binding
		/// <summary>
		/// Binds a texture and associated sampler to the given slot. The shader binding must be 
		/// <see cref="BindingType.Sampler1D"/>.
		/// </summary>
		/// <param name="slot">The slot to bind the texture/sampler pair to.</param>
		/// <param name="texture">The texture to bind to the slot.</param>
		/// <param name="sampler">The sampler to load texture samples with.</param>
		public void Bind(uint slot, Texture1D texture, Sampler sampler = Sampler.LinearNearestRepeat)
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot bind resource to non-recording command recorder");
			}
			if (texture.IsDisposed) {
				throw new ObjectDisposedException(nameof(texture));
			}

			// Bind
			if (!bindTextureSampler(slot, texture, sampler)) {
				throw new InvalidOperationException(
					$"Failed to bind Texture1D to slot {slot} - unused or invalid slot");
			}
		}

		/// <summary>
		/// Binds a texture and associated sampler to the given slot. The shader binding must be 
		/// <see cref="BindingType.Sampler2D"/>.
		/// </summary>
		/// <param name="slot">The slot to bind the texture/sampler pair to.</param>
		/// <param name="texture">The texture to bind to the slot.</param>
		/// <param name="sampler">The sampler to load texture samples with.</param>
		public void Bind(uint slot, Texture2D texture, Sampler sampler = Sampler.LinearNearestRepeat)
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot bind resource to non-recording command recorder");
			}
			if (texture.IsDisposed) {
				throw new ObjectDisposedException(nameof(texture));
			}

			// Bind
			if (!bindTextureSampler(slot, texture, sampler)) {
				throw new InvalidOperationException(
					$"Failed to bind Texture2D to slot {slot} - unused or invalid slot");
			}
		}

		/// <summary>
		/// Binds a texture and associated sampler to the given slot. The shader binding must be 
		/// <see cref="BindingType.Sampler3D"/>.
		/// </summary>
		/// <param name="slot">The slot to bind the texture/sampler pair to.</param>
		/// <param name="texture">The texture to bind to the slot.</param>
		/// <param name="sampler">The sampler to load texture samples with.</param>
		public void Bind(uint slot, Texture3D texture, Sampler sampler = Sampler.LinearNearestRepeat)
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot bind resource to non-recording command recorder");
			}
			if (texture.IsDisposed) {
				throw new ObjectDisposedException(nameof(texture));
			}

			// Bind
			if (!bindTextureSampler(slot, texture, sampler)) {
				throw new InvalidOperationException(
					$"Failed to bind Texture3D to slot {slot} - unused or invalid slot");
			}
		}

		// Combined image/sampler binding impl
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private bool bindTextureSampler(uint slot, TextureBase tex, Sampler samp)
		{
			// Validate
			if (slot >= VSL.MAX_BINDING_COUNT) {
				return false;
			}
			if (BoundShader!.Info.GetBindingType(slot) is not BindingType type) {
				return false;
			}
			if (type != tex.BindingType) {
				return false;
			}

			// Write index
			_bindingIndices[slot] = tex.EnsureSampler(BoundRenderer!.Graphics, samp);
			_bindingsDirty = true;
			return true;
		}
		#endregion // Texture Binding

		// Resets the cached rendering state
		private void resetRenderState()
		{
			Array.Fill(_bindingIndices, (ushort)0);
			_bindingsDirty = true;
			_bindingSize = ((BoundShader!.Info.MaxBindingSlot + 2) / 2) * 4;
		}

		// Called before a draw command is submitted to update descriptor sets and push new binding indices
		private void updateBindings()
		{
			// Push new binding indices
			if (_bindingsDirty) {
				fixed (ushort* bidx = _bindingIndices) {
					_cmd!.Cmd.CmdPushConstants(BoundShader!.PipelineLayout, 
						VkShaderStageFlags.Vertex | VkShaderStageFlags.Fragment, 0, _bindingSize, bidx);
				}
			}
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
