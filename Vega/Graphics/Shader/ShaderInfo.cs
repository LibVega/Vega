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
		#endregion // Fields

		internal ShaderInfo(
			ShaderStages stages,
			VertexInput[] vertexInputs,
			FragmentOutput[] fragmentOutputs,
			uint uniformSize, ShaderStages uniformStages, UniformMember[] uniformMembers
		)
		{
			Stages = stages;

			_vertexInputs = vertexInputs;

			_fragmentOutputs = fragmentOutputs;

			UniformSize = uniformSize;
			UniformStages = uniformStages;
			_uniformMembers = uniformMembers;
		}

		#region Stage Info
		/// <summary>
		/// Gets if the shader has all of the stages in the passed stage mask.
		/// </summary>
		/// <param name="stages">The mask of stages to check for.</param>
		public bool HasStages(ShaderStages stages) => (Stages & stages) == stages;
		#endregion // Stage Info
	}
}
