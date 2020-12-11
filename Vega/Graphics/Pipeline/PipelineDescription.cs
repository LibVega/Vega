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
		/// Gets if the pipeline is fully described by all fields (no required fields are <c>null</c>).
		/// </summary>
		public bool IsValid =>
			(_colorBlends is not null) && _depthStencil.HasValue && _vertexInput.HasValue && _rasterizer.HasValue;
		#endregion // Fields

		/// <summary>
		/// Creates a new pipeline description.
		/// </summary>
		/// <param name="colorBlends">The blend states to use for the color attachments.</param>
		/// <param name="depthStencil">The depth/stencil state.</param>
		/// <param name="vertexInput">The vertex input assembly state.</param>
		/// <param name="rasterizer">The pipeline rasterization state.</param>
		public PipelineDescription(
			ColorBlendState[]? colorBlends = null,
			DepthStencilState? depthStencil = null,
			VertexInput? vertexInput = null,
			RasterizerState? rasterizer = null
		)
		{
			AllColorBlends = colorBlends;
			DepthStencil = depthStencil;
			VertexInput = vertexInput;
			Rasterizer = rasterizer;
		}

		/// <summary>
		/// Creates a new pipeline description.
		/// </summary>
		/// <param name="colorBlend">The blend state to use for all color attachments.</param>
		/// <param name="depthStencil">The depth/stencil state.</param>
		/// <param name="vertexInput">The vertex input assembly state.</param>
		/// <param name="rasterizer">The pipeline rasterization state.</param>
		public PipelineDescription(
			ColorBlendState? colorBlend = null,
			DepthStencilState? depthStencil = null,
			VertexInput? vertexInput = null,
			RasterizerState? rasterizer = null
		)
		{
			SharedColorBlend = colorBlend;
			DepthStencil = depthStencil;
			VertexInput = vertexInput;
			Rasterizer = rasterizer;
		}

		// Populate the pipeline create info
		internal unsafe void CreatePipeline(Renderer renderer, uint subpass, MSAA msaa, out VkPipeline pipeline)
		{
			var dynstates = stackalloc VkDynamicState[2] { VkDynamicState.Viewport, VkDynamicState.Scissor };

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

			// Create the pipeline
			fixed (VkPipelineColorBlendAttachmentState* colorBlendPtr = cblends) 
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

				// Create info
				VkGraphicsPipelineCreateInfo ci = new(
					flags: VkPipelineCreateFlags.NoFlags, // TODO: see if we can utilize some of the flags
					stageCount: 0, // TODO
					stages: null, // TODO
					vertexInputState: null, // TODO
					inputAssemblyState: inputAssemblyPtr,
					tessellationState: null, // TODO
					viewportState: &viewportCI,
					rasterizationState: rasterizerPtr,
					multisampleState: &msaaCI,
					depthStencilState: depthStencilPtr,
					colorBlendState: &colorBlendCI,
					dynamicState: &dynamicCI,
					layout: VulkanHandle<VkPipelineLayout>.Null, // TODO
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
	}
}
