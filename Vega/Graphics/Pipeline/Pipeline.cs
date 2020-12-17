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
	/// Contains a compiled aggregate render state which fully defines how draw commands are processed through to 
	/// fragment outputs.
	/// <para>
	/// This is the core type for defining how rendering occurs in the library. Instances are tied to specific
	/// <see cref="Renderer"/>s, and are automatically rebuilt if the renderer changes.
	/// </para>
	/// </summary>
	public unsafe sealed class Pipeline : ResourceBase
	{
		#region Fields
		/// <summary>
		/// The shader program used by this pipeline.
		/// </summary>
		public readonly Shader Shader;
		/// <summary>
		/// The renderer that this pipeline is utilized within.
		/// </summary>
		public readonly Renderer Renderer;
		/// <summary>
		/// The subpass index that this pipeline is utilized within.
		/// </summary>
		public readonly uint Subpass;

		// The pipeline handle
		internal VkPipeline Handle { get; private set; }
		#endregion // Fields

		/// <summary>
		/// Create a new pipeline with the given description, for use in the given renderer and subpass.
		/// </summary>
		/// <param name="description">The description of the pipeline states to build into the pipeline.</param>
		/// <param name="renderer">The renderer to use the pipeline in.</param>
		/// <param name="subpass">The subpass index within the renderer to use the pipeline in.</param>
		public Pipeline(PipelineDescription description, Renderer renderer, uint subpass)
			: base(ResourceType.Pipeline)
		{
			// Create the pipeline handle
			description.CreatePipeline(renderer, subpass, renderer.MSAA, out var pipeline);
			Handle = pipeline;

			// Assign fields
			Shader = description.Shader!;
			Renderer = renderer;
			Subpass = subpass;
		}

		#region ResourceBase
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
			Handle.DestroyPipeline(null);
		}
		#endregion // ResourceBase
	}
}
