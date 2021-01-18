/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
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
		internal readonly TextureRegion FullRegion;
		/// <summary>
		/// The format of the texels in the texture.
		/// </summary>
		public readonly TexelFormat Format;
		/// <summary>
		/// The usage policy for the texture.
		/// </summary>
		public readonly TextureUsage Usage;

		/// <summary>
		/// Gets if the data in the texture has been initialized through at least one call to SetData().
		/// </summary>
		public bool Initialized { get; private set; } = false;

		// The resource binding type for this texture type
		internal abstract BindingType BindingType { get; }

		// The specific image type
		internal readonly VkImageViewType ImageType;

		// Vulkan objects
		internal readonly VkImage Handle;
		internal readonly MemoryAllocation Memory;
		internal readonly VkImageView View;

		// Global binding table indices for different shaders, UINT16_MAX for unassigned slots
		private readonly ushort[] _tableIndices;
		private readonly FastMutex _tableMutex = new();

		// Last frame in which a dynamic buffer was updated
		internal ulong _lastDynamicUpdate = 0;
		#endregion // Fields

		private protected TextureBase(uint w, uint h, uint d, uint m, uint l, TexelFormat format, TextureUsage use,
				ResourceType type)
			: base(type)
		{
			GetImageInfo(Graphics, type, out var vType, out var imType, out var sizeLim);

			// Validate
			if ((w > sizeLim) || (h > sizeLim) || (d > sizeLim)) {
				throw new ArgumentException($"Invalid texture dims {w}x{h}x{d} (limit={sizeLim})");
			}
			if (l > Graphics.Limits.MaxTextureLayers) {
				throw new ArgumentException($"Invalid texture layer count {l} (limit={Graphics.Limits.MaxTextureLayers})");
			}

			// Set values
			Dimensions = (w, h, d, m, l);
			FullRegion = new(0, 0, 0, w, h, d, 0, l);
			Format = format;
			Usage = use;
			ImageType = vType;
			_tableIndices = new ushort[SamplerPool.MAX_SAMPLER_COUNT];
			Array.Fill(_tableIndices, UInt16.MaxValue);

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
			Graphics.VkDevice.CreateImage(&ici, null, &imageHandle).Throw("Failed to create image handle");
			Handle = new(imageHandle, Graphics.VkDevice);

			// Allocate/bind memory
			VkMemoryRequirements memreq;
			Handle.GetImageMemoryRequirements(&memreq);
			Memory = Graphics.Resources.AllocateMemoryDevice(memreq) ?? 
				throw new Exception("Failed to allocate texture memory");
			Handle.BindImageMemory(Memory.Handle, Memory.Offset);

			// Create image view
			VkImageViewCreateInfo ivci = new(
				flags: VkImageViewCreateFlags.NoFlags,
				image: imageHandle,
				viewType: vType,
				format: (VkFormat)format,
				components: new(), // Identity mapping
				subresourceRange: new(Format.GetAspectFlags(), 0, m, 0, l)
			);
			VulkanHandle<VkImageView> viewHandle;
			Graphics.VkDevice.CreateImageView(&ivci, null, &viewHandle).Throw("Failed to create image view");
			View = new(viewHandle, Graphics.VkDevice);
		}

		#region SetData
		// Implementation of SetData for general images
		private protected void SetDataImpl(in TextureRegion region, void* data)
		{
			// Validate
			ThrowOnBadRegion(this, region);
			if (!Initialized && (region != FullRegion)) {
				throw new ArgumentException(
					"The first call to SetData for a texture must set the entire texture data", nameof(region));
			}

			// Switch on usage
			if (!Initialized || (Usage == TextureUsage.Static)) {
				if (Initialized) {
					throw new InvalidOperationException("Cannot set data on an initialized static texture");
				}
				Graphics.Resources.TransferManager.SetImageData(
					Handle, Format, region, data, RUID.Type, !Initialized || (region == FullRegion)
				);
				Initialized = true;
			}
			else { // Usage == Dynamic
				if (_lastDynamicUpdate == AppTime.FrameCount) {
					throw new InvalidOperationException("Dynamic textures can only be updated once per frame");
				}
				_lastDynamicUpdate = AppTime.FrameCount;
				// TODO: Set Data
			}
		}

		private protected void SetDataImpl(in TextureRegion region, ReadOnlySpan<byte> data)
		{
			// Validate
			ThrowOnBadRegion(this, region);
			if (!Initialized && (region != FullRegion)) {
				throw new ArgumentException(
					"The first call to SetData for a texture must set the entire texture data", nameof(region));
			}
			if ((ulong)data.Length < region.GetDataSize(Format)) {
				throw new InvalidOperationException("data span is not large enough for the requested data");
			}

			// Switch on usage
			if (!Initialized || (Usage == TextureUsage.Static)) {
				if (Initialized) {
					throw new InvalidOperationException("Cannot set data on an initialized static texture");
				}
				fixed (byte* dataptr = data) {
					Graphics.Resources.TransferManager.SetImageData(
						Handle, Format, region, dataptr, RUID.Type, !Initialized || (region == FullRegion)
					);
				}
				Initialized = true;
			}
			else { // Usage == Dynamic
				if (_lastDynamicUpdate == AppTime.FrameCount) {
					throw new InvalidOperationException("Dynamic textures can only be updated once per frame");
				}
				_lastDynamicUpdate = AppTime.FrameCount;
				// TODO: Set Data
			}
		}

		private protected void SetDataImpl(in TextureRegion region, HostBuffer data, ulong dataOffset)
		{
			// Validate
			ThrowOnBadRegion(this, region);
			if (!Initialized && (region != FullRegion)) {
				throw new ArgumentException(
					"The first call to SetData for a texture must set the entire texture data", nameof(region));
			}
			if (dataOffset >= data.DataSize) {
				throw new InvalidOperationException("Offset into texture source data is too large");
			}
			if ((data.DataSize - dataOffset) < region.GetDataSize(Format)) {
				throw new InvalidOperationException("host buffer is not large enough for the requested data");
			}

			// Switch on usage
			if (!Initialized || (Usage == TextureUsage.Static)) {
				if (Initialized) {
					throw new InvalidOperationException("Cannot set data on an initialized static texture");
				}
				Graphics.Resources.TransferManager.SetImageData(
					Handle, Format, region, data, dataOffset, RUID.Type, !Initialized || (region == FullRegion)
				);
				Initialized = true;
			}
			else { // Usage == Dynamic
				if (_lastDynamicUpdate == AppTime.FrameCount) {
					throw new InvalidOperationException("Dynamic textures can only be updated once per frame");
				}
				_lastDynamicUpdate = AppTime.FrameCount;
				// TODO: Set Data
			}
		}
		#endregion // SetData

		// Gets the binding table index for the given sampler, and creates a new one if required
		internal ushort EnsureSampler(GraphicsDevice gd, Sampler sampler)
		{
			using (var _ = _tableMutex.AcquireUNSAFE()) {
				var index = _tableIndices[(int)sampler];
				if (index == UInt16.MaxValue) {
					index = gd.BindingTable.Reserve(this, sampler);
					_tableIndices[(int)sampler] = index;
				}
				return index;
			}
		}

		private static void ThrowOnBadRegion(TextureBase tex, in TextureRegion reg)
		{
			if ((reg.X + reg.Width) > tex.Dimensions.Width) {
				throw new InvalidOperationException("width is outside of texture size range");
			}
			if ((reg.Y + reg.Height) > tex.Dimensions.Height) {
				throw new InvalidOperationException("height is outside of texture size range");
			}
			if ((reg.Z + reg.Depth) > tex.Dimensions.Depth) {
				throw new InvalidOperationException("depth is outside of texture size range");
			}
			if ((reg.LayerStart + reg.LayerCount) > tex.Dimensions.Layers) {
				throw new InvalidOperationException("array layers is outside of texture size range");
			}
		}

		protected override void OnDispose(bool disposing)
		{
			if (Core.Instance is not null) {
				Graphics.Resources.QueueDestroy(this);
			}
			else {
				Destroy();
			}
		}

		protected internal override void Destroy()
		{
			if (Core.Instance is null) {
				return;
			}

			// Free global binding table indices
			foreach (var idx in _tableIndices) {
				if (idx != UInt16.MaxValue) {
					Graphics.BindingTable.Release(BindingTableType.Sampler, idx);
				}
			}

			// Destroy objects
			View?.DestroyImageView(null);
			Handle?.DestroyImage(null);
			Memory?.Free();
		}

		private static void GetImageInfo(GraphicsDevice gd, ResourceType type,
			out VkImageViewType viewType, out VkImageType imType, out uint sizeLim)
		{
			(viewType, imType, sizeLim) = type switch {
				ResourceType.Texture1D => (VkImageViewType.E1D, VkImageType.E1D, gd.Limits.MaxTextureSize1D),
				ResourceType.Texture2D => (VkImageViewType.E2D, VkImageType.E2D, gd.Limits.MaxTextureSize2D),
				ResourceType.Texture3D => (VkImageViewType.E3D, VkImageType.E3D, gd.Limits.MaxTextureSize3D),
				ResourceType.Texture1DArray => (VkImageViewType.E1DArray, VkImageType.E1D, gd.Limits.MaxTextureSize1D),
				ResourceType.Texture2DArray => (VkImageViewType.E2DArray, VkImageType.E2D, gd.Limits.MaxTextureSize2D),
				_ => throw new Exception("LIBRARY BUG - Invalid image type for TextureBase")
			};
		}
	}
}
