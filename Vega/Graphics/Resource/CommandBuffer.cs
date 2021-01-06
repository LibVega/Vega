/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	// Represents a command buffer allocated from a per-thread CommandPool
	// Contains the necessary information to return a command buffer to its pool after execution is complete
	internal sealed class CommandBuffer
	{
		public readonly VkCommandBuffer Cmd;
		public readonly VkCommandBufferLevel Level;
		public readonly CommandPool SourcePool;
		public readonly bool Transient;

		public CommandBuffer(VkCommandBuffer cmd, VkCommandBufferLevel level, CommandPool src, bool transient)
		{
			Cmd = cmd;
			Level = level;
			SourcePool = src;
			Transient = transient;
		}
	}
}
