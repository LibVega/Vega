/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Describes how a vertex buffer is interpreted into primitives by a Pipeline.
	/// </summary>
	public struct VertexInput : IEquatable<VertexInput>
	{
		/// <summary>
		/// The primitive restart index for 32-bit index buffers.
		/// </summary>
		public const uint RESTART_INDEX32 = UInt32.MaxValue;
		/// <summary>
		/// The primitive restart index for 16-bit index buffers.
		/// </summary>
		public const ushort RESTART_INDEX16 = UInt16.MaxValue;

		/// <summary>
		/// Default no-restart version of <see cref="Topology.PointList"/>.
		/// </summary>
		public static readonly VertexInput PointList = new(Topology.PointList, false);
		/// <summary>
		/// Default no-restart version of <see cref="Topology.LineList"/>.
		/// </summary>
		public static readonly VertexInput LineList = new(Topology.LineList, false);
		/// <summary>
		/// Default no-restart version of <see cref="Topology.LineStrip"/>.
		/// </summary>
		public static readonly VertexInput LineStrip = new(Topology.LineStrip, false);
		/// <summary>
		/// Default no-restart version of <see cref="Topology.TriangleList"/>.
		/// </summary>
		public static readonly VertexInput TriangleList = new(Topology.TriangleList, false);
		/// <summary>
		/// Default no-restart version of <see cref="Topology.TriangleStrip"/>.
		/// </summary>
		public static readonly VertexInput TriangleStrip = new(Topology.TriangleStrip, false);
		/// <summary>
		/// Default no-restart version of <see cref="Topology.TriangleFan"/>.
		/// </summary>
		public static readonly VertexInput TriangleFan = new(Topology.TriangleFan, false);

		#region Fields
		/// <summary>
		/// The processing topology of the verticies.
		/// </summary>
		public Topology Topology;
		/// <summary>
		/// If the primitive stream can be restarted (for example, breaking a tringle strip to start a new one).
		/// </summary>
		public bool RestartEnable;
		#endregion // Fields
		
		/// <summary>
		/// Describe a new vertex input state.
		/// </summary>
		/// <param name="topology">The primitive topology.</param>
		/// <param name="restart">If primitive restarting is enabled.</param>
		public VertexInput(Topology topology, bool restart = false)
		{
			Topology = topology;
			RestartEnable = restart;
		}

		// Fill the vulkan info object
		internal readonly void ToVk(out VkPipelineInputAssemblyStateCreateInfo vk) => vk = new(
			flags: VkPipelineInputAssemblyStateCreateFlags.NoFlags,
			topology: (VkPrimitiveTopology)Topology,
			primitiveRestartEnable: RestartEnable
		);

		#region Overrides
		public readonly override int GetHashCode() => Topology.GetHashCode() ^ RestartEnable.GetHashCode();

		public readonly override string ToString() => $"[{Topology}:{RestartEnable}]";

		public readonly override bool Equals(object? obj) => (obj is VertexInput vi) && (vi == this);

		readonly bool IEquatable<VertexInput>.Equals(VertexInput other) => other == this;
		#endregion // Overrides

		#region Operators
		public static bool operator == (in VertexInput l, in VertexInput r) =>
			(l.Topology == r.Topology) && (l.RestartEnable == r.RestartEnable);

		public static bool operator != (in VertexInput l, in VertexInput r) =>
			(l.Topology != r.Topology) || (l.RestartEnable != r.RestartEnable);

		public static implicit operator VertexInput (Topology topo) => new(topo, false);
		#endregion // Operators
	}

	/// <summary>
	/// The possible primitives that a raw vertex buffer can be interpreted as.
	/// </summary>
	public enum Topology : int
	{
		/// <summary>
		/// Interpret as individual disconnected points.
		/// </summary>
		PointList = VkPrimitiveTopology.PointList,
		/// <summary>
		/// Interpret as pairs of points that create individual, disconnected line segments.
		/// </summary>
		LineList = VkPrimitiveTopology.LineList,
		/// <summary>
		/// Interpret as a set of points creating a continunous connected line.
		/// </summary>
		LineStrip = VkPrimitiveTopology.LineStrip,
		/// <summary>
		/// Interpret as point triples that create individual, disconnected triangles.
		/// </summary>
		TriangleList = VkPrimitiveTopology.TriangleList,
		/// <summary>
		/// Interpret as a collection of connected triangles, each sharing a side with the next.
		/// </summary>
		TriangleStrip = VkPrimitiveTopology.TriangleStrip,
		/// <summary>
		/// Interpret as a collection of connected triangles, each sharing the first point in the vertex stream.
		/// </summary>
		TriangleFan = VkPrimitiveTopology.TriangleFan
	}
}
