/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	// Manages the set of builtin samplers defined by the Sampler enum
	internal unsafe static class SamplerPool
	{
		// The maximum number of samplers, based on the Sampler num
		public const int MAX_SAMPLER_COUNT = (int)Sampler.LinearLinearTransparent + 1;

		#region Fields
		// The active graphics device
		private static GraphicsDevice? _GraphicsDevice;

		// The samplers
		private static readonly VkSampler?[] _Samplers = new VkSampler?[MAX_SAMPLER_COUNT];
		#endregion // Fields

		// Get or create the sampler object associated with the sampler type
		public static VkSampler? Get(Sampler samp) =>
			_Samplers[(int)samp] ?? (_Samplers[(int)samp] = null); // TODO: Create

		// Called by GraphicsDevice to initialize
		public static void Initialize(GraphicsDevice gd)
		{
			_GraphicsDevice = gd;
		}

		// Called by GraphicsDevice to terminate
		public static void Terminate()
		{
			foreach (var samp in _Samplers) {
				samp?.DestroySampler(null);
			}

			_GraphicsDevice = null;
		}
	}
}
