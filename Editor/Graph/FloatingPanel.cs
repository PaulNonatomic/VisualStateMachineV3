using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Base class for floating, draggable panels in the graph editor.
	/// Similar to Shader Graph's floating blackboard and inspector.
	/// </summary>
	public abstract class FloatingPanel : VisualElement
	{
		private const string PositionXPrefKey = "VSM.FloatingPanel.{0}.X";
		private const string PositionYPrefKey = "VSM.FloatingPanel.{0}.Y";
		private const string WidthPrefKey = "VSM.FloatingPanel.{0}.Width";
		private const string HeightPrefKey = "VSM.FloatingPanel.{0}.Height";
		private const string VisiblePrefKey = "VSM.FloatingPanel.{0}.Visible";
		private const string CollapsedPrefKey = "VSM.FloatingPanel.{0}.Collapsed";

		/// <summary>
		/// Called when panel visibility changes.
		/// </summary>
		public event System.Action<FloatingPanel, bool> OnVisibilityChanged;

		protected VisualElement TitleBar { get; private set; }
		protected Label TitleLabel { get; private set; }
		protected VisualElement ContentContainer { get; private set; }
		protected ScrollView ScrollContainer { get; private set; }

		private readonly string _panelId;
		private readonly string _title;
		private Vector2 _dragOffset;
		private bool _isDragging;
		private Vector2 _defaultPosition;
		private Vector2 _defaultSize;
		private bool _isCollapsed;
		private float _expandedHeight;
		private Button _collapseButton;
		private VisualElement _contentWrapper;

		protected FloatingPanel(string panelId, string title, Vector2 defaultPosition, Vector2 defaultSize)
		{
			_panelId = panelId;
			_title = title;
			_defaultPosition = defaultPosition;
			_defaultSize = defaultSize;

			AddToClassList("floating-panel");

			style.position = Position.Absolute;
			style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
			style.borderTopLeftRadius = 8;
			style.borderTopRightRadius = 8;
			style.borderBottomLeftRadius = 8;
			style.borderBottomRightRadius = 8;
			style.borderTopWidth = 1;
			style.borderBottomWidth = 1;
			style.borderLeftWidth = 1;
			style.borderRightWidth = 1;
			style.borderTopColor = new Color(0.1f, 0.1f, 0.1f);
			style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f);
			style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f);
			style.borderRightColor = new Color(0.1f, 0.1f, 0.1f);

			BuildPanelUI();
			LoadPosition();

			// Bring to front on click
			RegisterCallback<MouseDownEvent>(evt =>
			{
				BringToFront();
			});
		}

		private void BuildPanelUI()
		{
			// Title bar (draggable)
			TitleBar = new VisualElement();
			TitleBar.AddToClassList("floating-panel-titlebar");
			TitleBar.style.flexDirection = FlexDirection.Row;
			TitleBar.style.justifyContent = Justify.SpaceBetween;
			TitleBar.style.alignItems = Align.Center;
			TitleBar.style.paddingLeft = 8;
			TitleBar.style.paddingRight = 8;
			TitleBar.style.paddingTop = 6;
			TitleBar.style.paddingBottom = 6;
			TitleBar.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
			TitleBar.style.borderTopLeftRadius = 8;
			TitleBar.style.borderTopRightRadius = 8;
			TitleBar.style.cursor = new StyleCursor(new UnityEngine.UIElements.Cursor());
			Add(TitleBar);

			// Left side: collapse button and title
			var leftContainer = new VisualElement();
			leftContainer.style.flexDirection = FlexDirection.Row;
			leftContainer.style.alignItems = Align.Center;
			TitleBar.Add(leftContainer);

			// Collapse button
			_collapseButton = new Button(ToggleCollapse);
			_collapseButton.text = "\u25BC"; // Down arrow (expanded)
			_collapseButton.style.width = 18;
			_collapseButton.style.height = 18;
			_collapseButton.style.marginRight = 6;
			_collapseButton.style.paddingLeft = 0;
			_collapseButton.style.paddingRight = 0;
			_collapseButton.style.paddingTop = 0;
			_collapseButton.style.paddingBottom = 0;
			_collapseButton.style.fontSize = 8;
			_collapseButton.style.unityTextAlign = TextAnchor.MiddleCenter;
			_collapseButton.style.backgroundColor = Color.clear;
			_collapseButton.style.borderTopWidth = 0;
			_collapseButton.style.borderBottomWidth = 0;
			_collapseButton.style.borderLeftWidth = 0;
			_collapseButton.style.borderRightWidth = 0;
			leftContainer.Add(_collapseButton);

			// Title label
			TitleLabel = new Label(_title);
			TitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			TitleLabel.style.fontSize = 12;
			TitleLabel.pickingMode = PickingMode.Ignore;
			leftContainer.Add(TitleLabel);

			// Title bar buttons container (right side)
			var buttonsContainer = new VisualElement();
			buttonsContainer.style.flexDirection = FlexDirection.Row;
			TitleBar.Add(buttonsContainer);

			// Add custom title bar buttons
			AddTitleBarButtons(buttonsContainer);

			// Close button
			var closeButton = new Button(() => SetVisible(false));
			closeButton.text = "\u2715"; // X character
			closeButton.style.width = 18;
			closeButton.style.height = 18;
			closeButton.style.marginLeft = 4;
			closeButton.style.paddingLeft = 0;
			closeButton.style.paddingRight = 0;
			closeButton.style.paddingTop = 0;
			closeButton.style.paddingBottom = 0;
			closeButton.style.fontSize = 10;
			closeButton.style.unityTextAlign = TextAnchor.MiddleCenter;
			closeButton.style.backgroundColor = Color.clear;
			closeButton.style.borderTopWidth = 0;
			closeButton.style.borderBottomWidth = 0;
			closeButton.style.borderLeftWidth = 0;
			closeButton.style.borderRightWidth = 0;
			buttonsContainer.Add(closeButton);

			// Content wrapper (for collapse/expand)
			_contentWrapper = new VisualElement();
			_contentWrapper.style.flexGrow = 1;
			Add(_contentWrapper);

			// Scroll container
			ScrollContainer = new ScrollView();
			ScrollContainer.style.flexGrow = 1;
			_contentWrapper.Add(ScrollContainer);

			// Content container
			ContentContainer = new VisualElement();
			ContentContainer.AddToClassList("floating-panel-content");
			ContentContainer.style.paddingLeft = 8;
			ContentContainer.style.paddingRight = 8;
			ContentContainer.style.paddingTop = 8;
			ContentContainer.style.paddingBottom = 8;
			ScrollContainer.Add(ContentContainer);

			// Setup drag handlers on title bar
			TitleBar.RegisterCallback<MouseDownEvent>(OnTitleBarMouseDown);
			TitleBar.RegisterCallback<MouseMoveEvent>(OnTitleBarMouseMove);
			TitleBar.RegisterCallback<MouseUpEvent>(OnTitleBarMouseUp);
			TitleBar.RegisterCallback<MouseLeaveEvent>(OnTitleBarMouseLeave);

			// Double-click to toggle collapse
			TitleBar.RegisterCallback<MouseDownEvent>(evt =>
			{
				if (evt.clickCount == 2 && evt.button == 0)
				{
					ToggleCollapse();
					evt.StopPropagation();
				}
			});
		}

		/// <summary>
		/// Override to add custom buttons to the title bar.
		/// </summary>
		protected virtual void AddTitleBarButtons(VisualElement container) { }

		private void OnTitleBarMouseDown(MouseDownEvent evt)
		{
			if (evt.button != 0) return;

			_isDragging = true;
			_dragOffset = evt.localMousePosition;
			TitleBar.CaptureMouse();
			evt.StopPropagation();
		}

		private void OnTitleBarMouseMove(MouseMoveEvent evt)
		{
			if (!_isDragging) return;

			var parentRect = parent?.worldBound ?? new Rect(0, 0, 1920, 1080);
			var newPos = evt.mousePosition - _dragOffset;

			// Clamp to parent bounds
			newPos.x = Mathf.Clamp(newPos.x, 0, parentRect.width - resolvedStyle.width);
			newPos.y = Mathf.Clamp(newPos.y, 0, parentRect.height - resolvedStyle.height);

			style.left = newPos.x;
			style.top = newPos.y;

			evt.StopPropagation();
		}

		private void OnTitleBarMouseUp(MouseUpEvent evt)
		{
			if (!_isDragging) return;

			_isDragging = false;
			TitleBar.ReleaseMouse();
			SavePosition();
			evt.StopPropagation();
		}

		private void OnTitleBarMouseLeave(MouseLeaveEvent evt)
		{
			// Continue dragging even if mouse leaves title bar during drag
		}

		public void SetVisible(bool visible)
		{
			var wasVisible = IsVisible;
			style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
			SaveVisibility();

			if (wasVisible != visible)
			{
				OnVisibilityChanged?.Invoke(this, visible);
			}
		}

		public bool IsVisible => resolvedStyle.display == DisplayStyle.Flex;

		public void ToggleVisibility()
		{
			SetVisible(!IsVisible);
		}

		public bool IsCollapsed => _isCollapsed;

		public void ToggleCollapse()
		{
			SetCollapsed(!_isCollapsed);
		}

		public void SetCollapsed(bool collapsed)
		{
			if (_isCollapsed == collapsed) return;

			_isCollapsed = collapsed;

			if (collapsed)
			{
				// Store the current height before collapsing
				_expandedHeight = resolvedStyle.height;
				if (_expandedHeight < 50) _expandedHeight = _defaultSize.y;

				// Hide content and set to title bar height only
				_contentWrapper.style.display = DisplayStyle.None;
				style.height = StyleKeyword.Auto;

				// Update collapse button
				_collapseButton.text = "\u25B6"; // Right arrow (collapsed)

				// Round bottom corners of title bar when collapsed
				TitleBar.style.borderBottomLeftRadius = 8;
				TitleBar.style.borderBottomRightRadius = 8;
			}
			else
			{
				// Show content and restore height
				_contentWrapper.style.display = DisplayStyle.Flex;
				style.height = _expandedHeight;

				// Update collapse button
				_collapseButton.text = "\u25BC"; // Down arrow (expanded)

				// Remove bottom corner rounding from title bar
				TitleBar.style.borderBottomLeftRadius = 0;
				TitleBar.style.borderBottomRightRadius = 0;
			}

			SaveCollapsedState();
		}

		protected void LoadPosition()
		{
			var x = EditorPrefs.GetFloat(string.Format(PositionXPrefKey, _panelId), _defaultPosition.x);
			var y = EditorPrefs.GetFloat(string.Format(PositionYPrefKey, _panelId), _defaultPosition.y);
			var width = EditorPrefs.GetFloat(string.Format(WidthPrefKey, _panelId), _defaultSize.x);
			var height = EditorPrefs.GetFloat(string.Format(HeightPrefKey, _panelId), _defaultSize.y);
			var visible = EditorPrefs.GetBool(string.Format(VisiblePrefKey, _panelId), true);
			var collapsed = EditorPrefs.GetBool(string.Format(CollapsedPrefKey, _panelId), false);

			style.left = x;
			style.top = y;
			style.width = width;
			style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

			// Store expanded height and apply collapsed state
			_expandedHeight = height;
			if (collapsed)
			{
				_isCollapsed = true;
				_contentWrapper.style.display = DisplayStyle.None;
				style.height = StyleKeyword.Auto;
				_collapseButton.text = "\u25B6";
				TitleBar.style.borderBottomLeftRadius = 8;
				TitleBar.style.borderBottomRightRadius = 8;
			}
			else
			{
				style.height = height;
			}
		}

		protected void SavePosition()
		{
			EditorPrefs.SetFloat(string.Format(PositionXPrefKey, _panelId), resolvedStyle.left);
			EditorPrefs.SetFloat(string.Format(PositionYPrefKey, _panelId), resolvedStyle.top);
			EditorPrefs.SetFloat(string.Format(WidthPrefKey, _panelId), resolvedStyle.width);

			// Save the expanded height, not the collapsed height
			var heightToSave = _isCollapsed ? _expandedHeight : resolvedStyle.height;
			EditorPrefs.SetFloat(string.Format(HeightPrefKey, _panelId), heightToSave);
		}

		protected void SaveVisibility()
		{
			EditorPrefs.SetBool(string.Format(VisiblePrefKey, _panelId), IsVisible);
		}

		protected void SaveCollapsedState()
		{
			EditorPrefs.SetBool(string.Format(CollapsedPrefKey, _panelId), _isCollapsed);
		}

		/// <summary>
		/// Resets the panel to its default position, size, and expanded state.
		/// </summary>
		public void ResetPosition()
		{
			style.left = _defaultPosition.x;
			style.top = _defaultPosition.y;
			style.width = _defaultSize.x;
			style.height = _defaultSize.y;
			_expandedHeight = _defaultSize.y;

			// Reset to expanded state
			if (_isCollapsed)
			{
				_isCollapsed = false;
				_contentWrapper.style.display = DisplayStyle.Flex;
				_collapseButton.text = "\u25BC";
				TitleBar.style.borderBottomLeftRadius = 0;
				TitleBar.style.borderBottomRightRadius = 0;
			}

			SavePosition();
			SaveCollapsedState();
		}
	}
}
