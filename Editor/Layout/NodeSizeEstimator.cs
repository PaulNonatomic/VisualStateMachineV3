using Nonatomic.VSM.Editor.Ports;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Editor.Layout
{
	/// <summary>
	/// Estimates node dimensions for layout calculations.
	/// Sizes are based on UIElements rendering measurements.
	/// Values are intentionally generous to prevent overlap.
	/// </summary>
	public static class NodeSizeEstimator
	{
		// Base widths from USS styles (with padding for content)
		private const float EntryExitMinWidth = 120f;
		private const float BlackboardMinWidth = 160f;
		private const float StateMinWidth = 280f;
		private const float SubStateMachineMinWidth = 280f;
		private const float PortalNodeMinWidth = 180f;

		// Height components (generous estimates based on UIElements rendering)
		private const float TitleBarHeight = 36f;
		private const float PortRowHeight = 32f;
		private const float FooterHeight = 28f;
		private const float ContentPadding = 16f;

		// Port row layout
		private const float PortDotSize = 16f;
		private const float PortMargin = 8f;
		private const float PortGap = 20f;
		private const float CharWidth = 7.5f;
		private const float IconSize = 24f;
		private const float TitlePadding = 40f;

		// Portal node specific (has channel badge container)
		private const float PortalNodeExtensionHeight = 50f;

		// SubStateMachine specific
		private const float SubStateMachineInfoHeight = 60f;

		/// <summary>
		/// Estimates the dimensions of a graph node.
		/// </summary>
		public static Vector2 EstimateSize(GraphNode node)
		{
			if (node == null) return new Vector2(StateMinWidth, 120f);

			return node.NodeType switch
			{
				GraphNodeType.Entry => EstimateEntryNodeSize(),
				GraphNodeType.Exit => EstimateExitNodeSize(),
				GraphNodeType.State => EstimateStateNodeSize(node as StateNode),
				GraphNodeType.SubStateMachine => EstimateSubStateMachineSize(node as SubStateMachineNode),
				GraphNodeType.BlackboardVariable => EstimateBlackboardNodeSize(),
				GraphNodeType.PortalIn => EstimatePortalInNodeSize(),
				GraphNodeType.PortalOut => EstimatePortalOutNodeSize(),
				_ => new Vector2(StateMinWidth, 120f)
			};
		}

		private static Vector2 EstimateEntryNodeSize()
		{
			// Entry node: Title + 1 output port + footer (in title container)
			var height = TitleBarHeight + PortRowHeight + ContentPadding;
			return new Vector2(EntryExitMinWidth, height);
		}

		private static Vector2 EstimateExitNodeSize()
		{
			// Exit node: Title + 1 input port + footer (in title container)
			var height = TitleBarHeight + PortRowHeight + ContentPadding;
			return new Vector2(EntryExitMinWidth, height);
		}

		private static Vector2 EstimateStateNodeSize(StateNode stateNode)
		{
			if (stateNode == null)
			{
				return new Vector2(StateMinWidth, 120f);
			}

			// Get port counts from the state type
			var inputPortCount = 1; // Always has "Enter" flow input
			var outputPortCount = 0;
			var width = StateMinWidth;

			stateNode.RestoreType();
			if (stateNode.StateType != null)
			{
				var manifest = PortDiscoveryService.GetPortManifest(stateNode.StateType);
				inputPortCount += manifest.InputPorts.Count;
				outputPortCount = manifest.OutputPorts.Count + manifest.TransitionPorts.Count;

				// Estimate width by pairing input and output rows side by side
				var maxRows = Mathf.Max(inputPortCount, outputPortCount);
				for (int i = 0; i < maxRows; i++)
				{
					var leftWidth = 0f;
					var rightWidth = 0f;

					// Input side: port dot + label + type label + inline field
					if (i == 0)
					{
						// "Enter" flow port
						leftWidth = EstimateLabelWidth("Enter") + PortDotSize + PortMargin;
					}
					else if (i - 1 < manifest.InputPorts.Count)
					{
						var port = manifest.InputPorts[i - 1];
						leftWidth = EstimateLabelWidth(port.DisplayName)
							+ EstimateTypeLabelWidth(port.DataType)
							+ EstimateInlineFieldWidth(port.DataType)
							+ PortDotSize + PortMargin;
					}

					// Output side: label + type label + port dot
					if (i < manifest.TransitionPorts.Count)
					{
						var port = manifest.TransitionPorts[i];
						rightWidth = EstimateLabelWidth(port.DisplayName) + PortDotSize + PortMargin;
					}
					else
					{
						var outIdx = i - manifest.TransitionPorts.Count;
						if (outIdx >= 0 && outIdx < manifest.OutputPorts.Count)
						{
							var port = manifest.OutputPorts[outIdx];
							rightWidth = EstimateLabelWidth(port.DisplayName)
								+ EstimateTypeLabelWidth(port.DataType)
								+ PortDotSize + PortMargin;
						}
					}

					var rowWidth = leftWidth + rightWidth + PortGap;
					width = Mathf.Max(width, rowWidth);
				}

				// Also check title width
				var titleWidth = EstimateLabelWidth(stateNode.DisplayName) + IconSize + TitlePadding;
				width = Mathf.Max(width, titleWidth);
			}

			// Calculate height based on port count
			var maxPorts = Mathf.Max(inputPortCount, outputPortCount);
			maxPorts = Mathf.Max(maxPorts, 1);
			var contentHeight = maxPorts * PortRowHeight;

			var height = TitleBarHeight + contentHeight + FooterHeight + ContentPadding;

			return new Vector2(width, height);
		}

		private static float EstimateLabelWidth(string label)
		{
			if (string.IsNullOrEmpty(label)) return 0f;
			return label.Length * CharWidth;
		}

		private static float EstimateTypeLabelWidth(System.Type dataType)
		{
			if (dataType == null) return 0f;
			var typeName = dataType.Name;
			// Generic types like List<T> show as "IList" etc.
			if (dataType.IsGenericType)
			{
				typeName = dataType.Name.Split('`')[0];
			}
			// Wrap in parentheses: " (TypeName)"
			return (typeName.Length + 3) * CharWidth;
		}

		private static float EstimateInlineFieldWidth(System.Type dataType)
		{
			if (dataType == null) return 0f;

			// Based on the inline field widths in StateNodeView
			if (dataType == typeof(string)) return 120f;
			if (dataType == typeof(int)) return 60f;
			if (dataType == typeof(float)) return 60f;
			if (dataType == typeof(bool)) return 30f;
			if (dataType == typeof(Vector2)) return 120f;
			if (dataType == typeof(Vector3)) return 160f;
			if (dataType == typeof(Vector4)) return 200f;
			if (dataType == typeof(Quaternion)) return 160f;
			if (dataType == typeof(Color)) return 60f;
			if (dataType == typeof(LayerMask)) return 100f;
			if (dataType.IsEnum) return 80f;
			if (typeof(UnityEngine.Object).IsAssignableFrom(dataType)) return 120f;

			return 60f; // Default for unknown types
		}

		private static Vector2 EstimateSubStateMachineSize(SubStateMachineNode subNode)
		{
			// SubStateMachine has:
			// - 2 input ports (Enter + SubGraph data input with ObjectField)
			// - 1 output port (Exit)
			// - Info display area (name + count labels + warning)
			// - Open button in title bar
			// - Footer
			var inputPortCount = 2;
			var outputPortCount = 1;

			var maxPorts = Mathf.Max(inputPortCount, outputPortCount);
			var contentHeight = maxPorts * PortRowHeight;

			var height = TitleBarHeight + contentHeight + SubStateMachineInfoHeight + FooterHeight + ContentPadding;

			return new Vector2(SubStateMachineMinWidth, height);
		}

		private static Vector2 EstimateBlackboardNodeSize()
		{
			// Blackboard variable node: Title + 1 port + footer (in title)
			var height = TitleBarHeight + PortRowHeight + ContentPadding;
			return new Vector2(BlackboardMinWidth, height);
		}

		private static Vector2 EstimatePortalInNodeSize()
		{
			// PortalIn node has:
			// - Title bar
			// - Extension container with channel badge, label, and IntegerField
			// - 1 output port
			// - Footer in title bar
			var height = TitleBarHeight + PortalNodeExtensionHeight + PortRowHeight + ContentPadding;
			return new Vector2(PortalNodeMinWidth, height);
		}

		private static Vector2 EstimatePortalOutNodeSize()
		{
			// PortalOut node has:
			// - Title bar
			// - 1 input port
			// - Extension container with channel badge, label, and IntegerField
			// - Footer in title bar
			var height = TitleBarHeight + PortRowHeight + PortalNodeExtensionHeight + ContentPadding;
			return new Vector2(PortalNodeMinWidth, height);
		}
	}
}
