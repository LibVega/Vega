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
	/// Contains a set of rendering states that fully define a <see cref="Pipeline"/> object. Most (but not all) states
	/// are required:
	/// <list type="bullet">
	/// <item><see cref="SharedColorBlend"/> OR <see cref="AllColorBlends"/></item>
	/// <item><see cref="DepthStencil"/></item>
	/// <item><see cref="VertexInput"/></item>
	/// <item><see cref="Rasterizer"/></item>
	/// <item><see cref="VertexDescriptions"/></item>
	/// <item><see cref="Shader"/></item>
	/// </list>
	/// </summary>
	public sealed class PipelineDescription
	{
		#region Fields
		/// <summary>
		/// The singular blend state to use for all color attachments. Controls the same array as 
		/// <see cref="AllColorBlends"/>.
		/// </summary>
		public ColorBlendState? SharedColorBlend {
			get => _colorBlends?[0];
			set {
				if (value.HasValue) {
					_colorBlends = new ColorBlendState[1] { value.Value };
					_colorBlendsVk = new VkPipelineColorBlendAttachmentState[1];
					_colorBlends[0].ToVk(out _colorBlendsVk[0]);
				}
				else {
					_colorBlends = null;
					_colorBlendsVk = null;
				}
			}
		}
		/// <summary>
		/// The array of blending states for the color attachments. Must either have one state total, or one state for
		/// each attachment in the renderer.
		/// </summary>
		public ColorBlendState[]? AllColorBlends {
			get => _colorBlends;
			set {
				if (value is not null && (value.Length == 0)) {
					throw new InvalidOperationException("Cannot use an array of length zero for color blends");
				}
				_colorBlends = value;
				_colorBlendsVk = value?.Select(cb => { cb.ToVk(out var vk); return vk; })?.ToArray();
			}
		}
		private ColorBlendState[]? _colorBlends = null;
		private VkPipelineColorBlendAttachmentState[]? _colorBlendsVk = null;

		/// <summary>
		/// Optional constants for color attachment blend operations.
		/// </summary>
		public BlendConstants BlendConstants = default;

		/// <summary>
		/// The depth/stencil operations to perform for the pipeline.
		/// </summary>
		public DepthStencilState? DepthStencil {
			get => _depthStencil;
			set {
				_depthStencil = value;
				value?.ToVk(out _depthStencilVk);
			}
		}
		private DepthStencilState? _depthStencil;
		private VkPipelineDepthStencilStateCreateInfo _depthStencilVk;

		/// <summary>
		/// The vertex input assembly type to perform in the pipeline.
		/// </summary>
		public VertexInput? VertexInput {
			get => _vertexInput;
			set {
				_vertexInput = value;
				value?.ToVk(out _vertexInputVk);
			}
		}
		private VertexInput? _vertexInput;
		private VkPipelineInputAssemblyStateCreateInfo _vertexInputVk;

		/// <summary>
		/// The description of the vertex layout for the pipeline.
		/// </summary>
		public VertexDescription[]? VertexDescriptions {
			get => _vertexDescriptions;
			set {
				_vertexDescriptions = value;
				if (value?.Length == 0) {
					throw new InvalidOperationException("Cannot use an array of length zero for vertex descriptions");
				}
				if (value is not null) {
					CalculateVertexInfo(value, out _vertexAttributesVk, out _vertexBindingsVk);
				}
				else {
					_vertexAttributesVk = null;
					_vertexBindingsVk = null;
				}
			}
		}
		private VertexDescription[]? _vertexDescriptions;
		private VkVertexInputAttributeDescription[]? _vertexAttributesVk;
		private VkVertexInputBindingDescription[]? _vertexBindingsVk;

		/// <summary>
		/// The rasterizer engine state to use in the pipeline.
		/// </summary>
		public RasterizerState? Rasterizer {
			get => _rasterizer;
			set {
				_rasterizer = value;
				value?.ToVk(out _rasterizerVk);
			}
		}
		private RasterizerState? _rasterizer;
		private VkPipelineRasterizationStateCreateInfo _rasterizerVk;

		/// <summary>
		/// The shader program to execute for the pipieline.
		/// </summary>
		public Shader? Shader {
			get => _shader;
			set => _shader = value; // TODO: Validity check
		}
		private Shader? _shader;

		/// <summary>
		/// Gets if the pipeline is fully described by all fields (no required fields are <c>null</c>).
		/// </summary>
		public bool IsValid =>
			(_colorBlends is not null) && _depthStencil.HasValue && _vertexInput.HasValue && _rasterizer.HasValue &&
			(VertexDescriptions is not null) && (_shader is not null);
		#endregion // Fields

		/// <summary>
		/// Creates a new pipeline description.
		/// </summary>
		/// <param name="colorBlends">The blend states to use for the color attachments.</param>
		/// <param name="depthStencil">The depth/stencil state.</param>
		/// <param name="vertexInput">The vertex input assembly state.</param>
		/// <param name="vertexDescs">The vertex layout description.</param>
		/// <param name="rasterizer">The pipeline rasterization state.</param>
		/// <param name="shader">The shader program for the pipeline.</param>
		public PipelineDescription(
			ColorBlendState[]? colorBlends = null,
			DepthStencilState? depthStencil = null,
			VertexInput? vertexInput = null,
			VertexDescription[]? vertexDescs = null,
			RasterizerState? rasterizer = null,
			Shader? shader = null
		)
		{
			AllColorBlends = colorBlends;
			DepthStencil = depthStencil;
			VertexInput = vertexInput;
			VertexDescriptions = vertexDescs;
			Rasterizer = rasterizer;
			Shader = shader;
		}

		/// <summary>
		/// Creates a new pipeline description.
		/// </summary>
		/// <param name="colorBlend">The blend state to use for all color attachments.</param>
		/// <param name="depthStencil">The depth/stencil state.</param>
		/// <param name="vertexInput">The vertex input assembly state.</param>
		/// <param name="vertexDescs">The vertex layout description.</param>
		/// <param name="rasterizer">The pipeline rasterization state.</param>
		/// <param name="shader">The shader program for the pipeline.</param>
		public PipelineDescription(
			ColorBlendState? colorBlend = null,
			DepthStencilState? depthStencil = null,
			VertexInput? vertexInput = null,
			VertexDescription[]? vertexDescs = null,
			RasterizerState? rasterizer = null,
			Shader? shader = null
		)
		{
			SharedColorBlend = colorBlend;
			DepthStencil = depthStencil;
			VertexInput = vertexInput;
			VertexDescriptions = vertexDescs;
			Rasterizer = rasterizer;
			Shader = shader;
		}

		// Populate the pipeline create info
		internal unsafe void CreatePipeline(Renderer renderer, uint subpass, MSAA msaa, out VkPipeline pipeline)
		{
			var dynstates = stackalloc VkDynamicState[2] { 
				VkDynamicState.Viewport, VkDynamicState.Scissor
			};

			// Validate
			var rlayout = (msaa != MSAA.X1) ? renderer.MSAALayout! : renderer.Layout;
			if (!IsValid) {
				throw new InvalidOperationException("Cannot create a pipeline from an incomplete description");
			}
			var cacnt = rlayout.Subpasses[subpass].ColorCount;
			if (_colorBlends!.Length != 1 && (cacnt != _colorBlends.Length)) {
				throw new InvalidOperationException("Invalid color blend count for pipeline");
			}
			var hasds = rlayout.Subpasses[subpass].DepthOffset.HasValue;
			if (!hasds && (_depthStencil!.Value.DepthMode != DepthMode.None)) {
				throw new InvalidOperationException("Cannot perform depth/stencil operations on non-depth/stencil subpass");
			}

			// Describe the color blends
			var cblends = (_colorBlends.Length != 1)
				? _colorBlendsVk
				: Enumerable.Repeat(_colorBlendsVk![0], (int)cacnt).ToArray();

			// Inferred state objects
			VkPipelineMultisampleStateCreateInfo msaaCI = new(
				flags: VkPipelineMultisampleStateCreateFlags.NoFlags,
				rasterizationSamples: (VkSampleCountFlags)msaa,
				sampleShadingEnable: false, // TODO: Allow sample shading
				minSampleShading: 0,
				sampleMask: null,
				alphaToCoverageEnable: false,
				alphaToOneEnable: false
			);

			// Constant state objects
			VkPipelineViewportStateCreateInfo viewportCI = new(); // Dummy value b/c dynamic state
			VkPipelineDynamicStateCreateInfo dynamicCI = new(
				flags: VkPipelineDynamicStateCreateFlags.NoFlags,
				dynamicStateCount: 2,
				dynamicStates: dynstates
			);

			// Shader create info
			var stageCIs = _shader!.EnumerateModules().Select(mod => new VkPipelineShaderStageCreateInfo(
				flags: VkPipelineShaderStageCreateFlags.NoFlags,
				stage: (VkShaderStageFlags)mod.Stage,
				module: mod.Module.Handle,
				name: mod.Module.NativeEntryPoint.Data,
				specializationInfo: null // TODO: Public API for specialization
			)).ToArray();

			// Create the pipeline
			fixed (VkPipelineColorBlendAttachmentState* colorBlendPtr = cblends)
			fixed (VkPipelineShaderStageCreateInfo* stagePtr = stageCIs)
			fixed (VkVertexInputAttributeDescription* attributePtr = _vertexAttributesVk)
			fixed (VkVertexInputBindingDescription* bindingPtr = _vertexBindingsVk)
			fixed (VkPipelineDepthStencilStateCreateInfo* depthStencilPtr = &_depthStencilVk) 
			fixed (VkPipelineInputAssemblyStateCreateInfo* inputAssemblyPtr = &_vertexInputVk) 
			fixed (VkPipelineRasterizationStateCreateInfo* rasterizerPtr = &_rasterizerVk) {
				// Additional create objects
				VkPipelineColorBlendStateCreateInfo colorBlendCI = new(
					flags: VkPipelineColorBlendStateCreateFlags.NoFlags,
					logicOpEnable: false, // TODO: Maybe enable this in the future
					logicOp: VkLogicOp.Clear,
					attachmentCount: cacnt,
					attachments: colorBlendPtr,
					blendConstants_0: BlendConstants.R,
					blendConstants_1: BlendConstants.G,
					blendConstants_2: BlendConstants.B,
					blendConstants_3: BlendConstants.A
				);
				VkPipelineVertexInputStateCreateInfo vertexCI = new(
					flags: VkPipelineVertexInputStateCreateFlags.NoFlags,
					vertexBindingDescriptionCount: (uint)_vertexBindingsVk!.Length,
					vertexBindingDescriptions: bindingPtr,
					vertexAttributeDescriptionCount: (uint)_vertexAttributesVk!.Length,
					vertexAttributeDescriptions: attributePtr
				);

				// Create info
				VkGraphicsPipelineCreateInfo ci = new(
					flags: VkPipelineCreateFlags.NoFlags, // TODO: see if we can utilize some of the flags
					stageCount: (uint)stageCIs.Length,
					stages: stagePtr,
					vertexInputState: &vertexCI,
					inputAssemblyState: inputAssemblyPtr,
					tessellationState: null, // TODO
					viewportState: &viewportCI,
					rasterizationState: rasterizerPtr,
					multisampleState: &msaaCI,
					depthStencilState: depthStencilPtr,
					colorBlendState: &colorBlendCI,
					dynamicState: &dynamicCI,
					layout: _shader.PipelineLayoutHandle,
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
	}
}
