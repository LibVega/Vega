/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using Vega.Graphics;

namespace Vega.Render
{
	/// <summary>
	/// Represents the vertex input, pipeline parameters, and shader for a graphics pipeline. 
	/// <para>
	/// When bound to a <see cref="Renderer"/>, this describes the vertex buffer processing, shader uniform uploads, 
	/// and bindable resources.
	/// </para>
	/// </summary>
	public sealed class Material : IDisposable
	{
		#region Fields
		/// <summary>
		/// The shader program layout utilized by the material.
		/// </summary>
		public readonly ShaderLayout Layout;
		// The shader program bytecode
		internal readonly ShaderProgram Program;

		/// <summary>
		/// The vertex input parameters for the material.
		/// </summary>
		public readonly MaterialParams Params;

		/// <summary>
		/// The descriptions for the material vertex inputs.
		/// </summary>
		public IReadOnlyList<VertexDescription> Vertices => _vertices;
		private readonly VertexDescription[] _vertices;

		/// <summary>
		/// Object disposal flag.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		/// <summary>
		/// Describes a new material from a shader program and a specific set of vertex descriptions.
		/// </summary>
		/// <param name="shader">The shader program to process the material with.</param>
		/// <param name="params">The vertex input topology for the material.</param>
		/// <param name="vertices">The descriptions for the vertex inputs for the material.</param>
		public Material(Shader shader, in MaterialParams @params, params VertexDescription[] vertices)
			: this(shader.Layout, shader.Program, @params, vertices)
		{ }
		// Internal constructor
		internal Material(ShaderLayout layout, ShaderProgram program, in MaterialParams @params, 
			params VertexDescription[] vertices)
		{
			// Assign objects
			Layout = layout;
			Program = program;
			Layout.IncRefCount();
			Program.IncRefCount();
			Params = @params;
			_vertices = vertices;

			// Validate vertex inputs
			if (vertices.Length > 0) {
				uint vmask = 0;
				foreach (var vd in vertices) {
					if ((vmask & vd.LocationMask) != 0) {
						throw new ArgumentException("Duplicate vertex location in descriptions", nameof(vertices));
					}
					vmask |= vd.LocationMask;
				}
				if (vmask != Layout.VertexLocationMask) {
					throw new ArgumentException(
						$"Missing vertex location in material descriptions ({vmask:X8} != {Layout.VertexLocationMask:X8})",
						nameof(vertices));
				}
			}
		}
		~Material()
		{
			dispose(false);
		}

		#region Derivatives
		/// <summary>
		/// Create a derivative material, which uses the same shader and pipeline parameters, but a different set of
		/// vertex descriptions.
		/// </summary>
		/// <param name="vertices">The new vertex descriptions to use for the material.</param>
		/// <returns>The new derivative material type.</returns>
		public Material CreateDerivative(params VertexDescription[] vertices) => new(Layout, Program, Params, vertices);

		/// <summary>
		/// Create a derivative material, which uses the same pipeline parameters and vertex descriptions, but a
		/// different shader program.
		/// </summary>
		/// <param name="shader">The new shader program to use for the material.</param>
		/// <returns>The new derivative material type.</returns>
		public Material CreateDerivative(Shader shader) => new(shader, Params, _vertices);

		/// <summary>
		/// Create a derivative material, which uses the same shader and vertex descriptions, but with different
		/// parameters.
		/// </summary>
		/// <param name="params">The new pipeline parameters to use for the material.</param>
		/// <returns>The new derivative material type.</returns>
		public Material CreateDerivative(in MaterialParams @params) => new(Layout, Program, @params, _vertices);

		/// <summary>
		/// Create a derivative material, which uses the same shader and vertex descriptions, but with optionally
		/// different parameters.
		/// </summary>
		/// <param name="topology">The new topology to use, or <c>null</c> to use the same topology.</param>
		/// <param name="winding">The new winding to use, or <c>null</c> to use the same winding.</param>
		/// <param name="restartEnable">The new primitive stream restart value, or <c>null</c> to use the same value.</param>
		/// <returns>The new derivative material type.</returns>
		public Material CreateDerivative(Topology? topology = null, Winding? winding = null, bool? restartEnable = null)
			=> CreateDerivative(new MaterialParams(
				topology ?? Params.Topology, winding ?? Params.Winding, restartEnable ?? Params.RestartEnabled
			));
		#endregion // Derivatives

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed) {
				if (disposing) {
					Layout?.DecRefCount();
					Program?.DecRefCount();
				}
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
