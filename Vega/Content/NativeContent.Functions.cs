/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vega.Util;

namespace Vega.Content
{
	// Functions for the ContentLoader library
	internal unsafe static partial class NativeContent
	{
		// Library handle
		public static readonly NativeLibraryHandle Lib;

		// AUDIO
		private static readonly delegate* unmanaged<byte*, AudioError*, void*> _AudioOpenFile;
		private static readonly delegate* unmanaged<void*, void> _AudioCloseFile;
		private static readonly delegate* unmanaged<void*, AudioType> _AudioGetType;
		private static readonly delegate* unmanaged<void*, AudioError> _AudioGetError;
		private static readonly delegate* unmanaged<void*, ulong> _AudioGetFrameCount;
		private static readonly delegate* unmanaged<void*, uint> _AudioGetSampleRate;
		private static readonly delegate* unmanaged<void*, uint> _AudioGetChannelCount;
		private static readonly delegate* unmanaged<void*, ulong*, uint*, uint*, void> _AudioGetInfo;
		private static readonly delegate* unmanaged<void*, ulong> _AudioGetRemainingFrames;
		private static readonly delegate* unmanaged<void*, ulong, void*, ulong> _AudioReadFrames;

		// IMAGE
		private static readonly delegate* unmanaged<byte*, ImageError*, void*> _ImageOpenFile;
		private static readonly delegate* unmanaged<void*, void> _ImageCloseFile;
		private static readonly delegate* unmanaged<void*, ImageType> _ImageGetType;
		private static readonly delegate* unmanaged<void*, ImageError> _ImageGetError;
		private static readonly delegate* unmanaged<void*, uint*, uint*, void> _ImageGetSize;
		private static readonly delegate* unmanaged<void*, ImageChannels*, void> _ImageGetChannels;
		private static readonly delegate* unmanaged<void*, byte**, ImageChannels*, void> _ImageGetLoadedData;
		private static readonly delegate* unmanaged<void*, byte**, ImageChannels, uint> _ImageLoadData;

		// SPIRV
		private static readonly delegate* unmanaged<uint*, ulong, ReflectError*, void*> _SpirvCreateModule;
		private static readonly delegate* unmanaged<void*, ReflectError> _SpirvGetError;
		private static readonly delegate* unmanaged<void*, ReflectStage> _SpirvGetStage;
		private static readonly delegate* unmanaged<void*, byte*> _SpirvGetEntryPoint;
		private static readonly delegate* unmanaged<void*, uint> _SpirvGetDescriptorCount;
		private static readonly delegate* unmanaged<void*, uint> _SpirvGetInputCount;
		private static readonly delegate* unmanaged<void*, uint> _SpirvGetOutputCount;
		private static readonly delegate* unmanaged<void*, uint> _SpirvGetPushSize;
		private static readonly delegate* unmanaged<void*, uint, DescriptorInfo*, uint> _SpirvReflectDescriptor;
		private static readonly delegate* unmanaged<void*, void> _SpirvDestroyModule;

		// Loads the native library and functions
		static NativeContent()
		{
			// Load library
			Lib = NativeLibraryHandle.FromEmbedded(typeof(NativeContent).Assembly, "Vega.Lib.content", "content");
			var _ = Lib.Handle; // Force load handle

			// Load audio functions
			_AudioOpenFile = (delegate* unmanaged<byte*, AudioError*, void*>)Lib.LoadExport("vegaAudioOpenFile");
			_AudioCloseFile = (delegate* unmanaged<void*, void>)Lib.LoadExport("vegaAudioCloseFile");
			_AudioGetType = (delegate* unmanaged<void*, AudioType>)Lib.LoadExport("vegaAudioGetType");
			_AudioGetError = (delegate* unmanaged<void*, AudioError>)Lib.LoadExport("vegaAudioGetError");
			_AudioGetFrameCount = (delegate* unmanaged<void*, ulong>)Lib.LoadExport("vegaAudioGetFrameCount");
			_AudioGetSampleRate = (delegate* unmanaged<void*, uint>)Lib.LoadExport("vegaAudioGetSampleRate");
			_AudioGetChannelCount = (delegate* unmanaged<void*, uint>)Lib.LoadExport("vegaAudioGetChannelCount");
			_AudioGetInfo = (delegate* unmanaged<void*, ulong*, uint*, uint*, void>)Lib.LoadExport("vegaAudioGetInfo");
			_AudioGetRemainingFrames = (delegate* unmanaged<void*, ulong>)Lib.LoadExport("vegaAudioGetRemainingFrames");
			_AudioReadFrames = (delegate* unmanaged<void*, ulong, void*, ulong>)Lib.LoadExport("vegaAudioReadFrames");

			// Load image functions
			_ImageOpenFile = (delegate* unmanaged<byte*, ImageError*, void*>)Lib.LoadExport("vegaImageOpenFile");
			_ImageCloseFile = (delegate* unmanaged<void*, void>)Lib.LoadExport("vegaImageCloseFile");
			_ImageGetType = (delegate* unmanaged<void*, ImageType>)Lib.LoadExport("vegaImageGetType");
			_ImageGetError = (delegate* unmanaged<void*, ImageError>)Lib.LoadExport("vegaImageGetError");
			_ImageGetSize = (delegate* unmanaged<void*, uint*, uint*, void>)Lib.LoadExport("vegaImageGetSize");
			_ImageGetChannels = (delegate* unmanaged<void*, ImageChannels*, void>)Lib.LoadExport("vegaImageGetChannels");
			_ImageGetLoadedData = (delegate* unmanaged<void*, byte**, ImageChannels*, void>)Lib.LoadExport("vegaImageGetLoadedData");
			_ImageLoadData = (delegate* unmanaged<void*, byte**, ImageChannels, uint>)Lib.LoadExport("vegaImageLoadData");

			// Load SPIRV functions
			_SpirvCreateModule = (delegate* unmanaged<uint*, ulong, ReflectError*, void*>)Lib.LoadExport("vegaSpirvCreateModule");
			_SpirvGetError = (delegate* unmanaged<void*, ReflectError>)Lib.LoadExport("vegaSpirvGetError");
			_SpirvGetStage = (delegate* unmanaged<void*, ReflectStage>)Lib.LoadExport("vegaSpirvGetStage");
			_SpirvGetEntryPoint = (delegate* unmanaged<void*, byte*>)Lib.LoadExport("vegaSpirvGetEntryPoint");
			_SpirvGetDescriptorCount = (delegate* unmanaged<void*, uint>)Lib.LoadExport("vegaSpirvGetDescriptorCount");
			_SpirvGetInputCount = (delegate* unmanaged<void*, uint>)Lib.LoadExport("vegaSpirvGetInputCount");
			_SpirvGetOutputCount = (delegate* unmanaged<void*, uint>)Lib.LoadExport("vegaSpirvGetOutputCount");
			_SpirvGetPushSize = (delegate* unmanaged<void*, uint>)Lib.LoadExport("vegaSpirvGetPushSize");
			_SpirvReflectDescriptor = (delegate* unmanaged<void*, uint, DescriptorInfo*, uint>)Lib.LoadExport("vegaSpirvReflectDescriptor");
			_SpirvDestroyModule = (delegate* unmanaged<void*, void>)Lib.LoadExport("vegaSpirvDestroyModule");
		}
	}
}
