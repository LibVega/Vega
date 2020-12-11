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
	/// Contains a set of rendering states that fully define a <see cref="Pipeline"/> object.
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
		/// Gets if the pipeline is fully described by all fields (no required fields are <c>null</c>).
		/// </summary>
		public bool IsValid =>
			(_colorBlends is not null);
		#endregion // Fields

		/// <summary>
		/// Creates a new pipeline description.
		/// </summary>
		/// <param name="colorBlends">The blend states to use for the color attachments.</param>
		public PipelineDescription(
			ColorBlendState[]? colorBlends = null
		)
		{
			AllColorBlends = colorBlends;
		}

		/// <summary>
		/// Creates a new pipeline description.
		/// </summary>
		/// <param name="colorBlend">The blend state to use for all color attachments.</param>
		public PipelineDescription(
			ColorBlendState? colorBlend = null
		)
		{
			SharedColorBlend = colorBlend;
		}

		// Populate the pipeline create info
		internal unsafe void CreatePipeline(Renderer renderer, uint subpass, out VkPipeline pipeline)
		{
			// Validate
			if (!IsValid) {
				throw new InvalidOperationException("Cannot create a pipeline from an incomplete description");
			}
			uint cacnt = renderer.Layout.Subpasses[subpass].ColorCount;
			if (_colorBlends!.Length != 1 && (cacnt != _colorBlends.Length)) {
				throw new InvalidOperationException("Invalid color blend count for pipeline");
			}

			// Describe and pin the color blends
			var cblends = (_colorBlends.Length != 1)
				? _colorBlendsVk
				: Enumerable.Repeat(_colorBlendsVk![0], (int)cacnt).ToArray();

			// Create the pipeline
			fixed (VkPipelineColorBlendAttachmentState* colorBlendPtr = cblends) {
				// Additional create objects
				VkPipelineColorBlendStateCreateInfo colorBlendCI = new(
					flags: VkPipelineColorBlendStateCreateFlags.NoFlags,
					logicOpEnable: false, // TODO: Maybe enable this in the future
					logicOp: VkLogicOp.Clear,
					attachmentCount: cacnt,
					attachments: colorBlendPtr,
					blendConstants_0: 0,
					blendConstants_1: 0,
					blendConstants_2: 0,
					blendConstants_3: 0
				);

				// Create info
				VkGraphicsPipelineCreateInfo ci = new(
					flags: VkPipelineCreateFlags.NoFlags, // TODO: see if we can utilize some of the flags
					stageCount: 0, // TODO
					stages: null, // TODO
					vertexInputState: null, // TODO
					inputAssemblyState: null, // TODO
					tessellationState: null, // TODO
					viewportState: null, // TODO
					rasterizationState: null, // TODO
					multisampleState: null, // TODO
					depthStencilState: null, // TODO
					colorBlendState: &colorBlendCI,
					dynamicState: null, // TODO
					layout: VulkanHandle<VkPipelineLayout>.Null, // TODO
					renderPass: VulkanHandle<VkRenderPass>.Null, // TODO
					subpass: 0, // TODO
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
