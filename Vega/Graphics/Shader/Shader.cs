/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using Vega.Content;
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

		// Number of shader stages
		internal uint StageCount => 2u +
			((TessControlModule is not null) ? 1u : 0u) +
			((TessEvalModule is not null) ? 1u : 0u) +
			((GeometryModule is not null) ? 1u : 0u);
		#endregion // Fields

		#region Ctor

		public Shader(ShaderModule vert, ShaderModule frag)
			: this(vert, null, null, null, frag)
		{ }

		public Shader(ShaderModule vert, ShaderModule geom, ShaderModule frag)
			: this(vert, null, null, geom, frag)
		{ }

		public Shader(ShaderModule vert, ShaderModule tesc, ShaderModule tese, ShaderModule frag)
			: this(vert, tesc, tese, null, frag)
		{ }

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
	}
}
