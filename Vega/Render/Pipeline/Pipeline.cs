/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Linq;
using Vega.Graphics;
using Vulkan;

namespace Vega.Render
{
	/// <summary>
	/// Contains a compiled aggregate render state which fully defines how draw commands are processed through the
	/// graphics processing pipeline.
	/// <para>
	/// This is the core type for defining how rendering occurs in the library. Instances are tied to specific
	/// <see cref="Graphics.Renderer"/>s, and are automatically rebuilt if the renderer changes.
	/// </para>
	/// <para>
	/// Pipeline lifetimes are managed by their parent renderers. They can still be manually disposed, but will also
	/// be destroyed alongside their parent renderer.
	/// </para>
	/// </summary>
	public unsafe sealed class Pipeline : ResourceBase
	{
		#region Fields
		/// <summary>
		/// The shader program used by this pipeline.
		/// </summary>
		public readonly ShaderProgram Shader;
		/// <summary>
		/// The renderer that this pipeline is utilized within.
		/// </summary>
		public readonly Renderer Renderer;
		/// <summary>
		/// The subpass index that this pipeline is utilized within.
		/// </summary>
		public readonly uint Subpass;

		/// <summary>
		/// The expected number of vertex bindings for this pipeline.
		/// </summary>
		public readonly uint VertexBindingCount;

		// The pipeline handle
		internal VkPipeline Handle { get; private set; }
		// The cached build state for pipeline rebuilds
		internal readonly BuildStates? BuildCache = null;
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
			Handle = CreatePipeline(description, renderer, subpass, out var cache);
			if (renderer.MSAALayout is not null) {
				BuildCache = cache; // Only save cache if we might rebuild from MSAA change
			}

			// Assign fields
			Shader = description.Shader!;
			Shader.IncRef();
			Renderer = renderer;
			Subpass = subpass;

			// Assign values
			VertexBindingCount = (uint)cache.VertexBindings.Length;

			// Register to the renderer
			renderer.AddPipeline(this);
		}

		// Called by the parent renderer when the MSAA changes
		internal void Rebuild()
		{
			// Destroy old handle
			Handle?.DestroyPipeline(null);

			// Create new handle
			Handle = CreatePipeline(BuildCache!, Renderer, Subpass, Shader);
		}

