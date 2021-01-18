/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Runtime.InteropServices;

namespace Vega.Graphics
{
	/// <summary>
	/// A buffer that holds index values for indexed render commands.
	/// </summary>
	public unsafe sealed class IndexBuffer : DeviceBuffer
	{
		#region Fields
		/// <summary>
		/// The number of indicies stored in the buffer.
		/// </summary>
		public readonly uint IndexCount;
		/// <summary>
		/// The index size type.
		/// </summary>
		public readonly IndexType IndexType;
		#endregion // Fields

		/// <summary>
		/// Create a new index buffer with uninitialized data.
		/// </summary>
		/// <param name="indexCount">The number of indicies in the buffer.</param>
		/// <param name="type">The index type.</param>
		/// <param name="usage">The buffer usage policy.</param>
		public IndexBuffer(uint indexCount, IndexType type, BufferUsage usage = BufferUsage.Dynamic)
			: base(indexCount * (ulong)type, ResourceType.IndexBuffer, usage, (void*)null)
		{
			IndexCount = indexCount;
			IndexType = type;
		}

		/// <summary>
		/// Create a new index buffer with data initialized from a <see cref="HostBuffer"/>.
		/// </summary>
		/// <param name="indexCount">The number of indicies in the buffer.</param>
		/// <param name="type">The index type.</param>
		/// <param name="indexData">The initial index data, which must be large enough to supply the buffer.</param>
		/// <param name="dataOffset">The offset into the data source buffer from which to copy.</param>
		/// <param name="usage">The buffer usage policy.</param>
		public IndexBuffer(uint indexCount, IndexType type, HostBuffer indexData, ulong dataOffset = 0,
				BufferUsage usage = BufferUsage.Dynamic)
			: base(indexCount * (uint)type, ResourceType.IndexBuffer, usage, indexData, dataOffset)
		{
			IndexCount = indexCount;
			IndexType = type;
		}

		/// <summary>
		/// Create a new index buffer with <see cref="IndexType.Short"/> data.
		/// </summary>
		/// <param name="indexCount">The number of indicies in the buffer.</param>
		/// <param name="indexData">The initial index data.</param>
		/// <param name="usage">The buffer usage policy.</param>
		public IndexBuffer(uint indexCount, ushort* indexData, BufferUsage usage = BufferUsage.Static)
			: base(indexCount * 2, ResourceType.IndexBuffer, usage, indexData)
		{
			IndexCount = indexCount;
			IndexType = IndexType.Short;
		}

		/// <summary>
		/// Create a new buffer to contain the <see cref="IndexType.Short"/> data in the span.
		/// </summary>
		/// <param name="indexData">The index data for the buffer.</param>
		/// <param name="usage">The buffer usage policy.</param>
		public IndexBuffer(ReadOnlySpan<ushort> indexData, BufferUsage usage = BufferUsage.Static)
			: base((ulong)indexData.Length * 2, ResourceType.IndexBuffer, usage, MemoryMarshal.AsBytes(indexData))
		{
			IndexCount = (uint)indexData.Length;
			IndexType = IndexType.Short;
		}

		/// <summary>
		/// Create a new index buffer with <see cref="IndexType.Int"/> data.
		/// </summary>
		/// <param name="indexCount">The number of indicies in the buffer.</param>
		/// <param name="indexData">The initial index data.</param>
		/// <param name="usage">The buffer usage policy.</param>
		public IndexBuffer(uint indexCount, uint* indexData, BufferUsage usage = BufferUsage.Static)
			: base(indexCount * 4, ResourceType.IndexBuffer, usage, indexData)
		{
			IndexCount = indexCount;
			IndexType = IndexType.Int;
		}

		/// <summary>
		/// Create a new buffer to contain the <see cref="IndexType.Int"/> data in the span.
		/// </summary>
		/// <param name="indexData">The index data for the buffer.</param>
		/// <param name="usage">The buffer usage policy.</param>
		public IndexBuffer(ReadOnlySpan<uint> indexData, BufferUsage usage = BufferUsage.Static)
			: base((ulong)indexData.Length * 4, ResourceType.IndexBuffer, usage, MemoryMarshal.AsBytes(indexData))
		{
			IndexCount = (uint)indexData.Length;
			IndexType = IndexType.Int;
		}

		#region Data
		/// <summary>
		/// Updates the index buffer data with the passed data. Only valid on non-Static buffers.
		/// </summary>
		/// <param name="data">The index data to update the buffer with.</param>
		/// <param name="indexCount">The number of indices to update.</param>
		/// <param name="indexOffset">The offset into the buffer, in indices, to which to copy.</param>
		public void SetData(ushort* data, uint indexCount, uint indexOffset)
		{
			if (IndexType != IndexType.Short) {
				throw new InvalidOperationException("Cannot update 4-byte index buffer with 2-byte data");
			}
			SetDataImpl(data, indexCount * 2, indexOffset * 2);
		}

		/// <summary>
		/// Updates the index buffer data with the passed data. Only valid on non-Static buffers.
		/// </summary>
		/// <param name="data">The index data to update the buffer with.</param>
		/// <param name="indexOffset">The offset into the buffer, in indices, to which to copy.</param>
		public void SetData(ReadOnlySpan<ushort> data, uint indexOffset)
		{
			if (IndexType != IndexType.Short) {
				throw new InvalidOperationException("Cannot update 4-byte index buffer with 2-byte data");
			}
			SetDataImpl(MemoryMarshal.AsBytes(data), indexOffset * 2);
		}

		/// <summary>
		/// Updates the index buffer data with the passed data. Only valid on non-Static buffers.
		/// </summary>
		/// <param name="data">The index data to update the buffer with.</param>
		/// <param name="indexCount">The number of indices to update.</param>
		/// <param name="indexOffset">The offset into the buffer, in indices, to which to copy.</param>
		public void SetData(uint* data, uint indexCount, uint indexOffset)
		{
			if (IndexType != IndexType.Int) {
				throw new InvalidOperationException("Cannot update 2-byte index buffer with 4-byte data");
			}
			SetDataImpl(data, indexCount * 4, indexOffset * 4);
		}

		/// <summary>
		/// Updates the index buffer data with the passed data. Only valid on non-Static buffers.
		/// </summary>
		/// <param name="data">The index data to update the buffer with.</param>
		/// <param name="indexOffset">The offset into the buffer, in indices, to which to copy.</param>
		public void SetData(ReadOnlySpan<uint> data, uint indexOffset)
		{
			if (IndexType != IndexType.Int) {
				throw new InvalidOperationException("Cannot update 2-byte index buffer with 4-byte data");
			}
			SetDataImpl(MemoryMarshal.AsBytes(data), indexOffset * 4);
		}

		/// <summary>
		/// Updates the index buffer data with data from the passed host buffer. Only valid on non-Static buffers.
		/// </summary>
		/// <param name="data">The data buffer from which to copy.</param>
		/// <param name="indexCount">The number of indices to update.</param>
		/// <param name="srcOffset">The offset into the source buffer, in bytes.</param>
		/// <param name="indexOffset">The offset into the buffer, in indices, to which to copy.</param>
		public void SetData(HostBuffer data, uint indexCount, uint srcOffset, uint indexOffset)
		{
			var indexSize = (IndexType == IndexType.Short) ? 2u : 4u;
			SetDataImpl(data, indexCount * indexSize, srcOffset, indexOffset * indexSize);
		}
		#endregion // Data
	}
}
