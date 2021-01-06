/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;

namespace Vega
{
	// Manages the execution and lifetimes of coroutine instances
	internal static class CoroutineManager
	{
		private static readonly List<Coroutine> _Coroutines = new();
		private static readonly List<Coroutine> _NewCoroutines = new();
		private static readonly object _NewLock = new();
		private static bool _Ticking = false; // Cannot add to _Coroutines while ticking

		public static void Tick()
		{
			_Ticking = true;

			float 
				rdelta = (float)AppTime.RealDelta.TotalSeconds,
				sdelta = (float)AppTime.Delta.TotalSeconds;

			// Tick all coroutines
			foreach (var cor in _Coroutines) {
				if (!cor.Running) continue;

				// Update wait objects
				if (cor.WaitImpl.Time > 0) {
					float ntime = cor.WaitImpl.Time - (cor.UseUnscaledTime ? rdelta : sdelta);
					cor.WaitImpl = new(Math.Max(ntime, 0), null);
				}
				if ((!cor.WaitImpl.Coroutine?.Running) ?? false)
					cor.WaitImpl = new(0, null);

				// Tick and update
				if (!cor.Waiting) {
					++cor.TickCount;
					var ret = cor.Tick();

					if (ret == null) continue;
					else if (ReferenceEquals(ret, Coroutine.END)) cor.Running = false;
					else if (ret is Coroutine.WaitForTimeImpl waitImpl) cor.WaitImpl = new(waitImpl.Seconds, null);
					else if (ret is Coroutine retCor) cor.WaitImpl = new(0, retCor);
					else throw new CoroutineTickReturnException(ret);
				}
			}

			// Remove completed coroutines
			_Coroutines.RemoveAll(cor => {
				if (!cor.Running)
					cor.OnRemove();
				return !cor.Running;
			});

			// Add pending coroutines
			lock (_NewLock) {
				foreach (var cor in _NewCoroutines) {
					_Coroutines.Add(cor);
				}
				_NewCoroutines.Clear(); 
			}

			_Ticking = false;
		}

		// Adds a coroutine to be ticked
		public static void AddCoroutine(Coroutine cor)
		{
			cor.Running = true;
			lock (_NewLock) {
				(_Ticking ? _NewCoroutines : _Coroutines).Add(cor);
			}
		}

		// Cleanup all coroutines (when exiting)
		public static void Cleanup()
		{
			_Coroutines.ForEach(cor => cor.OnRemove());
			_Coroutines.Clear();
			_NewCoroutines.ForEach(cor => cor.OnRemove());
			_NewCoroutines.Clear();
		}
	}
}
