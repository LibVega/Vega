﻿/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Vega.Graphics;

namespace Vega.Render
{
	/// <summary>
	/// Describes the vertex data input layout for a <see cref="Material"/> instance.
	/// </summary>
	public sealed record MaterialInput
	{
		#region Fields
		/// <summary>
		/// The vertex input topology.
		/// </summary>
		public Topology Topology { get; init; } = Topology.PointList;
		/// <summary>
		/// The vertex winding order that specifies the "front face".
		/// </summary>
		public Winding Winding { get; init; } = Winding.CW;
		/// <summary>
		/// If primitive stream resetting is enabled using <see cref="UInt16.MaxValue"/> or <see cref="UInt32.MaxValue"/>.
		/// </summary>
		public bool RestartEnabled { get; init; } = false;

		/// <summary>
		/// The set of vertex data layouts used to input data into the material.
		/// </summary>
		public IReadOnlyList<VertexDescription> Vertices {
			get => _vertices;
			init => _vertices = value.ToArray();
		}
		private VertexDescription[] _vertices = Array.Empty<VertexDescription>();

		/// <summary>
		/// The hash value for this material input.
		/// </summary>
		public int Hash => _hash ?? (_hash = buildHash()).Value;
		private int? _hash = null;
		#endregion // Fields

		/// <summary>
		/// Describes a new material input with a given topology and vertex data layout.
		/// </summary>
		/// <param name="topology">The primitive topology of the vertex data.</param>
		/// <param name="vertices">The collection of vertex data descriptions.</param>
		public MaterialInput(Topology topology, params VertexDescription[] vertices)
		{
			Topology = topology;
			_vertices = vertices;
		}
		/// <summary>
		/// Describes a new material input with a given topology and vertex data layout.
		/// </summary>
		/// <param name="topology">The primitive topology of the vertex data.</param>
		/// <param name="vertices">The collection of vertex data descriptions.</param>
		public MaterialInput(Topology topology, IEnumerable<VertexDescription> vertices)
		{
			Topology = topology;
			_vertices = vertices.ToArray();
		}

		public override int GetHashCode() => Hash;

		public bool Equals(MaterialInput? input) => 
			(input is not null) && (Hash == input.Hash) &&
			(Topology == input.Topology) && (Winding == input.Winding) && (RestartEnabled == input.RestartEnabled) &&
			(_vertices.Length == input._vertices.Length) && Enumerable.SequenceEqual(_vertices, input._vertices);

		private int buildHash()
		{
			unchecked {
				int hash = Topology.GetHashCode();
				hash = ((hash << 5) + hash) ^ Winding.GetHashCode();
				hash = ((hash << 5) + hash) ^ RestartEnabled.GetHashCode();
				foreach (var desc in _vertices) {
					hash = ((hash << 5) + hash) ^ desc.GetHashCode();
				}
				return hash;
			}
		}
	}
}