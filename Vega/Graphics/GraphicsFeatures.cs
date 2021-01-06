/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Contains the set of known optional features that may be enabled on a graphics device.
	/// </summary>
	public unsafe struct GraphicsFeatures
	{
		private const int FEATURE_COUNT = 5;
		private static readonly GraphicsFeature[] FEATURES = new GraphicsFeature[FEATURE_COUNT] { 
			new("FillModeNonSolid", "FillModeNonSolid", null), // Line and Point fill modes
			new("WideLines", "WideLines", null), // Raster line widths other than one
			new("DepthClamp", "DepthClamp", null), // Depth clamp operations
			new("TessellationShader", "TessellationShader", null), // Tessellation shaders
			new("GeometryShader", "GeometryShader", null), // Geometry shaders
		};

		#region Fields
		// The array of feature flags
		private fixed bool _features[FEATURE_COUNT];

		/// <summary>
		/// Non-solid fill modes (enables use of <see cref="FillMode.Line"/> and <see cref="FillMode.Point"/>).
		/// </summary>
		public bool FillModeNonSolid
		{
			readonly get => _features[0];
			set => _features[0] = value;
		}
		/// <summary>
		/// If <see cref="RasterizerState.LineWidth"/> can have a value other than 1.0.
		/// </summary>
		public bool WideLines
		{
			readonly get => _features[1];
			set => _features[1] = value;
		}
		/// <summary>
		/// If <see cref="RasterizerState.DepthClamp"/> can be <c>true</c>.
		/// </summary>
		public bool DepthClamp
		{
			readonly get => _features[2];
			set => _features[2] = value;
		}
		/// <summary>
		/// If tessellation shader stages can be used in pipelines.
		/// </summary>
		public bool TessellationShaders
		{
			readonly get => _features[3];
			set => _features[3] = value;
		}
		/// <summary>
		/// If geometry shaders can be used in pipelines.
		/// </summary>
		public bool GeometryShaders
		{
			readonly get => _features[4];
			set => _features[4] = value;
		}
		#endregion // Fields

		// Populates the features with support flags
		internal GraphicsFeatures(in VkPhysicalDeviceFeatures afeats, IReadOnlyList<string> aexts)
		{
			for (int i = 0; i < FEATURE_COUNT; ++i) {
				_features[i] = FEATURES[i].Check(afeats, aexts);
			}
		}

		// Checks the features, populates the objects to create a device
		internal bool TryBuild(
			in VkPhysicalDeviceFeatures afeats, IReadOnlyList<string> aexts,
			out VkPhysicalDeviceFeatures efeats, List<string> eexts,
			out string? missing)
		{
			missing = null;
			efeats = new();

			// Check features and populate objects
			for (int i = 0; i < FEATURE_COUNT; ++i) {
				if (!_features[i]) {
					continue;
				}

				var feat = FEATURES[i];
				var check = feat.Check(afeats, aexts);
				if (!check) {
					missing = feat.Name;
					return false;
				}
				feat.Enable(ref efeats, eexts);
			}

			return true;
		}
	}
}
