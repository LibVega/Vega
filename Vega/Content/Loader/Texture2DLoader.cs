/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */


using System;
using Vega.Graphics;

namespace Vega.Content
{
	// Builtin type for loading images as Texture2D
	internal sealed class Texture2DLoader : ContentLoaderBase<Texture2D>
	{
		// Just call down to Texture2D.LoadFile, and create static usage texture
		public override Texture2D Load(string fullPath) => Texture2D.LoadFile(fullPath, TextureUsage.Static);
	}
}
