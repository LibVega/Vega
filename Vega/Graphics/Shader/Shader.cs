/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Threading;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Represets a specific set of <see cref="ShaderModule"/>s as a complete shader program. All shaders must have at 
	/// least the vertex and fragment stages. Both tessellation stages must be present if used.
	/// </summary>
	public unsafe sealed partial class Shader : ResourceBase
	{
		#region Fields
		/// <summary>
		/// The shader stages that are present in the program.
		/// </summary>
		public readonly ShaderStages Stages;

		/// <summary>
		/// The module used for the vertex stage in this shader.
		/// </summary>
		public readonly ShaderModule VertexModule;
		/// <summary>
		/// The module used for the tessellation control stage in this shader.
		/// </summary>
		public readonly ShaderModule? TessControlModule;
		/// <summary>
		/// The module used for the tessellation eval stage in this shader.
		/// </summary>
		public readonly ShaderModule? TessEvalModule;
		/// <summary>
		/// The module used for the geometry stage in this shader.
		/// </summary>
		public readonly ShaderModule? GeometryModule;
		/// <summary>
		/// The module used for the fragment stage in this shader.
		/// </summary>
		public readonly ShaderModule FragmentModule;

		/// <summary>
		/// For shaders using tessellation, this is the number of control points (vertices) that make up a tessellation
		/// patch.
		/// <para>
		/// This value is limited by <see cref="GraphicsLimits.MaxPatchSize"/>.
		/// </para>
		/// </summary>
		public readonly uint PatchSize;

		/// <summary>
		/// The binding layout for the buffers binding group.
		/// </summary>
		public readonly BindingLayout BufferLayout = new(BindingGroup.Buffers);
		/// <summary>
		/// The binding layout for the samplers binding group.
		/// </summary>
		public readonly BindingLayout SamplerLayout = new(BindingGroup.Samplers);
		/// <summary>
		/// The binding layout for the textures binding group.
		/// </summary>
		public readonly BindingLayout TextureLayout = new(BindingGroup.Textures);
		/// <summary>
		/// The binding layout for the input attachments binding group.
		/// </summary>
		public readonly BindingLayout InputAttachmentLayout = new(BindingGroup.InputAttachments);

		/// <summary>
		/// The size of the shader push constant block, in bytes.
		/// <para>
		/// This value is limited by <see cref="GraphicsLimits.MaxPushConstantSize"/>.
		/// </para>
		/// </summary>
		public readonly uint PushConstantSize;
		/// <summary>
		/// The stages that access the push constant block.
		/// </summary>
		public readonly ShaderStages PushConstantStages;

		// Descriptor set layout objects
		internal readonly VkDescriptorSetLayout? BufferLayoutHandle;
		internal readonly VkDescriptorSetLayout? SamplerLayoutHandle;
		internal readonly VkDescriptorSetLayout? TextureLayoutHandle;
		internal readonly VkDescriptorSetLayout? InputAttachmentLayoutHandle;
		// Pipeline layout object
		internal readonly VkPipelineLayout PipelineLayoutHandle;

		// Number of shader stages
		internal uint StageCount => 2u +
			((TessControlModule is not null) ? 1u : 0u) +
			((TessEvalModule is not null) ? 1u : 0u) +
			((GeometryModule is not null) ? 1u : 0u);

		/// <summary>
		/// The number of <see cref="Pipeline"/> objects that are actively using this shader. Attempting to dispose
		/// a shader that is in use will generate an exception.
		/// </summary>
		public uint RefCount => _refCount;
		private uint _refCount = 0;
		#endregion // Fields

		#region Ctor
		/// <summary>
		/// Describe a new shader with vertex and fragment stages.
		/// </summary>
		/// <param name="vert">The vertex module.</param>
		/// <param name="frag">The fragment module.</param>
		public Shader(ShaderModule vert, ShaderModule frag)
			: this(vert, null, null, null, frag)
		{ }
		/// <summary>
		/// Describe a new shader with vertex, geometry, and fragment stages.
		/// </summary>
		/// <param name="vert">The vertex module.</param>
		/// <param name="geom">The geometry module.</param>
		/// <param name="frag">The fragment module.</param>
		public Shader(ShaderModule vert, ShaderModule geom, ShaderModule frag)
			: this(vert, null, null, geom, frag)
		{ }
		/// <summary>
		/// Describe a new shader with vertex, tessellation, and fragment stages.
		/// </summary>
		/// <param name="vert">The vertex module.</param>
		/// <param name="tesc">The tessellation control module.</param>
		/// <param name="tese">The tessellation evaluation module.</param>
		/// <param name="frag">The fragment module.</param>
		/// <param name="patchSize">The number of control points (vertices) that make up a tessellation patch.</param>
		public Shader(ShaderModule vert, ShaderModule tesc, ShaderModule tese, ShaderModule frag, uint patchSize)
			: this(vert, tesc, tese, null, frag, patchSize)
		{ }
		/// <summary>
		/// Describe a new shader with explicit modules for each stage.
		/// </summary>
		/// <param name="vert">The required vertex module.</param>
		/// <param name="tesc">The optional tessellation control module.</param>
		/// <param name="tese">The optional tessellation eval module.</param>
		/// <param name="geom">The optional geometry module.</param>
		/// <param name="frag">The required fragment module.</param>
		/// <param name="patchSize">
		/// The number of control points (vertices) that make up a tessellation patch. <c>null</c> if tessellation is
		/// not enabled.
		/// </param>
		public Shader(ShaderModule vert, ShaderModule? tesc, ShaderModule? tese, ShaderModule? geom, ShaderModule frag,
				uint? patchSize = null)
			: base(ResourceType.Shader)
		{
			var gd = Core.Instance!.Graphics;

			// Validate stages
			if (vert.Stage != ShaderStages.Vertex) {
				throw new ArgumentException("Module given for vertex stage is not a vertex shader", nameof(vert));
			}
			if ((tesc is null) != (tese is null)) {
				throw new InvalidOperationException("Cannot specify only one tessellation stage");
			}
			if ((tesc is not null) && !gd.Features.TessellationShaders) {
				throw new InvalidOperationException("Tessellation shaders feature must be enabled");
			}
			if ((tesc is not null) && !patchSize.HasValue) {
				throw new ArgumentException("A patch size must be given for shaders using tessellation", nameof(patchSize));
			}
			if (patchSize.HasValue && (patchSize.Value > Core.Instance!.Graphics.Limits.MaxPatchSize)) {
				throw new ArgumentException("The patch size is larger than the device limits", nameof(patchSize));
			}
			if ((geom is not null) && !gd.Features.GeometryShaders) {
				throw new InvalidOperationException("Geometry shaders feature must be enabled");
			}
			if (frag.Stage != ShaderStages.Fragment) {
				throw new ArgumentException("Module given for fragment stage is not a fragment shader", nameof(frag));
			}

			// Assign stages and update ref counts
			VertexModule = vert;
			TessControlModule = tesc;
			TessEvalModule = tese;
			GeometryModule = geom;
			FragmentModule = frag;
			VertexModule.IncRef();
			TessControlModule?.IncRef();
			TessEvalModule?.IncRef();
			GeometryModule?.IncRef();
			FragmentModule.IncRef();
			PatchSize = patchSize.GetValueOrDefault();

			// Set reflection values
			Stages = ShaderStages.Vertex | ShaderStages.Fragment |
				((tesc is not null) ? ShaderStages.TessControl : ShaderStages.None) |
				((tese is not null) ? ShaderStages.TessEval : ShaderStages.None) |
				((geom is not null) ? ShaderStages.Geometry : ShaderStages.None);

			// Populate binding layouts
			MergeBindingLayouts(BufferLayout, vert.BufferLayout, tesc?.BufferLayout, tese?.BufferLayout,
				geom?.BufferLayout, frag.BufferLayout);
			MergeBindingLayouts(SamplerLayout, vert.SamplerLayout, tesc?.SamplerLayout, tese?.SamplerLayout,
				geom?.SamplerLayout, frag.SamplerLayout);
			MergeBindingLayouts(TextureLayout, vert.TextureLayout, tesc?.TextureLayout, tese?.TextureLayout,
				geom?.TextureLayout, frag.TextureLayout);
			MergeBindingLayouts(InputAttachmentLayout, vert.InputAttachmentLayout, tesc?.InputAttachmentLayout, 
				tese?.InputAttachmentLayout, geom?.InputAttachmentLayout, frag.InputAttachmentLayout);

			// Get push size info
			PushConstantSize = vert.PushConstantSize;
			PushConstantStages = (vert.PushConstantSize != 0) ? ShaderStages.Vertex : ShaderStages.None;
			CheckPushConstantSize(tesc, ref PushConstantSize, ref PushConstantStages);
			CheckPushConstantSize(tese, ref PushConstantSize, ref PushConstantStages);
			CheckPushConstantSize(geom, ref PushConstantSize, ref PushConstantStages);
			CheckPushConstantSize(frag, ref PushConstantSize, ref PushConstantStages);

			// Create the layout objects
			BufferLayoutHandle = BufferLayout.CreateDescriptorSetLayout();
			SamplerLayoutHandle = SamplerLayout.CreateDescriptorSetLayout();
			TextureLayoutHandle = TextureLayout.CreateDescriptorSetLayout();
			InputAttachmentLayoutHandle = InputAttachmentLayout.CreateDescriptorSetLayout();
			PipelineLayoutHandle = CreatePipelineLayout(this);
		}
		#endregion // Ctor

		internal IEnumerable<(ShaderStages Stage, ShaderModule Module)> EnumerateModules()
		{
			yield return (ShaderStages.Vertex, VertexModule);
			if (TessControlModule is not null) {
				yield return (ShaderStages.TessControl, TessControlModule);
			}
			if (TessEvalModule is not null) {
				yield return (ShaderStages.TessEval, TessEvalModule);
			}
			if (GeometryModule is not null) {
				yield return (ShaderStages.Geometry, GeometryModule);
			}
			yield return (ShaderStages.Fragment, FragmentModule);
		}

		internal void IncRef() => Interlocked.Increment(ref _refCount);
		internal void DecRef() => Interlocked.Decrement(ref _refCount);

		internal BindingSet AllocateBindingSet(BindingGroup group)
		{
			var res = Core.Instance!.Graphics.Resources;
			return group switch { 
				BindingGroup.Buffers => (BufferLayoutHandle is not null)
					? res.BufferBindingPool.Allocate(BufferLayout.Counts, BufferLayoutHandle)
					: throw new Exception("LIBRARY BUG - Attempt to allocate buffer set from non-buffer shader"),
				BindingGroup.Samplers => (SamplerLayoutHandle is not null)
					? res.SamplerBindingPool.Allocate(SamplerLayout.Counts, SamplerLayoutHandle)
					: throw new Exception("LIBRARY BUG - Attempt to allocate sampler set from non-sampler shader"),
				BindingGroup.Textures => (TextureLayoutHandle is not null)
					? res.TextureBindingPool.Allocate(TextureLayout.Counts, TextureLayoutHandle)
					: throw new Exception("LIBRARY BUG - Attempt to allocate texture set from non-texture shader"),
				BindingGroup.InputAttachments => (InputAttachmentLayoutHandle is not null)
					? res.InputAttachmentBindingPool.Allocate(InputAttachmentLayout.Counts, InputAttachmentLayoutHandle)
					: throw new Exception("LIBRARY BUG - Attempt to allocate input attachment set from non-buffer shader"),
				_ => throw new Exception("LIBRARY BUG - Invalid group argument")
			};
		}

		#region ResourceBase
		protected override void OnDispose(bool disposing)
		{
			if (disposing && (_refCount != 0)) {
				throw new InvalidOperationException("Cannot dispose of shader that is in use");
			}

			if (Core.Instance is not null) {
				Core.Instance.Graphics.Resources.QueueDestroy(this);
			}
			else {
				Destroy();
			}

			VertexModule?.DecRef();
			TessControlModule?.DecRef();
			TessEvalModule?.DecRef();
			GeometryModule?.DecRef();
			FragmentModule?.DecRef();
		}

		protected internal override void Destroy()
		{
			BufferLayoutHandle?.DestroyDescriptorSetLayout(null);
			SamplerLayoutHandle?.DestroyDescriptorSetLayout(null);
			TextureLayoutHandle?.DestroyDescriptorSetLayout(null);
			InputAttachmentLayoutHandle?.DestroyDescriptorSetLayout(null);
			PipelineLayoutHandle?.DestroyPipelineLayout(null);
		}
		#endregion // ResourceBase

		// Merge all module layouts into the single layout
		private static void MergeBindingLayouts(BindingLayout layout, BindingLayout vert, BindingLayout? tesc,
			BindingLayout? tese, BindingLayout? geom, BindingLayout frag)
		{
			if (vert.SlotCount > 0) {
				layout.Merge(vert, ShaderStages.Vertex);
			}
			if ((tesc is not null) && (tesc?.SlotCount > 0)) {
				layout.Merge(tesc, ShaderStages.TessControl);
			}
			if ((tese is not null) && (tese?.SlotCount > 0)) {
				layout.Merge(tese, ShaderStages.TessEval);
			}
			if ((geom is not null) && (geom?.SlotCount > 0)) {
				layout.Merge(geom, ShaderStages.Geometry);
			}
			if (frag.SlotCount > 0) {
				layout.Merge(frag, ShaderStages.Fragment);
			}
		}

		// Checks a module push constant size against the expected
		private static void CheckPushConstantSize(ShaderModule? mod, ref uint targSize, ref ShaderStages stages)
		{
			if ((mod is not null) && (mod?.PushConstantSize != 0)) {
				if (targSize != 0) {
					if (mod!.PushConstantSize != targSize) {
						throw new IncompatibleModuleException(mod.Stage, "incompatible push constant block size");
					}
				}
				else {
					targSize = mod!.PushConstantSize;
				}
				stages |= mod.Stage;
			}
		}

		// Create a pipeline layout object
		private static VkPipelineLayout CreatePipelineLayout(Shader shader)
		{
			// Collect descriptor layouts
			var layouts = stackalloc VulkanHandle<VkDescriptorSetLayout>[4] { 
				shader.BufferLayoutHandle?.Handle ?? Builtin.EmptyDescriptorSetLayout,
				shader.SamplerLayoutHandle?.Handle ?? Builtin.EmptyDescriptorSetLayout,
				shader.TextureLayoutHandle?.Handle ?? Builtin.EmptyDescriptorSetLayout,
				shader.InputAttachmentLayoutHandle?.Handle ?? Builtin.EmptyDescriptorSetLayout
			};
			var laycount =
				(shader.InputAttachmentLayoutHandle is not null) ? 4 :
				(shader.TextureLayoutHandle is not null) ? 3 :
				(shader.SamplerLayoutHandle is not null) ? 2 :
				(shader.BufferLayoutHandle is not null) ? 1 : 0;

			// Describe push constants
			VkPushConstantRange pcrange = new(
				stageFlags: (VkShaderStageFlags)shader.PushConstantStages,
				offset: 0,
				size: shader.PushConstantSize
			);

			// Create layout
			VkPipelineLayoutCreateInfo plci = new(
				flags: VkPipelineLayoutCreateFlags.NoFlags,
				setLayoutCount: (uint)laycount,
				setLayouts: layouts,
				pushConstantRangeCount: (pcrange.Size != 0) ? 1 : 0,
				pushConstantRanges: &pcrange
			);
			VulkanHandle<VkPipelineLayout> handle;
			Core.Instance!.Graphics.VkDevice.CreatePipelineLayout(&plci, null, &handle)
				.Throw("Failed to create pipeline layout");
			return new(handle, Core.Instance!.Graphics.VkDevice);
		}
	}
}
