using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class GraphViewDataTests
	{
		private StateMachineGraph _graph;
		private GraphViewData _data;

		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			_data = _graph.GraphViewData;
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
		public void HasBeenSet_DefaultsToFalse()
		{
			Assert.IsFalse(_data.HasBeenSet);
		}

		[Test]
		public void ViewPosition_SetValue_SetsHasBeenSetToTrue()
		{
			_data.ViewPosition = new Vector3(100, 200, 0);

			Assert.IsTrue(_data.HasBeenSet);
		}

		[Test]
		public void ViewPosition_SetValue_StoresPosition()
		{
			var position = new Vector3(50, 75, 0);

			_data.ViewPosition = position;

			Assert.AreEqual(position, _data.ViewPosition);
		}

		[Test]
		public void ViewPosition_SetToZero_StillSetsHasBeenSet()
		{
			_data.ViewPosition = Vector3.zero;

			Assert.IsTrue(_data.HasBeenSet);
		}



		[Test]
		public void ViewScale_DefaultsToOne()
		{
			Assert.AreEqual(Vector3.one, _data.ViewScale);
		}

		[Test]
		public void ViewScale_SetValue_StoresScale()
		{
			var scale = new Vector3(2, 2, 1);

			_data.ViewScale = scale;

			Assert.AreEqual(scale, _data.ViewScale);
		}

		[Test]
		public void Zoom_DefaultsToOne()
		{
			Assert.AreEqual(1f, _data.Zoom);
		}

		[Test]
		public void Zoom_SetValue_StoresZoom()
		{
			_data.Zoom = 0.5f;

			Assert.AreEqual(0.5f, _data.Zoom);
		}



		[Test]
		public void SetPeekPreviewState_AddsNewState()
		{
			_data.SetPeekPreviewState("node1", new Vector2(10, 20), new Vector2(5, 5), 1.5f);

			Assert.AreEqual(1, _data.OpenPeekPreviews.Count);
			Assert.AreEqual("node1", _data.OpenPeekPreviews[0].NodeId);
		}

		[Test]
		public void SetPeekPreviewState_StoresPositionCorrectly()
		{
			var position = new Vector2(100, 200);

			_data.SetPeekPreviewState("node1", position, Vector2.zero, 1f);

			Assert.AreEqual(position, _data.OpenPeekPreviews[0].Position);
		}

		[Test]
		public void SetPeekPreviewState_StoresPanOffsetCorrectly()
		{
			var panOffset = new Vector2(30, 40);

			_data.SetPeekPreviewState("node1", Vector2.zero, panOffset, 1f);

			Assert.AreEqual(panOffset, _data.OpenPeekPreviews[0].PanOffset);
		}

		[Test]
		public void SetPeekPreviewState_StoresZoomLevelCorrectly()
		{
			_data.SetPeekPreviewState("node1", Vector2.zero, Vector2.zero, 2.5f);

			Assert.AreEqual(2.5f, _data.OpenPeekPreviews[0].ZoomLevel);
		}

		[Test]
		public void SetPeekPreviewState_MultipleDifferentNodes_AddsAll()
		{
			_data.SetPeekPreviewState("node1", Vector2.zero, Vector2.zero, 1f);
			_data.SetPeekPreviewState("node2", Vector2.one, Vector2.one, 2f);
			_data.SetPeekPreviewState("node3", Vector2.right, Vector2.up, 0.5f);

			Assert.AreEqual(3, _data.OpenPeekPreviews.Count);
		}

		[Test]
		public void SetPeekPreviewState_SameNodeTwice_UpdatesExisting()
		{
			_data.SetPeekPreviewState("node1", new Vector2(10, 10), Vector2.zero, 1f);
			_data.SetPeekPreviewState("node1", new Vector2(50, 50), Vector2.one, 2f);

			Assert.AreEqual(1, _data.OpenPeekPreviews.Count);
			Assert.AreEqual(new Vector2(50, 50), _data.OpenPeekPreviews[0].Position);
			Assert.AreEqual(Vector2.one, _data.OpenPeekPreviews[0].PanOffset);
			Assert.AreEqual(2f, _data.OpenPeekPreviews[0].ZoomLevel);
		}



		[Test]
		public void GetPeekPreviewState_ExistingNode_ReturnsState()
		{
			_data.SetPeekPreviewState("node1", new Vector2(10, 20), new Vector2(5, 5), 1.5f);

			var state = _data.GetPeekPreviewState("node1");

			Assert.IsNotNull(state);
			Assert.AreEqual("node1", state.NodeId);
			Assert.AreEqual(new Vector2(10, 20), state.Position);
		}

		[Test]
		public void GetPeekPreviewState_NonExistentNode_ReturnsNull()
		{
			var state = _data.GetPeekPreviewState("nonexistent");

			Assert.IsNull(state);
		}

		[Test]
		public void GetPeekPreviewState_MultipleNodes_ReturnsCorrectOne()
		{
			_data.SetPeekPreviewState("node1", new Vector2(10, 10), Vector2.zero, 1f);
			_data.SetPeekPreviewState("node2", new Vector2(20, 20), Vector2.zero, 2f);
			_data.SetPeekPreviewState("node3", new Vector2(30, 30), Vector2.zero, 3f);

			var state = _data.GetPeekPreviewState("node2");

			Assert.IsNotNull(state);
			Assert.AreEqual("node2", state.NodeId);
			Assert.AreEqual(new Vector2(20, 20), state.Position);
			Assert.AreEqual(2f, state.ZoomLevel);
		}



		[Test]
		public void RemovePeekPreviewState_ExistingNode_RemovesIt()
		{
			_data.SetPeekPreviewState("node1", Vector2.zero, Vector2.zero, 1f);

			_data.RemovePeekPreviewState("node1");

			Assert.AreEqual(0, _data.OpenPeekPreviews.Count);
		}

		[Test]
		public void RemovePeekPreviewState_NonExistentNode_DoesNothing()
		{
			_data.SetPeekPreviewState("node1", Vector2.zero, Vector2.zero, 1f);

			_data.RemovePeekPreviewState("nonexistent");

			Assert.AreEqual(1, _data.OpenPeekPreviews.Count);
		}

		[Test]
		public void RemovePeekPreviewState_LeavesOtherNodesIntact()
		{
			_data.SetPeekPreviewState("node1", Vector2.zero, Vector2.zero, 1f);
			_data.SetPeekPreviewState("node2", Vector2.one, Vector2.one, 2f);
			_data.SetPeekPreviewState("node3", Vector2.right, Vector2.up, 3f);

			_data.RemovePeekPreviewState("node2");

			Assert.AreEqual(2, _data.OpenPeekPreviews.Count);
			Assert.IsNotNull(_data.GetPeekPreviewState("node1"));
			Assert.IsNull(_data.GetPeekPreviewState("node2"));
			Assert.IsNotNull(_data.GetPeekPreviewState("node3"));
		}

		[Test]
		public void RemovePeekPreviewState_ThenGet_ReturnsNull()
		{
			_data.SetPeekPreviewState("node1", Vector2.zero, Vector2.zero, 1f);
			_data.RemovePeekPreviewState("node1");

			var state = _data.GetPeekPreviewState("node1");

			Assert.IsNull(state);
		}



		[Test]
		public void SetPeekPreviewState_AfterRemove_AddsNewState()
		{
			_data.SetPeekPreviewState("node1", new Vector2(10, 10), Vector2.zero, 1f);
			_data.RemovePeekPreviewState("node1");
			_data.SetPeekPreviewState("node1", new Vector2(50, 50), Vector2.one, 2f);

			Assert.AreEqual(1, _data.OpenPeekPreviews.Count);
			Assert.AreEqual(new Vector2(50, 50), _data.OpenPeekPreviews[0].Position);
			Assert.AreEqual(2f, _data.OpenPeekPreviews[0].ZoomLevel);
		}



		[Test]
		public void UpdateBookmark_NonExistentId_DoesNothing()
		{
			var bookmark = new GraphBookmark("Test", Vector3.zero, Vector3.one, 1f);
			bookmark.Id = "nonexistent";

			_data.UpdateBookmark(bookmark);

			Assert.AreEqual(0, _data.Bookmarks.Count);
		}

		[Test]
		public void RemoveBookmark_NonExistentId_DoesNothing()
		{
			var bookmark = new GraphBookmark("Test", Vector3.zero, Vector3.one, 1f);
			_data.AddBookmark(bookmark);

			_data.RemoveBookmark("nonexistent");

			Assert.AreEqual(1, _data.Bookmarks.Count);
		}

		[Test]
		public void AddBookmark_MultipleWithSameProperties_AddsAll()
		{
			_data.AddBookmark(new GraphBookmark("Same", Vector3.zero, Vector3.one, 1f));
			_data.AddBookmark(new GraphBookmark("Same", Vector3.zero, Vector3.one, 1f));

			Assert.AreEqual(2, _data.Bookmarks.Count);
		}



		[Test]
		public void OpenPeekPreviews_DefaultsToEmptyList()
		{
			Assert.IsNotNull(_data.OpenPeekPreviews);
			Assert.AreEqual(0, _data.OpenPeekPreviews.Count);
		}

	}
}
