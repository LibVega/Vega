﻿/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vulkan;

namespace Vega.Graphics
{
	// Represents an optional feature that can be enabled on a graphics device.
	internal sealed class GraphicsFeature
	{
		// Reflection types for device features
		private static readonly Type VK_BOOL_TYPE = typeof(VkBool32);
		private static readonly IReadOnlyDictionary<string, FieldInfo> FEATURE_FIELDS =
			typeof(VkPhysicalDeviceFeatures).GetFields(BindingFlags.Public | BindingFlags.Instance)
				.Where(fld => fld.FieldType == VK_BOOL_TYPE)
				.ToDictionary(fld => fld.Name);

		#region Fields
		// The feature name
		public readonly string Name;
		// The request level
		public FeatureLevel Level = FeatureLevel.Disabled;

		// The field reference for device features
		private readonly FieldInfo? _field;
		// The extension name for device extensions
		private readonly string? _extName;
		#endregion // Fields

		public GraphicsFeature(string name, string? fieldName, string? extName)
		{
			Name = name;
			if (fieldName is not null) {
				if (!FEATURE_FIELDS.TryGetValue(fieldName, out var field)) {
					throw new Exception($"LIBRARY BUG - No such feature '{fieldName}'");
				}
				_field = field;
			}
			else {
				_extName = extName!;
			}
		}

		// Checks if the feature is available
		public bool Check(in VkPhysicalDeviceFeatures feats, IReadOnlyList<string> extensions)
		{
			if (_field is not null) {
				var res = _field.GetValue(feats);
				return ((res as VkBool32?)! == VkBool32.True);
			}
			else {
				return extensions.Contains(_extName!);
			}
		}

		// Mutates the correct object to enable the feature
		public void Enable(ref VkPhysicalDeviceFeatures feats, List<string> extensions)
		{
			if (_field is not null) {
				_field.SetValue(feats, VkBool32.True);
			}
			else {
				extensions.Add(_extName!);
			}
		}
	}

	/// <summary>
	/// Represents the level to which a graphics feature can be requested
	/// </summary>
	public enum FeatureLevel : byte
	{
		/// <summary>
		/// The feature is disabled and will not be requested.
		/// </summary>
		Disabled = 0,
		/// <summary>
		/// The feature is optional, it will be silently ignored if it is not available.
		/// </summary>
		Optional = 1,
		/// <summary>
		/// The feature is required, and an error will be generated if it is not available.
		/// </summary>
		Enabled = 2
	}
}