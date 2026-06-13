using System;
using Nonatomic.VSM.Utils;
using UnityEngine;

namespace Nonatomic.VSM.Ports
{
	/// <summary>
	/// Metadata for a transition port discovered on a state.
	/// Transition ports are Action fields that trigger state changes when invoked.
	/// </summary>
	[Serializable]
	public class TransitionPortInfo : PortInfo
	{
		[SerializeField] private string _colorHint;

		[SerializeField] private int _priority;

		/// <summary>
		/// Color hint for the transition line in the editor (hex format).
		/// </summary>
		public string ColorHint
		{
			get => _colorHint;
			set => _colorHint = value;
		}

		/// <summary>
		/// Priority for ordering transitions. Lower values appear first.
		/// </summary>
		public int Priority
		{
			get => _priority;
			set => _priority = value;
		}

		/// <summary>
		/// The direction of this port (always Transition).
		/// </summary>
		public override PortDirection Direction => PortDirection.Transition;

		/// <summary>
		/// Creates a new TransitionPortInfo with default values.
		/// </summary>
		public TransitionPortInfo()
		{
		}

		/// <summary>
		/// Creates a new TransitionPortInfo with the specified field name.
		/// </summary>
		public TransitionPortInfo(string fieldName)
		{
			FieldName = fieldName;
			DataType = typeof(Action);
			DisplayName = StringFormatUtils.FormatTransitionName(fieldName);
		}

		/// <summary>
		/// Creates a new TransitionPortInfo with full configuration.
		/// </summary>
		public TransitionPortInfo(string fieldName, string displayName, string colorHint = null, string tooltip = null, int priority = 0)
		{
			FieldName = fieldName;
			DataType = typeof(Action);
			DisplayName = displayName ?? StringFormatUtils.FormatTransitionName(fieldName);
			ColorHint = colorHint;
			Tooltip = tooltip;
			Priority = priority;
		}

		/// <summary>
		/// Parses the color hint into a Unity Color.
		/// Returns white if no hint is set or parsing fails.
		/// </summary>
		public Color GetColor()
		{
			return TryGetColor(out var color) ? color : Color.white;
		}

		/// <summary>
		/// Attempts to parse the color hint into a Unity Color.
		/// Returns false when no hint is set or the hint is not a valid color,
		/// letting callers fall back to a default rather than treating an
		/// unset/invalid hint as an explicit white.
		/// </summary>
		public bool TryGetColor(out Color color)
		{
			if (!string.IsNullOrEmpty(_colorHint) && ColorUtility.TryParseHtmlString(_colorHint, out color))
			{
				return true;
			}

			color = Color.white;
			return false;
		}
	}
}
