/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.IO;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Describes a shader program written in VSL, and its metadata.
	/// </summary>
	public unsafe sealed class ShaderProgram : ResourceBase
	{
		#region Fields
		/// <summary>
		/// The reflection information for this shader program.
		/// </summary>
		public readonly ShaderInfo Info;

		// The shader modules in the program
		internal readonly VkShaderModule VertexModule;
		internal readonly VkShaderModule FragmentModule;
		#endregion // Fields

		internal ShaderProgram(ShaderInfo info, VkShaderModule vertMod, VkShaderModule fragMod)
			: base(ResourceType.Shader)
		{
			Info = info;
			VertexModule = vertMod;
			FragmentModule = fragMod;
		}

		#region ResourceBase
		protected override void OnDispose(bool disposing)
		{
			if (Core.Instance is not null) {
				Core.Instance!.Graphics.Resources.QueueDestroy(this);
			}
			else {
				Destroy();
			}
		}

		protected internal override void Destroy()
		{
			VertexModule.DestroyShaderModule(null);
			FragmentModule.DestroyShaderModule(null);
		}
		#endregion // ResourceBase

		/// <summary>
		/// Loads a new shader program from a <em>compiled</em> VSL shader file.
		/// <para>
		/// The shader file must be a compiled VSL file (.vbc), <em>NOT</em> a raw shader source.
		/// </para>
		/// </summary>
		/// <param name="path">The path to the compiled file.</param>
		/// <returns>The loaded shader program.</returns>
		public static ShaderProgram LoadFile(string path)
		{
			try {
				if (!File.Exists(path)) {
					throw new InvalidShaderException(path, $"Shader file '{path}' does not exist");
				}
				using var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				VSL.LoadStream(path, file, out var info, out var vertMod, out var fragMod);
				return new(info, vertMod, fragMod);
			}
			catch (InvalidShaderException) { throw; }
			catch (Exception e) {
				throw new InvalidShaderException(path, e.Message, e);
			}
		}

		// Loads an embedded resource shader
		internal static ShaderProgram LoadInternalResource(string resName)
		{
			try {
				var asm = typeof(ShaderProgram).Assembly.GetManifestResourceStream(resName);
				if (asm is null) {
					throw new InvalidShaderException(resName, $"Failed to load embedded shader '{resName}'");
				}
				VSL.LoadStream(resName, asm, out var info, out var vertMod, out var fragMod);
				return new(info, vertMod, fragMod);
			}
			catch (InvalidShaderException) { throw; }
			catch (Exception e) {
				throw new InvalidShaderException(resName, e.Message, e);
			}
		}
	}
}
