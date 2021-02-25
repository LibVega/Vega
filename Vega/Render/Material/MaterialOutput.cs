/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Vega.Render
{
	/// <summary>
	/// Describes how a <see cref="Material"/> instance updates output attachments.
	/// </summary>
	public sealed record MaterialOutput
	{
		#region Fields
		/// <summary>
		/// The blend states to use to update attachments when the associated material instance is in use.
		/// </summary>
		public IReadOnlyList<BlendState> BlendStates {
			get => _blendStates;
			init => _blendStates = value.ToArray();
		}
		private BlendState[] _blendStates = Array.Empty<BlendState>();

		/// <summary>
		/// The hash value for the output states.
		/// </summary>
		public int Hash => _hash ?? (_hash = buildHash()).Value;
		private int? _hash = null;
		#endregion // Fields

		/// <summary>
		/// Create a new output description.
		/// </summary>
		/// <param name="blendStates">The attachment blend states.</param>
		public MaterialOutput(params BlendState[] blendStates)
		{
			_blendStates = blendStates;
		}

		public override int GetHashCode() => Hash;

		public bool Equals(MaterialOutput? output) => output?.CompareStates(this) ?? false;

		private int buildHash()
		{
			unchecked {
				int hash = _blendStates.Length.GetHashCode();
				foreach (var state in _blendStates) {
					hash = ((hash << 5) + hash) ^ state.GetHashCode();
				}
				return hash;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		internal bool CompareStates(MaterialOutput output)
		{
			if ((Hash != output.Hash) || (_blendStates.Length != output._blendStates.Length)) {
				return false;
			}
			for (int i = 0; i < _blendStates.Length; ++i) {
				if (!_blendStates[i].CompareStates(output._blendStates[i])) {
					return false;
				}
			}
			return true;
		}
	}
}
