namespace Nonatomic.VSM.Visualization
{
	/// <summary>
	/// Available edge-rendering styles for the runtime graph view. Mirrors the
	/// editor's <c>EdgeRendererType</c> but does not depend on editor code.
	/// </summary>
	public enum RuntimeEdgeRendererType
	{
		/// <summary>
		/// Simple cubic bezier with fixed horizontal tangents. Fastest, least visually rich.
		/// </summary>
		Default = 0,

		/// <summary>
		/// Wire-style bezier with adaptive tangent length and backward-loop handling.
		/// Matches the editor's wire renderer for visual consistency.
		/// </summary>
		Wire = 1
	}
}
