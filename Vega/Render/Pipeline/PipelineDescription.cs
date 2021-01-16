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
					ColorBlendsVk = new VkPipelineColorBlendAttachmentState[1];
					_colorBlends[0].ToVk(out ColorBlendsVk[0]);
				}
				else {
					_colorBlends = null;
					ColorBlendsVk = null;
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
				ColorBlendsVk = value?.Select(cb => { cb.ToVk(out var vk); return vk; })?.ToArray();
			}
		}
		private ColorBlendState[]? _colorBlends = null;
		internal VkPipelineColorBlendAttachmentState[]? ColorBlendsVk = null;

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
				if (value.HasValue) {
					value.Value.ToVk(out var vk);
					DepthStencilVk = vk;
				}
			}
		}
		private DepthStencilState? _depthStencil;
		internal VkPipelineDepthStencilStateCreateInfo DepthStencilVk;

		/// <summary>
		/// The vertex input assembly type to perform in the pipeline.
		/// </summary>
		public VertexInput? VertexInput {
			get => _vertexInput;
			set {
				_vertexInput = value;
				if (value.HasValue) {
					value.Value.ToVk(out var vk);
					VertexInputVk = vk; 
				}
			}
		}
		private VertexInput? _vertexInput;
		internal VkPipelineInputAssemblyStateCreateInfo VertexInputVk;

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
			}
		}
		private VertexDescription[]? _vertexDescriptions;

		/// <summary>
		/// The rasterizer engine state to use in the pipeline.
		/// </summary>
		public RasterizerState? Rasterizer {
			get => _rasterizer;
			set {
				_rasterizer = value;
				if (value.HasValue) {
					value.Value.ToVk(out var vk);
					RasterizerVk = vk;
				}
			}
		}
		private RasterizerState? _rasterizer;
		internal VkPipelineRasterizationStateCreateInfo RasterizerVk;

		/// <summary>
		/// The shader program to execute for the pipieline.
		/// </summary>
		public ShaderProgram? Shader {
			get => _shader;
			set => _shader = value;
		}
		private ShaderProgram? _shader;

		/// <summary>
		/// Gets if the pipeline is fully described by all fields (no required fields are <c>null</c>).
		/// </summary>
		public bool IsComplete =>
			(_colorBlends is not null) && _depthStencil.HasValue && _vertexInput.HasValue && _rasterizer.HasValue &&
			(VertexDescriptions is not null) && (_shader is not null);
		#endregion // Fields

		/// <summary>
		/// Create a default pipeline description with no specified states.
		/// </summary>
		public PipelineDescription()
		{
			AllColorBlends = null;
			DepthStencil = null;
			VertexInput = null;
			VertexDescriptions = null;
			Rasterizer = null;
			Shader = null;
		}

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
			ShaderProgram? shader = null
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
			ShaderProgram? shader = null
		)
		{
			SharedColorBlend = colorBlend;
			DepthStencil = depthStencil;
			VertexInput = vertexInput;
			VertexDescriptions = vertexDescs;
			Rasterizer = rasterizer;
			Shader = shader;
		}
	}
}
