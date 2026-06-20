using System;
using System.Collections.Generic;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Editor.Debugging;
using Nonatomic.VSM.Editor.Graph.Settings;
using Nonatomic.VSM.Editor.Ports;
using Nonatomic.VSM.Editor.Services;
using Nonatomic.VSM.Graph;
using Nonatomic.VSM.Ports;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Visual representation of a state node in the graph.
	/// </summary>
	public class StateNodeView : Node, INodeDataProvider, IPortProvider, IActiveStateNode
	{
		public StateNode NodeData { get; }
		GraphNode INodeDataProvider.NodeData => NodeData;

		private readonly StateMachineGraphView _graphView;
		private Port _flowInputPort;
		private readonly Dictionary<string, Port> _inputPorts = new Dictionary<string, Port>();
		private readonly Dictionary<string, Port> _outputPorts = new Dictionary<string, Port>();
		private readonly Dictionary<string, Port> _transitionPorts = new Dictionary<string, Port>();
		private readonly Dictionary<string, VisualElement> _inlineFields = new Dictionary<string, VisualElement>();
		private readonly Dictionary<string, InputPortInfo> _inputPortInfos = new Dictionary<string, InputPortInfo>();
		private readonly Dictionary<string, OutputPortInfo> _outputPortInfos = new Dictionary<string, OutputPortInfo>();
		private readonly Dictionary<string, TransitionPortInfo> _transitionPortInfos = new Dictionary<string, TransitionPortInfo>();

		private ActiveStateIndicator _activeStateIndicator;
		private BreakpointIndicator _breakpointIndicator;
		private VisualElement _breakpointHitBorder;
		private bool _breakpointHitActive;
		private double _breakpointHitLastTime;
		private float _breakpointHitPhase;
		private static readonly Color BreakpointHitColor = new Color(0.95f, 0.2f, 0.2f);
		private Color _titleColor = new Color(0.3f, 0.3f, 0.35f); // Default state node color

		public StateNodeView(StateNode stateNode, StateMachineGraphView graphView)
		{
			NodeData = stateNode;
			_graphView = graphView;

			// Set title
			title = string.IsNullOrEmpty(stateNode.DisplayName)
				? GetStateTypeName()
				: stateNode.DisplayName;

			// Add style classes
			AddToClassList("state-node");
			if (stateNode.IsEntryState)
			{
				AddToClassList("entry-state");
			}

			BuildNodeUI();

			// Register context menu with graph for breakpoint support
			NodeContextMenuHelper.RegisterContextMenu(this, this, graphView?.CurrentAsset);

			// Listen for breakpoint changes
			BreakpointManager.OnBreakpointToggled += HandleBreakpointToggled;

			// Unsubscribe when removed from panel to avoid leaks
			RegisterCallback<DetachFromPanelEvent>(_ =>
			{
				BreakpointManager.OnBreakpointToggled -= HandleBreakpointToggled;
				EditorApplication.update -= UpdateBreakpointHitPulse;
			});
		}

		private void BuildNodeUI()
		{
			// Add active state indicator
			_activeStateIndicator = new ActiveStateIndicator(this, mainContainer, titleContainer);

			// Breakpoint-hit highlight: a red pulsing border shown while execution is paused on this
			// node's breakpoint, so it's unmistakable the runtime stopped here rather than hung.
			_breakpointHitBorder = new VisualElement();
			_breakpointHitBorder.style.position = Position.Absolute;
			_breakpointHitBorder.style.top = 0;
			_breakpointHitBorder.style.left = 0;
			_breakpointHitBorder.style.right = 0;
			_breakpointHitBorder.style.bottom = 0;
			_breakpointHitBorder.style.borderTopWidth = 3;
			_breakpointHitBorder.style.borderBottomWidth = 3;
			_breakpointHitBorder.style.borderLeftWidth = 3;
			_breakpointHitBorder.style.borderRightWidth = 3;
			_breakpointHitBorder.style.display = DisplayStyle.None;
			_breakpointHitBorder.pickingMode = PickingMode.Ignore;
			SetBreakpointHitBorderColor(BreakpointHitColor);
			mainContainer.Add(_breakpointHitBorder);

			// Add breakpoint indicator
			_breakpointIndicator = new BreakpointIndicator(titleContainer);
			UpdateBreakpointIndicator();

			// Create flow input port (for transitions INTO this state)
			_flowInputPort = InstantiatePort(
				Orientation.Horizontal,
				Direction.Input,
				Port.Capacity.Multi,
				typeof(bool));
			_flowInputPort.portName = "Enter";
			_flowInputPort.AddToClassList("flow-port");
			inputContainer.Add(_flowInputPort);

			// Discover and create ports based on state type
			NodeData.RestoreType();
			if (NodeData.StateType != null)
			{
				var manifest = PortDiscoveryService.GetPortManifest(NodeData.StateType);
				CreatePortsFromManifest(manifest);

				// Apply StateStyle attribute if present
				ApplyStateStyle();
			}

			// Apply title color to node and all ports
			ApplyTitleAndPortColors();

			RefreshExpandedState();
			RefreshPorts();

			// Update inline field visibility once attached to a panel (schedule.Execute
			// on a detached element is silently lost in some Unity versions)
			RegisterCallback<AttachToPanelEvent>(_ =>
			{
				schedule.Execute(UpdateInlineFieldVisibility);
			});
		}

		private void ApplyStateStyle()
		{
			if (NodeData.StateType == null) return;

			// Look for StateStyleAttribute on the state type
			var styleAttr = (StateStyleAttribute)Attribute.GetCustomAttribute(
				NodeData.StateType,
				typeof(StateStyleAttribute),
				true);

			if (styleAttr == null) return;

			// Parse color from attribute
			if (!string.IsNullOrEmpty(styleAttr.Color))
			{
				if (ColorUtility.TryParseHtmlString(styleAttr.Color, out var color))
				{
					_titleColor = color;
				}
			}

			// Apply icon if specified
			if (!string.IsNullOrEmpty(styleAttr.Icon))
			{
				// First try loading from custom icons via ImageService
				var iconTexture = ImageService.LoadNodeIcon(styleAttr.Icon);

				// Fall back to Unity built-in icons if custom icon not found
				if (iconTexture == null)
				{
					iconTexture = EditorGUIUtility.IconContent(styleAttr.Icon)?.image as Texture2D;
				}

				if (iconTexture != null)
				{
					var iconElement = new Image { image = iconTexture };
					iconElement.style.width = 24;
					iconElement.style.height = 24;
					iconElement.style.marginLeft = 4;
					iconElement.style.marginTop = 5;

					// Insert icon at the beginning of title container
					titleContainer.Insert(0, iconElement);
				}
			}
		}

		private void ApplyTitleAndPortColors()
		{
			// Apply title color
			titleContainer.style.backgroundColor = new StyleColor(_titleColor);

			// Add a subtle border accent with darker color
			var darkerColor = new Color(_titleColor.r * 0.7f, _titleColor.g * 0.7f, _titleColor.b * 0.7f);
			titleContainer.style.borderBottomWidth = 2;
			titleContainer.style.borderBottomColor = new StyleColor(darkerColor);

			// Determine port coloring based on settings
			var useTypeColors = PortSettings.Instance.ColorMode == PortColorMode.TypeBased;

			// Apply color to flow input port
			_flowInputPort.portColor = useTypeColors ? PortTypeStyleService.GetFlowColor() : _titleColor;

			// Apply colors to data input ports
			ApplyColorToPorts(_inputPorts, useTypeColors, key =>
				_inputPortInfos.TryGetValue(key, out var info) ? info.DataType : null);

			// Apply colors to data output ports
			ApplyColorToPorts(_outputPorts, useTypeColors, key =>
				_outputPortInfos.TryGetValue(key, out var info) ? info.DataType : null);

			// Apply color to transition ports. An explicit [Transition(Color=...)] hint
			// takes precedence regardless of color mode; otherwise fall back to the
			// standard transition color (or the node color in node-based mode).
			ApplyColorToPorts(_transitionPorts, useTypeColors, _ => null,
				() => PortTypeStyleService.GetTransitionColor(),
				key => _transitionPortInfos.TryGetValue(key, out var info) && info.TryGetColor(out var hint)
					? hint
					: (Color?)null);
		}

		private void ApplyColorToPorts(
			Dictionary<string, Port> ports,
			bool useTypeColors,
			Func<string, Type> dataTypeResolver,
			Func<Color> typeColorFallback = null,
			Func<string, Color?> explicitColorResolver = null)
		{
			foreach (var kvp in ports)
			{
				// An explicit per-port color override wins over both color modes.
				if (explicitColorResolver != null)
				{
					var explicitColor = explicitColorResolver(kvp.Key);
					if (explicitColor.HasValue)
					{
						kvp.Value.portColor = explicitColor.Value;
						continue;
					}
				}

				if (!useTypeColors)
				{
					kvp.Value.portColor = _titleColor;
					continue;
				}

				var dataType = dataTypeResolver(kvp.Key);
				if (dataType != null)
				{
					kvp.Value.portColor = PortTypeStyleService.GetTypeColor(dataType);
				}
				else if (typeColorFallback != null)
				{
					kvp.Value.portColor = typeColorFallback();
				}
				else
				{
					kvp.Value.portColor = _titleColor;
				}
			}
		}

		private void CreatePortsFromManifest(StatePortManifest manifest)
		{
			// Create data input ports
			foreach (var portInfo in manifest.InputPorts)
			{
				var port = CreateDataInputPort(portInfo);
				inputContainer.Add(port);
				_inputPorts[portInfo.FieldName] = port;
			}

			// Create data output ports
			foreach (var portInfo in manifest.OutputPorts)
			{
				var port = CreateDataOutputPort(portInfo);
				outputContainer.Add(port);
				_outputPorts[portInfo.FieldName] = port;
			}

			// Create transition ports
			foreach (var portInfo in manifest.TransitionPorts)
			{
				var port = CreateTransitionPort(portInfo);
				outputContainer.Add(port);
				_transitionPorts[portInfo.FieldName] = port;
			}
		}

		private Port CreateDataInputPort(InputPortInfo portInfo)
		{
			var port = InstantiatePort(
				Orientation.Horizontal,
				Direction.Input,
				Port.Capacity.Single,
				portInfo.DataType ?? typeof(object));

			// Store the field name in userData for connection matching.
			// Setting label.text below overwrites port.portName, so we cannot
			// rely on portName to return the field name after construction.
			port.portName = portInfo.FieldName;
			port.userData = portInfo.FieldName;
			port.tooltip = portInfo.Tooltip;
			port.AddToClassList("data-input-port");

			// Store port info for later use
			_inputPortInfos[portInfo.FieldName] = portInfo;

			// Add type info and inline fields after the port's original label to preserve the Port's DOM structure.
			// Removing the original Label breaks the port connector's interaction.
			var label = port.Q<Label>();
			if (label != null)
			{
				label.text = portInfo.DisplayName;
				label.style.marginRight = 4;
				var parent = label.parent;
				var insertIndex = parent.IndexOf(label) + 1;

				// Type icon (after name for input ports, only if setting is enabled)
				if (PortSettings.Instance.ShowTypeIcons)
				{
					var typeIcon = PortTypeStyleService.GetTypeIcon(portInfo.DataType);
					if (typeIcon != null)
					{
						var iconElement = new Image { image = typeIcon };
						iconElement.style.width = 12;
						iconElement.style.height = 12;
						iconElement.style.marginRight = 2;
						iconElement.tintColor = PortTypeStyleService.GetTypeColor(portInfo.DataType);
						iconElement.pickingMode = PickingMode.Ignore;
						parent.Insert(insertIndex, iconElement);
						insertIndex++;
					}
				}

				// Type label - color depends on port color mode
				var typeName = portInfo.GetFriendlyTypeName();
				var useTypeColors = PortSettings.Instance.ColorMode == PortColorMode.TypeBased;
				var typeColor = useTypeColors
					? PortTypeStyleService.GetTypeColor(portInfo.DataType)
					: new Color(0.5f, 0.8f, 0.9f, 0.8f); // Cyan tint for state-based mode
				var typeLabel = new Label($"({typeName})");
				typeLabel.style.color = new StyleColor(typeColor);
				typeLabel.style.fontSize = 10;
				typeLabel.style.marginRight = 4;
				typeLabel.pickingMode = PickingMode.Ignore;
				parent.Insert(insertIndex, typeLabel);
				insertIndex++;

				// Create inline field for the port type
				var inlineField = CreateInlineField(portInfo);
				if (inlineField != null)
				{
					parent.Insert(insertIndex, inlineField);
					_inlineFields[portInfo.FieldName] = inlineField;
				}
			}

			if (portInfo.IsRequired)
			{
				port.AddToClassList("required-port");
			}

			return port;
		}

		private VisualElement CreateInlineField(InputPortInfo portInfo)
		{
			var type = portInfo.DataType;
			if (type == null) return null;

			VisualElement field = null;

			// Get current value from property overrides
			var currentOverride = NodeData.GetPropertyOverride(portInfo.FieldName);
			var currentValue = currentOverride?.GetValue();

			if (type == typeof(string))
			{
				var textField = new TextField();
				textField.value = currentValue as string ?? string.Empty;
				textField.style.minWidth = 60;
				textField.style.maxWidth = 120;
				textField.RegisterValueChangedCallback(evt => SaveInlineValue(portInfo.FieldName, evt.newValue, type));
				field = textField;
			}
			else if (type == typeof(int))
			{
				var intField = new IntegerField();
				intField.value = currentValue is int i ? i : 0;
				intField.style.minWidth = 40;
				intField.style.maxWidth = 60;
				intField.RegisterValueChangedCallback(evt => SaveInlineValue(portInfo.FieldName, evt.newValue, type));
				field = intField;
			}
			else if (type == typeof(float))
			{
				var floatField = new FloatField();
				floatField.value = currentValue is float f ? f : 0f;
				floatField.style.minWidth = 40;
				floatField.style.maxWidth = 60;
				floatField.RegisterValueChangedCallback(evt => SaveInlineValue(portInfo.FieldName, evt.newValue, type));
				field = floatField;
			}
			else if (type == typeof(bool))
			{
				var toggle = new Toggle();
				toggle.value = currentValue is bool b && b;
				toggle.RegisterValueChangedCallback(evt => SaveInlineValue(portInfo.FieldName, evt.newValue, type));
				field = toggle;
			}
			else if (type == typeof(Vector2))
			{
				var vectorField = new Vector2Field();
				vectorField.value = currentValue is Vector2 v2 ? v2 : Vector2.zero;
				vectorField.style.minWidth = 100;
				vectorField.RegisterValueChangedCallback(evt => SaveInlineValue(portInfo.FieldName, evt.newValue, type));
				field = vectorField;
			}
			else if (type == typeof(Vector3))
			{
				var vectorField = new Vector3Field();
				vectorField.value = currentValue is Vector3 v3 ? v3 : Vector3.zero;
				vectorField.style.minWidth = 140;
				vectorField.RegisterValueChangedCallback(evt => SaveInlineValue(portInfo.FieldName, evt.newValue, type));
				field = vectorField;
			}
			else if (type == typeof(Vector4))
			{
				var vectorField = new Vector4Field();
				vectorField.value = currentValue is Vector4 v4 ? v4 : Vector4.zero;
				vectorField.style.minWidth = 180;
				vectorField.RegisterValueChangedCallback(evt => SaveInlineValue(portInfo.FieldName, evt.newValue, type));
				field = vectorField;
			}
			else if (type == typeof(Quaternion))
			{
				// Display Quaternion as Euler angles for intuitive editing
				var vectorField = new Vector3Field();
				vectorField.value = currentValue is Quaternion q ? q.eulerAngles : Vector3.zero;
				vectorField.style.minWidth = 140;
				vectorField.RegisterValueChangedCallback(evt =>
				{
					var quaternion = Quaternion.Euler(evt.newValue);
					SaveInlineValue(portInfo.FieldName, quaternion, type);
				});
				field = vectorField;
			}
			else if (type == typeof(LayerMask))
			{
				var layerField = new LayerMaskField();
				layerField.value = currentValue is LayerMask lm ? lm : default;
				layerField.style.minWidth = 80;
				layerField.RegisterValueChangedCallback(evt => SaveInlineValue(portInfo.FieldName, (LayerMask)evt.newValue, type));
				field = layerField;
			}
			else if (type == typeof(Color))
			{
				var colorField = new ColorField();
				colorField.value = currentValue is Color c ? c : Color.white;
				colorField.style.minWidth = 40;
				colorField.style.maxWidth = 60;
				colorField.RegisterValueChangedCallback(evt => SaveInlineValue(portInfo.FieldName, evt.newValue, type));
				field = colorField;
			}
			else if (type.IsEnum)
			{
				var enumField = new EnumField((Enum)Activator.CreateInstance(type));
				if (currentValue is Enum e)
				{
					enumField.value = e;
				}
				enumField.style.minWidth = 60;
				enumField.RegisterValueChangedCallback(evt => SaveInlineValue(portInfo.FieldName, evt.newValue, type));
				field = enumField;
			}
			else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
			{
				var objectField = new ObjectField();
				objectField.objectType = type;
				objectField.value = currentValue as UnityEngine.Object;
				objectField.style.minWidth = 80;
				objectField.style.maxWidth = 120;
				objectField.RegisterValueChangedCallback(evt => SaveInlineValue(portInfo.FieldName, evt.newValue, type));
				field = objectField;
			}

			if (field != null)
			{
				field.AddToClassList("inline-field");
				field.style.marginLeft = 4;
			}

			return field;
		}

		private void SaveInlineValue(string fieldName, object value, Type type)
		{
			if (_graphView?.CurrentAsset == null) return;
			if (EditorApplication.isPlayingOrWillChangePlaymode) return;

			Undo.RecordObject(_graphView.CurrentAsset, "Edit Inline Value");
			NodeData.SetPropertyOverride(fieldName, value, type);
			_graphView.MarkAssetDirty();
		}

		/// <summary>
		/// Updates inline field visibility based on port connections.
		/// Call this when connections change.
		/// </summary>
		public void UpdateInlineFieldVisibility()
		{
			foreach (var kvp in _inlineFields)
			{
				var fieldName = kvp.Key;
				var field = kvp.Value;

				var hasConnection = _graphView?.CurrentAsset?.GetConnectionToPort(NodeData.Id, fieldName) != null;
				field.style.display = hasConnection ? DisplayStyle.None : DisplayStyle.Flex;
			}
		}

		private Port CreateDataOutputPort(OutputPortInfo portInfo)
		{
			var port = InstantiatePort(
				Orientation.Horizontal,
				Direction.Output,
				Port.Capacity.Multi,
				portInfo.DataType ?? typeof(object));

			// Store the field name in userData for connection matching.
			port.portName = portInfo.FieldName;
			port.userData = portInfo.FieldName;
			port.tooltip = portInfo.Tooltip;
			port.AddToClassList("data-output-port");

			// Store port info for later use (e.g., color application)
			_outputPortInfos[portInfo.FieldName] = portInfo;

			// Add type info before the port's original label to preserve the Port's DOM structure.
			// Removing the original Label breaks the port connector's drag interaction.
			var label = port.Q<Label>();
			if (label != null)
			{
				label.text = portInfo.DisplayName;
				var parent = label.parent;
				var insertIndex = parent.IndexOf(label);

				// Type label - color depends on port color mode (on left for output ports)
				var typeName = portInfo.GetFriendlyTypeName();
				var useTypeColors = PortSettings.Instance.ColorMode == PortColorMode.TypeBased;
				var typeColor = useTypeColors
					? PortTypeStyleService.GetTypeColor(portInfo.DataType)
					: new Color(0.5f, 0.8f, 0.9f, 0.8f); // Cyan tint for state-based mode
				var typeLabel = new Label($"({typeName})");
				typeLabel.style.color = new StyleColor(typeColor);
				typeLabel.style.fontSize = 10;
				typeLabel.style.marginRight = 4;
				typeLabel.pickingMode = PickingMode.Ignore;
				parent.Insert(insertIndex, typeLabel);

				// Type icon (on left for output ports, only if setting is enabled)
				if (PortSettings.Instance.ShowTypeIcons)
				{
					var typeIcon = PortTypeStyleService.GetTypeIcon(portInfo.DataType);
					if (typeIcon != null)
					{
						var iconElement = new Image { image = typeIcon };
						iconElement.style.width = 12;
						iconElement.style.height = 12;
						iconElement.style.marginRight = 2;
						iconElement.tintColor = PortTypeStyleService.GetTypeColor(portInfo.DataType);
						iconElement.pickingMode = PickingMode.Ignore;
						parent.Insert(insertIndex, iconElement);
					}
				}
			}

			// Replace default EdgeConnector with one using our custom listener
			if (_graphView == null) return port;

			if (port.edgeConnector != null)
			{
				port.RemoveManipulator(port.edgeConnector);
			}

			var connector = new EdgeConnector<Edge>(_graphView.EdgeConnectorListener);
			port.AddManipulator(connector);

			return port;
		}

		private Port CreateTransitionPort(TransitionPortInfo portInfo)
		{
			var port = InstantiatePort(
				Orientation.Horizontal,
				Direction.Output,
				Port.Capacity.Single,
				typeof(System.Action));

			// Store the field name in userData for connection matching.
			port.portName = portInfo.FieldName;
			port.userData = portInfo.FieldName;
			port.tooltip = portInfo.Tooltip;
			port.AddToClassList("transition-port");

			// Store port info so transition colors can be resolved later
			// (by the color application pass and by connected edges).
			_transitionPortInfos[portInfo.FieldName] = portInfo;

			// Set the visual label to the display name
			var label = port.Q<Label>();
			if (label != null)
			{
				label.text = portInfo.DisplayName;
			}

			// Remove default EdgeConnector and add our custom one
			// InstantiatePort creates EdgeConnector<Edge> by default, which we need to replace
			if (_graphView == null) return port;

			if (port.edgeConnector != null)
			{
				port.RemoveManipulator(port.edgeConnector);
			}

			var connector = new EdgeConnector<TransitionEdgeView>(_graphView.EdgeConnectorListener);
			port.AddManipulator(connector);

			return port;
		}

		/// <summary>
		/// Resolves the explicit transition color for the given transition port field name,
		/// if that port declares a valid [Transition(Color = ...)] hint. Returns false when
		/// the port is unknown or declares no valid hint, so callers can fall back to their
		/// default coloring.
		/// </summary>
		public bool TryGetTransitionPortColor(string fieldName, out Color color)
		{
			color = default;
			return fieldName != null
				&& _transitionPortInfos.TryGetValue(fieldName, out var info)
				&& info.TryGetColor(out color);
		}

		public Port GetInputPort(string portName)
		{
			if (portName == "Enter" || string.IsNullOrEmpty(portName))
			{
				return _flowInputPort;
			}

			// First try by field name (dictionary key)
			if (_inputPorts.TryGetValue(portName, out var port))
			{
				return port;
			}

			// Fall back to searching by portName (also field name now)
			foreach (var kvp in _inputPorts)
			{
				if (kvp.Value.portName == portName)
				{
					return kvp.Value;
				}
			}

			// Backwards compatibility: search by display name (label text) for old graphs
			foreach (var kvp in _inputPorts)
			{
				var label = kvp.Value.Q<Label>();
				if (label != null && label.text == portName)
				{
					return kvp.Value;
				}
			}

			return null;
		}

		public Port GetOutputPort(string portName)
		{
			// First try transition ports by field name (dictionary key)
			if (_transitionPorts.TryGetValue(portName, out var transitionPort))
			{
				return transitionPort;
			}

			// Then try output ports by field name (dictionary key)
			if (_outputPorts.TryGetValue(portName, out var outputPort))
			{
				return outputPort;
			}

			// Fall back to searching transition ports by portName (also field name now)
			foreach (var kvp in _transitionPorts)
			{
				if (kvp.Value.portName == portName)
				{
					return kvp.Value;
				}
			}

			// Fall back to searching output ports by portName
			foreach (var kvp in _outputPorts)
			{
				if (kvp.Value.portName == portName)
				{
					return kvp.Value;
				}
			}

			// Backwards compatibility: search by display name (label text) for old graphs
			foreach (var kvp in _transitionPorts)
			{
				var label = kvp.Value.Q<Label>();
				if (label != null && label.text == portName)
				{
					return kvp.Value;
				}
			}

			foreach (var kvp in _outputPorts)
			{
				var label = kvp.Value.Q<Label>();
				if (label != null && label.text == portName)
				{
					return kvp.Value;
				}
			}

			return null;
		}

		private string GetStateTypeName()
		{
			return NodeData.StateType?.Name ?? "Unknown State";
		}

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

		public bool IsActive => _activeStateIndicator.IsActive;

		public bool HasBreakpoint => _breakpointIndicator?.IsVisible ?? false;

		/// <summary>
		/// Flashes the breakpoint indicator to draw attention when this node's breakpoint is hit.
		/// </summary>
		public void FlashBreakpointHit()
		{
			_breakpointIndicator?.Flash();
		}

		/// <summary>
		/// Shows or hides a persistent red pulsing border while execution is paused on this node's
		/// breakpoint. It stays lit until execution resumes so a breakpoint pause can't be mistaken
		/// for a hang.
		/// </summary>
		public void SetBreakpointHit(bool hit)
		{
			if (_breakpointHitBorder == null) return;

			if (hit)
			{
				_breakpointHitActive = true;
				_breakpointHitPhase = 0f;
				_breakpointHitLastTime = EditorApplication.timeSinceStartup;
				_breakpointHitBorder.style.display = DisplayStyle.Flex;
				EditorApplication.update -= UpdateBreakpointHitPulse;
				EditorApplication.update += UpdateBreakpointHitPulse;
			}
			else
			{
				_breakpointHitActive = false;
				EditorApplication.update -= UpdateBreakpointHitPulse;
				_breakpointHitBorder.style.display = DisplayStyle.None;
			}
		}

		private void UpdateBreakpointHitPulse()
		{
			if (!_breakpointHitActive)
			{
				EditorApplication.update -= UpdateBreakpointHitPulse;
				return;
			}

			var now = EditorApplication.timeSinceStartup;
			_breakpointHitPhase += (float)(now - _breakpointHitLastTime) * 2f * Mathf.PI;
			_breakpointHitLastTime = now;

			var pulse = 0.5f + 0.5f * Mathf.Sin(_breakpointHitPhase);
			SetBreakpointHitBorderColor(new Color(BreakpointHitColor.r, BreakpointHitColor.g, BreakpointHitColor.b, pulse));
		}

		private void SetBreakpointHitBorderColor(Color color)
		{
			_breakpointHitBorder.style.borderTopColor = color;
			_breakpointHitBorder.style.borderBottomColor = color;
			_breakpointHitBorder.style.borderLeftColor = color;
			_breakpointHitBorder.style.borderRightColor = color;
		}

		private void UpdateBreakpointIndicator()
		{
			var graph = _graphView?.CurrentAsset;
			if (graph == null || _breakpointIndicator == null) return;

			// Transition breakpoints are drawn on the edge itself (see TransitionEdgeView); the node
			// shows an indicator only for its node-entry breakpoint (OnEntry / Conditional).
			var breakpoint = graph.GraphViewData.GetNodeEntryBreakpoint(NodeData.Id);
			if (breakpoint != null)
			{
				var isConditional = breakpoint.Type == BreakpointType.Conditional;
				_breakpointIndicator.Show(breakpoint.Enabled, isConditional);
			}
			else
			{
				_breakpointIndicator.Hide();
			}
		}

		private void HandleBreakpointToggled(StateMachineGraph graph, string nodeId)
		{
			if (graph != _graphView?.CurrentAsset) return;
			if (nodeId != null && nodeId != NodeData.Id) return;

			UpdateBreakpointIndicator();
		}
	}
}
