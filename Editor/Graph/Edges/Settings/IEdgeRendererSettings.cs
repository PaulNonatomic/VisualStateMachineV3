namespace Nonatomic.VSM.Editor.Graph.Settings
{
	/// <summary>
	/// Base interface for renderer-specific settings.
	/// </summary>
	public interface IEdgeRendererSettings
	{
		/// <summary>
		/// Saves settings to EditorPrefs.
		/// </summary>
		void Save();

		/// <summary>
		/// Loads settings from EditorPrefs.
		/// </summary>
		void Load();

		/// <summary>
		/// Resets to default values.
		/// </summary>
		void ResetToDefaults();
	}
}
