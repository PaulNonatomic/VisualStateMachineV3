using System;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Debugging
{
	public class BreakpointListPanel : FloatingPanel
	{
		private readonly StateMachineGraphWindow _window;
		private readonly ScrollView _listScroll;
		private readonly Label _emptyLabel;
		private readonly Toggle _globalToggle;
		private StateMachineGraph _graph;

		public event Action<string> OnNavigateToNode;

		public BreakpointListPanel(StateMachineGraphWindow window)
			: base("Breakpoints", "Breakpoints", new Vector2(10, 310), new Vector2(300, 200))
		{
			_window = window;

			AddToClassList("breakpoint-list-panel");

			// Global enable toggle + clear button header
			var headerRow = new VisualElement();
			headerRow.style.flexDirection = FlexDirection.Row;
			headerRow.style.justifyContent = Justify.SpaceBetween;
			headerRow.style.alignItems = Align.Center;
			headerRow.style.paddingLeft = 8;
			headerRow.style.paddingRight = 8;
			headerRow.style.paddingTop = 4;
			headerRow.style.paddingBottom = 4;
			headerRow.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);

			var toggleRow = new VisualElement();
			toggleRow.style.flexDirection = FlexDirection.Row;
			toggleRow.style.alignItems = Align.Center;

			_globalToggle = new Toggle();
			_globalToggle.value = BreakpointManager.GlobalEnabled;
			_globalToggle.tooltip = "Enable/disable all breakpoints globally";
			_globalToggle.RegisterValueChangedCallback(evt =>
			{
				BreakpointManager.GlobalEnabled = evt.newValue;
			});
			toggleRow.Add(_globalToggle);

			var enableLabel = new Label("Enabled");
			enableLabel.style.fontSize = 10;
			enableLabel.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
			toggleRow.Add(enableLabel);

			headerRow.Add(toggleRow);

			var clearButton = new Button(ClearAll) { text = "Clear All" };
			clearButton.style.fontSize = 10;
			clearButton.style.height = 18;
			clearButton.style.paddingTop = 1;
			clearButton.style.paddingBottom = 1;
			headerRow.Add(clearButton);

			ContentContainer.Add(headerRow);

			// Empty label
			_emptyLabel = new Label("No breakpoints set.\nRight-click a state node to add one.");
			_emptyLabel.style.color = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
			_emptyLabel.style.fontSize = 11;
			_emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
			_emptyLabel.style.paddingTop = 16;
			_emptyLabel.style.paddingBottom = 16;
			_emptyLabel.style.whiteSpace = WhiteSpace.Normal;

			// List container
			_listScroll = new ScrollView(ScrollViewMode.Vertical);
			_listScroll.style.flexGrow = 1;
			ContentContainer.Add(_listScroll);

			// Subscribe to changes
			BreakpointManager.OnBreakpointToggled += HandleBreakpointToggled;
			BreakpointManager.OnGlobalEnabledChanged += HandleGlobalEnabledChanged;

			// Unsubscribe when removed from panel to avoid leaks
			RegisterCallback<DetachFromPanelEvent>(_ =>
			{
				BreakpointManager.OnBreakpointToggled -= HandleBreakpointToggled;
				BreakpointManager.OnGlobalEnabledChanged -= HandleGlobalEnabledChanged;
			});

			// Start hidden
			SetVisible(false);
		}

		public void SetGraph(StateMachineGraph graph)
		{
			_graph = graph;
			Refresh();
		}

		public void Refresh()
		{
			_listScroll.Clear();

			if (_graph == null || _graph.GraphViewData.Breakpoints.Count == 0)
			{
				_listScroll.Add(_emptyLabel);
				return;
			}

			foreach (var breakpoint in _graph.GraphViewData.Breakpoints)
			{
				var row = CreateBreakpointRow(breakpoint);
				_listScroll.Add(row);
			}
		}

		private VisualElement CreateBreakpointRow(Breakpoint breakpoint)
		{
			var row = new VisualElement();
			row.style.flexDirection = FlexDirection.Row;
			row.style.alignItems = Align.Center;
			row.style.paddingTop = 3;
			row.style.paddingBottom = 3;
			row.style.paddingLeft = 8;
			row.style.paddingRight = 4;
			row.style.borderBottomWidth = 1;
			row.style.borderBottomColor = new StyleColor(new Color(0.25f, 0.25f, 0.25f));

			// Enabled toggle
			var toggle = new Toggle();
			toggle.value = breakpoint.Enabled;
			toggle.RegisterValueChangedCallback(evt =>
			{
				BreakpointManager.SetBreakpointEnabled(_graph, breakpoint.Id, evt.newValue);
			});
			row.Add(toggle);

			// Colored dot
			var dot = new VisualElement();
			dot.style.width = 8;
			dot.style.height = 8;
			dot.style.borderTopLeftRadius = 4;
			dot.style.borderTopRightRadius = 4;
			dot.style.borderBottomLeftRadius = 4;
			dot.style.borderBottomRightRadius = 4;
			dot.style.marginLeft = 2;
			dot.style.marginRight = 6;
			dot.style.backgroundColor = GetDotColor(breakpoint);
			row.Add(dot);

			// Node name (clickable to navigate)
			var nodeName = GetNodeDisplayName(breakpoint.NodeId);
			var nameButton = new Button(() => OnNavigateToNode?.Invoke(breakpoint.NodeId))
			{
				text = nodeName
			};
			nameButton.style.flexGrow = 1;
			nameButton.style.unityTextAlign = TextAnchor.MiddleLeft;
			nameButton.style.fontSize = 11;
			nameButton.style.backgroundColor = StyleKeyword.None;
			nameButton.style.borderTopWidth = 0;
			nameButton.style.borderBottomWidth = 0;
			nameButton.style.borderLeftWidth = 0;
			nameButton.style.borderRightWidth = 0;
			nameButton.style.paddingLeft = 0;
			row.Add(nameButton);

			// Type/condition label
			var typeText = breakpoint.Type switch
			{
				BreakpointType.OnEntry => "",
				BreakpointType.OnTransition => $"{GetNodeDisplayName(breakpoint.SourceNodeId)} ({breakpoint.TransitionPortName})",
				BreakpointType.Conditional => breakpoint.Condition ?? "",
				_ => ""
			};
			if (!string.IsNullOrEmpty(typeText))
			{
				var typeLabel = new Label(typeText);
				typeLabel.style.fontSize = 9;
				typeLabel.style.color = new StyleColor(new Color(0.55f, 0.55f, 0.55f));
				typeLabel.style.marginRight = 4;
				typeLabel.style.maxWidth = 120;
				typeLabel.style.overflow = Overflow.Hidden;
				row.Add(typeLabel);
			}

			// Remove button
			var removeButton = new Button(() =>
			{
				BreakpointManager.RemoveBreakpoint(_graph, breakpoint.Id);
			})
			{
				text = "X"
			};
			removeButton.style.width = 18;
			removeButton.style.height = 18;
			removeButton.style.fontSize = 10;
			removeButton.style.paddingTop = 0;
			removeButton.style.paddingBottom = 0;
			removeButton.style.paddingLeft = 0;
			removeButton.style.paddingRight = 0;
			row.Add(removeButton);

			return row;
		}

		private Color GetDotColor(Breakpoint breakpoint)
		{
			if (!breakpoint.Enabled)
				return new Color(0.5f, 0.5f, 0.5f, 0.6f);
			if (breakpoint.Type == BreakpointType.Conditional)
				return new Color(0.9f, 0.6f, 0.1f);
			return new Color(0.9f, 0.15f, 0.15f);
		}

		private string GetNodeDisplayName(string nodeId)
		{
			if (_graph == null || string.IsNullOrEmpty(nodeId)) return nodeId ?? "Unknown";

			var node = _graph.GetNode(nodeId);
			if (node is StateNode stateNode)
			{
				return stateNode.DisplayName ?? stateNode.StateType?.Name ?? nodeId;
			}

			return node?.NodeType.ToString() ?? nodeId;
		}

		private void ClearAll()
		{
			if (_graph == null) return;
			BreakpointManager.ClearAllBreakpoints(_graph);
		}

		private void HandleBreakpointToggled(StateMachineGraph graph, string nodeId)
		{
			if (graph == _graph)
			{
				Refresh();
			}
		}

		private void HandleGlobalEnabledChanged(bool enabled)
		{
			_globalToggle.SetValueWithoutNotify(enabled);
		}
	}
}
