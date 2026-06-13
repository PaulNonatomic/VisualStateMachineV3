using System;
using System.Collections.Generic;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Utils;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Represents a state in the visual graph.
	/// </summary>
	[Serializable]
	public class StateNode : GraphNode
	{
		[SerializeField] private string _stateTypeAssemblyQualifiedName;
		[SerializeField] private string _stateTypeScriptGuid;

		[SerializeField] private List<SerializedPropertyOverride> _propertyOverrides = new List<SerializedPropertyOverride>();

		[SerializeField] private bool _isEntryState;

		[NonSerialized] private Type _stateType;

		/// <summary>
		/// Assembly-qualified name of the State class.
		/// </summary>
		public string StateTypeAssemblyQualifiedName
		{
			get => _stateTypeAssemblyQualifiedName;
			set => _stateTypeAssemblyQualifiedName = value;
		}

		/// <summary>
		/// The MonoScript GUID for the State class. Used for rename-safe resolution.
		/// </summary>
		public string StateTypeScriptGuid
		{
			get => _stateTypeScriptGuid;
			set => _stateTypeScriptGuid = value;
		}

		/// <summary>
		/// The State type this node represents.
		/// </summary>
		public Type StateType
		{
			get
			{
				if (_stateType == null && !string.IsNullOrEmpty(_stateTypeAssemblyQualifiedName))
				{
					RestoreType();
				}

				return _stateType;
			}
			set
			{
				_stateType = value;
				_stateTypeAssemblyQualifiedName = value?.AssemblyQualifiedName;
			}
		}

		/// <summary>
		/// Serialized property overrides for input ports.
		/// </summary>
		public List<SerializedPropertyOverride> PropertyOverrides => _propertyOverrides;

		/// <summary>
		/// Whether this is the initial state (connected from entry).
		/// </summary>
		public bool IsEntryState
		{
			get => _isEntryState;
			set => _isEntryState = value;
		}

		public override GraphNodeType NodeType => GraphNodeType.State;

		/// <summary>
		/// Initializes a new state node with an auto-generated ID.
		/// </summary>
		public StateNode() : base()
		{
		}

		/// <summary>
		/// Initializes a new state node with the specified ID.
		/// </summary>
		/// <param name="id">Node ID, or null to auto-generate.</param>
		public StateNode(string id) : base(id)
		{
		}

		/// <summary>
		/// Initializes a new state node for the specified State type.
		/// </summary>
		/// <param name="stateType">The State class type this node represents.</param>
		public StateNode(Type stateType) : base()
		{
			StateType = stateType;
			DisplayName = stateType?.Name ?? "Unknown State";
		}

		/// <summary>
		/// Caches the type name for serialization.
		/// </summary>
		public void CacheTypeName()
		{
			if (_stateType != null)
			{
				_stateTypeAssemblyQualifiedName = _stateType.AssemblyQualifiedName;
			}
		}

		/// <summary>
		/// Restores the type from the cached name after deserialization.
		/// </summary>
		public void RestoreType()
		{
			if (!string.IsNullOrEmpty(_stateTypeAssemblyQualifiedName))
			{
				var resolvedType = TypeValidation.SafeGetType(_stateTypeAssemblyQualifiedName);

				// Validate that the resolved type inherits from State
				// This provides early warning for corrupted/invalid type data
				if (resolvedType != null && !typeof(State).IsAssignableFrom(resolvedType))
				{
					Debug.LogWarning($"StateNode '{DisplayName}': Type '{resolvedType.FullName}' does not inherit from State. This node will be skipped at runtime.");
					_stateType = null;
					return;
				}

				_stateType = resolvedType;
			}
		}

		/// <summary>
		/// Gets a property override by field name.
		/// </summary>
		public SerializedPropertyOverride GetPropertyOverride(string fieldName)
		{
			return _propertyOverrides.Find(p => p.FieldName == fieldName);
		}

		/// <summary>
		/// Sets a property override.
		/// </summary>
		public void SetPropertyOverride(string fieldName, object value, Type type)
		{
			var existing = GetPropertyOverride(fieldName);
			if (existing != null)
			{
				existing.SetValue(value, type);
			}
			else
			{
				var newOverride = new SerializedPropertyOverride(fieldName, value, type);
				_propertyOverrides.Add(newOverride);
			}
		}

		/// <summary>
		/// Removes a property override.
		/// </summary>
		public void RemovePropertyOverride(string fieldName)
		{
			_propertyOverrides.RemoveAll(p => p.FieldName == fieldName);
		}
	}

	/// <summary>
	/// Stores overridden values for input ports when not connected.
	/// </summary>
	[Serializable]
	public class SerializedPropertyOverride
	{
		[SerializeField] private string _fieldName;

		[SerializeField] private string _jsonValue;

		[SerializeField] private string _typeAssemblyQualifiedName;

		public string FieldName
		{
			get => _fieldName;
			set => _fieldName = value;
		}

		public string JsonValue
		{
			get => _jsonValue;
			set => _jsonValue = value;
		}

		public string TypeAssemblyQualifiedName
		{
			get => _typeAssemblyQualifiedName;
			set => _typeAssemblyQualifiedName = value;
		}

		public SerializedPropertyOverride()
		{
		}

		public SerializedPropertyOverride(string fieldName, object value, Type type)
		{
			_fieldName = fieldName;
			SetValue(value, type);
		}

		public void SetValue(object value, Type type)
		{
			_typeAssemblyQualifiedName = type?.AssemblyQualifiedName;

			if (value == null)
			{
				_jsonValue = null;
				return;
			}

			// Handle primitives using InvariantCulture for consistent serialization
			if (type == typeof(int))
			{
				_jsonValue = ((int)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (type == typeof(float))
			{
				_jsonValue = ((float)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (type == typeof(double))
			{
				_jsonValue = ((double)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (type == typeof(long))
			{
				_jsonValue = ((long)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (type == typeof(bool))
			{
				_jsonValue = value.ToString();
			}
			else if (type == typeof(string))
			{
				_jsonValue = value as string;
			}
			else
			{
				_jsonValue = JsonUtility.ToJson(value);
			}
		}

		public object GetValue()
		{
			if (string.IsNullOrEmpty(_jsonValue) || string.IsNullOrEmpty(_typeAssemblyQualifiedName))
			{
				return null;
			}

			var type = TypeValidation.SafeGetType(_typeAssemblyQualifiedName, logWarnings: false);

			// Fallback for primitive types when assembly resolution fails
			// This can happen in Unity 6+ where mscorlib may not resolve correctly
			if (type == null)
			{
				type = ResolvePrimitiveType(_typeAssemblyQualifiedName);
			}

			if (type == null)
			{
				return null;
			}

			// Handle primitives using InvariantCulture for consistent parsing
			if (type == typeof(int)) return int.TryParse(_jsonValue, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var i) ? i : 0;
			if (type == typeof(float)) return float.TryParse(_jsonValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0f;
			if (type == typeof(bool)) return bool.TryParse(_jsonValue, out var b) && b;
			if (type == typeof(double)) return double.TryParse(_jsonValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0.0;
			if (type == typeof(long)) return long.TryParse(_jsonValue, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var l) ? l : 0L;
			if (type == typeof(string)) return _jsonValue;

			return JsonUtility.FromJson(_jsonValue, type);
		}

		private static Type ResolvePrimitiveType(string typeAssemblyQualifiedName)
		{
			if (string.IsNullOrEmpty(typeAssemblyQualifiedName)) return null;

			// Check for common primitive types by their type name prefix
			// This handles cases where the assembly name doesn't resolve (e.g., mscorlib in .NET Standard)
			if (typeAssemblyQualifiedName.StartsWith("System.Single,")) return typeof(float);
			if (typeAssemblyQualifiedName.StartsWith("System.Int32,")) return typeof(int);
			if (typeAssemblyQualifiedName.StartsWith("System.Boolean,")) return typeof(bool);
			if (typeAssemblyQualifiedName.StartsWith("System.Double,")) return typeof(double);
			if (typeAssemblyQualifiedName.StartsWith("System.Int64,")) return typeof(long);
			if (typeAssemblyQualifiedName.StartsWith("System.String,")) return typeof(string);

			// Check for common Unity types
			if (typeAssemblyQualifiedName.StartsWith("UnityEngine.Vector2,")) return typeof(Vector2);
			if (typeAssemblyQualifiedName.StartsWith("UnityEngine.Vector3,")) return typeof(Vector3);
			if (typeAssemblyQualifiedName.StartsWith("UnityEngine.Vector4,")) return typeof(Vector4);
			if (typeAssemblyQualifiedName.StartsWith("UnityEngine.Quaternion,")) return typeof(Quaternion);
			if (typeAssemblyQualifiedName.StartsWith("UnityEngine.Color,")) return typeof(Color);
			if (typeAssemblyQualifiedName.StartsWith("UnityEngine.LayerMask,")) return typeof(LayerMask);

			return null;
		}
	}
}
