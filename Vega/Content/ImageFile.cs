/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.IO;

namespace Vega.Content
{
	// Represents a handle to an ImageFile object in the native content loader
	internal unsafe sealed class ImageFile : IDisposable
	{
		#region Fields
		// The content loader native handle
		private IntPtr _handle;

		// Image file info
		public readonly string Path;
		public readonly uint Width;
		public readonly uint Height;
		public readonly ImageChannels Channels;
		public readonly ImageType Type;

		// Error info
		public ImageError Error => NativeContent.ImageGetError(_handle);
		#endregion // Fields

		public ImageFile(string path)
		{
			// Ensure file
			if (!File.Exists(path)) {
				throw new FileNotFoundException($"The image file '{path}' does not exist or is an invalid path");
			}
			Path = path;

			// Try to load the file
			ImageError error;
			(_handle, error) = NativeContent.ImageOpenFile(path);
			if (_handle == IntPtr.Zero || error != ImageError.NoError) {
				throw new ContentLoadException(path, $"image file loading failed with {error}");
			}

			// Get the file info
			(Width, Height) = NativeContent.ImageGetSize(_handle);
			Channels = NativeContent.ImageGetChannels(_handle);
			Type = NativeContent.ImageGetType(_handle);
			if (Channels == ImageChannels.Unknown) {
				throw new ContentLoadException(path, "image file has unsupported color channel count");
			}
		}
		~ImageFile()
		{
			dispose(false);
		}

		// Loads the texture as RGBA data
		public ReadOnlySpan<Color> LoadDataRGBA()
		{
			var data = NativeContent.ImageLoadData(_handle, ImageChannels.RGBA);
			if ((data == null) || (Error != ImageError.NoError)) {
				throw new ContentLoadException(Path, $"image file data read failed with {Error}");
			}
			return new(data, (int)(Width * Height));
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (_handle != IntPtr.Zero) {
				NativeContent.ImageCloseFile(_handle);
			}
			_handle = IntPtr.Zero;
		}
		#endregion // IDisposable
	}
}
