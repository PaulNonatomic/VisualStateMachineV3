using System;
using System.Collections.Generic;
using Nonatomic.VSM.Editor.Debugging;
using Nonatomic.VSM.Editor.Graph.Renderers;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Visual representation of a transition edge between nodes.
	/// Supports multiple rendering styles through the IEdgeRenderer interface.
	/// </summary>
	public class TransitionEdgeView : Edge
	{
		/// <summary>
		/// The underlying connection data for this edge.
		/// </summary>
		public Connection ConnectionData { get; set; }

		/// <summary>
		/// Raised when the connection's curve data is modified by the user.
		/// </summary>
		public event Action<TransitionEdgeView> OnConnectionModified;

		/// <summary>
		/// The current edge renderer being used.
		/// </summary>
		public IEdgeRenderer Renderer => _renderer;

		private IEdgeRenderer _renderer;
		private Label _transitionTimeLabel;
		private VisualElement _warningIcon;
		private WireEdgeControl _wireEdgeControl;
		private VisualElement _handleContainer;
		private VisualElement _hitOverlay;
		private VisualElement _breakpointContainer;
		private BreakpointIndicator _breakpointIndicator;
		private IVisualElementScheduledItem _bpReposition;
		private bool _hasBreakpoint;
		private bool _isBreakpointHit;

		private SplinePointHandle _quarterPointHandle;
		private SplinePointHandle _midpointHandle;
		private SplinePointHandle _threeQuarterPointHandle;
		private SplinePointHandle _eighthPointHandle;
		private SplinePointHandle _threeEighthsPointHandle;
		private SplinePointHandle _fiveEighthsPointHandle;
		private SplinePointHandle _sevenEighthsPointHandle;

		private SplinePointHandle[] _allHandles;

		/// <summary>
		/// Encapsulates the getter/setter accessors for a single spline control point,
		/// allowing the drag, tangent, and reset handlers to be parameterized rather than duplicated.
		/// </summary>
		private struct SplineHandleAccessors
		{
			public Func<Vector2> GetOffset;
			public Action<Vector2> SetOffset;
			public Action<Vector2> SetConnectionOffset;
			public Func<Vector2> GetTangent;
			public Action<Vector2> SetTangent;
			public Action<Vector2> SetConnectionTangent;
			public Action ClearOffset;
			public Action ClearTangent;
			public Action ClearConnectionOffset;
			public Action ClearConnectionTangent;
		}

		protected override EdgeControl CreateEdgeControl()
		{
			_renderer = EdgeRendererFactory.CreateFromSettings();
			var control = _renderer.CreateEdgeControl();

			// Keep reference to WireEdgeControl if that's what we're using
			// This maintains backward compatibility with existing handle/curve code
			_wireEdgeControl = control as WireEdgeControl;

			return control;
		}

		public TransitionEdgeView(Connection connectionData)
		{
			ConnectionData = connectionData;
			AddToClassList("transition-edge");
			BuildEdgeUI();
			UpdateVisuals();
		}

		public TransitionEdgeView()
		{
			AddToClassList("transition-edge");
			BuildEdgeUI();
		}

		/// <summary>
		/// Creates a SplinePointHandle with the given color and wires up the standard
		/// drag, tangent-drag, and double-click callbacks using the provided accessors.
		/// </summary>
		private SplinePointHandle CreateSplineHandle(Color color, SplineHandleAccessors accessors)
		{
			var handle = new SplinePointHandle(color);
			handle.OnPositionDragged += delta => HandlePointDragged(delta, accessors);
			handle.OnTangentDragged += delta => HandleTangentDragged(delta, accessors);
			handle.OnDoubleClicked += () => HandlePointReset(accessors);
			handle.OnHandleSelected += OnSplineHandleSelected;
			handle.OnDragStarted += () => RecordUndo("Edit Edge Curve");
			_handleContainer.Add(handle);
			return handle;
		}

		/// <summary>
		/// Builds the accessors struct for the quarter point, binding to the appropriate
		/// WireEdgeControl and ConnectionData properties.
		/// </summary>
		private SplineHandleAccessors BuildQuarterPointAccessors()
		{
			return new SplineHandleAccessors
			{
				GetOffset = () => _wireEdgeControl.GetQuarterPointOffset(),
				SetOffset = v => _wireEdgeControl.SetQuarterPointOffset(v),
				SetConnectionOffset = v => ConnectionData.QuarterPointOffset = v,
				GetTangent = () => _wireEdgeControl.GetQuarterPointTangent(),
				SetTangent = v => _wireEdgeControl.SetQuarterPointTangent(v),
				SetConnectionTangent = v => ConnectionData.QuarterPointTangent = v,
				ClearOffset = () => _wireEdgeControl?.ClearQuarterPointOffset(),
				ClearTangent = () => _wireEdgeControl?.ClearQuarterPointTangent(),
				ClearConnectionOffset = () => ConnectionData.QuarterPointOffset = null,
				ClearConnectionTangent = () => ConnectionData.QuarterPointTangent = null
			};
		}

		/// <summary>
		/// Builds the accessors struct for the midpoint.
		/// </summary>
		private SplineHandleAccessors BuildMidpointAccessors()
		{
			return new SplineHandleAccessors
			{
				GetOffset = () => _wireEdgeControl.GetMidpointOffset(),
				SetOffset = v => _wireEdgeControl.SetMidpointOffset(v),
				SetConnectionOffset = v => ConnectionData.MidpointOffset = v,
				GetTangent = () => _wireEdgeControl.GetMidpointTangent(),
				SetTangent = v => _wireEdgeControl.SetMidpointTangent(v),
				SetConnectionTangent = v => ConnectionData.MidpointTangent = v,
				ClearOffset = () => _wireEdgeControl?.ClearMidpointOffset(),
				ClearTangent = () => _wireEdgeControl?.ClearMidpointTangent(),
				ClearConnectionOffset = () => ConnectionData.MidpointOffset = null,
				ClearConnectionTangent = () => ConnectionData.MidpointTangent = null
			};
		}

		/// <summary>
		/// Builds the accessors struct for the three-quarter point.
		/// </summary>
		private SplineHandleAccessors BuildThreeQuarterPointAccessors()
		{
			return new SplineHandleAccessors
			{
				GetOffset = () => _wireEdgeControl.GetThreeQuarterPointOffset(),
				SetOffset = v => _wireEdgeControl.SetThreeQuarterPointOffset(v),
				SetConnectionOffset = v => ConnectionData.ThreeQuarterPointOffset = v,
				GetTangent = () => _wireEdgeControl.GetThreeQuarterPointTangent(),
				SetTangent = v => _wireEdgeControl.SetThreeQuarterPointTangent(v),
				SetConnectionTangent = v => ConnectionData.ThreeQuarterPointTangent = v,
				ClearOffset = () => _wireEdgeControl?.ClearThreeQuarterPointOffset(),
				ClearTangent = () => _wireEdgeControl?.ClearThreeQuarterPointTangent(),
				ClearConnectionOffset = () => ConnectionData.ThreeQuarterPointOffset = null,
				ClearConnectionTangent = () => ConnectionData.ThreeQuarterPointTangent = null
			};
		}

		/// <summary>
		/// Builds the accessors struct for the eighth point (1/8).
		/// </summary>
		private SplineHandleAccessors BuildEighthPointAccessors()
		{
			return new SplineHandleAccessors
			{
				GetOffset = () => _wireEdgeControl.GetEighthPointOffset(),
				SetOffset = v => _wireEdgeControl.SetEighthPointOffset(v),
				SetConnectionOffset = v => ConnectionData.EighthPointOffset = v,
				GetTangent = () => _wireEdgeControl.GetEighthPointTangent(),
				SetTangent = v => _wireEdgeControl.SetEighthPointTangent(v),
				SetConnectionTangent = v => ConnectionData.EighthPointTangent = v,
				ClearOffset = () => _wireEdgeControl?.ClearEighthPointOffset(),
				ClearTangent = () => _wireEdgeControl?.ClearEighthPointTangent(),
				ClearConnectionOffset = () => ConnectionData.EighthPointOffset = null,
				ClearConnectionTangent = () => ConnectionData.EighthPointTangent = null
			};
		}

		/// <summary>
		/// Builds the accessors struct for the three-eighths point (3/8).
		/// </summary>
		private SplineHandleAccessors BuildThreeEighthsPointAccessors()
		{
			return new SplineHandleAccessors
			{
				GetOffset = () => _wireEdgeControl.GetThreeEighthsPointOffset(),
				SetOffset = v => _wireEdgeControl.SetThreeEighthsPointOffset(v),
				SetConnectionOffset = v => ConnectionData.ThreeEighthsPointOffset = v,
				GetTangent = () => _wireEdgeControl.GetThreeEighthsPointTangent(),
				SetTangent = v => _wireEdgeControl.SetThreeEighthsPointTangent(v),
				SetConnectionTangent = v => ConnectionData.ThreeEighthsPointTangent = v,
				ClearOffset = () => _wireEdgeControl?.ClearThreeEighthsPointOffset(),
				ClearTangent = () => _wireEdgeControl?.ClearThreeEighthsPointTangent(),
				ClearConnectionOffset = () => ConnectionData.ThreeEighthsPointOffset = null,
				ClearConnectionTangent = () => ConnectionData.ThreeEighthsPointTangent = null
			};
		}

		/// <summary>
		/// Builds the accessors struct for the five-eighths point (5/8).
		/// </summary>
		private SplineHandleAccessors BuildFiveEighthsPointAccessors()
		{
			return new SplineHandleAccessors
			{
				GetOffset = () => _wireEdgeControl.GetFiveEighthsPointOffset(),
				SetOffset = v => _wireEdgeControl.SetFiveEighthsPointOffset(v),
				SetConnectionOffset = v => ConnectionData.FiveEighthsPointOffset = v,
				GetTangent = () => _wireEdgeControl.GetFiveEighthsPointTangent(),
				SetTangent = v => _wireEdgeControl.SetFiveEighthsPointTangent(v),
				SetConnectionTangent = v => ConnectionData.FiveEighthsPointTangent = v,
				ClearOffset = () => _wireEdgeControl?.ClearFiveEighthsPointOffset(),
				ClearTangent = () => _wireEdgeControl?.ClearFiveEighthsPointTangent(),
				ClearConnectionOffset = () => ConnectionData.FiveEighthsPointOffset = null,
				ClearConnectionTangent = () => ConnectionData.FiveEighthsPointTangent = null
			};
		}

		/// <summary>
		/// Builds the accessors struct for the seven-eighths point (7/8).
		/// </summary>
		private SplineHandleAccessors BuildSevenEighthsPointAccessors()
		{
			return new SplineHandleAccessors
			{
				GetOffset = () => _wireEdgeControl.GetSevenEighthsPointOffset(),
				SetOffset = v => _wireEdgeControl.SetSevenEighthsPointOffset(v),
				SetConnectionOffset = v => ConnectionData.SevenEighthsPointOffset = v,
				GetTangent = () => _wireEdgeControl.GetSevenEighthsPointTangent(),
				SetTangent = v => _wireEdgeControl.SetSevenEighthsPointTangent(v),
				SetConnectionTangent = v => ConnectionData.SevenEighthsPointTangent = v,
				ClearOffset = () => _wireEdgeControl?.ClearSevenEighthsPointOffset(),
				ClearTangent = () => _wireEdgeControl?.ClearSevenEighthsPointTangent(),
				ClearConnectionOffset = () => ConnectionData.SevenEighthsPointOffset = null,
				ClearConnectionTangent = () => ConnectionData.SevenEighthsPointTangent = null
			};
		}

		private void BuildEdgeUI()
		{
			// Update visuals when attached to panel (ports will be set by then)
			RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);

			// Container for curve handles
			_handleContainer = new VisualElement();
			_handleContainer.name = "handle-container";
			_handleContainer.style.position = Position.Absolute;
			_handleContainer.style.left = 0;
			_handleContainer.style.top = 0;
			_handleContainer.style.right = 0;
			_handleContainer.style.bottom = 0;
			_handleContainer.pickingMode = PickingMode.Ignore;
			_handleContainer.style.display = DisplayStyle.None;
			Add(_handleContainer);

			// Curve handles are green (with the white outline from SplinePointHandle) so they read
			// as distinct from the red breakpoint dot, which can sit at the same midpoint.
			var level1Color = new Color(0.3f, 0.8f, 0.4f);
			var midpointColor = new Color(0.3f, 0.8f, 0.4f);
			var level2Color = new Color(0.3f, 0.8f, 0.4f);

			_quarterPointHandle = CreateSplineHandle(level1Color, BuildQuarterPointAccessors());
			_midpointHandle = CreateSplineHandle(midpointColor, BuildMidpointAccessors());
			_threeQuarterPointHandle = CreateSplineHandle(level1Color, BuildThreeQuarterPointAccessors());

			// Level 2 handles
			_eighthPointHandle = CreateSplineHandle(level2Color, BuildEighthPointAccessors());
			_threeEighthsPointHandle = CreateSplineHandle(level2Color, BuildThreeEighthsPointAccessors());
			_fiveEighthsPointHandle = CreateSplineHandle(level2Color, BuildFiveEighthsPointAccessors());
			_sevenEighthsPointHandle = CreateSplineHandle(level2Color, BuildSevenEighthsPointAccessors());

			_allHandles = new[]
			{
				_quarterPointHandle,
				_midpointHandle,
				_threeQuarterPointHandle,
				_eighthPointHandle,
				_threeEighthsPointHandle,
				_fiveEighthsPointHandle,
				_sevenEighthsPointHandle
			};

			// Transition time label
			_transitionTimeLabel = new Label();
			_transitionTimeLabel.AddToClassList("transition-time-label");
			_transitionTimeLabel.style.position = Position.Absolute;
			_transitionTimeLabel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
			_transitionTimeLabel.style.color = Color.white;
			_transitionTimeLabel.style.paddingLeft = 4;
			_transitionTimeLabel.style.paddingRight = 4;
			_transitionTimeLabel.style.paddingTop = 2;
			_transitionTimeLabel.style.paddingBottom = 2;
			_transitionTimeLabel.style.borderTopLeftRadius = 4;
			_transitionTimeLabel.style.borderTopRightRadius = 4;
			_transitionTimeLabel.style.borderBottomLeftRadius = 4;
			_transitionTimeLabel.style.borderBottomRightRadius = 4;
			_transitionTimeLabel.style.fontSize = 10;
			_transitionTimeLabel.style.display = DisplayStyle.None;
			Add(_transitionTimeLabel);

			// Warning icon
			_warningIcon = new VisualElement();
			_warningIcon.AddToClassList("transition-warning-icon");
			_warningIcon.style.position = Position.Absolute;
			_warningIcon.style.width = 16;
			_warningIcon.style.height = 16;
			_warningIcon.style.backgroundColor = new Color(1f, 0.6f, 0f);
			_warningIcon.style.borderTopLeftRadius = 8;
			_warningIcon.style.borderTopRightRadius = 8;
			_warningIcon.style.borderBottomLeftRadius = 8;
			_warningIcon.style.borderBottomRightRadius = 8;
			_warningIcon.style.display = DisplayStyle.None;
			Add(_warningIcon);

			// Breakpoint indicator: an always-visible dot drawn on the edge at its midpoint when this
			// transition carries an OnTransition breakpoint. It uses its own container rather than the
			// handle container, which is hidden unless the edge is selected for curve editing.
			_breakpointContainer = new VisualElement();
			_breakpointContainer.name = "breakpoint-container";
			_breakpointContainer.style.position = Position.Absolute;
			_breakpointContainer.style.left = 0;
			_breakpointContainer.style.top = 0;
			_breakpointContainer.style.right = 0;
			_breakpointContainer.style.bottom = 0;
			_breakpointContainer.pickingMode = PickingMode.Ignore;
			Add(_breakpointContainer);
			_breakpointIndicator = new BreakpointIndicator(_breakpointContainer);
			_breakpointIndicator.EnableClick(OnBreakpointDotClicked);

			BreakpointManager.OnBreakpointToggled += HandleBreakpointToggled;
			RegisterCallback<DetachFromPanelEvent>(_ => BreakpointManager.OnBreakpointToggled -= HandleBreakpointToggled);

			RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
			RegisterCallback<MouseDownEvent>(OnEdgeMouseDown);
			RegisterCallback<ContextualMenuPopulateEvent>(OnContextMenuPopulate);
			edgeControl.RegisterCallback<MouseDownEvent>(OnEdgeMouseDown);
			edgeControl.RegisterCallback<ContextualMenuPopulateEvent>(OnContextMenuPopulate);
		}

		/// <summary>
		/// Handles a position drag on a spline control point.
		/// Reads the current offset, applies the zoom-adjusted delta, and writes back to both
		/// the WireEdgeControl and the ConnectionData.
		/// </summary>
		private void HandlePointDragged(Vector2 delta, SplineHandleAccessors accessors)
		{
			if (_wireEdgeControl == null || ConnectionData == null) return;

			delta = GetZoomAdjustedDelta(delta);

			var currentOffset = accessors.GetOffset();
			var newOffset = currentOffset + delta;

			accessors.SetConnectionOffset(newOffset);
			accessors.SetOffset(newOffset);
			_wireEdgeControl.MarkDirty();

			UpdateHandlePositions();

			OnConnectionModified?.Invoke(this);
			MarkDirtyRepaint();
		}

		/// <summary>
		/// Handles a tangent drag on a spline control point.
		/// Reads the current tangent, applies the zoom-adjusted delta, and writes back to both
		/// the WireEdgeControl and the ConnectionData.
		/// </summary>
		private void HandleTangentDragged(Vector2 delta, SplineHandleAccessors accessors)
		{
			if (_wireEdgeControl == null || ConnectionData == null) return;

			delta = GetZoomAdjustedDelta(delta);

			var currentTangent = accessors.GetTangent();
			var newTangent = currentTangent + delta;

			accessors.SetConnectionTangent(newTangent);
			accessors.SetTangent(newTangent);
			_wireEdgeControl.MarkDirty();

			UpdateHandlePositions();

			OnConnectionModified?.Invoke(this);
			MarkDirtyRepaint();
		}

		/// <summary>
		/// Resets a spline control point to its default position by clearing both offset and tangent.
		/// </summary>
		private void HandlePointReset(SplineHandleAccessors accessors)
		{
			RecordUndo("Reset Curve Point");

			if (ConnectionData != null)
			{
				accessors.ClearConnectionOffset();
				accessors.ClearConnectionTangent();
			}

			accessors.ClearOffset();
			accessors.ClearTangent();
			_wireEdgeControl?.MarkDirty();

			UpdateHandlePositions();

			OnConnectionModified?.Invoke(this);
			MarkDirtyRepaint();
		}

		private void OnContextMenuPopulate(ContextualMenuPopulateEvent evt)
		{
			if (ConnectionData == null) return;

			// Breakpoint action applies to transition connections regardless of renderer,
			// so add it before the curve-customization early-return below.
			AppendTransitionBreakpointMenu(evt);

			// Only show curve customization options if the renderer supports it
			if (_renderer?.SupportsCurveCustomization != true) return;

			evt.menu.AppendSeparator();

			var currentLevel = ConnectionData.SubdivisionLevel;

			if (currentLevel < 2)
			{
				evt.menu.AppendAction("Add Control Points", _ => Subdivide(),
					DropdownMenuAction.AlwaysEnabled);
			}

			if (currentLevel > 0)
			{
				evt.menu.AppendAction("Remove Control Points", _ => Simplify(),
					DropdownMenuAction.AlwaysEnabled);
			}

			evt.menu.AppendAction("Reset Curve", _ => ResetCurve(),
				DropdownMenuAction.AlwaysEnabled);
		}

		/// <summary>
		/// Adds an "Add/Remove Transition Breakpoint" action for transition edges. The breakpoint
		/// is stored on the target node with this transition's source port, so it pauses when the
		/// machine enters that node specifically via this edge.
		/// </summary>
		private void AppendTransitionBreakpointMenu(ContextualMenuPopulateEvent evt)
		{
			if (ConnectionData.ConnectionType != ConnectionType.Transition) return;

			var targetNodeId = ConnectionData.TargetNodeId;
			var portName = ConnectionData.SourcePortName;
			var sourceNodeId = ConnectionData.SourceNodeId;
			if (string.IsNullOrEmpty(targetNodeId)) return;

			var graph = GetFirstAncestorOfType<StateMachineGraphView>()?.CurrentAsset;
			if (graph == null) return;

			var existing = graph.GraphViewData.GetTransitionBreakpoint(targetNodeId, sourceNodeId, portName);
			var label = existing != null ? "Remove Transition Breakpoint" : "Add Transition Breakpoint";

			evt.menu.AppendSeparator();
			evt.menu.AppendAction(
				label,
				_ => BreakpointManager.ToggleTransitionBreakpoint(graph, targetNodeId, portName, sourceNodeId),
				DropdownMenuAction.AlwaysEnabled);
		}

		private void HandleBreakpointToggled(StateMachineGraph graph, string nodeId)
		{
			if (ConnectionData == null) return;
			if (graph != GetFirstAncestorOfType<StateMachineGraphView>()?.CurrentAsset) return;
			if (nodeId != null && nodeId != ConnectionData.TargetNodeId) return;
			UpdateBreakpointIndicator();

			// Force an immediate redraw. The UIElements scheduler is throttled while the editor
			// window is idle, so the dot + emphasized wire otherwise only appear once the window
			// next regains focus.
			EditorWindow.focusedWindow?.Repaint();
		}

		/// <summary>
		/// Shows or hides the edge's breakpoint dot depending on whether this transition
		/// (target node + source port) currently carries an OnTransition breakpoint.
		/// </summary>
		private void UpdateBreakpointIndicator()
		{
			if (_breakpointIndicator == null || ConnectionData == null) return;

			if (ConnectionData.ConnectionType != ConnectionType.Transition)
			{
				_breakpointIndicator.Hide();
				return;
			}

			var graph = GetFirstAncestorOfType<StateMachineGraphView>()?.CurrentAsset;
			var breakpoint = graph?.GraphViewData.GetTransitionBreakpoint(
				ConnectionData.TargetNodeId, ConnectionData.SourceNodeId, ConnectionData.SourcePortName);

			// Repaint the wire (bold red) when the breakpoint state changes.
			var wasBreakpoint = _hasBreakpoint;
			_hasBreakpoint = breakpoint != null;
			if (_hasBreakpoint != wasBreakpoint) UpdateVisuals();

			if (breakpoint != null)
			{
				_breakpointIndicator.Show(breakpoint.Enabled, !string.IsNullOrEmpty(breakpoint.Condition));
				UpdateBreakpointIndicatorPosition();

				// The edge's own geometry events don't fire reliably while connected nodes are
				// dragged, so re-anchor to the midpoint on a light schedule while the dot is shown.
				if (_bpReposition == null)
				{
					_bpReposition = schedule.Execute(UpdateBreakpointIndicatorPosition).Every(50);
				}
				else
				{
					_bpReposition.Resume();
				}
			}
			else
			{
				_breakpointIndicator.Hide();
				_bpReposition?.Pause();
			}
		}

		/// <summary>
		/// Places the breakpoint dot on the edge: at the curve midpoint for the wire renderer,
		/// or the edge's centre as a fallback for the default renderer.
		/// </summary>
		private void UpdateBreakpointIndicatorPosition()
		{
			if (_breakpointIndicator == null || !_breakpointIndicator.IsVisible) return;

			if (_wireEdgeControl != null)
			{
				var graphView = GetFirstAncestorOfType<UnityEditor.Experimental.GraphView.GraphView>();
				if (graphView == null) return;

				var world = graphView.contentViewContainer.LocalToWorld(_wireEdgeControl.GetMidpoint());
				var local = _breakpointContainer.WorldToLocal(world);
				_breakpointIndicator.SetCenter(local.x, local.y);
			}
			else
			{
				var rect = _breakpointContainer.contentRect;
				if (float.IsNaN(rect.width)) return;
				_breakpointIndicator.SetCenter(rect.center.x, rect.center.y);
			}
		}

		/// <summary>
		/// Pulses the edge's breakpoint dot when this transition's breakpoint is hit at runtime.
		/// </summary>
		public void FlashBreakpointHit()
		{
			_breakpointIndicator?.Flash();
		}

		/// <summary>
		/// Marks this edge as the transition execution is currently paused on, repainting it a
		/// brighter red so it stands out from any other breakpoint'd edges.
		/// </summary>
		public void SetBreakpointHit(bool hit)
		{
			if (_isBreakpointHit == hit) return;
			_isBreakpointHit = hit;
			UpdateVisuals();
		}

		/// <summary>
		/// Click handler for the edge's breakpoint dot: highlights the breakpoint's row in the panel
		/// and opens its quick actions (enable/disable, remove).
		/// </summary>
		private void OnBreakpointDotClicked(Rect dotWorldBound)
		{
			if (ConnectionData == null) return;

			var graph = GetFirstAncestorOfType<StateMachineGraphView>()?.CurrentAsset;
			var breakpoint = graph?.GraphViewData.GetTransitionBreakpoint(
				ConnectionData.TargetNodeId, ConnectionData.SourceNodeId, ConnectionData.SourcePortName);
			if (breakpoint == null) return;

			BreakpointManager.RequestSelectBreakpoint(graph, breakpoint.Id);

			var menu = new GenericDropdownMenu();
			menu.AddItem(breakpoint.Enabled ? "Disable Breakpoint" : "Enable Breakpoint", false,
				() => BreakpointManager.SetBreakpointEnabled(graph, breakpoint.Id, !breakpoint.Enabled));
			menu.AddItem("Remove Breakpoint", false,
				() => BreakpointManager.RemoveBreakpoint(graph, breakpoint.Id));
			menu.DropDown(dotWorldBound, this);
		}

		private void Subdivide()
		{
			if (ConnectionData == null || _wireEdgeControl == null) return;

			RecordUndo("Add Control Points");

			var currentLevel = ConnectionData.SubdivisionLevel;
			var newLevel = Mathf.Min(currentLevel + 1, 2);

			// Calculate offsets to preserve the current curve shape
			if (currentLevel == 0 && newLevel == 1)
			{
				// Going from midpoint-only to 3 handles
				// Sample the current curve at 1/4 and 3/4 positions
				PreserveShapeForLevel1Subdivision();
			}
			else if (currentLevel == 1 && newLevel == 2)
			{
				// Going from 3 handles to 7 handles
				// Sample the current curve at 1/8, 3/8, 5/8, 7/8 positions
				PreserveShapeForLevel2Subdivision();
			}

			ConnectionData.SubdivisionLevel = newLevel;
			_wireEdgeControl.SetSubdivisionLevel(newLevel);
			_wireEdgeControl.MarkDirty();

			UpdateHandleVisibility();
			UpdateHandlePositions();

			OnConnectionModified?.Invoke(this);
			MarkDirtyRepaint();
		}

		/// <summary>
		/// Calculates offsets for quarter and three-quarter points to preserve curve shape
		/// when subdividing from level 0 to level 1.
		/// </summary>
		private void PreserveShapeForLevel1Subdivision()
		{
			var curvePoints = _wireEdgeControl.CurvePoints;
			if (curvePoints.Count < 4) return;

			var from = curvePoints[0];
			var to = curvePoints[curvePoints.Count - 1];
			var isBackward = to.x < from.x - 20f;

			// Sample curve positions and tangents at t=0.25 and t=0.75
			var quarterIndex = curvePoints.Count / 4;
			var threeQuarterIndex = (curvePoints.Count * 3) / 4;

			var curveQuarterPoint = curvePoints[Mathf.Clamp(quarterIndex, 0, curvePoints.Count - 1)];
			var curveThreeQuarterPoint = curvePoints[Mathf.Clamp(threeQuarterIndex, 0, curvePoints.Count - 1)];

			// Calculate tangents by sampling adjacent points on the curve
			// Level 1 has 4 segments, so each segment is 0.25 of the curve
			var quarterTangent = GetCurveTangentAtIndex(curvePoints, quarterIndex, 0.25f);
			var threeQuarterTangent = GetCurveTangentAtIndex(curvePoints, threeQuarterIndex, 0.25f);

			// Calculate default positions - handle backward connections differently
			Vector2 defaultQuarterPoint;
			Vector2 defaultThreeQuarterPoint;

			if (isBackward)
			{
				// For backward connections, default positions form a loop path
				var loopOffset = WireEdgeSettings.Instance.BackwardLoopOffset;
				var loopBelow = to.y <= from.y + loopOffset;
				var loopY = loopBelow
					? Mathf.Max(from.y, to.y) + loopOffset
					: Mathf.Min(from.y, to.y) - loopOffset;

				defaultQuarterPoint = new Vector2(from.x + 50f, loopY);
				defaultThreeQuarterPoint = new Vector2(to.x - 50f, loopY);
			}
			else
			{
				// For forward connections, default positions are linear interpolation
				defaultQuarterPoint = Vector2.Lerp(from, to, 0.25f);
				defaultThreeQuarterPoint = Vector2.Lerp(from, to, 0.75f);
			}

			// Calculate position offsets
			var quarterOffset = curveQuarterPoint - defaultQuarterPoint;
			var threeQuarterOffset = curveThreeQuarterPoint - defaultThreeQuarterPoint;

			// Apply position offsets
			ConnectionData.QuarterPointOffset = quarterOffset;
			_wireEdgeControl.SetQuarterPointOffset(quarterOffset);

			ConnectionData.ThreeQuarterPointOffset = threeQuarterOffset;
			_wireEdgeControl.SetThreeQuarterPointOffset(threeQuarterOffset);

			// Apply tangents to help preserve curve shape
			ConnectionData.QuarterPointTangent = quarterTangent;
			_wireEdgeControl.SetQuarterPointTangent(quarterTangent);

			ConnectionData.ThreeQuarterPointTangent = threeQuarterTangent;
			_wireEdgeControl.SetThreeQuarterPointTangent(threeQuarterTangent);
		}

		/// <summary>
		/// Calculates the tangent at a given index on the curve by sampling adjacent points.
		/// The tangent length is based on the distance to neighboring control points.
		/// </summary>
		private Vector2 GetCurveTangentAtIndex(IReadOnlyList<Vector2> curvePoints, int index, float segmentFraction)
		{
			// Sample points before and after to calculate tangent direction
			var sampleRange = Mathf.Max(2, curvePoints.Count / 32);
			var prevIndex = Mathf.Max(0, index - sampleRange);
			var nextIndex = Mathf.Min(curvePoints.Count - 1, index + sampleRange);

			var prevPoint = curvePoints[prevIndex];
			var currentPoint = curvePoints[index];
			var nextPoint = curvePoints[nextIndex];

			// Calculate direction along the curve
			var direction = (nextPoint - prevPoint).normalized;

			// Calculate tangent length based on distance to neighboring points on curve
			// Use the segment fraction to estimate appropriate tangent length
			var totalLength = 0f;
			for (var i = 0; i < curvePoints.Count - 1; i++)
			{
				totalLength += Vector2.Distance(curvePoints[i], curvePoints[i + 1]);
			}

			// Tangent length should be proportional to segment length
			// segmentFraction is the fraction of the curve this segment represents (e.g., 0.25 for Level 1)
			var segmentLength = totalLength * segmentFraction;
			var tangentLength = segmentLength * 0.35f; // ~35% of segment length for smooth curves

			return direction * tangentLength;
		}

		/// <summary>
		/// Calculates offsets for eighth points to preserve curve shape
		/// when subdividing from level 1 to level 2.
		/// </summary>
		private void PreserveShapeForLevel2Subdivision()
		{
			var curvePoints = _wireEdgeControl.CurvePoints;
			if (curvePoints.Count < 8) return;

			var from = curvePoints[0];
			var to = curvePoints[curvePoints.Count - 1];
			var isBackward = to.x < from.x - 20f;

			// Sample curve positions at t=0.125, 0.375, 0.625, 0.875
			var eighthIndex = curvePoints.Count / 8;
			var threeEighthsIndex = (curvePoints.Count * 3) / 8;
			var fiveEighthsIndex = (curvePoints.Count * 5) / 8;
			var sevenEighthsIndex = (curvePoints.Count * 7) / 8;

			var curveEighthPoint = curvePoints[Mathf.Clamp(eighthIndex, 0, curvePoints.Count - 1)];
			var curveThreeEighthsPoint = curvePoints[Mathf.Clamp(threeEighthsIndex, 0, curvePoints.Count - 1)];
			var curveFiveEighthsPoint = curvePoints[Mathf.Clamp(fiveEighthsIndex, 0, curvePoints.Count - 1)];
			var curveSevenEighthsPoint = curvePoints[Mathf.Clamp(sevenEighthsIndex, 0, curvePoints.Count - 1)];

			// Calculate tangents by sampling adjacent points on the curve
			// Level 2 has 8 segments, so each segment is 0.125 of the curve
			var eighthTangent = GetCurveTangentAtIndex(curvePoints, eighthIndex, 0.125f);
			var threeEighthsTangent = GetCurveTangentAtIndex(curvePoints, threeEighthsIndex, 0.125f);
			var fiveEighthsTangent = GetCurveTangentAtIndex(curvePoints, fiveEighthsIndex, 0.125f);
			var sevenEighthsTangent = GetCurveTangentAtIndex(curvePoints, sevenEighthsIndex, 0.125f);

			// Calculate default positions - handle backward connections differently
			Vector2 defaultEighthPoint;
			Vector2 defaultThreeEighthsPoint;
			Vector2 defaultFiveEighthsPoint;
			Vector2 defaultSevenEighthsPoint;
			if (isBackward)
			{
				// For backward connections, first calculate Level 1 default positions
				var loopOffset = WireEdgeSettings.Instance.BackwardLoopOffset;
				var loopBelow = to.y <= from.y + loopOffset;
				var loopY = loopBelow
					? Mathf.Max(from.y, to.y) + loopOffset
					: Mathf.Min(from.y, to.y) - loopOffset;

				// Level 1 positions with existing offsets
				var quarterPoint = new Vector2(from.x + 50f, loopY) +
								   (ConnectionData.QuarterPointOffset ?? Vector2.zero);
				var midpoint = new Vector2((from.x + to.x) / 2f, loopY) +
							   (ConnectionData.MidpointOffset ?? Vector2.zero);
				var threeQuarterPoint = new Vector2(to.x - 50f, loopY) +
										(ConnectionData.ThreeQuarterPointOffset ?? Vector2.zero);

				// Level 2 defaults are midpoints between Level 1 points
				defaultEighthPoint = Vector2.Lerp(from, quarterPoint, 0.5f);
				defaultThreeEighthsPoint = Vector2.Lerp(quarterPoint, midpoint, 0.5f);
				defaultFiveEighthsPoint = Vector2.Lerp(midpoint, threeQuarterPoint, 0.5f);
				defaultSevenEighthsPoint = Vector2.Lerp(threeQuarterPoint, to, 0.5f);
			}
			else
			{
				// For forward connections, calculate Level 1 positions with existing offsets
				var quarterPoint = Vector2.Lerp(from, to, 0.25f) +
								   (ConnectionData.QuarterPointOffset ?? Vector2.zero);
				var midpoint = Vector2.Lerp(from, to, 0.5f) +
							   (ConnectionData.MidpointOffset ?? Vector2.zero);
				var threeQuarterPoint = Vector2.Lerp(from, to, 0.75f) +
										(ConnectionData.ThreeQuarterPointOffset ?? Vector2.zero);

				// Level 2 defaults are midpoints between Level 1 points
				defaultEighthPoint = Vector2.Lerp(from, quarterPoint, 0.5f);
				defaultThreeEighthsPoint = Vector2.Lerp(quarterPoint, midpoint, 0.5f);
				defaultFiveEighthsPoint = Vector2.Lerp(midpoint, threeQuarterPoint, 0.5f);
				defaultSevenEighthsPoint = Vector2.Lerp(threeQuarterPoint, to, 0.5f);
			}

			// Calculate position offsets
			var eighthOffset = curveEighthPoint - defaultEighthPoint;
			var threeEighthsOffset = curveThreeEighthsPoint - defaultThreeEighthsPoint;
			var fiveEighthsOffset = curveFiveEighthsPoint - defaultFiveEighthsPoint;
			var sevenEighthsOffset = curveSevenEighthsPoint - defaultSevenEighthsPoint;

			// Apply position offsets
			ConnectionData.EighthPointOffset = eighthOffset;
			_wireEdgeControl.SetEighthPointOffset(eighthOffset);

			ConnectionData.ThreeEighthsPointOffset = threeEighthsOffset;
			_wireEdgeControl.SetThreeEighthsPointOffset(threeEighthsOffset);

			ConnectionData.FiveEighthsPointOffset = fiveEighthsOffset;
			_wireEdgeControl.SetFiveEighthsPointOffset(fiveEighthsOffset);

			ConnectionData.SevenEighthsPointOffset = sevenEighthsOffset;
			_wireEdgeControl.SetSevenEighthsPointOffset(sevenEighthsOffset);

			// Apply tangents to help preserve curve shape
			ConnectionData.EighthPointTangent = eighthTangent;
			_wireEdgeControl.SetEighthPointTangent(eighthTangent);

			ConnectionData.ThreeEighthsPointTangent = threeEighthsTangent;
			_wireEdgeControl.SetThreeEighthsPointTangent(threeEighthsTangent);

			ConnectionData.FiveEighthsPointTangent = fiveEighthsTangent;
			_wireEdgeControl.SetFiveEighthsPointTangent(fiveEighthsTangent);

			ConnectionData.SevenEighthsPointTangent = sevenEighthsTangent;
			_wireEdgeControl.SetSevenEighthsPointTangent(sevenEighthsTangent);
		}

		private void Simplify()
		{
			if (ConnectionData == null) return;

			RecordUndo("Remove Control Points");

			var currentLevel = ConnectionData.SubdivisionLevel;
			var newLevel = Mathf.Max(currentLevel - 1, 0);

			// Clear offsets for the level being removed
			if (currentLevel == 2)
			{
				// Simplifying from level 2 to level 1 - clear level 2 offsets
				ConnectionData.EighthPointOffset = null;
				ConnectionData.EighthPointTangent = null;
				ConnectionData.ThreeEighthsPointOffset = null;
				ConnectionData.ThreeEighthsPointTangent = null;
				ConnectionData.FiveEighthsPointOffset = null;
				ConnectionData.FiveEighthsPointTangent = null;
				ConnectionData.SevenEighthsPointOffset = null;
				ConnectionData.SevenEighthsPointTangent = null;

				_wireEdgeControl?.ClearEighthPointOffset();
				_wireEdgeControl?.ClearEighthPointTangent();
				_wireEdgeControl?.ClearThreeEighthsPointOffset();
				_wireEdgeControl?.ClearThreeEighthsPointTangent();
				_wireEdgeControl?.ClearFiveEighthsPointOffset();
				_wireEdgeControl?.ClearFiveEighthsPointTangent();
				_wireEdgeControl?.ClearSevenEighthsPointOffset();
				_wireEdgeControl?.ClearSevenEighthsPointTangent();

				// Deselect level 2 handles
				_eighthPointHandle?.Deselect();
				_threeEighthsPointHandle?.Deselect();
				_fiveEighthsPointHandle?.Deselect();
				_sevenEighthsPointHandle?.Deselect();
			}
			else if (currentLevel == 1)
			{
				// Simplifying from level 1 to level 0 - clear level 1 offsets
				ConnectionData.QuarterPointOffset = null;
				ConnectionData.QuarterPointTangent = null;
				ConnectionData.ThreeQuarterPointOffset = null;
				ConnectionData.ThreeQuarterPointTangent = null;

				_wireEdgeControl?.ClearQuarterPointOffset();
				_wireEdgeControl?.ClearQuarterPointTangent();
				_wireEdgeControl?.ClearThreeQuarterPointOffset();
				_wireEdgeControl?.ClearThreeQuarterPointTangent();

				// Deselect level 1 handles
				_quarterPointHandle?.Deselect();
				_threeQuarterPointHandle?.Deselect();
			}

			ConnectionData.SubdivisionLevel = newLevel;
			_wireEdgeControl?.SetSubdivisionLevel(newLevel);
			_wireEdgeControl?.MarkDirty();

			UpdateHandleVisibility();
			UpdateHandlePositions();

			OnConnectionModified?.Invoke(this);
			MarkDirtyRepaint();
		}

		private void ResetCurve()
		{
			if (ConnectionData == null) return;

			RecordUndo("Reset Curve");

			// Clear all offsets and tangents - Level 1
			ConnectionData.QuarterPointOffset = null;
			ConnectionData.QuarterPointTangent = null;
			ConnectionData.MidpointOffset = null;
			ConnectionData.MidpointTangent = null;
			ConnectionData.ThreeQuarterPointOffset = null;
			ConnectionData.ThreeQuarterPointTangent = null;

			// Clear Level 2 offsets
			ConnectionData.EighthPointOffset = null;
			ConnectionData.EighthPointTangent = null;
			ConnectionData.ThreeEighthsPointOffset = null;
			ConnectionData.ThreeEighthsPointTangent = null;
			ConnectionData.FiveEighthsPointOffset = null;
			ConnectionData.FiveEighthsPointTangent = null;
			ConnectionData.SevenEighthsPointOffset = null;
			ConnectionData.SevenEighthsPointTangent = null;

			ConnectionData.SubdivisionLevel = 0;

			_wireEdgeControl?.ClearAllOffsets();
			_wireEdgeControl?.SetSubdivisionLevel(0);
			_wireEdgeControl?.MarkDirty();

			// Deselect all handles
			DeselectAllHandles();

			UpdateHandleVisibility();
			UpdateHandlePositions();

			OnConnectionModified?.Invoke(this);
			MarkDirtyRepaint();
		}

		private void OnAttachedToPanel(AttachToPanelEvent evt)
		{
			// Ports are now connected, update visuals to get correct colors
			UpdateVisuals();
			_wireEdgeControl?.MarkDirty();
			MarkDirtyRepaint();
			UpdateBreakpointIndicator();
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			_wireEdgeControl?.MarkDirty();
			UpdateHandlePositions();
			UpdateBreakpointIndicatorPosition();
		}

		/// <summary>
		/// Forces the edge control to recalculate its geometry and update handle positions.
		/// Call this when connected nodes have moved.
		/// </summary>
		public void UpdateEdgeControl()
		{
			_wireEdgeControl?.MarkDirty();
			UpdateHandlePositions();
			UpdateBreakpointIndicatorPosition();
			MarkDirtyRepaint();
		}

		private void OnEdgeMouseDown(MouseDownEvent evt)
		{
			if (evt.button != 0) return;

			if (evt.clickCount == 1)
			{
				var graphView = GetFirstAncestorOfType<UnityEditor.Experimental.GraphView.GraphView>();
				if (graphView == null) return;

				if (evt.shiftKey || evt.ctrlKey)
				{
					if (selected)
						graphView.RemoveFromSelection(this);
					else
						graphView.AddToSelection(this);
				}
				else
				{
					graphView.ClearSelection();
					graphView.AddToSelection(this);
				}

				evt.StopPropagation();
			}
		}

		/// <summary>
		/// Updates the visual appearance based on the current connection data.
		/// </summary>
		public void UpdateVisuals()
		{
			if (ConnectionData == null) return;

			// Use the renderer to apply connection data (handles offset/tangent sync)
			_renderer?.ApplyConnectionData(edgeControl, ConnectionData);

			if (ConnectionData.TransitionTime > 0)
			{
				_transitionTimeLabel.text = $"{ConnectionData.TransitionTime:F2}s";
				_transitionTimeLabel.style.display = DisplayStyle.Flex;
			}
			else
			{
				_transitionTimeLabel.style.display = DisplayStyle.None;
			}

			// Get colors from connected ports for gradient effect
			var outputColor = output?.portColor ?? new Color(0.8f, 0.8f, 0.8f);
			var inputColor = input?.portColor ?? new Color(0.8f, 0.8f, 0.8f);

			// Override with data-specific colors if this is a data connection
			if (ConnectionData.ConnectionType == ConnectionType.Data ||
				ConnectionData.ConnectionType == ConnectionType.BlackboardRead ||
				ConnectionData.ConnectionType == ConnectionType.BlackboardWrite)
			{
				var dataColor = new Color(0.4f, 0.7f, 1f);
				outputColor = dataColor;
				inputColor = dataColor;
			}
			// For transitions, if the source port declares an explicit [Transition(Color=...)]
			// hint, paint the whole edge with that color. Without a hint we keep the default
			// gradient derived from the connected ports above.
			else if (output?.node is StateNodeView sourceNode
				&& output.userData is string sourcePortName
				&& sourceNode.TryGetTransitionPortColor(sourcePortName, out var transitionColor))
			{
				outputColor = transitionColor;
				inputColor = transitionColor;
			}

			// A breakpoint'd transition is painted bold red so it stands out from the handles; the
			// specific transition execution is paused on is painted a brighter red to single it out.
			if (_hasBreakpoint)
			{
				var breakpointRed = _isBreakpointHit
					? new Color(1f, 0.5f, 0.35f)
					: new Color(0.9f, 0.15f, 0.15f);
				inputColor = breakpointRed;
				outputColor = breakpointRed;
			}

			_wireEdgeControl?.SetEmphasized(_hasBreakpoint);

			if (_wireEdgeControl != null)
			{
				_wireEdgeControl.SetWireColors(inputColor, outputColor);
			}
			else
			{
				edgeControl.inputColor = inputColor;
				edgeControl.outputColor = outputColor;
			}
		}

		/// <summary>
		/// Positions a single handle at the given spline point using graph-to-local coordinate conversion.
		/// </summary>
		private void PositionHandle(
			SplinePointHandle handle,
			Vector2 point,
			Vector2 tangent,
			VisualElement contentViewContainer)
		{
			var worldPoint = contentViewContainer.LocalToWorld(point);
			var localPoint = _handleContainer.WorldToLocal(worldPoint);
			handle.SetPositionAndTangent(localPoint, tangent);
		}

		private void UpdateHandlePositions()
		{
			if (_wireEdgeControl == null || edgeControl == null) return;

			var graphView = GetFirstAncestorOfType<UnityEditor.Experimental.GraphView.GraphView>();
			if (graphView == null) return;

			var container = graphView.contentViewContainer;

			// Level 1 handles
			PositionHandle(_quarterPointHandle,
				_wireEdgeControl.GetQuarterPoint(),
				_wireEdgeControl.GetQuarterPointCalculatedTangent(),
				container);

			PositionHandle(_midpointHandle,
				_wireEdgeControl.GetMidpoint(),
				_wireEdgeControl.GetMidpointCalculatedTangent(),
				container);

			PositionHandle(_threeQuarterPointHandle,
				_wireEdgeControl.GetThreeQuarterPoint(),
				_wireEdgeControl.GetThreeQuarterPointCalculatedTangent(),
				container);

			// Level 2 handles
			PositionHandle(_eighthPointHandle,
				_wireEdgeControl.GetEighthPoint(),
				_wireEdgeControl.GetEighthPointCalculatedTangent(),
				container);

			PositionHandle(_threeEighthsPointHandle,
				_wireEdgeControl.GetThreeEighthsPoint(),
				_wireEdgeControl.GetThreeEighthsPointCalculatedTangent(),
				container);

			PositionHandle(_fiveEighthsPointHandle,
				_wireEdgeControl.GetFiveEighthsPoint(),
				_wireEdgeControl.GetFiveEighthsPointCalculatedTangent(),
				container);

			PositionHandle(_sevenEighthsPointHandle,
				_wireEdgeControl.GetSevenEighthsPoint(),
				_wireEdgeControl.GetSevenEighthsPointCalculatedTangent(),
				container);
		}

		private Vector2 GetZoomAdjustedDelta(Vector2 delta)
		{
			var graphView = GetFirstAncestorOfType<UnityEditor.Experimental.GraphView.GraphView>();
			if (graphView != null)
			{
				var zoom = graphView.contentViewContainer.resolvedStyle.scale.value.x;
				delta /= zoom;
			}

			return delta;
		}

		private void OnSplineHandleSelected(SplinePointHandle selectedHandle)
		{
			// Deselect all other handles when one is selected
			foreach (var handle in _allHandles)
			{
				if (handle != selectedHandle)
					handle.Deselect();
			}
		}

		/// <summary>
		/// Deselects all spline handles, hiding their tangent controls.
		/// </summary>
		private void DeselectAllHandles()
		{
			foreach (var handle in _allHandles)
			{
				handle?.Deselect();
			}
		}

		private void RecordUndo(string operation)
		{
			var graphView = GetFirstAncestorOfType<StateMachineGraphView>();
			var asset = graphView?.CurrentAsset;
			if (asset != null)
			{
				Undo.RecordObject(asset, operation);
			}
		}

		/// <summary>
		/// Shows or hides the curve control handles.
		/// Only shows handles if the current renderer supports curve customization.
		/// </summary>
		public void SetHandleVisible(bool visible)
		{
			// Only show handles if renderer supports curve customization
			var shouldShow = visible && (_renderer?.SupportsCurveCustomization ?? false);
			_handleContainer.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;

			if (shouldShow)
			{
				UpdateHandleVisibility();
				UpdateHandlePositions();
			}
		}

		private void UpdateHandleVisibility()
		{
			var level = ConnectionData?.SubdivisionLevel ?? 0;

			// Midpoint handle is always visible when edge is selected
			_midpointHandle.style.display = DisplayStyle.Flex;

			// Level 1 handles - Quarter and three-quarter handles visible at subdivision level 1+
			var showLevel1Handles = level >= 1 ? DisplayStyle.Flex : DisplayStyle.None;
			_quarterPointHandle.style.display = showLevel1Handles;
			_threeQuarterPointHandle.style.display = showLevel1Handles;

			// Level 2 handles - Eighth points only visible at subdivision level 2
			var showLevel2Handles = level >= 2 ? DisplayStyle.Flex : DisplayStyle.None;
			_eighthPointHandle.style.display = showLevel2Handles;
			_threeEighthsPointHandle.style.display = showLevel2Handles;
			_fiveEighthsPointHandle.style.display = showLevel2Handles;
			_sevenEighthsPointHandle.style.display = showLevel2Handles;
		}

		/// <summary>
		/// Displays a warning icon with the specified message.
		/// </summary>
		public void ShowLoopWarning(string message)
		{
			_warningIcon.style.display = DisplayStyle.Flex;
			_warningIcon.tooltip = message;
			AddToClassList("has-warning");
		}

		/// <summary>
		/// Clears any displayed warning.
		/// </summary>
		public void ClearWarning()
		{
			_warningIcon.style.display = DisplayStyle.None;
			RemoveFromClassList("has-warning");
		}

		/// <summary>
		/// Sets the visual state for an active transition.
		/// </summary>
		public void SetTransitioning(bool isTransitioning, float progress = 0f)
		{
			if (isTransitioning)
			{
				AddToClassList("transitioning");
				var transitionColor = new Color(0.2f, 0.8f, 0.2f);
				if (_wireEdgeControl != null)
				{
					_wireEdgeControl.SetWireColors(transitionColor, transitionColor);
				}
				else
				{
					edgeControl.inputColor = transitionColor;
					edgeControl.outputColor = transitionColor;
				}
			}
			else
			{
				RemoveFromClassList("transitioning");
				UpdateVisuals();
			}
		}

		public override bool ContainsPoint(Vector2 localPoint)
		{
			// First check the default behavior
			if (base.ContainsPoint(localPoint))
				return true;

			// If renderer provides curve points, do extended hit testing for curved parts
			if (_renderer != null)
			{
				var curvePoints = _renderer.GetCurvePoints(edgeControl);
				if (curvePoints == null || curvePoints.Count < 2)
					return false;

				// Convert local point to graph coordinates
				var graphView = GetFirstAncestorOfType<UnityEditor.Experimental.GraphView.GraphView>();
				if (graphView == null) return false;

				// Transform local point to world, then to graph content coordinates
				var worldPoint = this.LocalToWorld(localPoint);
				var graphPoint = graphView.contentViewContainer.WorldToLocal(worldPoint);

				// Check distance to each curve segment
				var hitDistance = 15f;
				for (var i = 0; i < curvePoints.Count - 1; i++)
				{
					var distance = DistanceToSegment(graphPoint, curvePoints[i], curvePoints[i + 1]);
					if (distance <= hitDistance)
						return true;
				}
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

		public override void OnSelected()
		{
			base.OnSelected();
			UpdateHandleVisibilityBasedOnSelection();
			UpdateSelectionVisuals(true);
		}

		/// <summary>
		/// Updates handle visibility based on the current selection.
		/// Handles are only shown when this edge is the only selected element.
		/// </summary>
		public void UpdateHandleVisibilityBasedOnSelection()
		{
			var graphView = GetFirstAncestorOfType<UnityEditor.Experimental.GraphView.GraphView>();
			if (graphView == null)
			{
				SetHandleVisible(false);
				return;
			}

			// Only show handles if this edge is the sole selected element
			var selection = graphView.selection;
			var showHandles = selection.Count == 1 && selection[0] == this;
			SetHandleVisible(showHandles);
		}

		public override void OnUnselected()
		{
			base.OnUnselected();
			SetHandleVisible(false);
			UpdateSelectionVisuals(false);

			// Hide all tangent handles
			DeselectAllHandles();
		}

		private void UpdateSelectionVisuals(bool isSelected)
		{
			if (isSelected)
			{
				var selectedColor = new Color(0.27f, 0.67f, 1f);
				if (_wireEdgeControl != null)
				{
					_wireEdgeControl.SetWireColors(selectedColor, selectedColor);
				}
				else
				{
					edgeControl.inputColor = selectedColor;
					edgeControl.outputColor = selectedColor;
				}
			}
			else
			{
				UpdateVisuals();
			}
		}
	}

	/// <summary>
	/// Spline point handle with main position control and tangent handles.
	/// Similar to bezier curve editors in vector graphics tools.
	/// </summary>
	public class SplinePointHandle : VisualElement
	{
		/// <summary>Raised when the main handle is dragged, providing the delta movement.</summary>
		public event Action<Vector2> OnPositionDragged;

		/// <summary>Raised when a tangent handle is dragged, providing the delta movement.</summary>
		public event Action<Vector2> OnTangentDragged;

		/// <summary>Raised when the main handle is double-clicked.</summary>
		public event Action OnDoubleClicked;

		/// <summary>Raised when this handle is selected (clicked).</summary>
		public event Action<SplinePointHandle> OnHandleSelected;

		/// <summary>Raised when a drag operation begins (position or tangent).</summary>
		public event Action OnDragStarted;

		private readonly VisualElement _mainHandle;
		private readonly VisualElement _tangentHandleIn;
		private readonly VisualElement _tangentHandleOut;
		private readonly VisualElement _tangentLineIn;
		private readonly VisualElement _tangentLineOut;

		private bool _isDraggingPosition;
		private bool _isDraggingTangent;
		private bool _isSelected;
		private Vector2 _lastMousePos;
		private Vector2 _currentPosition;
		private Vector2 _currentTangent;

		private const float MainHandleSize = 12f;
		private const float TangentHandleSize = 8f;
		private const float TangentLineWidth = 1f;

		/// <summary>
		/// Whether this handle is currently selected (showing tangent handles).
		/// </summary>
		public bool IsSelected => _isSelected;

		public SplinePointHandle(Color color)
		{
			style.position = Position.Absolute;
			pickingMode = PickingMode.Ignore;

			// Tangent line (incoming direction) - drawn first so it's behind handles
			_tangentLineIn = new VisualElement();
			_tangentLineIn.style.position = Position.Absolute;
			_tangentLineIn.style.height = TangentLineWidth;
			_tangentLineIn.style.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
			_tangentLineIn.style.transformOrigin = new TransformOrigin(0, Length.Percent(50));
			_tangentLineIn.pickingMode = PickingMode.Ignore;
			_tangentLineIn.style.display = DisplayStyle.None; // Hidden by default
			Add(_tangentLineIn);

			// Tangent line (outgoing direction)
			_tangentLineOut = new VisualElement();
			_tangentLineOut.style.position = Position.Absolute;
			_tangentLineOut.style.height = TangentLineWidth;
			_tangentLineOut.style.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
			_tangentLineOut.style.transformOrigin = new TransformOrigin(0, Length.Percent(50));
			_tangentLineOut.pickingMode = PickingMode.Ignore;
			_tangentLineOut.style.display = DisplayStyle.None; // Hidden by default
			Add(_tangentLineOut);

			// Main position handle (center)
			_mainHandle = new VisualElement();
			_mainHandle.style.position = Position.Absolute;
			_mainHandle.style.width = MainHandleSize;
			_mainHandle.style.height = MainHandleSize;
			_mainHandle.style.borderTopLeftRadius = MainHandleSize / 2;
			_mainHandle.style.borderTopRightRadius = MainHandleSize / 2;
			_mainHandle.style.borderBottomLeftRadius = MainHandleSize / 2;
			_mainHandle.style.borderBottomRightRadius = MainHandleSize / 2;
			_mainHandle.style.backgroundColor = color;
			_mainHandle.style.borderTopWidth = 2;
			_mainHandle.style.borderBottomWidth = 2;
			_mainHandle.style.borderLeftWidth = 2;
			_mainHandle.style.borderRightWidth = 2;
			_mainHandle.style.borderTopColor = Color.white;
			_mainHandle.style.borderBottomColor = Color.white;
			_mainHandle.style.borderLeftColor = Color.white;
			_mainHandle.style.borderRightColor = Color.white;
			_mainHandle.pickingMode = PickingMode.Position;
			_mainHandle.focusable = true;
			_mainHandle.tooltip = "Click to show tangent handles\nDrag to move point\nDouble-click to reset";
			_mainHandle.RegisterCallback<MouseDownEvent>(OnMainHandleMouseDown);
			_mainHandle.RegisterCallback<MouseMoveEvent>(OnMainHandleMouseMove);
			_mainHandle.RegisterCallback<MouseUpEvent>(OnMainHandleMouseUp);
			_mainHandle.RegisterCallback<MouseCaptureOutEvent>(OnMainHandleMouseCaptureOut);
			Add(_mainHandle);

			// Tangent handle (incoming direction - opposite of tangent)
			_tangentHandleIn = CreateTangentHandle(new Color(0.3f, 0.8f, 0.4f));
			_tangentHandleIn.style.display = DisplayStyle.None; // Hidden by default
			Add(_tangentHandleIn);

			// Tangent handle (outgoing direction - along tangent)
			_tangentHandleOut = CreateTangentHandle(new Color(0.3f, 0.8f, 0.4f));
			_tangentHandleOut.style.display = DisplayStyle.None; // Hidden by default
			Add(_tangentHandleOut);
		}

		/// <summary>
		/// Sets the selection state of this handle, showing or hiding tangent controls.
		/// </summary>
		public void SetSelected(bool selected)
		{
			_isSelected = selected;
			var display = selected ? DisplayStyle.Flex : DisplayStyle.None;
			_tangentLineIn.style.display = display;
			_tangentLineOut.style.display = display;
			_tangentHandleIn.style.display = display;
			_tangentHandleOut.style.display = display;
		}

		/// <summary>
		/// Deselects this handle, hiding tangent controls.
		/// </summary>
		public void Deselect()
		{
			SetSelected(false);
		}

		private VisualElement CreateTangentHandle(Color color)
		{
			var handle = new VisualElement();
			handle.style.position = Position.Absolute;
			handle.style.width = TangentHandleSize;
			handle.style.height = TangentHandleSize;
			handle.style.borderTopLeftRadius = TangentHandleSize / 2;
			handle.style.borderTopRightRadius = TangentHandleSize / 2;
			handle.style.borderBottomLeftRadius = TangentHandleSize / 2;
			handle.style.borderBottomRightRadius = TangentHandleSize / 2;
			handle.style.backgroundColor = color;
			handle.style.borderTopWidth = 1;
			handle.style.borderBottomWidth = 1;
			handle.style.borderLeftWidth = 1;
			handle.style.borderRightWidth = 1;
			handle.style.borderTopColor = Color.white;
			handle.style.borderBottomColor = Color.white;
			handle.style.borderLeftColor = Color.white;
			handle.style.borderRightColor = Color.white;
			handle.pickingMode = PickingMode.Position;
			handle.focusable = true;
			handle.tooltip = "Drag to adjust curve tangent";
			handle.RegisterCallback<MouseDownEvent>(OnTangentHandleMouseDown);
			handle.RegisterCallback<MouseMoveEvent>(OnTangentHandleMouseMove);
			handle.RegisterCallback<MouseUpEvent>(OnTangentHandleMouseUp);
			handle.RegisterCallback<MouseCaptureOutEvent>(OnTangentHandleMouseCaptureOut);
			return handle;
		}

		/// <summary>
		/// Updates the position and tangent of this handle.
		/// </summary>
		public void SetPositionAndTangent(Vector2 position, Vector2 tangent)
		{
			_currentPosition = position;
			_currentTangent = tangent;

			// Position main handle at center
			_mainHandle.style.left = position.x - MainHandleSize / 2;
			_mainHandle.style.top = position.y - MainHandleSize / 2;

			// Position tangent handles along the tangent direction
			var tangentOutPos = position + tangent;
			var tangentInPos = position - tangent;

			_tangentHandleOut.style.left = tangentOutPos.x - TangentHandleSize / 2;
			_tangentHandleOut.style.top = tangentOutPos.y - TangentHandleSize / 2;

			_tangentHandleIn.style.left = tangentInPos.x - TangentHandleSize / 2;
			_tangentHandleIn.style.top = tangentInPos.y - TangentHandleSize / 2;

			// Update tangent lines
			UpdateTangentLine(_tangentLineOut, position, tangentOutPos);
			UpdateTangentLine(_tangentLineIn, position, tangentInPos);
		}

		private void UpdateTangentLine(VisualElement line, Vector2 from, Vector2 to)
		{
			var delta = to - from;
			var length = delta.magnitude;
			var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

			line.style.left = from.x;
			line.style.top = from.y - TangentLineWidth / 2;
			line.style.width = length;
			line.style.rotate = new Rotate(angle);
		}

		// Main handle events
		private void OnMainHandleMouseDown(MouseDownEvent evt)
		{
			if (evt.button != 0) return;

			if (evt.clickCount == 2)
			{
				OnDoubleClicked?.Invoke();
				evt.StopPropagation();
				return;
			}

			// Show tangent handles and notify parent
			SetSelected(true);
			OnHandleSelected?.Invoke(this);
			OnDragStarted?.Invoke();

			_isDraggingPosition = true;
			_lastMousePos = evt.mousePosition;
			_mainHandle.CaptureMouse();
			_mainHandle.Focus();
			evt.StopPropagation();
		}

		private void OnMainHandleMouseMove(MouseMoveEvent evt)
		{
			if (!_isDraggingPosition) return;

			var delta = evt.mousePosition - _lastMousePos;
			_lastMousePos = evt.mousePosition;

			OnPositionDragged?.Invoke(delta);
			evt.StopPropagation();
		}

		private void OnMainHandleMouseUp(MouseUpEvent evt)
		{
			if (!_isDraggingPosition) return;

			_isDraggingPosition = false;
			_mainHandle.ReleaseMouse();
			evt.StopPropagation();
		}

		private void OnMainHandleMouseCaptureOut(MouseCaptureOutEvent evt)
		{
			_isDraggingPosition = false;
		}

		// Tangent handle events
		private void OnTangentHandleMouseDown(MouseDownEvent evt)
		{
			if (evt.button != 0) return;

			OnDragStarted?.Invoke();
			_isDraggingTangent = true;
			_lastMousePos = evt.mousePosition;
			((VisualElement)evt.target).CaptureMouse();
			((VisualElement)evt.target).Focus();
			evt.StopPropagation();
		}

		private void OnTangentHandleMouseMove(MouseMoveEvent evt)
		{
			if (!_isDraggingTangent) return;

			var delta = evt.mousePosition - _lastMousePos;
			_lastMousePos = evt.mousePosition;

			// If dragging the "in" handle, invert the delta for symmetric behavior
			if (evt.target == _tangentHandleIn)
			{
				delta = -delta;
			}

			OnTangentDragged?.Invoke(delta);
			evt.StopPropagation();
		}

		private void OnTangentHandleMouseUp(MouseUpEvent evt)
		{
			if (!_isDraggingTangent) return;

			_isDraggingTangent = false;
			((VisualElement)evt.target).ReleaseMouse();
			evt.StopPropagation();
		}

		private void OnTangentHandleMouseCaptureOut(MouseCaptureOutEvent evt)
		{
			_isDraggingTangent = false;
		}
	}
}
