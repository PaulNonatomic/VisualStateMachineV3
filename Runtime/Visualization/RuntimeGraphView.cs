using System.Collections.Generic;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Visualization
{
	/// <summary>
	/// Runtime UI Toolkit view that renders a <see cref="StateMachineGraph"/> and reflects
	/// live state from a <see cref="StateMachineInstance"/>. Suitable for in-game
	/// world-space rendering (via WorldSpace <c>PanelSettings</c>) or runtime debug overlays.
	///
	/// <para>
	/// Visuals are driven by a <see cref="RuntimeGraphTheme"/>. Edge rendering is delegated to an
	/// <see cref="IRuntimeEdgeRenderer"/> (selected by the theme, or overridden via
	/// <see cref="SetEdgeRenderer"/>). Node rendering is exposed through <c>protected virtual</c>
	/// build methods so consuming projects can subclass and customise node visuals without forking.
	/// </para>
	///
	/// <para>
	/// The view subscribes to the bound instance's <see cref="StateMachineInstance.OnStateChanged"/>
	/// event and unsubscribes automatically when it leaves the panel (<see cref="DetachFromPanelEvent"/>)
	/// or when rebound. Call <see cref="Unbind"/> for explicit teardown.
	/// </para>
	/// </summary>
	public class RuntimeGraphView : VisualElement
	{
		public new class UxmlFactory : UxmlFactory<RuntimeGraphView, UxmlTraits> { }

		protected const string UssRoot = "vsm-rt";

		private StateMachineGraph _graph;
		private StateMachineInstance _instance;
		private RuntimeGraphTheme _theme;
		private IRuntimeEdgeRenderer _edgeRenderer;
		private IRuntimeEdgeRenderer _edgeRendererOverride;

		private readonly VisualElement _edgeLayer;
		private readonly VisualElement _nodeLayer;
		private readonly Dictionary<string, VisualElement> _nodeElements = new Dictionary<string, VisualElement>();
		private readonly Dictionary<string, VisualElement> _portElements = new Dictionary<string, VisualElement>();

		private string _activeNodeId;

		/// <summary>Currently bound graph, or null if none.</summary>
		public StateMachineGraph Graph => _graph;

		/// <summary>Currently bound live instance, or null if running in static mode.</summary>
		public StateMachineInstance Instance => _instance;

		/// <summary>
		/// Active theme. Never null — falls back to <see cref="RuntimeGraphTheme.CreateDefault"/>
		/// if none has been assigned.
		/// </summary>
		public RuntimeGraphTheme Theme => _theme ??= RuntimeGraphTheme.CreateDefault();

		/// <summary>
		/// Active edge renderer: the override set via <see cref="SetEdgeRenderer"/> if present,
		/// otherwise the renderer selected by the theme's <see cref="RuntimeGraphTheme.EdgeRendererType"/>.
		/// </summary>
		public IRuntimeEdgeRenderer EdgeRenderer =>
			_edgeRendererOverride ?? (_edgeRenderer ??= ResolveEdgeRenderer(Theme.EdgeRendererType));

		public RuntimeGraphView()
		{
			AddToClassList(UssRoot);
			style.flexGrow = 1;
			style.overflow = Overflow.Hidden;
			pickingMode = PickingMode.Ignore;

			_edgeLayer = CreateFullSizeLayer("vsm-edge-layer");
			_edgeLayer.generateVisualContent += DrawEdges;
			Add(_edgeLayer);

			_nodeLayer = CreateFullSizeLayer("vsm-node-layer");
			Add(_nodeLayer);

			RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
			RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
		}

		/// <summary>
		/// Bind a graph (and optional live instance) to this view. Call again to rebind;
		/// the previous instance subscription is released first.
		/// </summary>
		public void Bind(StateMachineGraph graph, StateMachineInstance instance = null)
		{
			DetachInstance();

			_graph = graph;
			BuildNodes();

			AttachInstance(instance);
			_edgeLayer.MarkDirtyRepaint();
		}

		/// <summary>
		/// Release the live-instance subscription and clear the bound graph reference.
		/// Safe to call multiple times. Called automatically on <see cref="DetachFromPanelEvent"/>.
		/// </summary>
		public void Unbind()
		{
			DetachInstance();
			_graph = null;
		}

		/// <summary>
		/// Assign a theme and rebuild so colour/sizing changes take effect. Pass null to revert to defaults.
		/// </summary>
		public void SetTheme(RuntimeGraphTheme theme)
		{
			_theme = theme;
			_edgeRenderer = null; // re-resolve against the new theme's edge style
			Refresh();
		}

		/// <summary>
		/// Override the theme-selected edge renderer with a custom implementation.
		/// Pass null to fall back to the renderer chosen by the theme.
		/// </summary>
		public void SetEdgeRenderer(IRuntimeEdgeRenderer renderer)
		{
			_edgeRendererOverride = renderer;
			_edgeLayer.MarkDirtyRepaint();
		}

		/// <summary>Repaint edges only. Cheap; use after mutating edge colours/width on the theme.</summary>
		public void RefreshEdges()
		{
			_edgeLayer.MarkDirtyRepaint();
		}

		/// <summary>
		/// Rebuild nodes and edges from the bound graph. Call after mutating theme sizing/colours.
		/// No-op if no graph is bound.
		/// </summary>
		public void Refresh()
		{
			if (_graph == null)
			{
				return;
			}

			BuildNodes();
			if (_instance != null)
			{
				UpdateActiveHighlight(_instance.CurrentStateNodeId);
			}

			_edgeLayer.MarkDirtyRepaint();
		}

		private static VisualElement CreateFullSizeLayer(string name)
		{
			var layer = new VisualElement { name = name };
			layer.style.position = Position.Absolute;
			layer.style.left = 0;
			layer.style.top = 0;
			layer.style.right = 0;
			layer.style.bottom = 0;
			layer.pickingMode = PickingMode.Ignore;
			return layer;
		}

		private void BuildNodes()
		{
			_nodeLayer.Clear();
			_nodeElements.Clear();
			_portElements.Clear();
			_activeNodeId = null;

			if (_graph == null)
			{
				return;
			}

			var theme = Theme;
			var min = FindTopLeft();

			foreach (var node in _graph.GetAllNodes())
			{
				var element = CreateNodeElement(node, theme);
				var local = node.Position - min + new Vector2(theme.CanvasPadding, theme.CanvasPadding);

				element.style.left = local.x;
				element.style.top = local.y;

				// Each node redraws edges when its own layout settles. Without this the first
				// generateVisualContent call happens before layout has run and edges collapse to zero.
				element.RegisterCallback<GeometryChangedEvent>(_ => _edgeLayer.MarkDirtyRepaint());

				_nodeLayer.Add(element);
				_nodeElements[node.Id] = element;
			}
		}

		private Vector2 FindTopLeft()
		{
			var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
			foreach (var node in _graph.GetAllNodes())
			{
				if (node.Position.x < min.x) min.x = node.Position.x;
				if (node.Position.y < min.y) min.y = node.Position.y;
			}

			return float.IsPositiveInfinity(min.x) ? Vector2.zero : min;
		}

		/// <summary>
		/// Builds the visual element for a single node. Override to fully customise node visuals.
		/// </summary>
		protected virtual VisualElement CreateNodeElement(GraphNode node, RuntimeGraphTheme theme)
		{
			var element = new VisualElement { name = $"vsm-node-{node.Id}" };
			element.AddToClassList($"{UssRoot}__node");
			element.AddToClassList($"{UssRoot}__node--{node.NodeType.ToString().ToLowerInvariant()}");
			element.style.position = Position.Absolute;
			element.style.flexDirection = FlexDirection.Column;
			element.style.backgroundColor = theme.NodeBodyColor;
			ApplyBorder(element, theme.BorderColor, theme.BorderWidth);
			ApplyRadius(element, theme.NodeBorderRadius);
			element.style.overflow = Overflow.Hidden;

			if (node is StateNode stateNode)
			{
				element.style.width = theme.StateNodeWidth;
				BuildStateNode(element, stateNode, theme);
			}
			else
			{
				element.style.width = theme.EntryExitNodeWidth;
				BuildSimpleNode(element, node, theme);
			}

			return element;
		}

		/// <summary>
		/// Populates a state node element with a header and input/output port columns.
		/// Override to customise state-node layout; reuse <see cref="BuildHeader"/> and
		/// <see cref="BuildPortColumn"/> as needed.
		/// </summary>
		protected virtual void BuildStateNode(VisualElement element, StateNode stateNode, RuntimeGraphTheme theme)
		{
			var meta = StateNodeMetadataProvider.Get(stateNode.StateType);
			var headerColor = meta.StyleColor ?? theme.StateDefaultHeaderColor;

			element.Add(BuildHeader(GetNodeLabel(stateNode), headerColor, theme));

			var rows = Mathf.Max(meta.Inputs.Count, meta.Outputs.Count);
			var bodyHeight = Mathf.Max(theme.PortRowHeight, rows * theme.PortRowHeight) + theme.BodyVerticalPadding * 2f;
			element.style.height = theme.HeaderHeight + bodyHeight;

			var body = new VisualElement { name = "vsm-node-body" };
			body.style.flexDirection = FlexDirection.Row;
			body.style.flexGrow = 1;
			body.style.paddingTop = theme.BodyVerticalPadding;
			body.style.paddingBottom = theme.BodyVerticalPadding;

			body.Add(BuildPortColumn(stateNode.Id, meta.Inputs, leftAligned: true, theme));
			body.Add(BuildPortColumn(stateNode.Id, meta.Outputs, leftAligned: false, theme));

			element.Add(body);
		}

		/// <summary>
		/// Populates a non-state node (Entry/Exit/SubStateMachine) as a single coloured header.
		/// </summary>
		protected virtual void BuildSimpleNode(VisualElement element, GraphNode node, RuntimeGraphTheme theme)
		{
			var color = node switch
			{
				EntryNode => theme.EntryHeaderColor,
				ExitNode => theme.ExitHeaderColor,
				SubStateMachineNode => theme.SubStateMachineHeaderColor,
				_ => theme.StateDefaultHeaderColor
			};

			element.style.height = theme.HeaderHeight + 6f;
			element.style.backgroundColor = color;

			var label = new Label(GetNodeLabel(node));
			label.style.color = theme.HeaderTextColor;
			label.style.fontSize = theme.HeaderFontSize;
			label.style.unityTextAlign = TextAnchor.MiddleCenter;
			label.style.unityFontStyleAndWeight = FontStyle.Bold;
			label.style.flexGrow = 1;
			element.Add(label);
		}

		protected static VisualElement BuildHeader(string title, Color color, RuntimeGraphTheme theme)
		{
			var header = new VisualElement { name = "vsm-node-header" };
			header.style.height = theme.HeaderHeight;
			header.style.backgroundColor = color;
			header.style.flexDirection = FlexDirection.Row;
			header.style.alignItems = Align.Center;
			header.style.paddingLeft = 8;
			header.style.paddingRight = 8;

			var label = new Label(title);
			label.style.color = theme.HeaderTextColor;
			label.style.fontSize = theme.HeaderFontSize;
			label.style.unityFontStyleAndWeight = FontStyle.Bold;
			label.style.flexGrow = 1;
			header.Add(label);

			return header;
		}

		/// <summary>
		/// Builds a vertical column of port rows and registers each row as an edge anchor for
		/// the given node. Use <paramref name="leftAligned"/> true for inputs, false for outputs.
		/// </summary>
		protected VisualElement BuildPortColumn(string nodeId, IReadOnlyList<RuntimePortInfo> ports, bool leftAligned, RuntimeGraphTheme theme)
		{
			var column = new VisualElement { name = leftAligned ? "vsm-node-inputs" : "vsm-node-outputs" };
			column.style.flexGrow = 1;
			column.style.flexBasis = 0;
			column.style.flexDirection = FlexDirection.Column;

			foreach (var port in ports)
			{
				var row = BuildPortRow(port, leftAligned, theme);
				RegisterPortAnchor(nodeId, port.FieldName, row);
				column.Add(row);
			}

			return column;
		}

		protected static VisualElement BuildPortRow(RuntimePortInfo port, bool leftAligned, RuntimeGraphTheme theme)
		{
			var row = new VisualElement { name = $"vsm-port-{port.FieldName}" };
			row.style.flexDirection = leftAligned ? FlexDirection.Row : FlexDirection.RowReverse;
			row.style.alignItems = Align.Center;
			row.style.height = theme.PortRowHeight;
			row.style.paddingLeft = leftAligned ? 6 : 0;
			row.style.paddingRight = leftAligned ? 0 : 6;

			var dot = new VisualElement { name = "vsm-port-dot" };
			dot.style.width = 8;
			dot.style.height = 8;
			dot.style.flexShrink = 0;
			dot.style.flexGrow = 0;
			dot.style.marginLeft = leftAligned ? 0 : 6;
			dot.style.marginRight = leftAligned ? 6 : 0;
			dot.style.backgroundColor = GetPortColor(port.Kind, theme);
			ApplyRadius(dot, 4f);
			row.Add(dot);

			var label = new Label(port.DisplayName);
			label.style.color = theme.PortLabelColor;
			label.style.fontSize = theme.PortLabelFontSize;
			label.style.flexGrow = 1;
			label.style.unityTextAlign = leftAligned ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
			label.style.overflow = Overflow.Hidden;
			label.style.textOverflow = TextOverflow.Ellipsis;
			row.Add(label);

			return row;
		}

		/// <summary>
		/// Registers a visual element as the anchor for edges connecting to a node port.
		/// Custom node renderers should call this so edges attach to the right point.
		/// </summary>
		protected void RegisterPortAnchor(string nodeId, string portFieldName, VisualElement anchor)
		{
			_portElements[$"{nodeId}/{portFieldName}"] = anchor;
		}

		protected static Color GetPortColor(RuntimePortKind kind, RuntimeGraphTheme theme)
		{
			return kind switch
			{
				RuntimePortKind.Enter => theme.EnterPortColor,
				RuntimePortKind.Transition => theme.TransitionPortColor,
				RuntimePortKind.DataIn => theme.DataInPortColor,
				RuntimePortKind.DataOut => theme.DataOutPortColor,
				_ => Color.gray
			};
		}

		protected static string GetNodeLabel(GraphNode node)
		{
			if (!string.IsNullOrEmpty(node.DisplayName))
			{
				return node.DisplayName;
			}

			return node switch
			{
				StateNode state when state.StateType != null => state.StateType.Name,
				EntryNode => "Entry",
				ExitNode => "Exit",
				_ => node.NodeType.ToString()
			};
		}

		private void DrawEdges(MeshGenerationContext mgc)
		{
			if (_graph == null)
			{
				return;
			}

			var theme = Theme;
			var renderer = EdgeRenderer;
			var painter = mgc.painter2D;

			foreach (var connection in _graph.Connections)
			{
				if (connection.ConnectionType != ConnectionType.Transition)
				{
					continue;
				}

				if (!_nodeElements.TryGetValue(connection.SourceNodeId, out var source)) continue;
				if (!_nodeElements.TryGetValue(connection.TargetNodeId, out var target)) continue;

				if (!TryResolvePortPoint(connection.SourceNodeId, connection.SourcePortName, source, isOutput: true, out var p0)) continue;
				if (!TryResolvePortPoint(connection.TargetNodeId, connection.TargetPortName, target, isOutput: false, out var p3)) continue;

				var isActiveOutgoing = !string.IsNullOrEmpty(_activeNodeId) && connection.SourceNodeId == _activeNodeId;
				var style = new RuntimeEdgeStyle
				{
					StrokeColor = isActiveOutgoing ? theme.EdgeColorActive : theme.EdgeColor,
					Width = theme.EdgeWidth,
					DrawArrowhead = theme.DrawArrowheads,
					ArrowheadSize = theme.ArrowheadSize
				};

				renderer.Draw(painter, p0, p3, connection, style);
			}
		}

		/// <summary>
		/// Resolves the world-of-the-edge-layer point for a connection endpoint. Returns false
		/// when the node's layout has not been computed yet (so the edge is skipped this frame
		/// rather than drawn at the origin).
		/// </summary>
		private bool TryResolvePortPoint(string nodeId, string portName, VisualElement nodeElement, bool isOutput, out Vector2 point)
		{
			if (nodeElement.layout.width <= 0f)
			{
				point = default;
				return false;
			}

			var key = string.IsNullOrEmpty(portName) ? null : $"{nodeId}/{portName}";
			if (key != null && _portElements.TryGetValue(key, out var portElement) && portElement.layout.width > 0f)
			{
				var localX = isOutput ? portElement.layout.width : 0f;
				var localY = portElement.layout.height * 0.5f;
				point = portElement.ChangeCoordinatesTo(_edgeLayer, new Vector2(localX, localY));
				return true;
			}

			var nodeRect = nodeElement.layout;
			var fallbackX = isOutput ? nodeRect.xMax : nodeRect.xMin;
			point = new Vector2(fallbackX, nodeRect.center.y);
			return true;
		}

		private void AttachInstance(StateMachineInstance instance)
		{
			_instance = instance;
			if (_instance == null)
			{
				return;
			}

			_instance.OnStateChanged += HandleStateChanged;
			UpdateActiveHighlight(_instance.CurrentStateNodeId);
		}

		private void DetachInstance()
		{
			if (_instance == null)
			{
				return;
			}

			_instance.OnStateChanged -= HandleStateChanged;
			_instance = null;
		}

		private void HandleStateChanged(State previous, State next)
		{
			UpdateActiveHighlight(_instance?.CurrentStateNodeId);
		}

		private void UpdateActiveHighlight(string nodeId)
		{
			if (_activeNodeId == nodeId)
			{
				return;
			}

			var theme = Theme;
			if (_activeNodeId != null && _nodeElements.TryGetValue(_activeNodeId, out var previous))
			{
				ApplyActiveStyle(previous, false, theme);
			}

			_activeNodeId = nodeId;

			if (_activeNodeId != null && _nodeElements.TryGetValue(_activeNodeId, out var current))
			{
				ApplyActiveStyle(current, true, theme);
			}

			_edgeLayer.MarkDirtyRepaint();
		}

		private static void ApplyActiveStyle(VisualElement node, bool active, RuntimeGraphTheme theme)
		{
			ApplyBorder(node,
				active ? theme.ActiveBorderColor : theme.BorderColor,
				active ? theme.ActiveBorderWidth : theme.BorderWidth);

			var scale = active ? theme.ActiveScaleMultiplier : 1f;
			node.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1f)));
		}

		protected static void ApplyBorder(VisualElement element, Color color, float width)
		{
			element.style.borderTopColor = color;
			element.style.borderBottomColor = color;
			element.style.borderLeftColor = color;
			element.style.borderRightColor = color;
			element.style.borderTopWidth = width;
			element.style.borderBottomWidth = width;
			element.style.borderLeftWidth = width;
			element.style.borderRightWidth = width;
		}

		protected static void ApplyRadius(VisualElement element, float radius)
		{
			element.style.borderTopLeftRadius = radius;
			element.style.borderTopRightRadius = radius;
			element.style.borderBottomLeftRadius = radius;
			element.style.borderBottomRightRadius = radius;
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			_edgeLayer.MarkDirtyRepaint();
		}

		private void OnDetachFromPanel(DetachFromPanelEvent evt)
		{
			DetachInstance();
		}

		private static IRuntimeEdgeRenderer ResolveEdgeRenderer(RuntimeEdgeRendererType type)
		{
			return type switch
			{
				RuntimeEdgeRendererType.Wire => new WireRuntimeEdgeRenderer(),
				_ => new DefaultRuntimeEdgeRenderer()
			};
		}
	}
}
