/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.IO;

namespace Vega.Content
{
	/// <summary>
	/// Manages the loading and tracking of content objects from files. When tracked, the lifetime of loaded content
	/// is tied to the lifetime of the content manager.
	/// </summary>
	public sealed class ContentManager : IDisposable
	{
		// Static instance tracking
		private static readonly List<WeakReference<ContentManager>> _Managers = new();
		private static readonly object _ManagerLock = new();

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
		/// The dictionary of cached content items, indexed by the path (relative or absolute) that was used to load
		/// them.
		/// </summary>
		public IReadOnlyDictionary<string, object> ItemCache => _itemCache;
		private readonly Dictionary<string, object> _itemCache = new();
		// Separate list that tracks content items that implement IDisposable
		private readonly List<WeakReference<object>> _disposableItems = new();
		// Mutex for item list operations
		private readonly object _itemLock = new();

		// Content loader instances
		private readonly Dictionary<Type, ContentLoader> _loaders = new();
		private readonly object _loaderLock = new();

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
			RootPath = root;

			// Track the object
			TrackContentManager(this);
		}
		~ContentManager()
		{
			dispose(false);
		}

		#region Loading
		/// <summary>
		/// Attempts to load the content item with the given path, first checking the cache and then loading fresh
		/// from the disk.
		/// </summary>
		/// <typeparam name="T">The content type to load.</typeparam>
		/// <param name="path">The path to the content file, absolute or relative to the root path.</param>
		/// <param name="cache">If the item should be cached.</param>
		/// <param name="manage">If the item is <see cref="IDisposable"/> and should be managed.</param>
		/// <returns>The loaded content item.</returns>
		public T Load<T>(string path, bool cache = true, bool manage = true)
			where T : class
		{
			// Check for cached item
			lock (_itemLock) {
				if (_itemCache.TryGetValue(path, out var cachedItem)) {
					if (cachedItem is not T) {
						throw new ContentLoadException(path,
							$"Cached item type {cachedItem.GetType()} is not requested type {typeof(T)}");
					}
					return (cachedItem as T)!;
				} 
			}

			// Find the content loader for the type
			var loadType = typeof(T);
			ContentLoader loaderObject;
			lock (_loaderLock) {
				if (_loaders.TryGetValue(loadType, out var loader)) {
					loaderObject = loader;
				}
				else {
					// TODO - load a new loader type if one is found
					throw new ContentLoadException(path,
						$"No content loader type was found for loading {typeof(T)}.");
				}
			}

			// Load the new item
			var item = loadItem<T>(path, loaderObject);

			// Cache and track if required
			if (cache) {
				lock (_itemLock) {
					_itemCache.Add(path, item);
				}
			}
			if (manage && (item is IDisposable)) {
				lock (_itemLock) {
					_disposableItems.Add(new(item));
				}
			}

			// Return the object
			return item;
		}

		// Performs the loading for a content item
		private T loadItem<T>(string path, ContentLoader loader)
			where T : class
		{
			return null!;
		}
		#endregion // Loading

		#region Cache Management
		/// <summary>
		/// Unloads all content loaded and tracked by this content manager. This effect is immediate, and will dispose
		/// all <see cref="IDisposable"/> content items.
		/// </summary>
		public void UnloadAll()
		{
			lock (_itemLock) {
				foreach (var itemref in _disposableItems) {
					if (itemref.TryGetTarget(out var item)) {
						(item as IDisposable)!.Dispose();
					}
				}
				_disposableItems.Clear();
				_itemCache.Clear();
			}
		}
		#endregion // Cache Management

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
				if (disposing) {
					UnloadAll();
				}

				// Remove from tracking list
				UntrackContentManager(this);
			}
			IsDisposed = true;
		}
		#endregion // IDisposable

		#region Static Tracking
		// Adds an instance to the static tracker, also performs cleanup
		private static void TrackContentManager(ContentManager inst)
		{
			lock (_ManagerLock) {
				// Cleanup
				for (int i = 0; i < _Managers.Count; ++i) {
					if (!_Managers[i].TryGetTarget(out _)) {
						_Managers.RemoveAt(i);
						--i;
					}
				}

				// Add
				_Managers.Add(new(inst));
			}
		}

		// Removes an instance to the static tracker, also performs cleanup
		private static void UntrackContentManager(ContentManager inst)
		{
			lock (_ManagerLock) {
				// Cleanup and remove
				for (int i = 0; i < _Managers.Count; ++i) {
					if (!_Managers[i].TryGetTarget(out var cm) || ReferenceEquals(cm, inst)) {
						_Managers.RemoveAt(i);
						--i;
					}
				}
			}
		}
		#endregion // Static Tracking
	}
}
