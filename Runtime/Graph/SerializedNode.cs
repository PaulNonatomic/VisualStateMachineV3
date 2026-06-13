using System;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Wrapper for polymorphic node serialization.
	/// </summary>
	[Serializable]
	public class SerializedNode
	{
		[SerializeField] private GraphNodeType _nodeType;

		[SerializeField] private string _nodeId;

		[SerializeField] private string _jsonData;

		public GraphNodeType NodeType => _nodeType;
		public string NodeId => _nodeId;
		public string JsonData => _jsonData;

		public static SerializedNode FromNode(GraphNode node)
		{
			if (node == null)
			{
				return null;
			}

			// Pre-serialize type info for state nodes
			if (node is StateNode stateNode)
			{
				stateNode.CacheTypeName();
			}

			return new SerializedNode
			{
				_nodeType = node.NodeType,
				_nodeId = node.Id,
				_jsonData = JsonUtility.ToJson(node)
			};
		}

		public GraphNode ToNode()
		{
			if (string.IsNullOrEmpty(_jsonData))
			{
				return null;
			}

			GraphNode node;
			try
			{
				node = _nodeType switch
				{
					GraphNodeType.State => JsonUtility.FromJson<StateNode>(_jsonData),
					GraphNodeType.Entry => JsonUtility.FromJson<EntryNode>(_jsonData),
					GraphNodeType.Exit => JsonUtility.FromJson<ExitNode>(_jsonData),
					GraphNodeType.BlackboardVariable => JsonUtility.FromJson<BlackboardVariableNode>(_jsonData),
					GraphNodeType.SubStateMachine => JsonUtility.FromJson<SubStateMachineNode>(_jsonData),
					GraphNodeType.PortalOut => JsonUtility.FromJson<PortalOutNode>(_jsonData),
					GraphNodeType.PortalIn => JsonUtility.FromJson<PortalInNode>(_jsonData),
					_ => null
				};
			}
			catch (Exception ex)
			{
				Debug.LogError($"[SerializedNode] Failed to deserialize {_nodeType} node '{_nodeId}': {ex.Message}");
				return null;
			}

			// Post-deserialize type restoration
			if (node is StateNode stateNode)
			{
				stateNode.RestoreType();
			}
			else if (node is BlackboardVariableNode varNode)
			{
				varNode.RestoreType();
			}

			return node;
		}
	}
}
