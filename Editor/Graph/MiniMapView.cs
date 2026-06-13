using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Mini-map overlay for navigating large state machine graphs.
	/// Shows a scaled-down view of the entire graph with viewport indicator.
	/// </summary>
	public class MiniMapView : VisualElement
	{
		public event Action<bool> OnVisibilityChanged;

		private const string PrefsKeyVisible = "VSM.MiniMap.Visible";
		private const float DefaultWidth = 200f;
		private const float DefaultHeight = 150f;
		private const float MinWidth = 100f;
		private const float MinHeight = 75f;
		private const float MaxWidth = 400f;
		private const float MaxHeight = 300f;

		private readonly MiniMap _miniMap;
		private StateMachineGraphView _graphView;
		private readonly VisualElement _resizeHandle;
		private bool _isResizing;
		private Vector2 _resizeStartPos;
		private Vector2 _resizeStartSize;

		public MiniMapView(StateMachineGraphView graphView)
		{
			_graphView = graphView;

			// Container styling
			name = "mini-map-container";
			AddToClassList("mini-map-container");

			// Position in bottom-left corner (above coordinates display if present)
			style.position = Position.Absolute;
			style.left = 8;
			style.bottom = 40;

			// Visual styling
			style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
			style.borderTopLeftRadius = 4;
			style.borderTopRightRadius = 4;
			style.borderBottomLeftRadius = 4;
			style.borderBottomRightRadius = 4;
			style.borderTopWidth = 1;
			style.borderBottomWidth = 1;
			style.borderLeftWidth = 1;
			style.borderRightWidth = 1;
			style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
			style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
			style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
			style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
			style.overflow = Overflow.Hidden;

			// Create header with title and close button
			var header = CreateHeader();
			Add(header);

			// Create the Unity MiniMap
			_miniMap = new MiniMap();
			_miniMap.name = "mini-map";
			_miniMap.SetPosition(new Rect(0, 0, DefaultWidth, DefaultHeight));
			_miniMap.style.width = DefaultWidth;
			_miniMap.style.height = DefaultHeight;
			_miniMap.style.minWidth = MinWidth;
			_miniMap.style.minHeight = MinHeight;
			_miniMap.style.maxWidth = MaxWidth;
			_miniMap.style.maxHeight = MaxHeight;
			_miniMap.style.position = Position.Relative;

			// Override default MiniMap styling
			_miniMap.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);

			Add(_miniMap);

			// Add resize handle in bottom-right corner
			_resizeHandle = CreateResizeHandle();
			Add(_resizeHandle);

			// Set initial visibility from prefs
			var visible = EditorPrefs.GetBool(PrefsKeyVisible, true);
			style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
		}

		/// <summary>
		/// Gets or sets the visibility of the mini-map.
		/// </summary>
		public bool IsVisible
		{
			get => style.display == DisplayStyle.Flex;
			set
			{
				var wasVisible = IsVisible;
				style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
				EditorPrefs.SetBool(PrefsKeyVisible, value);

				if (wasVisible != value)
				{
					OnVisibilityChanged?.Invoke(value);
				}
			}
		}

		/// <summary>
		/// Toggles the mini-map visibility.
		/// </summary>
		public void ToggleVisibility()
		{
			IsVisible = !IsVisible;
		}

		/// <summary>
		/// Sets the graph view that the mini-map tracks.
		/// Unity's MiniMap finds its GraphView via GetFirstAncestorOfType,
		/// so this re-parents the mini-map under the target graph view.
		/// </summary>
		public void SetGraphView(StateMachineGraphView graphView)
		{
			if (graphView == _graphView) return;

			_graphView = graphView;

			if (graphView != null)
			{
				RemoveFromHierarchy();
				graphView.Add(this);
			}
		}

		private VisualElement CreateHeader()
		{
			var header = new VisualElement();
			header.name = "mini-map-header";
			header.style.flexDirection = FlexDirection.Row;
			header.style.justifyContent = Justify.SpaceBetween;
			header.style.alignItems = Align.Center;
			header.style.paddingLeft = 6;
			header.style.paddingRight = 2;
			header.style.paddingTop = 2;
			header.style.paddingBottom = 2;
			header.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
			header.style.borderBottomWidth = 1;
			header.style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f);

			var title = new Label("Mini-Map");
			title.name = "mini-map-title";
			title.style.fontSize = 10;
			title.style.color = new Color(0.7f, 0.7f, 0.7f);
			title.style.unityFontStyleAndWeight = FontStyle.Bold;
			header.Add(title);

			var closeButton = new Button(() => IsVisible = false)
			{
				text = "x"
			};
			closeButton.name = "mini-map-close";
			closeButton.style.width = 16;
			closeButton.style.height = 16;
			closeButton.style.fontSize = 10;
			closeButton.style.paddingLeft = 0;
			closeButton.style.paddingRight = 0;
			closeButton.style.paddingTop = 0;
			closeButton.style.paddingBottom = 0;
			closeButton.style.marginLeft = 0;
			closeButton.style.marginRight = 0;
			closeButton.style.marginTop = 0;
			closeButton.style.marginBottom = 0;
			closeButton.style.borderTopLeftRadius = 2;
			closeButton.style.borderTopRightRadius = 2;
			closeButton.style.borderBottomLeftRadius = 2;
			closeButton.style.borderBottomRightRadius = 2;
			closeButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
			closeButton.style.borderTopWidth = 0;
			closeButton.style.borderBottomWidth = 0;
			closeButton.style.borderLeftWidth = 0;
			closeButton.style.borderRightWidth = 0;
			header.Add(closeButton);

			return header;
		}

		private VisualElement CreateResizeHandle()
		{
			var handle = new VisualElement();
			handle.name = "mini-map-resize-handle";
			handle.style.position = Position.Absolute;
			handle.style.right = 0;
			handle.style.bottom = 0;
			handle.style.width = 12;
			handle.style.height = 12;
			handle.style.cursor = StyleKeyword.Null;

			// Draw resize grip lines
			handle.generateVisualContent += ctx =>
			{
				var painter = ctx.painter2D;
				painter.strokeColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
				painter.lineWidth = 1;

				// Draw diagonal grip lines
				for (var i = 0; i < 3; i++)
				{
					var offset = 3 + i * 3;
					painter.BeginPath();
					painter.MoveTo(new Vector2(12, offset));
					painter.LineTo(new Vector2(offset, 12));
					painter.Stroke();
				}
			};

			// Handle resize drag
			handle.RegisterCallback<PointerDownEvent>(OnResizeStart);
			handle.RegisterCallback<PointerMoveEvent>(OnResizeMove);
			handle.RegisterCallback<PointerUpEvent>(OnResizeEnd);
			handle.RegisterCallback<PointerCaptureOutEvent>(OnResizeCaptureOut);

			return handle;
		}

		private void OnResizeStart(PointerDownEvent evt)
		{
			if (evt.button != 0) return;

			_isResizing = true;
			_resizeStartPos = evt.position;
			_resizeStartSize = new Vector2(_miniMap.resolvedStyle.width, _miniMap.resolvedStyle.height);
			_resizeHandle.CapturePointer(evt.pointerId);
			evt.StopPropagation();
		}

		private void OnResizeMove(PointerMoveEvent evt)
		{
			if (!_isResizing) return;

			var delta = (Vector2)evt.position - _resizeStartPos;
			var newWidth = Mathf.Clamp(_resizeStartSize.x + delta.x, MinWidth, MaxWidth);
			var newHeight = Mathf.Clamp(_resizeStartSize.y + delta.y, MinHeight, MaxHeight);

			_miniMap.style.width = newWidth;
			_miniMap.style.height = newHeight;

			evt.StopPropagation();
		}

		private void OnResizeEnd(PointerUpEvent evt)
		{
			if (!_isResizing) return;

			_isResizing = false;
			_resizeHandle.ReleasePointer(evt.pointerId);
			evt.StopPropagation();
		}

		private void OnResizeCaptureOut(PointerCaptureOutEvent evt)
		{
			_isResizing = false;
		}
	}
}
