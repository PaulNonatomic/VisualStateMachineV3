using System;
using System.Collections.Generic;
using Nonatomic.VSM.Utils;
using UnityEngine;

namespace Nonatomic.VSM.Ports
{
	/// <summary>
	/// Contains all port information discovered for a state type.
	/// This is generated via reflection at edit-time and cached.
	/// </summary>
	[Serializable]
	public class StatePortManifest
	{
		[SerializeField] private string _stateTypeName;

		[SerializeField] private List<InputPortInfo> _inputPorts = new List<InputPortInfo>();

		[SerializeField] private List<OutputPortInfo> _outputPorts = new List<OutputPortInfo>();

		[SerializeField] private List<TransitionPortInfo> _transitionPorts = new List<TransitionPortInfo>();

		[NonSerialized] private Type _stateType;

		/// <summary>
		/// Assembly-qualified name of the state type.
		/// </summary>
		public string StateTypeName
		{
			get => _stateTypeName;
			set => _stateTypeName = value;
		}

		/// <summary>
		/// The state type this manifest describes.
		/// </summary>
		public Type StateType
		{
			get
			{
				if (_stateType == null && !string.IsNullOrEmpty(_stateTypeName))
				{
					_stateType = TypeValidation.SafeGetType(_stateTypeName);
				}

				return _stateType;
			}
			set
			{
				_stateType = value;
				_stateTypeName = value?.AssemblyQualifiedName;
			}
		}

		/// <summary>
		/// All input ports on this state.
		/// </summary>
		public List<InputPortInfo> InputPorts => _inputPorts;

		/// <summary>
		/// All output ports on this state.
		/// </summary>
		public List<OutputPortInfo> OutputPorts => _outputPorts;

		/// <summary>
		/// All transition ports on this state.
		/// </summary>
		public List<TransitionPortInfo> TransitionPorts => _transitionPorts;

		/// <summary>
		/// Enumerates all ports regardless of direction.
		/// </summary>
		public IEnumerable<PortInfo> AllPorts
		{
			get
			{
				foreach (var port in _inputPorts)
				{
					yield return port;
				}

				foreach (var port in _outputPorts)
				{
					yield return port;
				}

				foreach (var port in _transitionPorts)
				{
					yield return port;
				}
			}
		}

		/// <summary>
		/// Total number of ports.
		/// </summary>
		public int TotalPortCount => _inputPorts.Count + _outputPorts.Count + _transitionPorts.Count;

		/// <summary>
		/// Gets an input port by field name.
		/// </summary>
		public InputPortInfo GetInputPort(string fieldName)
		{
			return _inputPorts.Find(p => p.FieldName == fieldName);
		}

		/// <summary>
		/// Gets an output port by field name.
		/// </summary>
		public OutputPortInfo GetOutputPort(string fieldName)
		{
			return _outputPorts.Find(p => p.FieldName == fieldName);
		}

		/// <summary>
		/// Gets a transition port by field name.
		/// </summary>
		public TransitionPortInfo GetTransitionPort(string fieldName)
		{
			return _transitionPorts.Find(p => p.FieldName == fieldName);
		}

		/// <summary>
		/// Gets any port by field name.
		/// </summary>
		public PortInfo GetPort(string fieldName)
		{
			foreach (var port in AllPorts)
			{
				if (port.FieldName == fieldName)
				{
					return port;
				}
			}

			return null;
		}

		/// <summary>
		/// Checks if any input port is marked as required.
		/// </summary>
		public bool HasRequiredInputs()
		{
			foreach (var port in _inputPorts)
			{
				if (port.IsRequired)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Gets all required input ports.
		/// </summary>
		public IEnumerable<InputPortInfo> GetRequiredInputPorts()
		{
			foreach (var port in _inputPorts)
			{
				if (port.IsRequired)
				{
					yield return port;
				}
			}
		}

		/// <summary>
		/// Caches all type names for serialization.
		/// </summary>
		public void CacheTypeNames()
		{
			if (_stateType != null)
			{
				_stateTypeName = _stateType.AssemblyQualifiedName;
			}

			foreach (var port in AllPorts)
			{
				port.CacheTypeName();
			}
		}

		/// <summary>
		/// Restores all types from cached names after deserialization.
		/// </summary>
		public void RestoreTypes()
		{
			if (!string.IsNullOrEmpty(_stateTypeName))
			{
				_stateType = TypeValidation.SafeGetType(_stateTypeName);
			}

			foreach (var port in AllPorts)
			{
				port.RestoreType();
			}
		}
	}
}
