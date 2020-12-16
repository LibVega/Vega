/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using Vega.Graphics.Reflection;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Represets a specific set of <see cref="ShaderModule"/>s as a complete shader program. All shaders must have at 
	/// least the vertex and fragment stages. Both tessellation stages must be present if used.
	/// </summary>
	public unsafe sealed class Shader : ResourceBase
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
		/// </summary>
		public readonly uint PushConstantSize;
		/// <summary>
		/// The stages that access the push constant block.
		/// </summary>
		public readonly ShaderStages PushConstantStages;

		// Number of shader stages
		internal uint StageCount => 2u +
			((TessControlModule is not null) ? 1u : 0u) +
			((TessEvalModule is not null) ? 1u : 0u) +
			((GeometryModule is not null) ? 1u : 0u);
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
		public Shader(ShaderModule vert, ShaderModule tesc, ShaderModule tese, ShaderModule frag)
			: this(vert, tesc, tese, null, frag)
		{ }
		/// <summary>
		/// Describe a new shader with explicit modules for each stage.
		/// </summary>
		/// <param name="vert">The required vertex module.</param>
		/// <param name="tesc">The optional tessellation control module.</param>
		/// <param name="tese">The optional tessellation eval module.</param>
		/// <param name="geom">The optional geometry module.</param>
		/// <param name="frag">The required fragment module.</param>
		public Shader(ShaderModule vert, ShaderModule? tesc, ShaderModule? tese, ShaderModule? geom, ShaderModule frag)
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

			// Set reflection values
			Stages = ShaderStages.Vertex | ShaderStages.Fragment |
				((tesc is not null) ? ShaderStages.TessControl : ShaderStages.None) |
				((tese is not null) ? ShaderStages.TessEval : ShaderStages.None) |
				((geom is not null) ? ShaderStages.Geometry : ShaderStages.None);

			// Populate binding layouts
			PopulateBindingLayout(BufferLayout, vert.BufferGroup, tesc?.BufferGroup, tese?.BufferGroup,
				geom?.BufferGroup, frag.BufferGroup);
			PopulateBindingLayout(SamplerLayout, vert.SamplerGroup, tesc?.SamplerGroup, tese?.SamplerGroup,
				geom?.SamplerGroup, frag.SamplerGroup);
			PopulateBindingLayout(TextureLayout, vert.TextureGroup, tesc?.TextureGroup, tese?.TextureGroup,
				geom?.TextureGroup, frag.TextureGroup);
			PopulateBindingLayout(InputAttachmentLayout, vert.InputAttachmentGroup, tesc?.InputAttachmentGroup, 
				tese?.InputAttachmentGroup, geom?.InputAttachmentGroup, frag.InputAttachmentGroup);

			// Get push size info
			PushConstantSize = vert.PushConstantSize;
			PushConstantStages = (vert.PushConstantSize != 0) ? ShaderStages.Vertex : ShaderStages.None;
			if (!CheckPushConstantSize(tesc, ref PushConstantSize, ref PushConstantStages)) {
				throw new IncompatibleModuleException(ShaderStages.TessControl, "Push constant block size mismatch");
			}
			if (!CheckPushConstantSize(tese, ref PushConstantSize, ref PushConstantStages)) {
				throw new IncompatibleModuleException(ShaderStages.TessEval, "Push constant block size mismatch");
			}
			if (!CheckPushConstantSize(geom, ref PushConstantSize, ref PushConstantStages)) {
				throw new IncompatibleModuleException(ShaderStages.Geometry, "Push constant block size mismatch");
			}
			if (!CheckPushConstantSize(frag, ref PushConstantSize, ref PushConstantStages)) {
				throw new IncompatibleModuleException(ShaderStages.Fragment, "Push constant block size mismatch");
			}
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

		#region ResourceBase
		protected override void OnDispose(bool disposing)
		{
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

		}
		#endregion // ResourceBase

		// Adds the binding information into the binding layout
		private static void PopulateBindingLayout(BindingLayout layout, BindingSet vert, BindingSet? tesc,
			BindingSet? tese, BindingSet? geom, BindingSet frag)
		{
			foreach (var info in vert.EnumerateFilledBindings()) {
				layout.Add(info, ShaderStages.Vertex);
			}
			if (tesc is not null) {
				foreach (var info in tesc.EnumerateFilledBindings()) {
					layout.Add(info, ShaderStages.TessControl);
				}
			}
			if (tese is not null) {
				foreach (var info in tese.EnumerateFilledBindings()) {
					layout.Add(info, ShaderStages.TessEval);
				}
			}
			if (geom is not null) {
				foreach (var info in geom.EnumerateFilledBindings()) {
					layout.Add(info, ShaderStages.Geometry);
				}
			}
			foreach (var info in frag.EnumerateFilledBindings()) {
				layout.Add(info, ShaderStages.Fragment);
			}
		}

		// Checks a module push constant size against the expected
		private static bool CheckPushConstantSize(ShaderModule? mod, ref uint targSize, ref ShaderStages stages)
		{
			if (mod?.PushConstantSize != 0) {
				if (targSize != 0) {
					if (mod!.PushConstantSize != targSize) {
						return false;
					}
				}
				else {
					targSize = mod!.PushConstantSize;
				}
				stages |= stages;
				return true;
			}
			else {
				return true;
			}
		}
	}
}
