using Nonatomic.VSM.Graph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Visual representation of the entry node.
	/// </summary>
	public class EntryNodeView : Node, INodeDataProvider, IPortProvider
	{
		public EntryNode NodeData { get; }
		GraphNode INodeDataProvider.NodeData => NodeData;

		private Port _outputPort;
		private readonly StateMachineGraphView _graphView;

		public EntryNodeView(EntryNode entryNode, StateMachineGraphView graphView = null)
		{
			NodeData = entryNode;
			_graphView = graphView;

			title = "Entry";

			// Prevent deletion via delete key and context menu
			capabilities &= ~Capabilities.Deletable;

			AddToClassList("entry-node");

			// Register context menu
			NodeContextMenuHelper.RegisterContextMenu(this, this);

			// Entry nodes have a single output port - use Action type to identify as transition
			_outputPort = InstantiatePort(
				Orientation.Horizontal,
				Direction.Output,
				Port.Capacity.Single,
				typeof(System.Action));
			_outputPort.portName = "Start";
			_outputPort.AddToClassList("transition-port");

			// Remove default EdgeConnector and add our custom one
			// InstantiatePort creates EdgeConnector<Edge> by default, which we need to replace
			if (_graphView != null)
			{
				if (_outputPort.edgeConnector != null)
					_outputPort.RemoveManipulator(_outputPort.edgeConnector);

				var connector = new EdgeConnector<TransitionEdgeView>(_graphView.EdgeConnectorListener);
				_outputPort.AddManipulator(connector);
			}

			// Hide the port label
			var portLabel = _outputPort.Q<Label>();
			if (portLabel != null)
			{
				portLabel.style.display = DisplayStyle.None;
			}

			// Create port container with separate background
			var portContainer = new VisualElement();
			portContainer.name = "port-container";
			portContainer.style.flexDirection = FlexDirection.Row;
			portContainer.style.alignItems = Align.Center;
			portContainer.style.backgroundColor = new StyleColor(new Color(0.165f, 0.165f, 0.165f));
			portContainer.style.paddingLeft = 4;
			portContainer.style.paddingRight = 4;

			// Add port to container
			portContainer.Add(_outputPort);

			// Add port container to title container
			titleContainer.Add(portContainer);

			// Style the title label
			var titleLabel = this.Q<Label>("title-label");
			if (titleLabel != null)
			{
				titleLabel.style.marginRight = 10;
			}

			// Hide the contents container
			var contents = this.Q("contents");
			if (contents != null)
			{
				contents.style.display = DisplayStyle.None;
			}

			// Hide the title button container
			var titleButtonContainer = this.Q("title-button-container");
			if (titleButtonContainer != null)
			{
				titleButtonContainer.style.display = DisplayStyle.None;
			}

			// Style the node
			var titleColor = new Color(0.2f, 0.6f, 0.2f);
			titleContainer.style.backgroundColor = new StyleColor(titleColor);
			_outputPort.portColor = titleColor;

			RefreshExpandedState();
			RefreshPorts();
		}

		public Port GetInputPort(string portName) => null;

		public Port GetOutputPort(string portName)
		{
			return _outputPort;
		}
	}
}
