using System;
using System.Collections.Generic;
using Nonatomic.VSM.Blackboard;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Utils;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// ScriptableObject containing the complete state machine definition.
	/// This is the main asset that defines a state machine's structure.
	/// </summary>
	// Removed CreateAssetMenu - use StateMachineGraphAssetCreator instead
	public class StateMachineGraph : ScriptableObject, ISerializationCallbackReceiver
	{
		/// <summary>
		/// Fired when any graph's blackboard definitions change (variable added, removed, or exposure changed).
		/// Parameters: graph instance, variable key (null if multiple changed)
		/// </summary>
		public static event Action<StateMachineGraph, string> OnBlackboardChanged;

		/// <summary>
		/// Fired when a blackboard variable is renamed.
		/// Parameters: graph instance, old key, new key
		/// </summary>
		public static event Action<StateMachineGraph, string, string> OnBlackboardVariableRenamed;

		[SerializeField] private string _entryNodeId;

		[SerializeField] private List<SerializedNode> _serializedNodes = new List<SerializedNode>();

		[SerializeField] private List<Connection> _connections = new List<Connection>();

		[SerializeField] private List<BlackboardVariableDefinition> _blackboardDefinitions = new List<BlackboardVariableDefinition>();

		[SerializeField] private GraphViewData _graphViewData = new GraphViewData();

		[SerializeField] private List<GraphRegion> _regions = new List<GraphRegion>();

		[SerializeField] private string _contextTypeAssemblyQualifiedName;

		// Cached context type (restored from assembly qualified name)
		private Type _contextType;

		// Runtime data (populated after deserialization)
		private Dictionary<string, GraphNode> _nodes;
		private Dictionary<string, List<Connection>> _connectionsBySourceNode;
		private Dictionary<string, List<Connection>> _connectionsByTargetNode;

		// Connection lookup by ID for O(1) removal
		private Dictionary<string, Connection> _connectionsById;

		// Blackboard variable lookup by key for O(1) access
		private Dictionary<string, BlackboardVariableDefinition> _blackboardByKey;

		// Region lookup by ID for O(1) access
		private Dictionary<string, GraphRegion> _regionsById;

		// Deferred node deserialization flag — JsonUtility.FromJson on node types
		// containing Unity Object references (e.g. SubStateMachineNode._subGraph)
		// cannot run during OnAfterDeserialize as it may execute on a background thread.
		private bool _needsNodeDeserialization;

		/// <summary>
		/// Notifies listeners that blackboard definitions have changed.
		/// </summary>
		public void NotifyBlackboardChanged(string variableKey = null)
		{
			OnBlackboardChanged?.Invoke(this, variableKey);
		}

		/// <summary>
		/// Notifies listeners that a blackboard variable was renamed.
		/// </summary>
		public void NotifyBlackboardVariableRenamed(string oldKey, string newKey)
		{
			OnBlackboardVariableRenamed?.Invoke(this, oldKey, newKey);
		}

		/// <summary>
		/// Called when the ScriptableObject is loaded.
		/// For non-generic graphs, syncs ContextType from actual state requirements
		/// to handle stale serialized values after state changes.
		/// Generic StateMachineGraph&lt;T&gt; overrides this to enforce its typed context.
		/// </summary>
		protected virtual void OnEnable()
		{
			var requiredType = GetRequiredContextTypeFromStates();
			if (ContextType != requiredType)
			{
				ContextType = requiredType;
			}
		}

		protected void EnsureDictionariesInitialized()
		{
			_nodes ??= new Dictionary<string, GraphNode>();
			_connectionsBySourceNode ??= new Dictionary<string, List<Connection>>();
			_connectionsByTargetNode ??= new Dictionary<string, List<Connection>>();
			_connectionsById ??= new Dictionary<string, Connection>();
			_blackboardByKey ??= new Dictionary<string, BlackboardVariableDefinition>();
			_regionsById ??= new Dictionary<string, GraphRegion>();
		}

		/// <summary>
		/// Performs deferred node deserialization.
		/// JsonUtility.FromJson on types with Unity Object fields (e.g. SubStateMachineNode)
		/// cannot run during OnAfterDeserialize because it may execute on a background thread.
		/// This method is called lazily on first access from the main thread.
		/// </summary>
		protected void EnsureNodesDeserialized()
		{
			if (!_needsNodeDeserialization) return;
			_needsNodeDeserialization = false;

			EnsureDictionariesInitialized();
			_nodes.Clear();

			foreach (var serialized in _serializedNodes)
			{
				var node = serialized.ToNode();
				if (node != null)
				{
					_nodes[node.Id] = node;
				}
			}

			RebuildConnectionIndices();
		}

		protected Dictionary<string, GraphNode> NodesDictionary
		{
			get
			{
				EnsureNodesDeserialized();
				EnsureDictionariesInitialized();
				return _nodes;
			}
		}

		protected List<Connection> ConnectionsList => _connections;

		protected List<BlackboardVariableDefinition> BlackboardDefinitionsList => _blackboardDefinitions;

		protected List<SerializedNode> SerializedNodesList => _serializedNodes;

		protected Dictionary<string, List<Connection>> ConnectionsBySourceNode
		{
			get
			{
				EnsureDictionariesInitialized();
				return _connectionsBySourceNode;
			}
		}

		protected Dictionary<string, List<Connection>> ConnectionsByTargetNode
		{
			get
			{
				EnsureDictionariesInitialized();
				return _connectionsByTargetNode;
			}
		}

		protected Dictionary<string, Connection> ConnectionsById
		{
			get
			{
				EnsureDictionariesInitialized();
				return _connectionsById;
			}
		}

		/// <summary>
		/// Whether this graph is a variant of another graph.
		/// </summary>
		public virtual bool IsVariant => false;

		/// <summary>
		/// ID of the entry node.
		/// </summary>
		public virtual string EntryNodeId
		{
			get => _entryNodeId;
			set => _entryNodeId = value;
		}

		/// <summary>
		/// All connections in the graph.
		/// </summary>
		public virtual IReadOnlyList<Connection> Connections => _connections;

		/// <summary>
		/// Blackboard variable definitions.
		/// </summary>
		public virtual IReadOnlyList<BlackboardVariableDefinition> BlackboardDefinitions => _blackboardDefinitions;

		/// <summary>
		/// Graph view data (positions, zoom, etc.).
		/// </summary>
		public GraphViewData GraphViewData => _graphViewData;

		/// <summary>
		/// Visual regions for organizing nodes.
		/// </summary>
		public IReadOnlyList<GraphRegion> Regions => _regions;

		/// <summary>
		/// The context type required by this state machine graph.
		/// When set, states inheriting from State&lt;TContext&gt; can access typed shared context.
		/// Null if no typed context is required.
		/// </summary>
		public Type ContextType
		{
			get
			{
				if (_contextType == null && !string.IsNullOrEmpty(_contextTypeAssemblyQualifiedName))
				{
					_contextType = TypeValidation.SafeGetType(_contextTypeAssemblyQualifiedName);
				}

				return _contextType;
			}
			set
			{
				_contextType = value;
				_contextTypeAssemblyQualifiedName = value?.AssemblyQualifiedName;
			}
		}

		/// <summary>
		/// Whether this graph requires a typed context.
		/// </summary>
		public bool RequiresContext => ContextType != null;

		/// <summary>
		/// The assembly qualified name of the context type (for serialization).
		/// </summary>
		public string ContextTypeAssemblyQualifiedName => _contextTypeAssemblyQualifiedName;

		/// <summary>
		/// Scans state nodes to determine the required context type from State&lt;TContext&gt; inheritance.
		/// Useful when ContextType is not explicitly set on the graph.
		/// Returns null if no states require a typed context.
		/// </summary>
		public Type GetRequiredContextTypeFromStates()
		{
			foreach (var node in GetNodesOfType<StateNode>())
			{
				var stateType = node.StateType;
				if (stateType == null) continue;

				var contextType = StateMachineInstance.GetRequiredContextType(stateType);
				if (contextType != null) return contextType;
			}

			return null;
		}

		/// <summary>
		/// Number of nodes in the graph.
		/// </summary>
		public virtual int NodeCount => NodesDictionary.Count;

		/// <summary>
		/// Number of connections in the graph.
		/// </summary>
		public virtual int ConnectionCount => _connections.Count;


		/// <summary>
		/// Gets a node by ID.
		/// </summary>
		public virtual GraphNode GetNode(string id)
		{
			NodesDictionary.TryGetValue(id, out var node);
			return node;
		}

		/// <summary>
		/// Gets a typed node by ID.
		/// </summary>
		public T GetNode<T>(string id) where T : GraphNode
		{
			return GetNode(id) as T;
		}

		/// <summary>
		/// Gets all nodes in the graph.
		/// </summary>
		public virtual IEnumerable<GraphNode> GetAllNodes()
		{
			return NodesDictionary.Values;
		}

		/// <summary>
		/// Gets all nodes of a specific type.
		/// </summary>
		public virtual IEnumerable<T> GetNodesOfType<T>() where T : GraphNode
		{
			foreach (var node in NodesDictionary.Values)
			{
				if (node is T typed)
				{
					yield return typed;
				}
			}
		}

		/// <summary>
		/// Checks if a node exists.
		/// </summary>
		public virtual bool HasNode(string id)
		{
			return NodesDictionary.ContainsKey(id);
		}

		/// <summary>
		/// Gets the entry node.
		/// </summary>
		public virtual EntryNode GetEntryNode()
		{
			if (string.IsNullOrEmpty(_entryNodeId))
			{
				return null;
			}

			return GetNode<EntryNode>(_entryNodeId);
		}



		/// <summary>
		/// Gets all connections from a specific node.
		/// </summary>
		public virtual IReadOnlyList<Connection> GetConnectionsFromNode(string nodeId)
		{
			if (_connectionsBySourceNode != null && _connectionsBySourceNode.TryGetValue(nodeId, out var connections))
			{
				return connections;
			}

			return Array.Empty<Connection>();
		}

		/// <summary>
		/// Gets the first connection from a specific node, or null if none exist.
		/// O(1) lookup from the indexed dictionary without enumerator allocation.
		/// </summary>
		public virtual Connection GetFirstConnectionFromNode(string nodeId)
		{
			if (_connectionsBySourceNode != null &&
				_connectionsBySourceNode.TryGetValue(nodeId, out var connections) &&
				connections.Count > 0)
			{
				return connections[0];
			}

			return null;
		}

		/// <summary>
		/// Gets all connections to a specific node.
		/// </summary>
		public virtual IReadOnlyList<Connection> GetConnectionsToNode(string nodeId)
		{
			if (_connectionsByTargetNode != null && _connectionsByTargetNode.TryGetValue(nodeId, out var connections))
			{
				return connections;
			}

			return Array.Empty<Connection>();
		}

		/// <summary>
		/// Gets a connection to a specific port on a node.
		/// </summary>
		public virtual Connection GetConnectionToPort(string nodeId, string portName)
		{
			if (_connectionsByTargetNode != null && _connectionsByTargetNode.TryGetValue(nodeId, out var connections))
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

		/// <summary>
		/// Gets all connections from a specific port on a node.
		/// </summary>
		public virtual IEnumerable<Connection> GetConnectionsFromPort(string nodeId, string portName)
		{
			if (_connectionsBySourceNode != null && _connectionsBySourceNode.TryGetValue(nodeId, out var connections))
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

		/// <summary>
		/// Checks if a port has any connections.
		/// </summary>
		public virtual bool IsPortConnected(string nodeId, string portName, bool isInput)
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



		/// <summary>
		/// Called when the ScriptableObject is first created or reset.
		/// Ensures the graph is properly initialized with an entry node.
		/// </summary>
		protected virtual void Reset()
		{
			InitializeNewGraph();
		}

		/// <summary>
		/// Initializes a new graph with required elements (entry node).
		/// Call this after creating a new StateMachineGraph asset.
		/// </summary>
		public void InitializeNewGraph()
		{
			// Create entry node if one doesn't exist
			if (string.IsNullOrEmpty(_entryNodeId))
			{
				var entryNode = new EntryNode();
				entryNode.Position = Vector2.zero;

				// Set the entry node ID
				_entryNodeId = entryNode.Id;

				// Add to runtime dictionary
				NodesDictionary[entryNode.Id] = entryNode;

				// Add directly to serialized list for persistence
				_serializedNodes.Add(SerializedNode.FromNode(entryNode));
			}
		}

		/// <summary>
		/// Adds a node to the graph.
		/// </summary>
		public void AddNode(GraphNode node)
		{
			if (node == null || string.IsNullOrEmpty(node.Id)) return;

			var nodes = NodesDictionary;

			nodes[node.Id] = node;

			// Also add to serialized list to ensure persistence during asset creation
			var existingIndex = _serializedNodes.FindIndex(serializedNode => serializedNode.NodeId == node.Id);
			if (existingIndex >= 0)
			{
				_serializedNodes[existingIndex] = SerializedNode.FromNode(node);
			}
			else
			{
				_serializedNodes.Add(SerializedNode.FromNode(node));
			}

			if (node is EntryNode && string.IsNullOrEmpty(_entryNodeId))
			{
				_entryNodeId = node.Id;
			}
		}

		/// <summary>
		/// Removes a node from the graph.
		/// </summary>
		public void RemoveNode(string nodeId)
		{
			if (NodesDictionary.Remove(nodeId))
			{
				_serializedNodes.RemoveAll(serializedNode => serializedNode.NodeId == nodeId);
				_connections.RemoveAll(connection => connection.SourceNodeId == nodeId || connection.TargetNodeId == nodeId);
				RebuildConnectionIndices();

				// Clean up region containment references to the removed node
				foreach (var region in _regions)
				{
					region.RemoveNode(nodeId);
				}
			}
		}

		/// <summary>
		/// Adds a connection to the graph.
		/// </summary>
		public void AddConnection(Connection connection)
		{
			if (connection == null) return;

			EnsureDictionariesInitialized();

			_connections.Add(connection);
			IndexConnection(connection);
		}

		/// <summary>
		/// Removes a connection from the graph.
		/// </summary>
		public void RemoveConnection(string connectionId)
		{
			EnsureDictionariesInitialized();

			if (_connectionsById.ContainsKey(connectionId))
			{
				_connections.RemoveAll(c => c.Id == connectionId);
				RebuildConnectionIndices();
			}
		}

		/// <summary>
		/// Adds a blackboard variable definition.
		/// </summary>
		public void AddBlackboardVariable(BlackboardVariableDefinition definition)
		{
			if (definition == null)
			{
				return;
			}

			EnsureDictionariesInitialized();

			// Check for duplicate keys using dictionary
			if (_blackboardByKey.ContainsKey(definition.Key))
			{
				var existingIndex = _blackboardDefinitions.FindIndex(d => d.Key == definition.Key);
				if (existingIndex >= 0)
				{
					_blackboardDefinitions[existingIndex] = definition;
				}
			}
			else
			{
				_blackboardDefinitions.Add(definition);
			}

			_blackboardByKey[definition.Key] = definition;
		}

		/// <summary>
		/// Removes a blackboard variable definition.
		/// </summary>
		public void RemoveBlackboardVariable(string key)
		{
			EnsureDictionariesInitialized();

			_blackboardDefinitions.RemoveAll(d => d.Key == key);
			_blackboardByKey.Remove(key);
		}

		/// <summary>
		/// Gets a blackboard variable definition by key.
		/// </summary>
		public BlackboardVariableDefinition GetBlackboardVariable(string key)
		{
			EnsureDictionariesInitialized();

			_blackboardByKey.TryGetValue(key, out var definition);
			return definition;
		}

		/// <summary>
		/// Adds a region to the graph.
		/// </summary>
		public void AddRegion(GraphRegion region)
		{
			if (region == null) return;

			EnsureDictionariesInitialized();

			if (_regionsById.ContainsKey(region.Id))
			{
				var existingIndex = _regions.FindIndex(r => r.Id == region.Id);
				if (existingIndex >= 0)
				{
					_regions[existingIndex] = region;
				}
			}
			else
			{
				_regions.Add(region);
			}

			_regionsById[region.Id] = region;
		}

		/// <summary>
		/// Removes a region from the graph.
		/// </summary>
		public void RemoveRegion(string regionId)
		{
			EnsureDictionariesInitialized();

			_regions.RemoveAll(r => r.Id == regionId);
			_regionsById.Remove(regionId);
		}

		/// <summary>
		/// Gets a region by ID.
		/// </summary>
		public GraphRegion GetRegion(string regionId)
		{
			EnsureDictionariesInitialized();

			_regionsById.TryGetValue(regionId, out var region);
			return region;
		}

		/// <summary>
		/// Updates an existing region's properties.
		/// </summary>
		public void UpdateRegion(GraphRegion region)
		{
			if (region == null) return;

			EnsureDictionariesInitialized();

			if (_regionsById.ContainsKey(region.Id))
			{
				var existingIndex = _regions.FindIndex(r => r.Id == region.Id);
				if (existingIndex >= 0)
				{
					_regions[existingIndex] = region;
				}

				_regionsById[region.Id] = region;
			}
		}



		/// <inheritdoc />
		public virtual void OnBeforeSerialize()
		{
			// Cannot call Unity APIs during serialization - no logging here

			// Only rebuild serialized nodes if we have runtime nodes
			// This preserves _serializedNodes when _nodes hasn't been populated yet
			// Also skip if node deserialization is still deferred — _serializedNodes is authoritative
			if (!_needsNodeDeserialization && _nodes != null && _nodes.Count > 0)
			{
				_serializedNodes.Clear();

				foreach (var node in _nodes.Values)
				{
					_serializedNodes.Add(SerializedNode.FromNode(node));
				}
			}

			foreach (var definition in _blackboardDefinitions)
			{
				definition.CacheTypeName();
			}
		}

		/// <inheritdoc />
		public virtual void OnAfterDeserialize()
		{
			// Cannot call Unity APIs during deserialization - no logging here
			// Node deserialization is deferred to EnsureNodesDeserialized() because
			// JsonUtility.FromJson on types with Unity Object fields (e.g. SubStateMachineNode)
			// requires the main thread, but OnAfterDeserialize can run on background threads.

			EnsureDictionariesInitialized();

			_connectionsBySourceNode.Clear();
			_connectionsByTargetNode.Clear();
			_connectionsById.Clear();
			_blackboardByKey.Clear();
			_regionsById.Clear();

			_needsNodeDeserialization = true;

			foreach (var definition in _blackboardDefinitions)
			{
				definition.RestoreType();
				if (!string.IsNullOrEmpty(definition.Key))
				{
					_blackboardByKey[definition.Key] = definition;
				}
			}

			foreach (var region in _regions)
			{
				if (!string.IsNullOrEmpty(region.Id))
				{
					_regionsById[region.Id] = region;
				}
			}

			// Restore context type with validation
			if (!string.IsNullOrEmpty(_contextTypeAssemblyQualifiedName))
			{
				_contextType = TypeValidation.SafeGetType(_contextTypeAssemblyQualifiedName);
			}
		}

		protected void RebuildConnectionIndices()
		{
			EnsureDictionariesInitialized();

			_connectionsBySourceNode.Clear();
			_connectionsByTargetNode.Clear();
			_connectionsById.Clear();

			foreach (var connection in _connections)
			{
				IndexConnection(connection);
			}
		}

		protected void IndexConnection(Connection connection)
		{
			if (!string.IsNullOrEmpty(connection.Id))
			{
				_connectionsById[connection.Id] = connection;
			}

			if (!string.IsNullOrEmpty(connection.SourceNodeId))
			{
				if (!_connectionsBySourceNode.TryGetValue(connection.SourceNodeId, out var sourceList))
				{
					sourceList = new List<Connection>();
					_connectionsBySourceNode[connection.SourceNodeId] = sourceList;
				}

				sourceList.Add(connection);
			}

			if (!string.IsNullOrEmpty(connection.TargetNodeId))
			{
				if (!_connectionsByTargetNode.TryGetValue(connection.TargetNodeId, out var targetList))
				{
					targetList = new List<Connection>();
					_connectionsByTargetNode[connection.TargetNodeId] = targetList;
				}

				targetList.Add(connection);
			}
		}



		/// <summary>
		/// Validates the graph and returns any issues found.
		/// </summary>
		public List<GraphValidationIssue> Validate()
		{
			EnsureDictionariesInitialized();
			return GraphValidator.Validate(this);
		}

	}
}
