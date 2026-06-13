using System;
using System.Collections.Generic;
using Nonatomic.VSM.Blackboard;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Represents an embedded sub-state machine in the graph.
	/// </summary>
	[Serializable]
	public class SubStateMachineNode : GraphNode
	{
		[SerializeField] private string _subGraphAssetGuid;
		[SerializeField] private StateMachineGraph _subGraph;
		[SerializeField] private SubStateMachineDataMode _dataMode;
		[SerializeField] private List<VariableMapping> _inputMappings = new List<VariableMapping>();
		[SerializeField] private List<VariableMapping> _outputMappings = new List<VariableMapping>();
		[SerializeField] private List<string> _exposedInputs = new List<string>();
		[SerializeField] private List<string> _exposedOutputs = new List<string>();
		[SerializeField] private List<SerializedPropertyOverride> _exposedPortValues = new List<SerializedPropertyOverride>();

		/// <summary>
		/// GUID reference to the sub-state machine graph asset.
		/// </summary>
		public string SubGraphAssetGuid
		{
			get => _subGraphAssetGuid;
			set => _subGraphAssetGuid = value;
		}

		/// <summary>
		/// Direct reference to the sub-state machine graph asset.
		/// Used at runtime for instantiation.
		/// </summary>
		public StateMachineGraph SubGraph
		{
			get => _subGraph;
			set => _subGraph = value;
		}

		/// <summary>
		/// How the sub-state machine accesses parent data.
		/// </summary>
		public SubStateMachineDataMode DataMode
		{
			get => _dataMode;
			set => _dataMode = value;
		}

		/// <summary>
		/// Explicit variable mappings from parent to child.
		/// </summary>
		public List<VariableMapping> InputMappings => _inputMappings;

		/// <summary>
		/// Explicit variable mappings from child back to parent.
		/// </summary>
		public List<VariableMapping> OutputMappings => _outputMappings;

		/// <summary>
		/// Variables to expose as input ports on this node.
		/// </summary>
		public List<string> ExposedInputs => _exposedInputs;

		/// <summary>
		/// Variables to expose as output ports on this node.
		/// </summary>
		public List<string> ExposedOutputs => _exposedOutputs;

		public override GraphNodeType NodeType => GraphNodeType.SubStateMachine;

		/// <summary>
		/// Initializes a new sub-state machine node with an auto-generated ID.
		/// </summary>
		public SubStateMachineNode() : base()
		{
			DisplayName = "Sub State Machine";
			_dataMode = SubStateMachineDataMode.Inherit;
		}

		/// <summary>
		/// Initializes a new sub-state machine node with the specified ID.
		/// </summary>
		/// <param name="id">Node ID, or null to auto-generate.</param>
		public SubStateMachineNode(string id) : base(id)
		{
			DisplayName = "Sub State Machine";
			_dataMode = SubStateMachineDataMode.Inherit;
		}

		/// <summary>
		/// Gets a stored port value by key.
		/// </summary>
		public SerializedPropertyOverride GetExposedPortValue(string key)
		{
			return _exposedPortValues.Find(p => p.FieldName == key);
		}

		/// <summary>
		/// Sets a stored port value by key.
		/// </summary>
		public void SetExposedPortValue(string key, object value, Type type)
		{
			var existing = GetExposedPortValue(key);
			if (existing != null)
			{
				existing.SetValue(value, type);
			}
			else
			{
				_exposedPortValues.Add(new SerializedPropertyOverride(key, value, type));
			}
		}

		/// <summary>
		/// Returns all stored exposed port values.
		/// </summary>
		public IReadOnlyList<SerializedPropertyOverride> ExposedPortValues => _exposedPortValues;

		/// <summary>
		/// Returns blackboard variable definitions from the sub-graph that are exposed as inputs.
		/// </summary>
		public IEnumerable<BlackboardVariableDefinition> GetExposedInputDefinitions()
		{
			if (_subGraph == null) yield break;

			foreach (var definition in _subGraph.BlackboardDefinitions)
			{
				if (definition.ExposureDirection == ExposureDirection.Input ||
					definition.ExposureDirection == ExposureDirection.InputOutput)
				{
					yield return definition;
				}
			}
		}

		/// <summary>
		/// Returns blackboard variable definitions from the sub-graph that are exposed as outputs.
		/// </summary>
		public IEnumerable<BlackboardVariableDefinition> GetExposedOutputDefinitions()
		{
			if (_subGraph == null) yield break;

			foreach (var definition in _subGraph.BlackboardDefinitions)
			{
				if (definition.ExposureDirection == ExposureDirection.Output ||
					definition.ExposureDirection == ExposureDirection.InputOutput)
				{
					yield return definition;
				}
			}
		}
	}

	/// <summary>
	/// How a sub-state machine accesses parent data.
	/// </summary>
	public enum SubStateMachineDataMode
	{
		/// <summary>
		/// Child blackboard inherits from parent (can read parent vars).
		/// </summary>
		Inherit,

		/// <summary>
		/// Child has isolated blackboard with explicit mappings only.
		/// </summary>
		Isolated,

		/// <summary>
		/// Child shares the same blackboard instance as parent.
		/// </summary>
		Shared
	}

	/// <summary>
	/// Mapping between parent and child blackboard variables.
	/// </summary>
	[Serializable]
	public class VariableMapping
	{
		[SerializeField] private string _parentVariableKey;
		[SerializeField] private string _childVariableKey;
		[SerializeField] private VariableMappingDirection _direction;

		public string ParentVariableKey
		{
			get => _parentVariableKey;
			set => _parentVariableKey = value;
		}

		public string ChildVariableKey
		{
			get => _childVariableKey;
			set => _childVariableKey = value;
		}

		public VariableMappingDirection Direction
		{
			get => _direction;
			set => _direction = value;
		}

		public VariableMapping()
		{
		}

		public VariableMapping(string parentKey, string childKey, VariableMappingDirection direction)
		{
			_parentVariableKey = parentKey;
			_childVariableKey = childKey;
			_direction = direction;
		}
	}

	/// <summary>
	/// Direction of variable mapping between parent and child.
	/// </summary>
	public enum VariableMappingDirection
	{
		/// <summary>
		/// Value flows from parent to child on entry.
		/// </summary>
		ParentToChild,

		/// <summary>
		/// Value flows from child to parent on exit.
		/// </summary>
		ChildToParent,

		/// <summary>
		/// Value flows both directions.
		/// </summary>
		Bidirectional
	}
}
