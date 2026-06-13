using System;
using UnityEngine;

namespace Nonatomic.VSM.Blackboard
{
	/// <summary>
	/// Handles serialization and deserialization for enum types in the blackboard.
	/// Uses Enum.GetName/Enum.Parse for human-readable, rename-resilient storage.
	/// </summary>
	public class EnumTypeHandler : IBlackboardTypeHandler
	{
		private readonly Type _enumType;
		private readonly string _displayName;
		private readonly string _category;
		private readonly int _order;

		public EnumTypeHandler(Type enumType, string displayName = null, string category = null, int order = 0)
		{
			if (enumType == null) throw new ArgumentNullException(nameof(enumType));
			if (!enumType.IsEnum) throw new ArgumentException($"Type '{enumType.Name}' is not an enum.", nameof(enumType));

			_enumType = enumType;
			_displayName = displayName ?? enumType.Name;
			_category = category ?? "Enums";
			_order = order;
		}

		public Type HandledType => _enumType;
		public string DisplayName => _displayName;
		public string Category => _category;
		public int Order => _order;

		public string Serialize(object value)
		{
			if (value == null) return null;
			return Enum.GetName(_enumType, value);
		}

		public object Deserialize(string json)
		{
			if (string.IsNullOrEmpty(json)) return GetDefaultValue();

			try
			{
				return Enum.Parse(_enumType, json);
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[EnumTypeHandler] Failed to deserialize '{json}' as {_enumType.Name}: {e.Message}. Using default value.");
				return GetDefaultValue();
			}
		}

		public object GetDefaultValue()
		{
			var values = Enum.GetValues(_enumType);
			return values.Length > 0 ? values.GetValue(0) : Activator.CreateInstance(_enumType);
		}
	}
}
