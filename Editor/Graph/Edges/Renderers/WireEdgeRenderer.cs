using System.Collections.Generic;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Editor.Graph.Settings;
using Nonatomic.VSM.Graph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Nonatomic.VSM.Editor.Graph.Renderers
{
	/// <summary>
	/// Wire-style edge renderer with bezier splines and draggable control points.
	/// </summary>
	public class WireEdgeRenderer : IEdgeRenderer
	{
		/// <inheritdoc/>
		public EdgeRendererType Type => EdgeRendererType.Wire;

		/// <inheritdoc/>
		public string DisplayName => "Wire (Bezier)";

		/// <inheritdoc/>
		public bool SupportsCurveCustomization => true;

		/// <inheritdoc/>
		public IEdgeRendererSettings Settings => WireEdgeSettings.Instance;

		/// <inheritdoc/>
		public EdgeControl CreateEdgeControl()
		{
			return new WireEdgeControl(WireEdgeSettings.Instance);
		}

		/// <inheritdoc/>
		public void ApplyConnectionData(EdgeControl control, Connection connectionData)
		{
			if (control is not WireEdgeControl wireControl || connectionData == null) return;

			// Sync Level 1 position offsets
			if (connectionData.QuarterPointOffset.HasValue)
				wireControl.SetQuarterPointOffset(connectionData.QuarterPointOffset.Value);
			else
				wireControl.ClearQuarterPointOffset();

			if (connectionData.MidpointOffset.HasValue)
				wireControl.SetMidpointOffset(connectionData.MidpointOffset.Value);
			else
				wireControl.ClearMidpointOffset();

			if (connectionData.ThreeQuarterPointOffset.HasValue)
				wireControl.SetThreeQuarterPointOffset(connectionData.ThreeQuarterPointOffset.Value);
			else
				wireControl.ClearThreeQuarterPointOffset();

			// Sync Level 1 tangents
			if (connectionData.QuarterPointTangent.HasValue)
				wireControl.SetQuarterPointTangent(connectionData.QuarterPointTangent.Value);
			else
				wireControl.ClearQuarterPointTangent();

			if (connectionData.MidpointTangent.HasValue)
				wireControl.SetMidpointTangent(connectionData.MidpointTangent.Value);
			else
				wireControl.ClearMidpointTangent();

			if (connectionData.ThreeQuarterPointTangent.HasValue)
				wireControl.SetThreeQuarterPointTangent(connectionData.ThreeQuarterPointTangent.Value);
			else
				wireControl.ClearThreeQuarterPointTangent();

			// Sync Level 2 position offsets
			if (connectionData.EighthPointOffset.HasValue)
				wireControl.SetEighthPointOffset(connectionData.EighthPointOffset.Value);
			else
				wireControl.ClearEighthPointOffset();

			if (connectionData.ThreeEighthsPointOffset.HasValue)
				wireControl.SetThreeEighthsPointOffset(connectionData.ThreeEighthsPointOffset.Value);
			else
				wireControl.ClearThreeEighthsPointOffset();

			if (connectionData.FiveEighthsPointOffset.HasValue)
				wireControl.SetFiveEighthsPointOffset(connectionData.FiveEighthsPointOffset.Value);
			else
				wireControl.ClearFiveEighthsPointOffset();

			if (connectionData.SevenEighthsPointOffset.HasValue)
				wireControl.SetSevenEighthsPointOffset(connectionData.SevenEighthsPointOffset.Value);
			else
				wireControl.ClearSevenEighthsPointOffset();

			// Sync Level 2 tangents
			if (connectionData.EighthPointTangent.HasValue)
				wireControl.SetEighthPointTangent(connectionData.EighthPointTangent.Value);
			else
				wireControl.ClearEighthPointTangent();

			if (connectionData.ThreeEighthsPointTangent.HasValue)
				wireControl.SetThreeEighthsPointTangent(connectionData.ThreeEighthsPointTangent.Value);
			else
				wireControl.ClearThreeEighthsPointTangent();

			if (connectionData.FiveEighthsPointTangent.HasValue)
				wireControl.SetFiveEighthsPointTangent(connectionData.FiveEighthsPointTangent.Value);
			else
				wireControl.ClearFiveEighthsPointTangent();

			if (connectionData.SevenEighthsPointTangent.HasValue)
				wireControl.SetSevenEighthsPointTangent(connectionData.SevenEighthsPointTangent.Value);
			else
				wireControl.ClearSevenEighthsPointTangent();

			// Sync subdivision level
			wireControl.SetSubdivisionLevel(connectionData.SubdivisionLevel);

			wireControl.MarkDirty();
		}

		/// <inheritdoc/>
		public IReadOnlyList<Vector2> GetCurvePoints(EdgeControl control)
		{
			if (control is WireEdgeControl wireControl)
			{
				return wireControl.CurvePoints;
			}

			return System.Array.Empty<Vector2>();
		}
	}
}
