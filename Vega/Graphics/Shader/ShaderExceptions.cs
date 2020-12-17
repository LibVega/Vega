/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vega.Content;

namespace Vega.Graphics
{
	/// <summary>
	/// Represents an attempt to create a <see cref="ShaderModule"/> with invalid SPIR-V bytecode.
	/// </summary>
	public sealed class InvalidSpirvException : Exception
	{
		internal InvalidSpirvException()
			: base("Shader bytecode contained invalid SPIR-V instructions")
		{ }
	}

	/// <summary>
	/// Represents an invalid descriptor binding for a shader program.
	/// </summary>
	public sealed class InvalidBindingException : Exception
	{
		#region Fields
		/// <summary>
		/// The set index of the invalid binding ("set" value of <c>layout(...)</c>).
		/// </summary>
		public readonly uint Set;
		/// <summary>
		/// The slot index of the invalid binding ("binding" value of <c>layout(...)</c>).
		/// </summary>
		public readonly uint Slot;
		#endregion // Fields

		internal InvalidBindingException(uint set, uint slot, string message)
			: base($"Invalid binding {set}.{slot} - {message}")
		{
			Set = set;
			Slot = slot;
		}
	}

	/// <summary>
	/// Represents a general structural or interface error for a <see cref="ShaderModule"/>. This is distinct from
	/// invalid shader code, which is handled by <see cref="InvalidSpirvException"/>.
	/// </summary>
	public sealed class InvalidModuleException : Exception
	{
		#region Fields
		// Specific error code
		internal readonly NativeContent.ReflectError? Error;
		#endregion // Fields

		internal InvalidModuleException(NativeContent.ReflectError error)
			: base($"Invalid shader module - {error.GetErrorText()}")
		{
			Error = error;
		}

		internal InvalidModuleException(string error)
			: base($"Invalid shader module - {error}")
		{
			Error = null;
		}
	}

	/// <summary>
	/// Represents an error from attempting to construct a <see cref="Shader"/> using <see cref="ShaderModule"/>s that
	/// have incompatible bindings or push constants.
	/// </summary>
	public sealed class IncompatibleModuleException : Exception
	{
		#region Fields
		/// <summary>
		/// The shader stage module that is incompatible with the existing modules.
		/// </summary>
		public readonly ShaderStages BadStage;
		#endregion // Fields

		internal IncompatibleModuleException(ShaderStages stage, string message)
			: base($"Invalid {stage} module - {message}")
		{
			BadStage = stage;
		}
	}
}
