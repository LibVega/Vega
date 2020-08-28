/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;

namespace Vega
{
	// Internal implementation for IEnumerator based coroutines
	internal class EnumeratorCoroutine : Coroutine
	{
		private readonly IEnumerator<object?> _ticker;
		private readonly bool _unscaled;
		public override bool UseUnscaledTime => _unscaled;

		public EnumeratorCoroutine(IEnumerator<object?> ticker, bool unscaled)
		{
			_ticker = ticker;
			_unscaled = unscaled;
		}

		protected internal override object? Tick()
		{
			return _ticker.MoveNext() ? _ticker.Current : END;
		}
	}

	// Internal implementation for delayed and repeating action time-based coroutines
	internal class TimerCoroutine : EnumeratorCoroutine
	{
		public readonly Func<bool> Action;
		public readonly float Delay;
		public readonly float? Repeat;

		public TimerCoroutine(float delay, float? repeat, bool unscaled, Func<bool> action)
			: base(timer_func(delay, repeat, action), unscaled)
		{
			Action = action;
			Delay = delay;
			Repeat = repeat;
		}

		private	static IEnumerator<object?> timer_func(float delay, float? repeat, Func<bool> action)
		{
			yield return WaitForSeconds(delay);

			while (action() && repeat.HasValue) {
				yield return WaitForSeconds(repeat.Value);
			}
		}
	}
}
