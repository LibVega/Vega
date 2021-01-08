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

		// Sampler access lock
		private static FastMutex _SamplerMutex = new();
		#endregion // Fields

		// Get or create the sampler object associated with the sampler type
		public static VkSampler Get(Sampler samp)
		{
			using (var _ = _SamplerMutex.AcquireUNSAFE()) {
				return _Samplers[(int)samp] ?? (_Samplers[(int)samp] = CreateSampler(samp));
			}
		}

		// Called by GraphicsDevice to initialize
		public static void Initialize(GraphicsDevice gd)
		{
			_GraphicsDevice = gd;

			// Create the default sampler
			_Samplers[(int)Sampler.LinearNearestRepeat] = CreateSampler(Sampler.LinearNearestRepeat);
		}

		// Called by GraphicsDevice to terminate
		public static void Terminate()
		{
			foreach (var samp in _Samplers) {
				samp?.DestroySampler(null);
			}

			_GraphicsDevice = null;
		}

		private static VkSampler CreateSampler(Sampler sampler)
		{
			// Select the modes
			(VkFilter minmag, VkSamplerMipmapMode mip) = sampler switch {
				Sampler.NearestNearestRepeat      => (VkFilter.Nearest, VkSamplerMipmapMode.Nearest),
				Sampler.NearestNearestEdge        => (VkFilter.Nearest, VkSamplerMipmapMode.Nearest),
				Sampler.NearestNearestBlack       => (VkFilter.Nearest, VkSamplerMipmapMode.Nearest),
				Sampler.NearestNearestTransparent => (VkFilter.Nearest, VkSamplerMipmapMode.Nearest),
				Sampler.NearestLinearRepeat       => (VkFilter.Nearest, VkSamplerMipmapMode.Linear),
				Sampler.NearestLinearEdge         => (VkFilter.Nearest, VkSamplerMipmapMode.Linear),
				Sampler.NearestLinearBlack        => (VkFilter.Nearest, VkSamplerMipmapMode.Linear),
				Sampler.NearestLinearTransparent  => (VkFilter.Nearest, VkSamplerMipmapMode.Linear),
				Sampler.LinearNearestRepeat       => (VkFilter.Linear, VkSamplerMipmapMode.Nearest),
				Sampler.LinearNearestEdge         => (VkFilter.Linear, VkSamplerMipmapMode.Nearest),
				Sampler.LinearNearestBlack        => (VkFilter.Linear, VkSamplerMipmapMode.Nearest),
				Sampler.LinearNearestTransparent  => (VkFilter.Linear, VkSamplerMipmapMode.Nearest),
				Sampler.LinearLinearRepeat        => (VkFilter.Linear, VkSamplerMipmapMode.Linear),
				Sampler.LinearLinearEdge          => (VkFilter.Linear, VkSamplerMipmapMode.Linear),
				Sampler.LinearLinearBlack         => (VkFilter.Linear, VkSamplerMipmapMode.Linear),
				Sampler.LinearLinearTransparent   => (VkFilter.Linear, VkSamplerMipmapMode.Linear),
				_ => throw new NotImplementedException("Invalid sampler")
			};

			// Select the border info
			(VkSamplerAddressMode addr, VkBorderColor bcolor) = sampler switch {
				Sampler.NearestNearestRepeat      => (VkSamplerAddressMode.Repeat, default),
				Sampler.NearestNearestEdge        => (VkSamplerAddressMode.ClampToEdge, default),
				Sampler.NearestNearestBlack       => (VkSamplerAddressMode.ClampToBorder, VkBorderColor.FloatOpaqueBlack),
				Sampler.NearestNearestTransparent => (VkSamplerAddressMode.ClampToBorder, VkBorderColor.FloatTransparentBlack),
				Sampler.NearestLinearRepeat       => (VkSamplerAddressMode.Repeat, default),
				Sampler.NearestLinearEdge         => (VkSamplerAddressMode.ClampToEdge, default),
				Sampler.NearestLinearBlack        => (VkSamplerAddressMode.ClampToBorder, VkBorderColor.FloatOpaqueBlack),
				Sampler.NearestLinearTransparent  => (VkSamplerAddressMode.ClampToBorder, VkBorderColor.FloatTransparentBlack),
				Sampler.LinearNearestRepeat       => (VkSamplerAddressMode.Repeat, default),
				Sampler.LinearNearestEdge         => (VkSamplerAddressMode.ClampToEdge, default),
				Sampler.LinearNearestBlack        => (VkSamplerAddressMode.ClampToBorder, VkBorderColor.FloatOpaqueBlack),
				Sampler.LinearNearestTransparent  => (VkSamplerAddressMode.ClampToBorder, VkBorderColor.FloatTransparentBlack),
				Sampler.LinearLinearRepeat        => (VkSamplerAddressMode.Repeat, default),
				Sampler.LinearLinearEdge          => (VkSamplerAddressMode.ClampToEdge, default),
				Sampler.LinearLinearBlack         => (VkSamplerAddressMode.ClampToBorder, VkBorderColor.FloatOpaqueBlack),
				Sampler.LinearLinearTransparent   => (VkSamplerAddressMode.ClampToBorder, VkBorderColor.FloatTransparentBlack),
				_ => throw new NotImplementedException("Invalid sampler")
			};

			// Create the sampler
			VkSamplerCreateInfo sci = new(
				flags: VkSamplerCreateFlags.NoFlags,
				magFilter: minmag,
				minFilter: minmag,
				mipmapMode: mip,
				addressModeU: addr,
				addressModeV: addr,
				addressModeW: addr,
				mipLodBias: 0,
				anisotropyEnable: false,
				maxAnisotropy: 0,
				compareEnable: false,
				compareOp: VkCompareOp.Always,
				minLod: 0,
				maxLod: 0,
				borderColor: bcolor,
				unnormalizedCoordinates: false
			);
			VulkanHandle<VkSampler> handle;
			_GraphicsDevice!.VkDevice.CreateSampler(&sci, null, &handle).Throw($"Failed to create sampler {sampler}");
			return new(handle, _GraphicsDevice.VkDevice);
		}
	}
}
