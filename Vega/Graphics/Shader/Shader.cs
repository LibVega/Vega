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
	/// Represets a specific set of SPIR-V shader stages as a complete shader program.
	/// </summary>
	public unsafe sealed class Shader : ResourceBase
	{
		#region Fields
		/// <summary>
		/// The shader stages that are present in the program.
		/// </summary>
		public readonly ShaderStages Stages;

		// Shader modules
		internal readonly VkShaderModule? VertexModule;
		internal readonly VkShaderModule? TessControlModule;
		internal readonly VkShaderModule? TessEvalModule;
		internal readonly VkShaderModule? GeometryModule;
		internal readonly VkShaderModule? FragmentModule;
		#endregion // Fields

		public Shader()
			: base(ResourceType.Shader)
		{

		}

		#region IDisposable
		protected override void OnDispose(bool disposing)
		{
			if (Core.Instance is not null) {
				Core.Instance.Graphics.Resources.QueueDestroy(this);
			}
			else {
				Destroy();
			}
		}

		protected internal override void Destroy()
		{
			VertexModule?.DestroyShaderModule(null);
			TessControlModule?.DestroyShaderModule(null);
			TessEvalModule?.DestroyShaderModule(null);
			GeometryModule?.DestroyShaderModule(null);
			FragmentModule?.DestroyShaderModule(null);
		}
		#endregion // IDisposable
	}
}
