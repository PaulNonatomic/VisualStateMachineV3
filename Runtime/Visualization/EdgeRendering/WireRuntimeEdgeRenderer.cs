using Nonatomic.VSM.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Visualization
{
	/// <summary>
	/// Wire-style cubic bezier matching the editor's wire renderer. Honours the
	/// per-connection control-point offsets and tangents stored on
	/// <see cref="Connection"/>, so edges bent in the editor stay bent in the
	/// runtime view. Falls back to an adaptive single-bezier when no user edits
	/// are present.
	///
	/// <para>
	/// Backward connections (target left of source) loop above or below to avoid
	/// crossing the source node, matching the editor's behaviour.
	/// </para>
	/// </summary>
	public class WireRuntimeEdgeRenderer : IRuntimeEdgeRenderer
	{
		private const float BackwardLoopOffset = 80f;
		private const float MinControlOffset = 50f;
		private const float MaxControlOffset = 200f;
		private const float DistanceTangentFactor = 0.4f;
		private const float DefaultTangentLength = 50f;
		private const float OffsetEpsilonSqr = 0.01f;

		public RuntimeEdgeRendererType Type => RuntimeEdgeRendererType.Wire;

		public void Draw(Painter2D painter, Vector2 source, Vector2 target, Connection connection, RuntimeEdgeStyle style)
		{
			painter.lineWidth = style.Width;
			painter.strokeColor = style.StrokeColor;
			painter.fillColor = style.StrokeColor;
			painter.lineCap = LineCap.Round;
			painter.lineJoin = LineJoin.Round;

			var isBackward = target.x < source.x - 20f;
			var subdivisionLevel = connection?.SubdivisionLevel ?? 0;

			Vector2 arrowTangentSource;
			switch (subdivisionLevel)
			{
				case 1:
					arrowTangentSource = DrawLevel1(painter, source, target, connection, isBackward);
					break;
				case 2:
					arrowTangentSource = DrawLevel2(painter, source, target, connection, isBackward);
					break;
				default:
					arrowTangentSource = DrawLevel0(painter, source, target, connection, isBackward);
					break;
			}

			if (style.DrawArrowhead)
			{
				EdgeRenderingHelpers.DrawArrowhead(painter, arrowTangentSource, target, style.ArrowheadSize);
			}
		}

		/// <summary>
		/// Level 0 — no subdivision: either a single adaptive cubic bezier (no edits)
		/// or a 2-segment piecewise bezier through the user-adjusted midpoint.
		/// </summary>
		private static Vector2 DrawLevel0(Painter2D painter, Vector2 source, Vector2 target, Connection connection, bool isBackward)
		{
			var midOffset = connection?.MidpointOffset ?? Vector2.zero;
			var midTangentUser = connection?.MidpointTangent ?? Vector2.zero;

			var hasMidEdit = midOffset.sqrMagnitude > OffsetEpsilonSqr || midTangentUser.sqrMagnitude > OffsetEpsilonSqr;

			if (!hasMidEdit && !isBackward)
			{
				var distance = Vector2.Distance(source, target);
				var controlOffset = Mathf.Clamp(distance * DistanceTangentFactor, MinControlOffset, MaxControlOffset);
				var p1 = new Vector2(source.x + controlOffset, source.y);
				var p2 = new Vector2(target.x - controlOffset, target.y);

				painter.BeginPath();
				painter.MoveTo(source);
				painter.BezierCurveTo(p1, p2, target);
				painter.Stroke();
				return p2;
			}

			var midpoint = isBackward
				? new Vector2((source.x + target.x) * 0.5f, ComputeBackwardLoopY(source, target)) + midOffset
				: Vector2.Lerp(source, target, 0.5f) + midOffset;

			var midTangent = midTangentUser.sqrMagnitude > OffsetEpsilonSqr
				? midTangentUser
				: AutoMidpointTangent(source, midpoint, target);

			var startTangent = GetEdgeTangent(source, midpoint, true);
			var endTangent = GetEdgeTangent(target, midpoint, false);

			painter.BeginPath();
			painter.MoveTo(source);
			painter.BezierCurveTo(source + startTangent, midpoint - midTangent, midpoint);
			painter.BezierCurveTo(midpoint + midTangent, target + endTangent, target);
			painter.Stroke();

			return target + endTangent;
		}

		/// <summary>
		/// Level 1 — 4-segment bezier through quarter, mid, and three-quarter control points.
		/// </summary>
		private static Vector2 DrawLevel1(Painter2D painter, Vector2 source, Vector2 target, Connection connection, bool isBackward)
		{
			var (quarter, mid, threeQuarter) = ComputeLevel1Points(source, target, connection, isBackward);

			var quarterTangent = GetDefaultTangent(source, quarter, mid) + (connection?.QuarterPointTangent ?? Vector2.zero);
			var midTangent = GetDefaultTangent(quarter, mid, threeQuarter) + (connection?.MidpointTangent ?? Vector2.zero);
			var threeQuarterTangent = GetDefaultTangent(mid, threeQuarter, target) + (connection?.ThreeQuarterPointTangent ?? Vector2.zero);

			var startTangent = GetEdgeTangent(source, quarter, true);
			var endTangent = GetEdgeTangent(target, threeQuarter, false);

			painter.BeginPath();
			painter.MoveTo(source);
			painter.BezierCurveTo(source + startTangent, quarter - quarterTangent, quarter);
			painter.BezierCurveTo(quarter + quarterTangent, mid - midTangent, mid);
			painter.BezierCurveTo(mid + midTangent, threeQuarter - threeQuarterTangent, threeQuarter);
			painter.BezierCurveTo(threeQuarter + threeQuarterTangent, target + endTangent, target);
			painter.Stroke();

			return target + endTangent;
		}

		/// <summary>
		/// Level 2 — 8-segment bezier through all seven control points (1/8, 1/4, 3/8, 1/2, 5/8, 3/4, 7/8).
		/// </summary>
		private static Vector2 DrawLevel2(Painter2D painter, Vector2 source, Vector2 target, Connection connection, bool isBackward)
		{
			var (eighth, quarter, threeEighths, mid, fiveEighths, threeQuarter, sevenEighths)
				= ComputeLevel2Points(source, target, connection, isBackward);

			var eighthTangent = GetDefaultTangent(source, eighth, quarter) + (connection?.EighthPointTangent ?? Vector2.zero);
			var quarterTangent = GetDefaultTangent(eighth, quarter, threeEighths) + (connection?.QuarterPointTangent ?? Vector2.zero);
			var threeEighthsTangent = GetDefaultTangent(quarter, threeEighths, mid) + (connection?.ThreeEighthsPointTangent ?? Vector2.zero);
			var midTangent = GetDefaultTangent(threeEighths, mid, fiveEighths) + (connection?.MidpointTangent ?? Vector2.zero);
			var fiveEighthsTangent = GetDefaultTangent(mid, fiveEighths, threeQuarter) + (connection?.FiveEighthsPointTangent ?? Vector2.zero);
			var threeQuarterTangent = GetDefaultTangent(fiveEighths, threeQuarter, sevenEighths) + (connection?.ThreeQuarterPointTangent ?? Vector2.zero);
			var sevenEighthsTangent = GetDefaultTangent(threeQuarter, sevenEighths, target) + (connection?.SevenEighthsPointTangent ?? Vector2.zero);

			var startTangent = GetEdgeTangent(source, eighth, true);
			var endTangent = GetEdgeTangent(target, sevenEighths, false);

			painter.BeginPath();
			painter.MoveTo(source);
			painter.BezierCurveTo(source + startTangent, eighth - eighthTangent, eighth);
			painter.BezierCurveTo(eighth + eighthTangent, quarter - quarterTangent, quarter);
			painter.BezierCurveTo(quarter + quarterTangent, threeEighths - threeEighthsTangent, threeEighths);
			painter.BezierCurveTo(threeEighths + threeEighthsTangent, mid - midTangent, mid);
			painter.BezierCurveTo(mid + midTangent, fiveEighths - fiveEighthsTangent, fiveEighths);
			painter.BezierCurveTo(fiveEighths + fiveEighthsTangent, threeQuarter - threeQuarterTangent, threeQuarter);
			painter.BezierCurveTo(threeQuarter + threeQuarterTangent, sevenEighths - sevenEighthsTangent, sevenEighths);
			painter.BezierCurveTo(sevenEighths + sevenEighthsTangent, target + endTangent, target);
			painter.Stroke();

			return target + endTangent;
		}

		private static (Vector2 quarter, Vector2 mid, Vector2 threeQuarter) ComputeLevel1Points(
			Vector2 source, Vector2 target, Connection connection, bool isBackward)
		{
			Vector2 quarter, mid, threeQuarter;

			if (isBackward)
			{
				var loopY = ComputeBackwardLoopY(source, target);
				quarter = new Vector2(source.x + MinControlOffset, loopY);
				mid = new Vector2((source.x + target.x) * 0.5f, loopY);
				threeQuarter = new Vector2(target.x - MinControlOffset, loopY);
			}
			else
			{
				quarter = Vector2.Lerp(source, target, 0.25f);
				mid = Vector2.Lerp(source, target, 0.5f);
				threeQuarter = Vector2.Lerp(source, target, 0.75f);
			}

			quarter += connection?.QuarterPointOffset ?? Vector2.zero;
			mid += connection?.MidpointOffset ?? Vector2.zero;
			threeQuarter += connection?.ThreeQuarterPointOffset ?? Vector2.zero;

			return (quarter, mid, threeQuarter);
		}

		private static (Vector2 eighth, Vector2 quarter, Vector2 threeEighths, Vector2 mid,
			Vector2 fiveEighths, Vector2 threeQuarter, Vector2 sevenEighths) ComputeLevel2Points(
			Vector2 source, Vector2 target, Connection connection, bool isBackward)
		{
			var (quarter, mid, threeQuarter) = ComputeLevel1Points(source, target, connection, isBackward);

			// Level-2 in-between points are lerped between adjacent level-1 points,
			// then user offsets are applied — same as the editor's logic.
			var eighth = Vector2.Lerp(source, quarter, 0.5f) + (connection?.EighthPointOffset ?? Vector2.zero);
			var threeEighths = Vector2.Lerp(quarter, mid, 0.5f) + (connection?.ThreeEighthsPointOffset ?? Vector2.zero);
			var fiveEighths = Vector2.Lerp(mid, threeQuarter, 0.5f) + (connection?.FiveEighthsPointOffset ?? Vector2.zero);
			var sevenEighths = Vector2.Lerp(threeQuarter, target, 0.5f) + (connection?.SevenEighthsPointOffset ?? Vector2.zero);

			return (eighth, quarter, threeEighths, mid, fiveEighths, threeQuarter, sevenEighths);
		}

		private static float ComputeBackwardLoopY(Vector2 source, Vector2 target)
		{
			var loopBelow = target.y > source.y;
			return loopBelow
				? Mathf.Max(source.y, target.y) + BackwardLoopOffset
				: Mathf.Min(source.y, target.y) - BackwardLoopOffset;
		}

		private static Vector2 AutoMidpointTangent(Vector2 source, Vector2 midpoint, Vector2 target)
		{
			var dirFromStart = (midpoint - source).normalized;
			var dirToEnd = (target - midpoint).normalized;
			var avgDir = (dirFromStart + dirToEnd).normalized;
			var avgDist = (Vector2.Distance(source, midpoint) + Vector2.Distance(midpoint, target)) * 0.5f;
			return avgDir * Mathf.Clamp(avgDist * 0.35f, 20f, DefaultTangentLength);
		}

		/// <summary>
		/// Cardinal-spline tangent at <paramref name="current"/> given its neighbours.
		/// Tension and angle factor reduce overshoot at sharp turns.
		/// </summary>
		private static Vector2 GetDefaultTangent(Vector2 prev, Vector2 current, Vector2 next)
		{
			var toPrev = prev - current;
			var toNext = next - current;
			var distToPrev = toPrev.magnitude;
			var distToNext = toNext.magnitude;

			if (distToPrev < 0.001f || distToNext < 0.001f)
			{
				return (next - prev).normalized * DefaultTangentLength * 0.3f;
			}

			var dirToPrev = toPrev / distToPrev;
			var dirToNext = toNext / distToNext;

			var dot = Vector2.Dot(-dirToPrev, dirToNext);
			var angleFactor = Mathf.Sqrt(Mathf.Clamp01((dot + 1f) * 0.5f));

			var harmonicMean = 2f * distToPrev * distToNext / (distToPrev + distToNext);

			var totalDist = distToPrev + distToNext;
			var weightPrev = distToNext / totalDist;
			var weightNext = distToPrev / totalDist;
			var direction = (-dirToPrev * weightPrev + dirToNext * weightNext).normalized;

			const float tension = 0.4f;
			var length = Mathf.Clamp(harmonicMean * tension * angleFactor, 10f, DefaultTangentLength);

			return direction * length;
		}

		/// <summary>
		/// Smooth tangent at an edge endpoint (source or target). Blends a horizontal
		/// port-aligned direction with the direction toward the first interior control
		/// point — closer control points stay more horizontal, distant ones lean in.
		/// </summary>
		private static Vector2 GetEdgeTangent(Vector2 edgePoint, Vector2 firstControlPoint, bool isStart)
		{
			var toControl = firstControlPoint - edgePoint;
			var distToControl = toControl.magnitude;
			if (distToControl < 0.001f)
			{
				return new Vector2(isStart ? 30f : -30f, 0f);
			}

			var dirToControl = toControl / distToControl;
			var horizontal = new Vector2(isStart ? 1f : -1f, 0f);

			var blendFactor = Mathf.Clamp01(distToControl / 100f);
			blendFactor *= blendFactor;

			var direction = Vector2.Lerp(horizontal, dirToControl, blendFactor * 0.5f).normalized;
			var length = Mathf.Clamp(distToControl * 0.5f, 20f, DefaultTangentLength);

			return direction * length;
		}
	}
}
