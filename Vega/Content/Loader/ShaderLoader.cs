/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vega.Graphics;

namespace Vega.Content
{
	// Builtin type for loading Shader files
	internal sealed class ShaderLoader : ContentLoaderBase<Shader>
	{
		// Just call down to Shader.LoadFile
		public override Shader Load(string fullPath) => Shader.LoadFile(fullPath);
	}
}
