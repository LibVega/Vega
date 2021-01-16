/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;
using Vega.Graphics;
using Vega.Input;
using Vega.Render;

namespace Vega
{
	/// <summary>
	/// Provides a runtime management framework over <see cref="Core"/> and some top-level objects. This is designed
	/// to make development of standard Update/Draw loop applications easier.
	/// </summary>
	public abstract class ApplicationBase : IDisposable
	{
		/// <summary>
		/// Default size of windows created by ApplicationBase instances.
		/// </summary>
		public static readonly Extent2D DEFAULT_WINDOW_SIZE = new(1366, 768);

		/// <summary>
		/// The active instance of the application. Only one instance can be constructed at once.
		/// </summary>
		public static ApplicationBase? Instance { get; private set; } = null;

		#region Fields
		/// <summary>
		/// The name of the application.
		/// </summary>
		public readonly string AppName;
		/// <summary>
		/// The version of the application.
		/// </summary>
		public readonly Version AppVersion;

		#region Core Objects
		/// <summary>
		/// The core library object. This will not be constructed until <see cref="Run"/> has been called.
		/// <para>
		/// <em>Note:</em> this will be the same object instance as <see cref="Core.Instance"/>.
		/// </para>
		/// </summary>
		public Core Core => _core!;
		private Core? _core = null;
		/// <summary>
		/// The application graphics device interface. Equivalent to <see cref="Core.Graphics"/>.
		/// </summary>
		public GraphicsDevice Graphics => _core!.Graphics;

		/// <summary>
		/// The main window for the application.
		/// </summary>
		public Window MainWindow => _mainWindow!;
		private Window? _mainWindow = null;
		/// <summary>
		/// The keyboard handler for <see cref="MainWindow"/>.
		/// </summary>
		public Keyboard Keyboard => _mainWindow!.Keyboard;
		/// <summary>
		/// The mouse handler for <see cref="MainWindow"/>.
		/// </summary>
		public Mouse Mouse => _mainWindow!.Mouse;

		/// <summary>
		/// The main application renderer, attached to <see cref="MainWindow"/>. This must be set in 
		/// <see cref="Initialize"/> if a custom renderer is required.
		/// <para>
		/// The default renderer will be an MSAA-supported single-color-attachment renderer with a
		/// <see cref="TexelFormat.Depth24Stencil8"/> depth attachment.
		/// </para>
		/// </summary>
		public Renderer Renderer => _renderer!;
		//{
		//	get => _renderer!;
		//	protected set {
		//		if (_renderer is not null) {

		//		}
		//		if ((value.Window is null) || !ReferenceEquals(value.Window, _mainWindow)) {
		//			throw new InvalidOperationException(
		//				"Cannot set ApplicationBase.Renderer to a Renderer instance that does not use MainWindow");
		//		}
		//		_renderer = value;
		//	}
		//}
		private Renderer? _renderer = null;
		#endregion // Core Objects

		/// <summary>
		/// Gets a flag indicating if the application should exit. This can be triggered either through
		/// </summary>
		public bool ShouldExit => Core.ShouldExit;

		/// <summary>
		/// Flag telling if this application instance has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		/// <summary>
		/// Performs basic initialization and object construction. Note that the full library will not be initialized
		/// until <see cref="Run"/> is called.
		/// </summary>
		/// <param name="appName">The name of the application.</param>
		/// <param name="appVersion">The application version.</param>
		protected ApplicationBase(string appName, Version appVersion)
		{
			// Check instance
			if (Instance is not null) {
				throw new InvalidOperationException("Cannot construct more than one instance of ApplicationBase");
			}
			Instance = this;

			// Setup values
			AppName = appName;
			AppVersion = appVersion;

			// Register to core setup events
			Core.Events.Subscribe<DeviceDiscoveryEvent>((sender, time, evt) => OnSelectDevice(evt!));
			Core.Events.Subscribe<DeviceConfigureEvent>((sender, time, evt) => OnConfigureDevice(evt!));
		}
		~ApplicationBase()
		{
			dispose(false);
		}

