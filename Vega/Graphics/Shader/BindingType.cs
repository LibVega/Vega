/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// Contains the full set of types that resource bindings in VSL shaders can take on.
	/// </summary>
	// Note: These values need to stay <= 255, or else the _bindingTypes in ShaderInfo needs to be changed.
	public enum BindingType
	{
		/// <summary>
		/// Combined <see cref="Texture1D"/> and <see cref="Sampler"/>.
		/// </summary>
		Sampler1D,
		/// <summary>
		/// Combined <see cref="Texture2D"/> and <see cref="Sampler"/>.
		/// </summary>
		Sampler2D,
		/// <summary>
		/// Combined <see cref="Texture3D"/> and <see cref="Sampler"/>.
		/// </summary>
		Sampler3D,
		/// <summary>
		/// Combined Texture1DArray and <see cref="Sampler"/>.
		/// </summary>
		Sampler1DArray,
		/// <summary>
		/// Combined Texture2DArray and <see cref="Sampler"/>.
		/// </summary>
		Sampler2DArray,
		/// <summary>
		/// Combined TextureCube and <see cref="Sampler"/>.
		/// </summary>
		SamplerCube,

		/// <summary>
		/// A StorageImage1D.
		/// </summary>
		Image1D,
		/// <summary>
		/// A StorageImage2D.
		/// </summary>
		Image2D,
		/// <summary>
		/// A StorageImage3D.
		/// </summary>
		Image3D,
		/// <summary>
		/// A StorageImage1DArray.
		/// </summary>
		Image1DArray,
		/// <summary>
		/// A StorageImage2DArray.
		/// </summary>
		Image2DArray,

		/// <summary>
		/// A StorageBuffer with StorageAccess.ReadOnly.
		/// </summary>
		ROBuffer,
		/// <summary>
		/// A StorageBuffer with StorageAccess.ReadWrite.
		/// </summary>
		RWBuffer,
		/// <summary>
		/// A TexelBuffer with StorageAccess.ReadOnly.
		/// </summary>
		ROTexels,
		/// <summary>
		/// A TexelBuffer with StorageAccess.ReadWrite.
		/// </summary>
		RWTexels
	}
}
