using System;
using Nonatomic.VSM.Utils;
using UnityEngine;

namespace Nonatomic.VSM.Blackboard
{
	/// <summary>
	/// Serializable data container for blackboard variables.
	/// Used for Unity serialization and asset storage.
	/// </summary>
	[Serializable]
	public class BlackboardVariableData
	{
		[SerializeField] private string _key;

		[SerializeField] private string _typeAssemblyQualifiedName;

		[SerializeField] private string _jsonValue;

		[SerializeField] private bool _isExposed;

		[SerializeField] private string _category;

		[SerializeField] private string _tooltip;

		[SerializeField] private int _order;

		public string Key
		{
			get => _key;
			set => _key = value;
		}

		public string TypeAssemblyQualifiedName
		{
			get => _typeAssemblyQualifiedName;
			set => _typeAssemblyQualifiedName = value;
		}

		public string JsonValue
		{
			get => _jsonValue;
			set => _jsonValue = value;
		}

		public bool IsExposed
		{
			get => _isExposed;
			set => _isExposed = value;
		}

		public string Category
		{
			get => _category;
			set => _category = value;
		}

		public string Tooltip
		{
			get => _tooltip;
			set => _tooltip = value;
		}

		public int Order
		{
			get => _order;
			set => _order = value;
		}
	}

	/// <summary>
	/// Definition used when creating variables at design-time.
	/// Contains type information that can be resolved at runtime.
	/// </summary>
	[Serializable]
	public class BlackboardVariableDefinition
	{
		[SerializeField] private string _key;

		[SerializeField] private string _typeAssemblyQualifiedName;

		[SerializeField] private string _defaultValueJson;

		[SerializeField] private UnityEngine.Object _defaultObjectReference;

		[SerializeField] private bool _isExposed;

		[SerializeField] private bool _isExposedInGraph;

		[SerializeField] private ExposureDirection _exposureDirection;

		[SerializeField] private string _category;

		[SerializeField] private string _tooltip;

		[SerializeField] private Vector2 _graphPosition;

		[NonSerialized] private Type _valueType;

		[NonSerialized] private object _cachedDefaultValue;

		[NonSerialized] private bool _defaultValueDeserialized;

		public string Key
		{
			get => _key;
			set => _key = value;
		}

		public string TypeAssemblyQualifiedName
		{
			get => _typeAssemblyQualifiedName;
			set => _typeAssemblyQualifiedName = value;
		}

		public Type ValueType
		{
			get
			{
				if (_valueType == null && !string.IsNullOrEmpty(_typeAssemblyQualifiedName))
				{
					_valueType = TypeValidation.SafeGetType(_typeAssemblyQualifiedName);
				}

				return _valueType;
			}
			set
			{
				_valueType = value;
				_typeAssemblyQualifiedName = value?.AssemblyQualifiedName;
			}
		}

		public string DefaultValueJson
		{
			get => _defaultValueJson;
			set => _defaultValueJson = value;
		}

		public object DefaultValue
		{
			get
			{
				if (!_defaultValueDeserialized)
				{
					_cachedDefaultValue = DeserializeDefaultValue();
					_defaultValueDeserialized = true;
				}

				return _cachedDefaultValue;
			}
			set
			{
				_cachedDefaultValue = value;
				_defaultValueDeserialized = true;
				SerializeDefaultValue(value);
			}
		}

		public bool IsExposed
		{
			get => _isExposed;
			set => _isExposed = value;
		}

		public bool IsExposedInGraph
		{
			get => _isExposedInGraph;
			set => _isExposedInGraph = value;
		}

		public ExposureDirection ExposureDirection
		{
			get => _exposureDirection;
			set => _exposureDirection = value;
		}

		public string Category
		{
			get => _category;
			set => _category = value;
		}

		public string Tooltip
		{
			get => _tooltip;
			set => _tooltip = value;
		}

		public Vector2 GraphPosition
		{
			get => _graphPosition;
			set => _graphPosition = value;
		}

		/// <summary>
		/// Caches the type name for serialization.
		/// </summary>
		public void CacheTypeName()
		{
			if (_valueType != null)
			{
				_typeAssemblyQualifiedName = _valueType.AssemblyQualifiedName;
			}
		}

		/// <summary>
		/// Restores the type from the cached name after deserialization.
		/// </summary>
		public void RestoreType()
		{
			if (!string.IsNullOrEmpty(_typeAssemblyQualifiedName))
			{
				_valueType = TypeValidation.SafeGetType(_typeAssemblyQualifiedName);
			}
		}

		private void SerializeDefaultValue(object value)
		{
			var type = ValueType;
			if (type == null)
			{
				_defaultValueJson = null;
				_defaultObjectReference = null;
				return;
			}

			// Handle UnityEngine.Object references separately
			if (typeof(UnityEngine.Object).IsAssignableFrom(type))
			{
				_defaultObjectReference = value as UnityEngine.Object;
				_defaultValueJson = null;
				return;
			}

			_defaultObjectReference = null;

			if (value == null)
			{
				_defaultValueJson = null;
				return;
			}

			// Use the centralized registry for serialization
			_defaultValueJson = BlackboardTypeRegistry.Serialize(value, type);
		}

		private object DeserializeDefaultValue()
		{
			var type = ValueType;
			if (type == null) return null;

			// Handle UnityEngine.Object references
			if (typeof(UnityEngine.Object).IsAssignableFrom(type))
			{
				return _defaultObjectReference;
			}

			if (string.IsNullOrEmpty(_defaultValueJson)) return null;

			// Use the centralized registry for deserialization
			return BlackboardTypeRegistry.Deserialize(_defaultValueJson, type);
		}
	}
}
