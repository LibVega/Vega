/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;

namespace Vega
{
	/// <summary>
	/// Defines a viewing volume for intersection operations.
	/// </summary>
	public sealed class Frustum : IEquatable<Frustum>
	{
		/// <summary>
		/// The number of planes that define the edges of a frustum volume.
		/// </summary>
		public const uint PLANE_COUNT = 6;
		/// <summary>
		/// The number of points that define the corners of a frustum volume.
		/// </summary>
		public const uint CORNER_COUNT = 8;

		#region Fields
		/// <summary>
		/// The view*projection matrix that describes the frustum.
		/// </summary>
		public Matrix Matrix {
			get => _matrix;
			set {
				_matrix = value;
				updateVolume();
			}
		}
		private Matrix _matrix;

		/// <summary>
		/// The planes that define the volume, in the order (near, far, top, bottom, left, right). Plane normals
		/// point away from the bounding volume.
		/// </summary>
		public IReadOnlyCollection<Plane3D> Planes => _planes;
		private readonly Plane3D[] _planes = new Plane3D[PLANE_COUNT];

		/// <summary>
		/// The corners that define the volume, in clockwise order starting at the top-left, with the near points
		/// first.
		/// </summary>
		public IReadOnlyCollection<Vec3> Corners => _corners;
		private readonly Vec3[] _corners = new Vec3[CORNER_COUNT];

		#region Planes
		/// <summary>
		/// The near plane of the volume.
		/// </summary>
		public ref readonly Plane3D Near => ref _planes[0];
		/// <summary>
		/// The far plane of the volume.
		/// </summary>
		public ref readonly Plane3D Far => ref _planes[1];
		/// <summary>
		/// The top plane of the volume.
		/// </summary>
		public ref readonly Plane3D Top => ref _planes[2];
		/// <summary>
		/// The bottom plane of the volume.
		/// </summary>
		public ref readonly Plane3D Bottom => ref _planes[3];
		/// <summary>
		/// The left plane of the volume.
		/// </summary>
		public ref readonly Plane3D Left => ref _planes[4];
		/// <summary>
		/// The right plane of the volume.
		/// </summary>
		public ref readonly Plane3D Right => ref _planes[5];
		#endregion // Planes

		#region Corners
		/// <summary>
		/// The near-top-left corner.
		/// </summary>
		public ref readonly Vec3 CornerNTL => ref _corners[0];
		/// <summary>
		/// The near-top-right corner.
		/// </summary>
		public ref readonly Vec3 CornerNTR => ref _corners[1];
		/// <summary>
		/// The near-bottom-right corner.
		/// </summary>
		public ref readonly Vec3 CornerNBR => ref _corners[2];
		/// <summary>
		/// The near-bottom-left corner.
		/// </summary>
		public ref readonly Vec3 CornerNBL => ref _corners[3];
		/// <summary>
		/// The far-top-left corner.
		/// </summary>
		public ref readonly Vec3 CornerFTL => ref _corners[4];
		/// <summary>
		/// The far-top-right corner.
		/// </summary>
		public ref readonly Vec3 CornerFTR => ref _corners[5];
		/// <summary>
		/// The far-bottom-right corner.
		/// </summary>
		public ref readonly Vec3 CornerFBR => ref _corners[6];
		/// <summary>
		/// The far-bottom-left corner.
		/// </summary>
		public ref readonly Vec3 CornerFBL => ref _corners[7];
		#endregion // Corners
		#endregion // Fields

		#region Ctor
		/// <summary>
		/// Create a default uninitialized frustum.
		/// </summary>
		public Frustum()
			: this(Matrix.Identity)
		{ }

		/// <summary>
		/// Create a new frustum from the pre-multiplied view*projection matrix.
		/// </summary>
		/// <param name="viewProjection">The premultiplied view*projection matrix.</param>
		public Frustum(in Matrix viewProjection)
		{
			_matrix = viewProjection;
			updateVolume();
		}

		/// <summary>
		/// Create a new frustum from the separate view and projection matrices.
		/// </summary>
		/// <param name="view">The view matrix for the frustum.</param>
		/// <param name="projection">The projection matrix for the frustum.</param>
		public Frustum(in Matrix view, in Matrix projection)
			: this(Matrix.Multiply(view, projection))
		{ }
		#endregion // Ctor

		#region Overrides
		public override bool Equals(object? obj) => (obj is Frustum f) && (f == this);

		bool IEquatable<Frustum>.Equals(Frustum? other) => other == this;

		public override int GetHashCode() => _matrix.GetHashCode();

		public override string ToString() => $"[Frustum:{_matrix}]";
		#endregion // Overrides

		// Re-calculates the planes and corners for the volume
		private void updateVolume()
		{
			ref readonly var m = ref _matrix;

			// Create the planes
			_planes[0] = new(-m.M20,         -m.M21,         -m.M22,          m.M23        );
			_planes[1] = new( m.M20 - m.M30,  m.M21 - m.M31,  m.M22 - m.M32, -m.M23 + m.M33);
			_planes[2] = new( m.M10 - m.M30,  m.M11 - m.M31,  m.M12 - m.M32, -m.M13 + m.M33);
			_planes[3] = new(-m.M30 - m.M10, -m.M31 - m.M11, -m.M32 - m.M12,  m.M33 + m.M13);
			_planes[4] = new(-m.M30 - m.M00, -m.M31 - m.M01, -m.M32 - m.M02,  m.M33 + m.M03);
			_planes[5] = new( m.M00 - m.M30,  m.M01 - m.M31,  m.M02 - m.M32, -m.M03 + m.M33);
			for (uint i = 0; i < PLANE_COUNT; ++i) {
				_planes[i] = _planes[i].Normalized;
			}

			// Create the corners
			CalculateIntersection(Near, Top,    Left,  out _corners[0]);
			CalculateIntersection(Near, Top,    Right, out _corners[1]);
			CalculateIntersection(Near, Bottom, Right, out _corners[2]);
			CalculateIntersection(Near, Bottom, Left,  out _corners[3]);
			CalculateIntersection(Far,  Top,    Left,  out _corners[4]);
			CalculateIntersection(Far,  Top,    Right, out _corners[5]);
			CalculateIntersection(Far,  Bottom, Right, out _corners[6]);
			CalculateIntersection(Far,  Bottom, Left,  out _corners[7]);
		}

		// Calculates the intersection point of the three planes
		private static void CalculateIntersection(in Plane3D p1, in Plane3D p2, in Plane3D p3, out Vec3 point)
		{
			// Get cross vectors
			Vec3.Cross(p1.Normal, p2.Normal, out var c12);
			Vec3.Cross(p2.Normal, p3.Normal, out var c23);
			Vec3.Cross(p3.Normal, p1.Normal, out var c31);

			// Get basis vectors
			var v1 = -p1.D * c23;
			var v2 = -p2.D * c31;
			var v3 = -p3.D * c12;

			// Calculate
			var scale = 1 / Vec3.Dot(p1.Normal, c23);
			point = (v1 + v2 + v3) * scale;
		}

		#region Operators
		public static bool operator == (Frustum? l, Frustum? r)
		{
			if (l is null || r is null) {
				return l is null == r is null;
			}
			return l._matrix == r._matrix;
		}

		public static bool operator != (Frustum? l, Frustum? r)
		{
			if (l is null || r is null) {
				return l is null != r is null;
			}
			return l._matrix != r._matrix;
		}
		#endregion // Operators
	}
}
