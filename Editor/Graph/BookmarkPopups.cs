using System;
using System.Collections.Generic;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Popup window for entering a new bookmark name.
	/// </summary>
	public class BookmarkNamePopup : PopupWindowContent
	{
		private readonly Action<string> _onConfirm;
		private string _bookmarkName = "New Bookmark";

		public BookmarkNamePopup(Action<string> onConfirm)
		{
			_onConfirm = onConfirm;
		}

		public override Vector2 GetWindowSize()
		{
			return new Vector2(200, 70);
		}

		public override void OnOpen()
		{
			var root = editorWindow.rootVisualElement;
			root.style.paddingTop = 4;
			root.style.paddingLeft = 4;
			root.style.paddingRight = 4;
			root.style.paddingBottom = 4;

			var label = new Label("Bookmark Name:");
			label.style.unityFontStyleAndWeight = FontStyle.Bold;
			root.Add(label);

			var nameField = new TextField { value = _bookmarkName };
			nameField.RegisterValueChangedCallback(evt => _bookmarkName = evt.newValue);
			root.Add(nameField);

			nameField.schedule.Execute(() =>
			{
				nameField.Focus();
				nameField.SelectAll();
			});

			var buttonRow = new VisualElement();
			buttonRow.style.flexDirection = FlexDirection.Row;
			buttonRow.style.justifyContent = Justify.FlexEnd;
			buttonRow.style.marginTop = 8;
			root.Add(buttonRow);

			var cancelButton = new Button(() => editorWindow.Close()) { text = "Cancel" };
			buttonRow.Add(cancelButton);

			var addButton = new Button(() =>
			{
				_onConfirm?.Invoke(_bookmarkName);
				editorWindow.Close();
			}) { text = "Add" };
			buttonRow.Add(addButton);

			root.RegisterCallback<KeyDownEvent>(evt =>
			{
				if (evt.keyCode != KeyCode.Return) return;
				_onConfirm?.Invoke(_bookmarkName);
				editorWindow.Close();
			});
		}

		public override void OnGUI(Rect rect)
		{
			// Rendering handled by UI Toolkit via OnOpen()
		}
	}

	/// <summary>
	/// Popup window for managing bookmarks (rename, delete).
	/// </summary>
	public class ManageBookmarksPopup : PopupWindowContent
	{
		private readonly StateMachineGraphView _graphView;
		private ScrollView _scrollView;

		public ManageBookmarksPopup(StateMachineGraphView graphView)
		{
			_graphView = graphView;
		}

		public override Vector2 GetWindowSize()
		{
			var bookmarks = _graphView?.GetBookmarks();
			var count = bookmarks?.Count ?? 0;
			var height = Mathf.Clamp(60 + count * 24, 80, 300);
			return new Vector2(250, height);
		}

		public override void OnOpen()
		{
			var root = editorWindow.rootVisualElement;
			root.style.paddingTop = 4;
			root.style.paddingLeft = 4;
			root.style.paddingRight = 4;
			root.style.paddingBottom = 4;

			var header = new Label("Manage Bookmarks");
			header.style.unityFontStyleAndWeight = FontStyle.Bold;
			root.Add(header);

			var bookmarks = _graphView?.GetBookmarks();
			if (bookmarks == null || bookmarks.Count == 0)
			{
				var helpBox = new HelpBox("No bookmarks saved.", HelpBoxMessageType.Info);
				root.Add(helpBox);
				return;
			}

			_scrollView = new ScrollView();
			_scrollView.style.marginTop = 4;
			root.Add(_scrollView);

			RebuildBookmarkList();
		}

		private void RebuildBookmarkList()
		{
			_scrollView.Clear();

			var bookmarks = _graphView?.GetBookmarks();
			if (bookmarks == null || bookmarks.Count == 0)
			{
				editorWindow.Close();
				return;
			}

			var bookmarkList = new List<GraphBookmark>(bookmarks);

			foreach (var bookmark in bookmarkList)
			{
				var bm = bookmark;
				var row = new VisualElement();
				row.style.flexDirection = FlexDirection.Row;
				row.style.alignItems = Align.Center;
				row.style.marginBottom = 2;

				var nameButton = new Button(() =>
				{
					_graphView.GoToBookmark(bm);
					editorWindow.Close();
				})
				{
					text = bm.Name
				};
				nameButton.style.flexGrow = 1;
				nameButton.style.unityTextAlign = TextAnchor.MiddleLeft;
				row.Add(nameButton);

				var deleteButton = new Button(() =>
				{
					_graphView.DeleteBookmark(bm.Id);
					RebuildBookmarkList();
				})
				{
					text = "X"
				};
				deleteButton.style.width = 20;
				row.Add(deleteButton);

				_scrollView.Add(row);
			}
		}

		public override void OnGUI(Rect rect)
		{
			// Rendering handled by UI Toolkit via OnOpen()
		}
	}
}
