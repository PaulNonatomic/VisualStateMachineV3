using System;
using System.Collections.Generic;
using Nonatomic.VSM.Blackboard;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// A variant of a StateMachineGraph that inherits from a base graph
	/// and stores only its differences (overrides).
	/// Changes to the base graph automatically propagate to variants.
	/// </summary>
	public class StateMachineGraphVariant : StateMachineGraph
	{
		[SerializeField] private StateMachineGraph _baseGraph;
		[SerializeField] private List<NodeOverride> _nodeOverrides = new List<NodeOverride>();
		[SerializeField] private List<string> _disabledNodeIds = new List<string>();
		[SerializeField] private List<SerializedNode> _addedNodes = new List<SerializedNode>();
		[SerializeField] private List<string> _removedConnectionIds = new List<string>();
		[SerializeField] private List<Connection> _addedConnections = new List<Connection>();
		[SerializeField] private List<ConnectionOverride> _connectionOverrides = new List<ConnectionOverride>();
		[SerializeField] private List<BlackboardVariableOverride> _blackboardOverrides = new List<BlackboardVariableOverride>();
		[SerializeField] private List<BlackboardVariableDefinition> _addedBlackboardVariables = new List<BlackboardVariableDefinition>();
		[SerializeField] private GraphViewData _variantGraphViewData = new GraphViewData();

		[NonSerialized] private MergedGraphData _mergedCache;
		[NonSerialized] private bool _isMergedCacheDirty = true;
		[NonSerialized] private Dictionary<string, List<Connection>> _mergedConnectionsBySource;
		[NonSerialized] private Dictionary<string, List<Connection>> _mergedConnectionsByTarget;
		[NonSerialized] private Dictionary<string, Connection> _mergedConnectionsById;

		/// <summary>
		/// The base graph this variant inherits from.
		/// </summary>
		public StateMachineGraph BaseGraph => _baseGraph;

		public override bool IsVariant => true;

		/// <summary>
		/// How deep this variant is in the inheritance chain.
		/// </summary>
		public int VariantDepth
		{
			get
			{
				var depth = 0;
				StateMachineGraph current = _baseGraph;
				while (current is StateMachineGraphVariant v)
				{
					depth++;
					current = v._baseGraph;
					if (depth > VariantMergeService.MaxVariantDepth) break;
				}
				return depth;
			}
		}

		// Public accessors for variant delta data (used by VariantMergeService)
		public IReadOnlyList<NodeOverride> NodeOverrides => _nodeOverrides;
		public IReadOnlyList<string> DisabledNodeIds => _disabledNodeIds;
		public IReadOnlyList<SerializedNode> AddedSerializedNodes => _addedNodes;
		public IReadOnlyList<string> RemovedConnectionIds => _removedConnectionIds;
		public IReadOnlyList<Connection> AddedConnections => _addedConnections;
		public IReadOnlyList<ConnectionOverride> ConnectionOverrides => _connectionOverrides;
		public IReadOnlyList<BlackboardVariableOverride> BlackboardOverrides => _blackboardOverrides;
		public IReadOnlyList<BlackboardVariableDefinition> AddedBlackboardVariables => _addedBlackboardVariables;

		/// <summary>
		/// Graph view data specific to this variant's editor layout.
		/// </summary>
		public GraphViewData VariantGraphViewData => _variantGraphViewData;

		private void EnsureMergedCacheValid()
		{
			if (!_isMergedCacheDirty && _mergedCache != null) return;
			RebuildMergedCache();
		}

		private void RebuildMergedCache()
		{
			_mergedCache = VariantMergeService.Resolve(this);
			RebuildMergedConnectionIndices();
			_isMergedCacheDirty = false;
		}

		private void RebuildMergedConnectionIndices()
		{
			_mergedConnectionsBySource = new Dictionary<string, List<Connection>>();
			_mergedConnectionsByTarget = new Dictionary<string, List<Connection>>();
			_mergedConnectionsById = new Dictionary<string, Connection>();

			if (_mergedCache == null) return;

			foreach (var connection in _mergedCache.Connections)
			{
				if (!string.IsNullOrEmpty(connection.Id))
				{
					_mergedConnectionsById[connection.Id] = connection;
				}

				if (!string.IsNullOrEmpty(connection.SourceNodeId))
				{
					if (!_mergedConnectionsBySource.TryGetValue(connection.SourceNodeId, out var sourceList))
					{
						sourceList = new List<Connection>();
						_mergedConnectionsBySource[connection.SourceNodeId] = sourceList;
					}
					sourceList.Add(connection);
				}

				if (!string.IsNullOrEmpty(connection.TargetNodeId))
				{
					if (!_mergedConnectionsByTarget.TryGetValue(connection.TargetNodeId, out var targetList))
					{
						targetList = new List<Connection>();
						_mergedConnectionsByTarget[connection.TargetNodeId] = targetList;
					}
					targetList.Add(connection);
				}
			}
		}

		/// <summary>
		/// Invalidates the merged cache, forcing a rebuild on next access.
		/// </summary>
		public void InvalidateMergedCache()
		{
			_isMergedCacheDirty = true;
			_mergedCache = null;
		}

		public override string EntryNodeId
		{
			get
			{
				EnsureMergedCacheValid();
				return _mergedCache?.EntryNodeId;
			}
			set
			{
				Debug.LogWarning($"[{name}] Cannot set EntryNodeId directly on a variant. Entry node is inherited from the base graph.");
			}
		}

		public override IReadOnlyList<Connection> Connections
		{
			get
			{
				EnsureMergedCacheValid();
				return _mergedCache?.Connections ?? (IReadOnlyList<Connection>)Array.Empty<Connection>();
			}
		}

		public override IReadOnlyList<BlackboardVariableDefinition> BlackboardDefinitions
		{
			get
			{
				EnsureMergedCacheValid();
				return _mergedCache?.BlackboardDefinitions ?? (IReadOnlyList<BlackboardVariableDefinition>)Array.Empty<BlackboardVariableDefinition>();
			}
		}

		public override int NodeCount
		{
			get
			{
				EnsureMergedCacheValid();
				return _mergedCache?.Nodes.Count ?? 0;
			}
		}

		public override int ConnectionCount
		{
			get
			{
				EnsureMergedCacheValid();
				return _mergedCache?.Connections.Count ?? 0;
			}
		}

		public override GraphNode GetNode(string id)
		{
			EnsureMergedCacheValid();
			if (_mergedCache?.Nodes == null) return null;
			_mergedCache.Nodes.TryGetValue(id, out var node);
			return node;
		}

		public override IEnumerable<GraphNode> GetAllNodes()
		{
			EnsureMergedCacheValid();
			if (_mergedCache?.Nodes == null) return Array.Empty<GraphNode>();
			return _mergedCache.Nodes.Values;
		}

		public override IEnumerable<T> GetNodesOfType<T>()
		{
			EnsureMergedCacheValid();
			if (_mergedCache?.Nodes == null) yield break;

			foreach (var node in _mergedCache.Nodes.Values)
			{
				if (node is T typed)
				{
					yield return typed;
				}
			}
		}

		public override bool HasNode(string id)
		{
			EnsureMergedCacheValid();
			return _mergedCache?.Nodes != null && _mergedCache.Nodes.ContainsKey(id);
		}

		public override EntryNode GetEntryNode()
		{
			var entryId = EntryNodeId;
			if (string.IsNullOrEmpty(entryId)) return null;
			return GetNode(entryId) as EntryNode;
		}

		public override IReadOnlyList<Connection> GetConnectionsFromNode(string nodeId)
		{
			EnsureMergedCacheValid();
			if (_mergedConnectionsBySource != null && _mergedConnectionsBySource.TryGetValue(nodeId, out var connections))
			{
				return connections;
			}
			return Array.Empty<Connection>();
		}

		public override Connection GetFirstConnectionFromNode(string nodeId)
		{
			EnsureMergedCacheValid();
			if (_mergedConnectionsBySource != null &&
				_mergedConnectionsBySource.TryGetValue(nodeId, out var connections) &&
				connections.Count > 0)
			{
				return connections[0];
			}
			return null;
		}

		public override IReadOnlyList<Connection> GetConnectionsToNode(string nodeId)
		{
			EnsureMergedCacheValid();
			if (_mergedConnectionsByTarget != null && _mergedConnectionsByTarget.TryGetValue(nodeId, out var connections))
			{
				return connections;
			}
			return Array.Empty<Connection>();
		}

		public override Connection GetConnectionToPort(string nodeId, string portName)
		{
			EnsureMergedCacheValid();
			if (_mergedConnectionsByTarget != null && _mergedConnectionsByTarget.TryGetValue(nodeId, out var connections))
			{
				foreach (var connection in connections)
				{
					if (connection.TargetPortName == portName)
					{
						return connection;
					}
				}
			}
			return null;
		}

		public override IEnumerable<Connection> GetConnectionsFromPort(string nodeId, string portName)
		{
			EnsureMergedCacheValid();
			if (_mergedConnectionsBySource != null && _mergedConnectionsBySource.TryGetValue(nodeId, out var connections))
			{
				foreach (var connection in connections)
				{
					if (connection.SourcePortName == portName)
					{
						yield return connection;
					}
				}
			}
		}

		public override bool IsPortConnected(string nodeId, string portName, bool isInput)
		{
			if (isInput)
			{
				return GetConnectionToPort(nodeId, portName) != null;
			}

			foreach (var _ in GetConnectionsFromPort(nodeId, portName))
			{
				return true;
			}

			return false;
		}

		public override void OnBeforeSerialize()
		{
			// Do NOT call base.OnBeforeSerialize() — that would write the merged cache
			// to _serializedNodes. The variant only persists its delta data.
			// The delta fields (_nodeOverrides, _addedNodes, etc.) are [SerializeField]
			// and serialize automatically.

			foreach (var definition in _addedBlackboardVariables)
			{
				definition.CacheTypeName();
			}
		}

		protected override void OnEnable()
		{
			// Variants inherit context type from their base graph
			if (_baseGraph != null)
			{
				ContextType = _baseGraph.ContextType;
			}
		}

		public override void OnAfterDeserialize()
		{
			// Do NOT call base.OnAfterDeserialize() — it would try to populate from
			// _serializedNodes which is empty for variants.
			// Instead, mark cache dirty so it rebuilds lazily from base + deltas.

			_isMergedCacheDirty = true;
			_mergedCache = null;

			foreach (var definition in _addedBlackboardVariables)
			{
				definition.RestoreType();
			}
		}

#if UNITY_EDITOR
		protected override void Reset()
		{
			// Do NOT call base.Reset() — variants don't create their own entry node
		}

		/// <summary>
		/// Sets or updates a property override on an inherited node.
		/// </summary>
		public void SetNodePropertyOverride(string nodeId, string fieldName, object value, Type type)
		{
			var nodeOverride = GetOrCreateNodeOverride(nodeId);
			nodeOverride.SetPropertyOverride(fieldName, value, type);
			InvalidateMergedCache();
		}

		/// <summary>
		/// Removes a property override from an inherited node.
		/// </summary>
		public void RemoveNodePropertyOverride(string nodeId, string fieldName)
		{
			var nodeOverride = FindNodeOverride(nodeId);
			if (nodeOverride == null) return;

			nodeOverride.RemovePropertyOverride(fieldName);

			if (!nodeOverride.HasAnyOverrides)
			{
				_nodeOverrides.Remove(nodeOverride);
			}

			InvalidateMergedCache();
		}

		/// <summary>
		/// Disables an inherited node (excludes it from the merged graph).
		/// </summary>
		public void DisableNode(string nodeId)
		{
			if (!_disabledNodeIds.Contains(nodeId))
			{
				_disabledNodeIds.Add(nodeId);
				InvalidateMergedCache();
			}
		}

		/// <summary>
		/// Re-enables a previously disabled node.
		/// </summary>
		public void EnableNode(string nodeId)
		{
			if (_disabledNodeIds.Remove(nodeId))
			{
				InvalidateMergedCache();
			}
		}

		/// <summary>
		/// Reverts all overrides for a specific node.
		/// </summary>
		public void RevertNodeOverride(string nodeId)
		{
			_nodeOverrides.RemoveAll(o => o.NodeId == nodeId);
			_disabledNodeIds.Remove(nodeId);
			InvalidateMergedCache();
		}

		/// <summary>
		/// Adds a new node to this variant (not present in the base graph).
		/// </summary>
		public void AddVariantNode(GraphNode node)
		{
			if (node == null || string.IsNullOrEmpty(node.Id)) return;

			var serialized = SerializedNode.FromNode(node);
			if (serialized == null) return;

			var existingIndex = _addedNodes.FindIndex(n => n.NodeId == node.Id);
			if (existingIndex >= 0)
			{
				_addedNodes[existingIndex] = serialized;
			}
			else
			{
				_addedNodes.Add(serialized);
			}

			InvalidateMergedCache();
		}

		/// <summary>
		/// Removes a variant-only node.
		/// </summary>
		public void RemoveVariantNode(string nodeId)
		{
			_addedNodes.RemoveAll(n => n.NodeId == nodeId);

			// Also remove any connections to/from this node
			_addedConnections.RemoveAll(c =>
				c.SourceNodeId == nodeId || c.TargetNodeId == nodeId);

			InvalidateMergedCache();
		}

		/// <summary>
		/// Adds a new connection to this variant.
		/// </summary>
		public void AddVariantConnection(Connection connection)
		{
			if (connection == null) return;
			_addedConnections.Add(connection);
			InvalidateMergedCache();
		}

		/// <summary>
		/// Removes an inherited connection.
		/// </summary>
		public void RemoveInheritedConnection(string connectionId)
		{
			if (!_removedConnectionIds.Contains(connectionId))
			{
				_removedConnectionIds.Add(connectionId);
				InvalidateMergedCache();
			}
		}

		/// <summary>
		/// Removes a variant-only connection.
		/// </summary>
		public void RemoveVariantConnection(string connectionId)
		{
			_addedConnections.RemoveAll(c => c.Id == connectionId);
			InvalidateMergedCache();
		}

		/// <summary>
		/// Restores an inherited connection that was previously removed.
		/// </summary>
		public void RestoreInheritedConnection(string connectionId)
		{
			if (_removedConnectionIds.Remove(connectionId))
			{
				InvalidateMergedCache();
			}
		}

		/// <summary>
		/// Sets the base graph for this variant.
		/// </summary>
		public void SetBaseGraph(StateMachineGraph baseGraph)
		{
			_baseGraph = baseGraph;
			InvalidateMergedCache();
		}
#endif

		/// <summary>
		/// Checks if a specific node has been overridden in this variant.
		/// </summary>
		public bool IsNodeOverridden(string nodeId)
		{
			return _nodeOverrides.Exists(o => o.NodeId == nodeId);
		}

		/// <summary>
		/// Checks if a specific node has been disabled in this variant.
		/// </summary>
		public bool IsNodeDisabled(string nodeId)
		{
			return _disabledNodeIds.Contains(nodeId);
		}

		/// <summary>
		/// Checks if a node was added by this variant (not inherited from base).
		/// </summary>
		public bool IsNodeAdded(string nodeId)
		{
			return _addedNodes.Exists(n => n.NodeId == nodeId);
		}

		/// <summary>
		/// Checks if a connection was added by this variant.
		/// </summary>
		public bool IsConnectionAdded(string connectionId)
		{
			return _addedConnections.Exists(c => c.Id == connectionId);
		}

		/// <summary>
		/// Checks if an inherited connection has been removed.
		/// </summary>
		public bool IsConnectionRemoved(string connectionId)
		{
			return _removedConnectionIds.Contains(connectionId);
		}

		/// <summary>
		/// Gets the override data for a specific node, if any.
		/// </summary>
		public NodeOverride FindNodeOverride(string nodeId)
		{
			return _nodeOverrides.Find(o => o.NodeId == nodeId);
		}

		private NodeOverride GetOrCreateNodeOverride(string nodeId)
		{
			var existing = FindNodeOverride(nodeId);
			if (existing != null) return existing;

			var newOverride = new NodeOverride(nodeId);
			_nodeOverrides.Add(newOverride);
			return newOverride;
		}

	}
}
