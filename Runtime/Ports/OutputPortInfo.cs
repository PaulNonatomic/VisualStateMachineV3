using System;
using Nonatomic.VSM.Utils;
using UnityEngine;

namespace Nonatomic.VSM.Ports
{
	/// <summary>
	/// Metadata for an output port discovered on a state.
	/// Output ports provide data that can be written to blackboard variables.
	/// </summary>
	[Serializable]
	public class OutputPortInfo : PortInfo
	{
		/// <summary>
		/// The direction of this port (always Output).
		/// </summary>
		public override PortDirection Direction => PortDirection.Output;

		/// <summary>
		/// Creates a new OutputPortInfo with default values.
		/// </summary>
		public OutputPortInfo()
		{
		}

		/// <summary>
		/// Creates a new OutputPortInfo with the specified field name and type.
		/// </summary>
		public OutputPortInfo(string fieldName, Type dataType)
		{
			FieldName = fieldName;
			DataType = dataType;
			DisplayName = StringFormatUtils.FormatFieldName(fieldName);
		}

		/// <summary>
		/// Creates a new OutputPortInfo with full configuration.
		/// </summary>
		public OutputPortInfo(string fieldName, Type dataType, string displayName, string tooltip = null)
		{
			FieldName = fieldName;
			DataType = dataType;
			DisplayName = displayName ?? StringFormatUtils.FormatFieldName(fieldName);
			Tooltip = tooltip;
		}
	}
}
