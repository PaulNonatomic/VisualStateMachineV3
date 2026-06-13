using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Nonatomic.VSM.Blackboard;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Compares objects by reference identity rather than Equals/GetHashCode.
	/// Necessary because Unity's Object.GetHashCode uses recyclable instance IDs.
	/// </summary>
	internal sealed class ReferenceEqualityComparer : IEqualityComparer<StateMachineGraph>
	{
		public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

		public bool Equals(StateMachineGraph x, StateMachineGraph y) => ReferenceEquals(x, y);
		public int GetHashCode(StateMachineGraph obj) => RuntimeHelpers.GetHashCode(obj);
	}

	/// <summary>
	/// Result of resolving a graph variant's merged state.
	/// </summary>
	public class MergedGraphData
	{
		public string EntryNodeId;
		public Dictionary<string, GraphNode> Nodes = new Dictionary<string, GraphNode>();
		public List<Connection> Connections = new List<Connection>();
		public List<BlackboardVariableDefinition> BlackboardDefinitions = new List<BlackboardVariableDefinition>();
		public HashSet<string> DisabledNodeIds = new HashSet<string>();
		public HashSet<string> OverriddenNodeIds = new HashSet<string>();
		public HashSet<string> AddedNodeIds = new HashSet<string>();
	}

	/// <summary>
	/// Static utility that resolves a graph variant into its merged state
	/// by walking the variant chain and applying overrides.
	/// </summary>
	public static class VariantMergeService
	{
		public const int MaxVariantDepth = 8;

		/// <summary>
		/// Resolves a variant into its fully merged graph data.
		/// Walks the variant chain to the root base graph, then applies overrides in order.
		/// </summary>
		public static MergedGraphData Resolve(StateMachineGraphVariant variant)
		{
			if (variant == null) throw new ArgumentNullException(nameof(variant));
			if (variant.BaseGraph == null) return CreateEmptyResult();

			var chain = BuildVariantChain(variant);
			if (chain == null) return CreateEmptyResult();

			var rootBase = GetRootBaseGraph(chain);
			if (rootBase == null) return CreateEmptyResult();

			var result = CreateFromBaseGraph(rootBase);

			// Apply each variant's overrides in order (from root-most to leaf)
			for (var i = chain.Count - 1; i >= 0; i--)
			{
				ApplyVariantOverrides(chain[i], result);
			}

			return result;
		}

		/// <summary>
		/// Validates the variant chain for issues.
		/// </summary>
		public static List<GraphValidationIssue> ValidateVariantChain(StateMachineGraphVariant variant)
		{
			var issues = new List<GraphValidationIssue>();

			if (variant.BaseGraph == null)
			{
				issues.Add(new GraphValidationIssue(
					GraphValidationSeverity.Error,
					"Variant has no base graph assigned.",
					null));
				return issues;
			}

			var chain = WalkVariantChain(variant, out var chainError);
			if (chain == null)
			{
				issues.Add(new GraphValidationIssue(
					GraphValidationSeverity.Error,
					chainError,
					null));
				return issues;
			}

			// Validate context type compatibility
			if (variant.BaseGraph.ContextType != null && variant.ContextType != null &&
				variant.ContextType != variant.BaseGraph.ContextType)
			{
				issues.Add(new GraphValidationIssue(
					GraphValidationSeverity.Warning,
					$"Variant context type '{variant.ContextType.Name}' differs from base '{variant.BaseGraph.ContextType.Name}'.",
					null));
			}

			// Check for orphaned overrides
			ValidateOrphanedOverrides(variant, issues);

			return issues;
		}

		/// <summary>
		/// Builds the variant chain from leaf to root.
		/// Returns null if a circular reference or depth limit is detected.
		/// </summary>
		private static List<StateMachineGraphVariant> BuildVariantChain(StateMachineGraphVariant variant)
		{
			var chain = WalkVariantChain(variant, out var errorMessage);
			if (chain == null)
			{
				Debug.LogError(errorMessage);
			}

			return chain;
		}

		/// <summary>
		/// Walks the variant chain from leaf to root, checking for circular references and depth limits.
		/// Returns the chain list, or null with an error message on failure.
		/// </summary>
		private static List<StateMachineGraphVariant> WalkVariantChain(
			StateMachineGraphVariant variant,
			out string errorMessage)
		{
			errorMessage = null;
			var chain = new List<StateMachineGraphVariant>();
			var visited = new HashSet<StateMachineGraph>(ReferenceEqualityComparer.Instance);
			StateMachineGraph current = variant;

			while (current is StateMachineGraphVariant currentVariant)
			{
				if (!visited.Add(current))
				{
					errorMessage = "Circular variant reference detected.";
					return null;
				}

				if (chain.Count >= MaxVariantDepth)
				{
					errorMessage = $"Variant chain exceeds maximum depth of {MaxVariantDepth}.";
					return null;
				}

				chain.Add(currentVariant);
				current = currentVariant.BaseGraph;
			}

			return chain;
		}

		/// <summary>
		/// Gets the root non-variant base graph from a variant chain.
		/// </summary>
		private static StateMachineGraph GetRootBaseGraph(List<StateMachineGraphVariant> chain)
		{
			if (chain.Count == 0) return null;

			var deepest = chain[chain.Count - 1];
			return deepest.BaseGraph;
		}

		/// <summary>
		/// Creates a MergedGraphData from a base (non-variant) graph by deep-cloning all elements.
		/// </summary>
		private static MergedGraphData CreateFromBaseGraph(StateMachineGraph baseGraph)
		{
			var result = new MergedGraphData
			{
				EntryNodeId = baseGraph.EntryNodeId
			};

			// Deep-clone nodes via JSON round-trip
			foreach (var node in baseGraph.GetAllNodes())
			{
				var cloned = CloneNode(node);
				if (cloned != null)
				{
					result.Nodes[cloned.Id] = cloned;
				}
			}

			// Deep-clone connections
			foreach (var connection in baseGraph.Connections)
			{
				var cloned = CloneConnection(connection);
				if (cloned != null)
				{
					result.Connections.Add(cloned);
				}
			}

			// Deep-clone blackboard definitions
			foreach (var definition in baseGraph.BlackboardDefinitions)
			{
				var cloned = CloneBlackboardDefinition(definition);
				if (cloned != null)
				{
					result.BlackboardDefinitions.Add(cloned);
				}
			}

			return result;
		}

		/// <summary>
		/// Applies a single variant's overrides to the merged result.
		/// </summary>
		private static void ApplyVariantOverrides(StateMachineGraphVariant variant, MergedGraphData result)
		{
			// Apply disabled nodes
			foreach (var nodeId in variant.DisabledNodeIds)
			{
				result.DisabledNodeIds.Add(nodeId);
				result.Nodes.Remove(nodeId);

				// Remove connections to/from disabled nodes
				result.Connections.RemoveAll(c =>
					c.SourceNodeId == nodeId || c.TargetNodeId == nodeId);
			}

			// Apply node overrides
			foreach (var nodeOverride in variant.NodeOverrides)
			{
				if (nodeOverride.Action == OverrideAction.Disable)
				{
					result.DisabledNodeIds.Add(nodeOverride.NodeId);
					result.Nodes.Remove(nodeOverride.NodeId);
					result.Connections.RemoveAll(c =>
						c.SourceNodeId == nodeOverride.NodeId || c.TargetNodeId == nodeOverride.NodeId);
					continue;
				}

				if (!result.Nodes.TryGetValue(nodeOverride.NodeId, out var node)) continue;

				result.OverriddenNodeIds.Add(nodeOverride.NodeId);

				if (nodeOverride.Action == OverrideAction.Replace && node is StateNode stateNode)
				{
					if (!string.IsNullOrEmpty(nodeOverride.ReplacementStateTypeAQN))
					{
						stateNode.StateTypeAssemblyQualifiedName = nodeOverride.ReplacementStateTypeAQN;
						stateNode.RestoreType();
					}
				}

				// Apply property overrides to StateNode
				if (node is StateNode targetStateNode && nodeOverride.PropertyOverrides.Count > 0)
				{
					foreach (var propOverride in nodeOverride.PropertyOverrides)
					{
						var value = propOverride.GetValue();
						targetStateNode.SetPropertyOverride(
							propOverride.FieldName,
							value,
							value?.GetType());
					}
				}

				// Apply position override
				if (nodeOverride.HasPositionOverride)
				{
					node.Position = nodeOverride.PositionOverride;
				}

				// Apply display name override
				if (nodeOverride.HasDisplayNameOverride)
				{
					node.DisplayName = nodeOverride.DisplayNameOverride;
				}

				// Apply comment override
				if (nodeOverride.HasCommentOverride)
				{
					node.Comment = nodeOverride.CommentOverride;
				}
			}

			// Remove connections
			foreach (var connectionId in variant.RemovedConnectionIds)
			{
				result.Connections.RemoveAll(c => c.Id == connectionId);
			}

			// Apply connection overrides
			foreach (var connOverride in variant.ConnectionOverrides)
			{
				var connection = result.Connections.Find(c => c.Id == connOverride.ConnectionId);
				if (connection == null) continue;

				if (connOverride.HasTransitionTimeOverride)
				{
					connection.TransitionTime = connOverride.TransitionTimeOverride;
				}

				if (connOverride.HasEasingCurveOverride)
				{
					connection.EasingCurve = connOverride.EasingCurveOverride;
				}
			}

			// Add variant-only nodes
			foreach (var addedNode in variant.AddedSerializedNodes)
			{
				var node = addedNode.ToNode();
				if (node != null)
				{
					result.Nodes[node.Id] = node;
					result.AddedNodeIds.Add(node.Id);
				}
			}

			// Add variant-only connections
			foreach (var addedConnection in variant.AddedConnections)
			{
				var cloned = CloneConnection(addedConnection);
				if (cloned != null)
				{
					result.Connections.Add(cloned);
				}
			}

			// Apply blackboard variable overrides
			foreach (var bbOverride in variant.BlackboardOverrides)
			{
				var definition = result.BlackboardDefinitions.Find(d => d.Key == bbOverride.Key);
				if (definition == null) continue;

				if (bbOverride.HasDefaultValueOverride)
				{
					definition.DefaultValueJson = bbOverride.DefaultValueJsonOverride;
				}

				if (bbOverride.HasIsExposedOverride)
				{
					// Write both exposure flags. The runner inspector filters on IsExposedInGraph,
					// so writing only the legacy IsExposed (as before) left variant "expose" overrides
					// invisible in the inspector. Keeping the pair in sync fixes that regardless of
					// which flag any reader consults.
					definition.IsExposed = bbOverride.IsExposedOverride;
					definition.IsExposedInGraph = bbOverride.IsExposedOverride;
				}
			}

			// Add variant-only blackboard variables
			foreach (var addedVar in variant.AddedBlackboardVariables)
			{
				var cloned = CloneBlackboardDefinition(addedVar);
				if (cloned != null)
				{
					result.BlackboardDefinitions.Add(cloned);
				}
			}
		}

		private static void ValidateOrphanedOverrides(
			StateMachineGraphVariant variant,
			List<GraphValidationIssue> issues)
		{
			var baseGraph = variant.BaseGraph;
			if (baseGraph == null) return;

			foreach (var nodeOverride in variant.NodeOverrides)
			{
				if (!baseGraph.HasNode(nodeOverride.NodeId))
				{
					issues.Add(new GraphValidationIssue(
						GraphValidationSeverity.Warning,
						$"Node override references node '{nodeOverride.NodeId}' which no longer exists in the base graph.",
						nodeOverride.NodeId));
				}
			}

			foreach (var nodeId in variant.DisabledNodeIds)
			{
				if (!baseGraph.HasNode(nodeId))
				{
					issues.Add(new GraphValidationIssue(
						GraphValidationSeverity.Warning,
						$"Disabled node '{nodeId}' no longer exists in the base graph.",
						nodeId));
				}
			}
		}

		/// <summary>
		/// Deep-clones a node via SerializedNode JSON round-trip.
		/// </summary>
		private static GraphNode CloneNode(GraphNode node)
		{
			var serialized = SerializedNode.FromNode(node);
			return serialized?.ToNode();
		}

		/// <summary>
		/// Deep-clones a connection via JSON round-trip.
		/// </summary>
		private static Connection CloneConnection(Connection connection)
		{
			var json = JsonUtility.ToJson(connection);
			return JsonUtility.FromJson<Connection>(json);
		}

		/// <summary>
		/// Deep-clones a blackboard variable definition via JSON round-trip.
		/// Avoids mutating the input by serializing as-is (type name is already cached
		/// during OnBeforeSerialize) and restoring the type on the clone.
		/// </summary>
		private static BlackboardVariableDefinition CloneBlackboardDefinition(BlackboardVariableDefinition definition)
		{
			var json = JsonUtility.ToJson(definition);
			var cloned = JsonUtility.FromJson<BlackboardVariableDefinition>(json);
			cloned.RestoreType();
			return cloned;
		}

		private static MergedGraphData CreateEmptyResult()
		{
			return new MergedGraphData();
		}
	}
}
