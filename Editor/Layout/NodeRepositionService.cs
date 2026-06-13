using System.Collections.Generic;
using System.Linq;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Graph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Layout
{
	/// <summary>
	/// Service that automatically repositions connected nodes when a node's size changes
	/// (e.g., due to collapse/expand). Uses visitor pattern to traverse the graph and
	/// shift nodes to maintain proper alignment.
	/// </summary>
	public static class NodeRepositionService
	{
		/// <summary>
		/// Data class to track node state during repositioning.
		/// </summary>
		private class RepositionData
		{
			public StateMachineGraph Graph { get; }
			public Dictionary<string, Node> NodeViews { get; }
			public HashSet<string> Visited { get; } = new HashSet<string>();
			public Dictionary<string, List<Connection>> OutgoingCache { get; }
			public Dictionary<string, List<Connection>> IncomingCache { get; }

			public RepositionData(StateMachineGraph graph, Dictionary<string, Node> nodeViews)
			{
				Graph = graph;
				NodeViews = nodeViews;
				OutgoingCache = ConnectionCacheBuilder.Build(graph, isOutgoing: true);
				IncomingCache = ConnectionCacheBuilder.Build(graph, isOutgoing: false);
			}
		}

		/// <summary>
		/// Handles a node's geometry change by repositioning connected nodes.
		/// Call this when a node's size changes (e.g., after collapse/expand).
		/// </summary>
		/// <param name="changedNode">The node that changed size</param>
		/// <param name="previousWidth">The node's previous width</param>
		/// <param name="graph">The state machine graph data</param>
		/// <param name="nodeViews">Dictionary mapping node IDs to their visual Node views</param>
		public static void OnNodeSizeChanged(
			Node changedNode,
			float previousWidth,
			StateMachineGraph graph,
			Dictionary<string, Node> nodeViews)
		{
			if (changedNode == null || graph == null || nodeViews == null) return;

			// Get the node data
			var nodeDataProvider = changedNode as INodeDataProvider;
			if (nodeDataProvider == null) return;

			var nodeId = nodeDataProvider.NodeData.Id;
			var currentWidth = changedNode.resolvedStyle.width;

			// Calculate the delta (how much the width changed)
			var widthDelta = currentWidth - previousWidth;

			// Skip if the change is negligible
			if (Mathf.Abs(widthDelta) < 1f) return;

			var data = new RepositionData(graph, nodeViews);

			// Mark the changed node as visited so we don't move it
			data.Visited.Add(nodeId);

			// Shift nodes to the right (connected via output ports)
			ShiftNodesRight(nodeId, widthDelta, data);

			// Shift nodes to the left (connected via input ports)
			ShiftNodesLeft(nodeId, widthDelta, data);
		}

		/// <summary>
		/// Traverses and shifts all nodes connected via output ports (rightward).
		/// When the source node expands, push connected nodes to the right.
		/// When it collapses, pull them to the left.
		/// </summary>
		private static void ShiftNodesRight(string startNodeId, float delta, RepositionData data)
		{
			// Get nodes directly connected via output ports
			var outgoingConnections = data.OutgoingCache.TryGetValue(startNodeId, out var connections)
				? connections
				: new List<Connection>();

			foreach (var connection in outgoingConnections)
			{
				var targetNodeId = connection.TargetNodeId;
				if (data.Visited.Contains(targetNodeId)) continue;

				ShiftNodeAndDescendantsRight(targetNodeId, delta, data);
			}
		}

		/// <summary>
		/// Recursively shifts a node and all nodes connected to its outputs.
		/// </summary>
		private static void ShiftNodeAndDescendantsRight(string nodeId, float delta, RepositionData data)
		{
			if (data.Visited.Contains(nodeId)) return;
			data.Visited.Add(nodeId);

			// Shift this node
			ShiftNode(nodeId, new Vector2(delta, 0), data);

			// Recursively shift all nodes connected via output ports
			var outgoingConnections = data.OutgoingCache.TryGetValue(nodeId, out var connections)
				? connections
				: new List<Connection>();

			foreach (var connection in outgoingConnections)
			{
				var targetNodeId = connection.TargetNodeId;
				if (!data.Visited.Contains(targetNodeId))
				{
					ShiftNodeAndDescendantsRight(targetNodeId, delta, data);
				}
			}
		}

		/// <summary>
		/// Traverses and shifts all nodes connected via input ports (leftward).
		/// When the target node expands, push connected nodes to the left.
		/// When it collapses, pull them to the right.
		/// </summary>
		private static void ShiftNodesLeft(string startNodeId, float delta, RepositionData data)
		{
			// Get nodes directly connected via input ports
			var incomingConnections = data.IncomingCache.TryGetValue(startNodeId, out var connections)
				? connections
				: new List<Connection>();

			foreach (var connection in incomingConnections)
			{
				var sourceNodeId = connection.SourceNodeId;
				if (data.Visited.Contains(sourceNodeId)) continue;

				ShiftNodeAndAncestorsLeft(sourceNodeId, -delta, data);
			}
		}

		/// <summary>
		/// Recursively shifts a node and all nodes connected to its inputs.
		/// </summary>
		private static void ShiftNodeAndAncestorsLeft(string nodeId, float delta, RepositionData data)
		{
			if (data.Visited.Contains(nodeId)) return;
			data.Visited.Add(nodeId);

			// Shift this node
			ShiftNode(nodeId, new Vector2(delta, 0), data);

			// Recursively shift all nodes connected via input ports
			var incomingConnections = data.IncomingCache.TryGetValue(nodeId, out var connections)
				? connections
				: new List<Connection>();

			foreach (var connection in incomingConnections)
			{
				var sourceNodeId = connection.SourceNodeId;
				if (!data.Visited.Contains(sourceNodeId))
				{
					ShiftNodeAndAncestorsLeft(sourceNodeId, delta, data);
				}
			}
		}

		/// <summary>
		/// Shifts a single node's position by the given delta.
		/// </summary>
		private static void ShiftNode(string nodeId, Vector2 delta, RepositionData data)
		{
			// Update the visual node view position
			if (data.NodeViews.TryGetValue(nodeId, out var nodeView))
			{
				var currentRect = nodeView.GetPosition();
				nodeView.SetPosition(new Rect(
					currentRect.x + delta.x,
					currentRect.y + delta.y,
					currentRect.width,
					currentRect.height));
			}

			// Update the underlying graph data position
			var nodeData = data.Graph.GetNode(nodeId);
			if (nodeData != null)
			{
				nodeData.Position += delta;
			}
		}
	}
}
