/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vega.Content;

namespace Vega.Graphics.Reflection
{
	/// <summary>
	/// Provides information about a specific resource binding point within a <see cref="ShaderModule"/>.
	/// </summary>
	public sealed class BindingInfo
	{
		#region Fields
		/// <summary>
		/// The name of the binding point within the shader.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The resource type for the binding point.
		/// </summary>
		public readonly BindingType Type;
		/// <summary>
		/// The binding slot index within the binding set.
		/// </summary>
		public readonly uint Slot;
		/// <summary>
		/// The length of the array. Null if the binding is not an array, and <see cref="UInt32.MaxValue"/> if the
		/// array is dynamically sized or runtime sized.
		/// </summary>
		public readonly uint? ArraySize;
		/// <summary>
		/// The size of the block in bytes, for uniform buffer types. Null otherwise.
		/// </summary>
		public readonly uint? BlockSize;
		/// <summary>
		/// The texture dimension type for the binding, for texture/sampler/image types. Null otherwise.
		/// </summary>
		public readonly TextureDims? TextureDims;
		#endregion // Fields

		internal BindingInfo(string name, BindingType type, uint slot, uint? arrSize, uint? blockSize, 
			TextureDims? dims)
		{
			Name = name;
			Type = type;
			Slot = slot;
			ArraySize = arrSize;
			BlockSize = blockSize;
			TextureDims = dims;
		}

		public override string ToString() => $"[{Slot}:{Type}{(ArraySize.HasValue ? $"[{ArraySize.Value}]" : "")}]";

		/// <summary>
		/// Checks if this binding info is compatible with the passed one.
		/// <para>
		/// Compatibility checks the type, array size, block size, and texture dims.
		/// </para>
		/// </summary>
		/// <param name="other">The binding to check compatibility with.</param>
		public bool IsCompatible(BindingInfo other) =>
			(Type == other.Type) &&
			(ArraySize.HasValue == other.ArraySize.HasValue) &&
			(ArraySize.GetValueOrDefault(0) == other.ArraySize.GetValueOrDefault(0)) &&
			(BlockSize.HasValue == other.BlockSize.HasValue) &&
			(BlockSize.GetValueOrDefault(0) == other.BlockSize.GetValueOrDefault(0)) &&
			(TextureDims.HasValue == other.TextureDims.HasValue) &&
			(TextureDims.GetValueOrDefault() == other.TextureDims.GetValueOrDefault());
	}
}