		#region ResourceBase
		protected override void OnDispose(bool disposing)
		{
			Shader?.DecRef();

			if (Core.Instance is not null) {
				Core.Instance.Graphics.Resources.QueueDestroy(this);

				if (disposing && !Renderer.IsDisposed) {
					Renderer.RemovePipeline(this);
				}
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

		#region Creation
		internal static VkPipeline CreatePipeline(PipelineDescription desc, Renderer renderer, uint subpass, 
			out BuildStates buildState)
		{
			// Validate
			var rlayout = (renderer.MSAA != MSAA.X1) ? renderer.MSAALayout! : renderer.Layout;
			if (!desc.IsComplete) {
				throw new InvalidOperationException("Cannot create a pipeline from an incomplete description");
			}
			var cacnt = rlayout.Subpasses[subpass].ColorCount;
			if (desc.AllColorBlends!.Length != 1 && (cacnt != desc.AllColorBlends.Length)) {
				throw new InvalidOperationException("Invalid color blend count for pipeline");
			}
			var hasds = rlayout.Subpasses[subpass].DepthOffset.HasValue;
			if (!hasds && (desc.DepthStencil!.Value.DepthMode != DepthMode.None)) {
				throw new InvalidOperationException(
					"Cannot perform depth/stencil operations on non-depth/stencil subpass");
			}
			if (desc.Shader!.CheckCompatiblity(desc, renderer, subpass) is string shaderErr) {
				throw new InvalidOperationException($"Invalid shader for pipeline - {shaderErr}");
			}
				
			// Describe the color blends
			var cblends = (desc.AllColorBlends.Length != 1)
				? desc.AllColorBlends!
				: Enumerable.Repeat((ColorBlendState)desc.SharedColorBlend!, (int)cacnt).ToArray();

			// Vertex info
			CalculateVertexInfo(desc.VertexDescriptions!, out var vertexAttrs, out var vertexBinds);

			// Create the build states
			buildState = new(
				cblends,
				desc.BlendConstants,
				(DepthStencilState)desc.DepthStencil!,
				(VertexInput)desc.VertexInput!,
				vertexBinds,
				vertexAttrs,
				(RasterizerState)desc.Rasterizer!
			);

			// Create the initial pipeline
			return CreatePipeline(buildState, renderer, subpass, desc.Shader!);
		}

		// Create a pipeline from a set of build states and a shader
		private static VkPipeline CreatePipeline(BuildStates states, Renderer renderer, uint subpass,
			ShaderProgram shader)
		{
			var MAIN_STR = stackalloc byte[5] { (byte)'m', (byte)'a', (byte)'i', (byte)'n', (byte)'\0' };

			// Color blends
			var cblends = stackalloc VkPipelineColorBlendAttachmentState[states.ColorBlends.Length];
			for (int i = 0; i < states.ColorBlends.Length; ++i) {
				states.ColorBlends[i].ToVk(out cblends[i]);
			}
			VkPipelineColorBlendStateCreateInfo colorBlendCI = new(
				flags: VkPipelineColorBlendStateCreateFlags.NoFlags,
				logicOpEnable: false, // TODO: Maybe enable this in the future
				logicOp: VkLogicOp.Clear,
				attachmentCount: (uint)states.ColorBlends.Length,
				attachments: cblends,
				blendConstants_0: states.BlendConstants.R,
				blendConstants_1: states.BlendConstants.G,
				blendConstants_2: states.BlendConstants.B,
				blendConstants_3: states.BlendConstants.A
			);

			// Inferred state objects
			VkPipelineMultisampleStateCreateInfo msaaCI = new(
				flags: VkPipelineMultisampleStateCreateFlags.NoFlags,
				rasterizationSamples: (VkSampleCountFlags)renderer.MSAA,
				sampleShadingEnable: false, // TODO: Allow sample shading
				minSampleShading: 0,
				sampleMask: null,
				alphaToCoverageEnable: false,
				alphaToOneEnable: false
			);

			// Constant state objects
			var dynstates = stackalloc VkDynamicState[2] { VkDynamicState.Viewport, VkDynamicState.Scissor };
			VkViewport viewport = new();
			VkRect2D scissor = new();
			VkPipelineViewportStateCreateInfo viewportCI = new( // Dummy value b/c dynamic state
				flags: VkPipelineViewportStateCreateFlags.NoFlags,
				viewportCount: 1,
				viewports: &viewport,
				scissorCount: 1,
				scissors: &scissor
			);
			VkPipelineDynamicStateCreateInfo dynamicCI = new(
				flags: VkPipelineDynamicStateCreateFlags.NoFlags,
				dynamicStateCount: 2,
				dynamicStates: dynstates
			);

			// Shader create info
			var stageCIs = shader.EnumerateModules().Select(mod => new VkPipelineShaderStageCreateInfo(
				flags: VkPipelineShaderStageCreateFlags.NoFlags,
				stage: (VkShaderStageFlags)mod.stage,
				module: mod.mod,
				name: MAIN_STR,
				specializationInfo: null // TODO: Public API for specialization
			)).ToArray();
			VkPipelineTessellationStateCreateInfo.New(out var tessCI);

			// State cached objects
			states.DepthStencil.ToVk(out var depthStencilCI);
			states.VertexInput.ToVk(out var vertexInputCI);
			states.RasterizerState.ToVk(out var rasterCI);

			// Create the pipeline
			fixed (VkPipelineShaderStageCreateInfo* stagePtr = stageCIs)
			fixed (VkVertexInputAttributeDescription* attributePtr = states.VertexAttributes)
			fixed (VkVertexInputBindingDescription* bindingPtr = states.VertexBindings) {
				// Additional create objects
				VkPipelineVertexInputStateCreateInfo vertexCI = new(
					flags: VkPipelineVertexInputStateCreateFlags.NoFlags,
					vertexBindingDescriptionCount: (uint)states.VertexBindings.Length,
					vertexBindingDescriptions: bindingPtr,
					vertexAttributeDescriptionCount: (uint)states.VertexAttributes.Length,
					vertexAttributeDescriptions: attributePtr
				);

				// Create info
				VkGraphicsPipelineCreateInfo ci = new(
					flags: VkPipelineCreateFlags.NoFlags, // TODO: see if we can utilize some of the flags
					stageCount: (uint)stageCIs.Length,
					stages: stagePtr,
					vertexInputState: &vertexCI,
					inputAssemblyState: &vertexInputCI,
					tessellationState: &tessCI,
					viewportState: &viewportCI,
					rasterizationState: &rasterCI,
					multisampleState: &msaaCI,
					depthStencilState: &depthStencilCI,
					colorBlendState: &colorBlendCI,
					dynamicState: &dynamicCI,
					layout: shader.PipelineLayout,
					renderPass: renderer.RenderPass,
					subpass: subpass,
					basePipelineHandle: VulkanHandle<VkPipeline>.Null,
					basePipelineIndex: 0
				);
				VulkanHandle<VkPipeline> pipelineHandle;
				renderer.Graphics.Resources.PipelineCache.CreateGraphicsPipelines(1, &ci, null, &pipelineHandle)
					.Throw("Failed to recreate pipeline object");
				return new(pipelineHandle, renderer.Graphics.VkDevice);
			}
		}

		// Creates the set of vertex attributes and bindings from the descriptions
		private static void CalculateVertexInfo(VertexDescription[] descs,
			out VkVertexInputAttributeDescription[] attrs, out VkVertexInputBindingDescription[] binds)
		{
			// Create arrays
			var attrcnt = (int)descs.Sum(vd => vd.BindingCount);
			attrs = new VkVertexInputAttributeDescription[attrcnt];
			binds = new VkVertexInputBindingDescription[descs.Length];

			// Populate attributes & bindings
			var attroff = 0;
			for (var bi = 0; bi < descs.Length; ++bi) {
				var vd = descs[bi];
				for (var ei = 0; ei < vd.Elements.Count; ++ei) {
					var copyCount = vd.Elements[ei].BindingCount; // TODO: Won't work once we add double/long types
					for (uint ai = 0; ai < copyCount; ++ai) {
						attrs[attroff++] = new(
							location: vd.Locations[ei] + ai,
							binding: (uint)bi,
							format: vd.Elements[ei].Format.GetVulkanFormat(),
							offset: vd.Elements[ei].Offset + (ai * vd.Elements[ei].Format.GetSize())
						);
					}
				}
				binds[bi] = new(
					binding: (uint)bi,
					stride: vd.Stride,
					inputRate: (VkVertexInputRate)vd.Rate
				);
			}
		}
		#endregion // Creation

		// Manages the processed collection of states required to rebuild pipeline objects
		internal sealed class BuildStates
		{
			#region Fields
			// Prepared color blending states
			public readonly ColorBlendState[] ColorBlends;
			// Color blend constants
			public readonly BlendConstants BlendConstants;
			// Depth stencil state
			public readonly DepthStencilState DepthStencil;
			// Vertex input state
			public readonly VertexInput VertexInput;
			// Processed vertex attrib/binding information
			public readonly VkVertexInputBindingDescription[] VertexBindings;
			public readonly VkVertexInputAttributeDescription[] VertexAttributes;
			// The rasterizer state
			public readonly RasterizerState RasterizerState;
			#endregion // Fields

			public BuildStates(
				ColorBlendState[] cBlends,
				in BlendConstants bConsts,
				in DepthStencilState dStencil,
				in VertexInput vInput,
				VkVertexInputBindingDescription[] vBinds,
				VkVertexInputAttributeDescription[] vAttrs,
				in RasterizerState rState
			)
			{
				ColorBlends = cBlends;
				BlendConstants = bConsts;
				DepthStencil = dStencil;
				VertexInput = vInput;
				VertexBindings = vBinds;
				VertexAttributes = vAttrs;
				RasterizerState = rState;
			}
		}
	}
}
