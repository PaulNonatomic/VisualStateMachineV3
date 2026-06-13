using System;
using System.Collections.Generic;
using System.Linq;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// A popup window that displays a preview of a sub-state machine graph.
	/// Shows a simplified view of nodes and connections with pan and zoom support.
	/// </summary>
	public class SubGraphPreviewPopup : VisualElement
	{
		private const float PopupWidth = 400f;
		private const float PopupHeight = 300f;
		private const float NodeWidth = 100f;
		private const float NodeHeight = 30f;
		private const float Padding = 20f;
		private const float HeaderHeight = 30f;
		private const float InfoBarHeight = 24f;
		private const float MinZoom = 0.25f;
		private const float MaxZoom = 4f;
		private const float ZoomStep = 0.1f;
		private const float GridSpacing = 50f;

		private readonly StateMachineGraph _subGraph;
		private readonly VisualElement _contentContainer;
		private readonly VisualElement _nodesContainer;
		private readonly IMGUIContainer _gridAndConnectionsContainer;

		private Dictionary<string, Rect> _nodeRects = new Dictionary<string, Rect>();
		private Vector2 _panOffset;
		private float _zoomLevel = 1f;
		private float _initialScale = 1f;
		private Vector2 _initialOffset;

		// Content pan drag state
		private bool _isDragging;
		private Vector2 _dragStartPos;
		private Vector2 _panStartOffset;

		// Popup drag state (for dragging the popup itself)
		private bool _isPopupDragging;
		private Vector2 _popupDragStartPos;
		private Vector2 _popupStartPosition;

		// Graph bounds (original coordinates)
		private float _graphMinX;
		private float _graphMinY;

		// Node ID this popup belongs to (for tracking purposes)
		private string _nodeId;

		// Owner node view for tracking position changes
		private UnityEditor.Experimental.GraphView.Node _ownerNodeView;
		private Vector2 _offsetFromOwnerNode;

		/// <summary>
		/// The ID of the sub-state machine node this popup belongs to.
		/// </summary>
		public string NodeId
		{
			get => _nodeId;
			set => _nodeId = value;
		}

		/// <summary>
		/// The current pan offset within the popup.
		/// </summary>
		public Vector2 PanOffset => _panOffset;

		/// <summary>
		/// The current zoom level within the popup.
		/// </summary>
		public float ZoomLevel => _zoomLevel;

		/// <summary>
		/// The sub-graph being previewed.
		/// </summary>
		public StateMachineGraph SubGraph => _subGraph;

		/// <summary>
		/// The offset from the owner node's position where this popup should appear.
		/// </summary>
		public Vector2 OffsetFromOwnerNode => _offsetFromOwnerNode;

		/// <summary>
		/// Event fired when the popup is closed.
		/// </summary>
		public event System.Action<SubGraphPreviewPopup> OnClosed;

		public SubGraphPreviewPopup(StateMachineGraph subGraph)
		{
			_subGraph = subGraph;

			// Setup popup container
			style.position = Position.Absolute;
			style.width = PopupWidth;
			style.height = PopupHeight;
			style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.98f);
			style.borderTopWidth = 1;
			style.borderBottomWidth = 1;
			style.borderLeftWidth = 1;
			style.borderRightWidth = 1;
			style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
			style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
			style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
			style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
			style.borderTopLeftRadius = 8;
			style.borderTopRightRadius = 8;
			style.borderBottomLeftRadius = 8;
			style.borderBottomRightRadius = 8;
			AddToClassList("subgraph-preview-popup");

			// Header (draggable)
			var header = new VisualElement();
			header.style.height = HeaderHeight;
			header.style.flexDirection = FlexDirection.Row;
			header.style.justifyContent = Justify.SpaceBetween;
			header.style.alignItems = Align.Center;
			header.style.paddingLeft = 10;
			header.style.paddingRight = 10;
			header.style.backgroundColor = new Color(0.25f, 0.2f, 0.3f);
			header.style.borderTopLeftRadius = 8;
			header.style.borderTopRightRadius = 8;
			Add(header);

			// Register header drag events for moving the popup
			header.RegisterCallback<MouseDownEvent>(OnHeaderMouseDown);
			header.RegisterCallback<MouseMoveEvent>(OnHeaderMouseMove);
			header.RegisterCallback<MouseUpEvent>(OnHeaderMouseUp);

			// Drag handle indicator
			var dragHandle = new Label("⋮⋮");
			dragHandle.style.color = new Color(0.5f, 0.5f, 0.5f);
			dragHandle.style.fontSize = 10;
			dragHandle.style.marginRight = 6;
			dragHandle.tooltip = "Drag to move";
			header.Add(dragHandle);

			var titleLabel = new Label(subGraph != null ? subGraph.name : "Preview");
			titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			titleLabel.style.color = Color.white;
			titleLabel.style.flexGrow = 1;
			header.Add(titleLabel);

			// Button container
			var buttonContainer = new VisualElement();
			buttonContainer.style.flexDirection = FlexDirection.Row;
			buttonContainer.style.alignItems = Align.Center;
			header.Add(buttonContainer);

			// Recenter button
			var recenterButton = new Button(() => Recenter()) { text = "◎" };
			recenterButton.tooltip = "Recenter view (F)";
			recenterButton.style.width = 24;
			recenterButton.style.height = 24;
			recenterButton.style.fontSize = 14;
			recenterButton.style.unityTextAlign = TextAnchor.MiddleCenter;
			recenterButton.style.backgroundColor = Color.clear;
			recenterButton.style.borderTopWidth = 0;
			recenterButton.style.borderBottomWidth = 0;
			recenterButton.style.borderLeftWidth = 0;
			recenterButton.style.borderRightWidth = 0;
			recenterButton.style.marginRight = 4;
			buttonContainer.Add(recenterButton);

			// Close button
			var closeButton = new Button(Close) { text = "×" };
			closeButton.style.width = 24;
			closeButton.style.height = 24;
			closeButton.style.fontSize = 16;
			closeButton.style.unityTextAlign = TextAnchor.MiddleCenter;
			closeButton.style.backgroundColor = Color.clear;
			closeButton.style.borderTopWidth = 0;
			closeButton.style.borderBottomWidth = 0;
			closeButton.style.borderLeftWidth = 0;
			closeButton.style.borderRightWidth = 0;
			buttonContainer.Add(closeButton);

			// Content area
			_contentContainer = new VisualElement();
			_contentContainer.style.flexGrow = 1;
			_contentContainer.style.overflow = Overflow.Hidden;
			Add(_contentContainer);

			// Grid and connections layer (drawn with IMGUI)
			_gridAndConnectionsContainer = new IMGUIContainer(DrawGridAndConnections);
			_gridAndConnectionsContainer.style.position = Position.Absolute;
			_gridAndConnectionsContainer.style.left = 0;
			_gridAndConnectionsContainer.style.top = 0;
			_gridAndConnectionsContainer.style.right = 0;
			_gridAndConnectionsContainer.style.bottom = 0;
			_contentContainer.Add(_gridAndConnectionsContainer);

			// Nodes layer
			_nodesContainer = new VisualElement();
			_nodesContainer.style.position = Position.Absolute;
			_nodesContainer.style.left = 0;
			_nodesContainer.style.top = 0;
			_nodesContainer.style.right = 0;
			_nodesContainer.style.bottom = 0;
			_contentContainer.Add(_nodesContainer);

			// Info bar
			var infoBar = new VisualElement();
			infoBar.style.height = InfoBarHeight;
			infoBar.style.flexDirection = FlexDirection.Row;
			infoBar.style.alignItems = Align.Center;
			infoBar.style.paddingLeft = 10;
			infoBar.style.paddingRight = 10;
			infoBar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
			infoBar.style.borderBottomLeftRadius = 8;
			infoBar.style.borderBottomRightRadius = 8;
			Add(infoBar);

			var nodeCountLabel = new Label(subGraph != null ? $"{subGraph.NodeCount} nodes" : "No graph");
			nodeCountLabel.style.fontSize = 10;
			nodeCountLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
			infoBar.Add(nodeCountLabel);

			// Register events
			RegisterCallback<KeyDownEvent>(OnKeyDown);
			RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
			RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

			// Pan and zoom events on content container
			_contentContainer.RegisterCallback<MouseDownEvent>(OnMouseDown);
			_contentContainer.RegisterCallback<MouseMoveEvent>(OnMouseMove);
			_contentContainer.RegisterCallback<MouseUpEvent>(OnMouseUp);
			_contentContainer.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
			_contentContainer.RegisterCallback<WheelEvent>(OnWheel);

			// Make focusable to receive keyboard events
			focusable = true;

			if (subGraph != null)
			{
				CalculateInitialView();
				BuildPreview();
			}
		}

		private void CalculateInitialView()
		{
			if (_subGraph == null) return;

			var nodes = _subGraph.GetAllNodes().ToList();
			if (nodes.Count == 0) return;

			// Calculate bounds of all nodes (still needed for _graphMinX/Y)
			_graphMinX = float.MaxValue;
			_graphMinY = float.MaxValue;
			var maxX = float.MinValue;
			var maxY = float.MinValue;

			foreach (var node in nodes)
			{
				_graphMinX = Mathf.Min(_graphMinX, node.Position.x);
				_graphMinY = Mathf.Min(_graphMinY, node.Position.y);
				maxX = Mathf.Max(maxX, node.Position.x + NodeWidth);
				maxY = Mathf.Max(maxY, node.Position.y + NodeHeight);
			}

			// Calculate scale to fit all nodes
			var graphWidth = maxX - _graphMinX + Padding * 2;
			var graphHeight = maxY - _graphMinY + Padding * 2;
			var availableWidth = PopupWidth - Padding * 2;
			var availableHeight = PopupHeight - HeaderHeight - InfoBarHeight - Padding * 2;

			_initialScale = Mathf.Min(
				availableWidth / graphWidth,
				availableHeight / graphHeight,
				2.5f
			);

			_zoomLevel = 3f;

			// Find entry node and center on it
			var entryNode = _subGraph.GetEntryNode();
			if (entryNode != null)
			{
				// Calculate the center of the entry node
				var entryCenter = new Vector2(
					entryNode.Position.x + NodeWidth / 2f,
					entryNode.Position.y + NodeHeight / 2f
				);

				// Calculate offset to center entry node in the view, accounting for zoom
				var effectiveScale = _initialScale * _zoomLevel;
				var viewCenterX = availableWidth / 2f + Padding;
				var viewCenterY = availableHeight / 2f;

				_initialOffset = new Vector2(
					(viewCenterX - entryCenter.x * effectiveScale) / _zoomLevel,
					(viewCenterY - entryCenter.y * effectiveScale) / _zoomLevel
				);
			}
			else
			{
				// Fallback: center the entire graph
				var scaledWidth = graphWidth * _initialScale;
				var scaledHeight = graphHeight * _initialScale;
				_initialOffset = new Vector2(
					(availableWidth - scaledWidth) / 2 + Padding - _graphMinX * _initialScale,
					(availableHeight - scaledHeight) / 2 + Padding - _graphMinY * _initialScale
				);
			}

			_panOffset = Vector2.zero;
		}

		private float CurrentScale => _initialScale * _zoomLevel;
		private Vector2 CurrentOffset => _initialOffset * _zoomLevel + _panOffset;

		private void BuildPreview()
		{
			if (_subGraph == null) return;

			_nodesContainer.Clear();
			_nodeRects.Clear();

			var nodes = _subGraph.GetAllNodes().ToList();
			if (nodes.Count == 0) return;

			var scale = CurrentScale;
			var offset = CurrentOffset;

			// Create node elements
			foreach (var node in nodes)
			{
				var nodeElement = CreateNodeElement(node, scale, offset);
				_nodesContainer.Add(nodeElement);

				var scaledX = node.Position.x * scale + offset.x;
				var scaledY = node.Position.y * scale + offset.y;
				var scaledWidth = NodeWidth * scale;
				var scaledHeight = NodeHeight * scale;

				_nodeRects[node.Id] = new Rect(scaledX, scaledY, scaledWidth, scaledHeight);
			}
		}

		private VisualElement CreateNodeElement(GraphNode node, float scale, Vector2 offset)
		{
			var nodeElement = new VisualElement();

			var scaledX = node.Position.x * scale + offset.x;
			var scaledY = node.Position.y * scale + offset.y;
			var scaledWidth = NodeWidth * scale;
			var scaledHeight = NodeHeight * scale;

			nodeElement.style.position = Position.Absolute;
			nodeElement.style.left = scaledX;
			nodeElement.style.top = scaledY;
			nodeElement.style.width = scaledWidth;
			nodeElement.style.height = scaledHeight;
			nodeElement.style.borderTopLeftRadius = 4 * scale;
			nodeElement.style.borderTopRightRadius = 4 * scale;
			nodeElement.style.borderBottomLeftRadius = 4 * scale;
			nodeElement.style.borderBottomRightRadius = 4 * scale;
			nodeElement.style.alignItems = Align.Center;
			nodeElement.style.justifyContent = Justify.Center;

			// Color based on node type
			var (bgColor, borderColor) = GetNodeColors(node);
			nodeElement.style.backgroundColor = bgColor;
			nodeElement.style.borderTopWidth = 1;
			nodeElement.style.borderBottomWidth = 1;
			nodeElement.style.borderLeftWidth = 1;
			nodeElement.style.borderRightWidth = 1;
			nodeElement.style.borderTopColor = borderColor;
			nodeElement.style.borderBottomColor = borderColor;
			nodeElement.style.borderLeftColor = borderColor;
			nodeElement.style.borderRightColor = borderColor;

			// Label
			var label = new Label(GetNodeDisplayName(node));
			label.style.fontSize = Mathf.Max(8, 10 * scale);
			label.style.color = Color.white;
			label.style.overflow = Overflow.Hidden;
			label.style.textOverflow = TextOverflow.Ellipsis;
			label.style.unityTextAlign = TextAnchor.MiddleCenter;
			label.style.whiteSpace = WhiteSpace.NoWrap;
			label.style.maxWidth = scaledWidth - 4;
			nodeElement.Add(label);

			// Tooltip
			nodeElement.tooltip = $"{node.DisplayName}\n{node.NodeType}";

			return nodeElement;
		}

		private (Color bg, Color border) GetNodeColors(GraphNode node)
		{
			// Colors matched to actual node views for consistency
			Color bg;
			switch (node.NodeType)
			{
				case GraphNodeType.Entry:
					bg = new Color(0.2f, 0.6f, 0.2f); // Matches EntryNodeView
					break;
				case GraphNodeType.Exit:
					bg = new Color(0.6f, 0.2f, 0.2f); // Matches ExitNodeView
					break;
				case GraphNodeType.State:
					bg = new Color(0.3f, 0.3f, 0.35f); // Matches StateNodeView default
					break;
				case GraphNodeType.SubStateMachine:
					bg = new Color(0.4f, 0.3f, 0.5f); // Matches SubStateMachineNodeView
					break;
				case GraphNodeType.PortalIn:
				case GraphNodeType.PortalOut:
					// Portal nodes use channel colors; use a neutral teal as default
					bg = new Color(0.25f, 0.45f, 0.45f);
					break;
				case GraphNodeType.BlackboardVariable:
					bg = new Color(0.2f, 0.4f, 0.6f); // Matches BlackboardVariableNodeView getter
					break;
				default:
					bg = new Color(0.3f, 0.3f, 0.3f);
					break;
			}

			// Border is a brighter version of the background
			var border = new Color(
				Mathf.Min(1f, bg.r * 1.4f),
				Mathf.Min(1f, bg.g * 1.4f),
				Mathf.Min(1f, bg.b * 1.4f)
			);

			return (bg, border);
		}

		private string GetNodeDisplayName(GraphNode node)
		{
			if (!string.IsNullOrEmpty(node.DisplayName))
				return node.DisplayName;

			return node.NodeType switch
			{
				GraphNodeType.Entry => "Entry",
				GraphNodeType.Exit => "Exit",
				_ => node.NodeType.ToString()
			};
		}

		private void DrawGridAndConnections()
		{
			var contentRect = _contentContainer.contentRect;
			if (contentRect.width <= 0 || contentRect.height <= 0) return;

			Handles.BeginGUI();

			DrawGrid(contentRect);
			DrawConnections();

			Handles.EndGUI();
		}

		private void DrawGrid(Rect contentRect)
		{
			var scale = CurrentScale;
			var offset = CurrentOffset;
			var gridStep = GridSpacing * scale;

			// Skip drawing grid if spacing is too small
			if (gridStep < 10f) return;

			// Minor grid lines
			var minorColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
			Handles.color = minorColor;

			// Calculate grid start positions
			var startX = offset.x % gridStep;
			var startY = offset.y % gridStep;

			// Draw vertical lines
			for (var x = startX; x < contentRect.width; x += gridStep)
			{
				if (x >= 0)
				{
					Handles.DrawLine(
						new Vector3(x, 0, 0),
						new Vector3(x, contentRect.height, 0)
					);
				}
			}

			// Draw horizontal lines
			for (var y = startY; y < contentRect.height; y += gridStep)
			{
				if (y >= 0)
				{
					Handles.DrawLine(
						new Vector3(0, y, 0),
						new Vector3(contentRect.width, y, 0)
					);
				}
			}

			// Major grid lines (every 5 minor lines)
			var majorGridStep = gridStep * 5;
			if (majorGridStep >= 50f)
			{
				var majorColor = new Color(0.25f, 0.25f, 0.25f, 0.7f);
				Handles.color = majorColor;

				var majorStartX = offset.x % majorGridStep;
				var majorStartY = offset.y % majorGridStep;

				for (var x = majorStartX; x < contentRect.width; x += majorGridStep)
				{
					if (x >= 0)
					{
						Handles.DrawLine(
							new Vector3(x, 0, 0),
							new Vector3(x, contentRect.height, 0)
						);
					}
				}

				for (var y = majorStartY; y < contentRect.height; y += majorGridStep)
				{
					if (y >= 0)
					{
						Handles.DrawLine(
							new Vector3(0, y, 0),
							new Vector3(contentRect.width, y, 0)
						);
					}
				}
			}
		}

		private void DrawConnections()
		{
			if (_subGraph == null || _nodeRects.Count == 0) return;

			var scale = CurrentScale;

			foreach (var connection in _subGraph.Connections)
			{
				if (!_nodeRects.TryGetValue(connection.SourceNodeId, out var sourceRect)) continue;
				if (!_nodeRects.TryGetValue(connection.TargetNodeId, out var targetRect)) continue;

				var startPos = new Vector3(sourceRect.xMax, sourceRect.center.y, 0);
				var endPos = new Vector3(targetRect.xMin, targetRect.center.y, 0);

				var color = connection.ConnectionType switch
				{
					ConnectionType.Transition => new Color(0.8f, 0.7f, 0.4f, 0.8f),
					ConnectionType.Data => new Color(0.4f, 0.7f, 0.9f, 0.8f),
					_ => new Color(0.6f, 0.6f, 0.6f, 0.8f)
				};

				Handles.color = color;

				// Draw bezier curve
				var tangentOffset = Mathf.Abs(endPos.x - startPos.x) * 0.5f;
				tangentOffset = Mathf.Max(tangentOffset, 30f * scale);

				Handles.DrawBezier(
					startPos,
					endPos,
					startPos + Vector3.right * tangentOffset,
					endPos + Vector3.left * tangentOffset,
					color,
					null,
					2f * scale
				);

				// Draw arrow at end
				var arrowSize = 6f * scale;
				var arrowDir = (endPos - startPos).normalized;
				var arrowLeft = Quaternion.Euler(0, 0, 150) * arrowDir * arrowSize;
				var arrowRight = Quaternion.Euler(0, 0, -150) * arrowDir * arrowSize;

				Handles.DrawLine(endPos, endPos + arrowLeft);
				Handles.DrawLine(endPos, endPos + arrowRight);
			}
		}

		private void OnMouseDown(MouseDownEvent evt)
		{
			// Start drag with middle mouse button or left mouse button
			if (evt.button == 2 || evt.button == 0)
			{
				_isDragging = true;
				_dragStartPos = evt.localMousePosition;
				_panStartOffset = _panOffset;
				_contentContainer.CaptureMouse();
				evt.StopPropagation();
			}
		}

		private void OnMouseMove(MouseMoveEvent evt)
		{
			if (!_isDragging) return;

			var delta = evt.localMousePosition - _dragStartPos;
			_panOffset = _panStartOffset + delta;

			BuildPreview();
			_gridAndConnectionsContainer.MarkDirtyRepaint();
			evt.StopPropagation();
		}

		private void OnMouseUp(MouseUpEvent evt)
		{
			if (_isDragging && (evt.button == 2 || evt.button == 0))
			{
				_isDragging = false;
				_contentContainer.ReleaseMouse();
				evt.StopPropagation();
			}
		}

		private void OnMouseLeave(MouseLeaveEvent evt)
		{
			// Only cancel drag if we don't have mouse captured
			if (_isDragging && !_contentContainer.HasMouseCapture())
			{
				_isDragging = false;
				_contentContainer.ReleaseMouse();
			}
		}

		private void OnHeaderMouseDown(MouseDownEvent evt)
		{
			if (evt.button != 0) return;

			_isPopupDragging = true;
			_popupDragStartPos = evt.mousePosition;
			_popupStartPosition = new Vector2(style.left.value.value, style.top.value.value);

			var header = evt.currentTarget as VisualElement;
			header?.CaptureMouse();
			evt.StopPropagation();
		}

		private void OnHeaderMouseMove(MouseMoveEvent evt)
		{
			if (!_isPopupDragging) return;

			var delta = evt.mousePosition - _popupDragStartPos;
			style.left = _popupStartPosition.x + delta.x;
			style.top = _popupStartPosition.y + delta.y;

			evt.StopPropagation();
		}

		private void OnHeaderMouseUp(MouseUpEvent evt)
		{
			if (!_isPopupDragging || evt.button != 0) return;

			_isPopupDragging = false;

			// Update the offset from owner node after dragging
			UpdateOffsetFromOwnerNode();

			var header = evt.currentTarget as VisualElement;
			header?.ReleaseMouse();
			evt.StopPropagation();
		}

		private void OnWheel(WheelEvent evt)
		{
			// Zoom towards mouse position
			var mousePos = evt.localMousePosition;
			var zoomDelta = -evt.delta.y * ZoomStep;
			var newZoom = Mathf.Clamp(_zoomLevel + zoomDelta, MinZoom, MaxZoom);

			if (Mathf.Approximately(newZoom, _zoomLevel)) return;

			// Calculate zoom center offset to zoom towards mouse position
			var zoomRatio = newZoom / _zoomLevel;
			var offsetFromMouse = mousePos - (_initialOffset * _zoomLevel + _panOffset);
			_panOffset = mousePos - offsetFromMouse * zoomRatio - _initialOffset * newZoom;

			_zoomLevel = newZoom;

			BuildPreview();
			_gridAndConnectionsContainer.MarkDirtyRepaint();
			evt.StopPropagation();
		}

		private void OnKeyDown(KeyDownEvent evt)
		{
			if (evt.keyCode == KeyCode.Escape)
			{
				Close();
				evt.StopPropagation();
			}
			// Reset view with 'F' key
			else if (evt.keyCode == KeyCode.F)
			{
				Recenter();
				evt.StopPropagation();
			}
		}

		/// <summary>
		/// Recenters the view on the entry node with the default zoom level.
		/// </summary>
		private void Recenter()
		{
			CalculateInitialView();
			BuildPreview();
			_gridAndConnectionsContainer.MarkDirtyRepaint();
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			// Focus to receive keyboard events
			Focus();
		}

		/// <summary>
		/// Shows the popup in graph coordinates, attached to the contentViewContainer
		/// so it moves with the graph when panning/zooming.
		/// </summary>
		/// <param name="graphView">The StateMachineGraphView to attach to</param>
		/// <param name="graphPosition">Position in graph coordinates (not world coordinates)</param>
		public void ShowInGraph(StateMachineGraphView graphView, Vector2 graphPosition)
		{
			// Add to contentViewContainer so the popup moves with the graph
			var contentContainer = graphView.contentViewContainer;

			style.left = graphPosition.x;
			style.top = graphPosition.y;

			contentContainer.Add(this);
			Focus();
		}

		public void Show(VisualElement parent, Vector2 position)
		{
			// Position the popup, ensuring it stays within bounds
			var parentBounds = parent.worldBound;

			var x = position.x;
			var y = position.y;

			// Adjust if popup would go off-screen
			if (x + PopupWidth > parentBounds.xMax)
				x = parentBounds.xMax - PopupWidth - 10;
			if (y + PopupHeight > parentBounds.yMax)
				y = parentBounds.yMax - PopupHeight - 10;
			if (x < parentBounds.xMin)
				x = parentBounds.xMin + 10;
			if (y < parentBounds.yMin)
				y = parentBounds.yMin + 10;

			style.left = x - parentBounds.xMin;
			style.top = y - parentBounds.yMin;

			parent.Add(this);
			Focus();
		}

		public void Close()
		{
			OnClosed?.Invoke(this);
			RemoveFromHierarchy();
		}

		/// <summary>
		/// Restores the pan and zoom state of the popup.
		/// </summary>
		public void RestoreState(Vector2 panOffset, float zoomLevel)
		{
			_panOffset = panOffset;
			_zoomLevel = Mathf.Clamp(zoomLevel, MinZoom, MaxZoom);
			BuildPreview();
			_gridAndConnectionsContainer.MarkDirtyRepaint();
		}

		/// <summary>
		/// Gets the current position of the popup in graph coordinates.
		/// </summary>
		public Vector2 GetGraphPosition()
		{
			return new Vector2(
				style.left.value.value,
				style.top.value.value
			);
		}

		/// <summary>
		/// Sets the owner node view for this popup, enabling position tracking.
		/// The popup will follow the node when it moves.
		/// </summary>
		/// <param name="nodeView">The owner node view.</param>
		/// <param name="initialOffset">The initial offset from the node's position.</param>
		public void SetOwnerNode(UnityEditor.Experimental.GraphView.Node nodeView, Vector2 initialOffset)
		{
			// Unsubscribe from previous owner
			if (_ownerNodeView != null)
			{
				_ownerNodeView.UnregisterCallback<GeometryChangedEvent>(OnOwnerNodeGeometryChanged);
			}

			_ownerNodeView = nodeView;
			_offsetFromOwnerNode = initialOffset;

			// Subscribe to new owner
			if (_ownerNodeView != null)
			{
				_ownerNodeView.RegisterCallback<GeometryChangedEvent>(OnOwnerNodeGeometryChanged);
			}
		}

		/// <summary>
		/// Updates the offset from the owner node based on current popup position.
		/// Call this after the popup has been repositioned.
		/// </summary>
		public void UpdateOffsetFromOwnerNode()
		{
			if (_ownerNodeView == null) return;

			var nodeRect = _ownerNodeView.GetPosition();
			var popupPos = GetGraphPosition();

			_offsetFromOwnerNode = new Vector2(
				popupPos.x - nodeRect.xMax,
				popupPos.y - nodeRect.yMin
			);
		}

		private void OnOwnerNodeGeometryChanged(GeometryChangedEvent evt)
		{
			if (_ownerNodeView == null) return;

			// Update popup position based on new node position
			var nodeRect = _ownerNodeView.GetPosition();
			style.left = nodeRect.xMax + _offsetFromOwnerNode.x;
			style.top = nodeRect.yMin + _offsetFromOwnerNode.y;
		}

		/// <summary>
		/// Cleans up event subscriptions when the popup is closed.
		/// </summary>
		private void OnDetachFromPanel(DetachFromPanelEvent evt)
		{
			if (_ownerNodeView != null)
			{
				_ownerNodeView.UnregisterCallback<GeometryChangedEvent>(OnOwnerNodeGeometryChanged);
				_ownerNodeView = null;
			}
		}
	}
}
