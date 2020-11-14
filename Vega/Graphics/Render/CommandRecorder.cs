/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vk.Extras;

namespace Vega.Graphics
{
	/// <summary>
	/// Manages the process of recording render commands to be submitted to the <see cref="Renderer"/> instance.
	/// </summary>
	public unsafe sealed class CommandRecorder : IDisposable
	{
		#region Fields
		#region Current
		/// <summary>
		/// The renderer instance that this recorder builds command lists for.
		/// </summary>
		public readonly Renderer Renderer;
		/// <summary>
		/// The renderer subpass index that the current recording process is for.
		/// </summary>
		public uint? BoundSubpass { get; private set; } = null;
		/// <summary>
		/// Gets if the command list is currently recording commands.
		/// </summary>
		public bool IsRecording => _cmd is not null;

		// The currently bound command buffer to record into
		private CommandBuffer? _cmd = null;
		#endregion // Current

		/// <summary>
		/// Gets if this command list has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		/// <summary>
		/// Creates a new recorder in an initial ready and non-recording state.
		/// </summary>
		/// <param name="renderer">The renderer instance that this recorder will build commands for.</param>
		public CommandRecorder(Renderer renderer)
		{
			Renderer = renderer;
		}
		~CommandRecorder()
		{
			dispose(false);
		}

		#region Begin/End/Discard
		/// <summary>
		/// Begins recording a new set of commands for the given subpass.
		/// </summary>
		/// <param name="subpass">The subpass index that the recorded commands will be submitted to.</param>
		public void Begin(uint subpass)
		{
			// Validate state
			if (IsDisposed) throw new ObjectDisposedException(nameof(CommandRecorder));
			if (IsRecording) {
				throw new InvalidOperationException("Cannot call Begin() on a command recorder that is recording");
			}
			if (subpass >= Renderer.SubpassCount) {
				throw new InvalidOperationException("Invalid subpass index for given renderer");
			}

			// Get secondary command buffer
			_cmd = Renderer.Graphics.Resources.AllocateSecondaryCommandBuffer();

			// Start new command buffer
			Vk.CommandBufferInheritanceInfo cbii = new(
				renderPass: Renderer.CurrentRenderPassHandle,
				subpass: subpass,
				framebuffer: Renderer.RenderPass.CurrentFramebuffer,
				occlusionQueryEnable: Vk.Bool32.False,
				queryFlags: Vk.QueryControlFlags.NoFlags,
				pipelineStatistics: Vk.QueryPipelineStatisticFlags.NoFlags
			);
			Vk.CommandBufferBeginInfo cbbi = new(Vk.CommandBufferUsageFlags.RenderPassContinue, &cbii);
			_cmd.Cmd.BeginCommandBuffer(&cbbi);

			// Set values
			BoundSubpass = subpass;
		}

		/// <summary>
		/// Completes the current recording process and returns the recorded commands as a <see cref="CommandList"/>
		/// for submission to a <see cref="Renderer"/> instance.
		/// </summary>
		/// <returns>The recorded commands as a submittable list.</returns>
		public CommandList End()
		{
			// Validate state
			if (IsDisposed) throw new ObjectDisposedException(nameof(CommandRecorder));
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot call End() on a command recorder that is not recording");
			}

			// End the command buffer and create the list (TODO: pool the command list objects)
			_cmd!.Cmd.EndCommandBuffer().Throw("Failed to build command buffer");
			var list = new CommandList(Renderer, BoundSubpass!.Value, _cmd);

			// Set values
			BoundSubpass = null;
			_cmd = null;

			// Return
			return list;
		}

		/// <summary>
		/// Ends the current recording operation and discards the working command list.
		/// </summary>
		public void Discard()
		{
			// Validate state
			if (IsDisposed) throw new ObjectDisposedException(nameof(CommandRecorder));
			if (!IsRecording) {
				throw new InvalidOperationException("Cannot call Discard() on a command recorder that is not recording");
			}

			// Immediately return the pending buffer to its pool for reuse
			_cmd!.Cmd.EndCommandBuffer();
			_cmd.SourcePool.Return(_cmd);

			// Set values
			BoundSubpass = null;
			_cmd = null;
		}
		#endregion // Begin/End/Discard

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed) {
				if (IsRecording) {
					Discard();
				}
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
