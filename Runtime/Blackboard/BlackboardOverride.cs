using System;
using System.Collections;
using System.Collections.Generic;
using Nonatomic.VSM.Utils;
using UnityEngine;

namespace Nonatomic.VSM.Blackboard
{
	/// <summary>
	/// Serializable override for a blackboard variable value.
	/// Used to customize default values per StateMachineRunner instance.
	/// </summary>
	[Serializable]
	public class BlackboardOverride
	{
		[SerializeField] private string _key;

		[SerializeField] private string _typeAssemblyQualifiedName;

		[SerializeField] private string _jsonValue;

		[SerializeField] private UnityEngine.Object _objectReference;

		[SerializeField] private List<UnityEngine.Object> _objectReferences = new List<UnityEngine.Object>();

		[NonSerialized] private Type _cachedType;

		[NonSerialized] private object _cachedValue;

		[NonSerialized] private bool _valueDeserialized;

		public string Key
		{
			get => _key;
			set => _key = value;
		}

		public string TypeAssemblyQualifiedName
		{
			get => _typeAssemblyQualifiedName;
			set
			{
				_typeAssemblyQualifiedName = value;
				_cachedType = null;
			}
		}

		public Type ValueType
		{
			get
			{
				if (_cachedType == null && !string.IsNullOrEmpty(_typeAssemblyQualifiedName))
				{
					_cachedType = TypeValidation.SafeGetType(_typeAssemblyQualifiedName);
				}

				return _cachedType;
			}
			set
			{
				_cachedType = value;
				_typeAssemblyQualifiedName = value?.AssemblyQualifiedName;
			}
		}

		public object Value
		{
			get
			{
				if (!_valueDeserialized)
				{
					_cachedValue = DeserializeValue();
					_valueDeserialized = true;
				}

				return _cachedValue;
			}
			set
			{
				_cachedValue = value;
				_valueDeserialized = true;
				SerializeValue(value);
			}
		}

		public string JsonValue
		{
			get => _jsonValue;
			set => _jsonValue = value;
		}

		public UnityEngine.Object ObjectReference
		{
			get => _objectReference;
			set => _objectReference = value;
		}

		public BlackboardOverride()
		{
		}

		public BlackboardOverride(string key, Type type, object value)
		{
			_key = key;
			ValueType = type;
			Value = value;
		}

		/// <summary>
		/// The list of object references for List types containing UnityEngine.Object elements.
		/// </summary>
		public List<UnityEngine.Object> ObjectReferences => _objectReferences;

		private void SerializeValue(object value)
		{
			var type = ValueType;
			if (type == null)
			{
				_jsonValue = null;
				_objectReference = null;
				_objectReferences.Clear();
				return;
			}

			// Handle List<T> where T : UnityEngine.Object
			var elementType = GetListElementType(type);
			if (elementType != null && typeof(UnityEngine.Object).IsAssignableFrom(elementType))
			{
				_objectReferences.Clear();
				_objectReference = null;
				_jsonValue = null;

				if (value is IList list)
				{
					foreach (var item in list)
					{
						_objectReferences.Add(item as UnityEngine.Object);
					}
				}
				return;
			}

			// Handle UnityEngine.Object references
			if (typeof(UnityEngine.Object).IsAssignableFrom(type))
			{
				_objectReference = value as UnityEngine.Object;
				_jsonValue = null;
				return;
			}

			_objectReference = null;

			if (value == null)
			{
				_jsonValue = null;
				return;
			}

			// Handle primitive types
			if (type == typeof(int) || type == typeof(float) || type == typeof(bool) ||
				type == typeof(double) || type == typeof(long))
			{
				_jsonValue = value.ToString();
				return;
			}

			if (type == typeof(string))
			{
				_jsonValue = value as string;
				return;
			}

			// Use JsonUtility for complex Unity types
			_jsonValue = JsonUtility.ToJson(value);
		}

		private object DeserializeValue()
		{
			var type = ValueType;
			if (type == null) return null;

			// Handle List<T> where T : UnityEngine.Object
			var elementType = GetListElementType(type);
			if (elementType != null && typeof(UnityEngine.Object).IsAssignableFrom(elementType))
			{
				var listType = typeof(List<>).MakeGenericType(elementType);
				var list = (IList)Activator.CreateInstance(listType);
				foreach (var obj in _objectReferences)
				{
					list.Add(obj);
				}
				return list;
			}

			// Handle UnityEngine.Object references
			if (typeof(UnityEngine.Object).IsAssignableFrom(type))
			{
				return _objectReference;
			}

			if (string.IsNullOrEmpty(_jsonValue)) return null;

			// Handle primitive types
			if (type == typeof(int)) return int.TryParse(_jsonValue, out var i) ? i : 0;
			if (type == typeof(float)) return float.TryParse(_jsonValue, out var f) ? f : 0f;
			if (type == typeof(bool)) return bool.TryParse(_jsonValue, out var b) && b;
			if (type == typeof(string)) return _jsonValue;
			if (type == typeof(double)) return double.TryParse(_jsonValue, out var d) ? d : 0.0;
			if (type == typeof(long)) return long.TryParse(_jsonValue, out var l) ? l : 0L;

			// Use JsonUtility for complex Unity types
			try
			{
				return JsonUtility.FromJson(_jsonValue, type);
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[BlackboardOverride] Failed to deserialize '{_key}' as {type.Name}: {ex.Message}");
				return null;
			}
		}

		/// <summary>
		/// Returns the element type if the given type is List&lt;T&gt;, otherwise null.
		/// </summary>
		private static Type GetListElementType(Type type)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
			{
				return type.GetGenericArguments()[0];
			}
			return null;
		}

		/// <summary>
		/// Resets the deserialization cache, forcing re-deserialization on next Value access.
		/// </summary>
		public void InvalidateCache()
		{
			_valueDeserialized = false;
			_cachedValue = null;
			_cachedType = null;
		}
	}
}
