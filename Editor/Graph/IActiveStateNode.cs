namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Interface for node views that can display active state highlighting during runtime.
	/// </summary>
	public interface IActiveStateNode
	{
		/// <summary>
		/// Sets whether this node is currently the active state in runtime.
		/// </summary>
		/// <param name="isActive">True if this node is the current active state.</param>
		void SetActiveState(bool isActive);

		/// <summary>
		/// Briefly flashes the active indicator for transient highlighting.
		/// Used for nodes that are visited briefly during transitions (e.g., jump nodes).
		/// </summary>
		void FlashHighlight();
	}
}
