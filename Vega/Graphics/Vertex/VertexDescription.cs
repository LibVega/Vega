/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Vega.Graphics
{
	/// <summary>
	/// Fully describes a vertex layout and its shader access rules from a collection of <see cref="VertexElement"/>s
	/// and some additional metadata.
	/// </summary>
	public sealed class VertexDescription : IEquatable<VertexDescription>
	{
		#region Fields
		/// <summary>
		/// The collection of elements that make up the vertex.
		/// </summary>
		public IReadOnlyList<VertexElement> Elements => _elements;
		private readonly VertexElement[] _elements;
		
		/// <summary>
		/// The collection of element binding locations, which matches <see cref="Elements"/> by index.
		/// </summary>
		public IReadOnlyList<uint> Locations => _locations;
		private readonly uint[] _locations;

		/// <summary>
		/// The stride, in bytes, of the vertex within the vertex buffer.
		/// </summary>
		public readonly uint Stride;

		/// <summary>
		/// The input rate for the vertex.
		/// </summary>
		public readonly VertexRate Rate;

		/// <summary>
		/// The number of binding slots taken up by this description, taking into account matrices and arrays.
		/// </summary>
		public uint BindingCount => (uint)Elements.Sum(e => e.BindingCount);

		// A precalculated hash code for faster comparisons and lookups
		private readonly int _hashCode;
		#endregion // Fields

		/// <summary>
		/// Fully describes a new vertex description.
		/// </summary>
		/// <param name="elements">The vertex elements.</param>
		/// <param name="locations">The shader binding locations for the elements.</param>
		/// <param name="stride">The stride, or <c>null</c> to auto-calculate.</param>
		/// <param name="rate">The input rate for the vertex data.</param>
		public VertexDescription(IEnumerable<VertexElement> elements, IEnumerable<uint> locations, uint? stride = null,
			VertexRate rate = VertexRate.Vertex)
		{
			_elements = elements.ToArray();
			if (_elements.Length == 0) {
				throw new ArgumentException("Cannot have a vertex description with zero elements.", nameof(elements));
			}
			_locations = locations.ToArray();
			if (_elements.Length != _locations.Length) {
				throw new ArgumentException("Mismatch between element and binding location lengths", nameof(locations));
			}
			Stride = stride.HasValue ? stride.Value : _elements.Max(e => e.Offset + e.Format.GetSize());
			Rate = rate;
			_hashCode = CalculateHash(rate, _elements, _locations);
		}

		/// <summary>
		/// Describes a new vertex description with pairs of binding locations and elements, stride is calcuated.
		/// </summary>
		/// <param name="rate">The vertex input rate.</param>
		/// <param name="elements">The pairs of binding locations and vertex elements.</param>
		public VertexDescription(VertexRate rate, params (uint location, VertexElement element)[] elements)
		{
			if (elements.Length == 0) {
				throw new ArgumentException("Cannot have a vertex description with zero elements.", nameof(elements));
			}
			_elements = elements.Select(p => p.element).ToArray();
			_locations = elements.Select(p => p.location).ToArray();
			Stride = _elements.Max(e => e.Offset + e.Format.GetSize());
			Rate = rate;
			_hashCode = CalculateHash(rate, _elements, _locations);
		}

		/// <summary>
		/// Describes a new vertex description, assigning adjacent binding locations to the passed elements.
		/// </summary>
		/// <param name="elements">The vertex elements.</param>
		/// <param name="stride">The stride, or <c>null</c> to auto-calculate.</param>
		/// <param name="rate">The input rate for the vertex data.</param>
		public VertexDescription(IEnumerable<VertexElement> elements, uint? stride = null, 
			VertexRate rate = VertexRate.Vertex)
		{
			_elements = elements.ToArray();
			if (_elements.Length == 0) {
				throw new ArgumentException("Cannot have a vertex description with zero elements.", nameof(elements));
			}
			_locations = Enumerable.Range(0, _elements.Length).Select(i => (uint)i).ToArray();
			Stride = stride.HasValue ? stride.Value : _elements.Max(e => e.Offset + e.Format.GetSize());
			Rate = rate;
			_hashCode = CalculateHash(rate, _elements, _locations);
		}

		/// <summary>
		/// Describes a new vertex description, automatically assigning binding locations and the stride.
		/// </summary>
		/// <param name="rate">The vertex input data.</param>
		/// <param name="elements">The vertex elements.</param>
		public VertexDescription(VertexRate rate, params VertexElement[] elements)
		{
			if (elements.Length == 0) {
				throw new ArgumentException("Cannot have a vertex description with zero elements.", nameof(elements));
			}
			_elements = elements;
			_locations = Enumerable.Range(0, _elements.Length).Select(i => (uint)i).ToArray();
			Stride = _elements.Max(e => e.Offset + e.Format.GetSize());
			Rate = rate;
			_hashCode = CalculateHash(rate, _elements, _locations);
		}

		/// <summary>
		/// Describes a new vertex description, with tightly packed formats and adjacent binding locations.
		/// </summary>
		/// <param name="rate">The vertex input rate.</param>
		/// <param name="formats">The element formats to create tightly packed elements for.</param>
		public VertexDescription(VertexRate rate, params VertexFormat[] formats)
		{
			if (formats.Length == 0) {
				throw new ArgumentException("Cannot have a vertex description with zero elements.", nameof(formats));
			}
			uint off = 0;
			_elements = formats.Select(fmt => {
				VertexElement elem = new(fmt, off);
				off += fmt.GetSize();
				return elem;
			}).ToArray();
			_locations = Enumerable.Range(0, _elements.Length).Select(i => (uint)i).ToArray();
			Stride = off;
			Rate = rate;
			_hashCode = CalculateHash(rate, _elements, _locations);
		}

		#region Overrides
		public override int GetHashCode() => _hashCode;

		public override string ToString() => $"[{Stride}:{Rate}:{{{String.Join(":", _elements)}}}]";

		public override bool Equals(object? obj) => (obj is IEquatable<VertexDescription> vd) && vd.Equals(this);

		bool IEquatable<VertexDescription>.Equals(VertexDescription? other) =>
			(other is not null) && (other._hashCode == _hashCode) && (other.Stride == Stride) && 
			(other.Rate == Rate) &&
			other._elements.SequenceEqual(_elements) && other._locations.SequenceEqual(_locations);
		#endregion // Overrides

		/// <summary>
		/// Enumerates over pairs of vertex elements and their binding locations.
		/// </summary>
		public IEnumerable<(uint slot, VertexElement element)> EnumerateElements()
		{
			for (int i = 0; i < _elements.Length; ++i) {
				yield return (_locations[i], _elements[i]);
			}
		}

		// Duplicates the description (deep copy)
		internal VertexDescription Duplicate() => new(_elements, _locations, Stride, Rate);

		// Calculates a hash code for a set of elements and locations
		private static int CalculateHash(VertexRate rate, VertexElement[] elements, uint[] locations)
		{
			HashCode code = new();
			code.Add(rate);
			for (int i = 0; i < elements.Length; ++i) {
				code.Add(elements[i].GetHashCode() ^ locations[i].GetHashCode());
			}
			return code.ToHashCode();
		}
	}
}
