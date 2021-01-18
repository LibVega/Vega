/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Threading;
using Vulkan;

namespace Vega.Graphics
{
	// Reference-counted shader module/bytecode object
	internal sealed unsafe class ShaderProgram : ResourceBase
	{
		#region Fields
		// Shader modules
		public readonly VkShaderModule Vert;
		public readonly VkShaderModule? Tesc;
		public readonly VkShaderModule? Tese;
		public readonly VkShaderModule? Geom;
		public readonly VkShaderModule Frag;

		// Reference counting
		public uint RefCount => _refCount;
		private uint _refCount = 0;
		#endregion // Fields

		public ShaderProgram(
			VkShaderModule vert,
			VkShaderModule? tesc,
			VkShaderModule? tese,
			VkShaderModule? geom,
			VkShaderModule frag)
				: base(ResourceType.ShaderProgram)
		{
			Vert = vert;
			Tesc = tesc;
			Tese = tese;
			Geom = geom;
			Frag = frag;
		}

		// Increment object reference
		public void IncRefCount() => Interlocked.Increment(ref _refCount);
		// Decrement ref count, destroy when ref == 0
		public void DecRefCount()
		{
			var newVal = Interlocked.Decrement(ref _refCount);
			if ((newVal == 0) && (Core.Instance is not null)) {
				Graphics.Resources.QueueDestroy(this);
			}
		}

		// Enumerates over the available shader modules
		internal IEnumerable<(VkShaderModule mod, ShaderStages stage)> EnumerateModules()
		{
			yield return (Vert, ShaderStages.Vertex);
			if (Tesc is not null) {
				// TODO
			}
			if (Tese is not null) {
				// TODO
			}
			if (Geom is not null) {
				// TODO
			}
			yield return (Frag, ShaderStages.Fragment);
		}

		#region ResourceBase
		/// <summary>
		/// Do <em><b>NOT</b></em> call Dispose() on ShaderLayout directly.
		/// </summary>
		public override void Dispose() =>
			throw new InvalidOperationException("LIBRARY BUG - Cannot call Dispose() on ShaderProgram directly");

		// No-op for this resource
		protected override void OnDispose(bool disposing) { }

		protected internal override void Destroy()
		{
			Vert?.DestroyShaderModule(null);
			Tesc?.DestroyShaderModule(null);
			Tese?.DestroyShaderModule(null);
			Geom?.DestroyShaderModule(null);
			Frag?.DestroyShaderModule(null);
		}
		#endregion // ResourceBase
	}
}
