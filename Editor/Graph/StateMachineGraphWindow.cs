using System.Collections.Generic;
using System.Linq;
using Nonatomic.VSM;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Editor.Debugging;
using Nonatomic.VSM.Editor.Runtime;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Main editor window for the visual state machine graph.
	/// </summary>
	public class StateMachineGraphWindow : EditorWindow
	{
		private StateMachineGraph _targetAsset;
		private StateMachineGraphView _primaryGraphView;
		private StateMachineGraphView _secondaryGraphView;
		private StateMachineGraphView _activeGraphView;
		private SplitViewContainer _splitViewContainer;
		private BlackboardView _blackboardView;
		private InspectorView _inspectorView;
		private GroupInspectorPanel _groupInspectorPanel;
		private ToolbarView _toolbarView;
		private CoordinatesDisplayView _coordinatesDisplayView;
		private MiniMapView _miniMapView;
		private StateListPanel _stateListPanel;
		private DebugTimelinePanel _debugTimelinePanel;
		private BreakpointListPanel _breakpointListPanel;
		private NodeSearchBar _nodeSearchBar;
		private bool _isUpdatingLinkScroll;
		private Vector3 _linkScrollOffset;
		private Vector3 _lastLinkedPrimaryPosition;
		private Vector3 _lastLinkedSecondaryPosition;

		// Follow sub-state machines feature
		private bool _followSubStateMachines;
		private RuntimeInstanceInfo _followedInstance;

		// Auto-select first instance on play mode
		private bool _autoSelectFirstInstance;

		/// <summary>
		/// When enabled, the graph view will automatically navigate into sub-state machines
		/// when they become active and navigate back when they complete.
		/// </summary>
		public bool FollowSubStateMachines
		{
			get => _followSubStateMachines;
			set => _followSubStateMachines = value;
		}

		private const string WindowTitle = "State Machine Graph";

		[MenuItem("Window/Visual State Machine/Graph Editor")]
		public static StateMachineGraphWindow OpenWindow()
		{
			var window = GetWindow<StateMachineGraphWindow>();
			window.titleContent = new GUIContent(WindowTitle);
			window.minSize = new Vector2(800, 600);
			return window;
		}

		[OnOpenAsset]
		public static bool OnOpenAsset(int instanceId, int line)
		{
			var asset = EditorUtility.EntityIdToObject(instanceId) as StateMachineGraph;
			if (asset != null)
			{
				OpenAsset(asset);
				return true;
			}

			return false;
		}

		public static void OpenAsset(StateMachineGraph asset)
		{
			var window = GetWindow<StateMachineGraphWindow>();
			var suffix = asset.IsVariant ? " [Variant]" : "";
			window.titleContent = new GUIContent($"SM: {asset.name}{suffix}");
			window.LoadAsset(asset);
		}

		private void OnEnable()
		{
			BuildUI();
			RegisterCallbacks();

			// Restore last opened asset if any
			if (_targetAsset != null)
			{
				LoadAsset(_targetAsset);
			}
		}

		private void OnDisable()
		{
			UnregisterCallbacks();
			_coordinatesDisplayView?.Cleanup();

			if (_miniMapView != null)
			{
				_miniMapView.OnVisibilityChanged -= OnMiniMapVisibilityChanged;
			}

			if (_primaryGraphView != null)
			{
				_primaryGraphView.OnGraphDataChanged -= OnPrimaryGraphDataChanged;
			}

			if (_secondaryGraphView != null)
			{
				_secondaryGraphView.OnGraphDataChanged -= OnSecondaryGraphDataChanged;
			}

			if (_splitViewContainer != null)
			{
				_splitViewContainer.OnSplitToggled -= OnSplitToggled;
				_splitViewContainer.OnActivePaneChanged -= OnActivePaneChanged;
				_splitViewContainer.OnLinkScrollToggled -= OnLinkScrollToggled;
			}
		}

		private VisualElement _noAssetOverlay;

		private void BuildUI()
		{
			// Load stylesheet with fallback paths
			var styleSheet = LoadStyleSheet();
			if (styleSheet != null)
			{
				rootVisualElement.styleSheets.Add(styleSheet);
			}
			else
			{
				Debug.LogWarning("StateMachineGraph.uss stylesheet not found");
			}

			rootVisualElement.style.flexDirection = FlexDirection.Column;
			rootVisualElement.style.flexGrow = 1;

			// Create toolbar
			_toolbarView = new ToolbarView(this);
			rootVisualElement.Add(_toolbarView);

			// Create main container for graph and floating panels
			var mainContainer = new VisualElement();
			mainContainer.name = "main-container";
			mainContainer.style.flexGrow = 1;
			mainContainer.style.position = Position.Relative;
			rootVisualElement.Add(mainContainer);

			// Create split view container
			_splitViewContainer = new SplitViewContainer();
			_splitViewContainer.style.position = Position.Absolute;
			_splitViewContainer.style.top = 0;
			_splitViewContainer.style.left = 0;
			_splitViewContainer.style.right = 0;
			_splitViewContainer.style.bottom = 0;
			_splitViewContainer.OnSplitToggled += OnSplitToggled;
			_splitViewContainer.OnActivePaneChanged += OnActivePaneChanged;
			_splitViewContainer.OnLinkScrollToggled += OnLinkScrollToggled;
			mainContainer.Add(_splitViewContainer);

			// Create primary graph view
			_primaryGraphView = new StateMachineGraphView(this);
			_primaryGraphView.name = "primary-graph-view";
			_primaryGraphView.PaneId = "primary";
			_primaryGraphView.style.flexGrow = 1;
			_splitViewContainer.PrimaryPane.Add(_primaryGraphView);

			// Create secondary graph view (initially hidden via split container)
			_secondaryGraphView = new StateMachineGraphView(this);
			_secondaryGraphView.name = "secondary-graph-view";
			_secondaryGraphView.PaneId = "secondary";
			_secondaryGraphView.style.flexGrow = 1;
			_splitViewContainer.SecondaryPane.Add(_secondaryGraphView);

			// Set primary as active by default
			_activeGraphView = _primaryGraphView;

			// Subscribe to graph data changes for split view sync
			_primaryGraphView.OnGraphDataChanged += OnPrimaryGraphDataChanged;
			_secondaryGraphView.OnGraphDataChanged += OnSecondaryGraphDataChanged;

			// Create floating blackboard panel
			_blackboardView = new BlackboardView(this);
			_blackboardView.OnVisibilityChanged += OnPanelVisibilityChanged;
			mainContainer.Add(_blackboardView);

			// Create floating inspector panel
			_inspectorView = new InspectorView(this);
			_inspectorView.OnVisibilityChanged += OnPanelVisibilityChanged;
			mainContainer.Add(_inspectorView);

			// Create floating group inspector panel (hidden by default, shown on group selection)
			_groupInspectorPanel = new GroupInspectorPanel(this);
			_groupInspectorPanel.OnVisibilityChanged += OnPanelVisibilityChanged;
			_groupInspectorPanel.SetVisible(false);
			mainContainer.Add(_groupInspectorPanel);

			// Create coordinates display in bottom-right corner (uses active graph view)
			_coordinatesDisplayView = new CoordinatesDisplayView(_primaryGraphView);
			mainContainer.Add(_coordinatesDisplayView);

			// Create mini-map in bottom-left corner
			// Must be inside the GraphView hierarchy so MiniMap can find its ancestor GraphView
			_miniMapView = new MiniMapView(_primaryGraphView);
			_miniMapView.OnVisibilityChanged += OnMiniMapVisibilityChanged;
			_primaryGraphView.Add(_miniMapView);

			// Create state list panel
			_stateListPanel = new StateListPanel(this);
			_stateListPanel.OnVisibilityChanged += OnPanelVisibilityChanged;
			mainContainer.Add(_stateListPanel);

			// Create debug timeline panel
			_debugTimelinePanel = new DebugTimelinePanel(this);
			_debugTimelinePanel.OnVisibilityChanged += OnPanelVisibilityChanged;
			mainContainer.Add(_debugTimelinePanel);

			// Create breakpoint list panel
			_breakpointListPanel = new BreakpointListPanel(this);
			_breakpointListPanel.OnVisibilityChanged += OnPanelVisibilityChanged;
			_breakpointListPanel.OnNavigateToNode += OnNavigateToBreakpointNode;
			mainContainer.Add(_breakpointListPanel);

			// Create node search bar (Ctrl+F overlay)
			_nodeSearchBar = new NodeSearchBar(this);
			mainContainer.Add(_nodeSearchBar);

			// Register Ctrl+F shortcut at window level
			rootVisualElement.RegisterCallback<KeyDownEvent>(OnWindowKeyDown);

			// Create "no asset" overlay
			CreateNoAssetOverlay();
			UpdateOverlayVisibility();
		}

		private void OnPanelVisibilityChanged(FloatingPanel panel, bool visible)
		{
			_toolbarView?.SyncPanelToggles();
		}

		private void OnMiniMapVisibilityChanged(bool visible)
		{
			_toolbarView?.SyncPanelToggles();
		}

		private bool _isRefreshingPeerView;

		private void OnPrimaryGraphDataChanged()
		{
			RefreshPeerView(_primaryGraphView, _secondaryGraphView);
		}

		private void OnSecondaryGraphDataChanged()
		{
			RefreshPeerView(_secondaryGraphView, _primaryGraphView);
		}

		/// <summary>
		/// Repopulates the peer graph view after the source view modified the shared asset.
		/// Preserves the peer's current pan/zoom so the refresh is seamless.
		/// </summary>
		private void RefreshPeerView(StateMachineGraphView source, StateMachineGraphView peer)
		{
			if (_isRefreshingPeerView) return;
			if (peer == null) return;
			if (!(_splitViewContainer?.IsSplitEnabled ?? false)) return;
			if (peer.CurrentAsset == null) return;

			// Only sync when both views show the same graph
			if (source.CurrentAsset != peer.CurrentAsset) return;

			_isRefreshingPeerView = true;
			try
			{
				var savedPosition = peer.viewTransform.position;
				var savedScale = peer.viewTransform.scale;

				peer.PopulateFromAsset(peer.CurrentAsset);

				peer.UpdateViewTransform(savedPosition, savedScale);
			}
			finally
			{
				_isRefreshingPeerView = false;
			}
		}

		private void OnSplitToggled(bool isSplit)
		{
			if (isSplit && _targetAsset != null && _secondaryGraphView != null)
			{
				_secondaryGraphView.PopulateFromAsset(_targetAsset);
			}
			_toolbarView?.SyncPanelToggles();
		}

		private void OnActivePaneChanged(bool isPrimary)
		{
			_activeGraphView = isPrimary ? _primaryGraphView : _secondaryGraphView;

			// Update mini-map to show active view
			_miniMapView?.SetGraphView(_activeGraphView);

			// Update coordinates display
			_coordinatesDisplayView?.SetGraphView(_activeGraphView);

			// Update toolbar navigation for the active view
			_toolbarView?.UpdateNavigationUI();

			// Update panels with active view's current asset
			_blackboardView?.PopulateFromAsset(_activeGraphView?.CurrentAsset);
			_stateListPanel?.PopulateFromAsset(_activeGraphView?.CurrentAsset);
			_nodeSearchBar?.PopulateFromAsset(_activeGraphView?.CurrentAsset);
		}

		private void OnLinkScrollToggled(bool isLinked)
		{
			if (isLinked && _primaryGraphView != null && _secondaryGraphView != null)
			{
				_lastLinkedPrimaryPosition = _primaryGraphView.viewTransform.position;
				_lastLinkedSecondaryPosition = _secondaryGraphView.viewTransform.position;
			}

			_toolbarView?.SyncPanelToggles();
		}

		private void OnPrimaryViewTransformChanged(GraphView graphView)
		{
			SyncLinkedScroll(_primaryGraphView, _secondaryGraphView, ref _lastLinkedPrimaryPosition, ref _lastLinkedSecondaryPosition);
		}

		private void OnSecondaryViewTransformChanged(GraphView graphView)
		{
			SyncLinkedScroll(_secondaryGraphView, _primaryGraphView, ref _lastLinkedSecondaryPosition, ref _lastLinkedPrimaryPosition);
		}

		/// <summary>
		/// Called continuously during pan/zoom gestures in the primary view.
		/// Provides real-time sync unlike viewTransformChanged which only fires on completion.
		/// </summary>
		private void OnPrimaryViewManipulating(GraphView graphView)
		{
			SyncLinkedScroll(_primaryGraphView, _secondaryGraphView, ref _lastLinkedPrimaryPosition, ref _lastLinkedSecondaryPosition);
		}

		/// <summary>
		/// Called continuously during pan/zoom gestures in the secondary view.
		/// Provides real-time sync unlike viewTransformChanged which only fires on completion.
		/// </summary>
		private void OnSecondaryViewManipulating(GraphView graphView)
		{
			SyncLinkedScroll(_secondaryGraphView, _primaryGraphView, ref _lastLinkedSecondaryPosition, ref _lastLinkedPrimaryPosition);
		}

		/// <summary>
		/// Applies the pan delta from the source view to the target view so that
		/// both views move in parallel while preserving their independent positions.
		/// </summary>
		private void SyncLinkedScroll(
			StateMachineGraphView source,
			StateMachineGraphView target,
			ref Vector3 lastSourcePosition,
			ref Vector3 lastTargetPosition)
		{
			if (_isUpdatingLinkScroll) return;
			if (_splitViewContainer?.IsLinkScrollEnabled != true) return;
			if (!_splitViewContainer.IsSplitEnabled) return;
			if (source == null || target == null) return;

			_isUpdatingLinkScroll = true;
			try
			{
				var delta = source.viewTransform.position - lastSourcePosition;
				var newTargetPosition = lastTargetPosition + delta;

				target.UpdateViewTransform(newTargetPosition, source.viewTransform.scale);

				lastSourcePosition = source.viewTransform.position;
				lastTargetPosition = newTargetPosition;
			}
			finally
			{
				_isUpdatingLinkScroll = false;
			}
		}

		private void CreateNoAssetOverlay()
		{
			_noAssetOverlay = new VisualElement();
			_noAssetOverlay.style.position = Position.Absolute;
			_noAssetOverlay.style.top = 50;
			_noAssetOverlay.style.left = 0;
			_noAssetOverlay.style.right = 0;
			_noAssetOverlay.style.bottom = 0;
			_noAssetOverlay.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
			_noAssetOverlay.style.alignItems = Align.Center;
			_noAssetOverlay.style.justifyContent = Justify.Center;

			var container = new VisualElement();
			container.style.alignItems = Align.Center;

			var label = new Label("No State Machine Graph Loaded");
			label.style.fontSize = 18;
			label.style.color = new Color(0.7f, 0.7f, 0.7f);
			label.style.marginBottom = 20;
			container.Add(label);

			var buttonContainer = new VisualElement();
			buttonContainer.style.flexDirection = FlexDirection.Row;

			var createButton = new Button(() => CreateNewGraph()) { text = "Create New Graph" };
			createButton.style.paddingLeft = 20;
			createButton.style.paddingRight = 20;
			createButton.style.paddingTop = 10;
			createButton.style.paddingBottom = 10;
			createButton.style.marginRight = 10;
			buttonContainer.Add(createButton);

			var openButton = new Button(() => OpenExistingGraph()) { text = "Open Existing Graph" };
			openButton.style.paddingLeft = 20;
			openButton.style.paddingRight = 20;
			openButton.style.paddingTop = 10;
			openButton.style.paddingBottom = 10;
			buttonContainer.Add(openButton);

			container.Add(buttonContainer);

			var hintLabel = new Label("Or double-click a StateMachineGraph asset in the Project window");
			hintLabel.style.fontSize = 11;
			hintLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
			hintLabel.style.marginTop = 20;
			container.Add(hintLabel);

			_noAssetOverlay.Add(container);
			rootVisualElement.Add(_noAssetOverlay);
		}

		private void UpdateOverlayVisibility()
		{
			if (_noAssetOverlay != null)
			{
				_noAssetOverlay.style.display = _targetAsset == null ? DisplayStyle.Flex : DisplayStyle.None;
			}
		}

		private void CreateNewGraph()
		{
			var path = EditorUtility.SaveFilePanelInProject(
				"Create State Machine Graph",
				"NewStateMachine",
				"asset",
				"Choose a location to save the new state machine graph");

			if (string.IsNullOrEmpty(path))
			{
				return;
			}

			var graph = CreateInstance<StateMachineGraph>();
			graph.InitializeNewGraph();
			AssetDatabase.CreateAsset(graph, path);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			LoadAsset(graph);
		}

		private void OpenExistingGraph()
		{
			var path = EditorUtility.OpenFilePanel("Open State Machine Graph", "Assets", "asset");
			if (string.IsNullOrEmpty(path))
			{
				return;
			}

			// Convert to relative path
			if (path.StartsWith(Application.dataPath))
			{
				path = "Assets" + path.Substring(Application.dataPath.Length);
			}

			var graph = AssetDatabase.LoadAssetAtPath<StateMachineGraph>(path);
			if (graph != null)
			{
				LoadAsset(graph);
			}
			else
			{
				EditorUtility.DisplayDialog("Error", "Selected file is not a valid StateMachineGraph asset.", "OK");
			}
		}

		private void RegisterCallbacks()
		{
			if (_primaryGraphView != null)
			{
				_primaryGraphView.OnSelectionChanged += OnGraphSelectionChanged;
				_primaryGraphView.OnGroupSelectionChanged += OnGroupSelectionChanged;
				_primaryGraphView.OnNavigationChanged += OnNavigationChanged;
				_primaryGraphView.viewTransformChanged += OnPrimaryViewTransformChanged;
				_primaryGraphView.OnViewManipulating += OnPrimaryViewManipulating;
			}

			if (_secondaryGraphView != null)
			{
				_secondaryGraphView.OnSelectionChanged += OnGraphSelectionChanged;
				_secondaryGraphView.OnGroupSelectionChanged += OnGroupSelectionChanged;
				_secondaryGraphView.OnNavigationChanged += OnNavigationChanged;
				_secondaryGraphView.viewTransformChanged += OnSecondaryViewTransformChanged;
				_secondaryGraphView.OnViewManipulating += OnSecondaryViewManipulating;
			}

			Undo.undoRedoPerformed += OnUndoRedo;

			// Runtime tracking
			RuntimeInstanceTracker.OnInstanceStateChanged += OnRuntimeStateChanged;
			RuntimeInstanceTracker.OnInstanceRegistered += OnRuntimeInstanceRegistered;
			RuntimeInstanceTracker.OnInstanceUnregistered += OnRuntimeInstanceUnregistered;
			RuntimeInstanceTracker.OnInstancesCleared += OnRuntimeInstancesCleared;
			RuntimeInstanceTracker.OnNodeVisited += OnNodeVisited;
			RuntimeInstanceTracker.OnChildInstanceStarted += OnChildInstanceStarted;
			RuntimeInstanceTracker.OnChildInstanceStopped += OnChildInstanceStopped;
			EditorApplication.update += OnEditorUpdate;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			EditorApplication.pauseStateChanged += OnPauseStateChanged;
			BreakpointManager.OnBreakpointHit += OnBreakpointHit;

			// Selection sync
			Selection.selectionChanged += OnSelectionChanged;
		}

		private void UnregisterCallbacks()
		{
			if (_primaryGraphView != null)
			{
				_primaryGraphView.OnSelectionChanged -= OnGraphSelectionChanged;
				_primaryGraphView.OnGroupSelectionChanged -= OnGroupSelectionChanged;
				_primaryGraphView.OnNavigationChanged -= OnNavigationChanged;
				_primaryGraphView.viewTransformChanged -= OnPrimaryViewTransformChanged;
				_primaryGraphView.OnViewManipulating -= OnPrimaryViewManipulating;
			}

			if (_secondaryGraphView != null)
			{
				_secondaryGraphView.OnSelectionChanged -= OnGraphSelectionChanged;
				_secondaryGraphView.OnGroupSelectionChanged -= OnGroupSelectionChanged;
				_secondaryGraphView.OnNavigationChanged -= OnNavigationChanged;
				_secondaryGraphView.viewTransformChanged -= OnSecondaryViewTransformChanged;
				_secondaryGraphView.OnViewManipulating -= OnSecondaryViewManipulating;
			}

			Undo.undoRedoPerformed -= OnUndoRedo;

			// Runtime tracking
			RuntimeInstanceTracker.OnInstanceStateChanged -= OnRuntimeStateChanged;
			RuntimeInstanceTracker.OnInstanceRegistered -= OnRuntimeInstanceRegistered;
			RuntimeInstanceTracker.OnInstanceUnregistered -= OnRuntimeInstanceUnregistered;
			RuntimeInstanceTracker.OnInstancesCleared -= OnRuntimeInstancesCleared;
			RuntimeInstanceTracker.OnNodeVisited -= OnNodeVisited;
			RuntimeInstanceTracker.OnChildInstanceStarted -= OnChildInstanceStarted;
			RuntimeInstanceTracker.OnChildInstanceStopped -= OnChildInstanceStopped;
			EditorApplication.update -= OnEditorUpdate;
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.pauseStateChanged -= OnPauseStateChanged;
			BreakpointManager.OnBreakpointHit -= OnBreakpointHit;

			// Selection sync
			Selection.selectionChanged -= OnSelectionChanged;
		}

		private void OnRuntimeStateChanged(RuntimeInstanceInfo info)
		{
			var currentGraph = _activeGraphView?.CurrentAsset;
			if (currentGraph == null || !GraphsMatch(info.Graph, currentGraph)) return;

			// Only update highlight if this is the selected instance (or no instance selected yet)
			var selectedInstance = _toolbarView?.SelectedInstance;
			if (selectedInstance == null || selectedInstance == info)
			{
				if (!string.IsNullOrEmpty(info.CurrentStateNodeId))
				{
					// Highlight in both views if they show the same graph
					HighlightActiveStateInViews(info.CurrentStateNodeId);
					Repaint();
				}

				// Update runtime blackboard values on variable nodes
				UpdateRuntimeBlackboardValuesInViews(info.Instance?.Blackboard);
			}

			// Update the instance selector
			_toolbarView?.UpdateInstanceSelector();
		}

		private void OnRuntimeInstanceRegistered(RuntimeInstanceInfo info)
		{
			// Update the instance selector when new instances are registered
			_toolbarView?.UpdateInstanceSelector();

			var currentGraph = _activeGraphView?.CurrentAsset;
			if (currentGraph == null || !GraphsMatch(info.Graph, currentGraph))
			{
				return;
			}

			// Auto-select first instance when entering play mode with graph open
			if (_autoSelectFirstInstance)
			{
				_autoSelectFirstInstance = false;
				_toolbarView?.SelectInstance(info);

				// Select the runner's GameObject in the hierarchy
				if (info.Owner != null)
				{
					Selection.activeGameObject = info.Owner;
					EditorGUIUtility.PingObject(info.Owner);
				}
			}

			// If this is the first/only instance, highlight it
			var selectedInstance = _toolbarView?.SelectedInstance;
			if (selectedInstance == null || selectedInstance == info)
			{
				if (!string.IsNullOrEmpty(info.CurrentStateNodeId))
				{
					HighlightActiveStateInViews(info.CurrentStateNodeId);
					Repaint();
				}
				else if (_primaryGraphView?.AutoCenterOnActiveState == true)
				{
					// No current state yet, center on entry node as fallback
					CenterOnEntryNodeInViews();
					Repaint();
				}
			}
		}

		private void OnRuntimeInstanceUnregistered(RuntimeInstanceInfo info)
		{
			// Update the instance selector when instances are removed
			_toolbarView?.UpdateInstanceSelector();

			// If the unregistered instance was selected, the toolbar will auto-select another
			// and we need to update the highlight
			var selectedInstance = _toolbarView?.SelectedInstance;
			if (selectedInstance != null)
			{
				if (!string.IsNullOrEmpty(selectedInstance.CurrentStateNodeId))
				{
					HighlightActiveStateInViews(selectedInstance.CurrentStateNodeId);
					Repaint();
				}
			}
		}

		private void OnRuntimeInstancesCleared()
		{
			_primaryGraphView?.ClearAllHighlights();
			_secondaryGraphView?.ClearAllHighlights();
			_toolbarView?.UpdateInstanceSelector();
			Repaint();
		}

		private void OnNodeVisited(RuntimeInstanceInfo info, string nodeId, bool isTransient)
		{
			// Don't override graph highlighting when replay mode is active
			if (IsReplayActive) return;

			var currentGraph = _activeGraphView?.CurrentAsset;
			if (currentGraph == null || !GraphsMatch(info.Graph, currentGraph)) return;

			// Only update highlight if this is the selected instance (or no instance selected yet)
			var selectedInstance = _toolbarView?.SelectedInstance;
			if (selectedInstance == null || selectedInstance == info)
			{
				if (!string.IsNullOrEmpty(nodeId))
				{
					if (isTransient)
					{
						// Flash highlight for transient nodes (jump nodes) in both views
						_primaryGraphView?.FlashHighlight(nodeId);
						if (_splitViewContainer?.IsSplitEnabled == true)
						{
							_secondaryGraphView?.FlashHighlight(nodeId);
						}
					}
					else
					{
						// Persistent highlight for state nodes
						HighlightActiveStateInViews(nodeId);
					}
					Repaint();
				}
			}
		}

		private void OnBreakpointHit(string nodeId, string nodeName)
		{
			if (string.IsNullOrEmpty(nodeId)) return;

			// Flash + light a persistent highlight on the paused node in whichever view shows it.
			_primaryGraphView?.FlashBreakpointHit(nodeId);
			_primaryGraphView?.ShowBreakpointHit(nodeId);
			if (_splitViewContainer?.IsSplitEnabled == true)
			{
				_secondaryGraphView?.FlashBreakpointHit(nodeId);
				_secondaryGraphView?.ShowBreakpointHit(nodeId);
			}
			Repaint();
		}

		private void OnChildInstanceStarted(RuntimeInstanceInfo parentInfo, RuntimeInstanceInfo childInfo, StateMachineGraph childGraph)
		{
			if (!_followSubStateMachines) return;
			if (childGraph == null) return;

			// Only follow if we're currently viewing the parent's graph
			var currentGraph = _activeGraphView?.CurrentAsset;
			if (currentGraph == null || !GraphsMatch(parentInfo.Graph, currentGraph)) return;

			// Only follow if the parent is the selected instance
			var selectedInstance = _toolbarView?.SelectedInstance;
			if (selectedInstance != null && selectedInstance != parentInfo) return;

			// Track which instance we're following
			_followedInstance = parentInfo;

			// Navigate to the child graph
			_activeGraphView?.NavigateToSubStateMachine(childGraph);

			// Update the instance selector and select the child instance
			_toolbarView?.UpdateInstanceSelector();
			_toolbarView?.SelectInstance(childInfo);

			// Highlight the active state in the child graph
			if (!string.IsNullOrEmpty(childInfo.CurrentStateNodeId))
			{
				HighlightActiveStateInViews(childInfo.CurrentStateNodeId);
			}

			Repaint();
		}

		private void OnChildInstanceStopped(RuntimeInstanceInfo parentInfo, RuntimeInstanceInfo childInfo)
		{
			if (!_followSubStateMachines) return;

			// Only navigate back if we followed this instance into the child
			if (_followedInstance != parentInfo) return;

			// Only navigate back if we're currently viewing the child's graph
			var currentGraph = _activeGraphView?.CurrentAsset;
			if (currentGraph == null || !GraphsMatch(childInfo.Graph, currentGraph)) return;

			// Navigate back to the parent graph
			_activeGraphView?.NavigateBack();

			// Update the instance selector and select the parent instance
			_toolbarView?.UpdateInstanceSelector();
			_toolbarView?.SelectInstance(parentInfo);

			// Highlight the active state in the parent graph
			if (!string.IsNullOrEmpty(parentInfo.CurrentStateNodeId))
			{
				HighlightActiveStateInViews(parentInfo.CurrentStateNodeId);
			}

			Repaint();
		}

		private void OnEditorUpdate()
		{
			if (!Application.isPlaying || _targetAsset == null) return;

			var hasActiveAnimation =
				(_primaryGraphView != null && (_primaryGraphView.IsLerpAnimating || RuntimeGraphViewExtensions.IsAnimating(_primaryGraphView))) ||
				(_secondaryGraphView != null && (_secondaryGraphView.IsLerpAnimating || RuntimeGraphViewExtensions.IsAnimating(_secondaryGraphView)));

			if (hasActiveAnimation)
			{
				Repaint();
			}
		}

		public void OnInstanceSelectionChanged(RuntimeInstanceInfo instance)
		{
			if (instance == null) return;

			// Update the highlighted state for the newly selected instance
			if (!string.IsNullOrEmpty(instance.CurrentStateNodeId))
			{
				HighlightActiveStateInViews(instance.CurrentStateNodeId);
			}
			else if (_primaryGraphView?.AutoCenterOnActiveState == true)
			{
				// No current state yet, center on entry node as fallback
				CenterOnEntryNodeInViews();
			}

			// Update the debug timeline panel with the selected instance
			_debugTimelinePanel?.SetInstance(instance);
		}

		private void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.EnteredPlayMode)
			{
				HandleEnteredPlayMode();
				_toolbarView?.SetTimelineToggleVisible(true);
			}
			else if (state == PlayModeStateChange.EnteredEditMode)
			{
				HandleEnteredEditMode();
			}
		}

		private void OnPauseStateChanged(PauseState state)
		{
			// When execution resumes, drop the breakpoint-hit highlight so it doesn't linger.
			if (state != PauseState.Unpaused) return;
			_primaryGraphView?.ClearBreakpointHit();
			_secondaryGraphView?.ClearBreakpointHit();
		}

		private void HandleEnteredEditMode()
		{
			_debugTimelinePanel?.SetVisible(false);
			_toolbarView?.SetTimelineToggleValue(false);
			_toolbarView?.SetTimelineToggleVisible(false);

			// Drop any breakpoint-hit highlight left over from a paused session.
			_primaryGraphView?.ClearBreakpointHit();
			_secondaryGraphView?.ClearBreakpointHit();

			// Unlock graph views for editing
			_primaryGraphView?.SetPlayModeLocked(false);
			_secondaryGraphView?.SetPlayModeLocked(false);

			// Clear runtime blackboard values from variable nodes
			UpdateRuntimeBlackboardValuesInViews(null);
		}

		private void HandleEnteredPlayMode()
		{
			_toolbarView?.SetTimelineToggleVisible(true);

			// Lock graph views to prevent user edits during play mode
			_primaryGraphView?.SetPlayModeLocked(true);
			_secondaryGraphView?.SetPlayModeLocked(true);

			// Check if a GameObject with a StateMachineRunner is selected
			var selectedGO = Selection.activeGameObject;
			if (selectedGO != null)
			{
				var runner = selectedGO.GetComponent<StateMachineRunner>();
				if (runner != null && runner.Graph != null)
				{
					// Open the runner's graph (PopulateFromAsset also locks during play mode)
					LoadAsset(runner.Graph);
					return;
				}
			}

			// If graph is already open but no runner selected, wait for instances to register
			// and select the first one (handled by OnRuntimeInstanceRegistered with _autoSelectFirstInstance flag)
			if (_targetAsset != null)
			{
				_autoSelectFirstInstance = true;
			}
		}

		private void OnSelectionChanged()
		{
			// Check if a GameObject with a StateMachineRunner or related component is selected
			var selectedGO = Selection.activeGameObject;
			if (selectedGO == null) return;

			// Try to find a graph from the selected object
			var (graph, instance) = FindGraphFromGameObject(selectedGO);

			if (graph != null)
			{
				// Load the graph if different
				if (_targetAsset != graph)
				{
					LoadAsset(graph);
				}

				// If we found a specific instance, select it
				if (instance != null)
				{
					_toolbarView?.SelectInstance(instance);
					OnInstanceSelectionChanged(instance);
				}
			}
		}

		private (StateMachineGraph graph, RuntimeInstanceInfo instance) FindGraphFromGameObject(GameObject go)
		{
			// Check for StateMachineRunner
			var runner = go.GetComponent<StateMachineRunner>();
			if (runner != null && runner.Graph != null)
			{
				var instance = RuntimeInstanceTracker.GetInstancesForOwner(go).FirstOrDefault();
				return (runner.Graph, instance);
			}

			// Check for any component that might have a StateMachineController
			// by looking for running instances owned by this GameObject
			if (Application.isPlaying)
			{
				var instances = RuntimeInstanceTracker.GetInstancesForOwner(go).ToList();
				if (instances.Count > 0)
				{
					var firstInstance = instances.First();
					return (firstInstance.Graph, firstInstance);
				}
			}

			// Check for serialized StateMachineGraph fields on components
			var components = go.GetComponents<MonoBehaviour>();
			foreach (var component in components)
			{
				if (component == null) continue;

				var fields = component.GetType().GetFields(
					System.Reflection.BindingFlags.Instance |
					System.Reflection.BindingFlags.Public |
					System.Reflection.BindingFlags.NonPublic
				);

				foreach (var field in fields)
				{
					if (typeof(StateMachineGraph).IsAssignableFrom(field.FieldType))
					{
						var graph = field.GetValue(component) as StateMachineGraph;
						if (graph != null)
						{
							var instance = RuntimeInstanceTracker.GetInstancesForOwner(go).FirstOrDefault();
							return (graph, instance);
						}
					}
				}
			}

			return (null, null);
		}

		public void LoadAsset(StateMachineGraph asset)
		{
			_targetAsset = asset;

			if (_primaryGraphView != null)
			{
				_primaryGraphView.PopulateFromAsset(asset);
			}

			// Also populate secondary view if split is enabled
			if (_splitViewContainer?.IsSplitEnabled == true && _secondaryGraphView != null)
			{
				_secondaryGraphView.PopulateFromAsset(asset);
			}

			if (_blackboardView != null)
			{
				_blackboardView.PopulateFromAsset(asset);
			}

			if (_stateListPanel != null)
			{
				_stateListPanel.PopulateFromAsset(asset);
			}

			_breakpointListPanel?.SetGraph(asset);
			_nodeSearchBar?.PopulateFromAsset(asset);

			var suffix = asset.IsVariant ? " [Variant]" : "";
			titleContent = new GUIContent($"SM: {asset.name}{suffix}");
			_toolbarView?.UpdateNavigationUI();
			UpdateOverlayVisibility();

			// Add to recently viewed history
			Navigation.BreadcrumbNavigation.AddToHistory(asset);

			// Sync highlights with any running instance for this graph
			SyncHighlightsForCurrentGraph();
		}

		/// <summary>
		/// Syncs the highlight state with any running instance for the currently displayed graph.
		/// Called after loading/navigating to a new graph to catch up with runtime state.
		/// </summary>
		private void SyncHighlightsForCurrentGraph()
		{
			if (_primaryGraphView == null) return;

			// Use CurrentAsset which reflects the actually displayed graph (handles sub-graph navigation)
			var currentGraph = _primaryGraphView.CurrentAsset;
			if (currentGraph == null)
			{
				_primaryGraphView.ClearAllHighlights();
				_secondaryGraphView?.ClearAllHighlights();
				return;
			}

			if (StateMachineGraphView.DebugMode)
			{
				Debug.Log($"[GraphWindow] SyncHighlightsForCurrentGraph: graph={currentGraph.name}");
			}

			// Find an active instance for this graph
			var activeInstance = RuntimeInstanceTracker.GetActiveInstanceForGraph(currentGraph);
			if (activeInstance == null)
			{
				if (StateMachineGraphView.DebugMode)
				{
					Debug.Log($"[GraphWindow] No active instance found for graph: {currentGraph.name}");
				}
				_primaryGraphView.ClearAllHighlights();
				_secondaryGraphView?.ClearAllHighlights();

				// If in play mode with AutoCenter enabled, center on entry node as fallback
				if (Application.isPlaying && _primaryGraphView.AutoCenterOnActiveState)
				{
					CenterOnEntryNodeInViews();
				}
				return;
			}

			if (StateMachineGraphView.DebugMode)
			{
				Debug.Log($"[GraphWindow] Found instance: Status={activeInstance.Status}, CurrentStateNodeId={activeInstance.CurrentStateNodeId}");
			}

			// Highlight the current state in both views (also centers if AutoCenterOnActiveState is enabled)
			if (!string.IsNullOrEmpty(activeInstance.CurrentStateNodeId))
			{
				HighlightActiveStateInViews(activeInstance.CurrentStateNodeId);
			}
			else if (_primaryGraphView.AutoCenterOnActiveState)
			{
				// No current state yet, center on entry node as fallback
				CenterOnEntryNodeInViews();
			}

			// Show runtime blackboard values
			UpdateRuntimeBlackboardValuesInViews(activeInstance.Instance?.Blackboard);
		}

		public void SaveAsset()
		{
			if (_targetAsset == null)
			{
				return;
			}

			EditorUtility.SetDirty(_targetAsset);
			AssetDatabase.SaveAssetIfDirty(_targetAsset);
		}

		/// <summary>
		/// Compares two graphs by asset path to handle runtime vs editor reference differences.
		/// </summary>
		private bool GraphsMatch(StateMachineGraph a, StateMachineGraph b)
		{
			if (a == b) return true;
			if (a == null || b == null) return false;

			// Compare by asset path
			var pathA = AssetDatabase.GetAssetPath(a);
			var pathB = AssetDatabase.GetAssetPath(b);

			// If both have valid paths, compare them
			if (!string.IsNullOrEmpty(pathA) && !string.IsNullOrEmpty(pathB))
			{
				return pathA == pathB;
			}

			// Fallback: compare by name (for runtime-instantiated graphs)
			return a.name == b.name;
		}

		/// <summary>
		/// Toggles the split view on or off.
		/// </summary>
		public void ToggleSplitView()
		{
			_splitViewContainer?.ToggleSplit();
		}

		/// <summary>
		/// Sets whether split view is enabled.
		/// </summary>
		public void SetSplitViewEnabled(bool enabled)
		{
			_splitViewContainer?.SetSplitEnabled(enabled);
		}

		/// <summary>
		/// Toggles link scroll (synchronized pan/zoom between views).
		/// </summary>
		public void ToggleLinkScroll()
		{
			if (_splitViewContainer != null)
			{
				_splitViewContainer.IsLinkScrollEnabled = !_splitViewContainer.IsLinkScrollEnabled;
			}
		}

		/// <summary>
		/// Helper method to highlight the active state in both views if they're showing the same graph.
		/// </summary>
		private void HighlightActiveStateInViews(string nodeId)
		{
			_primaryGraphView?.HighlightActiveState(nodeId);

			if (_splitViewContainer?.IsSplitEnabled == true)
			{
				// Only highlight in secondary if it's showing the same graph level
				if (_secondaryGraphView?.CurrentAsset == _primaryGraphView?.CurrentAsset)
				{
					_secondaryGraphView?.HighlightActiveState(nodeId);
				}
			}
		}

		/// <summary>
		/// Updates runtime blackboard values on variable nodes in both views.
		/// </summary>
		private void UpdateRuntimeBlackboardValuesInViews(Nonatomic.VSM.Blackboard.Blackboard blackboard)
		{
			_primaryGraphView?.UpdateRuntimeBlackboardValues(blackboard);

			if (_splitViewContainer?.IsSplitEnabled == true)
			{
				if (_secondaryGraphView?.CurrentAsset == _primaryGraphView?.CurrentAsset)
				{
					_secondaryGraphView?.UpdateRuntimeBlackboardValues(blackboard);
				}
			}
		}

		/// <summary>
		/// Centers both views on the entry node. Used as fallback when no active state exists.
		/// </summary>
		private void CenterOnEntryNodeInViews()
		{
			_primaryGraphView?.FrameEntryNode(_primaryGraphView.UseLerpAnimation);

			if (_splitViewContainer?.IsSplitEnabled == true)
			{
				// Only center in secondary if it's showing the same graph level
				if (_secondaryGraphView?.CurrentAsset == _primaryGraphView?.CurrentAsset)
				{
					_secondaryGraphView?.FrameEntryNode(_secondaryGraphView.UseLerpAnimation);
				}
			}
		}

		public StateMachineGraph TargetAsset => _targetAsset;
		public StateMachineGraphView GraphView => _activeGraphView;
		public StateMachineGraphView PrimaryGraphView => _primaryGraphView;
		public StateMachineGraphView SecondaryGraphView => _secondaryGraphView;
		public SplitViewContainer SplitViewContainer => _splitViewContainer;
		public BlackboardView BlackboardView => _blackboardView;
		public InspectorView InspectorView => _inspectorView;
		public MiniMapView MiniMapView => _miniMapView;
		public StateListPanel StateListPanel => _stateListPanel;
		public DebugTimelinePanel DebugTimelinePanel => _debugTimelinePanel;
		public BreakpointListPanel BreakpointListPanel => _breakpointListPanel;

		/// <summary>
		/// Whether replay mode is active (not in live mode), suppressing live state highlighting.
		/// </summary>
		public bool IsReplayActive =>
			_debugTimelinePanel?.ReplayController?.Mode is ReplayMode mode && mode != ReplayMode.Live;
		public GroupInspectorPanel GroupInspectorPanel => _groupInspectorPanel;
		public NodeSearchBar NodeSearchBar => _nodeSearchBar;

		private void OnWindowKeyDown(KeyDownEvent evt)
		{
			if (evt.ctrlKey && !evt.shiftKey && !evt.altKey && evt.keyCode == KeyCode.F)
			{
				_nodeSearchBar?.Toggle();
				evt.StopPropagation();
			}
		}

		private void OnGraphSelectionChanged(GraphNode selectedNode)
		{
			_inspectorView?.ShowNodeInspector(selectedNode);
		}

		private void OnGroupSelectionChanged(GraphRegionView regionView)
		{
			if (regionView != null)
			{
				_groupInspectorPanel?.ShowGroupInspector(regionView);
				_groupInspectorPanel?.SetVisible(true);
			}
			else
			{
				_groupInspectorPanel?.SetVisible(false);
			}
		}

		private void OnNavigateToBreakpointNode(string nodeId)
		{
			if (_activeGraphView == null || string.IsNullOrEmpty(nodeId)) return;
			_activeGraphView.CenterOnState(nodeId);
		}

		private void OnNavigationChanged()
		{
			_toolbarView?.UpdateNavigationUI();
			_blackboardView?.PopulateFromAsset(_activeGraphView?.CurrentAsset);
			_stateListPanel?.PopulateFromAsset(_activeGraphView?.CurrentAsset);
			_breakpointListPanel?.SetGraph(_activeGraphView?.CurrentAsset);
			_nodeSearchBar?.PopulateFromAsset(_activeGraphView?.CurrentAsset);

			// Sync highlights for the newly navigated-to graph
			SyncHighlightsForCurrentGraph();
		}

		private void OnUndoRedo()
		{
			if (_targetAsset != null)
			{
				LoadAsset(_targetAsset);
			}
		}

		private static StyleSheet LoadStyleSheet()
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
					return styleSheet;
				}
			}

			// Try finding by GUID or searching
			var guids = AssetDatabase.FindAssets("StateMachineGraph t:StyleSheet");
			if (guids.Length > 0)
			{
				var path = AssetDatabase.GUIDToAssetPath(guids[0]);
				return AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
			}

			return null;
		}
	}
}
