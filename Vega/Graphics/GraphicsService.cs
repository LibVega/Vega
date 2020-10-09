/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using System.Linq;
using VVK;

namespace Vega.Graphics
{
	public sealed class GraphicsService
	{
		#region Fields
		/// <summary>
		/// The core instance controlling this graphics service.
		/// </summary>
		public readonly Core Core;

		// Vulkan objects
		internal readonly VulkanInstance Instance;
		internal Vk.Version ApiVersion => Instance.ApiVersion;

		internal bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		internal GraphicsService(Core core)
		{
			if (!Glfw.VulkanSupported()) {
				throw new PlatformNotSupportedException("The Vulkan runtime is not available on this platform");
			}
			Core = core;

			// Create Vulkan Instance
			var required = Glfw.GetRequiredInstanceExtensions().ToList();
			Instance = VulkanInstance.Create(
				core.AppName, core.AppVersion,
				"Vega", GetType().Assembly.GetName().Version!,
				Vk.Version.VK_VERSION_1_0, required);
		}
		~GraphicsService()
		{
			dispose(false);
		}

		#region Disposable
		internal void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed) {
				if (disposing) {
					Instance.Dispose();
				}
			}
			IsDisposed = true;
		}
		#endregion // Disposable
	}
}
