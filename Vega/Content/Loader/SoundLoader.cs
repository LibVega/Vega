/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */


using System;
using Vega.Audio;

namespace Vega.Content
{
	// Builtin type for loading audio files as sounds
	internal sealed class SoundLoader : ContentLoaderBase<Sound>
	{
		// Just call down to ShaderProgram.LoadFile
		public override Sound Load(string fullPath) => Sound.LoadFile(fullPath);
	}
}
