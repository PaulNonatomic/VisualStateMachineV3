using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nonatomic.VSM.Blackboard
{
	/// <summary>
	/// Handles serialization and deserialization for List&lt;T&gt; types in the blackboard.
	/// Delegates element serialization to the registered handler for the element type.
	/// </summary>
	public class ListTypeHandler : IBlackboardTypeHandler
	{
		private readonly Type _elementType;
		private readonly Type _listType;
		private readonly string _displayName;
		private readonly string _category;
		private readonly int _order;

		public ListTypeHandler(Type elementType, string displayName = null, string category = null, int order = 0)
		{
			if (elementType == null) throw new ArgumentNullException(nameof(elementType));

			_elementType = elementType;
			_listType = typeof(List<>).MakeGenericType(elementType);
			_displayName = displayName ?? $"List<{elementType.Name}>";
			_category = category ?? "Collections";
			_order = order;
		}

		public Type HandledType => _listType;
		public string DisplayName => _displayName;
		public string Category => _category;
		public int Order => _order;

		/// <summary>
		/// The element type for this list handler.
		/// </summary>
		public Type ElementType => _elementType;

		public string Serialize(object value)
		{
			if (value == null) return null;

			var list = value as IList;
			if (list == null) return null;

			var wrapper = new SerializedListData();
			for (var i = 0; i < list.Count; i++)
			{
				var elementJson = BlackboardTypeRegistry.Serialize(list[i], _elementType);
				wrapper.elements.Add(elementJson ?? string.Empty);
			}

			return JsonUtility.ToJson(wrapper);
		}

		public object Deserialize(string json)
		{
			if (string.IsNullOrEmpty(json)) return GetDefaultValue();

			try
			{
				var wrapper = JsonUtility.FromJson<SerializedListData>(json);
				if (wrapper?.elements == null) return GetDefaultValue();

				var list = (IList)Activator.CreateInstance(_listType);
				foreach (var elementJson in wrapper.elements)
				{
					var element = BlackboardTypeRegistry.Deserialize(elementJson, _elementType);
					list.Add(element);
				}

				return list;
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[ListTypeHandler] Failed to deserialize List<{_elementType.Name}>: {e.Message}. Using default value.");
				return GetDefaultValue();
			}
		}

		public object GetDefaultValue()
		{
			return Activator.CreateInstance(_listType);
		}

		[Serializable]
		private class SerializedListData
		{
			public List<string> elements = new List<string>();
		}
	}
}
