/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega.Graphics
{
	/// <summary>
	/// A buffer that holds vertex data for use in rendering.
	/// </summary>
	public unsafe sealed class VertexBuffer : DeviceBuffer
	{
		#region Fields
		/// <summary>
		/// The number of vertices represented by the data in the buffer.
		/// </summary>
		public readonly uint VertexCount;
		/// <summary>
		/// The layout of the data in the vertex buffer.
		/// </summary>
		public readonly VertexDescription VertexDescription;
		/// <summary>
		/// The buffer data stride (distance between adjacent vertex data).
		/// </summary>
		public uint Stride => VertexDescription.Stride;
		#endregion // Fields

		/// <summary>
		/// Create a new vertex buffer with optional pointer to initial data.
		/// </summary>
		/// <param name="vertexCount">The number of vertices in the buffer.</param>
		/// <param name="description">The layout of the vertices in the buffer.</param>
		/// <param name="data">The optional initial vertex data.</param>
		/// <param name="usage">The buffer usage policy.</param>
		public VertexBuffer(uint vertexCount, VertexDescription description, void* data = null, 
				BufferUsage usage = BufferUsage.Static)
			: base(vertexCount * description.Stride, ResourceType.VertexBuffer, usage, data)
		{
			VertexCount = vertexCount;
			VertexDescription = description.Duplicate();
		}

		/// <summary>
		/// Create a new vertex buffer with the data in the host buffer.
		/// </summary>
		/// <param name="vertexCount">The number of vertices in the buffer.</param>
		/// <param name="description">The layout of the vertices in the buffer.</param>
		/// <param name="data">The optional initial vertex data.</param>
		/// <param name="usage">The buffer usage policy.</param>
		public VertexBuffer(uint vertexCount, VertexDescription description, HostBuffer data,
				BufferUsage usage = BufferUsage.Static)
			: base(vertexCount * description.Stride, ResourceType.VertexBuffer, usage, data)
		{
			VertexCount = vertexCount;
			VertexDescription = description.Duplicate();
		}
	}
}
