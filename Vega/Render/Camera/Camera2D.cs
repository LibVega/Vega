/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Render
{
	/// <summary>
	/// Implements common camera functionality for views in right-handed 2D space (+x right, +y up).
	/// </summary>
	public sealed class Camera2D
	{
		#region Fields
		/// <summary>
		/// The location of the center of the camera view.
		/// </summary>
		public Vec2 Position {
			get => _position;
			set {
				_position = value;
				_viewDirty = true;
				_viewProjectionDirty = true;
			}
		}
		private Vec2 _position;

		/// <summary>
		/// The camera roll angle (in radians). A positive roll results in a counter-clockwise rotation in view space.
		/// </summary>
		public float Roll {
			get => _roll;
			set {
				_roll = value;
				_viewDirty = true;
				_viewProjectionDirty = true;
			}
		}
		private float _roll;

		/// <summary>
		/// The dimensions of the camera view.
		/// </summary>
		public Vec2 Size {
			get => _size;
			set {
				_size = value;
				_projectionDirty = true;
				_viewProjectionDirty = true;
			}
		}
		private Vec2 _size;

		/// <summary>
		/// The bounding rectangle of the camera view.
		/// </summary>
		public RectF Bounds {
			get => new(_position - Size / 2, Size);
			set {
				_position = value.Center;
				_size = value.Size;
				_projectionDirty = true;
				_viewProjectionDirty = true;
			}
		}

		/// <summary>
		/// The distance to the near view plane.
		/// </summary>
		public float Near {
			get => _near;
			set {
				_near = value;
				_projectionDirty = true;
				_viewProjectionDirty = true;
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
				_projectionDirty = true;
				_viewProjectionDirty = true;
			}
		}
		private float _far;

		#region Matrix
		/// <summary>
		/// The current camera view matrix.
		/// </summary>
		public ref readonly Matrix ViewMatrix {
			get {
				if (_viewDirty) {
					Vec3 pos = new(_position.X, _position.Y, _near * -2);
					Vec3 look = new(pos.X, pos.Y, _far);
					Vec3 up = new(MathF.Sin(_roll), MathF.Cos(_roll), 0);
					Matrix.CreateLookAt(pos, look, up, out _viewMatrix);
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
					Matrix.CreateOrthographic(_size.X, _size.Y, _near, _far, out _projectionMatrix);
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
				if (_viewProjectionDirty) {
					Matrix.Multiply(ViewMatrix, ProjectionMatrix, out _viewProjectionMatrix);
					_viewProjectionDirty = false;
				}
				return ref _viewProjectionMatrix;
			}
		}
		private Matrix _viewProjectionMatrix = Matrix.Identity;
		private bool _viewProjectionDirty = true;
		#endregion // Matrix
		#endregion // Fields

		/// <summary>
		/// Create a new camera with the given view parameters.
		/// </summary>
		/// <param name="position">The initial position of the camera.</param>
		/// <param name="width">The initial width of the camera view.</param>
		/// <param name="height">The initial height of the camera view.</param>
		/// <param name="near">The camera near plane distance.</param>
		/// <param name="far">The camera far plane distance.</param>
		public Camera2D(in Vec2 position, float width, float height, float near = 0.01f, float far = 1000f)
		{
			_position = position;
			_size = new(width, height);
			_near = near;
			_far = far;
		}

		/// <summary>
		/// Create a new camera with the given view parameters centered at (0, 0).
		/// </summary>
		/// <param name="width">The initial width of the camera view.</param>
		/// <param name="height">The initial height of the camera view.</param>
		/// <param name="near">The camera near plane distance.</param>
		/// <param name="far">The camera far plane distance.</param>
		public Camera2D(float width, float height, float near = 0.01f, float far = 1000f)
			: this(Vec2.Zero, width, height, near, far)
		{ }
	}
}
