using UnityEngine;

namespace Nonatomic.VSM.Visualization
{
	/// <summary>
	/// Per-edge styling supplied to <see cref="IRuntimeEdgeRenderer"/>. Pulled from the
	/// <see cref="RuntimeGraphTheme"/> by <see cref="RuntimeGraphView"/> with state-dependent
	/// overrides (active source colour, etc.).
	/// </summary>
	public struct RuntimeEdgeStyle
	{
		public Color StrokeColor;
		public float Width;
		public bool DrawArrowhead;
		public float ArrowheadSize;
	}
}
