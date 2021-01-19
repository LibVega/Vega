/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.IO;
using System.Linq;
using Vega.Render; // Breaks the no Graphics->Render reference rule, but its okay in this case (just compat checks)

namespace Vega.Graphics
{
	/// <summary>
	/// Describes a shader program written in VSL, and its metadata.
	/// </summary>
	public unsafe sealed class Shader : IDisposable
	{
		#region Fields
		/// <summary>
		/// The reflection information for this shader program.
		/// </summary>
		public readonly ShaderLayout Layout;

		// The shader modules
		internal readonly ShaderProgram Program;

		/// <summary>
		/// Object disposal flag.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		internal Shader(ShaderLayout info, ShaderProgram program)
		{
			Layout = info;
			Layout.IncRefCount();

			Program = program;
			Program.IncRefCount();
		}
		~Shader()
		{
			dispose(false);
		}

		// Validation against pipelines
		internal string? CheckCompatiblity(PipelineDescription desc, Renderer renderer, uint subpassIndex)
		{
			ref readonly var subpass = ref renderer.Layout.Subpasses[subpassIndex];

			// Check fragment outputs
			if (subpass.ColorCount != Layout.FragmentOutputs.Count) {
				return "color attachment count mismatch";
			}
			var outputs = renderer.Layout.Attachments
				.Where(att => att.Uses[subpassIndex] == (byte)AttachmentUse.Output)
				.ToArray();
			for (int i = 0; i < subpass.ColorCount; ++i) {
				ref readonly var output = ref outputs[i];
				if (!Layout.FragmentOutputs[i].Format.IsConvertible(output.Format)) {
					return $"incompatible formats for fragment output {i} (data: {output.Format}) " +
						$"(shader: {Layout.FragmentOutputs[i].Format})";
				}
			}

			// Check input attachments
			if (subpass.InputCount != Layout.SubpassInputs.Count) {
				return "input attachment count mismatch";
			}
			var spinputs = renderer.Layout.Attachments
				.Where(att => att.Uses[subpassIndex] == (byte)AttachmentUse.Input)
				.ToArray();
			for (int i = 0; i < subpass.InputCount; ++i) {
				ref readonly var spi = ref spinputs[i];
				if (!Layout.SubpassInputs[i].Format.IsConvertible(spi.Format)) {
					return $"incompatible formats for subpass input {i} (data: {spi.Format}) " +
						$"(shader: {Layout.SubpassInputs[i].Format})";
				}
			}

			// Check vertex inputs
			var allVertex = desc.VertexDescriptions!.SelectMany(desc => desc.EnumerateElements()).ToArray();
			if (allVertex.Length != Layout.VertexInputs.Count) {
				return "vertex attribute count mismatch";
			}
			foreach (var velem in allVertex) {
				var input = Layout.GetVertexInput(velem.slot);
				if (!input.HasValue) {
					return $"shader does not consume vertex attribute at index {velem.slot}";
				}
				if (!velem.element.Format.IsLoadableAs(input.Value.Format)) {
					return $"incompatible types for vertex attribute {velem.slot} (data: {velem.element.Format}) " +
						$"(shader: {input.Value.Format})";
				}
				if (input.Value.ArraySize != velem.element.ArraySize) {
					return $"array size mismatch for vertex attribute {velem.slot}";
				}
			}

			return null;
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed) {
				Layout.DecRefCount();
				Program.DecRefCount();
			}
			IsDisposed = true;
		}
		#endregion // IDisposable

		#region Loading
		/// <summary>
		/// Loads a new shader from a <em>compiled</em> VSL shader file.
		/// <para>
		/// The shader file must be a compiled VSL file (.vbc), <em>NOT</em> a raw shader source.
		/// </para>
		/// </summary>
		/// <param name="path">The path to the compiled file.</param>
		/// <returns>The loaded shader.</returns>
		public static Shader LoadFile(string path)
		{
			try {
				if (!File.Exists(path)) {
					throw new InvalidShaderException(path, $"Shader file '{path}' does not exist");
				}
				using var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				VSL.LoadStream(path, file, out var info, out var vertMod, out var fragMod);
				var program = new ShaderProgram(vertMod, null, null, null, fragMod);
				return new(info, program);
			}
			catch (InvalidShaderException) { throw; }
			catch (Exception e) {
				throw new InvalidShaderException(path, e.Message, e);
			}
		}

		// Loads an embedded resource shader
		internal static Shader LoadInternalResource(string resName)
		{
			try {
				var asm = typeof(Shader).Assembly.GetManifestResourceStream(resName);
				if (asm is null) {
					throw new InvalidShaderException(resName, $"Failed to load embedded shader '{resName}'");
				}
				VSL.LoadStream(resName, asm, out var info, out var vertMod, out var fragMod);
				var program = new ShaderProgram(vertMod, null, null, null, fragMod);
				return new(info, program);
			}
			catch (InvalidShaderException) { throw; }
			catch (Exception e) {
				throw new InvalidShaderException(resName, e.Message, e);
			}
		}
		#endregion // Loading
	}
}
