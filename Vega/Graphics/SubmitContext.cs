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
	// A pooled object which holds information about a set of submitted commands, and an object to track their
	//    execution state.
	// This is the core type for command buffer re-use on the queue side
	internal unsafe class SubmitContext
	{
		#region Fields
		// The associated queue
		public readonly DeviceQueue Queue;
		
		// The execution fence
		public readonly VkFence Fence;
		// Gets if the execution has completed for the context
		public bool IsFinished => Fence.GetFenceStatus() == VkResult.Success;

		// The set of commands contained in the context
		public IReadOnlyList<CommandBuffer> Commands => _commands;
		private readonly List<CommandBuffer> _commands = new(10);
		#endregion // Fields

		public SubmitContext(DeviceQueue queue)
		{
			Queue = queue;

			VkFenceCreateInfo fci = new(VkFenceCreateFlags.Signaled);
			VulkanHandle<VkFence> handle;
			queue.Graphics.VkDevice.CreateFence(&fci, null, &handle)
				.Throw("Failed to create fence for SubmitContext");
			Fence = new(handle, queue.Graphics.VkDevice);
		}

		public void Destroy() => Fence.DestroyFence(null);

		// Checks if the execution is complete, returns the command buffers if so, returns if complete
		public bool TryRelease()
		{
			if (IsFinished) {
				if (_commands.Count > 0) {
					foreach (var cmd in _commands) {
						cmd.SourcePool.Return(cmd);
					}
					_commands.Clear();
				}
				return true;
			}
			return false;
		}

		// Prepares the context for submission, marking the buffers and resetting the fence
		public void Prepare(IEnumerable<CommandBuffer> buffers)
		{
			if (!IsFinished) {
				throw new InvalidOperationException("Attempt to re-use a pending SubmitContext");
			}
			_commands.Clear();
			_commands.AddRange(buffers);
			var fhandle = Fence.Handle;
			Queue.Graphics.VkDevice.ResetFences(1, &fhandle);
		}

		// Ditto, but for a single command buffer
		public void Prepare(CommandBuffer buffer)
		{
			if (!IsFinished) {
				throw new InvalidOperationException("Attempt to re-use a pending SubmitContext");
			}
			_commands.Clear();
			if (!buffer.Transient) {
				_commands.Add(buffer); // Only track transient buffers
			}
			var fhandle = Fence.Handle;
			Queue.Graphics.VkDevice.ResetFences(1, &fhandle);
		}

		// Ditto, but for single primary + multiple secondaries
		public void Prepare(CommandBuffer buffer, IEnumerable<CommandBuffer> cmds)
		{
			if (!IsFinished) {
				throw new InvalidOperationException("Attempt to re-use a pending SubmitContext");
			}
			_commands.Clear();
			if (!buffer.Transient) {
				_commands.Add(buffer); 
			}
			_commands.AddRange(cmds.Where(cmd => !cmd.Transient));
			var fhandle = Fence.Handle;
			Queue.Graphics.VkDevice.ResetFences(1, &fhandle);
		}
	}
}
