/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;

namespace Vega
{
	/// <summary>
	/// Controls the subscription, publication, and dispatch of generic event messages.
	/// </summary>
	public sealed class EventHub
	{
		#region Fields
		private readonly Dictionary<Type, List<IEventSubscription>> _subscriptions;
		private readonly object _subLock;
		#endregion // Fields

		public EventHub()
		{
			_subscriptions = new();
			_subLock = new();
		}

		/// <summary>
		/// Clears all subscriptions for the given event type.
		/// </summary>
		/// <typeparam name="T">The event type to clear subscriptions for.</typeparam>
		public void Clear<T>()
		{
			lock (_subLock) {
				if (_subscriptions.TryGetValue(typeof(T), out var sublist)) {
					foreach (var sub in sublist) {
						sub.Disable();
					}
					sublist.Clear();
				}
			}
		}

		/// <summary>
		/// Clears all subscriptions in the event hub.
		/// </summary>
		public void ClearAll()
		{
			lock (_subLock) {
				foreach (var sublist in _subscriptions.Values) {
					foreach (var sub in sublist) {
						sub.Disable();
					}
				}
				_subscriptions.Clear();
			}
		}

		/// <summary>
		/// Publishes a new event to the hub, which is immediately dispatched to the interested subscribers.
		/// </summary>
		/// <typeparam name="T">The type of event to publish.</typeparam>
		/// <param name="event">The event to publish.</param>
		public void Publish<T>(T? @event)
		{
			lock (_subLock) {
				if (_subscriptions.TryGetValue(typeof(T), out var sublist)) {
					foreach (var sub in sublist) {
						sub.Dispatch(null, AppTime.Now, @event);
					}
				}
			}
		}

		/// <summary>
		/// Publishes a new event to the hub, which is immediately dispatched to the interested subscribers.
		/// </summary>
		/// <typeparam name="T">The type of event to publish.</typeparam>
		/// <param name="sender">The object that is publishing the event.</param>
		/// <param name="event">The event to publish.</param>
		public void Publish<T>(object? sender, T? @event)
		{
			lock (_subLock) {
				if (_subscriptions.TryGetValue(typeof(T), out var sublist)) {
					foreach (var sub in sublist) {
						sub.Dispatch(sender, AppTime.Now, @event);
					}
				}
			}
		}

		/// <summary>
		/// Creates a new subscription to a specific event type.
		/// </summary>
		/// <typeparam name="T">The event type to subscribe to.</typeparam>
		/// <param name="action">The action to call when an event of the correct type is published.</param>
		/// <returns>A token object representing the new subscription.</returns>
		public EventSubscription<T> Subscribe<T>(EventAction<T> action)
		{
			var sub = new EventSubscription<T>(this, action, null);
			lock (_subLock) {
				if (_subscriptions.TryGetValue(typeof(T), out var subs)) {
					subs.Add(sub);
				}
				else {
					(_subscriptions[typeof(T)] = new()).Add(sub);
				}
			}
			return sub;
		}

		/// <summary>
		/// Removes the subscription from this hub. Equivalent to calling <see cref="EventSubscription{T}.Dispose"/>.
		/// </summary>
		/// <typeparam name="T">The event type of the subscription.</typeparam>
		/// <param name="subscription">The subscription to remove.</param>
		public void Unsubscribe<T>(EventSubscription<T> subscription)
		{
			if (!subscription.Active) {
				return;
			}
			if (subscription.Hub != this) {
				throw new InvalidOperationException("Cannot unsubscribe an EventSubscription from a different hub");
			}

			lock (_subLock) {
				if (_subscriptions.TryGetValue(typeof(T), out var subs)) {
					subs.Remove(subscription);
					subscription._active = false;
				}
			}
		}

		/// <summary>
		/// Gets the number of objects subscribed to the event type.
		/// </summary>
		/// <typeparam name="T">The event type to check.</typeparam>
		public uint GetSubscriptionCount<T>()
		{
			lock (_subLock) {
				if (_subscriptions.TryGetValue(typeof(T), out var subs)) {
					return (uint)subs.Count;
				}
				return 0;
			}
		}
	}
}
