/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vega.Graphics;
using Vulkan;

namespace Vega.Render
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
		public Shader? BoundShader => BoundPipeline?.Shader;
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

		// Uniform data fields
		private ulong _uniformOffset = 0;
		private bool _uniformDirty = false;

		// Bound vertex/index buffer information
		private uint _vertexBufferMask = 0;
		private uint _vertexBufferCount => (uint)BitOperations.PopCount(_vertexBufferMask);
		private bool _boundIndexBuffer = false;
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
			if (pipeline.Shader.Layout.BindingMask != 0) { // Global binding table
				var tableHandle = pipeline.Renderer.Graphics.BindingTable.SetHandle.Handle;
				_cmd.Cmd.CmdBindDescriptorSets(VkPipelineBindPoint.Graphics,
					pipeline.Shader.Layout.PipelineLayout, 0, 1, &tableHandle, 0, null);
			}
			if (pipeline.Shader.Layout.UniformSize > 0) { // Uniform buffer
				var unifHandle = pipeline.Renderer.UniformDescriptor.Handle;
				var zero = 0u;
				_cmd.Cmd.CmdBindDescriptorSets(VkPipelineBindPoint.Graphics,
					pipeline.Shader.Layout.PipelineLayout, 1, 1, &unifHandle, 1, &zero);
			}
			if ((pipeline.Renderer.SubpassLayouts.Length > 0) && 
					(pipeline.Renderer.SubpassLayouts[pipeline.Subpass] is not null)) { // Subpass inputs
				var setHandle = pipeline.Renderer.SubpassDescriptors[pipeline.Subpass]!.Handle;
				_cmd.Cmd.CmdBindDescriptorSets(VkPipelineBindPoint.Graphics,
					pipeline.Shader.Layout.PipelineLayout, 2, 1, &setHandle, 0, null);
			}

			// Setup correct viewport/scissor states
			VkViewport viewport = new(0, 0, pipeline.Renderer.Size.Width, pipeline.Renderer.Size.Height, 0, 1);
			VkRect2D scissor = new(new(0, 0), pipeline.Renderer.Size.AsVkExtent());
			_cmd.Cmd.CmdSetViewport(0, 1, &viewport);
			_cmd.Cmd.CmdSetScissor(0, 1, &scissor);
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

		#region Draw
		/// <summary>
		/// Draw a number of vertices using the bound vertex buffer(s).
		/// </summary>
		/// <param name="vertexCount">The number of vertices to draw.</param>
		/// <param name="firstVertex">The index of the first vertex to draw.</param>
		public void Draw(uint vertexCount, uint firstVertex = 0)
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot draw in a non-recording command recorder");
			}
			if (_vertexBufferCount != BoundPipeline!.VertexBindingCount) {
				throw new InvalidOperationException("Vertex buffers not fully bound in command recorder");
			}

			updateBindings();

			// Record
			_cmd!.Cmd.CmdDraw(vertexCount, 1, firstVertex, 0);
		}

		/// <summary>
		/// Draw multiple instances of a number of vertices using the bound vertex buffer(s).
		/// </summary>
		/// <param name="vertexCount">The number of vertices to draw.</param>
		/// <param name="instanceCount">The number of instances to draw.</param>
		/// <param name="firstVertex">The index of the first vertex to draw.</param>
		/// <param name="firstInstance">The index of the first instance to draw.</param>
		public void DrawInstanced(uint vertexCount, uint instanceCount, uint firstVertex = 0, uint firstInstance = 0)
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot draw in a non-recording command recorder");
			}
			if (_vertexBufferCount != BoundPipeline!.VertexBindingCount) {
				throw new InvalidOperationException("Vertex buffers not fully bound in command recorder");
			}

			updateBindings();

			// Record
			_cmd!.Cmd.CmdDraw(vertexCount, instanceCount, firstVertex, firstInstance);
		}

		/// <summary>
		/// Draw indexed vertices using the bound vertex buffer(s) and index buffer.
		/// </summary>
		/// <param name="indexCount">The number of indexed vertices to draw.</param>
		/// <param name="firstIndex">The index of the first index to draw with.</param>
		/// <param name="vertexOffset">A global offset applied to all index values before vertex lookup.</param>
		public void DrawIndexed(uint indexCount, uint firstIndex = 0, int vertexOffset = 0)
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot draw in a non-recording command recorder");
			}
			if (_vertexBufferCount != BoundPipeline!.VertexBindingCount) {
				throw new InvalidOperationException("Vertex buffers not fully bound in command recorder");
			}
			if (!_boundIndexBuffer) {
				throw new InvalidCastException("Cannot perform indexed drawing without a bound index buffer");
			}

			updateBindings();

			// Record
			_cmd!.Cmd.CmdDrawIndexed(indexCount, 1, firstIndex, vertexOffset, 0);
		}

		/// <summary>
		/// Draw multiple instances of indexed vertices using the bound vertex buffer(s) and index buffer.
		/// </summary>
		/// <param name="indexCount">The number of indexed vertices to draw.</param>
		/// <param name="instanceCount">The number of instances to draw.</param>
		/// <param name="firstIndex">The index of the first index to draw with.</param>
		/// <param name="firstInstance">The index of the first instance to draw with.</param>
		/// <param name="vertexOffset">A global offset applied to all index values before vertex lookup.</param>
		public void DrawInstancedIndexed(uint indexCount, uint instanceCount, uint firstIndex = 0, 
			uint firstInstance = 0, int vertexOffset = 0)
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot draw in a non-recording command recorder");
			}
			if (_vertexBufferCount != BoundPipeline!.VertexBindingCount) {
				throw new InvalidOperationException("Vertex buffers not fully bound in command recorder");
			}
			if (!_boundIndexBuffer) {
				throw new InvalidCastException("Cannot perform indexed drawing without a bound index buffer");
			}

			updateBindings();

			// Record
			_cmd!.Cmd.CmdDrawIndexed(indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
		}
		#endregion // Draw

		#region Vertex/Index
		/// <summary>
		/// Binds the vertex buffer as the source of vertex data for following draw commands.
		/// </summary>
		/// <param name="buffer">The vertex buffer to bind.</param>
		public void BindVertexBuffer(VertexBuffer buffer)
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot bind a vertex buffer in a non-recording command recorder");
			}
			if (buffer.IsDisposed) {
				throw new ObjectDisposedException(nameof(buffer));
			}
			if (BoundPipeline!.VertexBindingCount != 1) {
				throw new InvalidOperationException(
					"Cannot bind a single vertex buffer to pipeline expecting more than one");
			}
			uint mask = buffer.VertexDescription.LocationMask;
			if ((BoundShader!.Layout.VertexLocationMask & mask) != mask) {
				throw new ArgumentException("Invalid vertex buffer bind - attribute location mismatch", nameof(buffer));
			}

			// Bind vertex buffer
			var handle = buffer.Handle.Handle;
			ulong ZERO = 0;
			_cmd!.Cmd.CmdBindVertexBuffers(0, 1, &handle, &ZERO);
			_vertexBufferMask = 0x1;
		}

		/// <summary>
		/// Binds the vertex buffer to the binding given by index.
		/// </summary>
		/// <param name="index">The binding index to bind the vertex buffer to.</param>
		/// <param name="buffer">The vertex buffer to bind.</param>
		public void BindVertexBuffer(uint index, VertexBuffer buffer)
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot bind a vertex buffer in a non-recording command recorder");
			}
			if (buffer.IsDisposed) {
				throw new ObjectDisposedException(nameof(buffer));
			}
			if (index >= BoundPipeline!.VertexBindingCount) {
				throw new InvalidOperationException($"Vertex buffer index {index} is invalid for the bound pipeline");
			}
			uint mask = buffer.VertexDescription.LocationMask;
			if ((BoundShader!.Layout.VertexLocationMask & mask) != mask) {
				throw new ArgumentException("Invalid vertex buffer bind - attribute location mismatch", nameof(buffer));
			}

			// Bind vertex buffer
			var handle = buffer.Handle.Handle;
			ulong ZERO = 0;
			_cmd!.Cmd.CmdBindVertexBuffers(index, 1, &handle, &ZERO);
			_vertexBufferMask |= (1u << (int)index);
		}

		/// <summary>
		/// Binds multiple vertex buffers to the pipeline, starting at binding index 0.
		/// </summary>
		/// <param name="buffers">The buffers to bind.</param>
		public void BindVertexBuffers(params VertexBuffer[] buffers)
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot bind a vertex buffer in a non-recording command recorder");
			}
			if (buffers.Length != BoundPipeline!.VertexBindingCount) {
				throw new InvalidOperationException("Vertex buffer count mismatch for pipeline bind");
			}
			if (buffers.Length == 0) {
				return;
			}

			// Per-buffer validate
			foreach (var buffer in buffers) {
				if (buffer.IsDisposed) {
					throw new ObjectDisposedException(nameof(buffer));
				}
				uint mask = buffer.VertexDescription.LocationMask;
				if ((BoundShader!.Layout.VertexLocationMask & mask) != mask) {
					throw new ArgumentException("Invalid vertex buffer bind - attribute location mismatch", nameof(buffers));
				}
			}

			// Bind the buffers
			var handles = stackalloc VulkanHandle<VkBuffer>[buffers.Length];
			var offsets = stackalloc ulong[buffers.Length];
			for (int i = 0; i < buffers.Length; ++i) {
				handles[i] = buffers[i].Handle.Handle;
				offsets[i] = 0;
				_vertexBufferMask |= (1u << i);
			}
			_cmd!.Cmd.CmdBindVertexBuffers(0, (uint)buffers.Length, handles, offsets);
		}

		/// <summary>
		/// Binds the index buffer as the source of index data for following indexed draw commands.
		/// </summary>
		/// <param name="buffer">The index buffer to bind.</param>
		public void BindIndexBuffer(IndexBuffer buffer)
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot bind a vertex buffer in a non-recording command recorder");
			}
			if (buffer.IsDisposed) {
				throw new ObjectDisposedException(nameof(buffer));
			}

			// Bind index buffer
			var handle = buffer.Handle.Handle;
			var idxt = (buffer.IndexType == IndexType.Short) ? VkIndexType.Uint16 : VkIndexType.Uint32;
			_cmd!.Cmd.CmdBindIndexBuffer(handle, 0, idxt);
			_boundIndexBuffer = true;
		}
		#endregion // Vertex/Index

		#region Data
		/// <summary>
		/// Sets new uniform data to be used in the next draw call in this command recorder. 
		/// <para>
		/// The data must completely specify the full uniform data - partial uniform data updates are not supported.
		/// </para>
		/// </summary>
		/// <param name="data">A pointer to the data to set as the shader uniform.</param>
		public void SetUniformData(void* data)
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot set uniform data on non-recording command recorder");
			}
			if (BoundShader!.Layout.UniformSize == 0) {
				throw new InvalidOperationException("Cannot set uniform data on shader that does not take uniforms");
			}

			// Push data
			if (!BoundRenderer!.PushUniformData(data, BoundShader!.Layout.UniformSize, out _uniformOffset)) {
				throw new InvalidOperationException("Per-frame limit for uniform buffer updates is exceeded");
			}
			_uniformDirty = true;
		}

		/// <summary>
		/// Sets new uniform data to be used in the next draw call in this command recorder. 
		/// <para>
		/// The data must completely specify the full uniform data - partial uniform data updates are not supported.
		/// </para>
		/// <param name="data">The region of data to update the uniform data with.</param>
		public void SetUniformData(in ReadOnlySpan<byte> data)
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot set uniform data on non-recording command recorder");
			}
			if (BoundShader!.Layout.UniformSize == 0) {
				throw new InvalidOperationException("Cannot set uniform data on shader that does not take uniforms");
			}
			if (data.Length < BoundShader!.Layout.UniformSize) {
				throw new ArgumentException("Not enough data in span for SetUniformData()", nameof(data));
			}

			// Push data
			fixed (byte* dataptr = data) {
				if (!BoundRenderer!.PushUniformData(dataptr, BoundShader!.Layout.UniformSize, out _uniformOffset)) {
					throw new InvalidOperationException("Per-frame limit for uniform buffer updates is exceeded");
				}
			}
			_uniformDirty = true;
		}

		/// <summary>
		/// Sets new uniform data to be used in the next draw call in this command recorder. 
		/// <para>
		/// The data must completely specify the full uniform data - partial uniform data updates are not supported.
		/// </para>
		/// <typeparam name="T">The type of the object used to update the uniform data.</typeparam>
		/// <param name="data">The object to update the uniform data value with.</param>
		public void SetUniformData<T>(in T data)
			where T : unmanaged
		{
			// Validate
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot set uniform data on non-recording command recorder");
			}
			if (BoundShader!.Layout.UniformSize == 0) {
				throw new InvalidOperationException("Cannot set uniform data on shader that does not take uniforms");
			}
			if ((uint)Marshal.SizeOf<T>() < BoundShader!.Layout.UniformSize) {
				throw new ArgumentException("Not enough data in generic object for SetUniformData()", nameof(data));
			}

			// Push data
			fixed (T* dataptr = &data) {
				if (!BoundRenderer!.PushUniformData(dataptr, BoundShader!.Layout.UniformSize, out _uniformOffset)) {
					throw new InvalidOperationException("Per-frame limit for uniform buffer updates is exceeded");
				}
			}
			_uniformDirty = true;
		}
		#endregion // Data

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
			texture.ThrowIfDisposed();
			if (!texture.Initialized) {
				throw new ArgumentException("Cannot bind uninitialized texture", nameof(texture));
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
			texture.ThrowIfDisposed();
			if (!texture.Initialized) {
				throw new ArgumentException("Cannot bind uninitialized texture", nameof(texture));
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
			texture.ThrowIfDisposed();
			if (!texture.Initialized) {
				throw new ArgumentException("Cannot bind uninitialized texture", nameof(texture));
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
			if (BoundShader!.Layout.GetBindingType(slot) is not BindingType type) {
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
			_bindingsDirty = false;
			_bindingSize = ((BoundShader!.Layout.MaxBindingSlot + 2) / 2) * 4;

			_vertexBufferMask = 0;
			_boundIndexBuffer = false;
		}

		// Called before a draw command is submitted to update descriptor sets and push new binding indices
		private void updateBindings()
		{
			// Update uniforms
			if (_uniformDirty) {
				var setHandle = BoundRenderer!.UniformDescriptor.Handle;
				var offset = (uint)_uniformOffset;
				// Dynamic offset update is *much* cheaper than a buffer rebinding update
				_cmd!.Cmd.CmdBindDescriptorSets(VkPipelineBindPoint.Graphics,
					BoundShader!.Layout.PipelineLayout, 1, 1, &setHandle, 1, &offset);
				_uniformDirty = false;
			}

			// Push new binding indices
			if (_bindingsDirty) {
				fixed (ushort* bidx = _bindingIndices) {
					_cmd!.Cmd.CmdPushConstants(BoundShader!.Layout.PipelineLayout, 
						VkShaderStageFlags.Vertex | VkShaderStageFlags.Fragment, 0, _bindingSize, bidx);
				}
				_bindingsDirty = false;
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
