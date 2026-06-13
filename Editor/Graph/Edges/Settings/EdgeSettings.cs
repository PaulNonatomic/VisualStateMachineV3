using System;
using Nonatomic.VSM.Editor.Graph;
using UnityEditor;

namespace Nonatomic.VSM.Editor.Graph.Settings
{
	/// <summary>
	/// Global edge settings including renderer type selection.
	/// </summary>
	public class EdgeSettings
	{
		private static EdgeSettings _instance;

		/// <summary>
		/// Singleton instance of edge settings.
		/// </summary>
		public static EdgeSettings Instance => _instance ??= new EdgeSettings();

		private const string RendererTypeKey = "VSM_Edge_RendererType";
		private const string EdgeWidthKey = "VSM_Edge_Width";

		private EdgeRendererType _rendererType = EdgeRendererType.Wire;

		/// <summary>
		/// Currently selected edge renderer type.
		/// </summary>
		public EdgeRendererType RendererType
		{
			get => _rendererType;
			set
			{
				if (_rendererType != value)
				{
					_rendererType = value;
					Save();
					OnRendererTypeChanged?.Invoke();
				}
			}
		}

		/// <summary>
		/// Edge rendering width (shared across renderers).
		/// </summary>
		public float EdgeWidth { get; set; } = 2f;

		/// <summary>
		/// Event raised when renderer type changes.
		/// </summary>
		public event Action OnRendererTypeChanged;

		private EdgeSettings()
		{
			Load();
		}

		/// <summary>
		/// Saves settings to EditorPrefs.
		/// </summary>
		public void Save()
		{
			EditorPrefs.SetInt(RendererTypeKey, (int)_rendererType);
			EditorPrefs.SetFloat(EdgeWidthKey, EdgeWidth);
		}

		/// <summary>
		/// Loads settings from EditorPrefs.
		/// </summary>
		public void Load()
		{
			_rendererType = (EdgeRendererType)EditorPrefs.GetInt(RendererTypeKey, (int)EdgeRendererType.Wire);
			EdgeWidth = EditorPrefs.GetFloat(EdgeWidthKey, 2f);
		}

		/// <summary>
		/// Resets to default values.
		/// </summary>
		public void ResetToDefaults()
		{
			_rendererType = EdgeRendererType.Wire;
			EdgeWidth = 2f;
			Save();
			OnRendererTypeChanged?.Invoke();
		}
	}
}
