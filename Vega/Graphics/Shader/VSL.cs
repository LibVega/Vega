/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Vulkan;

namespace Vega.Graphics
{
	// Implements loading and reflection for compiled VSL shaders
	internal unsafe static partial class VSL
	{
		// Parse a compiled VSL shader and return the info and modules
		public static void LoadFile(string path, out ShaderInfo info, 
			out VkShaderModule vertMod, out VkShaderModule fragMod)
		{
			// Check existence
			if (!File.Exists(path)) {
				throw new InvalidShaderException(path, $"Shader file '{path}' does not exist");
			}

			// Open the file and check the header
			using var file = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None));
			Span<byte> header = stackalloc byte[4];
			file.Read(header);
			if ((header[0] != 'V') || (header[1] != 'B') || (header[2] != 'C')) {
				throw new InvalidShaderException(path, "Invalid magic number");
			}
			if (header[3] != 1) {
				throw new InvalidShaderException(path, "Invalid shader file version");
			}

			// Read rest of header
			file.Read(header.Slice(0, 1));
			if (header[0] != 1) {
				throw new InvalidShaderException(path, "Invalid shader type, only graphics shaders supported");
			}
			Span<ushort> bcLengths = stackalloc ushort[5];
			file.Read(MemoryMarshal.AsBytes(bcLengths));
			Span<ushort> tableSizes = stackalloc ushort[5];
			file.Read(MemoryMarshal.AsBytes(tableSizes));

			// Validate header values
			if (bcLengths[0] == 0) {
				throw new InvalidShaderException(path, "Shader has no vertex stage");
			}
			if ((bcLengths[1] != 0) || (bcLengths[2] != 0) || (bcLengths[3] != 0)) {
				throw new InvalidShaderException(path, "Shader has unsupported stages");
			}
			if (bcLengths[4] == 0) {
				throw new InvalidShaderException(path, "Shader has no fragment stage");
			}
			if ((tableSizes[0] != BindingTable.DEFAULT_SIZE_SAMPLER) || (tableSizes[1] != BindingTable.DEFAULT_SIZE_IMAGE) ||
					(tableSizes[2] != BindingTable.DEFAULT_SIZE_BUFFER) || (tableSizes[3] != BindingTable.DEFAULT_SIZE_ROTEXELS) ||
					(tableSizes[4] != BindingTable.DEFAULT_SIZE_RWTEXELS)) {
				throw new InvalidShaderException(path, "Invalid binding table sizes");
			}

			// Read the vertex inputs
			var inputCount = file.ReadUInt32();
			Span<InterfaceVariable> inputs = stackalloc InterfaceVariable[(int)inputCount];
			file.Read(MemoryMarshal.AsBytes(inputs));

			// Read the fragment outputs
			var outputCount = file.ReadUInt32();
			Span<InterfaceVariable> outputs = stackalloc InterfaceVariable[(int)outputCount];
			file.Read(MemoryMarshal.AsBytes(outputs));

			// Read the bindings
			var bindingCount = file.ReadUInt32();
			Span<BindingVariable> bindings = stackalloc BindingVariable[(int)bindingCount];
			file.Read(MemoryMarshal.AsBytes(bindings));

			// Read the uniform info
			var uniformSize = file.ReadUInt32();
			var uniformStages = file.ReadUInt16();
			var uniformMemberCount = file.ReadUInt32();
			Span<UniformMember> members = stackalloc UniformMember[(int)uniformMemberCount];
			var memberNames = new string[uniformMemberCount];
			Span<byte> nameBytes = stackalloc byte[64];
			for (uint i = 0; i < uniformMemberCount; ++i) {
				file.Read(MemoryMarshal.AsBytes(members.Slice((int)i, 1)));
				var thisName = nameBytes.Slice(0, (int)members[(int)i].NameLength);
				file.Read(thisName);
				memberNames[i] = Encoding.ASCII.GetString(thisName);
			}

			// Read the subpass inputs
			var spiCount = file.ReadUInt32();
			Span<SubpassInput> spi = stackalloc SubpassInput[(int)spiCount];
			if (spiCount > 0) {
				file.Read(MemoryMarshal.AsBytes(spi));
			}

			// Read the bytecodes
			var vertBC = new uint[bcLengths[0]];
			var fragBC = new uint[bcLengths[4]];
			file.Read(MemoryMarshal.AsBytes(new Span<uint>(vertBC)));
			file.Read(MemoryMarshal.AsBytes(new Span<uint>(fragBC)));

			// Last validation
			if (file.BaseStream.Position != file.BaseStream.Length) {
				throw new InvalidShaderException(path, "File not fully consumed by parser");
			}

			// Create shader modules
			CreateShaderModules(path, Core.Instance!.Graphics.VkDevice,
				vertBC, fragBC,
				out vertMod, out fragMod
			);

			// Fill objects
			info = new();
		}

		// Performs the creation steps for the shader modules
		private static void CreateShaderModules(string? path, VkDevice device,
			Span<uint> vertBC, Span<uint> fragBC,
			out VkShaderModule vertMod, out VkShaderModule fragMod
		)
		{
			VkShaderModuleCreateInfo smci;
			VulkanHandle<VkShaderModule> modHandle;
			VkResult res;

			// Vertex
			fixed (uint* codeptr = vertBC) {
				smci = new(VkShaderModuleCreateFlags.NoFlags, (ulong)vertBC.Length * 4, codeptr);
				res = device.CreateShaderModule(&smci, null, &modHandle);
				if (res != VkResult.Success) {
					throw new InvalidShaderException(path, $"Failed to create vertex module: {res}");
				}
				vertMod = new(modHandle, device);
			}

			// Fragment
			fixed (uint* codeptr = fragBC) {
				smci = new(VkShaderModuleCreateFlags.NoFlags, (ulong)fragBC.Length * 4, codeptr);
				res = device.CreateShaderModule(&smci, null, &modHandle);
				if (res != VkResult.Success) {
					throw new InvalidShaderException(path, $"Failed to create fragment module: {res}");
				}
				fragMod = new(modHandle, device);
			}
		}
	}
}
