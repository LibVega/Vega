/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vega.Graphics;

namespace Vega.Content
{
	// Builtin type for loading ShaderProgram files
	internal sealed class ShaderProgramLoader : ContentLoaderBase<ShaderProgram>
	{
		// Just call down to ShaderProgram.LoadFile
		public override ShaderProgram Load(string fullPath) => ShaderProgram.LoadFile(fullPath);
	}
}
