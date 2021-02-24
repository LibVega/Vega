/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
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
		/// The input description for the material, describing how vertex data is read and interpreted.
		/// </summary>
		public readonly MaterialInput Input;

		/// <summary>
		/// Object disposal flag.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		/// <summary>
		/// Describes a new material from a shader program and a specific set of vertex descriptions.
		/// </summary>
		/// <param name="shader">The shader program to process the material with.</param>
		/// <param name="input">The vertex input topology for the material.</param>
		/// <param name="vertices">The descriptions for the vertex inputs for the material.</param>
		public Material(Shader shader, MaterialInput input)
			: this(shader.Layout, shader.Program, input)
		{ }
		// Internal constructor
		internal Material(ShaderLayout layout, ShaderProgram program, MaterialInput input)
		{
			// Assign objects
			Layout = layout;
			Program = program;
			Layout.IncRefCount();
			Program.IncRefCount();
			Input = input;

			// Validate vertex inputs
			if (input.Vertices.Count > 0) {
				uint vmask = 0;
				foreach (var vd in input.Vertices) {
					if ((vmask & vd.LocationMask) != 0) {
						throw new ArgumentException("Duplicate vertex location in descriptions", nameof(input));
					}
					vmask |= vd.LocationMask;
				}
				if (vmask != Layout.VertexLocationMask) {
					throw new ArgumentException(
						$"Missing vertex location in material descriptions ({vmask:X8} != {Layout.VertexLocationMask:X8})",
						nameof(input));
				}
			}
		}
		~Material()
		{
			dispose(false);
		}

		#region Derivatives
		/// <summary>
		/// Create a derivative material, which uses the same shader and input parameters, but a different set of
		/// vertex descriptions.
		/// </summary>
		/// <param name="vertices">The new vertex descriptions to use for the material.</param>
		/// <returns>The new derivative material type.</returns>
		public Material CreateDerivative(params VertexDescription[] vertices) => 
			new(Layout, Program, Input with { Vertices = vertices });

		/// <summary>
		/// Create a derivative material, which uses the same input, but a different shader program.
		/// </summary>
		/// <param name="shader">The new shader program to use for the material.</param>
		/// <returns>The new derivative material type.</returns>
		public Material CreateDerivative(Shader shader) => new(shader, Input);

		/// <summary>
		/// Create a derivative material, which uses the same shader, but with a different input description.
		/// </summary>
		/// <param name="params">The new pipeline parameters to use for the material.</param>
		/// <returns>The new derivative material type.</returns>
		public Material CreateDerivative(MaterialInput input) => new(Layout, Program, input);
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
