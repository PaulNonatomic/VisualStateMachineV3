using System.Collections.Generic;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Visual representation of a graph region for organizing nodes.
	/// </summary>
	public class GraphRegionView : Group
	{
		private const long SaveDebounceMs = 500;

		public GraphRegion RegionData { get; }

		private readonly StateMachineGraphView _graphView;
		private TextField _titleField;
		private bool _isRenaming;
		private bool _suppressGeometrySave;
		private IVisualElementScheduledItem _pendingSave;

		public GraphRegionView(GraphRegion regionData, StateMachineGraphView graphView)
		{
			RegionData = regionData;
			_graphView = graphView;

			// Suppress geometry saves from the start during load so that the
			// GeometryChangedEvent fired when AddElement places us in the visual
			// tree cannot overwrite the saved bounds before the restore loop runs. (Suppress geometry saves from region constructor during load)
			_suppressGeometrySave = graphView.IsLoadingAsset;

			title = regionData.Title;

			AddToClassList("graph-region");

			// Set position and size
			SetPosition(regionData.Bounds);

			// Apply region color
			ApplyRegionColor(regionData.Color);

			// Setup title editing
			SetupTitleEditing();

			// Add resize handles
			capabilities |= Capabilities.Resizable;

			// Register for geometry changes to save bounds
			RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

			// Context menu
			RegisterCallback<ContextualMenuPopulateEvent>(OnContextMenuPopulate);
		}

		private void OnContextMenuPopulate(ContextualMenuPopulateEvent evt)
		{
			evt.menu.AppendAction(
				"Convert to Sub-State Machine",
				_ => _graphView.ConvertRegionToSubStateMachine(RegionData.Id),
				_ => RegionData.ContainedNodeIds.Count > 0
					? DropdownMenuAction.Status.Normal
					: DropdownMenuAction.Status.Disabled);
			evt.menu.AppendSeparator();

			evt.menu.AppendAction(
				"Delete Group",
				_ => _graphView.RemoveRegion(RegionData.Id),
				DropdownMenuAction.AlwaysEnabled);
		}

		private void ApplyRegionColor(Color color)
		{
			style.backgroundColor = color;

			// Style the header
			var headerContainer = this.Q("headerContainer");
			if (headerContainer != null)
			{
				var headerColor = new Color(
					Mathf.Min(color.r + 0.15f, 1f),
					Mathf.Min(color.g + 0.15f, 1f),
					Mathf.Min(color.b + 0.15f, 1f),
					Mathf.Min(color.a + 0.2f, 0.9f)
				);
				headerContainer.style.backgroundColor = headerColor;
			}
		}

		private void SetupTitleEditing()
		{
			// Double-click to rename
			RegisterCallback<MouseDownEvent>(evt =>
			{
				if (evt.clickCount == 2 && evt.button == 0)
				{
					StartRenaming();
					evt.StopPropagation();
				}
			});
		}

		private void StartRenaming()
		{
			if (_isRenaming) return;
			_isRenaming = true;

			var titleLabel = this.Q<Label>("titleLabel");
			if (titleLabel == null) return;

			// Hide the label
			titleLabel.style.display = DisplayStyle.None;

			// Create and show text field
			_titleField = new TextField();
			_titleField.value = RegionData.Title;
			_titleField.style.minWidth = 100;
			_titleField.AddToClassList("region-title-field");

			// Insert after the title label
			titleLabel.parent.Add(_titleField);
			_titleField.Focus();
			_titleField.SelectAll();

			// Handle completion
			_titleField.RegisterCallback<FocusOutEvent>(evt => FinishRenaming());
			_titleField.RegisterCallback<KeyDownEvent>(evt =>
			{
				if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
				{
					FinishRenaming();
					evt.StopPropagation();
				}
				else if (evt.keyCode == KeyCode.Escape)
				{
					CancelRenaming();
					evt.StopPropagation();
				}
			});
		}

		private void FinishRenaming()
		{
			if (!_isRenaming) return;

			var newTitle = _titleField?.value ?? RegionData.Title;
			if (!string.IsNullOrWhiteSpace(newTitle))
			{
				RegionData.Title = newTitle;
				title = newTitle;
				SaveChanges();
			}

			CleanupRenaming();
		}

		private void CancelRenaming()
		{
			CleanupRenaming();
		}

		private void CleanupRenaming()
		{
			_isRenaming = false;

			var titleLabel = this.Q<Label>("titleLabel");
			if (titleLabel != null)
			{
				titleLabel.style.display = DisplayStyle.Flex;
			}

			if (_titleField != null)
			{
				_titleField.RemoveFromHierarchy();
				_titleField = null;
			}
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			if (_suppressGeometrySave) return;
			if (_graphView.IsLoadingAsset) return;
			if (EditorApplication.isPlayingOrWillChangePlaymode) return;

			// Update region bounds from current position
			var currentRect = GetPosition();
			if (currentRect != RegionData.Bounds)
			{
				RegionData.Bounds = currentRect;
				SaveChangesDirtyOnly();
				DebounceSaveToDisk();
			}
		}

		/// <summary>
		/// Suppresses geometry change saves during load to prevent auto-layout
		/// from overwriting the saved region position.
		/// </summary>
		public void SuppressGeometrySave(bool suppress)
		{
			_suppressGeometrySave = suppress;
		}

		private void SaveChanges()
		{
			if (_graphView == null || _graphView.CurrentAsset == null) return;
			if (EditorApplication.isPlayingOrWillChangePlaymode) return;

			_graphView.CurrentAsset.UpdateRegion(RegionData);
			_graphView.MarkAssetDirty();
			AssetDatabase.SaveAssetIfDirty(_graphView.CurrentAsset);
		}

		private void SaveChangesDirtyOnly()
		{
			if (_graphView == null || _graphView.CurrentAsset == null) return;
			if (EditorApplication.isPlayingOrWillChangePlaymode) return;

			_graphView.CurrentAsset.UpdateRegion(RegionData);
			EditorUtility.SetDirty(_graphView.CurrentAsset);
		}

		private void DebounceSaveToDisk()
		{
			_pendingSave?.Pause();
			_pendingSave = schedule.Execute(() =>
			{
				if (_graphView == null || _graphView.CurrentAsset == null) return;
				if (EditorApplication.isPlayingOrWillChangePlaymode) return;
				AssetDatabase.SaveAssetIfDirty(_graphView.CurrentAsset);
			}).StartingIn(SaveDebounceMs);
		}

		/// <summary>
		/// Updates the region bounds when moved or resized.
		/// </summary>
		public void UpdateBounds(Rect newBounds)
		{
			RegionData.Bounds = newBounds;
			SetPosition(newBounds);
			SaveChanges();
		}

		/// <summary>
		/// Sets the region color.
		/// </summary>
		public void SetColor(Color color)
		{
			RegionData.Color = color;
			ApplyRegionColor(color);
			SaveChanges();
		}

		/// <summary>
		/// Called when elements are added to this group.
		/// </summary>
		protected override void OnElementsAdded(IEnumerable<GraphElement> elements)
		{
			// Skip base call during load to prevent Group.UpdateGeometryFromContent
			// from overriding the saved region position with auto-layout bounds
			if (!_graphView.IsLoadingAsset)
			{
				_suppressGeometrySave = false;
				base.OnElementsAdded(elements);
			}

			if (_graphView.IsLoadingAsset) return;

			foreach (var element in elements)
			{
				if (element is Node node)
				{
					var nodeId = GetNodeId(node);
					if (!string.IsNullOrEmpty(nodeId))
					{
						RegionData.AddNode(nodeId);
					}
				}
			}

			SaveChanges();
		}

		/// <summary>
		/// Called when elements are removed from this group.
		/// </summary>
		protected override void OnElementsRemoved(IEnumerable<GraphElement> elements)
		{
			// Skip base call during load/clear to prevent Group.UpdateGeometryFromContent
			// from recalculating bounds when nodes are removed during graph switching
			if (!_graphView.IsLoadingAsset)
			{
				_suppressGeometrySave = false;
				base.OnElementsRemoved(elements);
			}

			if (_graphView.IsLoadingAsset) return;

			foreach (var element in elements)
			{
				if (element is Node node)
				{
					var nodeId = GetNodeId(node);
					if (!string.IsNullOrEmpty(nodeId))
					{
						RegionData.RemoveNode(nodeId);
					}
				}
			}

			SaveChanges();
		}

		private string GetNodeId(Node node)
		{
			if (node is INodeDataProvider provider)
			{
				return provider.NodeData?.Id;
			}

			return null;
		}

		/// <summary>
		/// Adds nodes to this region by their IDs.
		/// Called when restoring region membership on load.
		/// </summary>
		public void RestoreContainedNodes(IReadOnlyDictionary<string, Node> nodeViews)
		{
			var nodesToAdd = new List<GraphElement>();

			foreach (var nodeId in RegionData.ContainedNodeIds)
			{
				if (nodeViews.TryGetValue(nodeId, out var nodeView))
				{
					nodesToAdd.Add(nodeView);
				}
			}

			if (nodesToAdd.Count > 0)
			{
				AddElements(nodesToAdd);
			}
		}
	}
}
