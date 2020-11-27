/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Vulkan;

namespace Vega.Graphics
{
	/// <summary>
	/// Contains the set of specific graphics features that can be enabled for a graphics device, if 
	/// supported.
	/// </summary>
	public sealed class GraphicsFeatures
	{
		#region Fields
		// The array of features
		private readonly GraphicsFeature[] _features;
		// Lock status
		private bool _locked = false;
		#endregion // Fields

		/// <summary>
		/// Creates a new feature set with all features disabled to start.
		/// </summary>
		public GraphicsFeatures()
		{
			_features = new GraphicsFeature[0];
		}

		// Sets the feature value, if not locked
		private void setFeature(int index, FeatureLevel level)
		{
			if (_locked) {
				throw new InvalidOperationException("Cannot change a feature set once a device is created");
			}
			_features[index].Level = level;
		}

		// Checks the features, populates the objects to create a device, and locks the features from changes
		internal bool TryBuild(
			in VkPhysicalDeviceFeatures afeats, IReadOnlyList<string> aexts,
			out VkPhysicalDeviceFeatures efeats, List<string> eexts,
			out string? missing)
		{
			missing = null;
			efeats = new();

			// Check features and populate objects
			foreach (var feat in _features.Where(feat => feat.Level != FeatureLevel.Disabled)) {
				var check = feat.Check(afeats, aexts);
				if (!check && (feat.Level == FeatureLevel.Enabled)) {
					missing = feat.Name;
					return false;
				}
				feat.Level = check ? FeatureLevel.Enabled : FeatureLevel.Disabled;
				if (check) {
					feat.Enable(ref efeats, eexts);
				}
			}

			_locked = true;
			return true;
		}
	}
}
