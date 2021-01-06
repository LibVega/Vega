/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	// Contains the builtin objects used by shaders
	public unsafe sealed partial class Shader
	{
		internal static class Builtin
		{
			#region Fields
			// The descriptor set layout used to indicate an empty set
			public static VkDescriptorSetLayout? EmptyDescriptorSetLayout { get; private set; }
			#endregion // Fields

			public static void Initialize(GraphicsDevice gd)
			{
				// Create empty descriptor set
				VkDescriptorSetLayoutCreateInfo dslci = new(
					flags: VkDescriptorSetLayoutCreateFlags.NoFlags,
					bindingCount: 0,
					bindings: null
				);
				VulkanHandle<VkDescriptorSetLayout> layoutHandle;
				gd.VkDevice.CreateDescriptorSetLayout(&dslci, null, &layoutHandle)
					.Throw("Failed to create builtin empty descriptor set layout");
				EmptyDescriptorSetLayout = new(layoutHandle, gd.VkDevice);
			}

			public static void Cleanup(GraphicsDevice gd)
			{
				// Destroy set layout
				EmptyDescriptorSetLayout?.DestroyDescriptorSetLayout(null);
				EmptyDescriptorSetLayout = null;
			}
		}
	}
}
