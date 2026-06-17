using System;
using System.Collections.Generic;
using System.Linq;
using Nonatomic.VSM.Editor.Layout;
using Nonatomic.VSM.Graph;
using Nonatomic.VSM.Utils;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// The main graph view for the state machine editor.
	/// Uses Unity's GraphView API (experimental in older versions, stable in Unity 6.2).
	/// </summary>
	public class StateMachineGraphView : GraphView
	{
		public event Action<GraphNode> OnSelectionChanged;
		public event Action<GraphRegionView> OnGroupSelectionChanged;
		public event Action OnNavigationChanged;

		/// <summary>
		/// Fires continuously during pan/zoom gestures, unlike viewTransformChanged which only fires on completion.
		/// Use this for real-time synchronization between split views.
		/// </summary>
		public event Action<GraphView> OnViewManipulating;

		/// <summary>
		/// Fires when this view modifies the graph data (nodes, edges, regions added/removed/moved).
		/// Used by the window to refresh the peer view in split mode.
		/// Does NOT fire for view-only changes (pan/zoom, peek popups, node size tracking).
		/// </summary>
		public event Action OnGraphDataChanged;

		private readonly StateMachineGraphWindow _window;
		private StateMachineGraph _asset;

		/// <summary>
		/// Identifier for this pane in split view ("primary" or "secondary").
		/// </summary>
		public string PaneId { get; set; } = "primary";

		private readonly Dictionary<string, Node> _nodeViews = new Dictionary<string, Node>();
		private readonly Dictionary<string, Edge> _edgeViews = new Dictionary<string, Edge>();
		private readonly Dictionary<string, GraphRegionView> _regionViews = new Dictionary<string, GraphRegionView>();
		private readonly Dictionary<string, SubGraphPreviewPopup> _openPeekPopups = new Dictionary<string, SubGraphPreviewPopup>();
		private readonly Dictionary<string, NodeSizeTracker> _nodeSizeTrackers = new Dictionary<string, NodeSizeTracker>();
		private readonly List<BookmarkMarkerView> _bookmarkMarkers = new List<BookmarkMarkerView>();

		// Navigation stack for sub-state machines
		private readonly Stack<StateMachineGraph> _navigationStack = new Stack<StateMachineGraph>();

		// Edge connection listener
		private EdgeDropListener _edgeConnectorListener;

		// View options
		private const string AutoCenterPrefKey = "VSM.GraphView.AutoCenterOnActiveState";
		private const string UseLerpAnimationPrefKey = "VSM.GraphView.UseLerpAnimation";
		private const string LerpDurationPrefKey = "VSM.GraphView.LerpDuration";
		private const string AutoFrameEntryOnOpenPrefKey = "VSM.GraphView.AutoFrameEntryOnOpen";

		private bool _autoCenterOnActiveState;
		private bool _useLerpAnimation = true;
		private float _lerpDuration = 0.3f;
		private bool _autoFrameEntryOnOpen = true;

		// Mouse position tracking for paste operations
		private Vector2 _lastMousePosition;

		// Flag to suppress view transform saving during asset loading
		private bool _isLoadingAsset;

		// Flag to prevent user edits during play mode
		private bool _isPlayModeLocked;

		// Pending delayed tracker creation (cancelled on graph reload)
		private IVisualElementScheduledItem _pendingTrackerCreation;

		/// <summary>
		/// Indicates whether the graph view is currently loading an asset.
		/// Used by region views to suppress element tracking during load/clear.
		/// </summary>
		public bool IsLoadingAsset => _isLoadingAsset;

		/// <summary>
		/// Indicates whether the graph view is locked for play mode.
		/// </summary>
		public bool IsPlayModeLocked => _isPlayModeLocked;

		public bool AutoCenterOnActiveState
		{
			get => _autoCenterOnActiveState;
			set
			{
				_autoCenterOnActiveState = value;
				EditorPrefs.SetBool(AutoCenterPrefKey, value);
			}
		}

		public bool UseLerpAnimation
		{
			get => _useLerpAnimation;
			set
			{
				_useLerpAnimation = value;
				EditorPrefs.SetBool(UseLerpAnimationPrefKey, value);
			}
		}

		public float LerpDuration
		{
			get => _lerpDuration;
			set
			{
				_lerpDuration = value;
				EditorPrefs.SetFloat(LerpDurationPrefKey, value);
			}
		}

		/// <summary>
		/// When enabled, automatically frames the entry node when opening a graph for the first time.
		/// </summary>
		public bool AutoFrameEntryOnOpen
		{
			get => _autoFrameEntryOnOpen;
			set
			{
				_autoFrameEntryOnOpen = value;
				EditorPrefs.SetBool(AutoFrameEntryOnOpenPrefKey, value);
			}
		}

		private const string DebugModePrefKey = "VSM_DebugMode";
		private static bool _debugMode;

		/// <summary>
		/// When enabled, logs debug information about state transitions and highlighting.
		/// Persists across domain reloads via EditorPrefs.
		/// </summary>
		public static bool DebugMode
		{
			get => _debugMode;
			set
			{
				_debugMode = value;
				EditorPrefs.SetBool(DebugModePrefKey, value);
				// Also sync to runtime
				Nonatomic.VSM.Core.StateMachineInstance.DebugMode = value;
			}
		}

		[InitializeOnLoadMethod]
		private static void InitializeDebugMode()
		{
			_debugMode = EditorPrefs.GetBool(DebugModePrefKey, false);
			Nonatomic.VSM.Core.StateMachineInstance.DebugMode = _debugMode;
		}

		// Lerp animation state
		internal bool IsLerpAnimating => _isAnimating;
		private bool _isAnimating;
		private Vector3 _lerpStartPosition;
		private Vector3 _lerpTargetPosition;
		private Vector3 _lerpScale;
		private float _lerpProgress;
		private double _lerpStartTime;
		private float _currentLerpDuration;

		public StateMachineGraph CurrentAsset => _asset;
		public bool CanNavigateBack => _navigationStack.Count > 0;
		public IEdgeConnectorListener EdgeConnectorListener => _edgeConnectorListener;

		/// <summary>
		/// Marks the current asset dirty and notifies listeners that graph data has changed.
		/// Use this for structural changes (nodes, edges, regions).
		/// Does not fire during asset loading to prevent re-entrant peer refresh.
		/// </summary>
		internal void MarkAssetDirty()
		{
			EditorUtility.SetDirty(_asset);
			if (!_isLoadingAsset)
			{
				OnGraphDataChanged?.Invoke();
			}
		}

		public StateMachineGraphView(StateMachineGraphWindow window)
		{
			_window = window;
			_edgeConnectorListener = new EdgeDropListener(this, window);

			// Load view settings from preferences
			_autoCenterOnActiveState = EditorPrefs.GetBool(AutoCenterPrefKey, false);
			_useLerpAnimation = EditorPrefs.GetBool(UseLerpAnimationPrefKey, true);
			_lerpDuration = EditorPrefs.GetFloat(LerpDurationPrefKey, 0.3f);
			_autoFrameEntryOnOpen = EditorPrefs.GetBool(AutoFrameEntryOnOpenPrefKey, true);

			// Load and apply stylesheet
			LoadStyleSheet();

			// Setup manipulators
			this.AddManipulator(new ContentDragger());
			this.AddManipulator(new SelectionDragger());
			this.AddManipulator(new RectangleSelector());
			this.AddManipulator(new ContentZoomer());

			// Setup grid background
			var gridBackground = new GridBackground();
			gridBackground.name = "grid-background";
			gridBackground.AddToClassList("grid-background");
			Insert(0, gridBackground);
			gridBackground.StretchToParentSize();

			// Setup style
			AddToClassList("state-machine-graph-view");

			// Setup callbacks
			graphViewChanged = OnGraphViewChanged;
			serializeGraphElements = SerializeGraphElements;
			canPasteSerializedData = CanPasteSerializedData;
			unserializeAndPaste = UnserializeAndPaste;

			// Setup context menu
			RegisterCallback<ContextualMenuPopulateEvent>(OnContextMenuPopulate);

			// Setup selection callback
			RegisterCallback<MouseUpEvent>(OnMouseUp);

			// Track mouse position for paste operations
			RegisterCallback<MouseMoveEvent>(OnMouseMove);

			// Track pan gestures for real-time sync between split views
			RegisterCallback<PointerMoveEvent>(OnPointerMoveForPanSync);

			// Track zoom gestures for real-time sync between split views
			RegisterCallback<WheelEvent>(OnWheelForZoomSync);

			// Custom edge hit testing for curved edges
			RegisterCallback<MouseDownEvent>(OnMouseDownEdgeHitTest, TrickleDown.TrickleDown);

			// Save view transform when user pans or zooms
			viewTransformChanged += OnViewTransformChanged;

			// Keyboard shortcuts for bookmarks
			RegisterCallback<KeyDownEvent>(OnKeyDown);

			// Make sure we can receive keyboard events
			focusable = true;
		}

		/// <summary>
		/// Locks or unlocks the graph view for play mode.
		/// When locked, nodes cannot be moved, deleted, or added by the user.
		/// Programmatic changes (runtime highlighting, value display) still work.
		/// </summary>
		public void SetPlayModeLocked(bool locked)
		{
			_isPlayModeLocked = locked;

			foreach (var nodeView in _nodeViews.Values)
			{
				if (locked)
					nodeView.capabilities &= ~(Capabilities.Movable | Capabilities.Deletable);
				else
					nodeView.capabilities |= Capabilities.Movable | Capabilities.Deletable;
			}

			foreach (var edgeView in _edgeViews.Values)
			{
				if (locked)
					edgeView.capabilities &= ~Capabilities.Deletable;
				else
					edgeView.capabilities |= Capabilities.Deletable;
			}

			foreach (var regionView in _regionViews.Values)
			{
				if (locked)
					regionView.capabilities &= ~(Capabilities.Movable | Capabilities.Deletable);
				else
					regionView.capabilities |= Capabilities.Movable | Capabilities.Deletable;
			}
		}

		/// <summary>
		/// Custom hit testing for edges with curved parts that extend outside their layout bounds.
		/// </summary>
		private void OnMouseDownEdgeHitTest(MouseDownEvent evt)
		{
			if (evt.button != 0) return;

			// Don't intercept clicks on handles or other interactive elements
			var target = evt.target as VisualElement;
			while (target != null)
			{
				// Skip if clicking on a spline handle or its children
				if (target is SplinePointHandle)
					return;

				// Skip if clicking on a node (let normal selection handle it)
				if (target is Node)
					return;

				// Stop searching at the GraphView level
				if (target is GraphView)
					break;

				target = target.parent;
			}

			// Convert mouse position to graph coordinates
			var graphPoint = contentViewContainer.WorldToLocal(evt.mousePosition);

			// Check all edges for hit
			foreach (var edgeView in _edgeViews.Values)
			{
				if (edgeView is TransitionEdgeView transitionEdge)
				{
					if (IsPointOnEdgeCurve(transitionEdge, graphPoint))
					{
						// Handle selection based on modifier keys
						if (evt.shiftKey || evt.ctrlKey)
						{
							if (transitionEdge.selected)
								RemoveFromSelection(transitionEdge);
							else
								AddToSelection(transitionEdge);
						}
						else
						{
							ClearSelection();
							AddToSelection(transitionEdge);
						}

						evt.StopPropagation();
						return;
					}
				}
			}
		}

		/// <summary>
		/// Tests if a point in graph coordinates is near an edge's curve.
		/// </summary>
		private bool IsPointOnEdgeCurve(TransitionEdgeView edge, Vector2 graphPoint)
		{
			var wireEdgeControl = edge.edgeControl as WireEdgeControl;
			if (wireEdgeControl == null) return false;

			var curvePoints = wireEdgeControl.CurvePoints;
			if (curvePoints == null || curvePoints.Count < 2) return false;

			const float hitDistance = 15f;

			for (var i = 0; i < curvePoints.Count - 1; i++)
			{
				var distance = DistanceToSegment(graphPoint, curvePoints[i], curvePoints[i + 1]);
				if (distance <= hitDistance)
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

		public void PopulateFromAsset(StateMachineGraph asset)
		{
			// Suppress view transform saving during loading to prevent cross-asset contamination
			_isLoadingAsset = true;

			try
			{
				// Save peek popup states for the current graph before switching
				SavePeekPopupStates();

				// Close all open peek popups
				CloseAllPeekPopups();

				_asset = asset;
				ClearGraph();

				if (asset == null) return;

				// Create node views
				foreach (var node in asset.GetAllNodes())
				{
					var nodeView = CreateNodeView(node);
					if (nodeView != null)
					{
						AddElement(nodeView);
						_nodeViews[node.Id] = nodeView;
					}
				}

				// Create edge views
				foreach (var connection in asset.Connections)
				{
					var edgeView = CreateEdgeView(connection);
					if (edgeView != null)
					{
						AddElement(edgeView);
						_edgeViews[connection.Id] = edgeView;
					}
				}

				// Create region views
				foreach (var region in asset.Regions)
				{
					var regionView = CreateRegionView(region);
					if (regionView != null)
					{
						AddElement(regionView);
						_regionViews[region.Id] = regionView;
					}
				}

				// Restore region node membership
				// Group auto-layout is prevented during load (base.OnElementsAdded
				// is skipped in GraphRegionView) so the saved region position is not
				// overridden. Geometry save suppression blocks any residual layout
				// events from persisting stale bounds to disk.
				foreach (var regionView in _regionViews.Values)
				{
					var savedBounds = regionView.RegionData.Bounds;
					regionView.RestoreContainedNodes(_nodeViews);

					// Re-apply saved positions on the next frame after initial layout
					// settles. Suppression stays active until the user explicitly
					// interacts with the region (move, add/remove nodes) so that
					// residual Group auto-layout events cannot overwrite saved bounds.
					var view = regionView;
					var bounds = savedBounds;
					schedule.Execute(() =>
					{
						view.SetPosition(bounds);
						view.RegionData.Bounds = bounds;

						foreach (var nodeId in view.RegionData.ContainedNodeIds)
						{
							if (_nodeViews.TryGetValue(nodeId, out var nodeView) &&
								nodeView is INodeDataProvider provider)
							{
								nodeView.SetPosition(new Rect(provider.NodeData.Position, Vector2.zero));
							}
						}
					});
				}

				// Restore view transform or frame entry node for new graphs
				if (asset.GraphViewData != null && asset.GraphViewData.HasBeenSet)
				{
					UpdateViewTransform(asset.GraphViewData.ViewPosition, asset.GraphViewData.ViewScale);
				}
				else
				{
					// New graph - frame entry node or origin based on setting
					// Use GeometryChangedEvent to ensure layout is valid before framing
					if (_autoFrameEntryOnOpen && !string.IsNullOrEmpty(asset.EntryNodeId))
					{
						ScheduleFrameEntryNode();
					}
					else
					{
						ScheduleFrameOrigin();
					}
				}

				// Refresh orphan status for all portal nodes
				RefreshPortalNodeOrphanStatus();

				// Delay size tracker creation so layout settling (inline field visibility,
				// port rendering, font loading) completes before tracking begins.
				// Without this delay, false-positive width changes trigger repositioning
				// and mark the asset dirty, causing node position drift on every load.
				_pendingTrackerCreation?.Pause();
				_pendingTrackerCreation = schedule.Execute(CreateNodeSizeTrackers);
				_pendingTrackerCreation.ExecuteLater(500);

				// Restore peek popup states for this graph
				RestorePeekPopupStates();

				// Create visual markers for bookmarks
				RefreshBookmarkMarkers();
			}
			finally
			{
				_isLoadingAsset = false;

				// Lock the graph if loaded during play mode
				if (EditorApplication.isPlayingOrWillChangePlaymode)
				{
					SetPlayModeLocked(true);
				}
			}
		}

		private void ClearGraph()
		{
			// Cancel any pending delayed tracker creation
			_pendingTrackerCreation?.Pause();
			_pendingTrackerCreation = null;

			// Dispose all node size trackers
			foreach (var tracker in _nodeSizeTrackers.Values)
			{
				tracker.Dispose();
			}
			_nodeSizeTrackers.Clear();

			foreach (var nodeView in _nodeViews.Values)
			{
				RemoveElement(nodeView);
			}

			foreach (var edgeView in _edgeViews.Values)
			{
				RemoveElement(edgeView);
			}

			foreach (var regionView in _regionViews.Values)
			{
				RemoveElement(regionView);
			}

			foreach (var marker in _bookmarkMarkers)
			{
				marker.RemoveFromHierarchy();
			}
			_bookmarkMarkers.Clear();

			_nodeViews.Clear();
			_edgeViews.Clear();
			_regionViews.Clear();
		}

		/// <summary>
		/// Collects the actual rendered sizes of all node views.
		/// </summary>
		public Dictionary<string, Vector2> GetNodeSizes()
		{
			var sizes = new Dictionary<string, Vector2>();
			foreach (var kvp in _nodeViews)
			{
				var nodeView = kvp.Value;
				var width = nodeView.resolvedStyle.width;
				var height = nodeView.resolvedStyle.height;

				if (!float.IsNaN(width) && !float.IsNaN(height) && width > 0 && height > 0)
				{
					sizes[kvp.Key] = new Vector2(width, height);
				}
			}

			return sizes;
		}

		/// <summary>
		/// Creates size trackers for all nodes to enable automatic repositioning
		/// when nodes are collapsed or expanded.
		/// </summary>
		private void CreateNodeSizeTrackers()
		{
			if (_asset == null) return;

			foreach (var kvp in _nodeViews)
			{
				var nodeId = kvp.Key;
				var nodeView = kvp.Value;

				// Create a tracker for this node
				var tracker = new NodeSizeTracker(
					nodeView,
					_asset,
					_nodeViews,
					() => EditorUtility.SetDirty(_asset));

				_nodeSizeTrackers[nodeId] = tracker;
			}
		}

		/// <summary>
		/// Attaches a size tracker to a single node.
		/// Call this when dynamically adding nodes.
		/// </summary>
		private void AttachNodeSizeTracker(string nodeId, Node nodeView)
		{
			if (_asset == null || nodeView == null) return;

			// Dispose existing tracker if any
			if (_nodeSizeTrackers.TryGetValue(nodeId, out var existingTracker))
			{
				existingTracker.Dispose();
			}

			var tracker = new NodeSizeTracker(
				nodeView,
				_asset,
				_nodeViews,
				() => EditorUtility.SetDirty(_asset));

			_nodeSizeTrackers[nodeId] = tracker;
		}

		/// <summary>
		/// Removes and disposes the size tracker for a node.
		/// Call this when removing nodes.
		/// </summary>
		private void DetachNodeSizeTracker(string nodeId)
		{
			if (_nodeSizeTrackers.TryGetValue(nodeId, out var tracker))
			{
				tracker.Dispose();
				_nodeSizeTrackers.Remove(nodeId);
			}
		}

		/// <summary>
		/// Saves the current state of all open peek popups to the graph asset.
		/// </summary>
		private void SavePeekPopupStates()
		{
			if (_asset?.GraphViewData == null) return;

			// Clear existing states
			_asset.GraphViewData.OpenPeekPreviews.Clear();

			// Save state for each open popup
			foreach (var kvp in _openPeekPopups)
			{
				var nodeId = kvp.Key;
				var popup = kvp.Value;

				// Save the offset from owner node (not absolute position) for better portability
				_asset.GraphViewData.SetPeekPreviewState(
					nodeId,
					popup.OffsetFromOwnerNode,
					popup.PanOffset,
					popup.ZoomLevel
				);
			}

			if (_openPeekPopups.Count > 0)
			{
				EditorUtility.SetDirty(_asset);
			}
		}

		/// <summary>
		/// Restores peek popups from the saved state in the graph asset.
		/// </summary>
		private void RestorePeekPopupStates()
		{
			if (_asset?.GraphViewData == null) return;

			foreach (var state in _asset.GraphViewData.OpenPeekPreviews)
			{
				// Find the node view
				if (!_nodeViews.TryGetValue(state.NodeId, out var nodeView)) continue;
				if (nodeView is not SubStateMachineNodeView subNodeView) continue;

				// Get the sub-graph from the node
				var subNode = subNodeView.NodeData;
				if (subNode.SubGraph == null) continue;

				// Create and show the popup
				var popup = new SubGraphPreviewPopup(subNode.SubGraph);
				popup.NodeId = state.NodeId;
				popup.OnClosed += OnPeekPopupClosed;

				// Calculate absolute position from saved offset + node position
				var nodeRect = subNodeView.GetPosition();
				var absolutePosition = new Vector2(
					nodeRect.xMax + state.Position.x,
					nodeRect.yMin + state.Position.y
				);

				// Show at the calculated position
				popup.ShowInGraph(this, absolutePosition);

				// Set owner node for position tracking (popup follows node when moved)
				popup.SetOwnerNode(subNodeView, state.Position);

				// Restore pan/zoom state
				popup.RestoreState(state.PanOffset, state.ZoomLevel);

				// Track the popup
				_openPeekPopups[state.NodeId] = popup;
			}
		}

		/// <summary>
		/// Closes all open peek popups without saving their state.
		/// </summary>
		private void CloseAllPeekPopups()
		{
			foreach (var popup in _openPeekPopups.Values)
			{
				// Unsubscribe to avoid firing the closed event
				popup.OnClosed -= OnPeekPopupClosed;
				popup.Close();
			}
			_openPeekPopups.Clear();
		}

		/// <summary>
		/// Registers a peek popup for tracking.
		/// Call this when a SubStateMachineNodeView opens a peek popup.
		/// </summary>
		public void RegisterPeekPopup(string nodeId, SubGraphPreviewPopup popup)
		{
			// Close any existing popup for this node
			if (_openPeekPopups.TryGetValue(nodeId, out var existingPopup))
			{
				existingPopup.OnClosed -= OnPeekPopupClosed;
				existingPopup.Close();
			}

			popup.NodeId = nodeId;
			popup.OnClosed += OnPeekPopupClosed;
			_openPeekPopups[nodeId] = popup;
		}

		/// <summary>
		/// Called when a peek popup is closed.
		/// </summary>
		private void OnPeekPopupClosed(SubGraphPreviewPopup popup)
		{
			if (string.IsNullOrEmpty(popup.NodeId)) return;

			_openPeekPopups.Remove(popup.NodeId);

			// Also remove from the saved state
			if (_asset?.GraphViewData != null)
			{
				_asset.GraphViewData.RemovePeekPreviewState(popup.NodeId);
				EditorUtility.SetDirty(_asset);
			}
		}

		/// <summary>
		/// Gets the open peek popup for a node, if any.
		/// </summary>
		public SubGraphPreviewPopup GetPeekPopup(string nodeId)
		{
			_openPeekPopups.TryGetValue(nodeId, out var popup);
			return popup;
		}

		private Node CreateNodeView(GraphNode node)
		{
			return node.NodeType switch
			{
				GraphNodeType.State => CreateStateNodeView(node as StateNode),
				GraphNodeType.Entry => CreateEntryNodeView(node as EntryNode),
				GraphNodeType.Exit => CreateExitNodeView(node as ExitNode),
				GraphNodeType.BlackboardVariable => CreateBlackboardVariableNodeView(node as BlackboardVariableNode),
				GraphNodeType.SubStateMachine => CreateSubStateMachineNodeView(node as SubStateMachineNode),
				GraphNodeType.PortalOut => CreatePortalOutNodeView(node as PortalOutNode),
				GraphNodeType.PortalIn => CreatePortalInNodeView(node as PortalInNode),
				_ => null
			};
		}

		private Node CreateStateNodeView(StateNode stateNode)
		{
			var nodeView = new StateNodeView(stateNode, this);
			nodeView.SetPosition(new Rect(stateNode.Position, Vector2.zero));
			return nodeView;
		}

		private Node CreateEntryNodeView(EntryNode entryNode)
		{
			var nodeView = new EntryNodeView(entryNode, this);
			nodeView.SetPosition(new Rect(entryNode.Position, Vector2.zero));
			return nodeView;
		}

		private Node CreateExitNodeView(ExitNode exitNode)
		{
			var nodeView = new ExitNodeView(exitNode);
			nodeView.SetPosition(new Rect(exitNode.Position, Vector2.zero));
			return nodeView;
		}

		private Node CreateBlackboardVariableNodeView(BlackboardVariableNode varNode)
		{
			var nodeView = new BlackboardVariableNodeView(varNode, this);
			nodeView.SetPosition(new Rect(varNode.Position, Vector2.zero));
			return nodeView;
		}

		private Node CreateSubStateMachineNodeView(SubStateMachineNode subNode)
		{
			var nodeView = new SubStateMachineNodeView(subNode, this);
			nodeView.SetPosition(new Rect(subNode.Position, Vector2.zero));
			return nodeView;
		}

		private Node CreatePortalOutNodeView(PortalOutNode portalOutNode)
		{
			var nodeView = new PortalOutNodeView(portalOutNode, this);
			nodeView.SetPosition(new Rect(portalOutNode.Position, Vector2.zero));
			return nodeView;
		}

		private Node CreatePortalInNodeView(PortalInNode portalInNode)
		{
			var nodeView = new PortalInNodeView(portalInNode, this);
			nodeView.SetPosition(new Rect(portalInNode.Position, Vector2.zero));
			return nodeView;
		}

		private GraphRegionView CreateRegionView(GraphRegion region)
		{
			var regionView = new GraphRegionView(region, this);
			return regionView;
		}

		private Edge CreateEdgeView(Connection connection)
		{
			if (!_nodeViews.TryGetValue(connection.SourceNodeId, out var sourceNodeView)) return null;
			if (!_nodeViews.TryGetValue(connection.TargetNodeId, out var targetNodeView)) return null;

			Port outputPort = null;
			Port inputPort = null;

			if (sourceNodeView is IPortProvider sourceProvider)
			{
				outputPort = sourceProvider.GetOutputPort(connection.SourcePortName);
			}

			if (targetNodeView is IPortProvider targetProvider)
			{
				inputPort = targetProvider.GetInputPort(connection.TargetPortName);
			}

			if (outputPort == null || inputPort == null) return null;

			var edge = new TransitionEdgeView(connection)
			{
				output = outputPort,
				input = inputPort
			};

			edge.output.Connect(edge);
			edge.input.Connect(edge);

			// Subscribe to connection modifications to save curve changes
			edge.OnConnectionModified += OnEdgeConnectionModified;

			return edge;
		}

		private void OnEdgeConnectionModified(TransitionEdgeView edge)
		{
			if (_asset == null) return;

			// Mark asset dirty and save to persist curve changes
			MarkAssetDirty();
			AssetDatabase.SaveAssetIfDirty(_asset);

			if (DebugMode)
			{
				Debug.Log($"[VSM] Edge curve modified and saved: {edge.ConnectionData?.Id}");
			}
		}

		private void OnContextMenuPopulate(ContextualMenuPopulateEvent evt)
		{
			if (_isPlayModeLocked) return;

			var graphPosition = contentViewContainer.WorldToLocal(evt.mousePosition);
			var screenPosition = GUIUtility.GUIToScreenPoint(evt.mousePosition);

			evt.menu.AppendAction("Frame All", action => FrameAll());
			evt.menu.AppendAction("Frame Selection", action => FrameSelection());
			evt.menu.AppendAction("Frame Entry Node", action => FrameEntryNode());
			evt.menu.AppendSeparator();
			evt.menu.AppendAction("Add Node", action => { ShowStateSearchWindow(graphPosition, screenPosition); });
		}

		private void ShowStateSearchWindow(Vector2 graphPosition, Vector2 screenPosition)
		{
			var searchWindow = ScriptableObject.CreateInstance<StateSearchWindow>();
			searchWindow.Initialize(this, _window, graphPosition);
			SearchWindow.Open(new SearchWindowContext(screenPosition), searchWindow);
		}

		private void ShowSubStateMachineSelector(Vector2 position)
		{
			CreateSubStateMachineNode(null, position);
		}

		/// <summary>
		/// Creates a SubStateMachine node at the specified position.
		/// </summary>
		/// <param name="subGraph">Optional sub-graph to assign (can be null, user assigns via node UI)</param>
		/// <param name="position">Position in graph coordinates</param>
		public void CreateSubStateMachineNode(StateMachineGraph subGraph, Vector2 position)
		{
			if (_asset == null) return;

			// Prevent self-reference
			if (subGraph == _asset)
			{
				Debug.LogWarning("Cannot add a state machine as a sub-state machine of itself");
				return;
			}

			Undo.RecordObject(_asset, "Create Sub-State Machine Node");

			var subMachineNode = new SubStateMachineNode
			{
				Position = position,
				SubGraph = subGraph,
				DisplayName = subGraph != null ? subGraph.name : "Sub State Machine"
			};

			_asset.AddNode(subMachineNode);

			var nodeView = CreateSubStateMachineNodeView(subMachineNode);
			AddElement(nodeView);
			_nodeViews[subMachineNode.Id] = nodeView;
			AttachNodeSizeTracker(subMachineNode.Id, nodeView);

			MarkAssetDirty();
		}

		public void CreateStateNode(Type stateType, Vector2 position)
		{
			if (_asset == null || stateType == null) return;

			Undo.RecordObject(_asset, "Create State Node");

			var stateNode = new StateNode(stateType)
			{
				Position = position
			};

			_asset.AddNode(stateNode);

			var nodeView = CreateStateNodeView(stateNode);
			if (nodeView == null) return;

			AddElement(nodeView);
			_nodeViews[stateNode.Id] = nodeView;
			AttachNodeSizeTracker(stateNode.Id, nodeView);

			MarkAssetDirty();
		}

		public void CreateEntryNode(Vector2 position)
		{
			if (_asset == null || !string.IsNullOrEmpty(_asset.EntryNodeId))
			{
				return;
			}

			Undo.RecordObject(_asset, "Create Entry Node");

			var entryNode = new EntryNode
			{
				Position = position
			};

			_asset.AddNode(entryNode);

			var nodeView = new EntryNodeView(entryNode, this);
			AddElement(nodeView);
			_nodeViews[entryNode.Id] = nodeView;
			AttachNodeSizeTracker(entryNode.Id, nodeView);

			MarkAssetDirty();
		}

		public void CreateExitNode(Vector2 position)
		{
			if (_asset == null)
			{
				return;
			}

			Undo.RecordObject(_asset, "Create Exit Node");

			var exitNode = new ExitNode
			{
				Position = position
			};

			_asset.AddNode(exitNode);

			var nodeView = CreateExitNodeView(exitNode);
			AddElement(nodeView);
			_nodeViews[exitNode.Id] = nodeView;
			AttachNodeSizeTracker(exitNode.Id, nodeView);

			MarkAssetDirty();
		}

		public void CreatePortalOutNode(Vector2 position)
		{
			if (_asset == null) return;

			Undo.RecordObject(_asset, "Create Portal Out Node");

			var channelId = GetNextChannelForPortalOut();
			var portalOutNode = new PortalOutNode(channelId)
			{
				Position = position
			};

			_asset.AddNode(portalOutNode);

			var nodeView = CreatePortalOutNodeView(portalOutNode);
			AddElement(nodeView);
			_nodeViews[portalOutNode.Id] = nodeView;
			AttachNodeSizeTracker(portalOutNode.Id, nodeView);

			MarkAssetDirty();

			// Refresh orphan status for all portal nodes
			RefreshPortalNodeOrphanStatus();
		}

		public void CreatePortalInNode(Vector2 position)
		{
			if (_asset == null) return;

			Undo.RecordObject(_asset, "Create Portal In Node");

			var channelId = GetNextChannelForPortalIn();
			var portalInNode = new PortalInNode(channelId)
			{
				Position = position
			};

			_asset.AddNode(portalInNode);

			var nodeView = CreatePortalInNodeView(portalInNode);
			AddElement(nodeView);
			_nodeViews[portalInNode.Id] = nodeView;
			AttachNodeSizeTracker(portalInNode.Id, nodeView);

			MarkAssetDirty();

			// Refresh orphan status for all portal nodes
			RefreshPortalNodeOrphanStatus();
		}

		/// <summary>
		/// Creates a new region at the specified position.
		/// </summary>
		public void CreateRegion(Vector2 position)
		{
			if (_asset == null) return;

			Undo.RecordObject(_asset, "Create Region");

			var region = new GraphRegion
			{
				Title = "New Region",
				Bounds = new Rect(position.x, position.y, 300, 200)
			};

			_asset.AddRegion(region);

			var regionView = CreateRegionView(region);
			AddElement(regionView);
			_regionViews[region.Id] = regionView;

			MarkAssetDirty();
		}

		/// <summary>
		/// Groups the currently selected nodes into a new region.
		/// </summary>
		private void GroupSelectedNodes()
		{
			var selectedNodes = selection.OfType<Node>().ToList();
			if (selectedNodes.Count == 0) return;
			if (_asset == null) return;

			// Compute bounding rect of all selected nodes
			var first = selectedNodes[0].GetPosition();
			var xMin = first.xMin;
			var yMin = first.yMin;
			var xMax = first.xMax;
			var yMax = first.yMax;

			for (var i = 1; i < selectedNodes.Count; i++)
			{
				var pos = selectedNodes[i].GetPosition();
				xMin = Mathf.Min(xMin, pos.xMin);
				yMin = Mathf.Min(yMin, pos.yMin);
				xMax = Mathf.Max(xMax, pos.xMax);
				yMax = Mathf.Max(yMax, pos.yMax);
			}

			var bounds = Rect.MinMaxRect(xMin, yMin, xMax, yMax);

			// Add padding around the nodes
			var padding = 40f;
			bounds.x -= padding;
			bounds.y -= padding - 30; // Extra top padding for the region header
			bounds.width += padding * 2;
			bounds.height += padding * 2 + 30;

			Undo.RecordObject(_asset, "Group Selected Nodes");

			var region = new GraphRegion
			{
				Title = "New Region",
				Bounds = bounds
			};

			_asset.AddRegion(region);

			var regionView = CreateRegionView(region);
			AddElement(regionView);
			_regionViews[region.Id] = regionView;

			// Add selected nodes to the region
			regionView.AddElements(selectedNodes);

			MarkAssetDirty();
		}

		/// <summary>
		/// Removes the currently selected region, keeping its contained nodes.
		/// </summary>
		private void UngroupSelectedRegion()
		{
			var selectedRegion = selection.OfType<GraphRegionView>().FirstOrDefault();
			if (selectedRegion == null) return;

			RemoveRegion(selectedRegion.RegionData.Id);
		}

		/// <summary>
		/// Removes a region from the graph.
		/// </summary>
		public void RemoveRegion(string regionId)
		{
			if (_asset == null) return;
			if (!_regionViews.TryGetValue(regionId, out var regionView)) return;

			Undo.RecordObject(_asset, "Remove Region");

			RemoveElement(regionView);
			_regionViews.Remove(regionId);
			_asset.RemoveRegion(regionId);

			MarkAssetDirty();
		}

		/// <summary>
		/// Converts all nodes in a region into a new sub-state machine graph asset,
		/// replacing them with a single SubStateMachineNode in the parent graph.
		/// </summary>
		public void ConvertRegionToSubStateMachine(string regionId)
		{
			if (_asset == null) return;

			var region = _asset.GetRegion(regionId);
			if (region == null || region.ContainedNodeIds.Count == 0) return;

			// Collect the nodes to move (skip the parent graph's entry node)
			var nodeIdsToMove = new HashSet<string>(region.ContainedNodeIds);
			var parentEntryNodeId = GetParentEntryNodeId();
			nodeIdsToMove.Remove(parentEntryNodeId);

			if (nodeIdsToMove.Count == 0) return;

			// Classify all connections touching these nodes
			var internalConnections = new List<Connection>();
			var incomingConnections = new List<Connection>();
			var outgoingConnections = new List<Connection>();

			foreach (var connection in _asset.Connections)
			{
				var sourceInside = nodeIdsToMove.Contains(connection.SourceNodeId);
				var targetInside = nodeIdsToMove.Contains(connection.TargetNodeId);

				if (sourceInside && targetInside)
				{
					internalConnections.Add(connection);
				}
				else if (!sourceInside && targetInside)
				{
					incomingConnections.Add(connection);
				}
				else if (sourceInside && !targetInside)
				{
					outgoingConnections.Add(connection);
				}
			}

			// Prompt user for save location
			var parentPath = AssetDatabase.GetAssetPath(_asset);
			var defaultDir = string.IsNullOrEmpty(parentPath)
				? "Assets"
				: System.IO.Path.GetDirectoryName(parentPath)?.Replace("\\", "/") ?? "Assets";
			var defaultName = SanitizeFileName(region.Title);

			var savePath = EditorUtility.SaveFilePanelInProject(
				"Save Sub-State Machine",
				defaultName,
				"asset",
				"Choose a location for the new sub-state machine graph.",
				defaultDir);

			if (string.IsNullOrEmpty(savePath)) return;

			// Record undo on parent asset
			Undo.RecordObject(_asset, "Convert Region to Sub-State Machine");

			// Create the new sub-graph asset
			var subGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			subGraph.InitializeNewGraph();
			AssetDatabase.CreateAsset(subGraph, savePath);

			// Build old-ID → new-ID mapping and copy nodes
			var idMap = new Dictionary<string, string>();
			var subGraphEntryNodeId = subGraph.EntryNodeId;

			// Compute position offset so nodes are centered in the sub-graph
			var positionOffset = ComputeNodeCentroid(nodeIdsToMove);

			foreach (var oldId in nodeIdsToMove)
			{
				var originalNode = _asset.GetNode(oldId);
				if (originalNode == null) continue;

				var clone = CloneNodeWithNewId(originalNode);
				if (clone == null) continue;

				// Offset position relative to the group center
				clone.Position -= positionOffset;

				idMap[oldId] = clone.Id;
				subGraph.AddNode(clone);
			}

			// Copy internal connections with remapped IDs
			foreach (var conn in internalConnections)
			{
				if (!idMap.TryGetValue(conn.SourceNodeId, out var newSourceId)) continue;
				if (!idMap.TryGetValue(conn.TargetNodeId, out var newTargetId)) continue;

				var newConn = new Connection(
					newSourceId, conn.SourcePortName,
					newTargetId, conn.TargetPortName,
					conn.ConnectionType);

				if (conn.TransitionTime > 0)
				{
					newConn.TransitionTime = conn.TransitionTime;
					newConn.EasingCurve = conn.EasingCurve;
				}

				subGraph.AddConnection(newConn);
			}

			// Connect the sub-graph's entry node to nodes that had incoming transitions
			var incomingTargetIds = new HashSet<string>();
			foreach (var conn in incomingConnections)
			{
				if (conn.ConnectionType == ConnectionType.Transition &&
					idMap.TryGetValue(conn.TargetNodeId, out var newTargetId))
				{
					incomingTargetIds.Add(newTargetId);
				}
			}

			// Connect entry to the first incoming target (entry node has single output)
			if (incomingTargetIds.Count > 0)
			{
				var firstTarget = incomingTargetIds.GetEnumerator();
				firstTarget.MoveNext();

				var entryConn = new Connection(
					subGraphEntryNodeId, "Start",
					firstTarget.Current, "Enter",
					ConnectionType.Transition);
				subGraph.AddConnection(entryConn);
			}

			// Create exit nodes for outgoing transitions
			var exitNodeCreated = false;
			string firstExitNodeId = null;

			foreach (var conn in outgoingConnections)
			{
				if (conn.ConnectionType != ConnectionType.Transition) continue;
				if (!idMap.TryGetValue(conn.SourceNodeId, out var newSourceId)) continue;

				var exitNode = new ExitNode
				{
					Position = GetNodePosition(conn.SourceNodeId) - positionOffset + new Vector2(250, 0),
					ExitName = "Exit",
					DisplayName = "Exit"
				};
				subGraph.AddNode(exitNode);

				var exitConn = new Connection(
					newSourceId, conn.SourcePortName,
					exitNode.Id, "Enter",
					ConnectionType.Transition);
				subGraph.AddConnection(exitConn);

				if (!exitNodeCreated)
				{
					firstExitNodeId = exitNode.Id;
					exitNodeCreated = true;
				}
			}

			// Save the sub-graph
			EditorUtility.SetDirty(subGraph);
			AssetDatabase.SaveAssetIfDirty(subGraph);

			// Compute position for the SubStateMachineNode (region center)
			var regionCenter = new Vector2(
				region.Bounds.x + region.Bounds.width / 2f,
				region.Bounds.y + region.Bounds.height / 2f);

			// Create SubStateMachineNode in parent graph
			var subMachineNode = new SubStateMachineNode
			{
				Position = regionCenter,
				SubGraph = subGraph,
				SubGraphAssetGuid = AssetDatabase.AssetPathToGUID(savePath),
				DisplayName = region.Title,
				DataMode = SubStateMachineDataMode.Inherit
			};
			_asset.AddNode(subMachineNode);

			// Rewire incoming flow connections to SubStateMachineNode's Enter port
			foreach (var conn in incomingConnections)
			{
				if (conn.ConnectionType != ConnectionType.Transition) continue;

				var rewired = new Connection(
					conn.SourceNodeId, conn.SourcePortName,
					subMachineNode.Id, "Enter",
					ConnectionType.Transition);
				_asset.AddConnection(rewired);
			}

			// Rewire first outgoing flow connection from SubStateMachineNode's Exit port
			if (outgoingConnections.Count > 0)
			{
				var firstOutgoing = outgoingConnections.Find(c => c.ConnectionType == ConnectionType.Transition);
				if (firstOutgoing != null)
				{
					var rewired = new Connection(
						subMachineNode.Id, "Exit",
						firstOutgoing.TargetNodeId, firstOutgoing.TargetPortName,
						ConnectionType.Transition);
					_asset.AddConnection(rewired);
				}
			}

			// Remove original nodes from parent (this cascades connection removal)
			foreach (var nodeId in nodeIdsToMove)
			{
				_asset.RemoveNode(nodeId);
			}

			// Remove the region
			_asset.RemoveRegion(regionId);

			// Save parent
			MarkAssetDirty();
			AssetDatabase.SaveAssetIfDirty(_asset);

			// Reload the graph view
			PopulateFromAsset(_asset);
		}

		private string GetParentEntryNodeId()
		{
			foreach (var node in _asset.GetAllNodes())
			{
				if (node is EntryNode)
				{
					return node.Id;
				}
			}

			return null;
		}

		private Vector2 ComputeNodeCentroid(IEnumerable<string> nodeIds)
		{
			var sum = Vector2.zero;
			var count = 0;

			foreach (var id in nodeIds)
			{
				var node = _asset.GetNode(id);
				if (node == null) continue;
				sum += node.Position;
				count++;
			}

			return count > 0 ? sum / count : Vector2.zero;
		}

		private Vector2 GetNodePosition(string nodeId)
		{
			var node = _asset.GetNode(nodeId);
			return node?.Position ?? Vector2.zero;
		}

		private static GraphNode CloneNodeWithNewId(GraphNode original)
		{
			var serialized = SerializedNode.FromNode(original);
			if (serialized == null) return null;

			var clone = serialized.ToNode();
			if (clone == null) return null;

			clone.Id = Guid.NewGuid().ToString();
			return clone;
		}

		private static string SanitizeFileName(string name)
		{
			if (string.IsNullOrEmpty(name)) return "SubStateMachine";

			var invalid = System.IO.Path.GetInvalidFileNameChars();
			var sanitized = new System.Text.StringBuilder(name.Length);

			foreach (var c in name)
			{
				if (Array.IndexOf(invalid, c) < 0)
				{
					sanitized.Append(c);
				}
			}

			var result = sanitized.ToString().Trim();
			return string.IsNullOrEmpty(result) ? "SubStateMachine" : result;
		}

		/// <summary>
		/// Returns the region view that contains the given node, or null.
		/// </summary>
		public GraphRegionView FindContainingRegion(string nodeId)
		{
			if (string.IsNullOrEmpty(nodeId)) return null;

			foreach (var regionView in _regionViews.Values)
			{
				if (regionView.RegionData.ContainsNode(nodeId))
				{
					return regionView;
				}
			}

			return null;
		}

		/// <summary>
		/// Removes a node from its containing group.
		/// </summary>
		public void RemoveNodeFromGroup(Node node, string nodeId)
		{
			var regionView = FindContainingRegion(nodeId);
			if (regionView == null) return;

			regionView.RemoveElement(node);
		}

		/// <summary>
		/// Finds the best channel ID for a new PortalOut node.
		/// Prioritizes pairing with an orphaned PortalIn (one without a matching PortalOut).
		/// </summary>
		private int GetNextChannelForPortalOut() => GetNextPortalChannel(forPortalOut: true);

		/// <summary>
		/// Finds the best channel ID for a new PortalIn node.
		/// Prioritizes pairing with an orphaned PortalOut (one without a matching PortalIn).
		/// </summary>
		private int GetNextChannelForPortalIn() => GetNextPortalChannel(forPortalOut: false);

		/// <summary>
		/// Finds the best channel ID for a new portal node.
		/// When forPortalOut is true, prioritizes pairing with an orphaned PortalIn.
		/// When forPortalOut is false, prioritizes pairing with an orphaned PortalOut.
		/// </summary>
		private int GetNextPortalChannel(bool forPortalOut)
		{
			if (_asset == null) return 0;

			var portalOutChannels = new HashSet<int>();
			var portalInChannels = new HashSet<int>();

			foreach (var node in _asset.GetAllNodes())
			{
				if (node is PortalOutNode portalOut)
				{
					portalOutChannels.Add(portalOut.ChannelId);
				}
				else if (node is PortalInNode portalIn)
				{
					portalInChannels.Add(portalIn.ChannelId);
				}
			}

			// Search for orphaned portals of the opposite type to pair with
			var orphanCandidates = forPortalOut ? portalInChannels : portalOutChannels;
			var existingChannels = forPortalOut ? portalOutChannels : portalInChannels;

			foreach (var channel in orphanCandidates)
			{
				if (!existingChannels.Contains(channel))
				{
					return channel;
				}
			}

			// No orphan found, get a completely new channel
			var allUsedChannels = new HashSet<int>(portalOutChannels);
			allUsedChannels.UnionWith(portalInChannels);

			for (var i = 0; i < 1000; i++)
			{
				if (!allUsedChannels.Contains(i))
					return i;
			}

			return allUsedChannels.Count;
		}

		private GraphViewChange OnGraphViewChanged(GraphViewChange change)
		{
			if (_asset == null) return change;

			// Prevent all graph modifications from being persisted during play mode.
			// Runtime highlighting and value display are handled separately.
			if (EditorApplication.isPlayingOrWillChangePlaymode) return change;

			// Handle node removal
			if (change.elementsToRemove != null)
			{
				// When a Group is deleted, Unity automatically adds all contained
				// nodes to elementsToRemove.  Strip those out so that only the
				// region itself is removed, not the nodes inside it.
				var regionsBeingRemoved = new HashSet<GraphRegionView>();
				foreach (var element in change.elementsToRemove)
				{
					if (element is GraphRegionView rv)
					{
						regionsBeingRemoved.Add(rv);
					}
				}

				if (regionsBeingRemoved.Count > 0)
				{
					var protectedNodeIds = new HashSet<string>();
					foreach (var rv in regionsBeingRemoved)
					{
						foreach (var nodeId in rv.RegionData.ContainedNodeIds)
						{
							protectedNodeIds.Add(nodeId);
						}
					}

					change.elementsToRemove.RemoveAll(e =>
					{
						if (e is INodeDataProvider provider &&
						    protectedNodeIds.Contains(provider.NodeData.Id))
						{
							return true;
						}

						// Also protect edges connected to protected nodes
						if (e is TransitionEdgeView tev &&
						    tev.ConnectionData != null &&
						    (protectedNodeIds.Contains(tev.ConnectionData.SourceNodeId) ||
						     protectedNodeIds.Contains(tev.ConnectionData.TargetNodeId)))
						{
							return true;
						}

						return false;
					});
				}

				Undo.RecordObject(_asset, "Remove Elements");

				var portalNodeRemoved = false;

				foreach (var element in change.elementsToRemove)
				{
					// Prevent Entry node deletion
					if (element is EntryNodeView) continue;

					if (element is INodeDataProvider nodeProvider)
					{
						var nodeId = nodeProvider.NodeData.Id;

						// Collect connected edges before removing the node from the data model
						var connectedEdgeIds = new List<string>();
						foreach (var kvp in _edgeViews)
						{
							if (kvp.Value is TransitionEdgeView tev &&
							    tev.ConnectionData != null &&
							    (tev.ConnectionData.SourceNodeId == nodeId ||
							     tev.ConnectionData.TargetNodeId == nodeId))
							{
								connectedEdgeIds.Add(kvp.Key);
							}
						}

						_asset.RemoveNode(nodeId);
						_nodeViews.Remove(nodeId);
						DetachNodeSizeTracker(nodeId);

						// Remove orphaned edge views that were connected to the deleted node
						foreach (var edgeId in connectedEdgeIds)
						{
							if (!_edgeViews.TryGetValue(edgeId, out var edgeToRemove)) continue;

							// Update inline field visibility for target nodes that still exist
							if (edgeToRemove is TransitionEdgeView removedTev)
							{
								var targetId = removedTev.ConnectionData?.TargetNodeId;
								if (!string.IsNullOrEmpty(targetId) &&
								    targetId != nodeId &&
								    _nodeViews.TryGetValue(targetId, out var remainingNodeView))
								{
									if (remainingNodeView is StateNodeView stateNv)
									{
										schedule.Execute(() => stateNv.UpdateInlineFieldVisibility());
									}
									else if (remainingNodeView is SubStateMachineNodeView subNv)
									{
										schedule.Execute(() => subNv.UpdateInlineFieldVisibility());
									}
								}
							}

							edgeToRemove.input?.Disconnect(edgeToRemove);
							edgeToRemove.output?.Disconnect(edgeToRemove);
							RemoveElement(edgeToRemove);
							_edgeViews.Remove(edgeId);
						}

						// Track if a portal node was removed
						if (element is PortalInNodeView || element is PortalOutNodeView)
						{
							portalNodeRemoved = true;
						}
					}
					else if (element is TransitionEdgeView edgeView)
					{
						// Update inline field visibility for the target node before removing
						var targetNodeId = edgeView.ConnectionData?.TargetNodeId;
						if (!string.IsNullOrEmpty(targetNodeId) &&
						    _nodeViews.TryGetValue(targetNodeId, out var targetNodeView))
						{
							// Schedule update after removal is complete
							if (targetNodeView is StateNodeView stateNodeView)
							{
								schedule.Execute(() => stateNodeView.UpdateInlineFieldVisibility());
							}
							else if (targetNodeView is SubStateMachineNodeView subNodeView)
							{
								schedule.Execute(() => subNodeView.UpdateInlineFieldVisibility());
							}
						}

						_asset.RemoveConnection(edgeView.ConnectionData.Id);
						_edgeViews.Remove(edgeView.ConnectionData.Id);
					}
					else if (element is GraphRegionView regionView)
					{
						_asset.RemoveRegion(regionView.RegionData.Id);
						_regionViews.Remove(regionView.RegionData.Id);
					}
				}

				MarkAssetDirty();
				AssetDatabase.SaveAssetIfDirty(_asset);

				// Refresh orphan status if a portal node was removed
				if (portalNodeRemoved)
				{
					RefreshPortalNodeOrphanStatus();
				}
			}

			// Handle node movement — skip during play mode to prevent runtime
			// layout changes (debug highlights, content resizing) from persisting
			if (change.movedElements != null && !EditorApplication.isPlayingOrWillChangePlaymode)
			{
				Undo.RecordObject(_asset, "Move Elements");

				foreach (var element in change.movedElements)
				{
					if (element is INodeDataProvider nodeProvider)
					{
						var newPosition = (element as Node)?.GetPosition().position ?? Vector2.zero;
						nodeProvider.NodeData.Position = newPosition;
					}
					else if (element is GraphRegionView regionView)
					{
						// User interaction — allow geometry saves from this point
						regionView.SuppressGeometrySave(false);

						var newBounds = regionView.GetPosition();
						regionView.RegionData.Bounds = newBounds;
						_asset.UpdateRegion(regionView.RegionData);

						// When a Group is moved, the contained nodes move visually
						// but are NOT included in movedElements by GraphView.
						// Persist their updated positions so that on reload the
						// Group auto-layout computes the correct bounding box.
						foreach (var nodeId in regionView.RegionData.ContainedNodeIds)
						{
							if (_nodeViews.TryGetValue(nodeId, out var containedNodeView) &&
								containedNodeView is INodeDataProvider containedProvider)
							{
								var nodePos = containedNodeView.GetPosition().position;
								containedProvider.NodeData.Position = nodePos;
							}
						}
					}
				}

				MarkAssetDirty();
				AssetDatabase.SaveAssetIfDirty(_asset);
			}

			// Handle new edges
			if (change.edgesToCreate != null)
			{
				Undo.RecordObject(_asset, "Create Connections");

				for (int i = 0; i < change.edgesToCreate.Count; i++)
				{
					var edge = change.edgesToCreate[i];

					// If it's a plain Edge (not our custom type), replace it with TransitionEdgeView
					if (edge is not TransitionEdgeView)
					{
						// Create our custom edge
						var transitionEdge = new TransitionEdgeView
						{
							input = edge.input,
							output = edge.output
						};

						// Disconnect and remove the default edge
						edge.input?.Disconnect(edge);
						edge.output?.Disconnect(edge);
						RemoveElement(edge);

						// Connect our custom edge
						transitionEdge.input?.Connect(transitionEdge);
						transitionEdge.output?.Connect(transitionEdge);
						AddElement(transitionEdge);

						// Replace in the list
						change.edgesToCreate[i] = transitionEdge;
						edge = transitionEdge;
					}

					var connection = CreateConnectionFromEdge(edge);
					if (connection != null)
					{
						_edgeViews[connection.Id] = edge;

						// Update inline field visibility for the target node
						if (_nodeViews.TryGetValue(connection.TargetNodeId, out var targetNodeView))
						{
							if (targetNodeView is StateNodeView stateNodeView)
							{
								stateNodeView.UpdateInlineFieldVisibility();
							}
							else if (targetNodeView is SubStateMachineNodeView subNodeView)
							{
								subNodeView.UpdateInlineFieldVisibility();
							}
						}
					}
				}

				MarkAssetDirty();
				AssetDatabase.SaveAssetIfDirty(_asset);
			}

			return change;
		}

		private Connection CreateConnectionFromEdge(Edge edge)
		{
			var sourceNode = edge.output.node;
			var targetNode = edge.input.node;

			if (sourceNode is not INodeDataProvider sourceProvider ||
				targetNode is not INodeDataProvider targetProvider)
			{
				return null;
			}

			var connectionType = DetermineConnectionType(sourceNode, targetNode, edge);

			// Prefer userData (field name) over portName (which may be the display name
			// if the port label was overwritten after portName was set).
			var sourcePortName = edge.output.userData as string ?? edge.output.portName;
			var targetPortName = edge.input.userData as string ?? edge.input.portName;

			var connection = new Connection(
				sourceProvider.NodeData.Id,
				sourcePortName,
				targetProvider.NodeData.Id,
				targetPortName,
				connectionType
			);

			_asset.AddConnection(connection);

			// If it's our custom edge type, store the connection data
			if (edge is TransitionEdgeView transitionEdge)
			{
				transitionEdge.ConnectionData = connection;
			}

			return connection;
		}

		private ConnectionType DetermineConnectionType(Node sourceNode, Node targetNode, Edge edge)
		{
			// Entry node connections are always transitions
			if (sourceNode is EntryNodeView)
			{
				return ConnectionType.Transition;
			}

			// SubStateMachine node connections: Exit port is transition, exposed data ports use type-based logic
			if (sourceNode is SubStateMachineNodeView)
			{
				var sourcePortName = edge.output?.portName;
				if (sourcePortName == "Exit")
				{
					return ConnectionType.Transition;
				}

				// Exposed output data port — check target
				if (targetNode is BlackboardVariableNodeView subTargetVarNode &&
					subTargetVarNode.NodeData.Mode == BlackboardNodeMode.Set)
				{
					return ConnectionType.BlackboardWrite;
				}

				return ConnectionType.Data;
			}

			// PortalIn node connections are always transitions
			if (sourceNode is PortalInNodeView)
			{
				return ConnectionType.Transition;
			}

			// Check if source is a BlackboardVariableNode in Get mode
			if (sourceNode is BlackboardVariableNodeView sourceVarNode &&
				sourceVarNode.NodeData.Mode == BlackboardNodeMode.Get)
			{
				return ConnectionType.BlackboardRead;
			}

			// Check if target is a BlackboardVariableNode in Set mode
			if (targetNode is BlackboardVariableNodeView targetVarNode &&
				targetVarNode.NodeData.Mode == BlackboardNodeMode.Set)
			{
				return ConnectionType.BlackboardWrite;
			}

			// Check if the port type is Action (transition) or data
			var outputPortType = edge.output.portType;
			if (ActionTypeUtils.IsActionType(outputPortType))
			{
				return ConnectionType.Transition;
			}

			// Default to Data for non-Action types between states
			if (outputPortType != null)
			{
				return ConnectionType.Data;
			}

			return ConnectionType.Transition;
		}

		public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
		{
			var compatiblePorts = new List<Port>();

			if (_isPlayModeLocked) return compatiblePorts;

			ports.ForEach(port =>
			{
				// Cannot connect to self
				if (startPort.node == port.node)
				{
					return;
				}

				// Must be opposite directions
				if (startPort.direction == port.direction)
				{
					return;
				}

				// Check type compatibility
				if (ArePortsCompatible(startPort, port))
				{
					compatiblePorts.Add(port);
				}
			});

			return compatiblePorts;
		}

		private bool ArePortsCompatible(Port a, Port b)
		{
			var isATransition = IsTransitionPort(a);
			var isBTransition = IsTransitionPort(b);
			var isAFlow = IsFlowInputPort(a);
			var isBFlow = IsFlowInputPort(b);
			var isAData = IsDataPort(a);
			var isBData = IsDataPort(b);

			// Rule 1: Transition ports can ONLY connect to flow input ports
			if (isATransition) return isBFlow;
			if (isBTransition) return isAFlow;

			// Rule 2: Flow input ports can ONLY connect to transition ports (already handled above)
			if (isAFlow || isBFlow) return false;

			// Rule 3: Data ports can only connect to other data ports with compatible types
			if (isAData && isBData)
			{
				var sourcePort = a.direction == Direction.Output ? a : b;
				var targetPort = a.direction == Direction.Input ? a : b;
				return AreTypesCompatible(sourcePort.portType, targetPort.portType);
			}

			// Rule 4: If one is data and the other isn't properly categorized, block it
			if (isAData || isBData) return false;

			// Fallback: If no categories match (uncategorized ports), check types as before
			var source = a.direction == Direction.Output ? a : b;
			var target = a.direction == Direction.Input ? a : b;
			return AreTypesCompatible(source.portType, target.portType);
		}

		private bool IsTransitionPort(Port port)
		{
			return port.ClassListContains("transition-port");
		}

		private bool IsFlowInputPort(Port port)
		{
			return port.ClassListContains("flow-port") && port.direction == Direction.Input;
		}

		private bool IsDataPort(Port port)
		{
			return port.ClassListContains("data-input-port") || port.ClassListContains("data-output-port");
		}

		private bool AreTypesCompatible(Type sourceType, Type targetType)
		{
			if (sourceType == null || targetType == null)
			{
				return false;
			}

			// Exact type match
			if (sourceType == targetType)
			{
				return true;
			}

			// object output can connect to any input — runtime handles actual type validation
			if (sourceType == typeof(object))
			{
				return true;
			}

			// Target can accept from source if source is assignable to target
			if (targetType.IsAssignableFrom(sourceType))
			{
				return true;
			}

			// Special case: numeric conversions
			if (IsNumericType(sourceType) && IsNumericType(targetType))
			{
				// Allow implicit numeric conversions (int -> float, etc.)
				return CanConvertNumeric(sourceType, targetType);
			}

			return false;
		}

		private bool IsNumericType(Type type)
		{
			return type == typeof(int) ||
				   type == typeof(float) ||
				   type == typeof(double) ||
				   type == typeof(long) ||
				   type == typeof(short) ||
				   type == typeof(byte) ||
				   type == typeof(uint) ||
				   type == typeof(ulong) ||
				   type == typeof(ushort) ||
				   type == typeof(decimal);
		}

		private static readonly Dictionary<Type, Type[]> NumericConversions = new()
		{
			{ typeof(byte), new[] { typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) } },
			{ typeof(short), new[] { typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) } },
			{ typeof(int), new[] { typeof(long), typeof(float), typeof(double), typeof(decimal) } },
			{ typeof(long), new[] { typeof(float), typeof(double), typeof(decimal) } },
			{ typeof(float), new[] { typeof(double) } },
			{ typeof(uint), new[] { typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) } },
			{ typeof(ushort), new[] { typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) } },
			{ typeof(ulong), new[] { typeof(float), typeof(double), typeof(decimal) } }
		};

		private static bool CanConvertNumeric(Type sourceType, Type targetType)
		{
			if (NumericConversions.TryGetValue(sourceType, out var allowedTargets))
			{
				return Array.IndexOf(allowedTargets, targetType) >= 0;
			}

			return false;
		}

		private new string SerializeGraphElements(IEnumerable<GraphElement> elements)
		{
			var clipboardData = new ClipboardData();
			var selectedNodeIds = new HashSet<string>();

			// Collect nodes
			foreach (var element in elements)
			{
				if (element is INodeDataProvider nodeProvider)
				{
					var nodeData = nodeProvider.NodeData;
					// Skip entry nodes - there can only be one
					if (nodeData is EntryNode) continue;

					selectedNodeIds.Add(nodeData.Id);
					var clipNode = new ClipboardNode
					{
						OriginalId = nodeData.Id,
						NodeType = nodeData.NodeType,
						JsonData = JsonUtility.ToJson(nodeData)
					};
					clipboardData.Nodes.Add(clipNode);
				}
			}

			// Collect edges (only those between selected nodes)
			foreach (var element in elements)
			{
				if (element is Edge edge)
				{
					var sourceNode = edge.output?.node as INodeDataProvider;
					var targetNode = edge.input?.node as INodeDataProvider;

					if (sourceNode != null && targetNode != null &&
						selectedNodeIds.Contains(sourceNode.NodeData.Id) &&
						selectedNodeIds.Contains(targetNode.NodeData.Id))
					{
						// Find the connection in the graph
						var connections = _asset.GetConnectionsFromNode(sourceNode.NodeData.Id);
						var connection = connections.FirstOrDefault(c => c.TargetNodeId == targetNode.NodeData.Id);

						if (connection != null)
						{
							var clipConnection = new ClipboardConnection
							{
								SourceNodeId = connection.SourceNodeId,
								SourcePortName = connection.SourcePortName,
								TargetNodeId = connection.TargetNodeId,
								TargetPortName = connection.TargetPortName,
								ConnectionType = connection.ConnectionType
							};
							clipboardData.Connections.Add(clipConnection);
						}
					}
				}
			}

			if (clipboardData.Nodes.Count == 0)
				return "";

			return JsonUtility.ToJson(clipboardData);
		}

		private new bool CanPasteSerializedData(string data)
		{
			if (_isPlayModeLocked) return false;
			if (string.IsNullOrEmpty(data)) return false;

			try
			{
				var clipboardData = JsonUtility.FromJson<ClipboardData>(data);
				return clipboardData != null && clipboardData.Nodes.Count > 0;
			}
			catch (ArgumentException)
			{
				return false;
			}
		}

		private void UnserializeAndPaste(string operationName, string data)
		{
			if (_asset == null || string.IsNullOrEmpty(data)) return;

			ClipboardData clipboardData;
			try
			{
				clipboardData = JsonUtility.FromJson<ClipboardData>(data);
			}
			catch (ArgumentException)
			{
				return;
			}

			if (clipboardData == null || clipboardData.Nodes.Count == 0) return;

			Undo.RecordObject(_asset, "Paste Nodes");

			// Map from original IDs to new IDs
			var idMapping = new Dictionary<string, string>();

			// Calculate the center of copied nodes to offset to mouse position
			var nodePositions = new List<Vector2>();
			foreach (var clipNode in clipboardData.Nodes)
			{
				var tempNode = DeserializeNode(clipNode.NodeType, clipNode.JsonData);
				if (tempNode != null)
				{
					nodePositions.Add(tempNode.Position);
				}
			}

			// Calculate centroid of copied nodes
			var centroid = Vector2.zero;
			if (nodePositions.Count > 0)
			{
				foreach (var pos in nodePositions)
				{
					centroid += pos;
				}
				centroid /= nodePositions.Count;
			}

			// Offset from centroid to mouse position
			var pasteOffset = _lastMousePosition - centroid;

			// Create new nodes
			foreach (var clipNode in clipboardData.Nodes)
			{
				var newNode = DeserializeNode(clipNode.NodeType, clipNode.JsonData);
				if (newNode == null) continue;

				// Generate new ID
				var newId = System.Guid.NewGuid().ToString();
				idMapping[clipNode.OriginalId] = newId;

				// Update node with new ID and offset position
				newNode.Id = newId;
				newNode.Position += pasteOffset;

				// Add to graph
				_asset.AddNode(newNode);

				// Create view
				var nodeView = CreateNodeView(newNode);
				if (nodeView != null)
				{
					AddElement(nodeView);
					_nodeViews[newNode.Id] = nodeView;
					AttachNodeSizeTracker(newNode.Id, nodeView);
				}
			}

			// Recreate connections with new IDs
			foreach (var clipConn in clipboardData.Connections)
			{
				if (!idMapping.TryGetValue(clipConn.SourceNodeId, out var newSourceId) ||
					!idMapping.TryGetValue(clipConn.TargetNodeId, out var newTargetId))
				{
					continue;
				}

				var connection = new Connection(
					newSourceId,
					clipConn.SourcePortName,
					newTargetId,
					clipConn.TargetPortName,
					clipConn.ConnectionType
				);

				_asset.AddConnection(connection);

				// Create edge view
				var edge = CreateEdgeView(connection);
				if (edge != null)
				{
					AddElement(edge);
					_edgeViews[connection.Id] = edge;
				}
			}

			MarkAssetDirty();

			// Select the newly pasted nodes
			ClearSelection();
			foreach (var newId in idMapping.Values)
			{
				if (_nodeViews.TryGetValue(newId, out var nodeView))
				{
					AddToSelection(nodeView);
				}
			}
		}

		private GraphNode DeserializeNode(GraphNodeType nodeType, string jsonData)
		{
			try
			{
				return nodeType switch
				{
					GraphNodeType.State => JsonUtility.FromJson<StateNode>(jsonData),
					GraphNodeType.Exit => JsonUtility.FromJson<ExitNode>(jsonData),
					GraphNodeType.PortalIn => JsonUtility.FromJson<PortalInNode>(jsonData),
					GraphNodeType.PortalOut => JsonUtility.FromJson<PortalOutNode>(jsonData),
					GraphNodeType.SubStateMachine => JsonUtility.FromJson<SubStateMachineNode>(jsonData),
					_ => null
				};
			}
			catch (ArgumentException)
			{
				return null;
			}
		}

		private void OnMouseUp(MouseUpEvent evt)
		{
			var selectedNode = selection.Count == 1 && selection[0] is INodeDataProvider provider
				? provider.NodeData
				: null;

			OnSelectionChanged?.Invoke(selectedNode);

			// Fire group selection event
			var selectedGroup = selection.Count == 1 && selection[0] is GraphRegionView regionView
				? regionView
				: null;

			OnGroupSelectionChanged?.Invoke(selectedGroup);

			// Highlight paired portal nodes when a portal node is selected
			HighlightPairedPortalNodes(selectedNode);

			// Update edge handle visibility based on selection
			UpdateEdgeHandleVisibility();
		}

		/// <summary>
		/// Updates the handle visibility for all selected edges.
		/// Handles are only shown when a single edge is selected (no nodes).
		/// </summary>
		private void UpdateEdgeHandleVisibility()
		{
			foreach (var element in selection)
			{
				if (element is TransitionEdgeView edgeView)
				{
					edgeView.UpdateHandleVisibilityBasedOnSelection();
				}
			}
		}

		private void OnMouseMove(MouseMoveEvent evt)
		{
			// Convert mouse position to graph coordinates for paste operations
			_lastMousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
		}

		/// <summary>
		/// Detects pan gestures and fires OnViewManipulating for real-time sync.
		/// This supplements viewTransformChanged which only fires on gesture completion.
		/// </summary>
		private void OnPointerMoveForPanSync(PointerMoveEvent evt)
		{
			// Check if a pan gesture is in progress:
			// - Middle mouse button pressed, OR
			// - Alt + Left mouse button pressed (alternative pan gesture)
			const int middleButton = 4; // 1 << 2
			const int leftButton = 1;   // 1 << 0

			var isMiddleButtonPressed = (evt.pressedButtons & middleButton) != 0;
			var isAltLeftPressed = (evt.pressedButtons & leftButton) != 0 && evt.altKey;

			if (isMiddleButtonPressed || isAltLeftPressed)
			{
				OnViewManipulating?.Invoke(this);
			}
		}

		/// <summary>
		/// Detects zoom gestures (scroll wheel) and fires OnViewManipulating for real-time sync.
		/// </summary>
		private void OnWheelForZoomSync(WheelEvent evt)
		{
			// Scroll wheel is used for zooming - fire the event after ContentZoomer processes it
			// Schedule for next frame to ensure the zoom has been applied
			schedule.Execute(() => OnViewManipulating?.Invoke(this));
		}

		private void OnViewTransformChanged(GraphView graphView)
		{
			// Don't save during asset loading to prevent cross-asset contamination
			if (_isLoadingAsset) return;

			// Use explicit Unity null check - the ?. operator bypasses Unity's == overload
			// and won't detect destroyed objects, causing MissingReferenceException
			if (_asset == null || _asset.GraphViewData == null) return;

			// Wrap in try-catch because this callback can fire after the asset is destroyed
			// (e.g., user deletes the asset file while the graph window is still open)
			try
			{
				// Save the current view position and scale using resolvedStyle (Unity 6+ recommended approach)
				var translate = contentViewContainer.resolvedStyle.translate;
				var scale = contentViewContainer.resolvedStyle.scale;

				_asset.GraphViewData.ViewPosition = new Vector3(translate.x, translate.y, 0);
				_asset.GraphViewData.ViewScale = scale.value;

				// Mark dirty but don't save immediately (will be saved when asset is explicitly saved)
				EditorUtility.SetDirty(_asset);
			}
			catch (MissingReferenceException)
			{
				// Asset was destroyed externally - clear our reference to prevent further errors
				_asset = null;
			}
		}

		/// <summary>
		/// Refreshes the orphan status of all portal nodes in the graph.
		/// Call this when a portal node's channel changes or when portal nodes are added/removed.
		/// </summary>
		public void RefreshPortalNodeOrphanStatus()
		{
			foreach (var nodeView in _nodeViews.Values)
			{
				if (nodeView is PortalInNodeView portalInView)
				{
					portalInView.UpdateOrphanStatus();
				}
				else if (nodeView is PortalOutNodeView portalOutView)
				{
					portalOutView.UpdateOrphanStatus();
				}
			}
		}

		/// <summary>
		/// When a portal node is selected, briefly highlight all paired portal nodes with the same channel.
		/// </summary>
		private void HighlightPairedPortalNodes(GraphNode selectedNode)
		{
			if (selectedNode == null) return;

			int? channelId = selectedNode switch
			{
				PortalInNode portalIn => portalIn.ChannelId,
				PortalOutNode portalOut => portalOut.ChannelId,
				_ => null
			};

			if (!channelId.HasValue) return;

			// Flash all paired portal nodes (excluding the selected one)
			foreach (var kvp in _nodeViews)
			{
				if (kvp.Key == selectedNode.Id) continue;

				if (kvp.Value is PortalInNodeView portalInView && portalInView.NodeData.ChannelId == channelId.Value)
				{
					portalInView.FlashHighlight();
				}
				else if (kvp.Value is PortalOutNodeView portalOutView && portalOutView.NodeData.ChannelId == channelId.Value)
				{
					portalOutView.FlashHighlight();
				}
			}
		}

		// Sub-state machine navigation
		public void NavigateToSubStateMachine(StateMachineGraph subGraph)
		{
			if (_asset != null)
			{
				_navigationStack.Push(_asset);
			}

			PopulateFromAsset(subGraph);
			OnNavigationChanged?.Invoke();
		}

		public void NavigateBack()
		{
			if (_navigationStack.Count > 0)
			{
				var parent = _navigationStack.Pop();
				PopulateFromAsset(parent);
				OnNavigationChanged?.Invoke();
			}
		}

		public Stack<StateMachineGraph> GetNavigationStack()
		{
			return _navigationStack;
		}

		/// <summary>
		/// Navigates to a specific level in the breadcrumb trail.
		/// Level 0 = root (original graph), Level 1 = first sub-graph, etc.
		/// </summary>
		public void NavigateToLevel(int level)
		{
			if (_navigationStack.Count == 0) return;

			// Convert stack to list for easier access (bottom to top = 0 to N)
			var stackList = _navigationStack.ToList();
			stackList.Reverse(); // Now index 0 is the root

			// Level 0 = root, which is the first item in the stack
			// We need to pop until we reach the target level
			var targetGraph = level < stackList.Count ? stackList[level] : null;
			if (targetGraph == null) return;

			// Pop the stack until we have the right number of items
			while (_navigationStack.Count > level)
			{
				_navigationStack.Pop();
			}

			PopulateFromAsset(targetGraph);
			OnNavigationChanged?.Invoke();
		}

		/// <summary>
		/// Adds a node view to the graph and tracks it internally.
		/// Used for programmatically creating nodes (e.g., from drag-and-drop).
		/// </summary>
		public void AddNodeView(Node nodeView, string nodeId)
		{
			AddElement(nodeView);
			_nodeViews[nodeId] = nodeView;
		}

		/// <summary>
		/// Adds an edge view to the graph and tracks it internally.
		/// Used for programmatically creating edges (e.g., from drag-and-drop).
		/// </summary>
		public void AddEdgeView(Edge edgeView, string connectionId)
		{
			AddElement(edgeView);
			_edgeViews[connectionId] = edgeView;
		}

		/// <summary>
		/// Highlights the currently active state in the graph.
		/// </summary>
		public void HighlightActiveState(string nodeId)
		{
			if (DebugMode)
			{
				Debug.Log($"[GraphView] HighlightActiveState: nodeId={nodeId}, _nodeViews.Count={_nodeViews.Count}");
			}

			var found = false;
			foreach (var kvp in _nodeViews)
			{
				if (kvp.Value is IActiveStateNode activeNode)
				{
					var shouldHighlight = kvp.Key == nodeId;
					if (shouldHighlight)
					{
						found = true;
						if (DebugMode)
						{
							Debug.Log($"[GraphView] Highlighting node: {kvp.Key} ({kvp.Value.GetType().Name})");
						}
					}
					activeNode.SetActiveState(shouldHighlight);
				}
			}

			if (DebugMode && !found && !string.IsNullOrEmpty(nodeId))
			{
				Debug.LogWarning($"[GraphView] Node {nodeId} NOT FOUND in _nodeViews!");
			}

			// Center on the active state if enabled
			if (AutoCenterOnActiveState && !string.IsNullOrEmpty(nodeId))
			{
				CenterOnState(nodeId, UseLerpAnimation);
			}
		}

		public void ClearAllHighlights()
		{
			foreach (var nodeView in _nodeViews.Values)
			{
				// Exit nodes need special handling - they don't respond to SetActiveState(false)
				if (nodeView is ExitNodeView exitNode)
				{
					exitNode.ForceDeactivate();
				}
				else if (nodeView is IActiveStateNode activeNode)
				{
					activeNode.SetActiveState(false);
				}
			}
		}

		/// <summary>
		/// Updates runtime blackboard values on all BlackboardVariableNodeViews.
		/// </summary>
		public void UpdateRuntimeBlackboardValues(Nonatomic.VSM.Blackboard.Blackboard blackboard)
		{
			foreach (var nodeView in _nodeViews.Values)
			{
				if (nodeView is BlackboardVariableNodeView varNodeView)
				{
					if (blackboard != null)
					{
						varNodeView.UpdateRuntimeValue(blackboard);
					}
					else
					{
						varNodeView.ClearRuntimeValue();
					}
				}
			}
		}

		/// <summary>
		/// Briefly flashes a highlight on a node (for transient nodes like portals).
		/// </summary>
		public void FlashHighlight(string nodeId)
		{
			if (!_nodeViews.TryGetValue(nodeId, out var nodeView)) return;

			if (nodeView is IActiveStateNode activeNode)
			{
				activeNode.FlashHighlight();
			}
		}

		/// <summary>
		/// Flashes the breakpoint indicator on the given node, if it is shown in this view.
		/// No-op when the node is not part of this graph view.
		/// </summary>
		public void FlashBreakpointHit(string nodeId)
		{
			if (string.IsNullOrEmpty(nodeId)) return;

			// A transition breakpoint is drawn on its edge, so flash the edge rather than the node.
			var breakpoint = CurrentAsset?.GraphViewData?.GetBreakpointForNode(nodeId);
			if (breakpoint != null && breakpoint.Type == BreakpointType.OnTransition)
			{
				var edge = edges.ToList()
					.OfType<TransitionEdgeView>()
					.FirstOrDefault(e => e.ConnectionData != null
						&& e.ConnectionData.TargetNodeId == nodeId
						&& e.ConnectionData.SourcePortName == breakpoint.TransitionPortName);
				edge?.FlashBreakpointHit();
				return;
			}

			if (_nodeViews.TryGetValue(nodeId, out var nodeView) && nodeView is StateNodeView stateNodeView)
			{
				stateNodeView.FlashBreakpointHit();
			}
		}

		public void CenterOnState(string nodeId, bool animate = true)
		{
			if (!_nodeViews.TryGetValue(nodeId, out var nodeView))
			{
				return;
			}

			// Get the node's center position in content coordinates
			var nodeRect = nodeView.GetPosition();
			var nodeCenter = nodeRect.center;

			// Get current zoom scale
			var currentScale = contentViewContainer.resolvedStyle.scale;
			var scaleVector = new Vector3(currentScale.value.x, currentScale.value.y, 1);
			var zoom = currentScale.value.x;

			// Get the viewport size (the visible area of the graph view)
			var viewportWidth = layout.width;
			var viewportHeight = layout.height;

			// Calculate the viewport center
			var viewportCenterX = viewportWidth * 0.5f;
			var viewportCenterY = viewportHeight * 0.5f;

			// Calculate the position that would place the node center at the viewport center
			// viewTransform.position = viewportCenter - nodeCenter * scale
			var targetX = viewportCenterX - nodeCenter.x * zoom;
			var targetY = viewportCenterY - nodeCenter.y * zoom;
			var newPosition = new Vector3(targetX, targetY, 0);

			if (animate && UseLerpAnimation && LerpDuration > 0)
			{
				StartLerpAnimation(newPosition, scaleVector);
			}
			else
			{
				StopLerpAnimation();
				UpdateViewTransform(newPosition, scaleVector);
			}
		}

		/// <summary>
		/// Centers the view on the origin (0, 0).
		/// </summary>
		public new void FrameOrigin()
		{
			// Reset view transform first to ensure clean state
			UpdateViewTransform(Vector3.zero, Vector3.one);

			// Now set position to center of viewport
			// This puts content coordinate (0,0) at the center of the GraphView
			var center = new Vector3(layout.width * 0.5f, layout.height * 0.5f, 0);
			UpdateViewTransform(center, Vector3.one);
		}

		/// <summary>
		/// Centers the view on the entry node if one exists.
		/// </summary>
		public void FrameEntryNode(bool animate = true)
		{
			if (_asset == null || string.IsNullOrEmpty(_asset.EntryNodeId)) return;

			CenterOnState(_asset.EntryNodeId, animate);
		}

		/// <summary>
		/// Selects a node by its ID.
		/// </summary>
		public void SelectNode(string nodeId)
		{
			if (!_nodeViews.TryGetValue(nodeId, out var nodeView)) return;

			ClearSelection();
			AddToSelection(nodeView);
		}

		/// <summary>
		/// Selects a region by its ID.
		/// </summary>
		public void SelectRegion(string regionId)
		{
			if (!_regionViews.TryGetValue(regionId, out var regionView)) return;

			ClearSelection();
			AddToSelection(regionView);
		}

		/// <summary>
		/// Centers the view on a region by its ID.
		/// </summary>
		public void CenterOnRegion(string regionId, bool animate = true)
		{
			if (!_regionViews.TryGetValue(regionId, out var regionView)) return;

			var regionRect = regionView.GetPosition();
			var regionCenter = regionRect.center;

			var currentScale = contentViewContainer.resolvedStyle.scale;
			var scaleVector = new Vector3(currentScale.value.x, currentScale.value.y, 1);
			var zoom = currentScale.value.x;

			var viewportCenterX = layout.width * 0.5f;
			var viewportCenterY = layout.height * 0.5f;

			var targetX = viewportCenterX - regionCenter.x * zoom;
			var targetY = viewportCenterY - regionCenter.y * zoom;
			var newPosition = new Vector3(targetX, targetY, 0);

			if (animate && UseLerpAnimation && LerpDuration > 0)
			{
				StartLerpAnimation(newPosition, scaleVector);
			}
			else
			{
				UpdateViewTransform(newPosition, scaleVector);
			}
		}

		/// <summary>
		/// Schedules FrameOrigin to run after layout is computed.
		/// Uses GeometryChangedEvent to ensure the layout rect is valid.
		/// </summary>
		private void ScheduleFrameOrigin()
		{
			// Check if layout is already valid
			if (layout.width > 0 && layout.height > 0)
			{
				// Still need to delay slightly to ensure all elements are positioned
				schedule.Execute(FrameOrigin);
				return;
			}

			// Wait for geometry to be computed
			void OnGeometryChanged(GeometryChangedEvent evt)
			{
				UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
				// Delay one more frame to ensure everything is settled
				schedule.Execute(FrameOrigin);
			}

			RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
		}

		/// <summary>
		/// Schedules FrameEntryNode to run after layout is computed.
		/// Uses GeometryChangedEvent to ensure the layout rect is valid.
		/// </summary>
		private void ScheduleFrameEntryNode()
		{
			// Check if layout is already valid
			if (layout.width > 0 && layout.height > 0)
			{
				// Still need to delay slightly to ensure all elements are positioned
				schedule.Execute(() => FrameEntryNode(false));
				return;
			}

			// Wait for geometry to be computed
			void OnGeometryChanged(GeometryChangedEvent evt)
			{
				UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
				// Delay one more frame to ensure everything is settled
				schedule.Execute(() => FrameEntryNode(false));
			}

			RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
		}

		/// <summary>
		/// Creates a bookmark at the current view position.
		/// </summary>
		public GraphBookmark CreateBookmark(string name)
		{
			if (_asset?.GraphViewData == null) return null;

			var translate = contentViewContainer.resolvedStyle.translate;
			var scale = contentViewContainer.resolvedStyle.scale;
			var viewPosition = new Vector3(translate.x, translate.y, 0);
			var viewScale = new Vector3(scale.value.x, scale.value.y, 1);

			var bookmark = new GraphBookmark(name, viewPosition, viewScale, scale.value.x);

			// Store viewport center in graph coordinates for the visual marker
			var graphCenterX = (layout.width / 2f - translate.x) / scale.value.x;
			var graphCenterY = (layout.height / 2f - translate.y) / scale.value.y;
			bookmark.GraphPosition = new Vector2(graphCenterX, graphCenterY);

			_asset.GraphViewData.AddBookmark(bookmark);
			EditorUtility.SetDirty(_asset);

			RefreshBookmarkMarkers();

			return bookmark;
		}

		/// <summary>
		/// Navigates to a saved bookmark.
		/// </summary>
		public void GoToBookmark(GraphBookmark bookmark, bool animate = true)
		{
			if (bookmark == null) return;

			if (animate && UseLerpAnimation && LerpDuration > 0)
			{
				StartLerpAnimation(bookmark.ViewPosition, bookmark.ViewScale);
			}
			else
			{
				StopLerpAnimation();
				UpdateViewTransform(bookmark.ViewPosition, bookmark.ViewScale);
			}
		}

		/// <summary>
		/// Deletes a bookmark.
		/// </summary>
		public void DeleteBookmark(string bookmarkId)
		{
			if (_asset?.GraphViewData == null) return;

			_asset.GraphViewData.RemoveBookmark(bookmarkId);
			EditorUtility.SetDirty(_asset);

			RefreshBookmarkMarkers();
		}

		/// <summary>
		/// Gets all bookmarks for the current graph.
		/// </summary>
		public IReadOnlyList<GraphBookmark> GetBookmarks()
		{
			return _asset?.GraphViewData?.Bookmarks ?? Array.Empty<GraphBookmark>();
		}

		/// <summary>
		/// Rebuilds bookmark visual markers in the graph.
		/// </summary>
		public void RefreshBookmarkMarkers()
		{
			// Remove existing markers
			foreach (var marker in _bookmarkMarkers)
			{
				marker.RemoveFromHierarchy();
			}
			_bookmarkMarkers.Clear();

			// Create markers for all bookmarks that have a graph position
			var bookmarks = GetBookmarks();
			foreach (var bookmark in bookmarks)
			{
				if (bookmark.GraphPosition == Vector2.zero && bookmark.ViewPosition != Vector3.zero)
				{
					// Compute graph position for legacy bookmarks without stored position.
					// Use a reasonable viewport estimate of 1000x600.
					var graphCenterX = (500f - bookmark.ViewPosition.x) / bookmark.ViewScale.x;
					var graphCenterY = (300f - bookmark.ViewPosition.y) / bookmark.ViewScale.y;
					bookmark.GraphPosition = new Vector2(graphCenterX, graphCenterY);
				}

				var marker = new BookmarkMarkerView(bookmark, this);
				contentViewContainer.Add(marker);
				_bookmarkMarkers.Add(marker);
			}
		}

		/// <summary>
		/// Gets a bookmark by slot number (1-9).
		/// </summary>
		public GraphBookmark GetBookmarkBySlot(int slot)
		{
			var bookmarks = GetBookmarks();
			foreach (var bookmark in bookmarks)
			{
				if (bookmark.Slot == slot)
				{
					return bookmark;
				}
			}
			return null;
		}

		/// <summary>
		/// Sets a bookmark to a specific slot, clearing any existing bookmark in that slot.
		/// </summary>
		public void SetBookmarkSlot(int slot, string bookmarkId)
		{
			if (_asset?.GraphViewData == null) return;

			var bookmarks = GetBookmarks();
			foreach (var bookmark in bookmarks)
			{
				if (bookmark.Slot == slot && bookmark.Id != bookmarkId)
				{
					bookmark.Slot = 0;
				}
				else if (bookmark.Id == bookmarkId)
				{
					bookmark.Slot = slot;
				}
			}
			EditorUtility.SetDirty(_asset);
		}

		/// <summary>
		/// Creates a quick bookmark at the specified slot.
		/// </summary>
		public void CreateQuickBookmark(int slot)
		{
			if (_asset?.GraphViewData == null) return;

			// Clear any existing bookmark in this slot
			var existing = GetBookmarkBySlot(slot);
			if (existing != null)
			{
				existing.Slot = 0;
			}

			// Create new bookmark with slot
			var bookmark = CreateBookmark($"Bookmark {slot}");
			if (bookmark != null)
			{
				bookmark.Slot = slot;
				EditorUtility.SetDirty(_asset);
			}
		}

		/// <summary>
		/// Handles keyboard shortcuts for bookmarks and other functions.
		/// </summary>
		private void OnKeyDown(KeyDownEvent evt)
		{
			// A: Frame all nodes
			if (!evt.ctrlKey && !evt.shiftKey && !evt.altKey && evt.keyCode == KeyCode.A)
			{
				FrameAll();
				evt.StopPropagation();
				return;
			}

			// F: Frame selection
			if (!evt.ctrlKey && !evt.shiftKey && !evt.altKey && evt.keyCode == KeyCode.F)
			{
				FrameSelection();
				evt.StopPropagation();
				return;
			}

			// Ctrl+F: Toggle search bar
			if (evt.ctrlKey && !evt.shiftKey && !evt.altKey && evt.keyCode == KeyCode.F)
			{
				_window.NodeSearchBar?.Toggle();
				evt.StopPropagation();
				return;
			}

			// Ctrl+G: Group selected nodes into a region
			if (evt.ctrlKey && !evt.shiftKey && !evt.altKey && evt.keyCode == KeyCode.G && !_isPlayModeLocked)
			{
				GroupSelectedNodes();
				evt.StopPropagation();
				return;
			}

			// Ctrl+Shift+G: Ungroup selected region
			if (evt.ctrlKey && evt.shiftKey && !evt.altKey && evt.keyCode == KeyCode.G && !_isPlayModeLocked)
			{
				UngroupSelectedRegion();
				evt.StopPropagation();
				return;
			}

			// Ctrl+1-9: Jump to bookmark slot
			// Ctrl+Shift+1-9: Set bookmark at slot
			if (evt.ctrlKey && !evt.altKey)
			{
				var slot = GetSlotFromKeyCode(evt.keyCode);
				if (slot > 0)
				{
					if (evt.shiftKey)
					{
						// Set bookmark at slot
						CreateQuickBookmark(slot);
						evt.StopPropagation();
					}
					else
					{
						// Jump to bookmark slot
						var bookmark = GetBookmarkBySlot(slot);
						if (bookmark != null)
						{
							GoToBookmark(bookmark, UseLerpAnimation);
							evt.StopPropagation();
						}
					}
				}
			}
		}

		/// <summary>
		/// Converts a KeyCode to a bookmark slot number (1-9).
		/// </summary>
		private static int GetSlotFromKeyCode(KeyCode keyCode)
		{
			return keyCode switch
			{
				KeyCode.Alpha1 or KeyCode.Keypad1 => 1,
				KeyCode.Alpha2 or KeyCode.Keypad2 => 2,
				KeyCode.Alpha3 or KeyCode.Keypad3 => 3,
				KeyCode.Alpha4 or KeyCode.Keypad4 => 4,
				KeyCode.Alpha5 or KeyCode.Keypad5 => 5,
				KeyCode.Alpha6 or KeyCode.Keypad6 => 6,
				KeyCode.Alpha7 or KeyCode.Keypad7 => 7,
				KeyCode.Alpha8 or KeyCode.Keypad8 => 8,
				KeyCode.Alpha9 or KeyCode.Keypad9 => 9,
				_ => 0
			};
		}

		private void StartLerpAnimation(Vector3 targetPosition, Vector3 scale)
		{
			// Get current position from the view transform using resolvedStyle (Unity 6+ recommended)
			var translate = contentViewContainer.resolvedStyle.translate;
			var currentPosition = new Vector3(translate.x, translate.y, 0);

			// Don't animate if we're already very close to target
			if (Vector3.Distance(currentPosition, targetPosition) < 1f)
			{
				UpdateViewTransform(targetPosition, scale);
				return;
			}

			// If already animating, use the current interpolated position as the new start
			// and extend the duration for smoother transitions
			if (_isAnimating)
			{
				// Calculate current interpolated position
				var elapsed = (float)(EditorApplication.timeSinceStartup - _lerpStartTime);
				var progress = Mathf.Clamp01(elapsed / _currentLerpDuration);
				var smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
				currentPosition = Vector3.Lerp(_lerpStartPosition, _lerpTargetPosition, smoothProgress);

				// Slow down the lerp when redirecting mid-animation (1.5x duration)
				_currentLerpDuration = LerpDuration * 1.5f;
			}
			else
			{
				_currentLerpDuration = LerpDuration;
			}

			_lerpStartPosition = currentPosition;
			_lerpTargetPosition = targetPosition;
			_lerpScale = scale;
			_lerpProgress = 0f;
			_lerpStartTime = EditorApplication.timeSinceStartup;

			if (!_isAnimating)
			{
				_isAnimating = true;
				EditorApplication.update += UpdateLerpAnimation;
			}
		}

		private void StopLerpAnimation()
		{
			if (_isAnimating)
			{
				_isAnimating = false;
				EditorApplication.update -= UpdateLerpAnimation;
			}
		}

		private void UpdateLerpAnimation()
		{
			if (!_isAnimating) return;

			var elapsed = (float)(EditorApplication.timeSinceStartup - _lerpStartTime);
			_lerpProgress = Mathf.Clamp01(elapsed / _currentLerpDuration);

			// Use smooth step for easing
			var smoothProgress = Mathf.SmoothStep(0f, 1f, _lerpProgress);

			var currentPosition = Vector3.Lerp(_lerpStartPosition, _lerpTargetPosition, smoothProgress);
			UpdateViewTransform(currentPosition, _lerpScale);

			if (_lerpProgress >= 1f)
			{
				StopLerpAnimation();
			}
		}

		private void LoadStyleSheet()
		{
			// Try multiple paths for stylesheet loading
			string[] paths =
			{
				"Packages/com.nonatomic.visualstatemachinev3/Editor/Styles/StateMachineGraph.uss",
				"Packages/VisualStateMachineV3/Editor/Styles/StateMachineGraph.uss",
				"Assets/VisualStateMachineV3/Editor/Styles/StateMachineGraph.uss"
			};

			foreach (var path in paths)
			{
				var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
				if (styleSheet != null)
				{
					styleSheets.Add(styleSheet);
					return;
				}
			}

			// Try finding by searching
			var guids = AssetDatabase.FindAssets("StateMachineGraph t:StyleSheet");
			if (guids.Length > 0)
			{
				var path = AssetDatabase.GUIDToAssetPath(guids[0]);
				var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
				if (styleSheet != null)
				{
					styleSheets.Add(styleSheet);
				}
			}
		}
	}

	/// <summary>
	/// Interface for node views that provide GraphNode data.
	/// </summary>
	public interface INodeDataProvider
	{
		GraphNode NodeData { get; }
	}

	/// <summary>
	/// Interface for node views that provide ports.
	/// </summary>
	public interface IPortProvider
	{
		Port GetInputPort(string portName);
		Port GetOutputPort(string portName);
	}

	/// <summary>
	/// Data structure for clipboard serialization.
	/// </summary>
	[Serializable]
	public class ClipboardData
	{
		[SerializeField] private List<ClipboardNode> _nodes = new List<ClipboardNode>();
		[SerializeField] private List<ClipboardConnection> _connections = new List<ClipboardConnection>();

		public List<ClipboardNode> Nodes => _nodes;
		public List<ClipboardConnection> Connections => _connections;
	}

	/// <summary>
	/// Serialized node for clipboard.
	/// </summary>
	[Serializable]
	public class ClipboardNode
	{
		[SerializeField] private string _originalId;
		[SerializeField] private GraphNodeType _nodeType;
		[SerializeField] private string _jsonData;

		public string OriginalId { get => _originalId; set => _originalId = value; }
		public GraphNodeType NodeType { get => _nodeType; set => _nodeType = value; }
		public string JsonData { get => _jsonData; set => _jsonData = value; }
	}

	/// <summary>
	/// Serialized connection for clipboard.
	/// </summary>
	[Serializable]
	public class ClipboardConnection
	{
		[SerializeField] private string _sourceNodeId;
		[SerializeField] private string _sourcePortName;
		[SerializeField] private string _targetNodeId;
		[SerializeField] private string _targetPortName;
		[SerializeField] private ConnectionType _connectionType;

		public string SourceNodeId { get => _sourceNodeId; set => _sourceNodeId = value; }
		public string SourcePortName { get => _sourcePortName; set => _sourcePortName = value; }
		public string TargetNodeId { get => _targetNodeId; set => _targetNodeId = value; }
		public string TargetPortName { get => _targetPortName; set => _targetPortName = value; }
		public ConnectionType ConnectionType { get => _connectionType; set => _connectionType = value; }
	}
}
