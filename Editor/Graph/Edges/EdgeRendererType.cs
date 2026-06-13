namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Available edge rendering styles for the graph view.
	/// </summary>
	public enum EdgeRendererType
	{
		/// <summary>
		/// Unity's built-in bezier edge control (simple, performant).
		/// </summary>
		Default = 0,

		/// <summary>
		/// Custom wire-style bezier with draggable control points.
		/// </summary>
		Wire = 1,

		/// <summary>
		/// Straight lines between ports.
		/// </summary>
		Straight = 2,

		/// <summary>
		/// Right-angle (orthogonal) connections.
		/// </summary>
		Orthogonal = 3
	}
}
