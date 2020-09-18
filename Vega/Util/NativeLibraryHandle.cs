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
	/// Manages an handle to a native library loaded by the system.
	/// </summary>
	public sealed class NativeLibraryHandle : IDisposable
	{
		/// <summary>
		/// The folder path that embedded libraries are extracted to.
		/// </summary>
		public static readonly string ExtractPath;

		#region Fields
		/// <summary>
		/// Gets the handle to the native library. The library is loaded the first time this field is accessed.
		/// </summary>
		public IntPtr Handle
		{
			get {
				if (IsDisposed) throw new ObjectDisposedException(nameof(NativeLibraryHandle));
				return (_handle != IntPtr.Zero) ? _handle : (_handle = Load(this));
			}
		}
		private IntPtr _handle = IntPtr.Zero;

		/// <summary>
		/// The name/path used to load the library.
		/// </summary>
		public readonly string LoadName;
		/// <summary>
		/// If the native library is embedded in a managed assembly and is extracted at runtime.
		/// </summary>
		public bool IsEmbedded => _embedInfo.Assembly != null;
		// Embedded library info
		private readonly (
			Assembly? Assembly,
			string? ResourceName,
			bool HasWindows,
			bool HasLinux,
			bool HasMac
		) _embedInfo;

		/// <summary>
		/// Gets if this object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		private NativeLibraryHandle(string loadName)
		{
			LoadName = loadName;
			_embedInfo = (null, null, false, false, false);
		}
		private NativeLibraryHandle(string loadName, Assembly assembly, string resName, bool win, bool lin, bool mac)
		{
			LoadName = loadName;
			_embedInfo = (assembly, resName, win, lin, mac);
		}
		~NativeLibraryHandle()
		{
			dispose(false);
		}

		private static IntPtr Load(NativeLibraryHandle library)
		{
			if (library.IsEmbedded) {
				// Check current directory for library override
				var here = Path.Combine(Environment.CurrentDirectory, library.LoadName);
				if (File.Exists(here)) {
					return NativeLibrary.Load(here);
				}

				// Extract the embedded library
				var path = Path.Combine(ExtractPath, library.LoadName);
				var info = library._embedInfo;
				if (Runtime.OS.IsWindows && info.HasWindows) {
					ExtractLibrary(info.Assembly!, info.ResourceName! + ".win", path);
					return NativeLibrary.Load(path);
				}
				else if (Runtime.OS.IsMacOS && info.HasLinux) {
					ExtractLibrary(info.Assembly!, info.ResourceName! + ".lin", path);
					return NativeLibrary.Load(path);
				}
				else if (Runtime.OS.IsLinuxDesktop && info.HasMac) {
					ExtractLibrary(info.Assembly!, info.ResourceName! + ".mac", path);
					return NativeLibrary.Load(path);
				}

				// Fallback to system library load below
			}

			return NativeLibrary.Load(library.LoadName);
		}

		private static void ExtractLibrary(Assembly asm, string res, string path)
		{
			// Skip existing library extraction
			if (File.Exists(path)) return;

			// Extract resource
			using var reader = asm.GetManifestResourceStream(res)!;
			using var writer = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
			reader.CopyTo(writer, 32_768);
		}

		#region Function Load
		/// <summary>
		/// Attempts to load a function from the native library.
		/// </summary>
		/// <typeparam name="T">The delegate type to store the function handle in.</typeparam>
		/// <param name="name">The name of the function as seen in the library export table.</param>
		public T LoadFunction<T>(string name)
			where T : Delegate
		{
			if (NativeLibrary.TryGetExport(Handle, name, out var addr)) {
				return Marshal.GetDelegateForFunctionPointer<T>(addr);
			}
			throw new ArgumentException($"Native library export '{name}' was not found", nameof(name));
		}
		/// <summary>
		/// Attempts to load a function from the native library, returning if the load was successful.
		/// </summary>
		/// <typeparam name="T">The delegate type to store the function handle in.</typeparam>
		/// <param name="name">The name of the function as seen in the library export table..</param>
		/// <param name="func">The handle for the loaded function.</param>
		public bool TryLoadFunction<T>(string name, out T? func)
			where T : Delegate
		{
			if (NativeLibrary.TryGetExport(Handle, name, out var addr)) {
				func = Marshal.GetDelegateForFunctionPointer<T>(addr);
				return true;
			}
			else {
				func = null;
				return false;
			}
		}
		/// <summary>
		/// Attempts to load a function from the native library, with the same name as the delegate type.
		/// </summary>
		/// <typeparam name="T">The delegate type (and name) of the function handle.</typeparam>
		public T LoadFunction<T>() where T : Delegate => LoadFunction<T>(typeof(T).Name);
		/// <summary>
		/// Attempts to load a function from the native library, with the same name as the delegate type. Returns if
		/// the load was successful.
		/// </summary>
		/// <typeparam name="T">The delegate type (and name) of the function handle.</typeparam>
		/// <param name="func">The handle for the loaded function.</param>
		public bool TryLoadFunction<T>(out T? func) where T : Delegate => TryLoadFunction<T>(typeof(T).Name, out func);
		#endregion // Function Load

		#region Creation
		/// <summary>
		/// Describes a native library handle from the library base name.
		/// </summary>
		/// <param name="name">The base name of the library (without any prefix or extension).</param>
		public static NativeLibraryHandle FromName(string name)
		{
			string load = Runtime.OS.Family switch { 
				OSFamily.Windows => $"{name}.dll",
				OSFamily.LinuxDesktop => $"lib{name}.so",
				OSFamily.MacOS => $"lib{name}.so",
				_ => throw new PlatformNotSupportedException("Unsupported platform for NativeLibraryHandle")
			};
			return new NativeLibraryHandle(load);
		}

		/// <summary>
		/// Describes a native library handle from the library base name, supporting different names for each platform.
		/// </summary>
		/// <param name="win">The Windows base library name (without .dll).</param>
		/// <param name="lin">The Linux base library name (without 'lib' or .so).</param>
		/// <param name="mac">The MacOS base library name (without 'lib' or .so).</param>
		public static NativeLibraryHandle FromName(string win, string lin, string mac)
		{
			string load = Runtime.OS.Family switch { 
				OSFamily.Windows => $"{win}.dll",
				OSFamily.LinuxDesktop => $"lib{lin}.so",
				OSFamily.MacOS => $"lib{mac}.so",
				_ => throw new PlatformNotSupportedException("Unsupported platform for NativeLibraryHandle")
			};
			return new NativeLibraryHandle(load);
		}

		/// <summary>
		/// Describes a native library from an absolute or relative path.
		/// </summary>
		/// <param name="path">The path to the library file, including the full filename.</param>
		public static NativeLibraryHandle FromPath(string path) => new NativeLibraryHandle(Path.GetFullPath(path));

		/// <summary>
		/// Describes a native library from an absolute or relative path, supporting different paths for each platform.
		/// </summary>
		/// <param name="win">The Windows library path, including the full filename.</param>
		/// <param name="lin">The Linux library path, including the full filename.</param>
		/// <param name="mac">The MacOS library path, including the full filename.</param>
		public static NativeLibraryHandle FromPath(string win, string lin, string mac) => new NativeLibraryHandle(
			Path.GetFullPath(Runtime.OS.IsWindows ? win : Runtime.OS.IsLinuxDesktop ? lin : mac)
		);

		/// <summary>
		/// Describes a native library in the given assembly as an embedded resource. This library is extracted and
		/// loaded at runtime, based on some naming rules for the extension of the embedded resource names:
		/// <list type="bullet">
		/// <item><term>.win</term> Windows desktop</item>
		/// <item><term>.osx</term> Mac OSX desktop</item>
		/// <item><term>.lin</term> Linux desktop</item>
		/// </list>
		/// This also supports library overrides by putting the library into the application directory.
		/// </summary>
		/// <param name="assembly">The assembly containing the libraries as embedded resources.</param>
		/// <param name="resource">The base resource name (without the platform extensions) of the libraries.</param>
		/// <param name="fileName">The base name of the library once extracted to the disk.</param>
		public static NativeLibraryHandle FromEmbedded(Assembly assembly, string resource, string fileName)
		{
			// Get load name
			string load = Runtime.OS.Family switch {
				OSFamily.Windows => $"{fileName}.dll",
				OSFamily.LinuxDesktop => $"lib{fileName}.so",
				OSFamily.MacOS => $"lib{fileName}.so",
				_ => throw new PlatformNotSupportedException("Unsupported platform for NativeLibraryHandle")
			};

			// Get platform support
			var avail = assembly.GetManifestResourceNames();
			bool win = avail.Contains(resource + ".win");
			bool lin = avail.Contains(resource + ".lin");
			bool mac = avail.Contains(resource + ".osx");

			return new NativeLibraryHandle(load, assembly, resource, win, lin, mac);
		}
		#endregion // Creation

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed) {
				if (_handle != IntPtr.Zero) {
					NativeLibrary.Free(_handle);
					_handle = IntPtr.Zero;
				}
			}
			IsDisposed = true;
		}
		#endregion // IDisposable

		static NativeLibraryHandle()
		{
			var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			var version = typeof(NativeLibraryHandle).Assembly.GetName().Version!;
			var vstr = $"{version.Major}.{version.Minor}.{version.Revision}";
			ExtractPath = Path.Combine(local, "VegaLib", "Native", vstr);
			if (!Directory.Exists(ExtractPath)) {
				Directory.CreateDirectory(ExtractPath);
			}
		}
	}
}
