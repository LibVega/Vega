/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// Exception that is generated when an invalid shader is loaded.
	/// </summary>
	public sealed class InvalidShaderException : Exception
	{
		#region Fields
		/// <summary>
		/// The path of the shader file that is invalid, if the shader was loaded from a file.
		/// </summary>
		public readonly string? Path;
		#endregion // Fields

		internal InvalidShaderException(string? path, string message)
			: base((path is not null) 
				  ? $"Invalid shader '{path}' - {message}"
				  : $"Invalid shader - {message}")
		{
			Path = path;
		}

		internal InvalidShaderException(string? path, string message, Exception innerException)
			: base((path is not null)
				  ? $"Invalid shader '{path}' - {message}"
				  : $"Invalid shader - {message}", innerException)
		{
			Path = path;
		}
	}
}
