/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Vega.Render; // Breaks the no Graphics->Render reference rule, but its okay in this case (just compat checks)
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Describes a shader program written in VSL, and its metadata.
	/// </summary>
	public unsafe sealed class Shader : ResourceBase
	{
		#region Fields
		/// <summary>
		/// The reflection information for this shader program.
		/// </summary>
		public readonly ShaderLayout Layout;

		/// <summary>
		/// The number of <see cref="Pipeline"/> instances that are actively using this shader.
		/// </summary>
		public uint RefCount => _refCount;
		private uint _refCount = 0;

		// The shader modules in the program
		internal readonly VkShaderModule VertexModule;
		internal readonly VkShaderModule FragmentModule;
		#endregion // Fields

		internal Shader(ShaderLayout info, VkShaderModule vertMod, VkShaderModule fragMod)
			: base(ResourceType.Shader)
		{
			Layout = info;
			Layout.IncRefCount();

			VertexModule = vertMod;
			FragmentModule = fragMod;
		}

		// Reference counting functions for pipelines
		internal void IncRef() => Interlocked.Increment(ref _refCount);
		internal void DecRef() => Interlocked.Decrement(ref _refCount);

		// Enumerates over the available shader modules
		internal IEnumerable<(VkShaderModule mod, ShaderStages stage)> EnumerateModules()
		{
			yield return (VertexModule, ShaderStages.Vertex);
			yield return (FragmentModule, ShaderStages.Fragment);
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

		#region ResourceBase
		protected override void OnDispose(bool disposing)
		{
			if (disposing && (_refCount != 0)) {
				throw new InvalidOperationException("Cannot dispose a shader that is in use");
			}

			if (Core.Instance is not null) {
				Graphics.Resources.QueueDestroy(this);
			}
			else {
				Destroy();
			}

			Layout.DecRefCount();
		}

		protected internal override void Destroy()
		{
			VertexModule.DestroyShaderModule(null);
			FragmentModule.DestroyShaderModule(null);
		}
		#endregion // ResourceBase

		#region Loading
		/// <summary>
		/// Loads a new shader program from a <em>compiled</em> VSL shader file.
		/// <para>
		/// The shader file must be a compiled VSL file (.vbc), <em>NOT</em> a raw shader source.
		/// </para>
		/// </summary>
		/// <param name="path">The path to the compiled file.</param>
		/// <returns>The loaded shader program.</returns>
		public static Shader LoadFile(string path)
		{
			try {
				if (!File.Exists(path)) {
					throw new InvalidShaderException(path, $"Shader file '{path}' does not exist");
				}
				using var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				VSL.LoadStream(path, file, out var info, out var vertMod, out var fragMod);
				return new(info, vertMod, fragMod);
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
				return new(info, vertMod, fragMod);
			}
			catch (InvalidShaderException) { throw; }
			catch (Exception e) {
				throw new InvalidShaderException(resName, e.Message, e);
			}
		}
		#endregion // Loading
	}
}
