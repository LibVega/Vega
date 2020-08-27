/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega
{
	/// <summary>
	/// Callback type for handling events being posted to <see cref="EventHub"/> instances.
	/// </summary>
	/// <typeparam name="T">The message type.</typeparam>
	/// <param name="sender">The object that sent the message.</param>
	/// <param name="time">The time (derived from <see cref="AppTime.Now"/>) that the message was posted.</param>
	/// <param name="data">The message data.</param>
	public delegate void EventAction<T>(object? sender, TimeSpan time, T? data);

	// Internal non-generic event subscription type with generic dispatch function
	internal interface IEventSubscription
	{
		void Dispatch(object? sender, TimeSpan time, object? data);
	}

	/// <summary>
	/// Represents a subscription to an event type posted to <see cref="EventHub"/> instances.
	/// </summary>
	/// <typeparam name="T">The event type represented by the subscription.</typeparam>
	public sealed class EventSubscription<T> : IEventSubscription, IDisposable
	{
		#region Fields
		private readonly WeakReference<EventHub> _hub;
		/// <summary>
		/// The event hub that this subscription belongs to. Will return <c>null</c> if the hub is no longer alive.
		/// </summary>
		public EventHub? Hub => _hub.TryGetTarget(out var target) ? target : null;
		/// <summary>
		/// The action that is called when the event is posted.
		/// </summary>
		public readonly EventAction<T> Action;
		/// <summary>
		/// The optional filter to use to select which events to pass to the action.
		/// </summary>
		public IEventFilter<T>? Filter;

		/// <summary>
		/// The type for the event this subscription is for.
		/// </summary>
		public Type EventType => typeof(T);

		internal bool _active = true;
		/// <summary>
		/// Gets if the subscription is active (has not been disposed, and the hub is still alive).
		/// </summary>
		public bool Active => _active && _hub.TryGetTarget(out _);
		#endregion // Fields

		internal EventSubscription(EventHub hub, EventAction<T> action, IEventFilter<T>? filter)
		{
			_hub = new(hub);
			Action = action;
			Filter = filter;
		}

		void IEventSubscription.Dispatch(object? sender, TimeSpan time, object? data)
		{
			var msgData = (T)data;
			if (Filter?.Accept(sender, time, msgData) ?? true) {
				Action(sender, time, msgData);
			}
		}

		/// <summary>
		/// Unsubscribes this subscription instance, if currently active.
		/// </summary>
		public void Dispose()
		{
			if (Active) {
				Hub!.Unsubscribe(this);
			}
			_active = false;
		}
	}
}
