using Nonatomic.VSM.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Visualization
{
	/// <summary>
	/// Strategy interface for drawing a single edge between two ports. Implementations
	/// own the curve algorithm, stroke, and arrowhead. Consuming projects can implement
	/// custom edge styles (animated, dashed, glowing, etc.) by writing a new renderer
	/// and assigning it via <see cref="RuntimeGraphView.SetEdgeRenderer"/>.
	/// </summary>
	public interface IRuntimeEdgeRenderer
	{
		/// <summary>
		/// Identifier for this renderer. Used for theme selection.
		/// </summary>
		RuntimeEdgeRendererType Type { get; }

		/// <summary>
		/// Draw a single edge from <paramref name="source"/> to <paramref name="target"/>.
		/// Both points are in the edge layer's local coordinate space.
		/// </summary>
		/// <param name="painter">UI Toolkit painter to draw into.</param>
		/// <param name="source">Source port position (typically the right edge of the source node).</param>
		/// <param name="target">Target port position (typically the left edge of the target node).</param>
		/// <param name="connection">Underlying connection data (may contain user-edited curve control points).</param>
		/// <param name="style">Stroke colour, width, and arrowhead configuration.</param>
		void Draw(Painter2D painter, Vector2 source, Vector2 target, Connection connection, RuntimeEdgeStyle style);
	}
}
