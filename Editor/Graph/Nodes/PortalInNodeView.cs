using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Visual representation of a portal-in node.
	/// </summary>
	public class PortalInNodeView : Node, INodeDataProvider, IPortProvider, IActiveStateNode
	{
		public PortalInNode NodeData { get; }
		GraphNode INodeDataProvider.NodeData => NodeData;

		private readonly StateMachineGraphView _graphView;
		private Port _outputPort;
		private IntegerField _channelField;
		private VisualElement _orphanIndicator;
		private ActiveStateIndicator _activeStateIndicator;
		private bool _isOrphaned;

		public PortalInNodeView(PortalInNode portalInNode, StateMachineGraphView graphView = null)
		{
			NodeData = portalInNode;
			_graphView = graphView;

			title = "Portal In";

			AddToClassList("portal-in-node");

			BuildNodeUI();

			// Register context menu
			NodeContextMenuHelper.RegisterContextMenu(this, this);
		}

		private void BuildNodeUI()
		{
			// Add orphan indicator (warning border)
			_orphanIndicator = new VisualElement();
			_orphanIndicator.AddToClassList("orphan-indicator");
			_orphanIndicator.style.display = DisplayStyle.None;
			_orphanIndicator.style.position = Position.Absolute;
			_orphanIndicator.style.top = 0;
			_orphanIndicator.style.left = 0;
			_orphanIndicator.style.right = 0;
			_orphanIndicator.style.bottom = 0;
			_orphanIndicator.style.borderTopWidth = 2;
			_orphanIndicator.style.borderBottomWidth = 2;
			_orphanIndicator.style.borderLeftWidth = 2;
			_orphanIndicator.style.borderRightWidth = 2;
			_orphanIndicator.style.borderTopColor = new Color(0.9f, 0.6f, 0.1f);
			_orphanIndicator.style.borderBottomColor = new Color(0.9f, 0.6f, 0.1f);
			_orphanIndicator.style.borderLeftColor = new Color(0.9f, 0.6f, 0.1f);
			_orphanIndicator.style.borderRightColor = new Color(0.9f, 0.6f, 0.1f);
			_orphanIndicator.pickingMode = PickingMode.Ignore;
			mainContainer.Add(_orphanIndicator);

			// Add active state indicator
			_activeStateIndicator = new ActiveStateIndicator(this, mainContainer, titleContainer);

			// Output port (transition output) - use Action type for transitions
			_outputPort = InstantiatePort(
				Orientation.Horizontal,
				Direction.Output,
				Port.Capacity.Single,
				typeof(System.Action));
			_outputPort.portName = "Continue";
			_outputPort.AddToClassList("transition-port");

			// Replace default EdgeConnector with TransitionEdgeView connector
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
			portContainer.style.backgroundColor = new StyleColor(new Color(0.165f, 0.165f, 0.165f)); // #2a2a2a
			portContainer.style.paddingLeft = 10;
			portContainer.style.paddingRight = 4;

			// Channel label
			var channelLabel = new Label("Ch");
			channelLabel.style.marginRight = 4;
			portContainer.Add(channelLabel);

			// Channel integer field
			_channelField = new IntegerField();
			_channelField.name = "channel-field";
			_channelField.value = NodeData.ChannelId;
			_channelField.style.width = 30;
			_channelField.style.marginRight = 4;
			_channelField.RegisterValueChangedCallback(OnChannelChanged);
			portContainer.Add(_channelField);

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

			// Update channel display
			UpdateChannelDisplay();

			RefreshExpandedState();
			RefreshPorts();
		}

		public void UpdateChannelDisplay()
		{
			var color = NodeData.ChannelColor;

			// Update title bar color (darker version of channel color)
			var titleColor = new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f);
			titleContainer.style.backgroundColor = new StyleColor(titleColor);

			// Update port color to match title
			_outputPort.portColor = titleColor;

			// Update orphan status
			UpdateOrphanStatus();
		}

		private void OnChannelChanged(ChangeEvent<int> evt)
		{
			if (evt.newValue == NodeData.ChannelId) return;

			// Record undo
			if (_graphView?.CurrentAsset != null)
			{
				Undo.RecordObject(_graphView.CurrentAsset, "Change Portal Channel");
			}

			NodeData.ChannelId = evt.newValue;
			UpdateChannelDisplay();

			// Mark asset dirty and refresh all portal nodes
			if (_graphView?.CurrentAsset != null)
			{
				_graphView.MarkAssetDirty();
				_graphView.RefreshPortalNodeOrphanStatus();
			}
		}

		/// <summary>
		/// Updates the orphan status visual indicator.
		/// A PortalIn is orphaned if no PortalOut exists with the same channel ID.
		/// </summary>
		public void UpdateOrphanStatus()
		{
			var hasMatchingPortalOut = false;

			if (_graphView?.CurrentAsset != null)
			{
				foreach (var node in _graphView.CurrentAsset.GetAllNodes())
				{
					if (node is PortalOutNode portalOut && portalOut.ChannelId == NodeData.ChannelId)
					{
						hasMatchingPortalOut = true;
						break;
					}
				}
			}

			_isOrphaned = !hasMatchingPortalOut;
			_orphanIndicator.style.display = _isOrphaned ? DisplayStyle.Flex : DisplayStyle.None;

			if (_isOrphaned)
			{
				tooltip = $"No Portal Out node found for Channel {NodeData.ChannelId}";
			}
			else
			{
				tooltip = "";
			}
		}

		public Port GetInputPort(string portName) => null;

		public Port GetOutputPort(string portName) => _outputPort;

		public void SetActiveState(bool isActive)
		{
			_activeStateIndicator.SetActiveState(isActive);
		}

		/// <summary>
		/// Briefly flashes the highlight for a transient visit.
		/// </summary>
		public void FlashHighlight()
		{
			_activeStateIndicator.FlashHighlight();
		}

		public bool IsActive => _activeStateIndicator.IsActive;
	}
}
