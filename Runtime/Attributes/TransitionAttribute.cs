using System;

namespace Nonatomic.VSM.Attributes
{
	/// <summary>
	/// Marks an Action field as a transition output port.
	/// When the action is invoked, it triggers a state transition to the connected state.
	/// The visual graph shows these as flow output ports.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class TransitionAttribute : Attribute
	{
		/// <summary>
		/// Display name for the transition port in the graph editor.
		/// If null, the field name will be formatted and used.
		/// </summary>
		public string DisplayName { get; set; }

		/// <summary>
		/// Tooltip describing when this transition fires.
		/// </summary>
		public string Tooltip { get; set; }

		/// <summary>
		/// Optional color hint for the transition line in the editor (hex format: "#FF0000").
		/// </summary>
		public string Color { get; set; }

		/// <summary>
		/// Optional priority for ordering multiple transitions from the same state.
		/// Lower values appear first.
		/// </summary>
		public int Priority { get; set; }

		public TransitionAttribute()
		{
		}

		public TransitionAttribute(string displayName)
		{
			DisplayName = displayName;
		}

		public TransitionAttribute(string displayName, string color)
		{
			DisplayName = displayName;
			Color = color;
		}
	}
}
