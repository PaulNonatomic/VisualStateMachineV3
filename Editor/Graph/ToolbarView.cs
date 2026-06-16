using System;
using System.Collections.Generic;
using System.Linq;
using Nonatomic.VSM.Editor.Graph.Navigation;
using Nonatomic.VSM.Editor.Graph.Settings;
using Nonatomic.VSM.Graph;
using Nonatomic.VSM.Editor.Layout;
using Nonatomic.VSM.Editor.Runtime;
using Nonatomic.VSM.Editor.Services;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Toolbar view for the state machine graph editor.
	/// </summary>
	public class ToolbarView : Toolbar
	{
		private readonly StateMachineGraphWindow _window;

		private ToolbarButton _saveButton;
		private ToolbarButton _centerButton;
		private ToolbarButton _backButton;
		private BreadcrumbNavigation _breadcrumbNavigation;
		private ToolbarMenu _viewOptionsMenu;
		private ToolbarMenu _settingsMenu;
		private ToolbarMenu _windowsMenu;
		private ToolbarMenu _bookmarkMenu;
		private ToolbarMenu _layoutMenu;
		private ToolbarToggle _splitViewToggle;
		private ToolbarToggle _linkScrollToggle;
		private ToolbarMenu _instanceMenu;
		private VisualElement _instanceContainer;

		private RuntimeInstanceInfo _selectedInstance;
		private List<RuntimeInstanceInfo> _availableInstances = new List<RuntimeInstanceInfo>();

		public ToolbarView(StateMachineGraphWindow window)
		{
			_window = window;

			BuildToolbar();
		}

		private const string ToolbarIconPath = "Icons/Toolbar/";

		private Image CreateToolbarIcon(string iconName)
		{
			var texture = ImageService.LoadTexture(ToolbarIconPath + iconName);
			if (texture == null) return null;

			var icon = new Image { image = texture };
			icon.style.width = 16;
			icon.style.height = 16;
			icon.style.marginLeft = 2;
			icon.style.marginRight = 2;
			icon.style.alignSelf = Align.Center;
			icon.pickingMode = PickingMode.Ignore;
			return icon;
		}

		private void BuildToolbar()
		{
			name = "vsm-toolbar";

			// Back button for sub-state machine navigation
			_backButton = new ToolbarButton(OnBackClicked)
			{
				name = "vsm-toolbar__back-button",
				text = "",
				tooltip = "Navigate back to parent state machine"
			};
			_backButton.Add(CreateToolbarIcon("nav-arrow-left"));
			_backButton.SetEnabled(false);
			Add(_backButton);

			// Asset name / breadcrumb trail
			_breadcrumbNavigation = new BreadcrumbNavigation();
			_breadcrumbNavigation.name = "vsm-toolbar__breadcrumb";
			_breadcrumbNavigation.style.marginLeft = 8;
			_breadcrumbNavigation.style.marginRight = 8;
			_breadcrumbNavigation.OnBreadcrumbClicked += OnBreadcrumbClicked;
			_breadcrumbNavigation.OnRecentGraphSelected += OnRecentGraphSelected;
			Add(_breadcrumbNavigation);

			// Runtime instance selector (only visible during play mode)
			_instanceContainer = new VisualElement();
			_instanceContainer.name = "vsm-toolbar__instance-selector";
			_instanceContainer.style.flexDirection = FlexDirection.Row;
			_instanceContainer.style.alignItems = Align.Center;
			_instanceContainer.style.marginLeft = 8;
			_instanceContainer.style.display = DisplayStyle.None;

			var instanceLabel = new Label("Instance:");
			instanceLabel.name = "vsm-toolbar__instance-label";
			instanceLabel.style.marginRight = 4;
			instanceLabel.style.color = new Color(0.7f, 0.9f, 0.7f);
			_instanceContainer.Add(instanceLabel);

			_instanceMenu = new ToolbarMenu { text = "Select...", name = "vsm-toolbar__instance-menu" };
			_instanceMenu.style.minWidth = 150;
			_instanceContainer.Add(_instanceMenu);

			Add(_instanceContainer);

			// ---- Frequent view actions: kept on the LEFT, next to navigation ----

			// Center / frame-all button
			_centerButton = new ToolbarButton(OnCenterClicked)
			{
				name = "vsm-toolbar__center-button",
				text = "",
				tooltip = "Frame all nodes"
			};
			_centerButton.Add(CreateToolbarIcon("center-align"));
			Add(_centerButton);

			// Search button
			var searchButton = new ToolbarButton(() => _window.NodeSearchBar?.Toggle())
			{
				name = "vsm-toolbar__search-button",
				text = "",
				tooltip = "Search nodes in graph (Ctrl+F)"
			};
			searchButton.Add(CreateToolbarIcon("search"));
			Add(searchButton);

			// Layout menu
			_layoutMenu = new ToolbarMenu { text = "", name = "vsm-toolbar__layout-menu" };
			_layoutMenu.tooltip = "Layout options";
			_layoutMenu.Insert(0, CreateToolbarIcon("view-structure-up"));
			_layoutMenu.menu.AppendAction("Auto Layout (Hierarchical)", action => ApplyLayout(true));
			_layoutMenu.menu.AppendAction("Grid Layout", action => ApplyLayout(false));
			Add(_layoutMenu);

			// Flexible spacer: splits the bar into a left (navigation/view) group and a right
			// (menus/panels/file) group, instead of marooning every control on the far right.
			var spacer = new VisualElement();
			spacer.name = "vsm-toolbar__spacer";
			spacer.style.flexGrow = 1;
			Add(spacer);

			// ---- View menu: framing actions + view behaviour (Frame All is the center button) ----
			_viewOptionsMenu = new ToolbarMenu { text = "", name = "vsm-toolbar__view-menu" };
			_viewOptionsMenu.tooltip = "View";
			_viewOptionsMenu.Insert(0, CreateToolbarIcon("eye"));
			_viewOptionsMenu.menu.AppendAction("Frame Selection", action => { _window.GraphView?.FrameSelection(); });
			_viewOptionsMenu.menu.AppendAction("Frame Entry Node", action => { _window.GraphView?.FrameEntryNode(); });
			_viewOptionsMenu.menu.AppendSeparator();
			AppendToggleAction(_viewOptionsMenu.menu, "Auto-center on Active State",
				() => _window.GraphView?.AutoCenterOnActiveState == true,
				() => { if (_window.GraphView != null) _window.GraphView.AutoCenterOnActiveState = !_window.GraphView.AutoCenterOnActiveState; });
			AppendToggleAction(_viewOptionsMenu.menu, "Smooth Centering",
				() => _window.GraphView?.UseLerpAnimation == true,
				() => { if (_window.GraphView != null) _window.GraphView.UseLerpAnimation = !_window.GraphView.UseLerpAnimation; });
			AppendToggleAction(_viewOptionsMenu.menu, "Auto-frame Entry on Open",
				() => _window.GraphView?.AutoFrameEntryOnOpen == true,
				() => { if (_window.GraphView != null) _window.GraphView.AutoFrameEntryOnOpen = !_window.GraphView.AutoFrameEntryOnOpen; });
			AppendToggleAction(_viewOptionsMenu.menu, "Auto-frame Search Result",
				() => _window.StateListPanel?.AutoFrameSearchResult == true,
				() => { if (_window.StateListPanel != null) _window.StateListPanel.AutoFrameSearchResult = !_window.StateListPanel.AutoFrameSearchResult; });
			AppendToggleAction(_viewOptionsMenu.menu, "Follow Sub-State Machines",
				() => _window.FollowSubStateMachines,
				() => _window.FollowSubStateMachines = !_window.FollowSubStateMachines);
			Add(_viewOptionsMenu);

			// ---- Settings menu: persistent display preferences ----
			_settingsMenu = new ToolbarMenu { text = "Settings", name = "vsm-toolbar__settings-menu" };
			_settingsMenu.tooltip = "Display settings";
			AppendToggleAction(_settingsMenu.menu, "Connecting Line Style/Wire (Bezier)",
				() => EdgeSettings.Instance.RendererType == EdgeRendererType.Wire,
				() => SetEdgeRendererType(EdgeRendererType.Wire));
			AppendToggleAction(_settingsMenu.menu, "Connecting Line Style/Default (Unity)",
				() => EdgeSettings.Instance.RendererType == EdgeRendererType.Default,
				() => SetEdgeRendererType(EdgeRendererType.Default));
			AppendToggleAction(_settingsMenu.menu, "Port Colors/Type Colors",
				() => PortSettings.Instance.ColorMode == PortColorMode.TypeBased,
				() => SetPortColorMode(PortColorMode.TypeBased));
			AppendToggleAction(_settingsMenu.menu, "Port Colors/State Colors",
				() => PortSettings.Instance.ColorMode == PortColorMode.StateBased,
				() => SetPortColorMode(PortColorMode.StateBased));
			AppendToggleAction(_settingsMenu.menu, "Show Type Icons",
				() => PortSettings.Instance.ShowTypeIcons,
				() => ToggleTypeIcons());
			_settingsMenu.menu.AppendSeparator();
			AppendToggleAction(_settingsMenu.menu, "Debug Mode",
				() => StateMachineGraphView.DebugMode,
				() =>
				{
					StateMachineGraphView.DebugMode = !StateMachineGraphView.DebugMode;
					UnityEngine.Debug.Log($"[VSM] Debug Mode: {(StateMachineGraphView.DebugMode ? "ENABLED" : "DISABLED")}");
				});
			Add(_settingsMenu);

			// Bookmark menu (dynamically populated on click)
			_bookmarkMenu = new ToolbarMenu { text = "", name = "vsm-toolbar__bookmark-menu" };
			_bookmarkMenu.tooltip = "Bookmarks";
			_bookmarkMenu.Insert(0, CreateToolbarIcon("bookmark-book"));
			// Use TrickleDown to ensure menu is rebuilt BEFORE ToolbarMenu's internal click handling shows the dropdown
			_bookmarkMenu.RegisterCallback<MouseDownEvent>(evt => RebuildBookmarkMenu(), TrickleDown.TrickleDown);
			// Initialize with baseline content
			RebuildBookmarkMenu();
			Add(_bookmarkMenu);

			// Separator before the panels menu
			Add(new ToolbarSpacer());

			// ---- Windows menu: show/hide editor panels ----
			// Replaces six look-alike monochrome toggles (and the cryptic "TL"/"BP" text buttons)
			// with one labelled, self-documenting dropdown; rebuilt on open so it reflects live
			// visibility and only offers the Debug Timeline during play mode.
			_windowsMenu = new ToolbarMenu { text = "Windows", name = "vsm-toolbar__windows-menu" };
			_windowsMenu.tooltip = "Show or hide editor panels";
			_windowsMenu.RegisterCallback<MouseDownEvent>(evt => RebuildWindowsMenu(), TrickleDown.TrickleDown);
			RebuildWindowsMenu();
			Add(_windowsMenu);

			// Separator before split view controls
			Add(new ToolbarSpacer());

			// Split View toggle
			_splitViewToggle = new ToolbarToggle { text = "", name = "vsm-toolbar__splitview-toggle" };
			_splitViewToggle.tooltip = "Toggle split view to see two areas of the graph simultaneously";
			_splitViewToggle.Add(CreateToolbarIcon("view-columns-2"));
			_splitViewToggle.value = EditorPrefs.GetBool("VSM.SplitView.Enabled", false);
			_splitViewToggle.RegisterValueChangedCallback(evt =>
			{
				_window.SetSplitViewEnabled(evt.newValue);
			});
			Add(_splitViewToggle);

			// Link Scroll toggle (only visible when split view is enabled)
			_linkScrollToggle = new ToolbarToggle { text = "", name = "vsm-toolbar__linkscroll-toggle" };
			_linkScrollToggle.tooltip = "Synchronize pan/zoom between split views";
			_linkScrollToggle.Add(CreateToolbarIcon("link"));
			_linkScrollToggle.value = EditorPrefs.GetBool("VSM.SplitView.LinkScroll", false);
			_linkScrollToggle.RegisterValueChangedCallback(evt =>
			{
				_window.ToggleLinkScroll();
			});
			_linkScrollToggle.style.display = _splitViewToggle.value ? DisplayStyle.Flex : DisplayStyle.None;
			Add(_linkScrollToggle);

			// Save button
			_saveButton = new ToolbarButton(OnSaveClicked)
			{
				name = "vsm-toolbar__save-button",
				text = "",
				tooltip = "Save the current graph asset"
			};
			_saveButton.Add(CreateToolbarIcon("floppy-disk"));
			Add(_saveButton);
		}

		private void OnBackClicked()
		{
			_window.GraphView?.NavigateBack();
			UpdateNavigationUI();
		}

		private void OnBreadcrumbClicked(int level)
		{
			_window.GraphView?.NavigateToLevel(level);
			UpdateNavigationUI();
		}

		private void OnRecentGraphSelected(StateMachineGraph graph)
		{
			if (graph != null)
			{
				_window.LoadAsset(graph);
			}
		}

		private void OnCenterClicked()
		{
			_window.GraphView?.FrameAll();
		}

		private void OnSaveClicked()
		{
			_window.SaveAsset();
		}

		private void ApplyLayout(bool hierarchical)
		{
			var asset = _window.GraphView?.CurrentAsset ?? _window.TargetAsset;
			if (asset == null) return;

			Undo.RecordObject(asset, "Auto Layout Graph");

			if (hierarchical)
			{
				// First pass: layout using current rendered sizes
				var nodeSizes = _window.GraphView?.GetNodeSizes();
				GraphLayoutService.AutoLayout(asset, nodeSizes);
				EditorUtility.SetDirty(asset);

				// Refresh the view, then schedule a second pass
				// after nodes have rendered at their new positions
				_window.GraphView?.PopulateFromAsset(asset);
				_window.GraphView?.schedule.Execute(() =>
				{
					var refinedSizes = _window.GraphView?.GetNodeSizes();
					GraphLayoutService.AutoLayout(asset, refinedSizes);
					EditorUtility.SetDirty(asset);
					_window.GraphView?.PopulateFromAsset(asset);
					_window.GraphView?.schedule.Execute(() =>
					{
						_window.GraphView?.FrameOrigin();
					}).ExecuteLater(50);
				}).ExecuteLater(100);
			}
			else
			{
				GraphLayoutService.GridLayout(asset);
				EditorUtility.SetDirty(asset);
				_window.GraphView?.PopulateFromAsset(asset);
				_window.GraphView?.FrameAll();
			}
		}

		private void SetEdgeRendererType(EdgeRendererType type)
		{
			if (EdgeSettings.Instance.RendererType == type) return;

			EdgeSettings.Instance.RendererType = type;

			// Reload the graph to recreate edges with the new renderer
			var asset = _window.TargetAsset;
			if (asset != null)
			{
				_window.LoadAsset(asset);
			}
		}

		private void SetPortColorMode(PortColorMode mode)
		{
			if (PortSettings.Instance.ColorMode == mode) return;

			PortSettings.Instance.ColorMode = mode;

			// Reload the graph to apply new port colors
			var asset = _window.TargetAsset;
			if (asset != null)
			{
				_window.LoadAsset(asset);
			}
		}

		private void ToggleTypeIcons()
		{
			PortSettings.Instance.ShowTypeIcons = !PortSettings.Instance.ShowTypeIcons;

			// Reload the graph to apply the setting
			var asset = _window.TargetAsset;
			if (asset != null)
			{
				_window.LoadAsset(asset);
			}
		}

		private void AppendToggleAction(DropdownMenu menu, string name, Func<bool> getter, Action toggle)
		{
			menu.AppendAction(name,
				action => toggle(),
				action => getter()
					? DropdownMenuAction.Status.Checked
					: DropdownMenuAction.Status.Normal);
		}

		/// <summary>
		/// Rebuilds the Windows panel menu. Called on open so each item's checkmark reflects the
		/// panel's live visibility, and the Debug Timeline is only listed during play mode.
		/// </summary>
		private void RebuildWindowsMenu()
		{
			_windowsMenu.menu.ClearItems();

			AppendToggleAction(_windowsMenu.menu, "Blackboard",
				() => _window.BlackboardView?.IsVisible == true,
				() => { if (_window.BlackboardView != null) _window.BlackboardView.SetVisible(!_window.BlackboardView.IsVisible); });

			AppendToggleAction(_windowsMenu.menu, "Inspector",
				() => _window.InspectorView?.IsVisible == true,
				() => { if (_window.InspectorView != null) _window.InspectorView.SetVisible(!_window.InspectorView.IsVisible); });

			AppendToggleAction(_windowsMenu.menu, "Mini-Map",
				() => _window.MiniMapView?.IsVisible == true,
				() => { if (_window.MiniMapView != null) _window.MiniMapView.IsVisible = !_window.MiniMapView.IsVisible; });

			AppendToggleAction(_windowsMenu.menu, "State List",
				() => _window.StateListPanel?.IsVisible == true,
				() => { if (_window.StateListPanel != null) _window.StateListPanel.SetVisible(!_window.StateListPanel.IsVisible); });

			AppendToggleAction(_windowsMenu.menu, "Breakpoints",
				() => _window.BreakpointListPanel?.IsVisible == true,
				() => { if (_window.BreakpointListPanel != null) _window.BreakpointListPanel.SetVisible(!_window.BreakpointListPanel.IsVisible); });

			if (EditorApplication.isPlaying)
			{
				AppendToggleAction(_windowsMenu.menu, "Debug Timeline",
					() => _window.DebugTimelinePanel?.IsVisible == true,
					() => { if (_window.DebugTimelinePanel != null) _window.DebugTimelinePanel.SetVisible(!_window.DebugTimelinePanel.IsVisible); });
			}
		}

		private void RebuildBookmarkMenu()
		{
			_bookmarkMenu.menu.ClearItems();

			// Add bookmark action
			_bookmarkMenu.menu.AppendAction("Add Bookmark...", action => ShowAddBookmarkDialog());

			var graphView = _window.GraphView;
			var bookmarks = graphView?.GetBookmarks();

			if (bookmarks != null && bookmarks.Count > 0)
			{
				_bookmarkMenu.menu.AppendSeparator();

				foreach (var bookmark in bookmarks)
				{
					var capturedBookmark = bookmark;
					_bookmarkMenu.menu.AppendAction(
						bookmark.Name,
						action => graphView.GoToBookmark(capturedBookmark)
					);
				}

				_bookmarkMenu.menu.AppendSeparator();
				_bookmarkMenu.menu.AppendAction("Manage Bookmarks...", action => ShowManageBookmarksDialog());
			}
		}

		private void ShowAddBookmarkDialog()
		{
			var graphView = _window.GraphView;
			if (graphView == null) return;

			// Simple popup to enter bookmark name
			var popup = new BookmarkNamePopup(name =>
			{
				if (!string.IsNullOrWhiteSpace(name))
				{
					graphView.CreateBookmark(name);
				}
			});
			UnityEditor.PopupWindow.Show(_bookmarkMenu.worldBound, popup);
		}

		private void ShowManageBookmarksDialog()
		{
			var graphView = _window.GraphView;
			if (graphView == null) return;

			var popup = new ManageBookmarksPopup(graphView);
			UnityEditor.PopupWindow.Show(_bookmarkMenu.worldBound, popup);
		}

		public void UpdateNavigationUI()
		{
			var graphView = _window.GraphView;
			_backButton.SetEnabled(graphView?.CanNavigateBack ?? false);

			// Use CurrentAsset from graph view, or fall back to TargetAsset from window
			var asset = graphView?.CurrentAsset ?? _window.TargetAsset;
			if (asset != null)
			{
				var stack = graphView?.GetNavigationStack();
				if (stack != null && stack.Count > 0)
				{
					// Convert stack to list (top to bottom = most recent to oldest)
					var stackList = stack.ToList();
					stackList.Reverse(); // Now oldest to newest (root first)
					_breadcrumbNavigation.SetPath(stackList, asset);
				}
				else
				{
					_breadcrumbNavigation.SetPath(new List<StateMachineGraph>(), asset);
				}
			}
			else
			{
				_breadcrumbNavigation.Clear();
			}

			UpdateInstanceSelector();
		}

		public RuntimeInstanceInfo SelectedInstance => _selectedInstance;

		public void SelectInstance(RuntimeInstanceInfo instance)
		{
			_selectedInstance = instance;
			UpdateInstanceSelector();
		}

		public void UpdateInstanceSelector()
		{
			// Use the currently displayed graph, not the root target asset
			// This allows proper instance selection when navigated into sub-state machines
			var asset = _window.GraphView?.CurrentAsset ?? _window.TargetAsset;

			// Get all running instances for this graph
			_availableInstances.Clear();
			if (asset != null && Application.isPlaying)
			{
				_availableInstances.AddRange(
					RuntimeInstanceTracker.GetInstancesForGraph(asset)
						.Where(i => i.Status == RuntimeInstanceState.Running)
				);
			}

			// Show/hide the instance selector based on whether there are instances
			var hasInstances = _availableInstances.Count > 0;
			_instanceContainer.style.display = hasInstances ? DisplayStyle.Flex : DisplayStyle.None;

			if (!hasInstances)
			{
				_selectedInstance = null;
				return;
			}

			// Auto-select first instance if none selected or selected is gone
			if (_selectedInstance == null || !_availableInstances.Contains(_selectedInstance))
			{
				_selectedInstance = _availableInstances.FirstOrDefault();
			}

			// Update menu text
			if (_selectedInstance != null)
			{
				var ownerName = _selectedInstance.Owner != null ? _selectedInstance.Owner.name : "Unknown";
				_instanceMenu.text = _availableInstances.Count > 1
					? $"{ownerName} ({_availableInstances.Count})"
					: ownerName;
			}
			else
			{
				_instanceMenu.text = "No instances";
			}

			// Rebuild menu items
			_instanceMenu.menu.ClearItems();

			foreach (var instance in _availableInstances)
			{
				var ownerName = instance.Owner != null ? instance.Owner.name : "Unknown";
				var stateName = instance.CurrentStateName ?? "?";
				var menuText = $"{ownerName} [{stateName}]";

				var capturedInstance = instance;
				_instanceMenu.menu.AppendAction(
					menuText,
					action => OnInstanceSelected(capturedInstance),
					action => capturedInstance == _selectedInstance
						? DropdownMenuAction.Status.Checked
						: DropdownMenuAction.Status.Normal
				);
			}

			if (_availableInstances.Count > 1)
			{
				_instanceMenu.menu.AppendSeparator();
				_instanceMenu.menu.AppendAction("Select in Hierarchy", action =>
				{
					if (_selectedInstance?.Owner != null)
					{
						Selection.activeGameObject = _selectedInstance.Owner;
						EditorGUIUtility.PingObject(_selectedInstance.Owner);
					}
				});
			}
		}

		private void OnInstanceSelected(RuntimeInstanceInfo instance)
		{
			_selectedInstance = instance;
			UpdateInstanceSelector();

			// Ping the instance owner in the hierarchy
			if (instance?.Owner != null)
			{
				EditorGUIUtility.PingObject(instance.Owner);
			}

			// Notify the window to update the highlighted state
			_window.OnInstanceSelectionChanged(instance);
		}

		public void SetTimelineToggleValue(bool value)
		{
			// Retained for API compatibility — the Debug Timeline is toggled from the Windows menu.
		}

		/// <summary>
		/// Re-syncs the split-view toggles when their state changes outside the toolbar. Panel
		/// visibility is now surfaced by the Windows menu (which re-reads live state when opened),
		/// so the individual panels no longer need toggle syncing here.
		/// </summary>
		public void SyncPanelToggles()
		{
			if (_splitViewToggle != null && _window.SplitViewContainer != null)
			{
				_splitViewToggle.SetValueWithoutNotify(_window.SplitViewContainer.IsSplitEnabled);
				_linkScrollToggle.style.display = _window.SplitViewContainer.IsSplitEnabled
					? DisplayStyle.Flex
					: DisplayStyle.None;
			}

			if (_linkScrollToggle != null && _window.SplitViewContainer != null)
			{
				_linkScrollToggle.SetValueWithoutNotify(_window.SplitViewContainer.IsLinkScrollEnabled);
			}
		}

		/// <summary>
		/// Retained for API compatibility — the Debug Timeline now lives in the Windows menu
		/// (shown only during play mode), so there is no separate toggle to show or hide.
		/// </summary>
		public void SetTimelineToggleVisible(bool visible)
		{
		}
	}
}
