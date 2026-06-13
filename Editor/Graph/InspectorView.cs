using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Floating inspector panel for editing selected node properties.
	/// </summary>
	public class InspectorView : FloatingPanel
	{
		private readonly StateMachineGraphWindow _window;
		private GraphNode _selectedNode;

		private Label _noSelectionLabel;

		public InspectorView(StateMachineGraphWindow window)
			: base("Inspector", "Node Inspector", new Vector2(-10, 10), new Vector2(280, 350))
		{
			_window = window;

			AddToClassList("inspector-view");

			// Position on the right side
			RegisterCallback<GeometryChangedEvent>(OnFirstLayout);

			BuildContent();
		}

		private bool _hasInitializedPosition;

		private void OnFirstLayout(GeometryChangedEvent evt)
		{
			if (_hasInitializedPosition) return;
			_hasInitializedPosition = true;

			// Reposition to right side if still at default position
			if (parent != null && resolvedStyle.left < 0)
			{
				var parentWidth = parent.resolvedStyle.width;
				style.left = parentWidth - resolvedStyle.width - 20;
			}
		}

		private void BuildContent()
		{
			_noSelectionLabel = new Label("No node selected");
			_noSelectionLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
			_noSelectionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
			_noSelectionLabel.style.marginTop = 20;
			ContentContainer.Add(_noSelectionLabel);
		}

		public void ShowNodeInspector(GraphNode node)
		{
			_selectedNode = node;
			ContentContainer.Clear();

			if (node == null)
			{
				ContentContainer.Add(_noSelectionLabel);
				return;
			}

			switch (node.NodeType)
			{
				case GraphNodeType.State:
					ShowStateInspector(node as StateNode);
					break;
				case GraphNodeType.Entry:
					ShowEntryInspector(node as EntryNode);
					break;
				case GraphNodeType.Exit:
					ShowExitInspector(node as ExitNode);
					break;
				case GraphNodeType.BlackboardVariable:
					ShowBlackboardVariableInspector(node as BlackboardVariableNode);
					break;
				case GraphNodeType.SubStateMachine:
					ShowSubStateMachineInspector(node as SubStateMachineNode);
					break;
				case GraphNodeType.PortalOut:
					ShowPortalInspector(
						node as PortalOutNode,
						"Portal Out Node",
						"Teleports execution to the matching Portal In node with the same channel.");
					break;
				case GraphNodeType.PortalIn:
					ShowPortalInspector(
						node as PortalInNode,
						"Portal In Node",
						"Receives execution from Portal Out nodes with the same channel.");
					break;
			}
		}

		private void ShowStateInspector(StateNode stateNode)
		{
			var typeLabel = new Label("State Node");
			typeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			typeLabel.style.marginBottom = 8;
			ContentContainer.Add(typeLabel);

			var nameField = new TextField("Display Name");
			nameField.value = stateNode.DisplayName ?? "";
			nameField.RegisterValueChangedCallback(evt =>
			{
				RecordUndo("Change State Name");
				stateNode.DisplayName = evt.newValue;
				MarkDirty();
			});
			ContentContainer.Add(nameField);

			stateNode.RestoreType();
			var stateTypeLabel = new Label($"Type: {stateNode.StateType?.Name ?? "Unknown"}");
			stateTypeLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
			stateTypeLabel.style.marginTop = 8;
			ContentContainer.Add(stateTypeLabel);

			var commentField = new TextField("Comment");
			commentField.multiline = true;
			commentField.value = stateNode.Comment ?? "";
			commentField.style.height = 60;
			commentField.RegisterValueChangedCallback(evt =>
			{
				RecordUndo("Change State Comment");
				stateNode.Comment = evt.newValue;
				MarkDirty();
			});
			ContentContainer.Add(commentField);

			if (stateNode.PropertyOverrides.Count > 0)
			{
				var overridesHeader = new Label("Property Overrides");
				overridesHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
				overridesHeader.style.marginTop = 16;
				overridesHeader.style.marginBottom = 4;
				ContentContainer.Add(overridesHeader);

				foreach (var propertyOverride in stateNode.PropertyOverrides)
				{
					var overrideLabel = new Label($"{propertyOverride.FieldName}: {propertyOverride.JsonValue}");
					overrideLabel.style.marginLeft = 8;
					ContentContainer.Add(overrideLabel);
				}
			}
		}

		private void ShowEntryInspector(EntryNode entryNode)
		{
			var typeLabel = new Label("Entry Node");
			typeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			typeLabel.style.marginBottom = 8;
			ContentContainer.Add(typeLabel);

			var infoLabel = new Label("This is the starting point of the state machine.");
			infoLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
			infoLabel.style.whiteSpace = WhiteSpace.Normal;
			ContentContainer.Add(infoLabel);
		}

		private void ShowExitInspector(ExitNode exitNode)
		{
			var typeLabel = new Label("Exit Node");
			typeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			typeLabel.style.marginBottom = 8;
			ContentContainer.Add(typeLabel);

			var nameField = new TextField("Exit Name");
			nameField.value = exitNode.ExitName ?? "Exit";
			nameField.RegisterValueChangedCallback(evt =>
			{
				RecordUndo("Change Exit Name");
				exitNode.ExitName = evt.newValue;
				exitNode.DisplayName = evt.newValue;
				MarkDirty();
			});
			ContentContainer.Add(nameField);

			var codeField = new IntegerField("Exit Code");
			codeField.value = exitNode.ExitCode;
			codeField.RegisterValueChangedCallback(evt =>
			{
				RecordUndo("Change Exit Code");
				exitNode.ExitCode = evt.newValue;
				MarkDirty();
			});
			ContentContainer.Add(codeField);
		}

		private void ShowBlackboardVariableInspector(BlackboardVariableNode varNode)
		{
			var typeLabel = new Label("Blackboard Variable Node");
			typeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			typeLabel.style.marginBottom = 8;
			ContentContainer.Add(typeLabel);

			var keyLabel = new Label($"Variable: {varNode.VariableKey}");
			ContentContainer.Add(keyLabel);

			var modeLabel = new Label($"Mode: {varNode.Mode}");
			modeLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
			ContentContainer.Add(modeLabel);

			varNode.RestoreType();
			var valueTypeLabel = new Label($"Type: {varNode.VariableType?.Name ?? "Unknown"}");
			valueTypeLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
			ContentContainer.Add(valueTypeLabel);
		}

		private void ShowSubStateMachineInspector(SubStateMachineNode subNode)
		{
			var typeLabel = new Label("Sub-State Machine Node");
			typeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			typeLabel.style.marginBottom = 8;
			ContentContainer.Add(typeLabel);

			var nameField = new TextField("Display Name");
			nameField.value = subNode.DisplayName ?? "Sub State Machine";
			nameField.RegisterValueChangedCallback(evt =>
			{
				RecordUndo("Change Sub-State Machine Name");
				subNode.DisplayName = evt.newValue;
				MarkDirty();
			});
			ContentContainer.Add(nameField);

			var dataModeField = new EnumField("Data Mode", subNode.DataMode);
			dataModeField.RegisterValueChangedCallback(evt =>
			{
				RecordUndo("Change Data Mode");
				subNode.DataMode = (SubStateMachineDataMode)evt.newValue;
				MarkDirty();
			});
			ContentContainer.Add(dataModeField);

			if (subNode.InputMappings.Count > 0 || subNode.OutputMappings.Count > 0)
			{
				var mappingsHeader = new Label("Variable Mappings");
				mappingsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
				mappingsHeader.style.marginTop = 16;
				ContentContainer.Add(mappingsHeader);

				foreach (var mapping in subNode.InputMappings)
				{
					var label = new Label($"In: {mapping.ParentVariableKey} -> {mapping.ChildVariableKey}");
					label.style.marginLeft = 8;
					ContentContainer.Add(label);
				}

				foreach (var mapping in subNode.OutputMappings)
				{
					var label = new Label($"Out: {mapping.ChildVariableKey} -> {mapping.ParentVariableKey}");
					label.style.marginLeft = 8;
					ContentContainer.Add(label);
				}
			}
		}

		private void ShowPortalInspector(IPortalNode portalNode, string typeName, string infoText)
		{
			var typeLabel = new Label(typeName);
			typeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			typeLabel.style.marginBottom = 8;
			ContentContainer.Add(typeLabel);

			var channelField = new IntegerField("Channel");
			channelField.value = portalNode.ChannelId;
			channelField.RegisterValueChangedCallback(evt =>
			{
				RecordUndo("Change Portal Channel");
				portalNode.ChannelId = evt.newValue;
				MarkDirty();
				_window.GraphView?.PopulateFromAsset(_window.TargetAsset);
			});
			ContentContainer.Add(channelField);

			var labelField = new TextField("Label");
			labelField.value = portalNode.ChannelLabel ?? "";
			labelField.RegisterValueChangedCallback(evt =>
			{
				RecordUndo("Change Portal Label");
				portalNode.ChannelLabel = evt.newValue;
				MarkDirty();
				_window.GraphView?.PopulateFromAsset(_window.TargetAsset);
			});
			ContentContainer.Add(labelField);

			var colorBadge = new VisualElement();
			colorBadge.style.width = 20;
			colorBadge.style.height = 20;
			colorBadge.style.borderTopLeftRadius = 10;
			colorBadge.style.borderTopRightRadius = 10;
			colorBadge.style.borderBottomLeftRadius = 10;
			colorBadge.style.borderBottomRightRadius = 10;
			colorBadge.style.backgroundColor = portalNode.ChannelColor;
			colorBadge.style.marginTop = 8;
			ContentContainer.Add(colorBadge);

			var infoLabel = new Label(infoText);
			infoLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
			infoLabel.style.whiteSpace = WhiteSpace.Normal;
			infoLabel.style.marginTop = 12;
			ContentContainer.Add(infoLabel);
		}

		private void RecordUndo(string name)
		{
			if (_window.TargetAsset != null)
			{
				Undo.RecordObject(_window.TargetAsset, name);
			}
		}

		private void MarkDirty()
		{
			if (_window.TargetAsset != null)
			{
				EditorUtility.SetDirty(_window.TargetAsset);
			}
		}
	}
}
