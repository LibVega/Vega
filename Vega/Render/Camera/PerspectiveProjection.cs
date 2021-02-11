/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Render
{
	/// <summary>
	/// Contains the parameters for a perspective camera projection.
	/// </summary>
	public sealed class PerspectiveProjection : ICameraProjection
	{
		#region Fields
		/// <summary>
		/// The angle (in radians) of the horizontal field of view of the camera.
		/// </summary>
		public float FieldOfView {
			get => _fov;
			set {
				_fov = value;
				_dirty = true;
			}
		}
		private float _fov;

		/// <summary>
		/// The aspect ratio of the camera (width / height).
		/// </summary>
		public float Aspect {
			get => _aspect;
			set {
				_aspect = value;
				_dirty = true;
			}
		}
		private float _aspect;

		/// <summary>
		/// The distance to the near view plane.
		/// </summary>
		public float Near {
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
		public float Far {
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
		/// Describe a new perspective projection.
		/// </summary>
		/// <param name="fov">The initial field-of-view.</param>
		/// <param name="aspect">The initial aspect ratio.</param>
		/// <param name="near">The initial near plane distance.</param>
		/// <param name="far">The initial far plane distance.</param>
		public PerspectiveProjection(float fov, float aspect, float near = 0.01f, float far = 1000.0f)
		{
			_fov = fov;
			_aspect = aspect;
			_near = near;
			_far = far;
		}

		/// <inheritdoc cref="ICameraProjection.CreateProjectionMatrix(out Matrix)"/>
		void ICameraProjection.CreateProjectionMatrix(out Matrix matrix)
		{
			Matrix.CreatePerspective(_fov, _aspect, _near, _far, out matrix);
			_dirty = false;
		}
	}
}
