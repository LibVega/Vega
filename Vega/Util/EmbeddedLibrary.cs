/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Vega.Util
{
	/// <summary>
	/// Represents a native library embedded in an assembly, which can be detected, extracted, and loaded
	/// automatically. The base name of the resources is specified, and the extension is used to select the platform
	/// correct version to extract:
	/// <list type="bullet">
	/// <item><term>.win</term> Windows desktop</item>
	/// <item><term>.osx</term> Mac OSX desktop</item>
	/// <item><term>.lin</term> Linux desktop</item>
	/// </list>
	/// </summary>
	/// <remarks>
	/// Each library can be overridden by placing the new file into the same directory as the application. This
	/// allows easy "re-linking" which may be required by some of the shared library licenses.
	/// </remarks>
	public sealed class EmbeddedLibrary : IDisposable
	{
		/// <summary>
		/// The folder path that embedded libraries are extracted to.
		/// </summary>
		public static readonly string ExtractPath;

		#region Fields
		private IntPtr? _handle = null;
		/// <summary>
		/// Gets the handle of the open library, extracting and loading the library the first time it is accessed.
		/// Will be equal to <see cref="IntPtr.Zero"/> if the library has been unloaded.
		/// </summary>
		/// <exception cref="DllNotFoundException">The library could not be found.</exception>
		/// <exception cref="BadImageFormatException">The resource was not a loadable library.</exception>
		public IntPtr Handle => _handle ?? (_handle = load()).Value;
		/// <summary>
		/// Gets if the library is loaded.
		/// </summary>
		public bool Loaded => _handle.HasValue && _handle.Value != IntPtr.Zero;
		/// <summary>
		/// The full path to the file on disk loaded as the library, or <c>null</c> if not loaded.
		/// </summary>
		public string? LibraryPath { get; private set; } = null;
		/// <summary>
		/// Gets if the library was overridden by a library placed in the same directory as the application.
		/// </summary>
		public bool HasOverride { get; private set; } = false;

		/// <summary>
		/// The assembly containing the embedded library.
		/// </summary>
		public readonly Assembly Assembly;
		/// <summary>
		/// The base name (no extension) of the library resource.
		/// </summary>
		public readonly string BaseName;
		/// <summary>
		/// The filename of the extracted library.
		/// </summary>
		public readonly string FileName;
		/// <summary>
		/// If the library was found for the different platforms.
		/// </summary>
		public readonly (bool Win, bool OSX, bool Lin) Found;
		#endregion // Fields

		/// <summary>
		/// Describes a new embedded library, but does not extract the load the library. 
		/// </summary>
		/// <param name="assembly">The assembly containing the embedded library resources.</param>
		/// <param name="baseName">The base name (without the platform extensions) of the embedded resources.</param>
		/// <param name="fileName">The base name (used like (name).dll or lib(name).so) of the extracted file.</param>
		public EmbeddedLibrary(Assembly assembly, string baseName, string? fileName = null)
		{
			Assembly = assembly;
			BaseName = baseName;
			FileName = Runtime.OS.IsWindows ? $"{fileName ?? baseName}.dll" : $"lib{fileName ?? baseName}.so";

			var avail = assembly.GetManifestResourceNames();
			Found = (
				avail.Contains(baseName + ".win"),
				avail.Contains(baseName + ".osx"),
				avail.Contains(baseName + ".lin")
			);
		}
		~EmbeddedLibrary()
		{
			Dispose();
		}

		#region Loading
		// Performs the extracting the loading
		private IntPtr load()
		{
			// Check current directory for library override
			var here = Path.Combine(Environment.CurrentDirectory, FileName);
			if (File.Exists(here)) {
				HasOverride = true;
				LibraryPath = here;
				return NativeLibrary.Load(here);
			}

			// Extract the library
			HasOverride = false;
			LibraryPath = Path.Combine(ExtractPath, FileName);
			if (Runtime.OS.IsWindows && Found.Win) {
				ExtractLibrary(Assembly, BaseName + ".win", LibraryPath);
				return NativeLibrary.Load(LibraryPath);
			}
			else if (Runtime.OS.IsMacOS && Found.OSX) {
				ExtractLibrary(Assembly, BaseName + ".osx", LibraryPath);
				return NativeLibrary.Load(LibraryPath);
			}
			else if (Runtime.OS.IsLinuxDesktop && Found.Lin) {
				ExtractLibrary(Assembly, BaseName + ".lin", LibraryPath);
				return NativeLibrary.Load(LibraryPath);
			}

			// Attempt to load the library installed in the system
			if (NativeLibrary.TryLoad(FileName, out var handle)) {
				return handle;
			}
			throw new DllNotFoundException("Failed to load fallback library installed on system");
		}

		// Extract resource to file path
		private static void ExtractLibrary(Assembly asm, string resource, string path)
		{
			// Skip existing library extraction
			if (File.Exists(path)) return;

			// Extract resource
			using var reader = asm.GetManifestResourceStream(resource)!;
			using var writer = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
			reader.CopyTo(writer, 32_768);
		}
		#endregion // Loading

		public void Dispose()
		{
			if (_handle.HasValue && _handle.Value != IntPtr.Zero) {
				NativeLibrary.Free(_handle.Value);
				_handle = IntPtr.Zero;
				GC.SuppressFinalize(this);
			}
		}

		static EmbeddedLibrary()
		{
			var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			var version = typeof(EmbeddedLibrary).Assembly.GetName().Version!;
			var vstr = $"{version.Major}.{version.Minor}.{version.Revision}";
			ExtractPath = Path.Combine(local, "VegaLib", "Native", vstr);
		}
	}
}
