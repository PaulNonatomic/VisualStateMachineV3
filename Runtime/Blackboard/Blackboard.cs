using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nonatomic.VSM.Blackboard
{
	/// <summary>
	/// Central data storage for state machine variables.
	/// Supports typed access, change notifications, parent chaining, and serialization.
	/// </summary>
	[Serializable]
	public class Blackboard : ISerializationCallbackReceiver
	{
		/// <summary>
		/// Fired when any variable changes. Args: (key, oldValue, newValue)
		/// </summary>
		public event Action<string, object, object> OnVariableChanged;

		/// <summary>
		/// Fired when a variable is added. Args: (key, value)
		/// </summary>
		public event Action<string, object> OnVariableAdded;

		/// <summary>
		/// Fired when a variable is removed. Args: (key)
		/// </summary>
		public event Action<string> OnVariableRemoved;

		[SerializeField] private List<BlackboardVariableData> _serializedVariables = new List<BlackboardVariableData>();

		// Runtime storage
		private readonly Dictionary<string, BlackboardVariable> _variables = new Dictionary<string, BlackboardVariable>();

		// Parent blackboard for hierarchical lookups (sub-state machines)
		private Blackboard _parent;

		/// <summary>
		/// Parent blackboard for hierarchical access.
		/// Child blackboards can read from parent if a key is not found locally.
		/// </summary>
		public Blackboard Parent => _parent;

		/// <summary>
		/// All variable keys in this blackboard (not including parent).
		/// </summary>
		public IEnumerable<string> Keys => _variables.Keys;

		/// <summary>
		/// Number of variables in this blackboard (not including parent).
		/// </summary>
		public int Count => _variables.Count;

		/// <summary>
		/// Sets the parent blackboard for hierarchical lookups.
		/// </summary>
		public void SetParent(Blackboard parent)
		{
			_parent = parent;
		}


		/// <summary>
		/// Gets a typed value from the blackboard.
		/// Falls back to parent blackboard if not found locally.
		/// </summary>
		public T Get<T>(string key)
		{
			if (TryGet<T>(key, out var value))
			{
				return value;
			}

			throw new KeyNotFoundException($"Blackboard key '{key}' not found");
		}

		/// <summary>
		/// Gets a typed value with a default fallback.
		/// </summary>
		public T Get<T>(string key, T defaultValue)
		{
			if (TryGet<T>(key, out var value))
			{
				return value;
			}

			return defaultValue;
		}

		/// <summary>
		/// Tries to get a typed value from the blackboard.
		/// Falls back to parent blackboard if not found locally.
		/// </summary>
		public bool TryGet<T>(string key, out T value)
		{
			if (_variables.TryGetValue(key, out var variable))
			{
				if (variable.TryGetValue<T>(out value))
				{
					return true;
				}
			}

			// Check parent blackboard
			if (_parent != null && _parent.TryGet<T>(key, out value))
			{
				return true;
			}

			value = default;
			return false;
		}

		/// <summary>
		/// Sets a value in the blackboard. Creates the variable if it doesn't exist.
		/// </summary>
		public void Set<T>(string key, T value)
		{
			if (_variables.TryGetValue(key, out var existing))
			{
				if (existing is BlackboardVariable<T> typed)
				{
					if (typed.ValueEquals(value)) return;

					var oldValue = typed.Value;
					typed.SetValue(value);
					OnVariableChanged?.Invoke(key, oldValue, value);
				}
				else
				{
					var oldValue = existing.BoxedValue;
					if (Equals(oldValue, value)) return;

					existing.SetBoxedValue(value);
					OnVariableChanged?.Invoke(key, oldValue, value);
				}
			}
			else
			{
				var variable = new BlackboardVariable<T>(key, value);
				_variables[key] = variable;
				OnVariableAdded?.Invoke(key, value);
			}
		}

		/// <summary>
		/// Sets a boxed value (for use when type is not known at compile time).
		/// </summary>
		public void SetBoxed(string key, object value, Type type)
		{
			if (_variables.TryGetValue(key, out var existing))
			{
				var oldValue = existing.BoxedValue;
				if (Equals(oldValue, value)) return;

				existing.SetBoxedValue(value);
				OnVariableChanged?.Invoke(key, oldValue, value);
			}
			else
			{
				var variable = BlackboardVariable.Create(key, type, value);
				_variables[key] = variable;
				OnVariableAdded?.Invoke(key, value);
			}
		}

		/// <summary>
		/// Gets the boxed value without type constraints.
		/// </summary>
		public object GetBoxed(string key)
		{
			if (_variables.TryGetValue(key, out var variable))
			{
				return variable.BoxedValue;
			}

			if (_parent != null)
			{
				return _parent.GetBoxed(key);
			}

			return null;
		}



		/// <summary>
		/// Checks if a variable exists (locally or in parent).
		/// </summary>
		public bool HasVariable(string key)
		{
			if (_variables.ContainsKey(key))
			{
				return true;
			}

			return _parent?.HasVariable(key) ?? false;
		}

		/// <summary>
		/// Tries to get the boxed value for a key in a single dictionary lookup.
		/// </summary>
		public bool TryGetBoxed(string key, out object value)
		{
			if (_variables.TryGetValue(key, out var variable))
			{
				value = variable.BoxedValue;
				return true;
			}

			if (_parent != null)
			{
				return _parent.TryGetBoxed(key, out value);
			}

			value = null;
			return false;
		}

		/// <summary>
		/// Checks if a key exists (alias for HasVariable).
		/// </summary>
		public bool HasKey(string key) => HasVariable(key);

		/// <summary>
		/// Gets all variable keys (alias for Keys property).
		/// </summary>
		public IEnumerable<string> GetAllKeys() => Keys;

		/// <summary>
		/// Gets the value type for a key (alias for GetVariableType).
		/// </summary>
		public Type GetValueType(string key) => GetVariableType(key);

		/// <summary>
		/// Checks if a variable exists locally (not checking parent).
		/// </summary>
		public bool HasLocalVariable(string key)
		{
			return _variables.ContainsKey(key);
		}

		/// <summary>
		/// Gets the variable wrapper for direct access.
		/// </summary>
		public BlackboardVariable GetVariable(string key)
		{
			if (_variables.TryGetValue(key, out var variable))
			{
				return variable;
			}

			return _parent?.GetVariable(key);
		}

		/// <summary>
		/// Gets the variable type for the specified key.
		/// </summary>
		public Type GetVariableType(string key)
		{
			var variable = GetVariable(key);
			return variable?.ValueType;
		}

		/// <summary>
		/// Removes a variable from the blackboard.
		/// </summary>
		public bool Remove(string key)
		{
			if (_variables.Remove(key))
			{
				OnVariableRemoved?.Invoke(key);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Clears all local variables.
		/// </summary>
		public void Clear()
		{
			if (OnVariableRemoved == null)
			{
				_variables.Clear();
				return;
			}

			var keys = new List<string>(_variables.Keys);
			_variables.Clear();

			foreach (var key in keys)
			{
				OnVariableRemoved.Invoke(key);
			}
		}

		/// <summary>
		/// Resets all variables to their default values from the provided definitions.
		/// Does not fire change events. Intended for pool reset operations.
		/// </summary>
		public void ResetToDefaults(IEnumerable<BlackboardVariableDefinition> definitions)
		{
			_variables.Clear();

			foreach (var definition in definitions)
			{
				if (definition == null || string.IsNullOrEmpty(definition.Key)) continue;

				definition.RestoreType();
				if (definition.ValueType == null) continue;

				var variable = BlackboardVariable.Create(
					definition.Key,
					definition.ValueType,
					definition.DefaultValue
				);

				variable.IsExposed = definition.IsExposed;
				variable.IsExposedInGraph = definition.IsExposedInGraph;
				variable.Category = definition.Category;
				variable.Tooltip = definition.Tooltip;

				_variables[definition.Key] = variable;
			}
		}

		/// <summary>
		/// Clears all event subscribers. Intended for pool reset operations.
		/// </summary>
		public void ClearSubscribers()
		{
			OnVariableChanged = null;
			OnVariableAdded = null;
			OnVariableRemoved = null;
		}

		/// <summary>
		/// Defines a variable with metadata (used at design-time and initialization).
		/// </summary>
		public void DefineVariable(BlackboardVariableDefinition definition)
		{
			if (definition == null || string.IsNullOrEmpty(definition.Key))
			{
				return;
			}

			if (_variables.ContainsKey(definition.Key))
			{
				return;
			}

			definition.RestoreType();

			if (definition.ValueType == null)
			{
				Debug.LogWarning($"Could not resolve type for blackboard variable '{definition.Key}'");
				return;
			}

			var variable = BlackboardVariable.Create(
				definition.Key,
				definition.ValueType,
				definition.DefaultValue
			);

			variable.IsExposed = definition.IsExposed;
			variable.IsExposedInGraph = definition.IsExposedInGraph;
			variable.Category = definition.Category;
			variable.Tooltip = definition.Tooltip;

			_variables[definition.Key] = variable;
		}

		/// <summary>
		/// Gets all variables (including parent variables, with local taking precedence).
		/// </summary>
		public IEnumerable<BlackboardVariable> GetAllVariables()
		{
			if (_parent == null) return _variables.Values;

			return GetAllVariablesWithParent();
		}

		private IEnumerable<BlackboardVariable> GetAllVariablesWithParent()
		{
			var seen = new HashSet<string>();

			foreach (var kvp in _variables)
			{
				seen.Add(kvp.Key);
				yield return kvp.Value;
			}

			foreach (var variable in _parent.GetAllVariables())
			{
				if (!seen.Contains(variable.Key))
				{
					yield return variable;
				}
			}
		}

		/// <summary>
		/// Gets all exposed variables for Inspector display.
		/// </summary>
		public IEnumerable<BlackboardVariable> GetExposedVariables()
		{
			foreach (var variable in GetAllVariables())
			{
				if (variable.IsExposed)
				{
					yield return variable;
				}
			}
		}



		/// <summary>
		/// Subscribes to changes for a specific variable.
		/// Returns an IDisposable for easy unsubscription.
		/// </summary>
		public IDisposable Subscribe<T>(string key, Action<T, T> onChange)
		{
			void Handler(string k, object oldVal, object newVal)
			{
				if (k == key)
				{
					var oldTyped = oldVal is T o ? o : default;
					var newTyped = newVal is T n ? n : default;
					onChange(oldTyped, newTyped);
				}
			}

			OnVariableChanged += Handler;
			return new SubscriptionHandle(() => OnVariableChanged -= Handler);
		}

		/// <summary>
		/// Subscribes to changes for any variable.
		/// </summary>
		public IDisposable SubscribeAll(Action<string, object, object> onChange)
		{
			OnVariableChanged += onChange;
			return new SubscriptionHandle(() => OnVariableChanged -= onChange);
		}

		private class SubscriptionHandle : IDisposable
		{
			private Action _unsubscribe;

			public SubscriptionHandle(Action unsubscribe)
			{
				_unsubscribe = unsubscribe;
			}

			public void Dispose()
			{
				_unsubscribe?.Invoke();
				_unsubscribe = null;
			}
		}



		public void OnBeforeSerialize()
		{
			_serializedVariables.Clear();

			foreach (var kvp in _variables)
			{
				_serializedVariables.Add(kvp.Value.ToSerializedData());
			}
		}

		public void OnAfterDeserialize()
		{
			_variables.Clear();

			foreach (var data in _serializedVariables)
			{
				var variable = BlackboardVariable.FromSerializedData(data);
				if (variable != null)
				{
					_variables[data.Key] = variable;
				}
			}
		}



		/// <summary>
		/// Creates a deep copy of this blackboard (without parent reference).
		/// </summary>
		public Blackboard Clone()
		{
			var clone = new Blackboard();

			foreach (var kvp in _variables)
			{
				var data = kvp.Value.ToSerializedData();
				var variable = BlackboardVariable.FromSerializedData(data);
				if (variable != null)
				{
					clone._variables[kvp.Key] = variable;
				}
			}

			return clone;
		}

		/// <summary>
		/// Copies all variables from another blackboard into this one.
		/// </summary>
		public void CopyFrom(Blackboard other)
		{
			if (other == null)
			{
				return;
			}

			foreach (var kvp in other._variables)
			{
				var data = kvp.Value.ToSerializedData();
				var variable = BlackboardVariable.FromSerializedData(data);
				if (variable != null)
				{
					_variables[kvp.Key] = variable;
				}
			}
		}

	}
}
