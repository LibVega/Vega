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
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Describes a shader program written in VSL, and its metadata.
	/// </summary>
	public unsafe sealed class ShaderProgram : ResourceBase
	{
		#region Fields
		/// <summary>
		/// The reflection information for this shader program.
		/// </summary>
		public readonly ShaderInfo Info;

		/// <summary>
		/// The number of <see cref="Pipeline"/> instances that are actively using this shader.
		/// </summary>
		public uint RefCount => _refCount;
		private uint _refCount = 0;

		// The shader modules in the program
		internal readonly VkShaderModule VertexModule;
		internal readonly VkShaderModule FragmentModule;

		// The pipeline layout for this shader
		internal readonly VkPipelineLayout PipelineLayout;

		// The descriptor set layout for subpass inputs in this shader
		internal readonly VkDescriptorSetLayout? SubpassInputLayout = null;
		#endregion // Fields

		internal ShaderProgram(ShaderInfo info, VkShaderModule vertMod, VkShaderModule fragMod)
			: base(ResourceType.Shader)
		{
			Info = info;
			VertexModule = vertMod;
			FragmentModule = fragMod;

			var gd = Core.Instance!.Graphics;

			// Get the binding layouts
			var layouts = stackalloc VulkanHandle<VkDescriptorSetLayout>[3];
			uint layoutCount = 1;
			layouts[0] = gd.BindingTable.LayoutHandle;
			if (info.UniformSize > 0) {
				layouts[layoutCount++] = gd.BindingTable.UniformLayoutHandle;
			}
			if (info.SubpassInputs.Count > 0) {
				if (info.UniformSize == 0) {
					layouts[layoutCount++] = gd.BindingTable.BlankLayoutHandle;
				}
				SubpassInputLayout = CreateSubpassInputLayout(gd, info);
				layouts[layoutCount++] = SubpassInputLayout;
			}

			// Describe push constants for binding indices
			VkPushConstantRange pcr = new(
				stageFlags: (VkShaderStageFlags)info.Stages,
				offset: 0,
				size: ((info.MaxBindingSlot + 2) / 2) * 4
			);

			// Create the pipeline layout
			VkPipelineLayoutCreateInfo plci = new(
				flags: VkPipelineLayoutCreateFlags.NoFlags,
				setLayoutCount: layoutCount,
				setLayouts: layouts,
				pushConstantRangeCount: (info.BindingMask == 0) ? 0 : 1, // No bindings = no push constant indices
				pushConstantRanges: &pcr
			);
			VulkanHandle<VkPipelineLayout> layoutHandle;
			gd.VkDevice.CreatePipelineLayout(&plci, null, &layoutHandle)
				.Throw("Failed to create shader pipeline layout");
			PipelineLayout = new(layoutHandle, gd.VkDevice);
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
			if (subpass.ColorCount != Info.FragmentOutputs.Count) {
				return "color attachment count mismatch";
			}
			var outputs = renderer.Layout.Attachments
				.Where(att => att.Uses[subpassIndex] == (byte)AttachmentUse.Output)
				.ToArray();
			for (int i = 0; i < subpass.ColorCount; ++i) {
				ref readonly var output = ref outputs[i];
				if (!Info.FragmentOutputs[i].Format.IsConvertible(output.Format)) {
					return $"incompatible formats for fragment output {i} (data: {output.Format}) " +
						$"(shader: {Info.FragmentOutputs[i].Format})";
				}
			}

			// Check input attachments
			if (subpass.InputCount != Info.SubpassInputs.Count) {
				return "input attachment count mismatch";
			}
			var spinputs = renderer.Layout.Attachments
				.Where(att => att.Uses[subpassIndex] == (byte)AttachmentUse.Input)
				.ToArray();
			for (int i = 0; i < subpass.InputCount; ++i) {
				ref readonly var spi = ref spinputs[i];
				if (!Info.SubpassInputs[i].Format.IsConvertible(spi.Format)) {
					return $"incompatible formats for subpass input {i} (data: {spi.Format}) " +
						$"(shader: {Info.SubpassInputs[i].Format})";
				}
			}

			// Check vertex inputs
			var allVertex = desc.VertexDescriptions!.SelectMany(desc => desc.EnumerateElements()).ToArray();
			if (allVertex.Length != Info.VertexInputs.Count) {
				return "vertex attribute count mismatch";
			}
			foreach (var velem in allVertex) {
				var input = Info.GetVertexInput(velem.slot);
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
				Core.Instance!.Graphics.Resources.QueueDestroy(this);
			}
			else {
				Destroy();
			}
		}

		protected internal override void Destroy()
		{
			PipelineLayout.DestroyPipelineLayout(null);
			SubpassInputLayout?.DestroyDescriptorSetLayout(null);

			VertexModule.DestroyShaderModule(null);
			FragmentModule.DestroyShaderModule(null);
		}
		#endregion // ResourceBase

		// Creates a descriptor set layout matching the subpass inputs for the shader
		private static VkDescriptorSetLayout CreateSubpassInputLayout(GraphicsDevice gd, ShaderInfo info)
		{
			// Setup the layouts
			var spiCount = info.SubpassInputs.Count;
			var layouts = stackalloc VkDescriptorSetLayoutBinding[spiCount];
			for (int i = 0; i < spiCount; ++i) {
				layouts[i] = new(
					(uint)i, VkDescriptorType.InputAttachment, 1, VkShaderStageFlags.Fragment, null
				);
			}

			// Create the set layout
			VkDescriptorSetLayoutCreateInfo dslci = new(
				flags: VkDescriptorSetLayoutCreateFlags.NoFlags,
				bindingCount: (uint)spiCount,
				bindings: layouts
			);
			VulkanHandle<VkDescriptorSetLayout> handle;
			gd.VkDevice.CreateDescriptorSetLayout(&dslci, null, &handle)
				.Throw("Failed to create subpass input layout for shader");
			return new(handle, gd.VkDevice);
		}

		#region Loading
		/// <summary>
		/// Loads a new shader program from a <em>compiled</em> VSL shader file.
		/// <para>
		/// The shader file must be a compiled VSL file (.vbc), <em>NOT</em> a raw shader source.
		/// </para>
		/// </summary>
		/// <param name="path">The path to the compiled file.</param>
		/// <returns>The loaded shader program.</returns>
		public static ShaderProgram LoadFile(string path)
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
		internal static ShaderProgram LoadInternalResource(string resName)
		{
			try {
				var asm = typeof(ShaderProgram).Assembly.GetManifestResourceStream(resName);
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
