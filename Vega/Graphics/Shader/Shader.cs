/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.IO;
using Vega.Content;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Represets a specific set of SPIR-V shader stages as a complete shader program. All shaders must have at least
	/// the vertex and fragment stages. Both tessellation stages must be present if used.
	/// </summary>
	public unsafe sealed class Shader : ResourceBase
	{
		// Magic number for SPIR-V files (little endian)
		private const uint SPIRV_MAGIC = 0x07230203;

		#region Fields
		/// <summary>
		/// The shader stages that are present in the program.
		/// </summary>
		public readonly ShaderStages Stages;

		// Shader modules
		internal readonly VkShaderModule VertexModule;
		internal readonly VkShaderModule? TessControlModule;
		internal readonly VkShaderModule? TessEvalModule;
		internal readonly VkShaderModule? GeometryModule;
		internal readonly VkShaderModule FragmentModule;
		#endregion // Fields

		#region File Ctor
		/// <summary>
		/// Create a new shader program with the vertex and fragment stages from bytecode files.
		/// </summary>
		/// <param name="vPath">The path to the vertex stage bytecode file.</param>
		/// <param name="fPath">The path to the fragment stage bytecode file.</param>
		public Shader(string vPath, string fPath)
			: this(vPath, null, null, null, fPath)
		{ }

		/// <summary>
		/// Create a new shader program with the vertex, geometry, and fragment stages from bytecode files.
		/// </summary>
		/// <param name="vPath">The path to the vertex stage bytecode file.</param>
		/// <param name="gPath">The path to the geometry stage bytecode file.</param>
		/// <param name="fPath">The path to the fragment stage bytecode file.</param>
		public Shader(string vPath, string gPath, string fPath)
			: this(vPath, null, null, gPath, fPath)
		{ }

		/// <summary>
		/// Create a new shader program with the vertex, tessellation, and fragment stages from bytecode files.
		/// </summary>
		/// <param name="vPath">The path to the vertex stage bytecode file.</param>
		/// <param name="tcPath">The path to the tessellation control stage bytecode file.</param>
		/// <param name="tePath">The path to the tessellation evaluation stage bytecode file.</param>
		/// <param name="fPath">The path to the fragment stage bytecode file.</param>
		public Shader(string vPath, string tcPath, string tePath, string fPath)
			: this(vPath, tcPath, tePath, null, fPath)
		{ }

		/// <summary>
		/// Create a new shader program with all stages explicitly specified from bytecode files.
		/// </summary>
		/// <param name="vPath">The path to the vertex stage bytecode file.</param>
		/// <param name="tcPath">The optional path to the tessellation control stage bytecode file.</param>
		/// <param name="tePath">The optional path to the tessellation evaluation stage bytecode file.</param>
		/// <param name="gPath">The optional path to the geometry stage bytecode file.</param>
		/// <param name="fPath">The path to the fragment stage bytecode file.</param>
		public Shader(string vPath, string? tcPath, string? tePath, string? gPath, string fPath)
			: base(ResourceType.Shader)
		{
			// Validate
			if (tcPath?.Length == 0) {
				tcPath = null;
			}
			if (tePath?.Length == 0) {
				tePath = null;
			}
			if (gPath?.Length == 0) {
				gPath = null;
			}
			if (vPath.Length == 0) {
				throw new ArgumentException("Cannot use empty path for vertex shader file", nameof(vPath));
			}
			if ((tcPath is null) != (tePath is null)) {
				throw new ArgumentException("Cannot specify only one tessellation stage shader file");
			}
			if (fPath.Length == 0) {
				throw new ArgumentException("Cannot use empty path for fragment shader file", nameof(fPath));
			}

			// Load bytecodes
			uint[] 
				vCode = LoadBytecodeFile(vPath),
				fCode = LoadBytecodeFile(fPath);
			uint[]? 
				tcCode = (tcPath is not null) ? LoadBytecodeFile(tcPath) : null,
				teCode = (tePath is not null) ? LoadBytecodeFile(tePath) : null,
				gCode = (gPath is not null) ? LoadBytecodeFile(gPath) : null;

			// Create modules
			VertexModule = CreateShaderModule(vCode);
			TessControlModule = (tcCode is not null) ? CreateShaderModule(tcCode) : null;
			TessEvalModule = (teCode is not null) ? CreateShaderModule(teCode) : null;
			GeometryModule = (gCode is not null) ? CreateShaderModule(gCode) : null;
			FragmentModule = CreateShaderModule(fCode);

			// Set values
			Stages = ShaderStages.Vertex | ShaderStages.Fragment |
				((tcCode is not null) ? ShaderStages.TessControl : ShaderStages.None) |
				((teCode is not null) ? ShaderStages.TessEval : ShaderStages.None) |
				((gCode is not null) ? ShaderStages.Geometry : ShaderStages.None);
		}
		#endregion // File Ctor

		#region Code Ctor
		/// <summary>
		/// Create a new shader program with the vertex and fragment stages.
		/// </summary>
		/// <param name="vCode">The bytecode for the vertex stage.</param>
		/// <param name="fCode">The bytecode for the fragment stage.</param>
		public Shader(ReadOnlySpan<uint> vCode, ReadOnlySpan<uint> fCode)
			: this(vCode, null, null, null, fCode)
		{ }

		/// <summary>
		/// Create a new shader program with the vertex, geometry, and fragment stages.
		/// </summary>
		/// <param name="vCode">The bytecode for the vertex stage.</param>
		/// <param name="gCode">The bytecode for the geometry stage.</param>
		/// <param name="fCode">The bytecode for the fragment stage.</param>
		public Shader(ReadOnlySpan<uint> vCode, ReadOnlySpan<uint> gCode, ReadOnlySpan<uint> fCode)
			: this(vCode, null, null, gCode, fCode)
		{ }

		/// <summary>
		/// Create a new shader program with the vertex, tessellation, and fragment stages.
		/// </summary>
		/// <param name="vCode">The bytecode for the vertex stage.</param>
		/// <param name="tcCode">The bytecode for the tessellation control stage.</param>
		/// <param name="teCode">The bytecode for the tessellation evaluation stage.</param>
		/// <param name="fCode">The bytecode for the fragment stage.</param>
		public Shader(ReadOnlySpan<uint> vCode, ReadOnlySpan<uint> tcCode, ReadOnlySpan<uint> teCode, 
			ReadOnlySpan<uint> fCode)
			: this(vCode, tcCode, teCode, null, fCode)
		{ }

		/// <summary>
		/// Create a new shader program with all stages explicitly specified from raw bytecodes. Passing an empty
		/// span will disable that stage.
		/// </summary>
		/// <param name="vCode">The SPIR-V bytecode for the vertex stage.</param>
		/// <param name="tcCode">The optional SPIR-V bytecode for the tessellation control stage.</param>
		/// <param name="teCode">The optional SPIR-V bytecode for the tessellation evaluation stage.</param>
		/// <param name="gCode">The optional SPIR-V bytecode for the geometry stage.</param>
		/// <param name="fCode">The SPIR-V bytecode for the fragment stage.</param>
		public Shader(ReadOnlySpan<uint> vCode, ReadOnlySpan<uint> tcCode, ReadOnlySpan<uint> teCode,
			ReadOnlySpan<uint> gCode, ReadOnlySpan<uint> fCode)
			: base(ResourceType.Shader)
		{
			// Validate
			if (vCode.Length == 0) {
				throw new ArgumentException("Cannot use empty bytecode for vertex shader", nameof(vCode));
			}
			if ((tcCode.Length != 0) != (teCode.Length != 0)) {
				throw new ArgumentException("Cannot specify only one tessellation stage bytecode");
			}
			if (fCode.Length == 0) {
				throw new ArgumentException("Cannot use empty bytecode for fragment shader", nameof(fCode));
			}

			// Create modules
			VertexModule = CreateShaderModule(vCode);
			TessControlModule = (tcCode.Length != 0) ? CreateShaderModule(tcCode) : null;
			TessEvalModule = (teCode.Length != 0) ? CreateShaderModule(teCode) : null;
			GeometryModule = (gCode.Length != 0) ? CreateShaderModule(gCode) : null;
			FragmentModule = CreateShaderModule(fCode);

			// Set values
			Stages = ShaderStages.Vertex | ShaderStages.Fragment |
				((tcCode.Length != 0) ? ShaderStages.TessControl : ShaderStages.None) |
				((teCode.Length != 0) ? ShaderStages.TessEval : ShaderStages.None) |
				((gCode.Length != 0) ? ShaderStages.Geometry : ShaderStages.None);
		}
		#endregion // Code Ctor

		#region IDisposable
		protected override void OnDispose(bool disposing)
		{
			if (Core.Instance is not null) {
				Core.Instance.Graphics.Resources.QueueDestroy(this);
			}
			else {
				Destroy();
			}
		}

		protected internal override void Destroy()
		{
			VertexModule.DestroyShaderModule(null);
			TessControlModule?.DestroyShaderModule(null);
			TessEvalModule?.DestroyShaderModule(null);
			GeometryModule?.DestroyShaderModule(null);
			FragmentModule.DestroyShaderModule(null);
		}
		#endregion // IDisposable

		// Loads and performs basic validation on bytecode files
		private static uint[] LoadBytecodeFile(string path)
		{
			// Load the bytes
			try {
				// Open/check
				using var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
				if ((file.Length == 0) || ((file.Length % 4) != 0)) {
					throw new ContentLoadException(path, $"Invalid bytecode file ({file.Length} % 4 fail)");
				}

				// Load/check
				uint[] bytecode = new uint[file.Length / 4];
				fixed (uint* codeptr = bytecode) {
					file.Read(new Span<byte>(codeptr, (int)file.Length));
				}
				if (bytecode[0] != SPIRV_MAGIC) {
					throw new ContentLoadException(path, "Invalid bytecode file (magic number fail)");
				}

				// Return
				return bytecode;
			}
			catch (ContentLoadException) {
				throw;
			}
			catch (Exception e) {
				throw new ContentLoadException(path, "Failed to load shader bytecode file", e);
			}
		}

		// Create a shader module from the bytecode
		private static VkShaderModule CreateShaderModule(ReadOnlySpan<uint> code)
		{
			// Check for magic number
			if (code[0] != SPIRV_MAGIC) {
				throw new ArgumentException("Invalid SPIR-V bytecode (magic number fail)");
			}

			// Create module
			fixed (uint* codeptr = code) {
				VkShaderModuleCreateInfo smci = new(
					flags: VkShaderModuleCreateFlags.NoFlags,
					codeSize: (ulong)code.Length * 4, // Length is in bytes
					code: codeptr
				);
				VulkanHandle<VkShaderModule> handle;
				Core.Instance!.Graphics.VkDevice.CreateShaderModule(&smci, null, &handle)
					.Throw("Failed to create shader module - invalid bytecode");
				return new(handle, Core.Instance!.Graphics.VkDevice);
			}
		}
	}
}
