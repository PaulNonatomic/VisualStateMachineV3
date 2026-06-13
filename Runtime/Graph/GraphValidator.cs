using System.Collections.Generic;
using System.Reflection;
using Nonatomic.VSM.Attributes;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Validates a StateMachineGraph and returns any structural issues found.
	/// </summary>
	public static class GraphValidator
	{
		/// <summary>
		/// Validates the graph and returns any issues found.
		/// </summary>
		public static List<GraphValidationIssue> Validate(StateMachineGraph graph)
		{
			var issues = new List<GraphValidationIssue>();
			var portalInByChannel = BuildPortalCache(graph);

			// Check for entry node
			if (string.IsNullOrEmpty(graph.EntryNodeId) || graph.GetNode(graph.EntryNodeId) == null)
			{
				issues.Add(new GraphValidationIssue(
					GraphValidationSeverity.Error,
					"Graph has no entry node",
					null
				));
			}

			// Check entry node has outgoing connection
			if (!string.IsNullOrEmpty(graph.EntryNodeId))
			{
				var hasOutgoing = graph.GetConnectionsFromNode(graph.EntryNodeId).Count > 0;

				if (!hasOutgoing)
				{
					issues.Add(new GraphValidationIssue(
						GraphValidationSeverity.Warning,
						"Entry node has no outgoing transition",
						graph.EntryNodeId
					));
				}
			}

			// Check for unreachable states
			var reachable = new HashSet<string>();
			if (!string.IsNullOrEmpty(graph.EntryNodeId))
			{
				FindReachableNodes(graph, graph.EntryNodeId, reachable, portalInByChannel);
			}

			foreach (var node in graph.GetAllNodes())
			{
				if (node.NodeType == GraphNodeType.State && !reachable.Contains(node.Id))
				{
					issues.Add(new GraphValidationIssue(
						GraphValidationSeverity.Warning,
						$"State '{node.DisplayName}' is unreachable",
						node.Id
					));
				}
			}

			// Check for missing state types
			foreach (var node in graph.GetNodesOfType<StateNode>())
			{
				node.RestoreType();
				if (node.StateType == null)
				{
					issues.Add(new GraphValidationIssue(
						GraphValidationSeverity.Error,
						$"State node '{node.DisplayName}' has missing type: {node.StateTypeAssemblyQualifiedName}",
						node.Id
					));
				}
			}

			ValidateRequiredInputs(graph, issues);
			ValidatePortalNodes(graph, issues);
			ValidateSubStateMachineNodes(graph, issues);

			if (graph is StateMachineGraphVariant variant)
			{
				ValidateVariant(variant, issues);
			}

			return issues;
		}

		/// <summary>
		/// Flags state inputs marked [DataIn(Required = true)] that have no value source — i.e.
		/// no incoming data/blackboard connection and no serialized property override. Emitted as a
		/// warning (a field initializer can still provide a fallback at runtime).
		/// </summary>
		private static void ValidateRequiredInputs(StateMachineGraph graph, List<GraphValidationIssue> issues)
		{
			foreach (var node in graph.GetNodesOfType<StateNode>())
			{
				var stateType = node.StateType;
				if (stateType == null) continue;

				foreach (var field in stateType.GetFields(BindingFlags.Public | BindingFlags.Instance))
				{
					var dataIn = field.GetCustomAttribute<DataInAttribute>();
					if (dataIn == null || !dataIn.Required) continue;
					if (IsInputSatisfied(graph, node, field.Name)) continue;

					var portName = string.IsNullOrEmpty(dataIn.DisplayName) ? field.Name : dataIn.DisplayName;
					issues.Add(new GraphValidationIssue(
						GraphValidationSeverity.Warning,
						$"State '{node.DisplayName}' has a required input '{portName}' with no connection or override",
						node.Id
					));
				}
			}
		}

		/// <summary>
		/// Returns true if the named input port has a value source: a serialized property override,
		/// or an incoming Data/BlackboardRead connection.
		/// </summary>
		private static bool IsInputSatisfied(StateMachineGraph graph, StateNode node, string fieldName)
		{
			if (node.GetPropertyOverride(fieldName) != null)
			{
				return true;
			}

			var incoming = graph.GetConnectionsToNode(node.Id);
			for (var i = 0; i < incoming.Count; i++)
			{
				var conn = incoming[i];
				if (conn.TargetPortName != fieldName) continue;
				if (conn.ConnectionType == ConnectionType.Data || conn.ConnectionType == ConnectionType.BlackboardRead)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Builds the portal-in-by-channel cache for O(1) lookups.
		/// </summary>
		private static Dictionary<int, PortalInNode> BuildPortalCache(StateMachineGraph graph)
		{
			var portalInByChannel = new Dictionary<int, PortalInNode>();

			foreach (var node in graph.GetNodesOfType<PortalInNode>())
			{
				if (!portalInByChannel.ContainsKey(node.ChannelId))
				{
					portalInByChannel[node.ChannelId] = node;
				}
			}

			return portalInByChannel;
		}

		/// <summary>
		/// Validates portal node pairing and configuration.
		/// </summary>
		private static void ValidatePortalNodes(StateMachineGraph graph, List<GraphValidationIssue> issues)
		{
			var portalOutsByChannel = new Dictionary<int, List<PortalOutNode>>();
			var portalInsByChannel = new Dictionary<int, List<PortalInNode>>();

			foreach (var node in graph.GetNodesOfType<PortalOutNode>())
			{
				if (!portalOutsByChannel.ContainsKey(node.ChannelId))
					portalOutsByChannel[node.ChannelId] = new List<PortalOutNode>();
				portalOutsByChannel[node.ChannelId].Add(node);
			}

			foreach (var node in graph.GetNodesOfType<PortalInNode>())
			{
				if (!portalInsByChannel.ContainsKey(node.ChannelId))
					portalInsByChannel[node.ChannelId] = new List<PortalInNode>();
				portalInsByChannel[node.ChannelId].Add(node);
			}

			// Check for orphaned PortalOuts (no matching PortalIn)
			foreach (var kvp in portalOutsByChannel)
			{
				if (!portalInsByChannel.ContainsKey(kvp.Key) || portalInsByChannel[kvp.Key].Count == 0)
				{
					foreach (var portalOut in kvp.Value)
					{
						issues.Add(new GraphValidationIssue(
							GraphValidationSeverity.Warning,
							$"PortalOut node (channel {kvp.Key}) has no matching PortalIn node",
							portalOut.Id
						));
					}
				}
			}

			// Check for duplicate PortalIns on same channel
			foreach (var kvp in portalInsByChannel)
			{
				if (kvp.Value.Count > 1)
				{
					foreach (var portalIn in kvp.Value)
					{
						issues.Add(new GraphValidationIssue(
							GraphValidationSeverity.Error,
							$"Multiple PortalIn nodes share channel {kvp.Key}. Only one PortalIn per channel is allowed.",
							portalIn.Id
						));
					}
				}
			}

			// Check for orphaned PortalIns (no PortalOuts point to them)
			foreach (var kvp in portalInsByChannel)
			{
				if (!portalOutsByChannel.ContainsKey(kvp.Key) || portalOutsByChannel[kvp.Key].Count == 0)
				{
					foreach (var portalIn in kvp.Value)
					{
						issues.Add(new GraphValidationIssue(
							GraphValidationSeverity.Info,
							$"PortalIn node (channel {kvp.Key}) has no PortalOut nodes targeting it",
							portalIn.Id
						));
					}
				}
			}

			// Check PortalIn nodes have outgoing connections
			foreach (var kvp in portalInsByChannel)
			{
				foreach (var portalIn in kvp.Value)
				{
					var portalConns = graph.GetConnectionsFromNode(portalIn.Id);
					var hasOutgoing = false;
					for (var i = 0; i < portalConns.Count; i++)
					{
						if (portalConns[i].ConnectionType == ConnectionType.Transition)
						{
							hasOutgoing = true;
							break;
						}
					}
					if (!hasOutgoing)
					{
						issues.Add(new GraphValidationIssue(
							GraphValidationSeverity.Warning,
							$"PortalIn node (channel {kvp.Key}) has no outgoing connection",
							portalIn.Id
						));
					}
				}
			}
		}

		/// <summary>
		/// Validates sub-state machine nodes for context compatibility and configuration.
		/// </summary>
		private static void ValidateSubStateMachineNodes(StateMachineGraph graph, List<GraphValidationIssue> issues)
		{
			var parentContextType = graph.ContextType;

			foreach (var node in graph.GetNodesOfType<SubStateMachineNode>())
			{
				if (node.SubGraph == null)
				{
					issues.Add(new GraphValidationIssue(
						GraphValidationSeverity.Warning,
						$"SubStateMachine node '{node.DisplayName}' has no sub-graph assigned",
						node.Id
					));
					continue;
				}

				var subGraph = node.SubGraph;
				var subContextType = subGraph.ContextType;

				if (subContextType != null)
				{
					if (parentContextType == null)
					{
						issues.Add(new GraphValidationIssue(
							GraphValidationSeverity.Error,
							$"SubStateMachine '{node.DisplayName}' requires context type '{subContextType.Name}' " +
							$"but parent graph has no context type defined",
							node.Id
						));
					}
					else if (!subContextType.IsAssignableFrom(parentContextType))
					{
						issues.Add(new GraphValidationIssue(
							GraphValidationSeverity.Error,
							$"SubStateMachine '{node.DisplayName}' requires context type '{subContextType.Name}' " +
							$"which is not compatible with parent context type '{parentContextType.Name}'",
							node.Id
						));
					}
				}

				if (string.IsNullOrEmpty(subGraph.EntryNodeId))
				{
					issues.Add(new GraphValidationIssue(
						GraphValidationSeverity.Error,
						$"SubStateMachine '{node.DisplayName}' sub-graph has no entry node",
						node.Id
					));
				}

				var subConns = graph.GetConnectionsFromNode(node.Id);
				var hasOutgoing = false;
				for (var j = 0; j < subConns.Count; j++)
				{
					if (subConns[j].ConnectionType == ConnectionType.Transition)
					{
						hasOutgoing = true;
						break;
					}
				}
				if (!hasOutgoing)
				{
					issues.Add(new GraphValidationIssue(
						GraphValidationSeverity.Warning,
						$"SubStateMachine node '{node.DisplayName}' has no exit connection - " +
						$"parent will stop when sub-graph completes",
						node.Id
					));
				}

				if (HasCircularSubGraphReference(subGraph, new HashSet<StateMachineGraph> { graph }))
				{
					issues.Add(new GraphValidationIssue(
						GraphValidationSeverity.Error,
						$"SubStateMachine '{node.DisplayName}' creates a circular reference - " +
						$"this graph appears in the sub-graph chain",
						node.Id
					));
				}
			}
		}

		/// <summary>
		/// Recursively checks if any sub-graph in the chain references a graph in the visited set.
		/// </summary>
		internal static bool HasCircularSubGraphReference(StateMachineGraph graph, HashSet<StateMachineGraph> visited)
		{
			if (graph == null) return false;
			if (visited.Contains(graph)) return true;

			visited.Add(graph);

			foreach (var node in graph.GetNodesOfType<SubStateMachineNode>())
			{
				if (node.SubGraph != null && HasCircularSubGraphReference(node.SubGraph, visited))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Validates variant-specific rules.
		/// </summary>
		private static void ValidateVariant(StateMachineGraphVariant variant, List<GraphValidationIssue> issues)
		{
			var variantIssues = VariantMergeService.ValidateVariantChain(variant);
			issues.AddRange(variantIssues);
		}

		private static void FindReachableNodes(
			StateMachineGraph graph,
			string nodeId,
			HashSet<string> visited,
			Dictionary<int, PortalInNode> portalInByChannel)
		{
			if (visited.Contains(nodeId))
			{
				return;
			}

			visited.Add(nodeId);

			var node = graph.GetNode(nodeId);

			// Handle PortalOut -> PortalIn teleportation
			if (node is PortalOutNode portalOut)
			{
				if (portalInByChannel != null && portalInByChannel.TryGetValue(portalOut.ChannelId, out var portalIn))
				{
					FindReachableNodes(graph, portalIn.Id, visited, portalInByChannel);
				}
				return;
			}

			var reachConns = graph.GetConnectionsFromNode(nodeId);
			for (var i = 0; i < reachConns.Count; i++)
			{
				if (reachConns[i].ConnectionType == ConnectionType.Transition)
				{
					FindReachableNodes(graph, reachConns[i].TargetNodeId, visited, portalInByChannel);
				}
			}
		}
	}
}
