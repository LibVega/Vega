/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Vega
{
	/// <summary>
	/// Provides general runtime information about the platform and environment for the current application.
	/// </summary>
	public static class Runtime
	{
		/// <summary>
		/// Informaton about the operating system.
		/// </summary>
		public static class OS
		{
			#region Fields
			/// <summary>
			/// The current operating system family.
			/// </summary>
			public static readonly OSFamily Family;

			/// <summary>
			/// Gets if the operating system is Microsoft Windows desktop.
			/// </summary>
			public static bool IsWindows => Family == OSFamily.Windows;
			/// <summary>
			/// Gets if the operating system is Apple MacOS desktop.
			/// </summary>
			public static bool IsMacOS => Family == OSFamily.MacOS;
			/// <summary>
			/// Gets if the operating system is a Linux desktop variant.
			/// </summary>
			public static bool IsLinuxDesktop => Family == OSFamily.LinuxDesktop;

			/// <summary>
			/// The version of the current operating system.
			/// </summary>
			public static readonly Version Version;
			#endregion // Fields

			static OS()
			{
				var osName = Environment.OSVersion.VersionString;
				Family = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OSFamily.Windows :
					RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OSFamily.MacOS :
					RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OSFamily.LinuxDesktop :
					throw new PlatformNotSupportedException($"Cannot load Vega on platform '{osName}'");
				Version = Environment.OSVersion.Version;
			}
		}

		/// <summary>
		/// Information about the CPU.
		/// </summary>
		public static class CPU
		{
			#region Fields
			/// <summary>
			/// The number of distinct processors available to the process (either physical or hyperthreaded).
			/// </summary>
			public static readonly uint ProcCount;
			/// <summary>
			/// The processor architecture.
			/// </summary>
			public static CPUArch Arch;
			#endregion // Fields

			static CPU()
			{
				ProcCount = (uint)Environment.ProcessorCount;
				var arch = Assembly.GetExecutingAssembly().GetName().ProcessorArchitecture;
				Arch = arch switch { 
					ProcessorArchitecture.Amd64 => CPUArch.X64,
					ProcessorArchitecture.Arm => CPUArch.Arm64,
					_ => throw new PlatformNotSupportedException($"Unknown processor architecture ({arch})")
				};
			}
		}

		/// <summary>
		/// Information about the physical memory.
		/// </summary>
		public static class Memory
		{
			#region Fields
			/// <summary>
			/// Gets the approximate amount of memory used by the process.
			/// </summary>
			public static DataSize Used => new DataSize(GC.GetTotalMemory(false));
			/// <summary>
			/// Gets a more accurate reading of total used memory, but may cause a blocking garbage collection.
			/// </summary>
			public static DataSize UsedAccurate => new DataSize(GC.GetTotalMemory(true));
			#endregion // Fields

			static Memory()
			{

			}
		}
	}

	/// <summary>
	/// Supported operating system families.
	/// </summary>
	public enum OSFamily
	{
		/// <summary>
		/// Microsoft Windows desktop.
		/// </summary>
		Windows,
		/// <summary>
		/// Apple desktop macOS.
		/// </summary>
		MacOS,
		/// <summary>
		/// Linux desktop variant.
		/// </summary>
		LinuxDesktop
	}

	/// <summary>
	/// Supported CPU architectures.
	/// </summary>
	public enum CPUArch
	{
		/// <summary>
		/// The AMD64 (x86_64) architecture.
		/// </summary>
		X64,
		/// <summary>
		/// The 64-bit ARM architecture.
		/// </summary>
		Arm64
	}
}
