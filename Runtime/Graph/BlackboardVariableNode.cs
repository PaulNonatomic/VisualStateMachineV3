using System;
using Nonatomic.VSM.Utils;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Represents a blackboard variable dragged into the graph.
	/// Can act as either a getter (output port) or setter (input port).
	/// </summary>
	[Serializable]
	public class BlackboardVariableNode : GraphNode
	{
		[SerializeField] private string _variableKey;

		[SerializeField] private BlackboardNodeMode _mode;

		[SerializeField] private string _typeAssemblyQualifiedName;

		[NonSerialized] private Type _variableType;

		/// <summary>
		/// Key of the blackboard variable this node references.
		/// </summary>
		public string VariableKey
		{
			get => _variableKey;
			set => _variableKey = value;
		}

		/// <summary>
		/// Whether this is a getter (provides value) or setter (receives value).
		/// </summary>
		public BlackboardNodeMode Mode
		{
			get => _mode;
			set => _mode = value;
		}

		/// <summary>
		/// Assembly-qualified name of the variable type.
		/// </summary>
		public string TypeAssemblyQualifiedName
		{
			get => _typeAssemblyQualifiedName;
			set => _typeAssemblyQualifiedName = value;
		}

		/// <summary>
		/// The type of the variable.
		/// </summary>
		public Type VariableType
		{
			get
			{
				if (_variableType == null && !string.IsNullOrEmpty(_typeAssemblyQualifiedName))
				{
					_variableType = TypeValidation.SafeGetType(_typeAssemblyQualifiedName);
				}

				return _variableType;
			}
			set
			{
				_variableType = value;
				_typeAssemblyQualifiedName = value?.AssemblyQualifiedName;
			}
		}

		public override GraphNodeType NodeType => GraphNodeType.BlackboardVariable;

		public BlackboardVariableNode() : base()
		{
			_mode = BlackboardNodeMode.Get;
		}

		public BlackboardVariableNode(string id) : base(id)
		{
			_mode = BlackboardNodeMode.Get;
		}

		public BlackboardVariableNode(string variableKey, Type variableType, BlackboardNodeMode mode) : base()
		{
			_variableKey = variableKey;
			VariableType = variableType;
			_mode = mode;
			DisplayName = variableKey;
		}

		/// <summary>
		/// Restores the type from the cached name after deserialization.
		/// </summary>
		public void RestoreType()
		{
			if (!string.IsNullOrEmpty(_typeAssemblyQualifiedName))
			{
				_variableType = TypeValidation.SafeGetType(_typeAssemblyQualifiedName);
			}
		}
	}

	/// <summary>
	/// Mode of a blackboard variable node.
	/// </summary>
	public enum BlackboardNodeMode
	{
		/// <summary>
		/// Node outputs the variable value (getter).
		/// </summary>
		Get,

		/// <summary>
		/// Node receives a value to write to the variable (setter).
		/// </summary>
		Set
	}
}
