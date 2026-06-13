using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Floating inspector panel for editing group (region) properties.
	/// </summary>
	public class GroupInspectorPanel : FloatingPanel
	{
		private readonly StateMachineGraphWindow _window;
		private GraphRegionView _selectedRegion;
		private Label _noSelectionLabel;

		public GroupInspectorPanel(StateMachineGraphWindow window)
			: base("GroupInspector", "Group Inspector", new Vector2(-10, 370), new Vector2(260, 200))
		{
			_window = window;

			AddToClassList("group-inspector-panel");

			RegisterCallback<GeometryChangedEvent>(OnFirstLayout);

			BuildContent();
		}

		private bool _hasInitializedPosition;

		private void OnFirstLayout(GeometryChangedEvent evt)
		{
			if (_hasInitializedPosition) return;
			_hasInitializedPosition = true;

			if (parent != null && resolvedStyle.left < 0)
			{
				var parentWidth = parent.resolvedStyle.width;
				style.left = parentWidth - resolvedStyle.width - 20;
			}
		}

		private void BuildContent()
		{
			_noSelectionLabel = new Label("No group selected");
			_noSelectionLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
			_noSelectionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
			_noSelectionLabel.style.marginTop = 20;
			ContentContainer.Add(_noSelectionLabel);
		}

		/// <summary>
		/// Shows the inspector for the specified group.
		/// </summary>
		public void ShowGroupInspector(GraphRegionView regionView)
		{
			_selectedRegion = regionView;
			ContentContainer.Clear();

			if (regionView == null)
			{
				ContentContainer.Add(_noSelectionLabel);
				return;
			}

			var headerLabel = new Label("Group");
			headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			headerLabel.style.marginBottom = 8;
			ContentContainer.Add(headerLabel);

			// Title field
			var titleField = new TextField("Name");
			titleField.value = regionView.RegionData.Title;
			titleField.RegisterValueChangedCallback(evt =>
			{
				if (string.IsNullOrWhiteSpace(evt.newValue)) return;

				RecordUndo("Change Group Name");
				regionView.RegionData.Title = evt.newValue;
				regionView.title = evt.newValue;
				MarkDirty();
			});
			ContentContainer.Add(titleField);

			// Color field
			var colorField = new ColorField("Color");
			colorField.value = regionView.RegionData.Color;
			colorField.showAlpha = true;
			colorField.RegisterValueChangedCallback(evt =>
			{
				RecordUndo("Change Group Color");
				regionView.SetColor(evt.newValue);
			});
			ContentContainer.Add(colorField);

			// Node count info
			var nodeCount = regionView.RegionData.ContainedNodeIds.Count;
			var infoLabel = new Label($"Contains {nodeCount} node{(nodeCount == 1 ? "" : "s")}");
			infoLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
			infoLabel.style.marginTop = 12;
			ContentContainer.Add(infoLabel);
		}

		/// <summary>
		/// Clears the inspector when no group is selected.
		/// </summary>
		public void ClearInspector()
		{
			ShowGroupInspector(null);
		}

		private void RecordUndo(string name)
		{
			if (_window.TargetAsset != null)
			{
				Undo.RecordObject(_window.TargetAsset, name);
			}
		}

		private void MarkDirty()
		{
			if (_window.TargetAsset != null)
			{
				EditorUtility.SetDirty(_window.TargetAsset);
			}
		}
	}
}
