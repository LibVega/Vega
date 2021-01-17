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
	/// loader types can be reused.
	/// </para>
	/// <para>
	/// These types are not synchronized, meaning the same object may be used by multiple threads at one when loading.
	/// Ensure that this thread-safety is supported.
	/// </para>
	/// </summary>
	public abstract class ContentLoader
	{

	}
}
