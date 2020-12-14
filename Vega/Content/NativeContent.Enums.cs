﻿/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vega.Graphics;

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

	// Maps to the ImageError enum in the ContentLoader library
	internal enum ImageError : uint
	{
		NoError = 0,            // Special no error state
		FileNotFound = 1,       // The file to open does not exist
		UnknownType = 2,        // The file is not a known image type
		InvalidFile = 3,        // The file failed to open (most likely an invalid header)
		InvalidChannels = 4,    // The file has an unsupported channel count
		BadDataRead = 5,        // Reading samples failed (most likely corrupt image data)
		BadStateRead = 6,       // Attempting to read from a file object that is already errored
	}

	// Maps to the ImageType enum in the ContentLoader library
	internal enum ImageType : uint
	{
		Unknown = 0,			// Special unknown type signifying an error or uninitialized data
		Jpeg = 1,				// JPEG file
		Png = 2,				// PNG file
		Tga = 3,				// TGA file
		Bmp = 4,				// BMP file
	}

	// Maps to the ImageChannels enum in the ContentLoader library
	internal enum ImageChannels : uint
	{
		Unknown = 0,			// Unknown or unsupported channel data
		Gray = 1,				// Grayscale color data only
		GrayAlpha = 2,			// Grayscale color data with alpha
		RGB = 3,				// RGB color data
		RGBA = 4				// RBG color data with alpha
	}

	// Maps to ReflectError
	internal enum ReflectError : uint
	{
		None = 0,                    // No error
		NullModule = 1,              // Special public API error for passing a null module
		InvalidBytecode = 2,         // Bad bytecode or other parsing error
		InvalidStage = 3,            // Unsupported shader stage
		MultiplePushBlocks = 4,      // Too many push blocks (>1)
		MultipleEntryPoints = 5,     // Module has more than one entry point
		UnsupportedBindingType = 6,  // The descriptor type is unknown or unsupported
		InvalidBindingType = 7,      // The descriptor type is invalid for the set it appears in
		InvalidImageDims = 8,        // Invalid or unsupported image dims (includes multi-sampled)
		BindingSetOutOfRange = 9,    // A descriptor is bound to an invalid set index (>= VEGA_MAX_SET_COUNT)
		BindingSlotOutOfRange = 10,  // A descriptor is bound to an invalid slot index (>= VEGA_MAX_PER_SET_SLOTS)
	}

	// Maps to ReflectStage
	internal enum ReflectStage : uint
	{
		Invalid = 0,
		Vertex = 1,
		TessControl = 2,
		TessEval = 3,
		Geometry = 4,
		Fragment = 5
	}

	// Maps to BindingSet
	internal enum BindingSet : uint
	{
		Buffer = 0,            // Buffer objects set (=0)
		ReadOnlyTexel = 1,     // Read-Only texel objects set (=1)
		ReadWriteTexel = 2,    // Read/Write texel objects set (=2)
		InputAttachments = 3,  // Input attachments set (=3)
	}

	// Maps to BindingType
	internal enum BindingType : uint
	{
		Unknown = 0,
		Sampler = 1,
		CombinedImageSampler = 2,
		SampledImage = 3,
		StorageImage = 4,
		UniformTexelBuffer = 5,
		StorageTexelBuffer = 6,
		UniformBuffer = 7,
		StorageBuffer = 8,
		InputAttachment = 9
	}

	// Maps to ImageDims
	internal enum ImageDims : uint
	{
		Unknown = 0,
		E1D = 1,
		E1DArray = 2,
		E2D = 3,
		E2DArray = 4,
		E3D = 5,
		Cube = 6,
		CubeArray = 7,
		Buffer = 8,        // The image is a uniform texel buffer or storage texel buffer
		SubpassInput = 9,  // The image is a subpass input attachment
	}

	// Enum utilities
	internal static class NativeContentEnumUtils
	{
		// ReflectStage -> ShaderStages
		public static ShaderStages ToShaderStages(this ReflectStage stage) => stage switch {
			ReflectStage.Vertex => ShaderStages.Vertex,
			ReflectStage.TessControl => ShaderStages.TessControl,
			ReflectStage.TessEval => ShaderStages.TessEval,
			ReflectStage.Geometry => ShaderStages.Geometry,
			ReflectStage.Fragment => ShaderStages.Fragment,
			_ => ShaderStages.None
		};
	}
}
