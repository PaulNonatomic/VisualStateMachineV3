using System;
using Nonatomic.VSM.Utils;
using UnityEngine;

namespace Nonatomic.VSM.Ports
{
	/// <summary>
	/// Metadata for an input port discovered on a state.
	/// Input ports receive data from blackboard variables or other sources.
	/// </summary>
	[Serializable]
	public class InputPortInfo : PortInfo
	{
		[SerializeField] private bool _isRequired;

		[SerializeField] private string _defaultValueJson;

		[NonSerialized] private object _defaultValue;

		/// <summary>
		/// Whether this input is required for the state to function.
		/// </summary>
		public bool IsRequired
		{
			get => _isRequired;
			set => _isRequired = value;
		}

		/// <summary>
		/// JSON representation of the default value for serialization.
		/// </summary>
		public string DefaultValueJson
		{
			get => _defaultValueJson;
			set => _defaultValueJson = value;
		}

		/// <summary>
		/// The default value if no connection is made.
		/// </summary>
		public object DefaultValue
		{
			get => _defaultValue;
			set => _defaultValue = value;
		}

		/// <summary>
		/// The direction of this port (always Input).
		/// </summary>
		public override PortDirection Direction => PortDirection.Input;

		/// <summary>
		/// Creates a new InputPortInfo with default values.
		/// </summary>
		public InputPortInfo()
		{
		}

		/// <summary>
		/// Creates a new InputPortInfo with the specified field name and type.
		/// </summary>
		public InputPortInfo(string fieldName, Type dataType)
		{
			FieldName = fieldName;
			DataType = dataType;
			DisplayName = StringFormatUtils.FormatFieldName(fieldName);
		}

		/// <summary>
		/// Creates a new InputPortInfo with full configuration.
		/// </summary>
		public InputPortInfo(string fieldName, Type dataType, string displayName, bool isRequired, string tooltip = null)
		{
			FieldName = fieldName;
			DataType = dataType;
			DisplayName = displayName ?? StringFormatUtils.FormatFieldName(fieldName);
			IsRequired = isRequired;
			Tooltip = tooltip;
		}
	}
}
