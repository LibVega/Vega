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
		// Known constant values
		public const uint MAX_BINDING_COUNT = 32;
		public const uint MAX_INPUT_ATTACHMENTS = 4;

		// Parse a compiled VSL shader and return the info and modules
		public static void LoadStream(string path, Stream stream,
			out ShaderLayout info, 
			out VkShaderModule vertMod, out VkShaderModule fragMod)
		{
			// Open the file and check the header
			using var file = new BinaryReader(stream);
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
			ProcessVertexInputs(path, inputs, out var reflInputs);

			// Read the fragment outputs
			var outputCount = file.ReadUInt32();
			Span<InterfaceVariable> outputs = stackalloc InterfaceVariable[(int)outputCount];
			file.Read(MemoryMarshal.AsBytes(outputs));
			ProcessFragmentOutputs(path, outputs, out var reflOutputs);

			// Read the bindings
			var bindingCount = file.ReadUInt32();
			Span<BindingVariable> bindings = stackalloc BindingVariable[(int)bindingCount];
			file.Read(MemoryMarshal.AsBytes(bindings));
			ProcessBindings(path, bindings, out var reflBindings);

			// Read the uniform info
			var uniformSize = file.ReadUInt32();
			ShaderLayout.UniformMember[]? reflUniformMembers = null;
			ShaderStages uniformStages = ShaderStages.None;
			if (uniformSize > 0) {
				uniformStages = (ShaderStages)file.ReadUInt16();
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
				ProcessUniformMembers(path, members, memberNames, out reflUniformMembers);
			}

			// Read the subpass inputs
			var spiCount = file.ReadUInt32();
			Span<SubpassInput> spi = stackalloc SubpassInput[(int)spiCount];
			if (spiCount > 0) {
				file.Read(MemoryMarshal.AsBytes(spi));
			}
			ProcessSubpassInputs(path, spi, out var reflSpi);

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
			info = new(
				ShaderStages.Vertex | ShaderStages.Fragment,
				reflInputs,
				reflOutputs,
				reflBindings,
				uniformSize, uniformStages, reflUniformMembers ?? Array.Empty<ShaderLayout.UniformMember>(),
				reflSpi
			);
			CreateShaderModules(path, Core.Instance!.Graphics.VkDevice,
				vertBC, fragBC,
				out vertMod, out fragMod
			);
		}

		// Perform processing of vertex inputs
		private static void ProcessVertexInputs(string? path,
			Span<InterfaceVariable> rawInputs, out ShaderLayout.VertexInput[] inputs)
		{
			inputs = new ShaderLayout.VertexInput[rawInputs.Length];
			for (int i = 0; i < rawInputs.Length; ++i) {
				ref readonly var raw = ref rawInputs[i];
				var inputType = ParseVertexFormat(raw.BaseType, raw.Dims[0], raw.Dims[1]);
				if (!inputType.HasValue) {
					throw new InvalidShaderException(path, "Invalid vertex input type");
				}
				inputs[i] = new(raw.Location, inputType.Value, raw.ArraySize);
			}
		}

		// Perform processing of fragment outputs
		private static void ProcessFragmentOutputs(string? path,
			Span<InterfaceVariable> rawOutputs, out ShaderLayout.FragmentOutput[] outputs)
		{
			outputs = new ShaderLayout.FragmentOutput[rawOutputs.Length];
			for (int i = 0; i < rawOutputs.Length; ++i) {
				ref readonly var raw = ref rawOutputs[i];
				var outputType = ParseTexelFormat(raw.BaseType, 4, raw.Dims[0]);
				if (!outputType.HasValue) {
					throw new InvalidShaderException(path, "Invalid fragment output type");
				}
				outputs[i] = new(raw.Location, outputType.Value);
			}
		}

		// Perform processing of binding variables
		private static void ProcessBindings(string? path,
			Span<BindingVariable> rawBindings, out ShaderLayout.Binding[] bindings)
		{
			bindings = new ShaderLayout.Binding[rawBindings.Length];
			for (int i = 0; i < rawBindings.Length; ++i) {
				ref readonly var raw = ref rawBindings[i];
				var btype = ParseBindingType(raw);
				if (!btype.HasValue) {
					throw new InvalidShaderException(path, $"Invalid binding type at slot {raw.Slot}");
				}
				if ((raw.BaseType == ShaderBaseType.ROBuffer) || (raw.BaseType == ShaderBaseType.RWBuffer)) {
					bindings[i] = new(raw.Slot, (ShaderStages)raw.StageMask, btype.Value, raw.BufferSize);
				}
				else {
					var ttype = ParseBindingTexelFormat(raw);
					if (!ttype.HasValue) {
						throw new InvalidShaderException(path, $"Invalid texel format at slot {raw.Slot}");
					}
					bindings[i] = new(raw.Slot, (ShaderStages)raw.StageMask, btype.Value, ttype.Value);
				}
			}
		}

		// Perform processing of the uniform members into the final reflection types
		private static void ProcessUniformMembers(string? path,
			Span<UniformMember> rawMembers, string[] names, out ShaderLayout.UniformMember[] members)
		{
			members = new ShaderLayout.UniformMember[rawMembers.Length];
			for (int i = 0; i < rawMembers.Length; ++i) {
				ref readonly var raw = ref rawMembers[i];
				var memType = ParseVertexFormat(raw.BaseType, raw.Dims[0], raw.Dims[1]);
				if (!memType.HasValue) {
					throw new InvalidShaderException(path, $"Invalid shader type for uniform member '{names[i]}'");
				}
				members[i] = new(names[i], raw.Offset, memType.Value, raw.ArraySize);
			}
		}

		// Perform processing of subpass inputs
		private static void ProcessSubpassInputs(string? path,
			Span<SubpassInput> rawInputs, out ShaderLayout.SubpassInput[] inputs)
		{
			inputs = new ShaderLayout.SubpassInput[rawInputs.Length];
			for (int i = 0; i < rawInputs.Length; ++i) {
				ref readonly var raw = ref rawInputs[i];
				var tfmt = raw.ComponentType switch {
					ShaderBaseType.Float => TexelFormat.Float4,
					ShaderBaseType.Signed => TexelFormat.Int4,
					ShaderBaseType.Unsigned => TexelFormat.UInt4,
					_ => (TexelFormat?)null
				};
				if (!tfmt.HasValue) {
					throw new InvalidShaderException(path, $"Invalid texel format for subpass input {i}");
				}
				inputs[i] = new((uint)i, tfmt.Value);
			}
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