		#region Execution Control
		/// <summary>
		/// Performs final initialization and resource loading, then launches the main application loop.
		/// <para>
		/// Note that this function blocks until the application is exited.
		/// </para>
		/// </summary>
		public void Run()
		{
			// Create the core object
			_core = new(AppName, AppVersion);

			// Open the window
			_mainWindow = _core!.CreateWindow(AppName, DEFAULT_WINDOW_SIZE.Width, DEFAULT_WINDOW_SIZE.Height);

			// Do initialization
			Initialize();

			// Create default renderer if custom is not selected
			if (_renderer is null) {
				// Default description - single-pass forward renderer with depth/stencil that supports MSAA
				RendererDescription desc = new(new(MainWindow.SurfaceFormat, true, AttachmentUse.Output));
				desc.AddAttachment(new(TexelFormat.Depth24Stencil8, false, AttachmentUse.Output));
				desc.SetResolveSubpass(0);
				_renderer = new(MainWindow, desc, MSAA.X1);
			}

			// One GC cleanup before main loop
			GC.Collect();

			BeforeStart();
			mainLoop();
		}

		/// <summary>
		/// Marks that the application should exit. This will happen automatically at the end of the current frame.
		/// <para>
		/// This is equivalent to calling <see cref="Core.Exit"/>.
		/// </para>
		/// </summary>
		public void Exit() => Core.Exit();
		#endregion // Execution Control

		#region Setup/Takedown
		/// <summary>
		/// Called to perform post-constructor initialization. When this is called, most of the core objects will have
		/// been created.
		/// <para>
		/// Do not load content in this function, instead use <see cref="LoadContent"/>.
		/// </para>
		/// </summary>
		protected virtual void Initialize() { }

		/// <summary>
		/// Called after initialization is complete to load content for use in the application.
		/// </summary>
		protected virtual void LoadContent() { }

		/// <summary>
		/// Called immediately before the main application loop is entered, to perform any final core application 
		/// setup.
		/// </summary>
		protected virtual void BeforeStart() { }

		/// <summary>
		/// Called after the main loop exits and the application starts shutdown. This is where content should be
		/// unloaded and custom shutdown logic implemented.
		/// </summary>
		/// <param name="disposing">
		/// <c>true</c> if <see cref="Dispose"/> was called, indicating a normal shutdown. <c>false</c> implies the
		/// garbage collector cleaned up the object which may indicate an anormal shutdown.
		/// </param>
		protected virtual void Terminate(bool disposing) { }
		#endregion // Setup/Takedown

		#region Core Loop
		// Implements the core main loop of the application, including calling frame functions and other frame logic
		// This function is blocking
		private void mainLoop()
		{
			while (!ShouldExit) {
				Core.NextFrame();

				PreUpdate();
				Update();
				PostUpdate();

				PreRender();
				_renderer!.Begin();
				Render();
				PostRender();
				_renderer.End();
			}
		}

		/// <summary>
		/// Called before <see cref="Update"/> to perform frame setup and update logic preparations.
		/// </summary>
		protected virtual void PreUpdate() { }

		/// <summary>
		/// Called once per frame to perform core application update logic.
		/// </summary>
		protected abstract void Update();

		/// <summary>
		/// Called after <see cref="Update"/> and before <see cref="PreRender"/> to perform final update logic and
		/// cleanup.
		/// </summary>
		protected virtual void PostUpdate() { }

		/// <summary>
		/// Called before <see cref="Render"/> to perform render logic setup.
		/// </summary>
		protected virtual void PreRender() { }

		/// <summary>
		/// Called once per frame to perform render command preparation, recording, and submission.
		/// </summary>
		protected abstract void Render();

		/// <summary>
		/// Called after <see cref="Render"/> to perform final render logic and cleanup.
		/// </summary>
		protected virtual void PostRender() { }
		#endregion // Core Loop

		#region Event Handlers
		/// <summary>
		/// Called when a GPU is found on the system, and is used to select which GPU to use for the 
		/// application. This is done by calling <paramref name="evt"/><c>.Use()</c>.
		/// <para>
		/// Implementing this function is optional. By default, the first available discrete GPU will be used.
		/// </para>
		/// </summary>
		/// <param name="evt">An event describing the GPU that was discovered.</param>
		protected virtual void OnSelectDevice(DeviceDiscoveryEvent evt) { }

		/// <summary>
		/// Called to configure the GPU device selected for use by the application.
		/// </summary>
		/// <param name="evt">The device configuration event through which to configure the device.</param>
		protected virtual void OnConfigureDevice(DeviceConfigureEvent evt) { }
		#endregion // Event Handlers

		#region IDisposable
		/// <summary>
		/// Performs cleanup of the application and associated managed objects.
		/// </summary>
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed) {
				Terminate(disposing);

				if (disposing) {
					_renderer?.Dispose();
					_mainWindow?.Dispose();
					_core?.Dispose();
				}
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
