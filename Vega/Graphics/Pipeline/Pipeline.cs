/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Linq;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Contains a compiled aggregate render state which fully defines how draw commands are processed through the
	/// graphics processing pipeline.
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
		// The cached build state for pipeline rebuilds
		internal readonly BuildStates? BuildState;
		internal bool CanRebuild => BuildState is not null;
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
			Handle = CreatePipeline(description, renderer, subpass, out BuildState);

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

		#region Creation
		internal static VkPipeline CreatePipeline(PipelineDescription desc, Renderer renderer, uint subpass, 
			out BuildStates? buildState)
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

			// Describe the color blends
			var cblends = (desc.AllColorBlends.Length != 1)
				? desc.ColorBlendsVk
				: Enumerable.Repeat(desc.ColorBlendsVk![0], (int)cacnt).ToArray();

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
			VkPipelineViewportStateCreateInfo viewportCI = new(); // Dummy value b/c dynamic state
			VkPipelineDynamicStateCreateInfo dynamicCI = new(
				flags: VkPipelineDynamicStateCreateFlags.NoFlags,
				dynamicStateCount: 2,
				dynamicStates: dynstates
			);

			// Vertex info
			CalculateVertexInfo(desc.VertexDescriptions!, out var vertexAttrs, out var vertexBinds);

			// Shader create info
			var stageCIs = desc.Shader!.EnumerateModules().Select(mod => new VkPipelineShaderStageCreateInfo(
				flags: VkPipelineShaderStageCreateFlags.NoFlags,
				stage: (VkShaderStageFlags)mod.Stage,
				module: mod.Module.Handle,
				name: mod.Module.NativeEntryPoint.Data,
				specializationInfo: null // TODO: Public API for specialization
			)).ToArray();
			VkPipelineTessellationStateCreateInfo tessCI = new(
				flags: VkPipelineTessellationStateCreateFlags.NoFlags,
				patchControlPoints: desc.Shader.PatchSize
			);

			// Create the pipeline
			VkPipeline pipeline;
			fixed (VkPipelineColorBlendAttachmentState* colorBlendPtr = cblends)
			fixed (VkPipelineShaderStageCreateInfo* stagePtr = stageCIs)
			fixed (VkVertexInputAttributeDescription* attributePtr = vertexAttrs)
			fixed (VkVertexInputBindingDescription* bindingPtr = vertexBinds)
			fixed (VkPipelineDepthStencilStateCreateInfo* depthStencilPtr = &desc.DepthStencilVk)
			fixed (VkPipelineInputAssemblyStateCreateInfo* inputAssemblyPtr = &desc.VertexInputVk)
			fixed (VkPipelineRasterizationStateCreateInfo* rasterizerPtr = &desc.RasterizerVk) {
				// Additional create objects
				VkPipelineColorBlendStateCreateInfo colorBlendCI = new(
					flags: VkPipelineColorBlendStateCreateFlags.NoFlags,
					logicOpEnable: false, // TODO: Maybe enable this in the future
					logicOp: VkLogicOp.Clear,
					attachmentCount: cacnt,
					attachments: colorBlendPtr,
					blendConstants_0: desc.BlendConstants.R,
					blendConstants_1: desc.BlendConstants.G,
					blendConstants_2: desc.BlendConstants.B,
					blendConstants_3: desc.BlendConstants.A
				);
				VkPipelineVertexInputStateCreateInfo vertexCI = new(
					flags: VkPipelineVertexInputStateCreateFlags.NoFlags,
					vertexBindingDescriptionCount: (uint)vertexBinds.Length,
					vertexBindingDescriptions: bindingPtr,
					vertexAttributeDescriptionCount: (uint)vertexAttrs.Length,
					vertexAttributeDescriptions: attributePtr
				);

				// Create info
				VkGraphicsPipelineCreateInfo ci = new(
					flags: VkPipelineCreateFlags.NoFlags, // TODO: see if we can utilize some of the flags
					stageCount: (uint)stageCIs.Length,
					stages: stagePtr,
					vertexInputState: &vertexCI,
					inputAssemblyState: inputAssemblyPtr,
					tessellationState: &tessCI,
					viewportState: &viewportCI,
					rasterizationState: rasterizerPtr,
					multisampleState: &msaaCI,
					depthStencilState: depthStencilPtr,
					colorBlendState: &colorBlendCI,
					dynamicState: &dynamicCI,
					layout: desc.Shader.PipelineLayoutHandle,
					renderPass: renderer.RenderPass,
					subpass: subpass,
					basePipelineHandle: VulkanHandle<VkPipeline>.Null,
					basePipelineIndex: 0
				);
				VulkanHandle<VkPipeline> pipelineHandle;
				renderer.Graphics.Resources.PipelineCache.CreateGraphicsPipelines(1, &ci, null, &pipelineHandle)
					.Throw("Failed to create pipeline object");
				pipeline = new(pipelineHandle, renderer.Graphics.VkDevice);
			}

			// If there is a chance for a pipeline rebuild, create the build state
			if (renderer.MSAALayout is not null) {
				var cblendarr = (desc.AllColorBlends!.Length != 1)
					? desc.AllColorBlends // No copy needed since the array is not modified in PipelineDescription
					: Enumerable.Repeat(desc.SharedColorBlend!.Value, cblends!.Length).ToArray();
				buildState = new(
					cblendarr!,
					desc.BlendConstants,
					desc.DepthStencil!.Value,
					desc.VertexInput!.Value,
					vertexBinds,
					vertexAttrs,
					desc.Rasterizer!.Value
				);
			}
			else {
				buildState = null;
			}

			// Return pipeline object
			return pipeline;
		}

		// Rebuilds the pipeline from the cached build state
		private static VkPipeline RebuildPipeline(BuildStates states, Renderer renderer, uint subpass, Shader shader)
		{
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
			VkPipelineViewportStateCreateInfo viewportCI = new(); // Dummy value b/c dynamic state
			VkPipelineDynamicStateCreateInfo dynamicCI = new(
				flags: VkPipelineDynamicStateCreateFlags.NoFlags,
				dynamicStateCount: 2,
				dynamicStates: dynstates
			);

			// Shader create info
			var stageCIs = shader.EnumerateModules().Select(mod => new VkPipelineShaderStageCreateInfo(
				flags: VkPipelineShaderStageCreateFlags.NoFlags,
				stage: (VkShaderStageFlags)mod.Stage,
				module: mod.Module.Handle,
				name: mod.Module.NativeEntryPoint.Data,
				specializationInfo: null // TODO: Public API for specialization
			)).ToArray();
			VkPipelineTessellationStateCreateInfo tessCI = new(
				flags: VkPipelineTessellationStateCreateFlags.NoFlags,
				patchControlPoints: shader.PatchSize
			);

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
					layout: shader.PipelineLayoutHandle,
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
			var attrcnt = descs.Sum(vd => vd.Elements.Count);
			attrs = new VkVertexInputAttributeDescription[attrcnt];
			binds = new VkVertexInputBindingDescription[descs.Length];

			// Populate attributes & bindings
			var attroff = 0;
			for (var bi = 0; bi < descs.Length; ++bi) {
				var vd = descs[bi];
				for (var ei = 0; ei < vd.Elements.Count; ++ei) {
					attrs[attroff++] = new(
						location: vd.Locations[ei],
						binding: (uint)bi,
						format: (VkFormat)vd.Elements[ei].Format,
						offset: vd.Elements[ei].Offset
					);
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
