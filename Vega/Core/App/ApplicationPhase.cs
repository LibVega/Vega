/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020-2021 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

using System;

namespace Vega
{
	/// <summary>
	/// Describes the phases that <see cref="ApplicationBase"/> types go through in execution.
	/// </summary>
	public enum ApplicationPhase
	{
		/// <summary>
		/// In the constructors and before <see cref="ApplicationBase.Run"/> is called.
		/// </summary>
		BeforeRun,
		/// <summary>
		/// Between when <see cref="ApplicationBase.Run"/> is called and <see cref="Initialize"/> ends.
		/// </summary>
		Initialize,
		/// <summary>
		/// Between when <see cref="Renderer"/> is setup and the end of <see cref="LoadContent"/>.
		/// </summary>
		LoadContent,
		/// <summary>
		/// After the content is loaded, and before the main loop starts.
		/// </summary>
		BeforeStart,
		/// <summary>
		/// During the calls to <see cref="ApplicationBase.PreUpdate"/>, <see cref="ApplicationBase.Update"/>, and
		/// <see cref="ApplicationBase.PostUpdate"/>.
		/// </summary>
		Update,
		/// <summary>
		/// During the calls to <see cref="ApplicationBase.PreRender"/>, <see cref="ApplicationBase.Render"/>, and
		/// <see cref="ApplicationBase.PostRender"/>.
		/// </summary>
		Render,
		/// <summary>
		/// The time in the main application loop between the last call to <see cref="ApplicationBase.PostRender"/> and
		/// <see cref="ApplicationBase.PreUpdate"/>.
		/// </summary>
		InterFrame,
		/// <summary>
		/// After the main loop ends, and the application is performing cleanup.
		/// </summary>
		Terminate
	}
}
