/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;

namespace Vega.Graphics
{
	/// <summary>
	/// Contains reflection information about a shader and its public interface.
	/// </summary>
	public sealed partial class ShaderInfo
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
		#endregion // Fields

		internal ShaderInfo(
			ShaderStages stages,
			VertexInput[] vertexInputs,
			FragmentOutput[] fragmentOutputs,
			Binding[] bindings,
			uint uniformSize, ShaderStages uniformStages, UniformMember[] uniformMembers,
			SubpassInput[] spInputs
		)
		{
			Stages = stages;

			_vertexInputs = vertexInputs;

			_fragmentOutputs = fragmentOutputs;

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

			UniformSize = uniformSize;
			UniformStages = uniformStages;
			_uniformMembers = uniformMembers;

			_subpassInputs = spInputs;
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
	}
}
