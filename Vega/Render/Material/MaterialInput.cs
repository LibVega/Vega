/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

		public bool Equals(MaterialInput? input) => input?.CompareStates(this) ?? false;

		private int buildHash()
		{
			unchecked {
				int hash = Topology.GetHashCode();
				foreach (var desc in _vertices) {
					hash = ((hash << 5) + hash) ^ desc.GetHashCode();
				}
				return hash;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		internal bool CompareStates(MaterialInput input)
		{
			if ((Hash != input.Hash) || (Topology != input.Topology) || (_vertices.Length != input._vertices.Length)) {
				return false;
			}
			for (int i = 0; i < _vertices.Length; ++i) {
				if (!_vertices[i].Equals(input._vertices[i])) {
					return false;
				}
			}
			return true;
		}
	}
}
