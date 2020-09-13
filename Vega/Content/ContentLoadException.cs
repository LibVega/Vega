/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Content
{
	/// <summary>
	/// Exception that is thrown when content file loading or processing encounters an error.
	/// </summary>
	public sealed class ContentLoadException : Exception
	{
		/// <summary>
		/// The path to the content file that caused the error.
		/// </summary>
		public readonly string FilePath;
		/// <summary>
		/// A message providing details about the exception.
		/// </summary>
		public readonly string Details;

		/// <summary>
		/// Construct a new content load exception.
		/// </summary>
		/// <param name="path">The file generating the error.</param>
		/// <param name="details">Details about the error.</param>
		public ContentLoadException(string path, string details)
			: base($"Content error ({path}) - {details}")
		{
			FilePath = path;
			Details = details;
		}

		/// <summary>
		/// Construct a new content load exception.
		/// </summary>
		/// <param name="path">The file generating the error.</param>
		/// <param name="details">Details about the error.</param>
		/// <param name="inner">The inner exception reported by this exception.</param>
		public ContentLoadException(string path, string details, Exception inner)
			: base($"Content error ({path}) - {details}", inner)
		{
			FilePath = path;
			Details = details;
		}
	}
}
