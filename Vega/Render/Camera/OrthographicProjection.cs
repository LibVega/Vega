/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Render
{
	/// <summary>
	/// Contains the parameters for a orthographic camera projection.
	/// </summary>
	public sealed class OrthographicProjection : ICameraProjection
	{
		#region Fields
		/// <summary>
		/// The width of the camera plane.
		/// </summary>
		public float Width
		{
			get => _width;
			set {
				_width = value;
				_dirty = true;
			}
		}
		private float _width;

		/// <summary>
		/// The height of the camera plane.
		/// </summary>
		public float Height
		{
			get => _height;
			set {
				_height = value;
				_dirty = true;
			}
		}
		private float _height;

		/// <summary>
		/// The distance to the near view plane.
		/// </summary>
		public float Near
		{
			get => _near;
			set {
				_near = value;
				_dirty = true;
			}
		}
		private float _near;

		/// <summary>
		/// The distance to the far view plane.
		/// </summary>
		public float Far
		{
			get => _far;
			set {
				_far = value;
				_dirty = true;
			}
		}
		private float _far;

		/// <inheritdoc cref="ICameraProjection.Dirty"/>
		public bool Dirty => _dirty;
		private bool _dirty = true;
		#endregion // Fields

		/// <summary>
		/// Describe a new orthographic projection.
		/// </summary>
		/// <param name="fov">The initial width.</param>
		/// <param name="aspect">The initial height.</param>
		/// <param name="near">The initial near plane distance.</param>
		/// <param name="far">The initial far plane distance.</param>
		public OrthographicProjection(float width, float height, float near = 0.01f, float far = 1000.0f)
		{
			_width = width;
			_height = height;
			_near = near;
			_far = far;
		}

		/// <inheritdoc cref="ICameraProjection.CreateProjectionMatrix(out Matrix)"/>
		void ICameraProjection.CreateProjectionMatrix(out Matrix matrix)
		{
			Matrix.CreateOrthographic(_width, _height, _near, _far, out matrix);
			_dirty = false;
		}
	}
}
