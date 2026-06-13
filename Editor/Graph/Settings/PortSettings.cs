using System;
using UnityEditor;

namespace Nonatomic.VSM.Editor.Graph.Settings
{
	/// <summary>
	/// Port coloring mode for data ports.
	/// </summary>
	public enum PortColorMode
	{
		/// <summary>
		/// Ports are colored based on their data type.
		/// </summary>
		TypeBased,

		/// <summary>
		/// Ports inherit their color from the node's title/state color.
		/// </summary>
		StateBased
	}

	/// <summary>
	/// Global port display settings.
	/// </summary>
	public class PortSettings
	{
		private static PortSettings _instance;

		/// <summary>
		/// Singleton instance of port settings.
		/// </summary>
		public static PortSettings Instance => _instance ??= new PortSettings();

		private const string ColorModeKey = "VSM_Port_ColorMode";
		private const string ShowTypeIconsKey = "VSM_Port_ShowTypeIcons";

		private PortColorMode _colorMode = PortColorMode.TypeBased;
		private bool _showTypeIcons = true;

		/// <summary>
		/// The current port coloring mode.
		/// </summary>
		public PortColorMode ColorMode
		{
			get => _colorMode;
			set
			{
				if (_colorMode != value)
				{
					_colorMode = value;
					Save();
					OnSettingsChanged?.Invoke();
				}
			}
		}

		/// <summary>
		/// Whether to show type icons next to port labels.
		/// </summary>
		public bool ShowTypeIcons
		{
			get => _showTypeIcons;
			set
			{
				if (_showTypeIcons != value)
				{
					_showTypeIcons = value;
					Save();
					OnSettingsChanged?.Invoke();
				}
			}
		}

		/// <summary>
		/// Event raised when any port setting changes.
		/// </summary>
		public event Action OnSettingsChanged;

		private PortSettings()
		{
			Load();
		}

		/// <summary>
		/// Saves settings to EditorPrefs.
		/// </summary>
		public void Save()
		{
			EditorPrefs.SetInt(ColorModeKey, (int)_colorMode);
			EditorPrefs.SetBool(ShowTypeIconsKey, _showTypeIcons);
		}

		/// <summary>
		/// Loads settings from EditorPrefs.
		/// </summary>
		public void Load()
		{
			_colorMode = (PortColorMode)EditorPrefs.GetInt(ColorModeKey, (int)PortColorMode.TypeBased);
			_showTypeIcons = EditorPrefs.GetBool(ShowTypeIconsKey, true);
		}

		/// <summary>
		/// Resets to default values.
		/// </summary>
		public void ResetToDefaults()
		{
			_colorMode = PortColorMode.TypeBased;
			_showTypeIcons = true;
			Save();
			OnSettingsChanged?.Invoke();
		}
	}
}
