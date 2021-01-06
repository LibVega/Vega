/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.InteropServices;

namespace Vega.Content
{
	// Structs specific to the native content library
	internal unsafe static partial class NativeContent
	{
		// Maps to BindingInfo struct for SPIRV api (Pack = 1 to match pack(push, 1))
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct BindingInfo
		{
			public byte* Name;
			public BindingSet Set;
			public uint Slot;
			public BindingType Type;
			public uint ArraySize;
			public uint BlockSize;
			public ImageDims ImageDims;
		}
	}
}
