using System;
using System.Collections.Generic;
using System.Reflection;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using Nonatomic.VSM.Utils;
using UnityEngine;

namespace Nonatomic.VSM.Binding
{
	/// <summary>
	/// Handles binding data between ports, blackboard variables, and state fields at runtime.
	/// </summary>
	public class PortBindingService
	{
		private readonly StateMachineGraph _graph;
		private readonly Blackboard.Blackboard _blackboard;
		private readonly Dictionary<string, State> _stateInstances;

		public PortBindingService(
			StateMachineGraph graph,
			Blackboard.Blackboard blackboard,
			Dictionary<string, State> stateInstances)
		{
			_graph = graph ?? throw new ArgumentNullException(nameof(graph));
			_blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
			_stateInstances = stateInstances ?? throw new ArgumentNullException(nameof(stateInstances));
		}

		/// <summary>
		/// Populates input ports on a state before OnEnter is called.
		/// </summary>
		public void BindInputs(State state)
		{
			if (state == null)
			{
				return;
			}

			var nodeId = state.NodeId;
			if (string.IsNullOrEmpty(nodeId))
			{
				return;
			}

			var stateType = state.GetType();

			// Get all connections to this node
			var inputConnections = _graph.GetConnectionsToNode(nodeId);
			for (var i = 0; i < inputConnections.Count; i++)
			{
				var connection = inputConnections[i];
				if (connection.ConnectionType == ConnectionType.Data ||
					connection.ConnectionType == ConnectionType.BlackboardRead)
				{
					BindInputPort(state, stateType, connection);
				}
			}

			// Apply property overrides for unconnected ports
			var stateNode = _graph.GetNode<StateNode>(nodeId);
			if (stateNode?.PropertyOverrides != null)
			{
				foreach (var propertyOverride in stateNode.PropertyOverrides)
				{
					if (!HasConnectionToPort(nodeId, propertyOverride.FieldName))
					{
						ApplyPropertyOverride(state, stateType, propertyOverride);
					}
				}
			}
		}

		/// <summary>
		/// Reads output ports from a state and propagates values to blackboard.
		/// </summary>
		public void BindOutputs(State state)
		{
			if (state == null)
			{
				return;
			}

			var nodeId = state.NodeId;
			if (string.IsNullOrEmpty(nodeId))
			{
				return;
			}

			var stateType = state.GetType();

			var outputConnections = _graph.GetConnectionsFromNode(nodeId);
			for (var i = 0; i < outputConnections.Count; i++)
			{
				var connection = outputConnections[i];
				if (connection.ConnectionType == ConnectionType.BlackboardWrite)
				{
					WriteOutputToBlackboard(state, stateType, connection);
				}
			}
		}

		/// <summary>
		/// Wires up transition actions to trigger state changes.
		/// </summary>
		public void BindTransitions(State state, Action<string> onTransition)
		{
			if (state == null)
			{
				return;
			}

			var nodeId = state.NodeId;
			if (string.IsNullOrEmpty(nodeId))
			{
				return;
			}

			var stateType = state.GetType();

			var transitionConnections = _graph.GetConnectionsFromNode(nodeId);
			for (var i = 0; i < transitionConnections.Count; i++)
			{
				var connection = transitionConnections[i];
				if (connection.ConnectionType == ConnectionType.Transition)
				{
					// Try to find field by SourcePortName (could be FieldName or DisplayName)
					var field = FindTransitionField(stateType, connection.SourcePortName);
					if (field != null && ActionTypeUtils.IsActionType(field.FieldType))
					{
						var targetNodeId = connection.TargetNodeId;
						field.SetValue(state, (Action)(() => onTransition(targetNodeId)));
					}
				}
			}
		}

