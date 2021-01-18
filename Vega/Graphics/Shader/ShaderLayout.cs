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
	/// Describes the layout of a shader program, including the public shader interface and other reflection info.
	/// </summary>
	public sealed unsafe partial class ShaderLayout : ResourceBase
	{
		#region Fields
		/// <summary>
		/// The mask of stages that are present in the shader.
		/// </summary>
		public readonly ShaderStages Stages;

		/// <summary>
		/// A list of the vertex input attributes in the shader.
		/// </summary>
		public IReadOnlyList<VertexInput> VertexInputs => _vertexInputs;
		private readonly VertexInput[] _vertexInputs;
		/// <summary>
		/// A bitmask of all attribute locations used by this shader.
		/// </summary>
		public readonly uint VertexLocationMask;

		/// <summary>
		/// A list of the fragment outputs in the shader.
		/// </summary>
		public IReadOnlyList<FragmentOutput> FragmentOutputs => _fragmentOutputs;
		private readonly FragmentOutput[] _fragmentOutputs;

		/// <summary>
		/// The bindings in the shader.
		/// </summary>
		public IReadOnlyList<Binding> Bindings => _bindings;
		private readonly Binding[] _bindings;
		/// <summary>
		/// A bitmask of bindings present in the shader.
		/// </summary>
		public readonly uint BindingMask;
		/// <summary>
		/// The highest used binding slot index in the shader.
		/// </summary>
		public readonly uint MaxBindingSlot;
		// A quick, compact reference to the binding types at different slots
		internal readonly byte[] BindingTypes = new byte[VSL.MAX_BINDING_COUNT];

		/// <summary>
		/// The size of the shader uniform data, in bytes.
		/// </summary>
		public readonly uint UniformSize;
		/// <summary>
		/// The mask of stages that access the uniform data.
		/// </summary>
		public readonly ShaderStages UniformStages;
		/// <summary>
		/// The members of the uniform struct type.
		/// </summary>
		public IReadOnlyList<UniformMember> UniformMembers => _uniformMembers;
		private readonly UniformMember[] _uniformMembers;
		/// <summary>
		/// Gets if the shader uses uniform values.
		/// </summary>
		public bool HasUniform => UniformSize > 0;

		/// <summary>
		/// The subpass inputs in the shader.
		/// </summary>
		public IReadOnlyList<SubpassInput> SubpassInputs => _subpassInputs;
		private readonly SubpassInput[] _subpassInputs;

		// Layout objects for the shader program
		internal readonly VkPipelineLayout PipelineLayout;
		internal readonly VkDescriptorSetLayout? SubpassInputLayout = null;

		// Reference counting
		internal uint RefCount => _refCount;
		private uint _refCount = 0;
		#endregion // Fields

		internal ShaderLayout(
			ShaderStages stages,
			VertexInput[] vertexInputs,
			FragmentOutput[] fragmentOutputs,
			Binding[] bindings,
			uint uniformSize, ShaderStages uniformStages, UniformMember[] uniformMembers,
			SubpassInput[] spInputs)
				: base(ResourceType.ShaderLayout)
		{
			// Top-level info
			Stages = stages;

			// Vertex info
			_vertexInputs = vertexInputs;
			VertexLocationMask = 0;
			foreach (var input in vertexInputs) {
				var loc = input.Location;
				var locnum = input.Format.GetBindingCount() * input.ArraySize;
				for (uint l = loc; l < (loc + locnum); ++l) {
					VertexLocationMask |= (1u << (int)l);
				}
			}
			
			// Fragment info
			_fragmentOutputs = fragmentOutputs;

			// Binding info
			_bindings = bindings;
			BindingMask = 0;
			Array.Fill(BindingTypes, (byte)0);
			MaxBindingSlot = 0;
			for (int i = 0; i < bindings.Length; ++i) {
				ref readonly var bind = ref bindings[i];
				BindingMask |= (1u << (int)bind.Slot);
				BindingTypes[bind.Slot] = (byte)bind.Type;
				if (bind.Slot > MaxBindingSlot) {
					MaxBindingSlot = bind.Slot;
				}
			}

			// Uniform info
			UniformSize = uniformSize;
			UniformStages = uniformStages;
			_uniformMembers = uniformMembers;

			// Subpass input info
			_subpassInputs = spInputs;

			// Create the layouts
			PipelineLayout = CreatePipelineLayout(Graphics, this, out SubpassInputLayout);
		}

		// Increment the number of objects referring to this layout
		internal void IncRefCount() => Interlocked.Increment(ref _refCount);
		// Decrement the number of objects referring to this layout, destroying at ref == 0
		internal void DecRefCount()
		{
			var newVal = Interlocked.Decrement(ref _refCount);
			if ((newVal == 0) && (Core.Instance is not null)) {
				Graphics.Resources.QueueDestroy(this);
			}
		}

		#region Vertex Info
		/// <summary>
		/// Gets the vertex input associated with the given slot index, or <c>null</c> if no input uses that slot.
		/// </summary>
		/// <param name="slot">The vertex input slot index to check.</param>
		public VertexInput? GetVertexInput(uint slot)
		{
			foreach (var vin in VertexInputs) {
				var rangeEnd = vin.Location + (vin.Format.GetBindingCount() * vin.ArraySize);
				if ((slot >= vin.Location) && (slot < rangeEnd)) {
					return vin;
				}
			}
			return null;
		}
		#endregion // Vertex Info

		#region Stage Info
		/// <summary>
		/// Gets if the shader has all of the stages in the passed stage mask.
		/// </summary>
		/// <param name="stages">The mask of stages to check for.</param>
		public bool HasStages(ShaderStages stages) => (Stages & stages) == stages;
		#endregion // Stage Info

		#region Binding Info
		/// <summary>
		/// Gets if the shader has a resource binding in the given slot.
		/// </summary>
		/// <param name="slot">The slot to check.</param>
		public bool HasBinding(uint slot) => (BindingMask & (1u << (int)slot)) > 0;
		/// <summary>
		/// Gets the binding type in the given slot, or <c>null</c> if the given slot has no binding.
		/// </summary>
		/// <param name="slot">The slot to check.</param>
		public BindingType? GetBindingType(uint slot) =>
			((BindingMask & (1u << (int)slot)) > 0) ? (BindingType)BindingTypes[slot] : null;
		#endregion // Binding Info

		#region ResourceBase
		/// <summary>
		/// Do <em><b>NOT</b></em> call Dispose() on ShaderLayout directly.
		/// </summary>
		public override void Dispose() =>
			throw new InvalidOperationException("Cannot call Dispose() on ShaderLayout directly");

		// No-op for this resource
		protected override void OnDispose(bool disposing) { }

		protected internal override void Destroy()
		{
			PipelineLayout?.DestroyPipelineLayout(null);
			SubpassInputLayout?.DestroyDescriptorSetLayout(null);
		}
		#endregion // ResourceBase

		#region Layouts
		// Creates a pipeline layout object from the given info
		private static VkPipelineLayout CreatePipelineLayout(GraphicsDevice gd, ShaderLayout info,
			out VkDescriptorSetLayout? subpassLayout)
		{
			// Get layout info, and create subpass layout if needed
			var layouts = stackalloc VulkanHandle<VkDescriptorSetLayout>[3];
			uint layoutCount = 1;
			layouts[0] = info.Graphics.BindingTable.LayoutHandle;
			if (info.UniformSize > 0) {
				layouts[layoutCount++] = info.Graphics.BindingTable.UniformLayoutHandle;
			}
			if (info.SubpassInputs.Count > 0) {
				if (info.UniformSize == 0) {
					layouts[layoutCount++] = info.Graphics.BindingTable.BlankLayoutHandle;
				}
				subpassLayout = CreateSubpassInputLayout(info.Graphics, info);
				layouts[layoutCount++] = subpassLayout;
			}
			else {
				subpassLayout = null;
			}

			// Create the pipline layout
			VkPushConstantRange pcr = new(
				stageFlags: (VkShaderStageFlags)info.Stages,
				offset: 0,
				size: ((info.MaxBindingSlot + 2) / 2) * 4
			);
			VkPipelineLayoutCreateInfo plci = new(
				flags: VkPipelineLayoutCreateFlags.NoFlags,
				setLayoutCount: layoutCount,
				setLayouts: layouts,
				pushConstantRangeCount: (info.BindingMask == 0) ? 0 : 1, // No bindings = no push constant indices
				pushConstantRanges: &pcr
			);
			VulkanHandle<VkPipelineLayout> layoutHandle;
			info.Graphics.VkDevice.CreatePipelineLayout(&plci, null, &layoutHandle)
				.Throw("Failed to create shader pipeline layout");
			return new(layoutHandle, info.Graphics.VkDevice);
		}

		// Creates a descriptor set layout matching the subpass inputs for the shader
		private static VkDescriptorSetLayout CreateSubpassInputLayout(GraphicsDevice gd, ShaderLayout info)
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
		#endregion // Layouts
	}
}
