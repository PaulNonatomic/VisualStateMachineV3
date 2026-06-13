using System;

namespace Nonatomic.VSM.Attributes
{
	/// <summary>
	/// Customizes the visual appearance of a state node in the graph editor.
	/// </summary>
	/// <example>
	/// <code>
	/// [StateStyle(Color = "#4A90D9", Icon = "d_AnimationClip Icon")]
	/// public class PatrolState : State { }
	///
	/// [StateStyle(Color = StateColors.Red)]
	/// public class AttackState : State { }
	/// </code>
	/// </example>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class StateStyleAttribute : Attribute
	{
		/// <summary>
		/// The color of the state node header/border.
		/// Accepts hex color strings (e.g., "#4A90D9") or named colors.
		/// </summary>
		public string Color { get; set; }

		/// <summary>
		/// The icon to display in the state node header.
		/// Can be a Unity built-in icon name (e.g., "d_AnimationClip Icon", "d_Prefab Icon")
		/// or a path to a custom icon asset.
		/// </summary>
		public string Icon { get; set; }

		/// <summary>
		/// Creates a StateStyle attribute with default values.
		/// </summary>
		public StateStyleAttribute()
		{
		}

		/// <summary>
		/// Creates a StateStyle attribute with a color.
		/// </summary>
		/// <param name="color">Hex color string (e.g., "#4A90D9")</param>
		public StateStyleAttribute(string color)
		{
			Color = color;
		}

		/// <summary>
		/// Creates a StateStyle attribute with color and icon.
		/// </summary>
		/// <param name="color">Hex color string (e.g., "#4A90D9")</param>
		/// <param name="icon">Unity icon name or asset path</param>
		public StateStyleAttribute(string color, string icon)
		{
			Color = color;
			Icon = icon;
		}
	}

	/// <summary>
	/// Predefined color constants for state node styling.
	/// Use with StateStyleAttribute for consistent coloring across your project.
	/// Matches the VSM2 color palette for visual consistency.
	/// </summary>
	/// <example>
	/// <code>
	/// [StateStyle(Color = StateColors.Red)]
	/// public class AttackState : State { }
	/// </code>
	/// </example>
	public static class StateColors
	{
		public const string Grey = "#444444";
		public const string Red = "#990e23";
		public const string Orange = "#B06101";
		public const string LimeGreen = "#6d9111";
		public const string Green = "#116f1c";
		public const string ForestGreen = "#08704a";
		public const string Teal = "#066670";
		public const string LightBlue = "#037091";
		public const string Blue = "#084870";
		public const string Purple = "#4a0e99";
		public const string Violet = "#740e99";
		public const string Pink = "#750b55";
		public const string Cantera = "#551C25";
		public const string Dijon = "#a89d46";
		public const string Black = "#000000";
		public const string White = "#ffffff";

		// Aliases for compatibility
		public const string Gray = Grey;
		public const string Yellow = Dijon;
		public const string Cyan = Teal;
		public const string Indigo = Purple;
		public const string Brown = Cantera;
	}
}
