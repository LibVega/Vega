/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Content
{
	/// <summary>
	/// Base type for implementing content item loading logic used by <see cref="ContentManager"/>.
	/// <para>
	/// Each content loader type will be initialized once per content loader, and will be reused. Ensure that content
	/// loader types can be reused. A no-args constructor is required.
	/// </para>
	/// <para>
	/// These types are not synchronized, meaning the same object may be used by multiple threads at one when loading.
	/// Ensure that this thread-safety is supported.
	/// </para>
	/// </summary>
	/// <typeparam name="T">The runtime content type that is loaded by the loader subtype.</typeparam>
	public abstract class ContentLoaderBase<T> : IContentLoader
		where T : class
	{
		#region Fields
		/// <summary>
		/// Gets the runtime type that this content loader is designed to load.
		/// </summary>
		public Type ContentType => typeof(T);
		#endregion // Fields

		/// <summary>
		/// Implements the functionality for loading a runtime content item of type <typeparamref name="T"/>.
		/// <para>
		/// Each content loader instance will be reused, and may even be used by more than one thread at a time. Ensure
		/// that implementation of this function can support this.
		/// </para>
		/// </summary>
		/// <param name="fullPath">
		/// The absolute path to the item to load, which has already been checked to exist.
		/// </param>
		/// <returns>The new runtime object representing the content item at the given path.</returns>
		public abstract T Load(string fullPath);

		// Internal non-generic load call (supporting non-genertic IContentLoader)
		object IContentLoader.LoadNonGeneric(string fullPath) => Load(fullPath);
	}
}
