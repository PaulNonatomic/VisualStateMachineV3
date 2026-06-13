using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Visualization
{
	/// <summary>
	/// Shared drawing primitives used by <see cref="IRuntimeEdgeRenderer"/> implementations.
	/// </summary>
	public static class EdgeRenderingHelpers
	{
		/// <summary>
		/// Draws a filled-triangle arrowhead at <paramref name="tip"/>, oriented along the
		/// vector from <paramref name="tangentSource"/> to <paramref name="tip"/>.
		/// </summary>
		public static void DrawArrowhead(Painter2D painter, Vector2 tangentSource, Vector2 tip, float arrowSize)
		{
			var tangent = (tip - tangentSource).normalized;
			if (tangent.sqrMagnitude < 0.0001f)
			{
				return;
			}

			var perp = new Vector2(-tangent.y, tangent.x);
			var back = tip - tangent * arrowSize;
			var a = back + perp * (arrowSize * 0.5f);
			var b = back - perp * (arrowSize * 0.5f);

			painter.BeginPath();
			painter.MoveTo(tip);
			painter.LineTo(a);
			painter.LineTo(b);
			painter.ClosePath();
			painter.Fill();
		}
	}
}
