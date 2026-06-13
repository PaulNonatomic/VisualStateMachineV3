using Nonatomic.VSM.Graph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Visual representation of a blackboard variable node in the graph.
	/// </summary>
	public class BlackboardVariableNodeView : Node, INodeDataProvider, IPortProvider
	{
		public BlackboardVariableNode NodeData { get; }
		GraphNode INodeDataProvider.NodeData => NodeData;

		private readonly StateMachineGraphView _graphView;
		private Port _port;
		private Label _runtimeValueLabel;

		public BlackboardVariableNodeView(BlackboardVariableNode varNode, StateMachineGraphView graphView)
		{
			NodeData = varNode;
			_graphView = graphView;

			title = varNode.VariableKey;

			AddToClassList("blackboard-variable-node");
			AddToClassList($"variable-mode-{varNode.Mode.ToString().ToLower()}");

			varNode.RestoreType();

			BuildNodeUI();

			// Register context menu
			NodeContextMenuHelper.RegisterContextMenu(this, this);
		}

		private void BuildNodeUI()
		{
			var type = NodeData.VariableType ?? typeof(object);
			var typeName = GetFriendlyTypeName(type);

			if (NodeData.Mode == BlackboardNodeMode.Get)
			{
				// Getter node - has output port
				_port = InstantiatePort(
					Orientation.Horizontal,
					Direction.Output,
					Port.Capacity.Multi,
					type);
				_port.portName = typeName;
				_port.AddToClassList("data-output-port");

				// Style the type label with cyan color
				ApplyTypeColorToPortLabel(_port, typeName);

				// Remove default EdgeConnector and add our custom one
				if (_graphView != null)
				{
					if (_port.edgeConnector != null)
						_port.RemoveManipulator(_port.edgeConnector);

					var connector = new EdgeConnector<TransitionEdgeView>(_graphView.EdgeConnectorListener);
					_port.AddManipulator(connector);
				}

				outputContainer.Add(_port);

				// Style for getter
				var getterColor = new Color(0.2f, 0.4f, 0.6f);
				titleContainer.style.backgroundColor = new StyleColor(getterColor);
				_port.portColor = getterColor;
			}
			else
			{
				// Setter node - has input port
				_port = InstantiatePort(
					Orientation.Horizontal,
					Direction.Input,
					Port.Capacity.Single,
					type);
				_port.portName = typeName;
				_port.AddToClassList("data-input-port");

				// Style the type label with cyan color
				ApplyTypeColorToPortLabel(_port, typeName);

				// Remove default EdgeConnector and add our custom one
				if (_graphView != null)
				{
					if (_port.edgeConnector != null)
						_port.RemoveManipulator(_port.edgeConnector);

					var connector = new EdgeConnector<TransitionEdgeView>(_graphView.EdgeConnectorListener);
					_port.AddManipulator(connector);
				}

				inputContainer.Add(_port);

				// Style for setter
				var setterColor = new Color(0.6f, 0.4f, 0.2f);
				titleContainer.style.backgroundColor = new StyleColor(setterColor);
				_port.portColor = setterColor;
			}

			// Runtime value display (hidden until play mode)
			_runtimeValueLabel = new Label();
			_runtimeValueLabel.style.paddingLeft = 8;
			_runtimeValueLabel.style.paddingRight = 8;
			_runtimeValueLabel.style.paddingTop = 4;
			_runtimeValueLabel.style.paddingBottom = 4;
			_runtimeValueLabel.style.fontSize = 11;
			_runtimeValueLabel.style.color = new StyleColor(new Color(0.6f, 1f, 0.6f, 0.9f));
			_runtimeValueLabel.style.display = DisplayStyle.None;
			_runtimeValueLabel.style.maxWidth = 160;
			_runtimeValueLabel.style.overflow = Overflow.Hidden;
			_runtimeValueLabel.style.textOverflow = TextOverflow.Ellipsis;
			extensionContainer.Add(_runtimeValueLabel);

			RefreshExpandedState();
			RefreshPorts();
		}

		/// <summary>
		/// Updates the displayed runtime value from a live blackboard.
		/// </summary>
		public void UpdateRuntimeValue(Nonatomic.VSM.Blackboard.Blackboard blackboard)
		{
			if (_runtimeValueLabel == null) return;

			if (blackboard == null)
			{
				_runtimeValueLabel.style.display = DisplayStyle.None;
				return;
			}

			var key = NodeData.VariableKey;
			if (string.IsNullOrEmpty(key) || !blackboard.HasKey(key))
			{
				_runtimeValueLabel.style.display = DisplayStyle.None;
				return;
			}

			var value = blackboard.GetBoxed(key);
			_runtimeValueLabel.text = value?.ToString() ?? "null";
			_runtimeValueLabel.style.display = DisplayStyle.Flex;

			// Ensure extension container is visible
			RefreshExpandedState();
		}

		/// <summary>
		/// Hides the runtime value display.
		/// </summary>
		public void ClearRuntimeValue()
		{
			if (_runtimeValueLabel == null) return;
			_runtimeValueLabel.style.display = DisplayStyle.None;
		}

		public Port GetInputPort(string portName)
		{
			return NodeData.Mode == BlackboardNodeMode.Set ? _port : null;
		}

		public Port GetOutputPort(string portName)
		{
			return NodeData.Mode == BlackboardNodeMode.Get ? _port : null;
		}

		private void ApplyTypeColorToPortLabel(Port port, string typeName)
		{
			var label = port.Q<Label>();
			if (label != null)
			{
				label.text = typeName;
				label.style.color = new StyleColor(new Color(0.5f, 0.8f, 0.9f, 0.9f)); // Cyan tint
			}
		}

		private string GetFriendlyTypeName(System.Type type)
		{
			if (type == typeof(int)) return "int";
			if (type == typeof(float)) return "float";
			if (type == typeof(bool)) return "bool";
			if (type == typeof(string)) return "string";
			if (type == typeof(Vector2)) return "Vector2";
			if (type == typeof(Vector3)) return "Vector3";
			if (type == typeof(Color)) return "Color";

			if (type.IsGenericType)
			{
				var baseName = type.Name.Substring(0, type.Name.IndexOf('`'));
				var args = type.GetGenericArguments();
				var argNames = new string[args.Length];
				for (var i = 0; i < args.Length; i++)
				{
					argNames[i] = GetFriendlyTypeName(args[i]);
				}
				return $"{baseName}<{string.Join(", ", argNames)}>";
			}

			return type.Name;
		}
	}
}
