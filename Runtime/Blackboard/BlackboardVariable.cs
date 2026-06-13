using System;
using System.Collections.Generic;
using Nonatomic.VSM.Utils;
using UnityEngine;

namespace Nonatomic.VSM.Blackboard
{
	/// <summary>
	/// Base class for blackboard variables. Provides type-erased access
	/// for scenarios where the concrete type is not known at compile time.
	/// </summary>
	[Serializable]
	public abstract class BlackboardVariable
	{
		[SerializeField] protected string _key;

		/// <summary>
		/// The unique key identifying this variable in the blackboard.
		/// </summary>
		public string Key
		{
			get => _key;
			set => _key = value;
		}

		/// <summary>
		/// The type of value stored in this variable.
		/// </summary>
		public abstract Type ValueType { get; }

		/// <summary>
		/// The current value as an object (boxed for value types).
		/// </summary>
		public abstract object BoxedValue { get; }

		/// <summary>
		/// Legacy exposure flag, kept in sync with <see cref="IsExposedInGraph"/>. The runner
		/// inspector and variant-override system consult IsExposedInGraph; prefer it.
		/// </summary>
		public bool IsExposed { get; set; }

		/// <summary>
		/// Whether this variable is exposed — surfaced in the StateMachineRunner inspector for
		/// per-instance overrides and available as a blackboard node in the graph.
		/// </summary>
		public bool IsExposedInGraph { get; set; }

		/// <summary>
		/// Category for grouping in the Inspector.
		/// </summary>
		public string Category { get; set; }

		/// <summary>
		/// Tooltip shown in the Inspector.
		/// </summary>
		public string Tooltip { get; set; }

		/// <summary>
		/// Order for display in the Inspector.
		/// </summary>
		public int Order { get; set; }

		/// <summary>
		/// Sets the value using a boxed object.
		/// </summary>
		public abstract void SetBoxedValue(object value);

		/// <summary>
		/// Attempts to get the value as a specific type.
		/// </summary>
		public abstract bool TryGetValue<T>(out T value);

		/// <summary>
		/// Converts this variable to serializable data.
		/// </summary>
		public abstract BlackboardVariableData ToSerializedData();

		/// <summary>
		/// Creates a new BlackboardVariable instance with the specified type.
		/// </summary>
		public static BlackboardVariable Create(string key, Type type, object value = null)
		{
			var genericType = typeof(BlackboardVariable<>).MakeGenericType(type);
			var variable = (BlackboardVariable)Activator.CreateInstance(genericType);
			variable.Key = key;

			if (value != null)
			{
				variable.SetBoxedValue(value);
			}

			return variable;
		}

		/// <summary>
		/// Reconstructs a variable from serialized data.
		/// </summary>
		public static BlackboardVariable FromSerializedData(BlackboardVariableData data)
		{
			if (data == null || string.IsNullOrEmpty(data.TypeAssemblyQualifiedName))
			{
				return null;
			}

			var type = TypeValidation.SafeGetType(data.TypeAssemblyQualifiedName);
			if (type == null)
			{
				Debug.LogError($"Could not resolve type: {data.TypeAssemblyQualifiedName}");
				return null;
			}

			var variable = Create(data.Key, type);
			variable.IsExposed = data.IsExposed;
			variable.Category = data.Category;
			variable.Tooltip = data.Tooltip;
			variable.Order = data.Order;

			if (!string.IsNullOrEmpty(data.JsonValue))
			{
				try
				{
					var value = DeserializeValue(data.JsonValue, type);
					if (value != null)
					{
						variable.SetBoxedValue(value);
					}
				}
				catch (Exception e)
				{
					Debug.LogWarning($"Failed to deserialize blackboard variable '{data.Key}': {e.Message}");
				}
			}

			return variable;
		}

		private static object DeserializeValue(string json, Type type)
		{
			return BlackboardTypeRegistry.Deserialize(json, type);
		}
	}

	/// <summary>
	/// Typed blackboard variable with compile-time type safety.
	/// </summary>
	[Serializable]
	public class BlackboardVariable<T> : BlackboardVariable
	{
		[SerializeField] private T _value;

		/// <summary>
		/// The type of value stored in this variable.
		/// </summary>
		public override Type ValueType => typeof(T);

		/// <summary>
		/// The current value as an object (boxed for value types).
		/// </summary>
		public override object BoxedValue => _value;

		/// <summary>
		/// The current typed value.
		/// </summary>
		public T Value
		{
			get => _value;
			set => _value = value;
		}

		public BlackboardVariable()
		{
		}

		public BlackboardVariable(string key)
		{
			_key = key;
			_value = default;
		}

		public BlackboardVariable(string key, T initialValue)
		{
			_key = key;
			_value = initialValue;
		}

		/// <summary>
		/// Compares the stored value to another value of the same type without boxing.
		/// </summary>
		public bool ValueEquals(T other)
		{
			return EqualityComparer<T>.Default.Equals(_value, other);
		}

		/// <summary>
		/// Sets the typed value directly.
		/// </summary>
		public void SetValue(T value)
		{
			_value = value;
		}

		/// <summary>
		/// Sets the value using a boxed object.
		/// </summary>
		public override void SetBoxedValue(object value)
		{
			if (value is T typed)
			{
				_value = typed;
			}
			else if (value == null)
			{
				_value = default;
			}
			else
			{
				try
				{
					_value = (T)Convert.ChangeType(value, typeof(T));
				}
				catch
				{
					Debug.LogWarning($"Failed to convert {value?.GetType().Name} to {typeof(T).Name}");
					_value = default;
				}
			}
		}

		/// <summary>
		/// Attempts to get the value as a specific type.
		/// </summary>
		public override bool TryGetValue<TOut>(out TOut value)
		{
			if (_value is TOut typed)
			{
				value = typed;
				return true;
			}

			value = default;
			return false;
		}

		/// <summary>
		/// Converts this variable to serializable data.
		/// </summary>
		public override BlackboardVariableData ToSerializedData()
		{
			return new BlackboardVariableData
			{
				Key = Key,
				TypeAssemblyQualifiedName = typeof(T).AssemblyQualifiedName,
				JsonValue = SerializeValue(_value),
				IsExposed = IsExposed,
				Category = Category,
				Tooltip = Tooltip,
				Order = Order
			};
		}

		private static string SerializeValue(T value)
		{
			return BlackboardTypeRegistry.Serialize(value, typeof(T));
		}
	}
}
