/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Content
{
	// Maps to the AudioError enum in the ContentLoader library
	internal enum AudioError : uint
	{
		NoError = 0,			// Special no error state
		FileNotFound = 1,		// The file does not exist
		UnknownType = 2,		// The file is not a known audio type
		InvalidFile = 3,        // The file failed to open (most likely an invalid header)
		BadDataRead = 4,        // Reading samples failed (most likely corrupt frame data)
		ReadAtEnd = 5,          // Attempting to read a fully consumed file
		BadStateRead = 6,       // Attempting to read from a file object that is already errored
	}

	// Maps to the AudioType enum in the ContentLoader library
	internal enum AudioType : uint
	{
		Unknown = 0,            // Special unknown type signifying and error or uninitialized data
		Wav = 1,                // WAVE file
		Vorbis = 2,             // OGG/Vorbis file
		Flac = 3,               // Flac file
	}
}
