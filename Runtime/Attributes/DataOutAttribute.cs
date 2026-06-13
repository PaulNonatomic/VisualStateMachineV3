using System;

namespace Nonatomic.VSM.Attributes
{
	/// <summary>
	/// Marks a field as an output port that provides data to connected nodes.
	/// Output ports can be connected to input ports on other states or to
	/// blackboard variable nodes for writing values.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class DataOutAttribute : Attribute
	{
		/// <summary>
		/// Display name for the port in the graph editor.
		/// If null, the field name will be formatted and used.
		/// </summary>
		public string DisplayName { get; set; }

		/// <summary>
		/// Tooltip shown when hovering over the port in the editor.
		/// </summary>
		public string Tooltip { get; set; }

		public DataOutAttribute()
		{
		}

		public DataOutAttribute(string displayName)
		{
			DisplayName = displayName;
		}
	}
}
