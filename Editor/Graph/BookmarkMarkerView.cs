using Nonatomic.VSM.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// A small visual marker placed in the graph at a bookmark's saved location.
	/// Provides context menu for navigation and deletion.
	/// </summary>
	public class BookmarkMarkerView : VisualElement
	{
		public GraphBookmark Bookmark { get; }

		private readonly StateMachineGraphView _graphView;

		public BookmarkMarkerView(GraphBookmark bookmark, StateMachineGraphView graphView)
		{
			Bookmark = bookmark;
			_graphView = graphView;

			pickingMode = PickingMode.Position;
			AddToClassList("bookmark-marker");

			style.position = Position.Absolute;
			style.flexDirection = FlexDirection.Row;
			style.alignItems = Align.Center;
			style.backgroundColor = new Color(0.15f, 0.25f, 0.45f, 0.65f);
			style.borderTopLeftRadius = 3;
			style.borderTopRightRadius = 3;
			style.borderBottomLeftRadius = 3;
			style.borderBottomRightRadius = 3;
			style.borderLeftWidth = 2;
			style.borderLeftColor = new Color(0.4f, 0.6f, 0.9f, 0.8f);
			style.paddingLeft = 6;
			style.paddingRight = 6;
			style.paddingTop = 2;
			style.paddingBottom = 2;

			BuildContent();
			UpdatePosition();

			RegisterCallback<ContextualMenuPopulateEvent>(OnContextMenu);
			RegisterCallback<MouseDownEvent>(OnMouseDown);
		}

		private void BuildContent()
		{
			var nameLabel = new Label(Bookmark.Name);
			nameLabel.style.color = new Color(0.85f, 0.9f, 1f, 0.9f);
			nameLabel.style.fontSize = 10;
			nameLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
			nameLabel.pickingMode = PickingMode.Ignore;
			Add(nameLabel);

			if (Bookmark.Slot > 0)
			{
				var slotLabel = new Label($" [{Bookmark.Slot}]");
				slotLabel.style.color = new Color(0.6f, 0.75f, 1f, 0.7f);
				slotLabel.style.fontSize = 9;
				slotLabel.pickingMode = PickingMode.Ignore;
				Add(slotLabel);
			}
		}

		/// <summary>
		/// Updates the marker's position from the bookmark's graph position.
		/// </summary>
		public void UpdatePosition()
		{
			style.left = Bookmark.GraphPosition.x - 10;
			style.top = Bookmark.GraphPosition.y - 8;
		}

		private void OnMouseDown(MouseDownEvent evt)
		{
			if (evt.button == 0 && evt.clickCount == 2)
			{
				_graphView.GoToBookmark(Bookmark);
				evt.StopPropagation();
			}
		}

		private void OnContextMenu(ContextualMenuPopulateEvent evt)
		{
			evt.menu.AppendAction(
				"Go to Bookmark",
				_ => _graphView.GoToBookmark(Bookmark));

			evt.menu.AppendAction(
				"Delete Bookmark",
				_ =>
				{
					_graphView.DeleteBookmark(Bookmark.Id);
					_graphView.RefreshBookmarkMarkers();
				});

			evt.StopPropagation();
		}
	}
}
