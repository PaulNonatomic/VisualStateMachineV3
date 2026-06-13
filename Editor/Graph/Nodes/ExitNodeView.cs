using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Visual representation of an exit node.
	/// </summary>
	public class ExitNodeView : Node, INodeDataProvider, IPortProvider, IActiveStateNode
	{
		public ExitNode NodeData { get; }
		GraphNode INodeDataProvider.NodeData => NodeData;

		private Port _inputPort;

		// Runtime active state visualization
		private ActiveStateIndicator _activeStateIndicator;

		public ExitNodeView(ExitNode exitNode)
		{
			NodeData = exitNode;

			title = string.IsNullOrEmpty(exitNode.ExitName) ? "Exit" : exitNode.ExitName;

			AddToClassList("exit-node");

			// Register context menu
			NodeContextMenuHelper.RegisterContextMenu(this, this);

			// Exit nodes have a single input port (flow input port)
			_inputPort = InstantiatePort(
				Orientation.Horizontal,
				Direction.Input,
				Port.Capacity.Multi,
				typeof(bool));
			_inputPort.portName = "Enter";
			_inputPort.AddToClassList("flow-port");

			// Hide the port label
			var portLabel = _inputPort.Q<Label>();
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
			portContainer.Add(_inputPort);

			// Add port container to title container (insert at beginning for input port)
			titleContainer.Insert(0, portContainer);

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
			var titleColor = new Color(0.6f, 0.2f, 0.2f);
			titleContainer.style.backgroundColor = new StyleColor(titleColor);
			_inputPort.portColor = titleColor;

			// Add active state indicator (persists on deactivate - exit nodes stay highlighted)
			_activeStateIndicator = new ActiveStateIndicator(this, this, null, 8f, persistOnDeactivate: true);

			RefreshExpandedState();
			RefreshPorts();
		}

		public Port GetInputPort(string portName)
		{
			return _inputPort;
		}

		public Port GetOutputPort(string portName) => null;

		/// <summary>
		/// Sets whether this node is currently the active state in runtime.
		/// Exit nodes stay highlighted until the Entry node is entered again.
		/// </summary>
		public void SetActiveState(bool isActive)
		{
			_activeStateIndicator.SetActiveState(isActive);
		}

		/// <summary>
		/// Forces the exit node to clear its highlight.
		/// Called when the graph is reloaded or play mode exits.
		/// </summary>
		public void ForceDeactivate()
		{
			_activeStateIndicator.ForceDeactivate();
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
