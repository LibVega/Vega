/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Render
{
	/// <summary>
	/// Interface for types that implement camera projection matrix calculations.
	/// </summary>
	public interface ICameraProjection
	{
		/// <summary>
		/// Gets if the values defining the projection have been changed since the last call to
		/// <see cref="CreateProjectionMatrix(out Matrix)"/>.
		/// </summary>
		bool Dirty { get; }

		/// <summary>
		/// Called to create the projection matrix.
		/// </summary>
		/// <param name="matrix">The matrix to calculte the projection values into.</param>
		void CreateProjectionMatrix(out Matrix matrix);
	}
}
