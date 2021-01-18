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

		#region Data
		/// <summary>
		/// Updates the vertex buffer data with the passed data. Only allowed on non-Static buffers.
		/// </summary>
		/// <param name="data">Pointer to the data to upload to the buffer.</param>
		/// <param name="vertexCount">The number of vertices to update in the buffer.</param>
		/// <param name="vertexOffset">The offset into the buffer, in vertices, to which to copy.</param>
		public void SetData(void* data, uint vertexCount, uint vertexOffset) =>
			SetDataImpl(data, vertexCount * Stride, vertexOffset * Stride);

		/// <summary>
		/// Updates the vertex buffer data with the passed data. Only allowed on non-Static buffers.
		/// </summary>
		/// <param name="data">The data to update to the buffer, must have length multiple of the stride.</param>
		/// <param name="vertexOffset">The offset into the buffer, in vertices, to which to copy.</param>
		public void SetData(ReadOnlySpan<byte> data, uint vertexOffset)
		{
			if ((data.Length % Stride) != 0) {
				throw new ArgumentException("Data span length must be multiple of vertex stride", nameof(data));
			}
			SetDataImpl(data, vertexOffset);
		}

		/// <summary>
		/// Updates the vertex buffer data with data from the passed host buffer. Only allowed on non-Static buffers.
		/// </summary>
		/// <param name="data">The host buffer to update from.</param>
		/// <param name="vertexCount">The number of vertices to update.</param>
		/// <param name="srcOffset">The offset into <paramref name="data"/>, in bytes, from which to copy.</param>
		/// <param name="dstVertexOffset">The offset into the buffer, in vertices, to which to copy.</param>
		public void SetData(HostBuffer data, uint vertexCount, uint srcOffset, uint dstVertexOffset) =>
			SetDataImpl(data, vertexCount * Stride, srcOffset, dstVertexOffset * Stride);
		#endregion // Data
	}
}
