using System;

namespace Nonatomic.VSM.Blackboard
{
	/// <summary>
	/// Handles blackboard registration for UnityEngine.Object subclasses.
	/// Serialization of object references is handled separately by BlackboardVariableDefinition
	/// via the _defaultObjectReference field, so this handler returns null for serialize/deserialize.
	/// </summary>
	public class UnityObjectTypeHandler : IBlackboardTypeHandler
	{
		private readonly Type _objectType;
		private readonly string _displayName;
		private readonly string _category;
		private readonly int _order;

		public UnityObjectTypeHandler(Type objectType, string displayName = null, string category = null, int order = 0)
		{
			if (objectType == null) throw new ArgumentNullException(nameof(objectType));

			_objectType = objectType;
			_displayName = displayName ?? objectType.Name;
			_category = category ?? "Custom";
			_order = order;
		}

		public Type HandledType => _objectType;
		public string DisplayName => _displayName;
		public string Category => _category;
		public int Order => _order;

		public string Serialize(object value)
		{
			return null;
		}

		public object Deserialize(string json)
		{
			return null;
		}

		public object GetDefaultValue()
		{
			return null;
		}
	}
}
