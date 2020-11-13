/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	// Represents a command buffer allocated from a per-thread CommandPool
	// Contains the necessary information to return a command buffer to its pool after execution is complete
	internal sealed class CommandBuffer
	{
		public readonly Vk.CommandBuffer Cmd;
		public readonly Vk.CommandBufferLevel Level;
		public readonly CommandPool SourcePool;

		public CommandBuffer(Vk.CommandBuffer cmd, Vk.CommandBufferLevel level, CommandPool src)
		{
			Cmd = cmd;
			Level = level;
			SourcePool = src;
		}
	}
}
