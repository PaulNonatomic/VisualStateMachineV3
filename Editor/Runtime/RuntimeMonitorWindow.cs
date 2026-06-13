using System.Collections.Generic;
using System.Linq;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Runtime
{
	/// <summary>
	/// Standalone dockable window for monitoring all state machine instances.
	/// </summary>
	public class RuntimeMonitorWindow : EditorWindow
	{
		private VisualElement _rootContainer;
		private VisualElement _instanceListContainer;
		private VisualElement _detailsContainer;
		private RuntimeInstanceInfo _selectedInstance;
		private TransitionTimelineView _timelineView;

		private readonly Dictionary<string, VisualElement> _instanceElements =
			new Dictionary<string, VisualElement>();

		[MenuItem("Window/Visual State Machine/Runtime Monitor")]
		public static void ShowWindow()
		{
			var window = GetWindow<RuntimeMonitorWindow>();
			window.titleContent = new GUIContent("VSM Runtime Monitor");
			window.minSize = new Vector2(400, 300);
			window.Show();
		}

		private void OnEnable()
		{
			BuildUI();
			SubscribeToEvents();
			RefreshInstanceList();
		}

		private void OnDisable()
		{
			UnsubscribeFromEvents();
		}

		private void BuildUI()
		{
			_rootContainer = new VisualElement();
			_rootContainer.style.flexGrow = 1;
			_rootContainer.style.flexDirection = FlexDirection.Row;
			rootVisualElement.Add(_rootContainer);

			// Apply stylesheet
			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
				"Packages/com.nonatomic.visualstatemachinev3/Editor/Styles/RuntimeMonitor.uss");
			if (styleSheet != null)
			{
				rootVisualElement.styleSheets.Add(styleSheet);
			}

			// Left panel - Instance list
			var leftPanel = new VisualElement();
			leftPanel.style.width = 250;
			leftPanel.style.borderRightWidth = 1;
			leftPanel.style.borderRightColor = new Color(0.15f, 0.15f, 0.15f);
			leftPanel.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
			_rootContainer.Add(leftPanel);

			// Header
			var header = new VisualElement();
			header.style.flexDirection = FlexDirection.Row;
			header.style.justifyContent = Justify.SpaceBetween;
			header.style.alignItems = Align.Center;
			header.style.paddingLeft = 8;
			header.style.paddingRight = 8;
			header.style.paddingTop = 8;
			header.style.paddingBottom = 8;
			header.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);

			var titleLabel = new Label("Instances");
			titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			header.Add(titleLabel);

			var refreshButton = new Button(RefreshInstanceList) { text = "Refresh" };
			refreshButton.style.height = 20;
			header.Add(refreshButton);

			leftPanel.Add(header);

			// Stats bar
			var statsBar = new VisualElement();
			statsBar.style.flexDirection = FlexDirection.Row;
			statsBar.style.paddingLeft = 8;
			statsBar.style.paddingRight = 8;
			statsBar.style.paddingTop = 4;
			statsBar.style.paddingBottom = 4;
			statsBar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);

			var statsLabel = new Label();
			statsLabel.name = "stats-label";
			statsLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
			statsBar.Add(statsLabel);

			leftPanel.Add(statsBar);

			// Instance list
			var scrollView = new ScrollView();
			scrollView.style.flexGrow = 1;
			leftPanel.Add(scrollView);

			_instanceListContainer = new VisualElement();
			scrollView.Add(_instanceListContainer);

			// Right panel - Details
			_detailsContainer = new VisualElement();
			_detailsContainer.style.flexGrow = 1;
			_detailsContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
			_rootContainer.Add(_detailsContainer);

			ShowNoSelectionView();
		}

		private void SubscribeToEvents()
		{
			RuntimeInstanceTracker.OnInstanceRegistered += OnInstanceRegistered;
			RuntimeInstanceTracker.OnInstanceUnregistered += OnInstanceUnregistered;
			RuntimeInstanceTracker.OnInstanceStateChanged += OnInstanceStateChanged;
			RuntimeInstanceTracker.OnInstancesCleared += OnInstancesCleared;
		}

		private void UnsubscribeFromEvents()
		{
			RuntimeInstanceTracker.OnInstanceRegistered -= OnInstanceRegistered;
			RuntimeInstanceTracker.OnInstanceUnregistered -= OnInstanceUnregistered;
			RuntimeInstanceTracker.OnInstanceStateChanged -= OnInstanceStateChanged;
			RuntimeInstanceTracker.OnInstancesCleared -= OnInstancesCleared;
		}

		private void OnInstanceRegistered(RuntimeInstanceInfo info)
		{
			RefreshInstanceList();
		}

		private void OnInstanceUnregistered(RuntimeInstanceInfo info)
		{
			if (_selectedInstance == info)
			{
				_selectedInstance = null;
				ShowNoSelectionView();
			}

			RefreshInstanceList();
		}

		private void OnInstanceStateChanged(RuntimeInstanceInfo info)
		{
			UpdateInstanceElement(info);
			if (_selectedInstance == info)
			{
				_timelineView?.RefreshTimeline();
			}
		}

		private void OnInstancesCleared()
		{
			_selectedInstance = null;
			RefreshInstanceList();
			ShowNoSelectionView();
		}

		private void RefreshInstanceList()
		{
			_instanceListContainer.Clear();
			_instanceElements.Clear();

			var instances = RuntimeInstanceTracker.AllInstances.ToList();

			// Update stats
			var stats = RuntimeInstanceTracker.GetStats();
			var statsLabel = _rootContainer.Q<Label>("stats-label");
			if (statsLabel != null)
			{
				statsLabel.text = $"Total: {stats.TotalInstances} | Running: {stats.RunningInstances}";
			}

			if (instances.Count == 0)
			{
				var noInstances = new Label("No running instances");
				noInstances.style.color = new Color(0.5f, 0.5f, 0.5f);
				noInstances.style.unityTextAlign = TextAnchor.MiddleCenter;
				noInstances.style.marginTop = 20;
				_instanceListContainer.Add(noInstances);
				return;
			}

			// Group by graph
			var byGraph = instances.GroupBy(i => i.Graph).OrderBy(g => g.Key?.name ?? "Unknown");

			foreach (var group in byGraph)
			{
				// Graph header
				var graphHeader = new Foldout();
				graphHeader.text = group.Key?.name ?? "Unknown Graph";
				graphHeader.value = true;
				graphHeader.style.marginLeft = 4;
				graphHeader.style.marginRight = 4;
				graphHeader.style.marginTop = 4;
				_instanceListContainer.Add(graphHeader);

				foreach (var instance in group)
				{
					var element = CreateInstanceElement(instance);
					graphHeader.Add(element);
					_instanceElements[instance.InstanceId] = element;
				}
			}
		}

		private VisualElement CreateInstanceElement(RuntimeInstanceInfo info)
		{
			var element = new VisualElement();
			element.style.flexDirection = FlexDirection.Row;
			element.style.alignItems = Align.Center;
			element.style.paddingLeft = 8;
			element.style.paddingRight = 8;
			element.style.paddingTop = 6;
			element.style.paddingBottom = 6;
			element.style.marginTop = 1;
			element.style.marginBottom = 1;
			element.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
			element.style.borderTopLeftRadius = 4;
			element.style.borderTopRightRadius = 4;
			element.style.borderBottomLeftRadius = 4;
			element.style.borderBottomRightRadius = 4;

			// Status indicator
			var statusIndicator = new VisualElement();
			statusIndicator.name = "status";
			statusIndicator.style.width = 10;
			statusIndicator.style.height = 10;
			statusIndicator.style.borderTopLeftRadius = 5;
			statusIndicator.style.borderTopRightRadius = 5;
			statusIndicator.style.borderBottomLeftRadius = 5;
			statusIndicator.style.borderBottomRightRadius = 5;
			statusIndicator.style.marginRight = 8;
			statusIndicator.style.backgroundColor = GetStatusColor(info.Status);
			element.Add(statusIndicator);

			// Info container
			var infoContainer = new VisualElement();
			infoContainer.style.flexGrow = 1;
			element.Add(infoContainer);

			var ownerLabel = new Label(info.Owner?.name ?? "Unknown");
			ownerLabel.name = "owner";
			infoContainer.Add(ownerLabel);

			var stateLabel = new Label(info.CurrentStateName ?? "Starting");
			stateLabel.name = "state";
			stateLabel.style.fontSize = 10;
			stateLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
			infoContainer.Add(stateLabel);

			// Open button
			var openButton = new Button(() => OpenInGraphWindow(info))
			{
				text = "Open"
			};
			openButton.style.height = 20;
			element.Add(openButton);

			// Selection handling
			element.RegisterCallback<ClickEvent>(evt => { SelectInstance(info, element); });

			element.RegisterCallback<MouseEnterEvent>(evt =>
			{
				if (_selectedInstance != info)
				{
					element.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
				}
			});

			element.RegisterCallback<MouseLeaveEvent>(evt =>
			{
				if (_selectedInstance != info)
				{
					element.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
				}
			});

			return element;
		}

		private void UpdateInstanceElement(RuntimeInstanceInfo info)
		{
			if (!_instanceElements.TryGetValue(info.InstanceId, out var element))
			{
				return;
			}

			var statusIndicator = element.Q("status");
			if (statusIndicator != null)
			{
				statusIndicator.style.backgroundColor = GetStatusColor(info.Status);
			}

			var stateLabel = element.Q<Label>("state");
			if (stateLabel != null)
			{
				stateLabel.text = info.CurrentStateName ?? "Unknown";
			}
		}

		private Color GetStatusColor(RuntimeInstanceState status)
		{
			return status switch
			{
				RuntimeInstanceState.Running => new Color(0.2f, 0.8f, 0.2f),
				RuntimeInstanceState.Paused => new Color(0.8f, 0.8f, 0.2f),
				RuntimeInstanceState.Stopped => new Color(0.5f, 0.5f, 0.5f),
				RuntimeInstanceState.Error => new Color(0.8f, 0.2f, 0.2f),
				_ => new Color(0.5f, 0.5f, 0.5f)
			};
		}

		private void SelectInstance(RuntimeInstanceInfo info, VisualElement element)
		{
			// Deselect previous
			foreach (var kvp in _instanceElements)
			{
				kvp.Value.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
				kvp.Value.style.borderLeftWidth = 0;
			}

			// Select new
			_selectedInstance = info;
			element.style.backgroundColor = new Color(0.22f, 0.32f, 0.42f);
			element.style.borderLeftWidth = 3;
			element.style.borderLeftColor = new Color(0.27f, 0.75f, 1f);

			RefreshDetailsView();
		}

		private void ShowNoSelectionView()
		{
			_detailsContainer.Clear();

			var noSelection = new VisualElement();
			noSelection.style.flexGrow = 1;
			noSelection.style.alignItems = Align.Center;
			noSelection.style.justifyContent = Justify.Center;

			var label = new Label("Select an instance to view details");
			label.style.color = new Color(0.5f, 0.5f, 0.5f);
			label.style.fontSize = 14;
			noSelection.Add(label);

			_detailsContainer.Add(noSelection);
		}

		private void RefreshDetailsView()
		{
			if (_selectedInstance == null)
			{
				ShowNoSelectionView();
				return;
			}

			_detailsContainer.Clear();

			// Header
			var header = new VisualElement();
			header.style.paddingLeft = 12;
			header.style.paddingRight = 12;
			header.style.paddingTop = 12;
			header.style.paddingBottom = 12;
			header.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);

			var ownerLabel = new Label(_selectedInstance.Owner?.name ?? "Unknown");
			ownerLabel.style.fontSize = 16;
			ownerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			header.Add(ownerLabel);

			var graphLabel = new Label($"Graph: {_selectedInstance.Graph?.name ?? "Unknown"}");
			graphLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
			header.Add(graphLabel);

			_detailsContainer.Add(header);

			// Current state
			var currentStateSection = CreateSection("Current State");
			var currentState = new Label(_selectedInstance.CurrentStateName ?? "Unknown");
			currentState.style.fontSize = 14;
			currentState.style.unityFontStyleAndWeight = FontStyle.Bold;
			currentState.style.color = new Color(0.4f, 0.8f, 0.4f);
			currentStateSection.Add(currentState);
			_detailsContainer.Add(currentStateSection);

			// Transition timeline
			_timelineView = new TransitionTimelineView();
			_timelineView.SetInstance(_selectedInstance);
			_timelineView.OnNavigateToState += nodeId =>
			{
				if (_selectedInstance?.Graph != null)
				{
					var graphWindow = StateMachineGraphWindow.OpenWindow();
					graphWindow.LoadAsset(_selectedInstance.Graph);
					graphWindow.GraphView?.CenterOnState(nodeId, true);
					graphWindow.GraphView?.HighlightActiveState(nodeId);
				}
			};
			_detailsContainer.Add(_timelineView);

			// Blackboard
			var blackboardSection = CreateSection("Blackboard");
			var snapshot = _selectedInstance.GetBlackboardSnapshot();

			if (snapshot.Count == 0)
			{
				var noVars = new Label("No variables");
				noVars.style.color = new Color(0.5f, 0.5f, 0.5f);
				blackboardSection.Add(noVars);
			}
			else
			{
				foreach (var kvp in snapshot)
				{
					var row = new VisualElement();
					row.style.flexDirection = FlexDirection.Row;
					row.style.paddingTop = 2;
					row.style.paddingBottom = 2;

					var keyLabel = new Label(kvp.Key);
					keyLabel.style.width = 120;
					keyLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
					row.Add(keyLabel);

					var valueLabel = new Label(kvp.Value?.ToString() ?? "null");
					valueLabel.style.flexGrow = 1;
					valueLabel.style.color = new Color(0.6f, 0.8f, 1f);
					row.Add(valueLabel);

					blackboardSection.Add(row);
				}
			}

			_detailsContainer.Add(blackboardSection);
		}

		private VisualElement CreateSection(string title)
		{
			var section = new VisualElement();
			section.style.marginLeft = 12;
			section.style.marginRight = 12;
			section.style.marginTop = 12;

			var header = new Label(title);
			header.style.unityFontStyleAndWeight = FontStyle.Bold;
			header.style.marginBottom = 8;
			section.Add(header);

			return section;
		}

		private void OpenInGraphWindow(RuntimeInstanceInfo info)
		{
			if (info.Graph == null)
			{
				return;
			}

			var graphWindow = StateMachineGraphWindow.OpenWindow();
			graphWindow.LoadAsset(info.Graph);

			if (!string.IsNullOrEmpty(info.CurrentStateNodeId))
			{
				graphWindow.GraphView?.CenterOnState(info.CurrentStateNodeId, true);
				graphWindow.GraphView?.HighlightActiveState(info.CurrentStateNodeId);
			}
		}
	}
}
