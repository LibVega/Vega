/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega
{
	/// <summary>
	/// Represents a pre-filtering step to select <see cref="EventHub"/> events to forward to 
	/// <see cref="EventSubscription{T}"/> instances.
	/// </summary>
	/// <typeparam name="T">The event type to filter.</typeparam>
	public interface IEventFilter<T>
	{
		/// <summary>
		/// Selection function to check an event message for filtering.
		/// </summary>
		/// <param name="sender">The optional sender of the event.</param>
		/// <param name="time">The app time that the event was published at.</param>
		/// <param name="data">The event message to check.</param>
		/// <returns><c>true</c> to pass the event through to the subscription, <c>false</c> to ignore.</returns>
		bool Accept(object? sender, TimeSpan time, T? data) => true;
	}
}
