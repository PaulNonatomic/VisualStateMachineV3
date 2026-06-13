using System.Collections.Generic;
using Nonatomic.VSM.Blackboard;
using Nonatomic.VSM.Editor.Ports;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Visual representation of a sub-state machine node.
	/// </summary>
	public class SubStateMachineNodeView : Node, INodeDataProvider, IPortProvider, IActiveStateNode
	{
		private const string SubGraphPortName = "SubGraph";

		public SubStateMachineNode NodeData { get; }
		GraphNode INodeDataProvider.NodeData => NodeData;

		private readonly StateMachineGraphView _graphView;
		private Port _flowInputPort;
		private Port _flowOutputPort;
		private Port _subGraphInputPort;
		private Button _openButton;
		private Button _previewButton;
		private StateMachineGraph _subGraph;
		private ObjectField _subGraphField;
		private Label _nameLabel;
		private Label _countLabel;
		private Label _warningLabel;
		private VisualElement _infoContainer;
		private SubGraphPreviewPopup _activePreviewPopup;

		// Exposed variable ports
		private readonly Dictionary<string, Port> _exposedInputPorts = new();
		private readonly Dictionary<string, Port> _exposedOutputPorts = new();
		private readonly Dictionary<string, VisualElement> _exposedInlineFields = new();

		// Runtime active state visualization
		private ActiveStateIndicator _activeStateIndicator;

		public SubStateMachineNodeView(SubStateMachineNode subNode, StateMachineGraphView graphView)
		{
			NodeData = subNode;
			_graphView = graphView;

			title = "Sub State Machine";

			AddToClassList("sub-state-machine-node");

			// Load referenced sub-graph (prefer direct reference, fallback to GUID)
			if (subNode.SubGraph != null)
			{
				_subGraph = subNode.SubGraph;
			}
			else if (!string.IsNullOrEmpty(subNode.SubGraphAssetGuid))
			{
				var path = AssetDatabase.GUIDToAssetPath(subNode.SubGraphAssetGuid);
				_subGraph = AssetDatabase.LoadAssetAtPath<StateMachineGraph>(path);
				// Also set the direct reference for runtime use
				subNode.SubGraph = _subGraph;
			}

			BuildNodeUI();

			// Register context menu
			NodeContextMenuHelper.RegisterContextMenu(this, this);

			// Update inline field visibility once attached to a panel (schedule.Execute
			// on a detached element is silently lost in some Unity versions)
			RegisterCallback<AttachToPanelEvent>(_ =>
			{
				schedule.Execute(UpdateInlineFieldVisibility);
			});
		}

		private void BuildNodeUI()
		{
			// Flow input port - uses Action type for transition compatibility
			_flowInputPort = InstantiatePort(
				Orientation.Horizontal,
				Direction.Input,
				Port.Capacity.Multi,
				typeof(System.Action));
			_flowInputPort.portName = "Enter";
			_flowInputPort.AddToClassList("flow-port");
			inputContainer.Add(_flowInputPort);

			// SubGraph input port - allows dynamic sub-graph assignment via connections
			_subGraphInputPort = InstantiatePort(
				Orientation.Horizontal,
				Direction.Input,
				Port.Capacity.Single,
				typeof(StateMachineGraph));
			_subGraphInputPort.portName = SubGraphPortName;
			_subGraphInputPort.AddToClassList("data-input-port");

			// Create inline ObjectField for the SubGraph port
			_subGraphField = new ObjectField
			{
				objectType = typeof(StateMachineGraph),
				value = _subGraph
			};
			_subGraphField.style.minWidth = 100;
			_subGraphField.style.maxWidth = 140;
			_subGraphField.style.marginLeft = 4;
			_subGraphField.AddToClassList("inline-field");
			_subGraphField.RegisterValueChangedCallback(OnSubGraphChanged);

			// Add the inline field to the port
			_subGraphInputPort.Add(_subGraphField);
			inputContainer.Add(_subGraphInputPort);

			// Flow output port - uses Action type so connections are classified as Transition
			_flowOutputPort = InstantiatePort(
				Orientation.Horizontal,
				Direction.Output,
				Port.Capacity.Single,
				typeof(System.Action));
			_flowOutputPort.portName = "Exit";
			_flowOutputPort.AddToClassList("transition-port");

			// Remove default EdgeConnector and add our custom one
			if (_graphView != null)
			{
				if (_flowOutputPort.edgeConnector != null)
					_flowOutputPort.RemoveManipulator(_flowOutputPort.edgeConnector);

				var connector = new EdgeConnector<TransitionEdgeView>(_graphView.EdgeConnectorListener);
				_flowOutputPort.AddManipulator(connector);
			}

			outputContainer.Add(_flowOutputPort);

			// Button container for Preview, Open, and collapse button
			var buttonContainer = new VisualElement();
			buttonContainer.name = "sub-state-button-container";
			buttonContainer.style.flexDirection = FlexDirection.Row;
			buttonContainer.style.alignItems = Align.Center;
			buttonContainer.style.marginTop = 4;
			buttonContainer.style.marginBottom = 4;
			titleButtonContainer.Add(buttonContainer);

			// Preview button
			_previewButton = new Button(OnPreviewClicked)
			{
				text = "Peek",
				tooltip = "Preview sub-graph contents"
			};
			_previewButton.style.marginRight = 4;
			buttonContainer.Add(_previewButton);

			// Open button
			_openButton = new Button(OnOpenClicked)
			{
				text = "Open"
			};
			_openButton.style.marginRight = 4;
			buttonContainer.Add(_openButton);

			// Sub-graph info display
			_infoContainer = new VisualElement();
			_infoContainer.style.paddingLeft = 8;
			_infoContainer.style.paddingRight = 8;
			_infoContainer.style.paddingTop = 4;
			_infoContainer.style.paddingBottom = 4;

			_nameLabel = new Label();
			_nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			_infoContainer.Add(_nameLabel);

			_countLabel = new Label();
			_countLabel.style.fontSize = 10;
			_countLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
			_infoContainer.Add(_countLabel);

			_warningLabel = new Label("No sub-machine assigned");
			_warningLabel.style.color = new Color(1f, 0.6f, 0.2f);
			_infoContainer.Add(_warningLabel);

			extensionContainer.Add(_infoContainer);

			UpdateInfoDisplay();

			// Build exposed variable ports from sub-graph
			BuildExposedPorts();

			// Style the node and ports
			var titleColor = new Color(0.4f, 0.3f, 0.5f);
			titleContainer.style.backgroundColor = new StyleColor(titleColor);
			_flowInputPort.portColor = titleColor;
			_subGraphInputPort.portColor = titleColor;
			_flowOutputPort.portColor = titleColor;

			// Add active state indicator (hidden by default)
			_activeStateIndicator = new ActiveStateIndicator(this, this, null, 8f);

			RefreshExpandedState();
			RefreshPorts();

			// Move the collapse button to the far right of the button container
			// This must happen after RefreshExpandedState() which creates the collapse button
			var collapseButton = this.Q<Button>("collapse-button");
			var btnContainer = this.Q<VisualElement>("sub-state-button-container");
			if (collapseButton != null && btnContainer != null)
			{
				collapseButton.RemoveFromHierarchy();
				btnContainer.Add(collapseButton);
			}
		}

		private void BuildExposedPorts()
		{
			// Remove existing exposed ports
			foreach (var port in _exposedInputPorts.Values)
			{
				port.DisconnectAll();
				inputContainer.Remove(port);
			}
			_exposedInputPorts.Clear();
			_exposedInlineFields.Clear();

			foreach (var port in _exposedOutputPorts.Values)
			{
				port.DisconnectAll();
				outputContainer.Remove(port);
			}
			_exposedOutputPorts.Clear();

			if (_subGraph == null) return;

			foreach (var definition in _subGraph.BlackboardDefinitions)
			{
				if (definition.ExposureDirection == ExposureDirection.None) continue;

				var valueType = definition.ValueType;
				if (valueType == null) continue;

				// Input port
				if (definition.ExposureDirection == ExposureDirection.Input ||
					definition.ExposureDirection == ExposureDirection.InputOutput)
				{
					var port = InstantiatePort(
						Orientation.Horizontal,
						Direction.Input,
						Port.Capacity.Single,
						valueType);
					port.portName = definition.Key;
					port.portColor = PortTypeStyleService.GetTypeColor(valueType);
					port.AddToClassList("data-input-port");

					// Add inline field for setting the value directly
					var inlineField = CreateExposedInlineField(definition.Key, valueType);
					if (inlineField != null)
					{
						port.Add(inlineField);
						_exposedInlineFields[definition.Key] = inlineField;
					}

					inputContainer.Add(port);
					_exposedInputPorts[definition.Key] = port;
				}

				// Output port
				if (definition.ExposureDirection == ExposureDirection.Output ||
					definition.ExposureDirection == ExposureDirection.InputOutput)
				{
					var port = InstantiatePort(
						Orientation.Horizontal,
						Direction.Output,
						Port.Capacity.Multi,
						valueType);
					port.portName = definition.Key;
					port.portColor = PortTypeStyleService.GetTypeColor(valueType);
					port.AddToClassList("data-output-port");

					// Add custom EdgeConnector for drag-to-connect
					if (_graphView != null)
					{
						if (port.edgeConnector != null)
							port.RemoveManipulator(port.edgeConnector);

						var connector = new EdgeConnector<TransitionEdgeView>(_graphView.EdgeConnectorListener);
						port.AddManipulator(connector);
					}

					outputContainer.Add(port);
					_exposedOutputPorts[definition.Key] = port;
				}
			}

			RefreshPorts();
		}

		private void UpdateInfoDisplay()
		{
			if (_subGraph != null)
			{
				_nameLabel.text = _subGraph.name;
				_countLabel.text = $"{_subGraph.NodeCount} nodes";
				_nameLabel.style.display = DisplayStyle.Flex;
				_countLabel.style.display = DisplayStyle.Flex;
				_warningLabel.style.display = DisplayStyle.None;
			}
			else
			{
				_nameLabel.style.display = DisplayStyle.None;
				_countLabel.style.display = DisplayStyle.None;
				_warningLabel.style.display = DisplayStyle.Flex;
			}
		}

		/// <summary>
		/// Updates the inline field visibility based on port connections.
		/// Call this when connections change.
		/// </summary>
		public void UpdateInlineFieldVisibility()
		{
			if (_subGraphField != null)
			{
				var hasConnection = _graphView?.CurrentAsset?.GetConnectionToPort(NodeData.Id, SubGraphPortName) != null;
				_subGraphField.style.display = hasConnection ? DisplayStyle.None : DisplayStyle.Flex;
			}

			// Update exposed input port inline fields
			foreach (var kvp in _exposedInlineFields)
			{
				var portKey = kvp.Key;
				var field = kvp.Value;
				var hasConnection = _graphView?.CurrentAsset?.GetConnectionToPort(NodeData.Id, portKey) != null;
				field.style.display = hasConnection ? DisplayStyle.None : DisplayStyle.Flex;
			}
		}

		private VisualElement CreateExposedInlineField(string key, System.Type valueType)
		{
			var existingOverride = NodeData.GetExposedPortValue(key);
			var currentValue = existingOverride?.GetValue();

			VisualElement field = null;

			if (valueType == typeof(bool))
			{
				var toggle = new Toggle();
				toggle.value = currentValue is bool b && b;
				toggle.RegisterValueChangedCallback(evt => SaveExposedPortValue(key, evt.newValue, valueType));
				field = toggle;
			}
			else if (valueType == typeof(int))
			{
				var intField = new IntegerField();
				intField.value = currentValue is int i ? i : 0;
				intField.style.minWidth = 40;
				intField.style.maxWidth = 60;
				intField.RegisterValueChangedCallback(evt => SaveExposedPortValue(key, evt.newValue, valueType));
				field = intField;
			}
			else if (valueType == typeof(float))
			{
				var floatField = new FloatField();
				floatField.value = currentValue is float f ? f : 0f;
				floatField.style.minWidth = 40;
				floatField.style.maxWidth = 60;
				floatField.RegisterValueChangedCallback(evt => SaveExposedPortValue(key, evt.newValue, valueType));
				field = floatField;
			}
			else if (valueType == typeof(string))
			{
				var textField = new TextField();
				textField.value = currentValue as string ?? string.Empty;
				textField.style.minWidth = 60;
				textField.style.maxWidth = 120;
				textField.RegisterValueChangedCallback(evt => SaveExposedPortValue(key, evt.newValue, valueType));
				field = textField;
			}
			else if (valueType.IsEnum)
			{
				var enumField = new EnumField((System.Enum)System.Activator.CreateInstance(valueType));
				if (currentValue is System.Enum e)
				{
					enumField.value = e;
				}
				enumField.style.minWidth = 60;
				enumField.RegisterValueChangedCallback(evt => SaveExposedPortValue(key, evt.newValue, valueType));
				field = enumField;
			}
			else if (valueType == typeof(Vector2))
			{
				var vectorField = new Vector2Field();
				vectorField.value = currentValue is Vector2 v2 ? v2 : Vector2.zero;
				vectorField.style.minWidth = 100;
				vectorField.RegisterValueChangedCallback(evt => SaveExposedPortValue(key, evt.newValue, valueType));
				field = vectorField;
			}
			else if (valueType == typeof(Vector3))
			{
				var vectorField = new Vector3Field();
				vectorField.value = currentValue is Vector3 v3 ? v3 : Vector3.zero;
				vectorField.style.minWidth = 140;
				vectorField.RegisterValueChangedCallback(evt => SaveExposedPortValue(key, evt.newValue, valueType));
				field = vectorField;
			}
			else if (valueType == typeof(Color))
			{
				var colorField = new ColorField();
				colorField.value = currentValue is Color c ? c : Color.white;
				colorField.style.minWidth = 40;
				colorField.style.maxWidth = 60;
				colorField.RegisterValueChangedCallback(evt => SaveExposedPortValue(key, evt.newValue, valueType));
				field = colorField;
			}
			else if (typeof(Object).IsAssignableFrom(valueType))
			{
				var objectField = new ObjectField();
				objectField.objectType = valueType;
				objectField.value = currentValue as Object;
				objectField.style.minWidth = 80;
				objectField.style.maxWidth = 120;
				objectField.RegisterValueChangedCallback(evt => SaveExposedPortValue(key, evt.newValue, valueType));
				field = objectField;
			}

			if (field != null)
			{
				field.AddToClassList("inline-field");
				field.style.marginLeft = 4;
			}

			return field;
		}

		private void SaveExposedPortValue(string key, object value, System.Type type)
		{
			if (_graphView?.CurrentAsset == null) return;
			if (EditorApplication.isPlayingOrWillChangePlaymode) return;

			Undo.RecordObject(_graphView.CurrentAsset, "Edit Exposed Port Value");
			NodeData.SetExposedPortValue(key, value, type);
			_graphView.MarkAssetDirty();
		}

		private void OnOpenClicked()
		{
			if (_subGraph == null) return;

			_graphView.NavigateToSubStateMachine(_subGraph);
		}

		private void OnPreviewClicked()
		{
			if (_subGraph == null) return;

			// Get the graph view
			var graphView = GetFirstAncestorOfType<StateMachineGraphView>();
			if (graphView == null) return;

			// Check if there's already an open popup for this node
			var existingPopup = graphView.GetPeekPopup(NodeData.Id);
			if (existingPopup != null)
			{
				// Close existing popup (toggle behavior)
				existingPopup.Close();
				_activePreviewPopup = null;
				return;
			}

			// Create and show the preview popup
			_activePreviewPopup = new SubGraphPreviewPopup(_subGraph);

			// Position near this node in graph coordinates (so it moves with the graph)
			var nodeRect = GetPosition();
			var initialOffset = new Vector2(10, 0); // 10 pixels to the right of the node
			var popupPosition = new Vector2(nodeRect.xMax + initialOffset.x, nodeRect.yMin + initialOffset.y);

			_activePreviewPopup.ShowInGraph(graphView, popupPosition);

			// Set owner node for position tracking (popup follows node when moved)
			_activePreviewPopup.SetOwnerNode(this, initialOffset);

			// Register with the graph view for central tracking
			graphView.RegisterPeekPopup(NodeData.Id, _activePreviewPopup);
		}

		private void ClosePreview()
		{
			if (_activePreviewPopup != null)
			{
				_activePreviewPopup.Close();
				_activePreviewPopup = null;
			}
		}

		private void OnSubGraphChanged(ChangeEvent<Object> evt)
		{
			_subGraph = evt.newValue as StateMachineGraph;
			NodeData.SubGraph = _subGraph;
			NodeData.SubGraphAssetGuid = _subGraph != null
				? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_subGraph))
				: null;

			// Update display name
			NodeData.DisplayName = _subGraph != null ? _subGraph.name : "Sub State Machine";
			title = NodeData.DisplayName;

			// Update the info display (name, node count, warning)
			UpdateInfoDisplay();

			// Rebuild exposed variable ports for the new sub-graph
			BuildExposedPorts();

			// Mark the graph asset as dirty so changes are saved
			if (_graphView?.CurrentAsset != null)
			{
				_graphView.MarkAssetDirty();
			}
		}

		public Port GetInputPort(string portName)
		{
			if (portName == SubGraphPortName)
				return _subGraphInputPort;

			if (_exposedInputPorts.TryGetValue(portName, out var exposedInput))
				return exposedInput;

			return _flowInputPort;
		}

		public Port GetOutputPort(string portName)
		{
			if (_exposedOutputPorts.TryGetValue(portName, out var exposedOutput))
				return exposedOutput;

			return _flowOutputPort;
		}

		/// <summary>
		/// Sets whether this node is currently the active state in runtime.
		/// </summary>
		public void SetActiveState(bool isActive)
		{
			_activeStateIndicator.SetActiveState(isActive);
		}

		/// <summary>
		/// Briefly flashes the active indicator for transient highlighting.
		/// </summary>
		public void FlashHighlight()
		{
			_activeStateIndicator.FlashHighlight();
		}
	}
}
