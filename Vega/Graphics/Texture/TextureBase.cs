/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Base type for all device-accessible textures, providing common functionality.
	/// </summary>
	public unsafe abstract class TextureBase : ResourceBase
	{
		#region Fields
		/// <summary>
		/// The dimensions of the texture, plus the number of array layers.
		/// </summary>
		public readonly (
			uint Width,
			uint Height,
			uint Depth,
			uint Layers,
			uint Mips
		) Dimensions;
		/// <summary>
		/// The format of the texels in the texture.
		/// </summary>
		public readonly TexelFormat Format;

		// The specific image type
		internal readonly VkImageViewType ImageType;

		// Vulkan objects
		internal readonly VkImage Handle;
		internal readonly MemoryAllocation Memory;
		internal readonly VkImageView View;
		#endregion // Fields

		private protected TextureBase(uint w, uint h, uint d, uint m, uint l, VkImageViewType type, TexelFormat format, 
				ResourceType rtype)
			: base(rtype)
		{
			var gd = Core.Instance!.Graphics;
			GetImageInfo(gd, type, out var imType, out var sizeLim);

			// Validate
			if ((w > sizeLim) || (h > sizeLim) || (d > sizeLim)) {
				throw new ArgumentException($"Invalid texture dims {w}x{h}x{d} (limit={sizeLim})");
			}
			if (l > gd.Limits.MaxTextureLayers) {
				throw new ArgumentException($"Invalid texture layer count {l} (limit={gd.Limits.MaxTextureLayers})");
			}

			// Set values
			Dimensions = (w, h, d, m, l);
			Format = format;
			ImageType = type;

			// Create image
			VkImageCreateInfo ici = new(
				flags: VkImageCreateFlags.NoFlags,
				imageType: imType,
				format: (VkFormat)format,
				extent: new(w, h, d),
				mipLevels: m,
				arrayLayers: l,
				samples: VkSampleCountFlags.E1,
				tiling: VkImageTiling.Optimal,
				usage: VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled,
				sharingMode: VkSharingMode.Exclusive,
				initialLayout: VkImageLayout.Undefined
			);
			VulkanHandle<VkImage> imageHandle;
			gd.VkDevice.CreateImage(&ici, null, &imageHandle).Throw("Failed to create image handle");
			Handle = new(imageHandle, gd.VkDevice);

			// Allocate/bind memory
			VkMemoryRequirements memreq;
			Handle.GetImageMemoryRequirements(&memreq);
			Memory = gd.Resources.AllocateMemoryDevice(memreq) ?? 
				throw new Exception("Failed to allocate texture memory");
			Handle.BindImageMemory(Memory.Handle, Memory.Offset);

			// Create image view
			VkImageViewCreateInfo ivci = new(
				flags: VkImageViewCreateFlags.NoFlags,
				image: imageHandle,
				viewType: type,
				format: (VkFormat)format,
				components: new(), // Identity mapping
				subresourceRange: new(Format.GetAspectFlags(), 0, m, 0, l)
			);
			VulkanHandle<VkImageView> viewHandle;
			gd.VkDevice.CreateImageView(&ivci, null, &viewHandle).Throw("Failed to create image view");
			View = new(viewHandle, gd.VkDevice);
		}

		private static void GetImageInfo(GraphicsDevice gd, VkImageViewType viewType, 
			out VkImageType imType, out uint sizeLim)
		{
			(imType, sizeLim) = viewType switch {
				VkImageViewType.E1D => (VkImageType.E1D, gd.Limits.MaxTextureSize1D),
				VkImageViewType.E2D => (VkImageType.E2D, gd.Limits.MaxTextureSize2D),
				VkImageViewType.E3D => (VkImageType.E3D, gd.Limits.MaxTextureSize3D),
				VkImageViewType.E1DArray => (VkImageType.E1D, gd.Limits.MaxTextureSize1D),
				VkImageViewType.E2DArray => (VkImageType.E2D, gd.Limits.MaxTextureSize2D),
				_ => throw new Exception("LIBRARY BUG - Invalid image type for TextureBase")
			};
		}
	}
}
