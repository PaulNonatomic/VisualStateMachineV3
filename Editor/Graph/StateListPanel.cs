using System;
using System.Collections.Generic;
using System.Linq;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Filter options for the state list panel.
	/// </summary>
	public enum StateListFilter
	{
		All,
		States,
		Navigation,
		SubStateMachines,
		Variables
	}

	/// <summary>
	/// Floating panel that displays a searchable, filterable list of all states in the current graph.
	/// Click a state to frame it in the viewport.
	/// </summary>
	public class StateListPanel : FloatingPanel
	{
		private const string AutoFrameSearchResultPrefKey = "VSM.StateList.AutoFrameSearchResult";

		private readonly StateMachineGraphWindow _window;
		private readonly Dictionary<string, StateListItem> _stateItems = new();

		// Cached connection counts to avoid repeated iterations (O(n) per node -> O(1) lookup)
		private Dictionary<string, (int inCount, int outCount)> _connectionCountCache = new();

		private StateMachineGraph _asset;
		private VisualElement _stateContainer;
		private TextField _searchField;
		private EnumField _filterDropdown;
		private Label _countLabel;
		private StateListFilter _currentFilter = StateListFilter.All;
		private string _currentSelectedNodeId;
		private bool _autoFrameSearchResult;

		/// <summary>
		/// When enabled, automatically frames the node when search filters to a single result.
		/// </summary>
		public bool AutoFrameSearchResult
		{
			get => _autoFrameSearchResult;
			set
			{
				_autoFrameSearchResult = value;
				EditorPrefs.SetBool(AutoFrameSearchResultPrefKey, value);
			}
		}

		public StateListPanel(StateMachineGraphWindow window)
			: base("StateList", "State List", new Vector2(280, 10), new Vector2(240, 350))
		{
			_window = window;
			_autoFrameSearchResult = EditorPrefs.GetBool(AutoFrameSearchResultPrefKey, true);

			AddToClassList("state-list-panel");
			BuildContent();

			// Subscribe to selection changes
			if (_window.GraphView != null)
			{
				_window.GraphView.OnSelectionChanged += OnGraphSelectionChanged;
			}
		}

		private void BuildContent()
		{
			// Search row
			var searchRow = new VisualElement();
			searchRow.style.flexDirection = FlexDirection.Row;
			searchRow.style.marginBottom = 4;
			ContentContainer.Add(searchRow);

			// Search field
			_searchField = new TextField();
			_searchField.style.flexGrow = 1;
			_searchField.RegisterValueChangedCallback(OnSearchChanged);

			var placeholder = new Label("Search nodes...");
			placeholder.style.position = Position.Absolute;
			placeholder.style.left = 4;
			placeholder.style.top = 2;
			placeholder.style.color = new Color(0.5f, 0.5f, 0.5f);
			placeholder.pickingMode = PickingMode.Ignore;
			_searchField.Add(placeholder);

			_searchField.RegisterCallback<FocusInEvent>(evt => placeholder.style.display = DisplayStyle.None);
			_searchField.RegisterCallback<FocusOutEvent>(evt =>
			{
				if (string.IsNullOrEmpty(_searchField.value))
				{
					placeholder.style.display = DisplayStyle.Flex;
				}
			});

			searchRow.Add(_searchField);

			// Filter row
			var filterRow = new VisualElement();
			filterRow.style.flexDirection = FlexDirection.Row;
			filterRow.style.alignItems = Align.Center;
			filterRow.style.marginBottom = 6;
			ContentContainer.Add(filterRow);

			// Filter dropdown
			_filterDropdown = new EnumField(StateListFilter.All);
			_filterDropdown.style.flexGrow = 1;
			_filterDropdown.RegisterValueChangedCallback(OnFilterChanged);
			filterRow.Add(_filterDropdown);

			// Count label
			_countLabel = new Label("0 nodes");
			_countLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
			_countLabel.style.fontSize = 10;
			_countLabel.style.marginLeft = 8;
			filterRow.Add(_countLabel);

			// State container (inside scroll view)
			_stateContainer = new VisualElement();
			ContentContainer.Add(_stateContainer);
		}

		public void PopulateFromAsset(StateMachineGraph asset)
		{
			_asset = asset;
			RebuildStateList();
		}

		/// <summary>
		/// Rebuilds the entire state list from the asset.
		/// </summary>
		private void RebuildStateList()
		{
			_stateContainer.Clear();
			_stateItems.Clear();

			if (_asset == null)
			{
				_connectionCountCache.Clear();
				_countLabel.text = "0 nodes";
				return;
			}

			// Pre-compute all connection counts once (O(connections) instead of O(nodes * connections))
			BuildConnectionCountCache();

			// Get all nodes and sort by type then name
			var nodes = _asset.GetAllNodes()
				.Where(n => n != null)
				.OrderBy(n => GetNodeTypeSortOrder(n.NodeType))
				.ThenBy(n => GetNodeDisplayName(n))
				.ToList();

			foreach (var node in nodes)
			{
				var displayName = GetNodeDisplayName(node);
				var listItem = CreateStateListItem(node, displayName);
				_stateContainer.Add(listItem.Element);
				_stateItems[node.Id] = listItem;
			}

			ApplyFilters();
			RefreshSelection();
		}

		/// <summary>
		/// Applies search and type filters to show/hide items.
		/// </summary>
		private void ApplyFilters()
		{
			var searchTerm = _searchField?.value?.ToLowerInvariant() ?? "";
			var visibleCount = 0;
			var totalCount = _stateItems.Count;
			GraphNode singleVisibleNode = null;

			foreach (var kvp in _stateItems)
			{
				var item = kvp.Value;
				var node = item.Node;
				var displayName = GetNodeDisplayName(node);

				var matchesSearch = string.IsNullOrEmpty(searchTerm) ||
				                    displayName.ToLowerInvariant().Contains(searchTerm) ||
				                    node.NodeType.ToString().ToLowerInvariant().Contains(searchTerm);

				var matchesFilter = MatchesFilter(node, _currentFilter);

				var visible = matchesSearch && matchesFilter;
				item.Element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

				if (visible)
				{
					visibleCount++;
					singleVisibleNode = visibleCount == 1 ? node : null;
				}
			}

			if (visibleCount == totalCount)
			{
				_countLabel.text = totalCount == 1 ? "1 node" : $"{totalCount} nodes";
			}
			else
			{
				_countLabel.text = $"{visibleCount}/{totalCount}";
			}

			// Auto-frame when search filters to a single result
			if (_autoFrameSearchResult && visibleCount == 1 && singleVisibleNode != null && !string.IsNullOrEmpty(searchTerm))
			{
				_window.GraphView?.SelectNode(singleVisibleNode.Id);
				_window.GraphView?.CenterOnState(singleVisibleNode.Id, _window.GraphView.UseLerpAnimation);
			}
		}

		private bool MatchesFilter(GraphNode node, StateListFilter filter)
		{
			return filter switch
			{
				StateListFilter.All => true,
				StateListFilter.States => node.NodeType == GraphNodeType.State,
				StateListFilter.Navigation => node.NodeType is GraphNodeType.Entry or GraphNodeType.Exit
				                              or GraphNodeType.PortalIn or GraphNodeType.PortalOut,
				StateListFilter.SubStateMachines => node.NodeType == GraphNodeType.SubStateMachine,
				StateListFilter.Variables => node.NodeType == GraphNodeType.BlackboardVariable,
				_ => true
			};
		}

		/// <summary>
		/// Updates selection highlight to match graph selection.
		/// </summary>
		public void RefreshSelection()
		{
			var graphView = _window.GraphView;
			if (graphView == null) return;

			var selectedNodeId = graphView.selection
				.OfType<INodeDataProvider>()
				.Select(p => p.NodeData.Id)
				.FirstOrDefault();

			_currentSelectedNodeId = selectedNodeId;

			foreach (var kvp in _stateItems)
			{
				var isSelected = kvp.Key == selectedNodeId;
				kvp.Value.SetSelected(isSelected);
			}
		}

		private void OnGraphSelectionChanged(GraphNode selectedNode)
		{
			RefreshSelection();
		}

		private void OnFilterChanged(ChangeEvent<Enum> evt)
		{
			_currentFilter = (StateListFilter)evt.newValue;
			ApplyFilters();
		}

		private StateListItem CreateStateListItem(GraphNode node, string displayName)
		{
			var item = new VisualElement();
			item.AddToClassList("state-list-item");
			item.style.flexDirection = FlexDirection.Row;
			item.style.alignItems = Align.Center;
			item.style.paddingLeft = 6;
			item.style.paddingRight = 4;
			item.style.paddingTop = 4;
			item.style.paddingBottom = 4;
			item.style.marginBottom = 2;
			item.style.borderTopLeftRadius = 4;
			item.style.borderTopRightRadius = 4;
			item.style.borderBottomLeftRadius = 4;
			item.style.borderBottomRightRadius = 4;
			item.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);

			// Selected indicator (left border)
			var selectedIndicator = new VisualElement();
			selectedIndicator.style.position = Position.Absolute;
			selectedIndicator.style.left = 0;
			selectedIndicator.style.top = 0;
			selectedIndicator.style.bottom = 0;
			selectedIndicator.style.width = 3;
			selectedIndicator.style.backgroundColor = new Color(0.3f, 0.6f, 1f);
			selectedIndicator.style.borderTopLeftRadius = 4;
			selectedIndicator.style.borderBottomLeftRadius = 4;
			selectedIndicator.style.display = DisplayStyle.None;
			item.Add(selectedIndicator);

			// Type indicator (colored dot)
			var typeIndicator = new VisualElement();
			typeIndicator.style.width = 8;
			typeIndicator.style.height = 8;
			typeIndicator.style.marginRight = 6;
			typeIndicator.style.borderTopLeftRadius = 4;
			typeIndicator.style.borderTopRightRadius = 4;
			typeIndicator.style.borderBottomLeftRadius = 4;
			typeIndicator.style.borderBottomRightRadius = 4;
			typeIndicator.style.backgroundColor = GetNodeTypeColor(node.NodeType);
			item.Add(typeIndicator);

			// Text container for name and type
			var textContainer = new VisualElement();
			textContainer.style.flexGrow = 1;
			textContainer.style.overflow = Overflow.Hidden;
			item.Add(textContainer);

			// Name label
			var nameLabel = new Label(displayName);
			nameLabel.style.overflow = Overflow.Hidden;
			nameLabel.style.textOverflow = TextOverflow.Ellipsis;
			nameLabel.style.whiteSpace = WhiteSpace.NoWrap;
			nameLabel.style.fontSize = 11;
			nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			textContainer.Add(nameLabel);

			// Type label (below name)
			var typeName = GetNodeTypeDisplayName(node);
			var typeLabel = new Label(typeName);
			typeLabel.style.fontSize = 9;
			typeLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
			textContainer.Add(typeLabel);

			// Connection count (for states and sub-machines)
			if (node.NodeType is GraphNodeType.State or GraphNodeType.SubStateMachine)
			{
				var connContainer = new VisualElement();
				connContainer.style.flexDirection = FlexDirection.Row;
				connContainer.style.alignItems = Align.Center;
				connContainer.style.marginLeft = 4;
				item.Add(connContainer);

				var inCount = GetInConnectionCount(node);
				var outCount = GetOutConnectionCount(node);

				if (inCount > 0 || outCount > 0)
				{
					var connLabel = new Label($"{inCount}\u2192{outCount}");
					connLabel.style.fontSize = 9;
					connLabel.style.color = new Color(0.4f, 0.4f, 0.4f);
					connLabel.tooltip = $"Incoming: {inCount}, Outgoing: {outCount}";
					connContainer.Add(connLabel);
				}
			}

			// Hover effect
			item.RegisterCallback<MouseEnterEvent>(evt =>
			{
				item.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
			});

			item.RegisterCallback<MouseLeaveEvent>(evt =>
			{
				item.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
			});

			// Click to select and frame
			item.RegisterCallback<ClickEvent>(evt =>
			{
				_window.GraphView?.SelectNode(node.Id);
				_window.GraphView?.CenterOnState(node.Id, _window.GraphView.UseLerpAnimation);
				evt.StopPropagation();
			});

			return new StateListItem
			{
				Node = node,
				Element = item,
				SelectedIndicator = selectedIndicator
			};
		}

		private void OnSearchChanged(ChangeEvent<string> evt)
		{
			ApplyFilters();
		}

		/// <summary>
		/// Builds the connection count cache for all nodes in a single pass.
		/// </summary>
		private void BuildConnectionCountCache()
		{
			_connectionCountCache.Clear();

			if (_asset == null) return;

			// Single pass through all connections to count in/out for each node
			foreach (var connection in _asset.Connections)
			{
				// Count outgoing connection
				if (!string.IsNullOrEmpty(connection.SourceNodeId))
				{
					if (!_connectionCountCache.TryGetValue(connection.SourceNodeId, out var sourceCounts))
					{
						sourceCounts = (0, 0);
					}
					_connectionCountCache[connection.SourceNodeId] = (sourceCounts.inCount, sourceCounts.outCount + 1);
				}

				// Count incoming connection
				if (!string.IsNullOrEmpty(connection.TargetNodeId))
				{
					if (!_connectionCountCache.TryGetValue(connection.TargetNodeId, out var targetCounts))
					{
						targetCounts = (0, 0);
					}
					_connectionCountCache[connection.TargetNodeId] = (targetCounts.inCount + 1, targetCounts.outCount);
				}
			}
		}

		private int GetInConnectionCount(GraphNode node)
		{
			if (_connectionCountCache.TryGetValue(node.Id, out var counts))
			{
				return counts.inCount;
			}
			return 0;
		}

		private int GetOutConnectionCount(GraphNode node)
		{
			if (_connectionCountCache.TryGetValue(node.Id, out var counts))
			{
				return counts.outCount;
			}
			return 0;
		}

		private static string GetNodeDisplayName(GraphNode node)
		{
			// Use the node's display name or fall back to type name
			if (!string.IsNullOrEmpty(node.DisplayName))
			{
				return node.DisplayName;
			}

			// For states, try to get the state class name
			if (node is StateNode stateNode && stateNode.StateType != null)
			{
				return stateNode.StateType.Name;
			}

			return node.NodeType.ToString();
		}

		private static string GetNodeTypeDisplayName(GraphNode node)
		{
			if (node is StateNode stateNode && stateNode.StateType != null)
			{
				// Remove "State" suffix for cleaner display
				var typeName = stateNode.StateType.Name;
				if (typeName.EndsWith("State") && typeName.Length > 5)
				{
					return typeName.Substring(0, typeName.Length - 5);
				}

				return typeName;
			}

			return node.NodeType.ToString();
		}

		private static int GetNodeTypeSortOrder(GraphNodeType type)
		{
			return type switch
			{
				GraphNodeType.Entry => 0,
				GraphNodeType.State => 1,
				GraphNodeType.SubStateMachine => 2,
				GraphNodeType.PortalOut => 3,
				GraphNodeType.PortalIn => 4,
				GraphNodeType.Exit => 5,
				GraphNodeType.BlackboardVariable => 6,
				_ => 99
			};
		}

		private static Color GetNodeTypeColor(GraphNodeType type)
		{
			return type switch
			{
				GraphNodeType.Entry => new Color(0.4f, 0.8f, 0.4f),       // Green
				GraphNodeType.Exit => new Color(0.8f, 0.4f, 0.4f),        // Red
				GraphNodeType.State => new Color(0.5f, 0.6f, 0.8f),       // Blue-gray
				GraphNodeType.SubStateMachine => new Color(0.6f, 0.4f, 0.8f), // Purple
				GraphNodeType.PortalOut => new Color(0.4f, 0.7f, 0.9f),   // Cyan
				GraphNodeType.PortalIn => new Color(0.4f, 0.7f, 0.9f),    // Cyan
				GraphNodeType.BlackboardVariable => new Color(0.8f, 0.7f, 0.4f), // Yellow
				_ => new Color(0.5f, 0.5f, 0.5f)
			};
		}

		private class StateListItem
		{
			public GraphNode Node { get; set; }
			public VisualElement Element { get; set; }
			public VisualElement SelectedIndicator { get; set; }

			public void SetSelected(bool selected)
			{
				if (SelectedIndicator != null)
				{
					SelectedIndicator.style.display = selected ? DisplayStyle.Flex : DisplayStyle.None;
				}
			}
		}
	}
}
