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
		/// The output description for the material, describing how the renderer attachments are updated.
		/// </summary>
		public readonly MaterialOutput Output;

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
		public Material(Shader shader, MaterialInput input, MaterialOutput output)
			: this(shader.Layout, shader.Program, input, output)
		{ }
		// Internal constructor
		internal Material(ShaderLayout layout, ShaderProgram program, MaterialInput input, MaterialOutput output)
		{
			// Assign objects
			Layout = layout;
			Program = program;
			Layout.IncRefCount();
			Program.IncRefCount();
			Input = input;
			Output = output;

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

			// Validate outputs
			if (output.BlendStates.Count != layout.FragmentOutputs.Count) {
				throw new ArgumentException("Output count mismatch", nameof(output));
			}
		}
		~Material()
		{
			dispose(false);
		}

		#region Derivatives
		/// <summary>
		/// Create a derivative material with a different shader.
		/// </summary>
		/// <param name="shader">The new shader program to use.</param>
		/// <returns>The new derivative material type.</returns>
		public Material CreateDerivative(Shader shader) => new(shader, Input, Output);

		/// <summary>
		/// Create a derivative material with a different input state.
		/// </summary>
		/// <param name="params">The new pipeline parameters to use.</param>
		/// <returns>The new derivative material type.</returns>
		public Material CreateDerivative(MaterialInput input) => new(Layout, Program, input, Output);

		/// <summary>
		/// Create a derivative material with a different primitive topology.
		/// </summary>
		/// <param name="topology">The new topology to use.</param>
		/// <returns>The new derivative material type.</returns>
		public Material CreateDerivative(Topology topology) => 
			new(Layout, Program, Input with { Topology = topology }, Output);

		/// <summary>
		/// Create a derivative material with a different set of vertex descriptions.
		/// </summary>
		/// <param name="vertices">The new vertex descriptions to use.</param>
		/// <returns>The new derivative material type.</returns>
		public Material CreateDerivative(params VertexDescription[] vertices) =>
			new(Layout, Program, Input with { Vertices = vertices }, Output);

		/// <summary>
		/// Create a derived material with a different output state.
		/// </summary>
		/// <param name="output">The new output state to use.</param>
		/// <returns>The new derivative material type.</returns>
		public Material CreateDerivative(MaterialOutput output) => new(Layout, Program, Input, output);

		/// <summary>
		/// Create a derived material with a different depth state.
		/// </summary>
		/// <param name="depthState">The new depth state to use.</param>
		/// <returns>The new derivative material type.</returns>
		public Material CreateDerivative(DepthState depthState) =>
			new(Layout, Program, Input, Output with { DepthState = depthState });

		/// <summary>
		/// Create a derivative material with a different set of blend states.
		/// </summary>
		/// <param name="blendStates">The new blend states to use as the output.</param>
		/// <returns>The new derivative material type.</returns>
		public Material CreateDerivative(params BlendState[] blendStates) =>
			new(Layout, Program, Input, Output with { BlendStates = blendStates });
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
