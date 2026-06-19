using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Custom edge control that renders cubic bezier connections.
	/// Uses a midpoint offset to control the curve bend.
	/// </summary>
	public class WireEdgeControl : EdgeControl
	{
		private readonly WireEdgeSettings _settings;
		private readonly List<Vector2> _curvePoints = new List<Vector2>();
		private bool _isDirty = true;

		// Cached positions for dirty checking
		private Vector2 _lastFrom;
		private Vector2 _lastTo;

		// Spline control points (positions on the curve)
		private Vector2 _quarterPoint;
		private Vector2 _midpoint;
		private Vector2 _threeQuarterPoint;

		// User-defined offsets for the control points
		private Vector2 _quarterPointOffset;
		private Vector2 _midpointOffset;
		private Vector2 _threeQuarterPointOffset;

		// User-defined tangents (symmetric, applied in both directions)
		private Vector2 _quarterPointTangent;
		private Vector2 _midpointTangent;
		private Vector2 _threeQuarterPointTangent;

		// Level 2 subdivision points (1/8, 3/8, 5/8, 7/8)
		private Vector2 _eighthPoint;
		private Vector2 _threeEighthsPoint;
		private Vector2 _fiveEighthsPoint;
		private Vector2 _sevenEighthsPoint;

		// Level 2 user-defined offsets
		private Vector2 _eighthPointOffset;
		private Vector2 _threeEighthsPointOffset;
		private Vector2 _fiveEighthsPointOffset;
		private Vector2 _sevenEighthsPointOffset;

		// Level 2 user-defined tangents
		private Vector2 _eighthPointTangent;
		private Vector2 _threeEighthsPointTangent;
		private Vector2 _fiveEighthsPointTangent;
		private Vector2 _sevenEighthsPointTangent;

		// Subdivision level: 0 = midpoint only, 1 = quarter + mid + three-quarter, 2 = all 7 points
		private int _subdivisionLevel;

		// Default tangent length (longer = smoother curves)
		private const float DefaultTangentLength = 50f;

		// Our own colors for wire rendering
		private Color _wireInputColor = new Color(0.8f, 0.8f, 0.8f);
		private Color _wireOutputColor = new Color(0.8f, 0.8f, 0.8f);

		// Hover state
		private bool _isHovered;
		// Emphasis: drawn thicker when this edge carries a breakpoint.
		private bool _emphasized;
		private static readonly Color HoverTint = new Color(0.3f, 0.3f, 0.3f, 0f);

		// Higher value = smoother curves but more vertices
		// 128 ensures at least 16 segments per section even at Level 2 (8 sections)
		private const int CurveSegments = 128;

		public WireEdgeControl() : this(WireEdgeSettings.Instance)
		{
		}

		public WireEdgeControl(WireEdgeSettings settings)
		{
			_settings = settings ?? WireEdgeSettings.Instance;

			generateVisualContent = null;
			generateVisualContent = OnGenerateVisualContent;

			pickingMode = PickingMode.Position;
			edgeWidth = 10;

			inputColor = Color.clear;
			outputColor = Color.clear;

			// Register for hover events
			RegisterCallback<MouseEnterEvent>(OnMouseEnter);
			RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

			AddToClassList("wire-edge-control");
		}

		private void OnMouseEnter(MouseEnterEvent evt)
		{
			_isHovered = true;
			MarkDirtyRepaint();
		}

		private void OnMouseLeave(MouseLeaveEvent evt)
		{
			_isHovered = false;
			MarkDirtyRepaint();
		}

		protected override void ComputeControlPoints()
		{
			base.ComputeControlPoints();
			ComputeBezierCurve();
			UpdateBoundsFromCurve();
		}

		/// <summary>
		/// Expands the element's layout to encompass all curve points.
		/// This ensures hit testing works for curves that bend outside the from-to rectangle.
		/// </summary>
		private void UpdateBoundsFromCurve()
		{
			if (_curvePoints.Count < 2) return;

			// Find bounding box of all curve points
			var minX = float.MaxValue;
			var minY = float.MaxValue;
			var maxX = float.MinValue;
			var maxY = float.MinValue;

			foreach (var point in _curvePoints)
			{
				minX = Mathf.Min(minX, point.x);
				minY = Mathf.Min(minY, point.y);
				maxX = Mathf.Max(maxX, point.x);
				maxY = Mathf.Max(maxY, point.y);
			}

			// Add padding for hit detection
			var padding = Mathf.Max(_settings.EdgeWidth * 2f, 15f);
			minX -= padding;
			minY -= padding;
			maxX += padding;
			maxY += padding;

			// Update the element's layout to encompass the curve
			style.left = minX;
			style.top = minY;
			style.width = maxX - minX;
			style.height = maxY - minY;
		}

		private void ComputeBezierCurve()
		{
			if (!_isDirty && Vector2.Distance(_lastFrom, from) < 0.1f && Vector2.Distance(_lastTo, to) < 0.1f)
				return;

			_lastFrom = from;
			_lastTo = to;

			CalculateSplinePoints();
			GenerateCurvePoints();

			_isDirty = false;
		}

		private void CalculateSplinePoints()
		{
			// Check if backward connection
			var isBackward = to.x < from.x - 20f;

			if (isBackward)
			{
				// For backward connections, create a loop path
				var loopOffset = _settings.BackwardLoopOffset;
				var loopBelow = to.y <= from.y + loopOffset;
				var loopY = loopBelow
					? Mathf.Max(from.y, to.y) + loopOffset
					: Mathf.Min(from.y, to.y) - loopOffset;

				// Default positions for backward loop - Level 1 points
				_quarterPoint = new Vector2(from.x + 50f, loopY) + _quarterPointOffset;
				_midpoint = new Vector2((from.x + to.x) / 2f, loopY) + _midpointOffset;
				_threeQuarterPoint = new Vector2(to.x - 50f, loopY) + _threeQuarterPointOffset;

				// Level 2 points for backward loop
				_eighthPoint = Vector2.Lerp(from, _quarterPoint, 0.5f) + _eighthPointOffset;
				_threeEighthsPoint = Vector2.Lerp(_quarterPoint, _midpoint, 0.5f) + _threeEighthsPointOffset;
				_fiveEighthsPoint = Vector2.Lerp(_midpoint, _threeQuarterPoint, 0.5f) + _fiveEighthsPointOffset;
				_sevenEighthsPoint = Vector2.Lerp(_threeQuarterPoint, to, 0.5f) + _sevenEighthsPointOffset;
			}
			else
			{
				// For forward connections, default points on a straight line - Level 1 points
				_quarterPoint = Vector2.Lerp(from, to, 0.25f) + _quarterPointOffset;
				_midpoint = Vector2.Lerp(from, to, 0.5f) + _midpointOffset;
				_threeQuarterPoint = Vector2.Lerp(from, to, 0.75f) + _threeQuarterPointOffset;

				// Level 2 points
				_eighthPoint = Vector2.Lerp(from, to, 0.125f) + _eighthPointOffset;
				_threeEighthsPoint = Vector2.Lerp(from, to, 0.375f) + _threeEighthsPointOffset;
				_fiveEighthsPoint = Vector2.Lerp(from, to, 0.625f) + _fiveEighthsPointOffset;
				_sevenEighthsPoint = Vector2.Lerp(from, to, 0.875f) + _sevenEighthsPointOffset;
			}
		}

		private void GenerateCurvePoints()
		{
			_curvePoints.Clear();

			var isBackward = to.x < from.x - 20f;

			if (_subdivisionLevel == 0)
			{
				// Level 0: Single midpoint control - use 2-segment bezier through midpoint
				GenerateMidpointCurve(isBackward);
			}
			else if (_subdivisionLevel == 1)
			{
				// Level 1: Full control with quarter, mid, and three-quarter points (4 segments)
				GenerateLevel1SubdividedCurve(isBackward);
			}
			else
			{
				// Level 2: Maximum control with all 7 points (8 segments)
				GenerateLevel2SubdividedCurve(isBackward);
			}
		}

		/// <summary>
		/// Generates a curve with only midpoint control (subdivision level 0).
		/// Uses a single bezier when no offset, or 2-segment bezier when midpoint is adjusted.
		/// </summary>
		private void GenerateMidpointCurve(bool isBackward)
		{
			// Check if midpoint has been adjusted
			var hasMidpointOffset = _midpointOffset.sqrMagnitude > 0.01f || _midpointTangent.sqrMagnitude > 0.01f;

			// Calculate adaptive tangent lengths
			var totalDist = Vector2.Distance(from, to);
			var controlOffset = Mathf.Clamp(totalDist * 0.4f, 50f, 200f);

			if (!hasMidpointOffset && !isBackward)
			{
				// Clean single bezier for default look
				var p0 = from;
				var p1 = from + new Vector2(controlOffset, 0);
				var p2 = to - new Vector2(controlOffset, 0);
				var p3 = to;

				// Update midpoint to be on the actual curve for handle positioning
				_midpoint = CubicBezier(p0, p1, p2, p3, 0.5f);

				// Generate smooth curve
				for (var i = 0; i <= CurveSegments; i++)
				{
					var t = (float)i / CurveSegments;
					_curvePoints.Add(CubicBezier(p0, p1, p2, p3, t));
				}
			}
			else
			{
				// 2-segment bezier through the adjusted midpoint
				var segmentsPerHalf = CurveSegments / 2;

				// Calculate smooth midpoint tangent
				var midTangent = _midpointTangent;
				if (midTangent.sqrMagnitude < 0.01f)
				{
					// Auto-calculate tangent based on curve geometry
					var dirFromStart = (_midpoint - from).normalized;
					var dirToEnd = (to - _midpoint).normalized;
					var avgDir = (dirFromStart + dirToEnd).normalized;

					// Length based on average distance to endpoints
					var avgDist = (Vector2.Distance(from, _midpoint) + Vector2.Distance(_midpoint, to)) * 0.5f;
					midTangent = avgDir * Mathf.Clamp(avgDist * 0.35f, 20f, DefaultTangentLength);
				}

				// Calculate smooth edge tangents that blend toward the midpoint
				var startTangent = GetEdgeTangent(from, _midpoint, true);
				var endTangent = GetEdgeTangent(to, _midpoint, false);

				// Segment 1: from → midpoint
				GenerateSegment(from, from + startTangent, _midpoint - midTangent, _midpoint, segmentsPerHalf, true);

				// Segment 2: midpoint → to
				GenerateSegment(_midpoint, _midpoint + midTangent, to + endTangent, to, segmentsPerHalf, false);
			}
		}

		/// <summary>
		/// Generates a curve with level 1 subdivision (3 handles).
		/// Uses 4-segment bezier: from → quarter → mid → three-quarter → to
		/// </summary>
		private void GenerateLevel1SubdividedCurve(bool isBackward)
		{
			var segmentsPerSection = CurveSegments / 4;

			// Calculate default tangent directions based on curve flow
			var defaultQuarterTangent = GetDefaultTangent(from, _quarterPoint, _midpoint);
			var defaultMidTangent = GetDefaultTangent(_quarterPoint, _midpoint, _threeQuarterPoint);
			var defaultThreeQuarterTangent = GetDefaultTangent(_midpoint, _threeQuarterPoint, to);

			// Apply user tangent modifications
			var quarterTangent = defaultQuarterTangent + _quarterPointTangent;
			var midTangent = defaultMidTangent + _midpointTangent;
			var threeQuarterTangent = defaultThreeQuarterTangent + _threeQuarterPointTangent;

			// Calculate smooth edge tangents that blend with the curve
			var startTangent = GetEdgeTangent(from, _quarterPoint, true);
			var endTangent = GetEdgeTangent(to, _threeQuarterPoint, false);

			// Segment 1: from → quarterPoint
			GenerateSegment(from, from + startTangent, _quarterPoint - quarterTangent, _quarterPoint, segmentsPerSection, true);

			// Segment 2: quarterPoint → midpoint
			GenerateSegment(_quarterPoint, _quarterPoint + quarterTangent, _midpoint - midTangent, _midpoint, segmentsPerSection, false);

			// Segment 3: midpoint → threeQuarterPoint
			GenerateSegment(_midpoint, _midpoint + midTangent, _threeQuarterPoint - threeQuarterTangent, _threeQuarterPoint, segmentsPerSection, false);

			// Segment 4: threeQuarterPoint → to
			GenerateSegment(_threeQuarterPoint, _threeQuarterPoint + threeQuarterTangent, to + endTangent, to, segmentsPerSection, false);
		}

		/// <summary>
		/// Generates a curve with level 2 subdivision (7 handles).
		/// Uses 8-segment bezier: from → 1/8 → 1/4 → 3/8 → 1/2 → 5/8 → 3/4 → 7/8 → to
		/// </summary>
		private void GenerateLevel2SubdividedCurve(bool isBackward)
		{
			var segmentsPerSection = CurveSegments / 8;

			// Calculate default tangent directions based on curve flow for all 7 control points
			var defaultEighthTangent = GetDefaultTangent(from, _eighthPoint, _quarterPoint);
			var defaultQuarterTangent = GetDefaultTangent(_eighthPoint, _quarterPoint, _threeEighthsPoint);
			var defaultThreeEighthsTangent = GetDefaultTangent(_quarterPoint, _threeEighthsPoint, _midpoint);
			var defaultMidTangent = GetDefaultTangent(_threeEighthsPoint, _midpoint, _fiveEighthsPoint);
			var defaultFiveEighthsTangent = GetDefaultTangent(_midpoint, _fiveEighthsPoint, _threeQuarterPoint);
			var defaultThreeQuarterTangent = GetDefaultTangent(_fiveEighthsPoint, _threeQuarterPoint, _sevenEighthsPoint);
			var defaultSevenEighthsTangent = GetDefaultTangent(_threeQuarterPoint, _sevenEighthsPoint, to);

			// Apply user tangent modifications
			var eighthTangent = defaultEighthTangent + _eighthPointTangent;
			var quarterTangent = defaultQuarterTangent + _quarterPointTangent;
			var threeEighthsTangent = defaultThreeEighthsTangent + _threeEighthsPointTangent;
			var midTangent = defaultMidTangent + _midpointTangent;
			var fiveEighthsTangent = defaultFiveEighthsTangent + _fiveEighthsPointTangent;
			var threeQuarterTangent = defaultThreeQuarterTangent + _threeQuarterPointTangent;
			var sevenEighthsTangent = defaultSevenEighthsTangent + _sevenEighthsPointTangent;

			// Calculate smooth edge tangents that blend with the curve
			var startTangent = GetEdgeTangent(from, _eighthPoint, true);
			var endTangent = GetEdgeTangent(to, _sevenEighthsPoint, false);

			// Segment 1: from → eighthPoint
			GenerateSegment(from, from + startTangent, _eighthPoint - eighthTangent, _eighthPoint, segmentsPerSection, true);

			// Segment 2: eighthPoint → quarterPoint
			GenerateSegment(_eighthPoint, _eighthPoint + eighthTangent, _quarterPoint - quarterTangent, _quarterPoint, segmentsPerSection, false);

			// Segment 3: quarterPoint → threeEighthsPoint
			GenerateSegment(_quarterPoint, _quarterPoint + quarterTangent, _threeEighthsPoint - threeEighthsTangent, _threeEighthsPoint, segmentsPerSection, false);

			// Segment 4: threeEighthsPoint → midpoint
			GenerateSegment(_threeEighthsPoint, _threeEighthsPoint + threeEighthsTangent, _midpoint - midTangent, _midpoint, segmentsPerSection, false);

			// Segment 5: midpoint → fiveEighthsPoint
			GenerateSegment(_midpoint, _midpoint + midTangent, _fiveEighthsPoint - fiveEighthsTangent, _fiveEighthsPoint, segmentsPerSection, false);

			// Segment 6: fiveEighthsPoint → threeQuarterPoint
			GenerateSegment(_fiveEighthsPoint, _fiveEighthsPoint + fiveEighthsTangent, _threeQuarterPoint - threeQuarterTangent, _threeQuarterPoint, segmentsPerSection, false);

			// Segment 7: threeQuarterPoint → sevenEighthsPoint
			GenerateSegment(_threeQuarterPoint, _threeQuarterPoint + threeQuarterTangent, _sevenEighthsPoint - sevenEighthsTangent, _sevenEighthsPoint, segmentsPerSection, false);

			// Segment 8: sevenEighthsPoint → to
			GenerateSegment(_sevenEighthsPoint, _sevenEighthsPoint + sevenEighthsTangent, to + endTangent, to, segmentsPerSection, false);
		}

		private Vector2 GetDefaultTangent(Vector2 prev, Vector2 current, Vector2 next)
		{
			// Cardinal spline tangent calculation with automatic tension
			var toPrev = prev - current;
			var toNext = next - current;

			var distToPrev = toPrev.magnitude;
			var distToNext = toNext.magnitude;

			// Avoid division by zero
			if (distToPrev < 0.001f || distToNext < 0.001f)
			{
				return (next - prev).normalized * DefaultTangentLength * 0.3f;
			}

			// Normalize directions
			var dirToPrev = toPrev / distToPrev;
			var dirToNext = toNext / distToNext;

			// Calculate the angle between incoming and outgoing directions
			// This helps us adjust tension based on how sharp the turn is
			var dot = Vector2.Dot(-dirToPrev, dirToNext);

			// Angle factor: 1.0 when straight, decreasing as angle gets sharper
			// This prevents overshooting on sharp corners
			var angleFactor = Mathf.Clamp01((dot + 1f) * 0.5f);
			angleFactor = Mathf.Sqrt(angleFactor); // Smooth the falloff

			// Use harmonic mean of distances for more balanced tangent lengths
			// This handles uneven point spacing better than min
			var harmonicMean = 2f * distToPrev * distToNext / (distToPrev + distToNext);

			// Cardinal spline direction: weighted average of normalized directions
			// Weight by distance to favor the longer segment slightly
			var totalDist = distToPrev + distToNext;
			var weightPrev = distToNext / totalDist;
			var weightNext = distToPrev / totalDist;
			var direction = (-dirToPrev * weightPrev + dirToNext * weightNext).normalized;

			// Base tension factor (0.5 = Catmull-Rom, lower = tighter, higher = looser)
			const float tension = 0.4f;

			// Final tangent length combines harmonic mean with angle-based adjustment
			var length = harmonicMean * tension * angleFactor;
			length = Mathf.Clamp(length, 10f, DefaultTangentLength);

			return direction * length;
		}

		/// <summary>
		/// Calculates a smooth edge tangent (for start/end points) that blends the horizontal
		/// port direction with the direction toward the first control point.
		/// </summary>
		private Vector2 GetEdgeTangent(Vector2 edgePoint, Vector2 firstControlPoint, bool isStart)
		{
			var toControl = firstControlPoint - edgePoint;
			var distToControl = toControl.magnitude;

			if (distToControl < 0.001f)
			{
				return new Vector2(isStart ? 30f : -30f, 0);
			}

			var dirToControl = toControl / distToControl;

			// Horizontal direction for port alignment
			var horizontal = new Vector2(isStart ? 1f : -1f, 0);

			// Blend horizontal with direction to control point
			// Use more horizontal when control point is close, more toward control when far
			var blendFactor = Mathf.Clamp01(distToControl / 100f);
			blendFactor = blendFactor * blendFactor; // Ease in

			var direction = Vector2.Lerp(horizontal, dirToControl, blendFactor * 0.5f).normalized;

			// Tangent length based on distance to control point
			var length = Mathf.Clamp(distToControl * 0.5f, 20f, DefaultTangentLength);

			return direction * length;
		}

		private void GenerateSegment(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int segments, bool includeFirst)
		{
			var startIndex = includeFirst ? 0 : 1;
			for (var i = startIndex; i <= segments; i++)
			{
				var t = (float)i / segments;
				var point = CubicBezier(p0, p1, p2, p3, t);
				_curvePoints.Add(point);
			}
		}

		private static Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
		{
			var oneMinusT = 1f - t;
			var oneMinusT2 = oneMinusT * oneMinusT;
			var oneMinusT3 = oneMinusT2 * oneMinusT;
			var t2 = t * t;
			var t3 = t2 * t;

			return oneMinusT3 * p0 +
				   3f * oneMinusT2 * t * p1 +
				   3f * oneMinusT * t2 * p2 +
				   t3 * p3;
		}

		private void OnGenerateVisualContent(MeshGenerationContext mgc)
		{
			ComputeBezierCurve();

			if (_curvePoints.Count < 2) return;

			var offset = layout.position;
			var painter = mgc.painter2D;

			var baseWidth = _emphasized ? _settings.EdgeWidth + 4f : _settings.EdgeWidth;
			var lineWidth = _isHovered ? baseWidth + 1f : baseWidth;
			painter.lineWidth = lineWidth;
			painter.lineJoin = LineJoin.Round;

			// Draw gradient by rendering overlapping segments
			// Each segment overlaps with the next to ensure smooth color transitions
			var totalPoints = _curvePoints.Count;
			var numSegments = 16; // Number of gradient steps
			var pointsPerSegment = Mathf.Max(2, totalPoints / numSegments);

			painter.lineCap = LineCap.Round;

			for (var seg = 0; seg < numSegments; seg++)
			{
				var segmentStart = Mathf.Max(0, seg * pointsPerSegment - 1); // Overlap with previous
				var segmentEnd = Mathf.Min((seg + 1) * pointsPerSegment + 1, totalPoints - 1); // Overlap with next

				if (segmentStart >= totalPoints - 1) break;

				// Calculate the color at this segment position
				var t = (float)seg / (numSegments - 1);
				var segmentColor = Color.Lerp(_wireOutputColor, _wireInputColor, t);

				if (_isHovered)
				{
					segmentColor += HoverTint;
				}

				painter.BeginPath();
				painter.MoveTo(_curvePoints[segmentStart] - offset);

				for (var i = segmentStart + 1; i <= segmentEnd; i++)
				{
					painter.LineTo(_curvePoints[i] - offset);
				}

				painter.strokeColor = segmentColor;
				painter.Stroke();
			}
		}

		/// <summary>
		/// Sets the colors used to render the wire.
		/// </summary>
		public void SetWireColors(Color input, Color output)
		{
			_wireInputColor = input;
			_wireOutputColor = output;
			MarkDirtyRepaint();
		}

		/// <summary>
		/// When true, the wire is rendered thicker (used to highlight a breakpoint'd transition).
		/// </summary>
		public void SetEmphasized(bool emphasized)
		{
			if (_emphasized == emphasized) return;
			_emphasized = emphasized;
			MarkDirtyRepaint();
		}

		/// <summary>
		/// Marks the curve as dirty, forcing a recalculation on the next frame.
		/// </summary>
		public void MarkDirty()
		{
			_isDirty = true;
			MarkDirtyRepaint();
		}

		public override bool ContainsPoint(Vector2 localPoint)
		{
			// Ensure curve is computed
			if (_curvePoints.Count < 2)
			{
				ComputeBezierCurve();
				if (_curvePoints.Count < 2) return false;
			}

			// Convert local point to graph coordinates
			var offset = layout.position;
			var graphPoint = localPoint + offset;

			// Use a generous hit distance for better selection
			var hitDistance = Mathf.Max(_settings.EdgeWidth * 2f, 15f);

			for (var i = 0; i < _curvePoints.Count - 1; i++)
			{
				var distance = DistanceToSegment(graphPoint, _curvePoints[i], _curvePoints[i + 1]);
				if (distance <= hitDistance)
					return true;
			}

			return false;
		}

		public override bool Overlaps(Rect rectangle)
		{
			// Ensure curve is computed
			if (_curvePoints.Count < 2)
			{
				ComputeBezierCurve();
				if (_curvePoints.Count < 2) return false;
			}

			// Convert local rect to graph coordinates
			var offset = layout.position;
			var graphRect = new Rect(rectangle.x + offset.x, rectangle.y + offset.y, rectangle.width, rectangle.height);

			foreach (var point in _curvePoints)
			{
				if (graphRect.Contains(point))
					return true;
			}

			for (var i = 0; i < _curvePoints.Count - 1; i++)
			{
				if (LineIntersectsRect(_curvePoints[i], _curvePoints[i + 1], graphRect))
					return true;
			}

			return false;
		}

		private static float DistanceToSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
		{
			var segment = segmentEnd - segmentStart;
			var lengthSquared = segment.sqrMagnitude;
			if (lengthSquared < 0.0001f) return Vector2.Distance(point, segmentStart);

			var t = Mathf.Clamp01(Vector2.Dot(point - segmentStart, segment) / lengthSquared);
			var projection = segmentStart + t * segment;
			return Vector2.Distance(point, projection);
		}

		private static bool LineIntersectsRect(Vector2 p1, Vector2 p2, Rect rect)
		{
			if (rect.Contains(p1) || rect.Contains(p2))
				return true;

			var topLeft = new Vector2(rect.xMin, rect.yMin);
			var topRight = new Vector2(rect.xMax, rect.yMin);
			var bottomLeft = new Vector2(rect.xMin, rect.yMax);
			var bottomRight = new Vector2(rect.xMax, rect.yMax);

			return LinesIntersect(p1, p2, topLeft, topRight) ||
				   LinesIntersect(p1, p2, topRight, bottomRight) ||
				   LinesIntersect(p1, p2, bottomRight, bottomLeft) ||
				   LinesIntersect(p1, p2, bottomLeft, topLeft);
		}

		private static bool LinesIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
		{
			var d1 = Direction(p3, p4, p1);
			var d2 = Direction(p3, p4, p2);
			var d3 = Direction(p1, p2, p3);
			var d4 = Direction(p1, p2, p4);

			if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
				((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
				return true;

			if (d1 == 0 && OnSegment(p3, p4, p1)) return true;
			if (d2 == 0 && OnSegment(p3, p4, p2)) return true;
			if (d3 == 0 && OnSegment(p1, p2, p3)) return true;
			if (d4 == 0 && OnSegment(p1, p2, p4)) return true;

			return false;
		}

		private static float Direction(Vector2 a, Vector2 b, Vector2 c)
		{
			return (c.x - a.x) * (b.y - a.y) - (b.x - a.x) * (c.y - a.y);
		}

		private static bool OnSegment(Vector2 a, Vector2 b, Vector2 c)
		{
			return Mathf.Min(a.x, b.x) <= c.x && c.x <= Mathf.Max(a.x, b.x) &&
				   Mathf.Min(a.y, b.y) <= c.y && c.y <= Mathf.Max(a.y, b.y);
		}


		public void SetQuarterPointOffset(Vector2 offset)
		{
			_quarterPointOffset = offset;
			_isDirty = true;
		}

		public Vector2 GetQuarterPointOffset() => _quarterPointOffset;

		public void SetQuarterPointTangent(Vector2 tangent)
		{
			_quarterPointTangent = tangent;
			_isDirty = true;
		}

		public Vector2 GetQuarterPointTangent() => _quarterPointTangent;

		public Vector2 GetQuarterPoint()
		{
			ComputeBezierCurve();
			return _quarterPoint;
		}

		public void ClearQuarterPointOffset()
		{
			_quarterPointOffset = Vector2.zero;
			_isDirty = true;
		}

		public void ClearQuarterPointTangent()
		{
			_quarterPointTangent = Vector2.zero;
			_isDirty = true;
		}



		public void SetMidpointOffset(Vector2 offset)
		{
			_midpointOffset = offset;
			_isDirty = true;
		}

		public Vector2 GetMidpointOffset() => _midpointOffset;

		public void SetMidpointTangent(Vector2 tangent)
		{
			_midpointTangent = tangent;
			_isDirty = true;
		}

		public Vector2 GetMidpointTangent() => _midpointTangent;

		public Vector2 GetMidpoint()
		{
			ComputeBezierCurve();
			return _midpoint;
		}

		public void ClearMidpointOffset()
		{
			_midpointOffset = Vector2.zero;
			_isDirty = true;
		}

		public void ClearMidpointTangent()
		{
			_midpointTangent = Vector2.zero;
			_isDirty = true;
		}



		public void SetThreeQuarterPointOffset(Vector2 offset)
		{
			_threeQuarterPointOffset = offset;
			_isDirty = true;
		}

		public Vector2 GetThreeQuarterPointOffset() => _threeQuarterPointOffset;

		public void SetThreeQuarterPointTangent(Vector2 tangent)
		{
			_threeQuarterPointTangent = tangent;
			_isDirty = true;
		}

		public Vector2 GetThreeQuarterPointTangent() => _threeQuarterPointTangent;

		public Vector2 GetThreeQuarterPoint()
		{
			ComputeBezierCurve();
			return _threeQuarterPoint;
		}

		public void ClearThreeQuarterPointOffset()
		{
			_threeQuarterPointOffset = Vector2.zero;
			_isDirty = true;
		}

		public void ClearThreeQuarterPointTangent()
		{
			_threeQuarterPointTangent = Vector2.zero;
			_isDirty = true;
		}



		public void SetSubdivisionLevel(int level)
		{
			_subdivisionLevel = Mathf.Clamp(level, 0, 2);
			_isDirty = true;
		}

		public int GetSubdivisionLevel() => _subdivisionLevel;



		public void SetEighthPointOffset(Vector2 offset)
		{
			_eighthPointOffset = offset;
			_isDirty = true;
		}

		public Vector2 GetEighthPointOffset() => _eighthPointOffset;

		public void SetEighthPointTangent(Vector2 tangent)
		{
			_eighthPointTangent = tangent;
			_isDirty = true;
		}

		public Vector2 GetEighthPointTangent() => _eighthPointTangent;

		public Vector2 GetEighthPoint()
		{
			ComputeBezierCurve();
			return _eighthPoint;
		}

		public void ClearEighthPointOffset()
		{
			_eighthPointOffset = Vector2.zero;
			_isDirty = true;
		}

		public void ClearEighthPointTangent()
		{
			_eighthPointTangent = Vector2.zero;
			_isDirty = true;
		}



		public void SetThreeEighthsPointOffset(Vector2 offset)
		{
			_threeEighthsPointOffset = offset;
			_isDirty = true;
		}

		public Vector2 GetThreeEighthsPointOffset() => _threeEighthsPointOffset;

		public void SetThreeEighthsPointTangent(Vector2 tangent)
		{
			_threeEighthsPointTangent = tangent;
			_isDirty = true;
		}

		public Vector2 GetThreeEighthsPointTangent() => _threeEighthsPointTangent;

		public Vector2 GetThreeEighthsPoint()
		{
			ComputeBezierCurve();
			return _threeEighthsPoint;
		}

		public void ClearThreeEighthsPointOffset()
		{
			_threeEighthsPointOffset = Vector2.zero;
			_isDirty = true;
		}

		public void ClearThreeEighthsPointTangent()
		{
			_threeEighthsPointTangent = Vector2.zero;
			_isDirty = true;
		}



		public void SetFiveEighthsPointOffset(Vector2 offset)
		{
			_fiveEighthsPointOffset = offset;
			_isDirty = true;
		}

		public Vector2 GetFiveEighthsPointOffset() => _fiveEighthsPointOffset;

		public void SetFiveEighthsPointTangent(Vector2 tangent)
		{
			_fiveEighthsPointTangent = tangent;
			_isDirty = true;
		}

		public Vector2 GetFiveEighthsPointTangent() => _fiveEighthsPointTangent;

		public Vector2 GetFiveEighthsPoint()
		{
			ComputeBezierCurve();
			return _fiveEighthsPoint;
		}

		public void ClearFiveEighthsPointOffset()
		{
			_fiveEighthsPointOffset = Vector2.zero;
			_isDirty = true;
		}

		public void ClearFiveEighthsPointTangent()
		{
			_fiveEighthsPointTangent = Vector2.zero;
			_isDirty = true;
		}



		public void SetSevenEighthsPointOffset(Vector2 offset)
		{
			_sevenEighthsPointOffset = offset;
			_isDirty = true;
		}

		public Vector2 GetSevenEighthsPointOffset() => _sevenEighthsPointOffset;

		public void SetSevenEighthsPointTangent(Vector2 tangent)
		{
			_sevenEighthsPointTangent = tangent;
			_isDirty = true;
		}

		public Vector2 GetSevenEighthsPointTangent() => _sevenEighthsPointTangent;

		public Vector2 GetSevenEighthsPoint()
		{
			ComputeBezierCurve();
			return _sevenEighthsPoint;
		}

		public void ClearSevenEighthsPointOffset()
		{
			_sevenEighthsPointOffset = Vector2.zero;
			_isDirty = true;
		}

		public void ClearSevenEighthsPointTangent()
		{
			_sevenEighthsPointTangent = Vector2.zero;
			_isDirty = true;
		}


		/// <summary>
		/// Gets the calculated tangent for a point (default + user offset).
		/// </summary>
		public Vector2 GetQuarterPointCalculatedTangent()
		{
			ComputeBezierCurve();
			return GetDefaultTangent(from, _quarterPoint, _midpoint) + _quarterPointTangent;
		}

		public Vector2 GetMidpointCalculatedTangent()
		{
			ComputeBezierCurve();
			return GetDefaultTangent(_quarterPoint, _midpoint, _threeQuarterPoint) + _midpointTangent;
		}

		public Vector2 GetThreeQuarterPointCalculatedTangent()
		{
			ComputeBezierCurve();
			return GetDefaultTangent(_midpoint, _threeQuarterPoint, to) + _threeQuarterPointTangent;
		}


		/// <summary>
		/// Gets the calculated tangent at the eighth point (default + user offset).
		/// </summary>
		public Vector2 GetEighthPointCalculatedTangent()
		{
			ComputeBezierCurve();
			return GetDefaultTangent(from, _eighthPoint, _quarterPoint) + _eighthPointTangent;
		}

		/// <summary>
		/// Gets the calculated tangent at the three-eighths point (default + user offset).
		/// </summary>
		public Vector2 GetThreeEighthsPointCalculatedTangent()
		{
			ComputeBezierCurve();
			return GetDefaultTangent(_quarterPoint, _threeEighthsPoint, _midpoint) + _threeEighthsPointTangent;
		}

		/// <summary>
		/// Gets the calculated tangent at the five-eighths point (default + user offset).
		/// </summary>
		public Vector2 GetFiveEighthsPointCalculatedTangent()
		{
			ComputeBezierCurve();
			return GetDefaultTangent(_midpoint, _fiveEighthsPoint, _threeQuarterPoint) + _fiveEighthsPointTangent;
		}

		/// <summary>
		/// Gets the calculated tangent at the seven-eighths point (default + user offset).
		/// </summary>
		public Vector2 GetSevenEighthsPointCalculatedTangent()
		{
			ComputeBezierCurve();
			return GetDefaultTangent(_threeQuarterPoint, _sevenEighthsPoint, to) + _sevenEighthsPointTangent;
		}


		/// <summary>
		/// Clears all offsets and tangents, reverting to default curve.
		/// </summary>
		public void ClearAllOffsets()
		{
			// Level 1 points
			_quarterPointOffset = Vector2.zero;
			_quarterPointTangent = Vector2.zero;
			_midpointOffset = Vector2.zero;
			_midpointTangent = Vector2.zero;
			_threeQuarterPointOffset = Vector2.zero;
			_threeQuarterPointTangent = Vector2.zero;

			// Level 2 points
			_eighthPointOffset = Vector2.zero;
			_eighthPointTangent = Vector2.zero;
			_threeEighthsPointOffset = Vector2.zero;
			_threeEighthsPointTangent = Vector2.zero;
			_fiveEighthsPointOffset = Vector2.zero;
			_fiveEighthsPointTangent = Vector2.zero;
			_sevenEighthsPointOffset = Vector2.zero;
			_sevenEighthsPointTangent = Vector2.zero;

			_isDirty = true;
		}

		/// <summary>
		/// Whether any control point has a custom offset or tangent applied.
		/// </summary>
		public bool HasCustomOffsets =>
			_quarterPointOffset.sqrMagnitude > 0.01f ||
			_quarterPointTangent.sqrMagnitude > 0.01f ||
			_midpointOffset.sqrMagnitude > 0.01f ||
			_midpointTangent.sqrMagnitude > 0.01f ||
			_threeQuarterPointOffset.sqrMagnitude > 0.01f ||
			_threeQuarterPointTangent.sqrMagnitude > 0.01f ||
			_eighthPointOffset.sqrMagnitude > 0.01f ||
			_eighthPointTangent.sqrMagnitude > 0.01f ||
			_threeEighthsPointOffset.sqrMagnitude > 0.01f ||
			_threeEighthsPointTangent.sqrMagnitude > 0.01f ||
			_fiveEighthsPointOffset.sqrMagnitude > 0.01f ||
			_fiveEighthsPointTangent.sqrMagnitude > 0.01f ||
			_sevenEighthsPointOffset.sqrMagnitude > 0.01f ||
			_sevenEighthsPointTangent.sqrMagnitude > 0.01f;

		/// <summary>
		/// The computed curve points used for rendering and hit detection.
		/// </summary>
		public IReadOnlyList<Vector2> CurvePoints => _curvePoints;
	}
}
