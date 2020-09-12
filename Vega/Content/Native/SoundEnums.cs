/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Content
{
	// Mapping to the SoundError enum type in the native content library
	internal enum SoundError
	{
		NoError = 0,
		FileNotFound = 1,
		UnknownType = 2,
		InvalidFile = 3
	}

	// Mapping to the SoundFileHandle::FileType enum
	internal enum SoundFileType
	{
		Unknown = 0,
		Wav = 1,
		Flac = 2,
		Vorbis = 3
	}
}
