using System;

namespace Nonatomic.VSM.Attributes
{
	/// <summary>
	/// Provides documentation and informational metadata for a state node.
	/// This information is displayed in the graph editor to help designers
	/// understand the purpose and behavior of each state.
	/// </summary>
	/// <example>
	/// <code>
	/// [StateInfo(
	///     Tooltip = "Mobile combat mode",
	///     Description = "Tank can move freely and fire with reduced damage. " +
	///                   "Press E to enter Siege Mode for increased firepower."
	/// )]
	/// public class TankModeState : State&lt;SiegeTankContext&gt; { }
	/// </code>
	/// </example>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class StateInfoAttribute : Attribute
	{
		/// <summary>
		/// Short tooltip text shown when hovering over the state node.
		/// Keep this brief - one line recommended.
		/// </summary>
		public string Tooltip { get; set; }

		/// <summary>
		/// Longer description shown in the inspector or details panel.
		/// Can include multiple sentences explaining the state's behavior,
		/// requirements, and interactions with other states.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Optional category for organizing states in the "Add State" menu.
		/// Use forward slashes for nested categories (e.g., "Combat/Ranged").
		/// </summary>
		public string Category { get; set; }

		/// <summary>
		/// Optional URL to external documentation or wiki page.
		/// The editor can provide a button to open this link.
		/// </summary>
		public string HelpUrl { get; set; }

		/// <summary>
		/// Creates a StateInfo attribute with default values.
		/// </summary>
		public StateInfoAttribute()
		{
		}

		/// <summary>
		/// Creates a StateInfo attribute with a tooltip.
		/// </summary>
		/// <param name="tooltip">Short description shown on hover</param>
		public StateInfoAttribute(string tooltip)
		{
			Tooltip = tooltip;
		}

		/// <summary>
		/// Creates a StateInfo attribute with tooltip and description.
		/// </summary>
		/// <param name="tooltip">Short description shown on hover</param>
		/// <param name="description">Longer explanation for details panel</param>
		public StateInfoAttribute(string tooltip, string description)
		{
			Tooltip = tooltip;
			Description = description;
		}
	}
}
