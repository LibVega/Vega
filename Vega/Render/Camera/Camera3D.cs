/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Render
{
	/// <summary>
	/// Implements common camera-like functionality for views in 3D space.
	/// </summary>
	public sealed class Camera3D
	{
		#region Fields
		/// <summary>
		/// The position of the camera.
		/// </summary>
		public Vec3 Position {
			get => _position;
			set {
				_position = value;
				_viewDirty = true;
			}
		}
		private Vec3 _position;

		/// <summary>
		/// The look-at target of the camera.
		/// </summary>
		public Vec3 Target {
			get => _target;
			set {
				_target = value;
				_viewDirty = true;
			}
		}
		private Vec3 _target;

		/// <summary>
		/// The angle of roll around the look direction. Positive roll indicates a clockwise rotation in view space.
		/// </summary>
		public float Roll {
			get => _roll;
			set {
				_roll = value;
				_viewDirty = true;
			}
		}
		private float _roll = 0;

		/// <summary>
		/// The parameters for the camera projection.
		/// </summary>
		public ICameraProjection Projection {
			get => _projection;
			set {
				_projection = value;
				_projectionDirty = true;
			}
		}
		private ICameraProjection _projection;

		#region Matrix
		/// <summary>
		/// The current camera view matrix.
		/// </summary>
		public ref readonly Matrix ViewMatrix {
			get {
				if (_viewDirty) {
					var dir = _target - _position;
					Matrix.CreateAxisRotation(dir.Normalized, _roll, out var rotMat);
					Matrix.CreateLookAt(_position, _target, rotMat * Vec3.Up, out _viewMatrix);
					_viewDirty = false;
				}
				return ref _viewMatrix;
			}
		}
		private Matrix _viewMatrix = Matrix.Identity;
		private bool _viewDirty = true;

		/// <summary>
		/// The current camera projection matrix.
		/// </summary>
		public ref readonly Matrix ProjectionMatrix { 
			get {
				if (_projectionDirty) {
					_projection.CreateProjectionMatrix(out _projectionMatrix);
					_projectionDirty = false;
				}
				return ref _projectionMatrix;
			}
		}
		private Matrix _projectionMatrix = Matrix.Identity;
		private bool _projectionDirty = true;

		/// <summary>
		/// The combined view and projection matrices.
		/// </summary>
		public ref readonly Matrix ViewProjectionMatrix {
			get {
				if (_viewDirty || _projectionDirty || _projection.Dirty) {
					Matrix.Multiply(ViewMatrix, ProjectionMatrix, out _viewProjectionMatrix);
				}
				return ref _viewProjectionMatrix;
			}
		}
		private Matrix _viewProjectionMatrix = Matrix.Identity;
		#endregion // Matrix
		#endregion // Fields

		/// <summary>
		/// Create a new camera at (10, 10, 10), looking at (0, 0, 0), with the given projection.
		/// </summary>
		/// <param name="projection">The projection to use for the camera.</param>
		public Camera3D(ICameraProjection projection)
		{
			_projection = projection;
			_target = Vec3.Zero;
			_position = new(10, 10, 10);
		}

		/// <summary>
		/// Create a new camera with the given projection and view parameters.
		/// </summary>
		/// <param name="projection">The projection to use for the camera.</param>
		/// <param name="position">The initial camera position.</param>
		/// <param name="target">The initial camera target.</param>
		/// <param name="roll">The initial camera roll.</param>
		public Camera3D(ICameraProjection projection, in Vec3 position, in Vec3 target, float roll = 0)
		{
			_projection = projection;
			_position = position;
			_target = target;
			_roll = roll;
		}
	}
}
