using System;
using System.Collections.Generic;
using Nonatomic.VSM.Editor.Graph.Renderers;
using Nonatomic.VSM.Editor.Graph.Settings;
using UnityEngine;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Factory for creating edge renderers.
	/// Uses a registry pattern for extensibility.
	/// </summary>
	public static class EdgeRendererFactory
	{
		private static readonly Dictionary<EdgeRendererType, Func<IEdgeRenderer>> Renderers
			= new Dictionary<EdgeRendererType, Func<IEdgeRenderer>>();

		private static bool _initialized;

		/// <summary>
		/// Ensures the factory is initialized with built-in renderers.
		/// </summary>
		private static void EnsureInitialized()
		{
			if (_initialized) return;

			// Register built-in renderers
			Register(EdgeRendererType.Default, () => new DefaultEdgeRenderer());
			Register(EdgeRendererType.Wire, () => new WireEdgeRenderer());
			// Future renderers can be registered here or via external initialization
			// Register(EdgeRendererType.Straight, () => new StraightEdgeRenderer());
			// Register(EdgeRendererType.Orthogonal, () => new OrthogonalEdgeRenderer());

			_initialized = true;
		}

		/// <summary>
		/// Registers a renderer factory for a given type.
		/// </summary>
		/// <param name="type">The renderer type to register.</param>
		/// <param name="factory">Factory function that creates the renderer.</param>
		public static void Register(EdgeRendererType type, Func<IEdgeRenderer> factory)
		{
			Renderers[type] = factory;
		}

		/// <summary>
		/// Creates a renderer for the specified type.
		/// </summary>
		/// <param name="type">The type of renderer to create.</param>
		/// <returns>A new renderer instance.</returns>
		public static IEdgeRenderer Create(EdgeRendererType type)
		{
			EnsureInitialized();

			if (Renderers.TryGetValue(type, out var factory))
			{
				return factory();
			}

			Debug.LogWarning($"Unknown edge renderer type: {type}. Falling back to Wire.");
			return new WireEdgeRenderer();
		}

		/// <summary>
		/// Creates a renderer using the current global settings.
		/// </summary>
		/// <returns>A new renderer instance based on current settings.</returns>
		public static IEdgeRenderer CreateFromSettings()
		{
			return Create(EdgeSettings.Instance.RendererType);
		}

		/// <summary>
		/// Gets all available renderer types.
		/// </summary>
		/// <returns>Collection of available renderer types.</returns>
		public static IEnumerable<EdgeRendererType> GetAvailableTypes()
		{
			EnsureInitialized();
			return Renderers.Keys;
		}

		/// <summary>
		/// Checks if a renderer type is registered.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns>True if the type is registered.</returns>
		public static bool IsRegistered(EdgeRendererType type)
		{
			EnsureInitialized();
			return Renderers.ContainsKey(type);
		}
	}
}
