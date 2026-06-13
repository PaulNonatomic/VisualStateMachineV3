using System.Collections.Generic;
using System.Linq;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Editor.Layout
{
	/// <summary>
	/// Provides automatic layout algorithms for state machine graphs.
	/// </summary>
	public static class GraphLayoutService
	{
		private static readonly Dictionary<System.Type, List<string>> PortOrderCache = new();
		private static readonly Dictionary<System.Type, List<string>> InputPortOrderCache = new();

		// Margins between nodes during auto layout
		private const float HorizontalMargin = 100f;
		private const float VerticalMargin = 66f;

		// Grid layout spacing
		private const float GridHorizontalSpacing = 380f;
		private const float GridVerticalSpacing = 200f;
		private const float StartX = 100f;
		private const float StartY = 300f;

		/// <summary>
		/// Automatically arranges all nodes in the graph using a hierarchical layout.
		/// </summary>
		public static void AutoLayout(StateMachineGraph graph, Dictionary<string, Vector2> nodeSizes = null)
		{
			if (graph == null) return;

			var nodes = graph.GetAllNodes().ToList();
			if (nodes.Count == 0) return;

			// Step 1: Place all nodes at the origin
			foreach (var node in nodes)
			{
				node.Position = Vector2.zero;
			}

			// Step 2: Calculate distance from entry node via BFS over transition connections
			var distances = CalculateDistancesFromEntry(graph, nodes);

			// Step 3: Stack highest scoring leaf nodes vertically at x=0, park others out of the way
			var maxDistance = distances.Count > 0 ? distances.Values.Max() : 0;
			var leafNodes = new List<GraphNode>();

			Debug.Log($"[AutoLayout] Total nodes: {nodes.Count}, Distances found: {distances.Count}, Max distance: {maxDistance}");
			foreach (var kvp in distances)
			{
				var n = nodes.FirstOrDefault(x => x.Id == kvp.Key);
				Debug.Log($"[AutoLayout]   {n?.DisplayName ?? kvp.Key}: distance={kvp.Value}");
			}

			foreach (var node in nodes)
			{
				if (!distances.TryGetValue(node.Id, out var distance))
				{
					node.Position = new Vector2(-2000f, -2000f);
					continue;
				}

				if (distance == maxDistance)
				{
					leafNodes.Add(node);
				}
				else
				{
					node.Position = new Vector2(-2000f, -2000f);
				}
			}

			Debug.Log($"[AutoLayout] Leaf nodes at max distance: {leafNodes.Count}");
			var positioned = new HashSet<string>();
			var currentY = 0f;
			foreach (var leaf in leafNodes)
			{
				Debug.Log($"[AutoLayout]   Placing '{leaf.DisplayName}' at (0, {currentY})");
				leaf.Position = new Vector2(0f, currentY);
				positioned.Add(leaf.Id);
				var height = GetNodeSize(leaf, nodeSizes).y;
				currentY += height + VerticalMargin;
			}

			// Step 4: Work backwards from leaf nodes, positioning input and output nodes
			var processingQueue = new Queue<GraphNode>(leafNodes);
			while (processingQueue.Count > 0)
			{
				var current = processingQueue.Dequeue();

				// Position unpositioned input nodes to the left
				var newInputs = PositionInputNodes(graph, current, nodes, positioned, nodeSizes);
				foreach (var input in newInputs)
				{
					processingQueue.Enqueue(input);
				}

				// Position unpositioned output nodes to the right
				var newOutputs = PositionOutputNodes(graph, current, nodes, positioned, nodeSizes);
				foreach (var output in newOutputs)
				{
					processingQueue.Enqueue(output);
				}
			}

			// Step 5: Centre the graph on the entry node
			var entryNode = nodes.FirstOrDefault(n => n.Id == graph.EntryNodeId);
			if (entryNode != null)
			{
				var offset = entryNode.Position;
				foreach (var node in nodes)
				{
					node.Position -= offset;
				}
			}
		}

		/// <summary>
		/// Arranges nodes in a simple grid layout.
		/// </summary>
		public static void GridLayout(StateMachineGraph graph, int columns = 4)
		{
			if (graph == null) return;

			var nodes = graph.GetAllNodes().ToList();
			if (nodes.Count == 0) return;

			nodes = nodes.OrderBy(n => n.NodeType switch
			{
				GraphNodeType.Entry => 0,
				GraphNodeType.State => 1,
				GraphNodeType.BlackboardVariable => 2,
				GraphNodeType.SubStateMachine => 3,
				GraphNodeType.Exit => 4,
				_ => 5
			}).ThenBy(n => n.DisplayName).ToList();

			for (int i = 0; i < nodes.Count; i++)
			{
				int col = i % columns;
				int row = i / columns;

				nodes[i].Position = new Vector2(
					StartX + col * GridHorizontalSpacing,
					StartY + row * GridVerticalSpacing
				);
			}
		}

		#region Node Positioning

		private static List<GraphNode> PositionInputNodes(
			StateMachineGraph graph,
			GraphNode targetNode,
			List<GraphNode> allNodes,
			HashSet<string> positioned,
			Dictionary<string, Vector2> nodeSizes)
		{
			var newlyPositioned = new List<GraphNode>();
			var inputConnections = graph.GetConnectionsToNode(targetNode.Id)
				.OrderBy(c => GetInputPortOrderIndex(targetNode, c.TargetPortName))
				.ToList();
			var inputNodes = new List<GraphNode>();

			foreach (var connection in inputConnections)
			{
				var sourceNode = allNodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
				if (sourceNode != null && !positioned.Contains(sourceNode.Id) && !inputNodes.Contains(sourceNode))
				{
					inputNodes.Add(sourceNode);
				}
			}

			if (inputNodes.Count == 0) return newlyPositioned;

			var rightEdgeX = targetNode.Position.x - HorizontalMargin;
			var currentY = targetNode.Position.y;

			foreach (var inputNode in inputNodes)
			{
				var size = GetNodeSize(inputNode, nodeSizes);
				var x = rightEdgeX - size.x;
				var nodeRect = new Rect(x, currentY, size.x, size.y);

				Rect? overlap;
				while ((overlap = FindOverlap(nodeRect, inputNode.Id, allNodes, positioned, nodeSizes)) != null)
				{
					currentY = overlap.Value.yMax + VerticalMargin;
					nodeRect.y = currentY;
				}

				inputNode.Position = new Vector2(x, currentY);
				positioned.Add(inputNode.Id);
				newlyPositioned.Add(inputNode);

				Debug.Log($"[AutoLayout]   Input '{inputNode.DisplayName}' at ({x}, {currentY})");
				currentY += size.y + VerticalMargin;
			}

			return newlyPositioned;
		}

		private static List<GraphNode> PositionOutputNodes(
			StateMachineGraph graph,
			GraphNode sourceNode,
			List<GraphNode> allNodes,
			HashSet<string> positioned,
			Dictionary<string, Vector2> nodeSizes)
		{
			var newlyPositioned = new List<GraphNode>();
			var outputConnections = graph.GetConnectionsFromNode(sourceNode.Id)
				.OrderBy(c => GetOutputPortOrderIndex(sourceNode, c.SourcePortName))
				.ToList();
			var outputNodes = new List<GraphNode>();

			var sourceSize = GetNodeSize(sourceNode, nodeSizes);
			var x = sourceNode.Position.x + sourceSize.x + HorizontalMargin;

			foreach (var connection in outputConnections)
			{
				var targetNode = allNodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);
				if (targetNode == null || outputNodes.Contains(targetNode)) continue;

				if (!positioned.Contains(targetNode.Id))
				{
					outputNodes.Add(targetNode);
				}
				else if (targetNode.Position.x < x)
				{
					outputNodes.Add(targetNode);
				}
			}

			if (outputNodes.Count == 0) return newlyPositioned;

			// Find the lowest bottom edge of already-positioned output nodes
			var lowestY = float.MinValue;
			foreach (var connection in graph.GetConnectionsFromNode(sourceNode.Id))
			{
				var targetNode = allNodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);
				if (targetNode != null && positioned.Contains(targetNode.Id))
				{
					var targetSize = GetNodeSize(targetNode, nodeSizes);
					var bottomY = targetNode.Position.y + targetSize.y;
					if (bottomY > lowestY) lowestY = bottomY;
				}
			}

			// If no positioned output nodes exist, start at the source node's Y
			var currentY = lowestY > float.MinValue
				? lowestY + VerticalMargin
				: sourceNode.Position.y;

			foreach (var outputNode in outputNodes)
			{
				var size = GetNodeSize(outputNode, nodeSizes);
				var nodeRect = new Rect(x, currentY, size.x, size.y);

				// Move below any overlapping node until clear
				Rect? overlap;
				while ((overlap = FindOverlap(nodeRect, outputNode.Id, allNodes, positioned, nodeSizes)) != null)
				{
					currentY = overlap.Value.yMax + VerticalMargin;
					nodeRect.y = currentY;
				}

				outputNode.Position = new Vector2(x, currentY);
				positioned.Add(outputNode.Id);
				newlyPositioned.Add(outputNode);

				Debug.Log($"[AutoLayout]   Output '{outputNode.DisplayName}' at ({x}, {currentY})");
				currentY += size.y + VerticalMargin;
			}

			return newlyPositioned;
		}

		#endregion

		#region Overlap Detection

		private static Rect? FindOverlap(
			Rect rect,
			string excludeNodeId,
			List<GraphNode> allNodes,
			HashSet<string> positioned,
			Dictionary<string, Vector2> nodeSizes)
		{
			foreach (var node in allNodes)
			{
				if (node.Id == excludeNodeId) continue;
				if (!positioned.Contains(node.Id)) continue;

				var size = GetNodeSize(node, nodeSizes);
				var nodeRect = new Rect(
					node.Position.x - VerticalMargin * 0.5f,
					node.Position.y - VerticalMargin * 0.5f,
					size.x + VerticalMargin,
					size.y + VerticalMargin);

				if (rect.Overlaps(nodeRect)) return nodeRect;
			}

			return null;
		}

		#endregion

		#region Size Lookup

		private static Vector2 GetNodeSize(GraphNode node, Dictionary<string, Vector2> nodeSizes)
		{
			if (nodeSizes != null && nodeSizes.TryGetValue(node.Id, out var actualSize))
			{
				return actualSize;
			}

			return NodeSizeEstimator.EstimateSize(node);
		}

		#endregion

		#region Distance Calculation

		private static Dictionary<string, int> CalculateDistancesFromEntry(
			StateMachineGraph graph,
			List<GraphNode> nodes)
		{
			var distances = new Dictionary<string, int>();
			var entryNodeId = graph.EntryNodeId;

			if (string.IsNullOrEmpty(entryNodeId)) return distances;

			var maxPossibleDistance = nodes.Count;
			var queue = new Queue<string>();
			queue.Enqueue(entryNodeId);
			distances[entryNodeId] = 0;

			while (queue.Count > 0)
			{
				var currentId = queue.Dequeue();
				var currentDistance = distances[currentId];

				foreach (var connection in graph.GetConnectionsFromNode(currentId))
				{
					var newDistance = currentDistance + 1;

					if (newDistance > maxPossibleDistance) continue;

					if (distances.TryGetValue(connection.TargetNodeId, out var existing) && existing >= newDistance)
						continue;

					distances[connection.TargetNodeId] = newDistance;
					queue.Enqueue(connection.TargetNodeId);
				}
			}

			return distances;
		}

		#endregion

		#region Utilities

		private static List<string> GetPortOrder(System.Type stateType)
		{
			if (PortOrderCache.TryGetValue(stateType, out var cached)) return cached;

			var portNames = new List<string>();
			var fields = stateType.GetFields(
				System.Reflection.BindingFlags.Public |
				System.Reflection.BindingFlags.NonPublic |
				System.Reflection.BindingFlags.Instance);

			foreach (var field in fields)
			{
				var transitionAttr = field.GetCustomAttributes(typeof(Nonatomic.VSM.Attributes.TransitionAttribute), true)
					.FirstOrDefault() as Nonatomic.VSM.Attributes.TransitionAttribute;

				if (transitionAttr != null)
				{
					portNames.Add(string.IsNullOrEmpty(transitionAttr.DisplayName) ? field.Name : transitionAttr.DisplayName);
				}

				var dataOutAttr = field.GetCustomAttributes(typeof(Nonatomic.VSM.Attributes.DataOutAttribute), true)
					.FirstOrDefault() as Nonatomic.VSM.Attributes.DataOutAttribute;

				if (dataOutAttr != null)
				{
					portNames.Add(string.IsNullOrEmpty(dataOutAttr.DisplayName) ? field.Name : dataOutAttr.DisplayName);
				}
			}

			PortOrderCache[stateType] = portNames;
			return portNames;
		}

		private static List<string> GetInputPortOrder(System.Type stateType)
		{
			if (InputPortOrderCache.TryGetValue(stateType, out var cached)) return cached;

			var portNames = new List<string> { "Enter" };
			var fields = stateType.GetFields(
				System.Reflection.BindingFlags.Public |
				System.Reflection.BindingFlags.NonPublic |
				System.Reflection.BindingFlags.Instance);

			foreach (var field in fields)
			{
				var dataInAttr = field.GetCustomAttributes(typeof(Nonatomic.VSM.Attributes.DataInAttribute), true)
					.FirstOrDefault() as Nonatomic.VSM.Attributes.DataInAttribute;

				if (dataInAttr != null)
				{
					portNames.Add(string.IsNullOrEmpty(dataInAttr.DisplayName) ? field.Name : dataInAttr.DisplayName);
				}
			}

			InputPortOrderCache[stateType] = portNames;
			return portNames;
		}

		private static int GetOutputPortOrderIndex(GraphNode node, string portName)
		{
			if (node is StateNode stateNode && stateNode.StateType != null)
			{
				var portOrder = GetPortOrder(stateNode.StateType);
				var index = portOrder.IndexOf(portName);
				return index >= 0 ? index : int.MaxValue;
			}

			return 0;
		}

		private static int GetInputPortOrderIndex(GraphNode node, string portName)
		{
			if (portName == "Enter") return 0;

			if (node is StateNode stateNode && stateNode.StateType != null)
			{
				var portOrder = GetInputPortOrder(stateNode.StateType);
				var index = portOrder.IndexOf(portName);
				return index >= 0 ? index : int.MaxValue;
			}

			if (node is SubStateMachineNode subNode)
			{
				if (portName == "SubGraph") return 1;

				var exposedIndex = 2;
				if (subNode.SubGraph != null)
				{
					foreach (var definition in subNode.SubGraph.BlackboardDefinitions)
					{
						if (definition.ExposureDirection == Nonatomic.VSM.Blackboard.ExposureDirection.Input ||
							definition.ExposureDirection == Nonatomic.VSM.Blackboard.ExposureDirection.InputOutput)
						{
							if (definition.Key == portName) return exposedIndex;
							exposedIndex++;
						}
					}
				}

				return int.MaxValue;
			}

			return 0;
		}

		#endregion
	}
}
