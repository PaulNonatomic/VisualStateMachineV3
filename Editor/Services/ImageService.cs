using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nonatomic.VSM.Editor.Services
{
	/// <summary>
	/// Service for loading and caching images from the Editor Resources folder.
	/// </summary>
	public static class ImageService
	{
		private static readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();

		/// <summary>
		/// Base path for node icons within the Resources folder.
		/// </summary>
		public const string IconBasePath = "Icons/Nodes/";

		/// <summary>
		/// Loads a texture from the Editor Resources folder.
		/// Results are cached for performance.
		/// </summary>
		/// <param name="path">Path relative to Editor/Resources (without extension)</param>
		/// <returns>The loaded texture, or null if not found</returns>
		public static Texture2D LoadTexture(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return null;
			}

			// Check cache first
			if (_cache.TryGetValue(path, out var cached))
			{
				return cached;
			}

			// Try loading from Resources
			var texture = Resources.Load<Texture2D>(path);

			if (texture == null)
			{
				// Try with full asset path as fallback
				texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
					$"Packages/com.nonatomic.visualstatemachinev3/Editor/Resources/{path}.png");
			}

			if (texture == null)
			{
				// Try alternative package path
				texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
					$"Assets/VisualStateMachineV3/Editor/Resources/{path}.png");
			}

			// Cache result (even if null to avoid repeated lookups)
			_cache[path] = texture;

			return texture;
		}

		/// <summary>
		/// Loads a node icon by name.
		/// </summary>
		/// <param name="iconName">Icon name (without path or extension)</param>
		/// <returns>The loaded texture, or null if not found</returns>
		public static Texture2D LoadNodeIcon(string iconName)
		{
			if (string.IsNullOrEmpty(iconName))
			{
				return null;
			}

			// If the icon name already includes the path, use it directly
			if (iconName.Contains("/"))
			{
				return LoadTexture(iconName);
			}

			// Otherwise, prepend the icon base path
			return LoadTexture(IconBasePath + iconName);
		}

		/// <summary>
		/// Clears the texture cache.
		/// Call this if icons are updated at runtime.
		/// </summary>
		public static void ClearCache()
		{
			_cache.Clear();
		}
	}
}
