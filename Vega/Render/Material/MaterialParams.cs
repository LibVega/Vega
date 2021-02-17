/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.CompilerServices;

namespace Vega.Render
{
	/// <summary>
	/// The vertex input parameters for <see cref="Material"/> types.
	/// </summary>
	public struct MaterialParams
	{
		#region Fields
		/// <summary>
		/// The vertex input topology.
		/// </summary>
		public Topology Topology;
		/// <summary>
		/// The vertex winding order that specifies the "front face".
		/// </summary>
		public Winding Winding;
		/// <summary>
		/// If primitive stream resetting is enabled using <see cref="UInt16.MaxValue"/> or <see cref="UInt32.MaxValue"/>.
		/// </summary>
		public bool RestartEnabled;
		#endregion // Fields

		/// <summary>
		/// Create a new set of material parameters.
		/// </summary>
		/// <param name="topology">The vertex input topology.</param>
		/// <param name="winding">The vertex winding.</param>
		/// <param name="restartEnabled">If primitive stream restart is enabled.</param>
		public MaterialParams(Topology topology, Winding winding = Winding.CW, bool restartEnabled = false)
		{
			Topology = topology;
			Winding = winding;
			RestartEnabled = restartEnabled;
		}

		// Implicit cast from Topology
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator MaterialParams(Topology topology) => new(topology);
	}
}
