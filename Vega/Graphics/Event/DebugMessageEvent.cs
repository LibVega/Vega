/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// <see cref="EventHub"/> event that is raised when the graphics validation layers (if enabled) raise a message.
	/// </summary>
	public sealed class DebugMessageEvent
	{
		#region Fields
		/// <summary>
		/// The severity level of the message.
		/// </summary>
		public DebugMessageSeverity Severity { get; internal set; } = DebugMessageSeverity.Verbose;
		/// <summary>
		/// The type of the message.
		/// </summary>
		public DebugMessageType Type { get; internal set; } = DebugMessageType.General;
		/// <summary>
		/// The message text.
		/// </summary>
		public string Message { get; internal set; } = String.Empty;
		/// <summary>
		/// A unique ID for the specific message type.
		/// </summary>
		public int MessageId { get; internal set; } = 0;
		/// <summary>
		/// The objects that were associated with the message, if applicable.
		/// </summary>
		public IReadOnlyList<string> Objects => ObjectNames;
		internal readonly List<string> ObjectNames = new();
		#endregion // Fields
	}

	/// <summary>
	/// Severity levels for graphics debug messages.
	/// </summary>
	[Flags]
	public enum DebugMessageSeverity : uint
	{
		/// <summary>
		/// The message is a low importance debug message.
		/// </summary>
		Verbose = VkDebugUtilsMessageSeverityFlagsEXT.Verbose,
		/// <summary>
		/// The message is a standard importance informative message.
		/// </summary>
		Info = VkDebugUtilsMessageSeverityFlagsEXT.Info,
		/// <summary>
		/// The message is reporting an off-nominal condition that does not represent an error.
		/// </summary>
		Warning = VkDebugUtilsMessageSeverityFlagsEXT.Warning,
		/// <summary>
		/// The message is the highest importance message representing an internal error.
		/// </summary>
		Error = VkDebugUtilsMessageSeverityFlagsEXT.Error
	}

	/// <summary>
	/// Type flags for graphics debug messages.
	/// </summary>
	[Flags]
	public enum DebugMessageType : uint
	{
		/// <summary>
		/// The message is a general purpose information message.
		/// </summary>
		General = VkDebugUtilsMessageTypeFlagsEXT.General,
		/// <summary>
		/// The message is reporting performance-related information.
		/// </summary>
		Performance = VkDebugUtilsMessageTypeFlagsEXT.Performance,
		/// <summary>
		/// The message is reporting validation failure information.
		/// </summary>
		Validation = VkDebugUtilsMessageTypeFlagsEXT.Validation
	}
}
