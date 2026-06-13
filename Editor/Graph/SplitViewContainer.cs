using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Container that manages split view functionality for the graph editor.
	/// Wraps Unity's TwoPaneSplitView to provide split/unsplit toggle capability.
	/// </summary>
	public class SplitViewContainer : VisualElement
	{
		private const float DefaultSplitRatio = 0.5f;
		private const float MinPaneSize = 200f;
		private const float DragLineWidth = 6f;
		private const string SplitRatioEditorPref = "VSM.SplitView.Ratio";
		private const string SplitEnabledEditorPref = "VSM.SplitView.Enabled";
		private const string LinkScrollEditorPref = "VSM.SplitView.LinkScroll";

		private static readonly Color DragLineColor = new Color(0.153f, 0.153f, 0.153f, 1f);
		private static readonly Color DragLineHoverColor = new Color(0.208f, 0.208f, 0.208f, 1f);
		private static readonly Color DragLineDragColor = new Color(0.27f, 0.75f, 1f, 1f);

		private TwoPaneSplitView _splitView;
		private VisualElement _primaryPane;
		private VisualElement _secondaryPane;
		private VisualElement _dragLine;
		private VisualElement _dragLineHandle;
		private bool _isSplitEnabled;
		private bool _isLinkScrollEnabled;
		private float _splitRatio = DefaultSplitRatio;
		private bool _isDragging;
		private float _lastDragWidth;
		private IVisualElementScheduledItem _dragPoll;

		/// <summary>
		/// Fired when the split view is toggled on or off.
		/// </summary>
		public event Action<bool> OnSplitToggled;

		/// <summary>
		/// Fired when the active pane changes (user clicks on a pane).
		/// Parameter is true for primary, false for secondary.
		/// </summary>
		public event Action<bool> OnActivePaneChanged;

		/// <summary>
		/// Fired when link scroll is toggled.
		/// </summary>
		public event Action<bool> OnLinkScrollToggled;

		/// <summary>
		/// Whether the split view is currently enabled.
		/// </summary>
		public bool IsSplitEnabled => _isSplitEnabled;

		/// <summary>
		/// Whether link scroll is enabled (sync pan/zoom between views).
		/// </summary>
		public bool IsLinkScrollEnabled
		{
			get => _isLinkScrollEnabled;
			set
			{
				if (_isLinkScrollEnabled == value) return;
				_isLinkScrollEnabled = value;
				UnityEditor.EditorPrefs.SetBool(LinkScrollEditorPref, value);
				OnLinkScrollToggled?.Invoke(value);
			}
		}

		/// <summary>
		/// The primary pane container (always visible).
		/// </summary>
		public VisualElement PrimaryPane => _primaryPane;

		/// <summary>
		/// The secondary pane container (visible only when split is enabled).
		/// </summary>
		public VisualElement SecondaryPane => _secondaryPane;

		public SplitViewContainer()
		{
			name = "split-view-container";
			style.flexGrow = 1;
			style.flexDirection = FlexDirection.Row;

			// Load saved preferences
			_splitRatio = UnityEditor.EditorPrefs.GetFloat(SplitRatioEditorPref, DefaultSplitRatio);
			_isSplitEnabled = UnityEditor.EditorPrefs.GetBool(SplitEnabledEditorPref, false);
			_isLinkScrollEnabled = UnityEditor.EditorPrefs.GetBool(LinkScrollEditorPref, false);

			BuildUI();
		}

		private void BuildUI()
		{
			// Create the TwoPaneSplitView
			_splitView = new TwoPaneSplitView(0, MinPaneSize, TwoPaneSplitViewOrientation.Horizontal);
			_splitView.name = "graph-split-view";
			_splitView.style.flexGrow = 1;

			// Create primary pane
			_primaryPane = new VisualElement();
			_primaryPane.name = "primary-pane";
			_primaryPane.style.flexGrow = 1;
			_primaryPane.AddToClassList("graph-pane");
			_primaryPane.RegisterCallback<MouseDownEvent>(evt => OnPaneClicked(true));

			// Create secondary pane
			_secondaryPane = new VisualElement();
			_secondaryPane.name = "secondary-pane";
			_secondaryPane.style.flexGrow = 1;
			_secondaryPane.AddToClassList("graph-pane");
			_secondaryPane.RegisterCallback<MouseDownEvent>(evt => OnPaneClicked(false));

			// Add panes to split view
			_splitView.Add(_primaryPane);
			_splitView.Add(_secondaryPane);

			Add(_splitView);

			// Apply initial state
			UpdateSplitState();

			// Listen for geometry changes to update split ratio
			RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
		}

		private void FindAndStyleDragLine()
		{
			if (_dragLine != null) return;

			_dragLine = _splitView.Q(className: "unity-two-pane-split-view__dragline");

			if (_dragLine == null) return;

			// Keep drag line full-size and transparent for a wide interaction area
			_dragLine.style.backgroundColor = Color.clear;
			_dragLine.style.left = -2.5f;

			// Create a visible handle as a child
			_dragLineHandle = new VisualElement();
			_dragLineHandle.name = "split-drag-handle";
			_dragLineHandle.pickingMode = PickingMode.Ignore;
			_dragLineHandle.style.position = Position.Absolute;
			_dragLineHandle.style.width = DragLineWidth;
			_dragLineHandle.style.left = 0;
			_dragLineHandle.style.height = new Length(10, LengthUnit.Percent);
			_dragLineHandle.style.top = new Length(45, LengthUnit.Percent);
			_dragLineHandle.style.backgroundColor = DragLineColor;
			_dragLineHandle.style.borderTopLeftRadius = 3;
			_dragLineHandle.style.borderTopRightRadius = 3;
			_dragLineHandle.style.borderBottomLeftRadius = 3;
			_dragLineHandle.style.borderBottomRightRadius = 3;
			_dragLineHandle.style.borderTopWidth = 1;
			_dragLineHandle.style.borderBottomWidth = 1;
			_dragLineHandle.style.borderLeftWidth = 1;
			_dragLineHandle.style.borderRightWidth = 1;
			_dragLineHandle.style.borderTopColor = DragLineHoverColor;
			_dragLineHandle.style.borderBottomColor = DragLineHoverColor;
			_dragLineHandle.style.borderLeftColor = DragLineHoverColor;
			_dragLineHandle.style.borderRightColor = DragLineHoverColor;
			_dragLine.Add(_dragLineHandle);

			_dragLine.RegisterCallback<MouseEnterEvent>(_ =>
			{
				_dragLineHandle.style.backgroundColor = DragLineHoverColor;
			});

			_dragLine.RegisterCallback<MouseLeaveEvent>(_ =>
			{
				if (!_isDragging) _dragLineHandle.style.backgroundColor = DragLineColor;
			});

			_dragLine.RegisterCallback<MouseDownEvent>(_ =>
			{
				_isDragging = true;
				_lastDragWidth = _primaryPane.resolvedStyle.width;
				SetHandleBorderColor(DragLineDragColor);

				// Poll to detect when dragging stops — delay start so the
				// first tick doesn't fire before the user has moved the mouse
				_dragPoll?.Pause();
				_dragPoll = schedule.Execute(() =>
				{
					var currentWidth = _primaryPane.resolvedStyle.width;
					if (Mathf.Abs(currentWidth - _lastDragWidth) < 0.1f)
					{
						// Width stopped changing — drag ended
						_isDragging = false;
						SetHandleBorderColor(DragLineHoverColor);
						_dragLineHandle.style.backgroundColor = DragLineColor;
						_dragPoll?.Pause();
					}
					_lastDragWidth = currentWidth;
				}).StartingIn(500).Every(100);
			});
		}

		private void SetHandleBorderColor(Color color)
		{
			if (_dragLineHandle == null) return;
			_dragLineHandle.style.borderTopColor = color;
			_dragLineHandle.style.borderBottomColor = color;
			_dragLineHandle.style.borderLeftColor = color;
			_dragLineHandle.style.borderRightColor = color;
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			if (resolvedStyle.width <= 0) return;

			var totalWidth = resolvedStyle.width;

			if (!_isSplitEnabled)
			{
				// Keep primary pane expanded to fill container on resize
				_splitView.fixedPaneInitialDimension = totalWidth;
				return;
			}

			// Try to find and style the drag line on each geometry pass until found
			FindAndStyleDragLine();

			// Persist split ratio
			var primaryWidth = _primaryPane.resolvedStyle.width;
			if (primaryWidth > 0 && totalWidth > 0)
			{
				var newRatio = primaryWidth / totalWidth;
				if (Mathf.Abs(newRatio - _splitRatio) > 0.01f)
				{
					_splitRatio = newRatio;
					UnityEditor.EditorPrefs.SetFloat(SplitRatioEditorPref, _splitRatio);
				}
			}
		}

		private void OnPaneClicked(bool isPrimary)
		{
			OnActivePaneChanged?.Invoke(isPrimary);
		}

		/// <summary>
		/// Toggles the split view on or off.
		/// </summary>
		public void ToggleSplit()
		{
			SetSplitEnabled(!_isSplitEnabled);
		}

		/// <summary>
		/// Sets whether the split view is enabled.
		/// </summary>
		public void SetSplitEnabled(bool enabled)
		{
			if (_isSplitEnabled == enabled) return;

			_isSplitEnabled = enabled;
			UnityEditor.EditorPrefs.SetBool(SplitEnabledEditorPref, enabled);

			UpdateSplitState();
			OnSplitToggled?.Invoke(enabled);
		}

		private void UpdateSplitState()
		{
			if (_isSplitEnabled)
			{
				// Show secondary pane
				_secondaryPane.style.display = DisplayStyle.Flex;

				// Reset drag line cache so it gets re-found after toggling
				_dragLine = null;
				_dragLineHandle = null;

				var totalWidth = resolvedStyle.width;
				if (totalWidth > 0)
				{
					var fixedPaneSize = totalWidth * _splitRatio;
					_splitView.fixedPaneInitialDimension = Mathf.Max(fixedPaneSize, MinPaneSize);
				}
				else
				{
					// Width not resolved yet — apply once layout is ready
					schedule.Execute(() =>
					{
						var width = resolvedStyle.width;
						if (width > 0)
						{
							var size = width * _splitRatio;
							_splitView.fixedPaneInitialDimension = Mathf.Max(size, MinPaneSize);
						}
					});
				}
			}
			else
			{
				// Hide secondary pane
				_secondaryPane.style.display = DisplayStyle.None;

				// Expand primary pane to fill container by setting fixed dimension to full width
				// This prevents the grey gap that appears when the split view reserves space for the hidden pane
				var totalWidth = resolvedStyle.width > 0 ? resolvedStyle.width : 10000;
				_splitView.fixedPaneInitialDimension = totalWidth;
			}
		}

		/// <summary>
		/// Sets the split ratio (0-1, where 0.5 = 50/50 split).
		/// </summary>
		public void SetSplitRatio(float ratio)
		{
			_splitRatio = Mathf.Clamp(ratio, 0.2f, 0.8f);
			UnityEditor.EditorPrefs.SetFloat(SplitRatioEditorPref, _splitRatio);

			if (_isSplitEnabled)
			{
				UpdateSplitState();
			}
		}
	}
}
