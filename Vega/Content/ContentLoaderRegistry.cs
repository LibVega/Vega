/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using Vega.Graphics;

namespace Vega.Content
{
	// Manages a registry of content loader types that are available for instantiation
	internal static class ContentLoaderRegistry
	{
		#region Fields
		// Generic type for checking content loader base types
		private static readonly Type CONTENT_LOADER_BASE_TYPE = 
			typeof(ContentLoaderBase<Texture2D>).GetGenericTypeDefinition();

		// The registry of loader types
		private static readonly Dictionary<Type, Type> _Registry = new();
		#endregion // Fields

		// Try to instantiate a new instance of the loader for the given content type
		public static bool TryCreateLoaderType<T>(out ContentLoaderBase<T> loader, out string err)
			where T : class
		{
			if (_Registry.TryGetValue(typeof(T), out var loaderType)) {
				try {
					// Try instantiate
					loader = (loaderType.GetConstructor(Array.Empty<Type>())!.Invoke(null) as ContentLoaderBase<T>)!;
					err = String.Empty;
					return true;
				}
				catch (Exception ex) {
					loader = null!;
					err = $"Unhandled loader construction exception - {ex.Message}";
					return false;
				}
			}

			err = $"No loader type found for content type {typeof(T)}";
			loader = null!;
			return false;
		}

		// Register a new content loader type
		private static bool TryRegisterLoaderType(Type loaderType, out string err)
		{
			// Check base type
			if (!IsContentLoader(loaderType, out var contentType)) {
				err = "Not ContentLoaderBase<> type";
				return false;
			}

			// Check that it is instantiable
			if (!loaderType.IsClass || loaderType.IsAbstract) {
				err = "Not instantiable class type";
				return false;
			}
			var defaultCtor = loaderType.GetConstructor(Array.Empty<Type>());
			if (defaultCtor is null) {
				err = "No no-args constructor";
				return false;
			}

			// Check if a loader for the type already exists
			if (_Registry.TryGetValue(contentType, out _)) {
				err = $"Content type {contentType} already has loader";
				return false;
			}

			// Add the loader
			err = String.Empty;
			_Registry.Add(contentType, loaderType);
			return true;
		}

		// Checks if the type is a descendent of ContentLoaderBase<T>
		private static bool IsContentLoader(Type loaderType, out Type contentType)
		{
			var checkType = loaderType.BaseType;
			while ((checkType is not null) && (checkType != typeof(object))) {
				var toCheck = checkType.IsGenericType ? checkType.GetGenericTypeDefinition() : checkType;
				if (toCheck == CONTENT_LOADER_BASE_TYPE) {
					contentType = checkType.GetGenericArguments()[0];
					return true;
				}
			}
			contentType = null!;
			return false;
		}

		static ContentLoaderRegistry()
		{
			// Register default builtin loader types
			if (!TryRegisterLoaderType(typeof(ShaderProgramLoader), out var err)) {
				throw new Exception($"LIBRARY BUG - Failed to register ShaderProgramLoader - {err}");
			}
		}
	}
}
