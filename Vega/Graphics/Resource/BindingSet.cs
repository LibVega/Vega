/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	// Contains an allocation result from a BindingPool object, for use in command recorders
	internal struct BindingSet
	{
		#region Fields
		// The handle to the binding set object
		public readonly VulkanHandle<VkDescriptorSet> Handle;
		// The frame index in which the binding set was generated
		public readonly ulong FrameIndex;
		#endregion // Fields

		public BindingSet(VulkanHandle<VkDescriptorSet> handle, ulong frame)
		{
			Handle = handle;
			FrameIndex = frame;
		}
	}
}
