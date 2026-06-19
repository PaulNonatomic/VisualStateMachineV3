using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Debugging
{
	public class BreakpointIndicator
	{
		private static readonly Color BreakpointColor = new Color(0.9f, 0.15f, 0.15f);
		private static readonly Color DisabledBreakpointColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
		private static readonly Color ConditionalColor = new Color(0.9f, 0.6f, 0.1f);

		private const float FlashDuration = 0.8f;

		private readonly VisualElement _dot;
		private bool _isVisible;
		private bool _isEnabled = true;
		private bool _isConditional;
		private bool _isFlashing;
		private double _flashStartTime;

		public bool IsVisible => _isVisible;

		public BreakpointIndicator(VisualElement container)
		{
			_dot = new VisualElement();
			_dot.style.position = Position.Absolute;
			_dot.style.top = 6;
			_dot.style.left = -5;
			_dot.style.width = 12;
			_dot.style.height = 12;
			_dot.style.borderTopLeftRadius = 6;
			_dot.style.borderTopRightRadius = 6;
			_dot.style.borderBottomLeftRadius = 6;
			_dot.style.borderBottomRightRadius = 6;
			_dot.style.backgroundColor = BreakpointColor;
			_dot.style.borderTopWidth = 2;
			_dot.style.borderBottomWidth = 2;
			_dot.style.borderLeftWidth = 2;
			_dot.style.borderRightWidth = 2;
			_dot.style.borderTopColor = Color.black;
			_dot.style.borderBottomColor = Color.black;
			_dot.style.borderLeftColor = Color.black;
			_dot.style.borderRightColor = Color.black;
			_dot.style.display = DisplayStyle.None;
			_dot.pickingMode = PickingMode.Ignore;

			container.Add(_dot);
		}

		public void Show(bool enabled, bool conditional)
		{
			_isVisible = true;
			_isEnabled = enabled;
			_isConditional = conditional;
			_dot.style.display = DisplayStyle.Flex;
			_dot.style.opacity = 1f;
			UpdateColor();
		}

		public void Hide()
		{
			_isVisible = false;
			_dot.style.display = DisplayStyle.None;
			_isFlashing = false;
			EditorApplication.update -= UpdateFlash;
		}

		/// <summary>
		/// Positions the dot centred on the given point, in its container's local space. Used by
		/// edge indicators, which sit at the transition midpoint rather than a fixed node corner.
		/// </summary>
		public void SetCenter(float centerX, float centerY)
		{
			_dot.style.left = centerX - 6f;
			_dot.style.top = centerY - 6f;
		}

		/// <summary>
		/// Briefly pulses the indicator (brightening toward white) to draw attention when this
		/// node's breakpoint is hit. No-op if the indicator is not currently shown.
		/// </summary>
		public void Flash()
		{
			if (!_isVisible) return;

			_isFlashing = true;
			_flashStartTime = EditorApplication.timeSinceStartup;
			EditorApplication.update -= UpdateFlash;
			EditorApplication.update += UpdateFlash;
		}

		private void UpdateFlash()
		{
			if (!_isFlashing || !_isVisible)
			{
				StopFlash();
				return;
			}

			var elapsed = (float)(EditorApplication.timeSinceStartup - _flashStartTime);
			if (elapsed >= FlashDuration)
			{
				StopFlash();
				return;
			}

			// Three quick pulses over the duration: brighten toward white and stay opaque at peaks.
			var pulse = Mathf.Abs(Mathf.Sin(elapsed / FlashDuration * Mathf.PI * 3f));
			_dot.style.backgroundColor = Color.Lerp(BreakpointColor, Color.white, pulse);
			_dot.style.opacity = 0.45f + 0.55f * pulse;
		}

		private void StopFlash()
		{
			_isFlashing = false;
			EditorApplication.update -= UpdateFlash;
			_dot.style.opacity = 1f;
			UpdateColor();
		}

		private void UpdateColor()
		{
			if (!_isEnabled)
			{
				_dot.style.backgroundColor = DisabledBreakpointColor;
			}
			else if (_isConditional)
			{
				_dot.style.backgroundColor = ConditionalColor;
			}
			else
			{
				_dot.style.backgroundColor = BreakpointColor;
			}
		}
	}
}
