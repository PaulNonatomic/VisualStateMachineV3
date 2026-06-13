using System;

namespace Nonatomic.VSM.Attributes
{
	/// <summary>
	/// Marks a field as an input port that can receive data from blackboard variables
	/// or other data sources in the visual graph.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class DataInAttribute : Attribute
	{
		/// <summary>
		/// Display name for the port in the graph editor.
		/// If null, the field name will be formatted and used.
		/// </summary>
		public string DisplayName { get; set; }

		/// <summary>
		/// Whether this input is required for the state to function. Required inputs are marked
		/// visually in the editor, and graph validation raises a warning when one has no value
		/// source (no incoming connection and no property override).
		/// </summary>
		public bool Required { get; set; }

		/// <summary>
		/// Tooltip shown when hovering over the port in the editor.
		/// </summary>
		public string Tooltip { get; set; }

		public DataInAttribute()
		{
		}

		public DataInAttribute(string displayName)
		{
			DisplayName = displayName;
		}

		public DataInAttribute(string displayName, bool required)
		{
			DisplayName = displayName;
			Required = required;
		}
	}
}