		/// <summary>
		/// Finds a transition field by name, checking both field name and display name for backwards compatibility.
		/// </summary>
		private FieldInfo FindTransitionField(Type stateType, string portName)
		{
			// First try direct field lookup (new format uses FieldName)
			var field = ReflectionCache.GetField(stateType, portName);
			if (field != null && ReflectionCache.GetAttribute<TransitionAttribute>(field) != null)
			{
				return field;
			}

			// Fallback: search for a field whose TransitionAttribute DisplayName matches (old format)
			foreach (var f in ReflectionCache.GetFields(stateType))
			{
				var attr = ReflectionCache.GetAttribute<TransitionAttribute>(f);
				if (attr != null)
				{
					var displayName = attr.DisplayName ?? StringFormatUtils.FormatTransitionName(f.Name);
					if (displayName == portName)
					{
						return f;
					}
				}
			}

			return null;
		}

		private void BindInputPort(State state, Type stateType, Connection connection)
		{
			var field = ReflectionCache.GetField(stateType, connection.TargetPortName);
			if (field == null)
			{
				Debug.LogWarning($"Field '{connection.TargetPortName}' not found on {stateType.Name}");
				return;
			}

			if (ReflectionCache.GetAttribute<DataInAttribute>(field) == null)
			{
				Debug.LogWarning($"Field '{field.Name}' on {stateType.Name} is not marked with [DataIn] and cannot receive data.");
				return;
			}

			object value = null;

			if (connection.ConnectionType == ConnectionType.BlackboardRead)
			{
				// Source is a blackboard variable node
				var varNode = _graph.GetNode<BlackboardVariableNode>(connection.SourceNodeId);
				if (varNode != null)
				{
					value = _blackboard.GetVariable(varNode.VariableKey)?.BoxedValue;
				}
			}
			else if (connection.ConnectionType == ConnectionType.Data)
			{
				// Source is another state's output port
				if (_stateInstances.TryGetValue(connection.SourceNodeId, out var sourceState))
				{
					var sourceField = ReflectionCache.GetField(sourceState.GetType(), connection.SourcePortName);
					if (sourceField != null)
					{
						value = sourceField.GetValue(sourceState);
					}
				}
			}

			if (value != null)
			{
				SetFieldValue(state, field, value);
			}
		}

		private void WriteOutputToBlackboard(State state, Type stateType, Connection connection)
		{
			var field = ReflectionCache.GetField(stateType, connection.SourcePortName);
			if (field == null)
			{
				return;
			}

			if (ReflectionCache.GetAttribute<DataOutAttribute>(field) == null)
			{
				Debug.LogWarning($"Field '{field.Name}' on {stateType.Name} is not marked with [DataOut] and cannot provide data.");
				return;
			}

			var varNode = _graph.GetNode<BlackboardVariableNode>(connection.TargetNodeId);
			if (varNode == null)
			{
				return;
			}

			var value = field.GetValue(state);
			_blackboard.SetBoxed(varNode.VariableKey, value, field.FieldType);
		}

		private void ApplyPropertyOverride(State state, Type stateType, SerializedPropertyOverride propertyOverride)
		{
			var field = ReflectionCache.GetField(stateType, propertyOverride.FieldName);
			if (field == null)
			{
				return;
			}

			if (ReflectionCache.GetAttribute<DataInAttribute>(field) == null)
			{
				Debug.LogWarning($"Field '{field.Name}' on {stateType.Name} is not marked with [DataIn] and cannot receive property overrides.");
				return;
			}

			try
			{
				var value = propertyOverride.GetValue();
				if (value != null)
				{
					SetFieldValue(state, field, value);
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning($"Failed to apply override for '{propertyOverride.FieldName}': {e.Message}");
			}
		}

		private void SetFieldValue(State state, FieldInfo field, object value)
		{
			try
			{
				if (field.FieldType.IsAssignableFrom(value.GetType()))
				{
					field.SetValue(state, value);
				}
				else
				{
					var converted = Convert.ChangeType(value, field.FieldType);
					field.SetValue(state, converted);
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"Failed to set value on {state.GetType().Name}.{field.Name}: {e.Message}");
			}
		}

		private bool HasConnectionToPort(string nodeId, string portName)
		{
			return _graph.GetConnectionToPort(nodeId, portName) != null;
		}

	}
}
