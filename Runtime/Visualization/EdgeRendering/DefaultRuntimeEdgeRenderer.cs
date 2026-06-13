using Nonatomic.VSM.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Visualization
{
	/// <summary>
	/// Simple cubic bezier between two ports with fixed-direction horizontal tangents.
	/// Cheap, predictable, matches Unity's default graph view look.
	/// </summary>
	public class DefaultRuntimeEdgeRenderer : IRuntimeEdgeRenderer
	{
		public RuntimeEdgeRendererType Type => RuntimeEdgeRendererType.Default;

		public void Draw(Painter2D painter, Vector2 source, Vector2 target, Connection connection, RuntimeEdgeStyle style)
		{
			painter.lineWidth = style.Width;
			painter.strokeColor = style.StrokeColor;
			painter.fillColor = style.StrokeColor;
			painter.lineCap = LineCap.Round;
			painter.lineJoin = LineJoin.Round;

			var dx = Mathf.Max(40f, Mathf.Abs(target.x - source.x) * 0.5f);
			var p1 = new Vector2(source.x + dx, source.y);
			var p2 = new Vector2(target.x - dx, target.y);

			painter.BeginPath();
			painter.MoveTo(source);
			painter.BezierCurveTo(p1, p2, target);
			painter.Stroke();

			if (style.DrawArrowhead)
			{
				EdgeRenderingHelpers.DrawArrowhead(painter, p2, target, style.ArrowheadSize);
			}
		}
	}
}
