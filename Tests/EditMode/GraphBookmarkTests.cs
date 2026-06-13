using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class GraphBookmarkTests
	{
		private StateMachineGraph _graph;

		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_graph != null)
			{
				Object.DestroyImmediate(_graph);
			}
		}

		[Test]
		public void GraphBookmark_Constructor_GeneratesUniqueId()
		{
			var bookmark1 = new GraphBookmark();
			var bookmark2 = new GraphBookmark();

			Assert.IsNotNull(bookmark1.Id);
			Assert.IsNotNull(bookmark2.Id);
			Assert.AreNotEqual(bookmark1.Id, bookmark2.Id);
		}

		[Test]
		public void GraphBookmark_Constructor_SetsDefaultName()
		{
			var bookmark = new GraphBookmark();

			Assert.AreEqual("New Bookmark", bookmark.Name);
		}

		[Test]
		public void GraphBookmark_ParameterizedConstructor_SetsProperties()
		{
			var position = new Vector3(100, 200, 0);
			var scale = new Vector3(2, 2, 1);
			var zoom = 2f;

			var bookmark = new GraphBookmark("Test Bookmark", position, scale, zoom);

			Assert.AreEqual("Test Bookmark", bookmark.Name);
			Assert.AreEqual(position, bookmark.ViewPosition);
			Assert.AreEqual(scale, bookmark.ViewScale);
			Assert.AreEqual(zoom, bookmark.Zoom);
		}

		[Test]
		public void GraphViewData_AddBookmark_AddsToList()
		{
			var data = _graph.GraphViewData;
			var bookmark = new GraphBookmark("Test", Vector3.zero, Vector3.one, 1f);

			data.AddBookmark(bookmark);

			Assert.AreEqual(1, data.Bookmarks.Count);
			Assert.AreEqual("Test", data.Bookmarks[0].Name);
		}

		[Test]
		public void GraphViewData_RemoveBookmark_RemovesFromList()
		{
			var data = _graph.GraphViewData;
			var bookmark = new GraphBookmark("Test", Vector3.zero, Vector3.one, 1f);
			data.AddBookmark(bookmark);

			data.RemoveBookmark(bookmark.Id);

			Assert.AreEqual(0, data.Bookmarks.Count);
		}

		[Test]
		public void GraphViewData_GetBookmark_ReturnsCorrectBookmark()
		{
			var data = _graph.GraphViewData;
			var bookmark1 = new GraphBookmark("First", Vector3.zero, Vector3.one, 1f);
			var bookmark2 = new GraphBookmark("Second", Vector3.one, Vector3.one, 1f);
			data.AddBookmark(bookmark1);
			data.AddBookmark(bookmark2);

			var found = data.GetBookmark(bookmark2.Id);

			Assert.IsNotNull(found);
			Assert.AreEqual("Second", found.Name);
		}

		[Test]
		public void GraphViewData_GetBookmark_ReturnsNullForMissingId()
		{
			var data = _graph.GraphViewData;

			var found = data.GetBookmark("nonexistent-id");

			Assert.IsNull(found);
		}

		[Test]
		public void GraphViewData_UpdateBookmark_UpdatesExisting()
		{
			var data = _graph.GraphViewData;
			var bookmark = new GraphBookmark("Original", Vector3.zero, Vector3.one, 1f);
			data.AddBookmark(bookmark);

			bookmark.Name = "Updated";
			data.UpdateBookmark(bookmark);

			Assert.AreEqual(1, data.Bookmarks.Count);
			Assert.AreEqual("Updated", data.Bookmarks[0].Name);
		}

		[Test]
		public void GraphViewData_MultipleBookmarks_MaintainsOrder()
		{
			var data = _graph.GraphViewData;
			data.AddBookmark(new GraphBookmark("First", Vector3.zero, Vector3.one, 1f));
			data.AddBookmark(new GraphBookmark("Second", Vector3.zero, Vector3.one, 1f));
			data.AddBookmark(new GraphBookmark("Third", Vector3.zero, Vector3.one, 1f));

			Assert.AreEqual(3, data.Bookmarks.Count);
			Assert.AreEqual("First", data.Bookmarks[0].Name);
			Assert.AreEqual("Second", data.Bookmarks[1].Name);
			Assert.AreEqual("Third", data.Bookmarks[2].Name);
		}

		[Test]
		public void GraphBookmark_Slot_DefaultsToZero()
		{
			var bookmark = new GraphBookmark();

			Assert.AreEqual(0, bookmark.Slot);
		}

		[Test]
		public void GraphBookmark_Slot_CanBeSetAndRetrieved()
		{
			var bookmark = new GraphBookmark("Test", Vector3.zero, Vector3.one, 1f);

			bookmark.Slot = 5;

			Assert.AreEqual(5, bookmark.Slot);
		}

		[Test]
		public void GraphBookmark_Slot_CanBeClearedBySettingToZero()
		{
			var bookmark = new GraphBookmark("Test", Vector3.zero, Vector3.one, 1f);
			bookmark.Slot = 3;

			bookmark.Slot = 0;

			Assert.AreEqual(0, bookmark.Slot);
		}

		[Test]
		public void GraphBookmark_GraphPosition_DefaultsToZero()
		{
			var bookmark = new GraphBookmark();

			Assert.AreEqual(Vector2.zero, bookmark.GraphPosition);
		}

		[Test]
		public void GraphBookmark_GraphPosition_CanBeSetAndRetrieved()
		{
			var bookmark = new GraphBookmark("Test", Vector3.zero, Vector3.one, 1f);

			bookmark.GraphPosition = new Vector2(500, 300);

			Assert.AreEqual(500, bookmark.GraphPosition.x);
			Assert.AreEqual(300, bookmark.GraphPosition.y);
		}

		[Test]
		public void GraphBookmark_GraphPosition_PreservedThroughGraphViewData()
		{
			var bookmark = new GraphBookmark("Positioned", Vector3.zero, Vector3.one, 1f);
			bookmark.GraphPosition = new Vector2(750, 420);
			_graph.GraphViewData.AddBookmark(bookmark);

			var loaded = _graph.GraphViewData.GetBookmark(bookmark.Id);

			Assert.AreEqual(750, loaded.GraphPosition.x);
			Assert.AreEqual(420, loaded.GraphPosition.y);
		}
	}
}
