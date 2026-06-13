using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Helper class that manages active state visual indicators for graph nodes.
	/// Provides border highlighting and optional pulsing dot animation.
	/// </summary>
	public class ActiveStateIndicator
	{
		private static readonly Color ActiveColor = new Color(0.2f, 0.8f, 0.2f);

		private readonly VisualElement _borderIndicator;
		private readonly VisualElement _pulsingDot;
		private readonly VisualElement _ownerNode;
		private readonly bool _hasPulsingDot;
		private readonly bool _persistOnDeactivate;

		private bool _isActive;
		private bool _isFlashing;
		private float _flashAlpha;
		private double _lastUpdateTime;
		private float _pulsePhase;

		/// <summary>
		/// Whether this node is currently in the active state.
		/// </summary>
		public bool IsActive => _isActive;

		/// <summary>
		/// Creates a new ActiveStateIndicator.
		/// </summary>
		/// <param name="ownerNode">The node to add indicators to.</param>
		/// <param name="borderContainer">The container for the border indicator (usually mainContainer).</param>
		/// <param name="dotContainer">The container for the pulsing dot (usually titleContainer). Pass null to disable pulsing dot.</param>
		/// <param name="borderRadius">Optional border radius for the indicator.</param>
		/// <param name="persistOnDeactivate">If true, the indicator won't hide when SetActiveState(false) is called.</param>
		public ActiveStateIndicator(
			VisualElement ownerNode,
			VisualElement borderContainer,
			VisualElement dotContainer = null,
			float borderRadius = 0f,
			bool persistOnDeactivate = false)
		{
			_ownerNode = ownerNode;
			_hasPulsingDot = dotContainer != null;
			_persistOnDeactivate = persistOnDeactivate;

			// Create border indicator
			_borderIndicator = new VisualElement();
			_borderIndicator.AddToClassList("active-indicator");
			_borderIndicator.style.display = DisplayStyle.None;
			_borderIndicator.style.position = Position.Absolute;
			_borderIndicator.style.top = 0;
			_borderIndicator.style.left = 0;
			_borderIndicator.style.right = 0;
			_borderIndicator.style.bottom = 0;
			_borderIndicator.style.borderTopWidth = 3;
			_borderIndicator.style.borderBottomWidth = 3;
			_borderIndicator.style.borderLeftWidth = 3;
			_borderIndicator.style.borderRightWidth = 3;
			SetBorderColor(ActiveColor);
			_borderIndicator.pickingMode = PickingMode.Ignore;

			if (borderRadius > 0)
			{
				_borderIndicator.style.borderTopLeftRadius = borderRadius;
				_borderIndicator.style.borderTopRightRadius = borderRadius;
				_borderIndicator.style.borderBottomLeftRadius = borderRadius;
				_borderIndicator.style.borderBottomRightRadius = borderRadius;
			}

			borderContainer.Add(_borderIndicator);

			// Create pulsing dot if container provided
			if (_hasPulsingDot)
			{
				_pulsingDot = new VisualElement();
				_pulsingDot.AddToClassList("pulsing-dot");
				_pulsingDot.style.display = DisplayStyle.None;
				_pulsingDot.style.position = Position.Absolute;
				_pulsingDot.style.top = 6;
				_pulsingDot.style.right = 6;
				_pulsingDot.style.width = 10;
				_pulsingDot.style.height = 10;
				_pulsingDot.style.borderTopLeftRadius = 5;
				_pulsingDot.style.borderTopRightRadius = 5;
				_pulsingDot.style.borderBottomLeftRadius = 5;
				_pulsingDot.style.borderBottomRightRadius = 5;
				_pulsingDot.style.backgroundColor = ActiveColor;
				_pulsingDot.pickingMode = PickingMode.Ignore;
				dotContainer.Add(_pulsingDot);
			}
		}

		/// <summary>
		/// Sets whether this node is currently the active state.
		/// </summary>
		public void SetActiveState(bool isActive)
		{
			// Handle persist mode - don't deactivate unless forced
			if (_persistOnDeactivate && !isActive && _isActive)
			{
				return;
			}

			if (_isActive == isActive) return;

			_isActive = isActive;

			if (isActive)
			{
				// Stop any fade-out animation
				_isFlashing = false;
				EditorApplication.update -= UpdateFlashAnimation;

				// Show immediately at full opacity
				_borderIndicator.style.display = DisplayStyle.Flex;
				_borderIndicator.style.opacity = 1f;
				SetBorderColor(ActiveColor);

				if (_hasPulsingDot)
				{
					_pulsingDot.style.display = DisplayStyle.Flex;
					_pulsingDot.style.opacity = 1f;
				}

				_ownerNode.AddToClassList("state-active");
				_pulsePhase = 0f;
				_lastUpdateTime = EditorApplication.timeSinceStartup;
				EditorApplication.update += UpdatePulseAnimation;
			}
			else
			{
				_ownerNode.RemoveFromClassList("state-active");
				EditorApplication.update -= UpdatePulseAnimation;

				// Hide immediately instead of fading to ensure proper visual update
				// even when EditorApplication.update isn't running frequently
				_isFlashing = false;
				_borderIndicator.style.display = DisplayStyle.None;
				_borderIndicator.style.opacity = 0f;
				if (_hasPulsingDot)
				{
					_pulsingDot.style.display = DisplayStyle.None;
					_pulsingDot.style.opacity = 0f;
				}
			}
		}

		/// <summary>
		/// Forces deactivation, ignoring the persistOnDeactivate setting.
		/// </summary>
		public void ForceDeactivate()
		{
			_isActive = false;
			_isFlashing = false;
			EditorApplication.update -= UpdatePulseAnimation;
			EditorApplication.update -= UpdateFlashAnimation;
			_borderIndicator.style.display = DisplayStyle.None;
			if (_hasPulsingDot)
			{
				_pulsingDot.style.display = DisplayStyle.None;
			}
			_ownerNode.RemoveFromClassList("state-active");
		}

		/// <summary>
		/// Briefly flashes the active indicator for transient highlighting.
		/// </summary>
		public void FlashHighlight()
		{
			_isFlashing = true;
			_flashAlpha = 1f;
			_borderIndicator.style.display = DisplayStyle.Flex;
			if (_hasPulsingDot)
			{
				_pulsingDot.style.display = DisplayStyle.Flex;
			}
			_lastUpdateTime = EditorApplication.timeSinceStartup;

			EditorApplication.update -= UpdateFlashAnimation;
			EditorApplication.update += UpdateFlashAnimation;
		}

		private void UpdatePulseAnimation()
		{
			if (!_isActive)
			{
				EditorApplication.update -= UpdatePulseAnimation;
				return;
			}

			var currentTime = EditorApplication.timeSinceStartup;
			var deltaTime = (float)(currentTime - _lastUpdateTime);
			_lastUpdateTime = currentTime;

			// Update pulse phase (2 Hz pulse rate)
			_pulsePhase += deltaTime * 2f * Mathf.PI;
			if (_pulsePhase > Mathf.PI * 2f)
			{
				_pulsePhase -= Mathf.PI * 2f;
			}

			// Calculate pulse value (0.65 to 1.0 opacity)
			var pulse = 0.65f + 0.35f * Mathf.Sin(_pulsePhase);

			// Apply to pulsing dot
			if (_hasPulsingDot)
			{
				_pulsingDot.style.opacity = pulse;
			}

			// Also pulse the border slightly
			SetBorderColor(new Color(ActiveColor.r, ActiveColor.g, ActiveColor.b, pulse));
		}

		private void UpdateFlashAnimation()
		{
			if (!_isFlashing)
			{
				EditorApplication.update -= UpdateFlashAnimation;
				return;
			}

			var currentTime = EditorApplication.timeSinceStartup;
			var deltaTime = (float)(currentTime - _lastUpdateTime);
			_lastUpdateTime = currentTime;

			_flashAlpha -= deltaTime * 2f; // Fade out over 0.5 seconds

			if (_flashAlpha <= 0f)
			{
				_flashAlpha = 0f;
				_isFlashing = false;

				// Only hide if not actively running
				if (!_isActive)
				{
					_borderIndicator.style.display = DisplayStyle.None;
					if (_hasPulsingDot)
					{
						_pulsingDot.style.display = DisplayStyle.None;
					}
				}

				EditorApplication.update -= UpdateFlashAnimation;
			}
			else
			{
				_borderIndicator.style.opacity = _flashAlpha;
				SetBorderColor(new Color(ActiveColor.r, ActiveColor.g, ActiveColor.b, _flashAlpha));
				if (_hasPulsingDot)
				{
					_pulsingDot.style.opacity = _flashAlpha;
				}
			}
		}

		private void SetBorderColor(Color color)
		{
			_borderIndicator.style.borderTopColor = color;
			_borderIndicator.style.borderBottomColor = color;
			_borderIndicator.style.borderLeftColor = color;
			_borderIndicator.style.borderRightColor = color;
		}
	}
}
