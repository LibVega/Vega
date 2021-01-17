/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.IO;

namespace Vega.Content
{
	/// <summary>
	/// Manages the loading and tracking of content objects from files. When tracked, the lifetime of loaded content
	/// is tied to the lifetime of the content manager.
	/// </summary>
	public sealed class ContentManager : IDisposable
	{
		#region Fields
		/// <summary>
		/// The root absolute path used to resolve relative paths to content files.
		/// </summary>
		public string RootPath {
			get => _rootPath;
			set => _rootPath = Path.GetFullPath(value);
		}
		private string _rootPath = String.Empty;

		/// <summary>
		/// Object disposal flag.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		/// <summary>
		/// Creates a new content manager with no loaded content items.
		/// </summary>
		/// <param name="root">The root path in which to look for content items. Defaults to current path.</param>
		public ContentManager(string root = "./")
		{
			// Get the root path
			RootPath = Path.GetFullPath(root);
		}
		~ContentManager()
		{
			dispose(false);
		}

		#region IDisposable
		/// <summary>
		/// Disposes the ContentManager, emptying the cache and disposing all tracked content items.
		/// </summary>
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed) {

			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
