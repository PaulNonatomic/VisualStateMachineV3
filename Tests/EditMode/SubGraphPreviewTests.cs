using System;
using NUnit.Framework;
using Nonatomic.VSM.Graph;
using Nonatomic.VSM.Editor.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class SubGraphPreviewTests
	{
		private StateMachineGraph _graph;
		private StateMachineGraph _subGraph;

		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			_subGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_graph != null)
			{
				UnityEngine.Object.DestroyImmediate(_graph);
			}
			if (_subGraph != null)
			{
				UnityEngine.Object.DestroyImmediate(_subGraph);
			}
		}

		[Test]
		public void SubGraphPreviewPopup_CanBeCreated_WithValidGraph()
		{
			var popup = new SubGraphPreviewPopup(_subGraph);

			Assert.IsNotNull(popup);
		}

		[Test]
		public void SubGraphPreviewPopup_CanBeCreated_WithNullGraph()
		{
			var popup = new SubGraphPreviewPopup(null);

			Assert.IsNotNull(popup);
		}

		[Test]
		public void SubGraphPreviewPopup_Close_RemovesFromHierarchy()
		{
			var popup = new SubGraphPreviewPopup(_subGraph);
			var parent = new UnityEngine.UIElements.VisualElement();
			parent.Add(popup);

			Assert.IsTrue(parent.Contains(popup));

			popup.Close();

			Assert.IsFalse(parent.Contains(popup));
		}

		[Test]
		public void SubStateMachineNode_HasCorrectNodeType()
		{
			var node = new SubStateMachineNode();

			Assert.AreEqual(GraphNodeType.SubStateMachine, node.NodeType);
		}

		[Test]
		public void SubStateMachineNode_CanReferenceSubGraph()
		{
			var node = new SubStateMachineNode();
			node.SubGraph = _subGraph;

			Assert.AreEqual(_subGraph, node.SubGraph);
		}

		[Test]
		public void SubStateMachineNode_DefaultDataMode_IsInherit()
		{
			var node = new SubStateMachineNode();

			Assert.AreEqual(SubStateMachineDataMode.Inherit, node.DataMode);
		}

		[Test]
		public void SubStateMachineNode_CanSetDataMode()
		{
			var node = new SubStateMachineNode();
			node.DataMode = SubStateMachineDataMode.Isolated;

			Assert.AreEqual(SubStateMachineDataMode.Isolated, node.DataMode);
		}

		[Test]
		public void SubGraph_WithNodes_HasCorrectNodeCount()
		{
			// Add some nodes to the sub-graph
			var entryNode = new EntryNode { Id = Guid.NewGuid().ToString() };
			var stateNode = new StateNode { Id = Guid.NewGuid().ToString(), DisplayName = "TestState" };
			var exitNode = new ExitNode { Id = Guid.NewGuid().ToString() };

			_subGraph.AddNode(entryNode);
			_subGraph.AddNode(stateNode);
			_subGraph.AddNode(exitNode);

			// Account for auto-created entry node
			Assert.GreaterOrEqual(_subGraph.NodeCount, 3);
		}

		[Test]
		public void SubGraph_WithConnections_HasCorrectConnectionCount()
		{
			var entryNode = new EntryNode { Id = "entry" };
			var stateNode = new StateNode { Id = "state" };

			_subGraph.AddNode(entryNode);
			_subGraph.AddNode(stateNode);

			var connection = new Connection
			{
				Id = Guid.NewGuid().ToString(),
				SourceNodeId = "entry",
				SourcePortName = "output",
				TargetNodeId = "state",
				TargetPortName = "input",
				ConnectionType = ConnectionType.Transition
			};

			_subGraph.AddConnection(connection);

			Assert.AreEqual(1, _subGraph.ConnectionCount);
		}

		[Test]
		public void SubGraphPreviewPopup_HasFocusableProperty()
		{
			var popup = new SubGraphPreviewPopup(_subGraph);

			Assert.IsTrue(popup.focusable);
		}

		[Test]
		public void SubGraphPreviewPopup_Show_AddsToParent()
		{
			var popup = new SubGraphPreviewPopup(_subGraph);
			var parent = new UnityEngine.UIElements.VisualElement();

			popup.Show(parent, new Vector2(100, 100));

			Assert.IsTrue(parent.Contains(popup));
		}

		[Test]
		public void SubGraphPreviewPopup_Show_SetsPosition()
		{
			var popup = new SubGraphPreviewPopup(_subGraph);
			var parent = new UnityEngine.UIElements.VisualElement();

			popup.Show(parent, new Vector2(100, 200));

			// Position should be set (actual value depends on bounds calculations)
			Assert.AreEqual(UnityEngine.UIElements.Position.Absolute, popup.style.position.value);
		}

		[Test]
		public void SubGraphPreviewPopup_WithNodes_CalculatesInitialView()
		{
			// Add some nodes to the sub-graph
			var entryNode = new EntryNode { Id = "entry", Position = new Vector2(0, 0) };
			var stateNode = new StateNode { Id = "state", Position = new Vector2(200, 100) };

			_subGraph.AddNode(entryNode);
			_subGraph.AddNode(stateNode);

			var popup = new SubGraphPreviewPopup(_subGraph);

			// Popup should be created successfully with nodes
			Assert.IsNotNull(popup);
		}

		[Test]
		public void SubGraphPreviewPopup_HasCorrectDimensions()
		{
			var popup = new SubGraphPreviewPopup(_subGraph);

			// Check popup dimensions
			Assert.AreEqual(400f, popup.style.width.value.value);
			Assert.AreEqual(300f, popup.style.height.value.value);
		}

		[Test]
		public void SubGraphPreviewPopup_NodeId_CanBeSetAndRetrieved()
		{
			var popup = new SubGraphPreviewPopup(_subGraph);
			var nodeId = "test-node-id";

			popup.NodeId = nodeId;

			Assert.AreEqual(nodeId, popup.NodeId);
		}

		[Test]
		public void SubGraphPreviewPopup_PanOffset_IsInitiallyZero()
		{
			var popup = new SubGraphPreviewPopup(_subGraph);

			Assert.AreEqual(Vector2.zero, popup.PanOffset);
		}

		[Test]
		public void SubGraphPreviewPopup_ZoomLevel_IsInitiallyOne()
		{
			var popup = new SubGraphPreviewPopup(_subGraph);

			Assert.AreEqual(1f, popup.ZoomLevel);
		}

		[Test]
		public void SubGraphPreviewPopup_RestoreState_SetsCorrectValues()
		{
			var popup = new SubGraphPreviewPopup(_subGraph);
			var expectedPan = new Vector2(50, 100);
			var expectedZoom = 1.5f;

			popup.RestoreState(expectedPan, expectedZoom);

			Assert.AreEqual(expectedPan, popup.PanOffset);
			Assert.AreEqual(expectedZoom, popup.ZoomLevel);
		}

		[Test]
		public void SubGraphPreviewPopup_RestoreState_ClampsZoomLevel()
		{
			var popup = new SubGraphPreviewPopup(_subGraph);

			// Try to set zoom below minimum (0.25f)
			popup.RestoreState(Vector2.zero, 0.1f);
			Assert.AreEqual(0.25f, popup.ZoomLevel);

			// Try to set zoom above maximum (4f)
			popup.RestoreState(Vector2.zero, 10f);
			Assert.AreEqual(4f, popup.ZoomLevel);
		}

		[Test]
		public void SubGraphPreviewPopup_OnClosed_FiresWhenClosed()
		{
			var popup = new SubGraphPreviewPopup(_subGraph);
			var wasClosed = false;
			SubGraphPreviewPopup closedPopup = null;

			popup.OnClosed += p =>
			{
				wasClosed = true;
				closedPopup = p;
			};

			popup.Close();

			Assert.IsTrue(wasClosed);
			Assert.AreEqual(popup, closedPopup);
		}

		[Test]
		public void SubGraphPreviewPopup_SubGraph_ReturnsCorrectGraph()
		{
			var popup = new SubGraphPreviewPopup(_subGraph);

			Assert.AreEqual(_subGraph, popup.SubGraph);
		}

		[Test]
		public void PeekPreviewState_DefaultValues_AreCorrect()
		{
			var state = new PeekPreviewState();

			Assert.IsNull(state.NodeId);
			Assert.AreEqual(Vector2.zero, state.Position);
			Assert.AreEqual(Vector2.zero, state.PanOffset);
			Assert.AreEqual(1f, state.ZoomLevel);
		}

		[Test]
		public void PeekPreviewState_CanSetAllProperties()
		{
			var state = new PeekPreviewState
			{
				NodeId = "test-node",
				Position = new Vector2(100, 200),
				PanOffset = new Vector2(50, 75),
				ZoomLevel = 2.0f
			};

			Assert.AreEqual("test-node", state.NodeId);
			Assert.AreEqual(new Vector2(100, 200), state.Position);
			Assert.AreEqual(new Vector2(50, 75), state.PanOffset);
			Assert.AreEqual(2.0f, state.ZoomLevel);
		}

		[Test]
		public void GraphViewData_OpenPeekPreviews_IsInitiallyEmpty()
		{
			var graphViewData = new GraphViewData();

			Assert.IsNotNull(graphViewData.OpenPeekPreviews);
			Assert.AreEqual(0, graphViewData.OpenPeekPreviews.Count);
		}

		[Test]
		public void GraphViewData_SetPeekPreviewState_AddsNewState()
		{
			var graphViewData = new GraphViewData();

			graphViewData.SetPeekPreviewState("node1", new Vector2(100, 200), new Vector2(50, 75), 1.5f);

			Assert.AreEqual(1, graphViewData.OpenPeekPreviews.Count);
			var state = graphViewData.GetPeekPreviewState("node1");
			Assert.IsNotNull(state);
			Assert.AreEqual("node1", state.NodeId);
			Assert.AreEqual(new Vector2(100, 200), state.Position);
			Assert.AreEqual(new Vector2(50, 75), state.PanOffset);
			Assert.AreEqual(1.5f, state.ZoomLevel);
		}

		[Test]
		public void GraphViewData_SetPeekPreviewState_UpdatesExistingState()
		{
			var graphViewData = new GraphViewData();

			graphViewData.SetPeekPreviewState("node1", new Vector2(100, 200), new Vector2(50, 75), 1.5f);
			graphViewData.SetPeekPreviewState("node1", new Vector2(300, 400), new Vector2(10, 20), 2.0f);

			Assert.AreEqual(1, graphViewData.OpenPeekPreviews.Count);
			var state = graphViewData.GetPeekPreviewState("node1");
			Assert.AreEqual(new Vector2(300, 400), state.Position);
			Assert.AreEqual(new Vector2(10, 20), state.PanOffset);
			Assert.AreEqual(2.0f, state.ZoomLevel);
		}

		[Test]
		public void GraphViewData_RemovePeekPreviewState_RemovesState()
		{
			var graphViewData = new GraphViewData();
			graphViewData.SetPeekPreviewState("node1", Vector2.zero, Vector2.zero, 1f);
			graphViewData.SetPeekPreviewState("node2", Vector2.zero, Vector2.zero, 1f);

			graphViewData.RemovePeekPreviewState("node1");

			Assert.AreEqual(1, graphViewData.OpenPeekPreviews.Count);
			Assert.IsNull(graphViewData.GetPeekPreviewState("node1"));
			Assert.IsNotNull(graphViewData.GetPeekPreviewState("node2"));
		}

		[Test]
		public void GraphViewData_GetPeekPreviewState_ReturnsNullForNonExistent()
		{
			var graphViewData = new GraphViewData();

			var state = graphViewData.GetPeekPreviewState("nonexistent");

			Assert.IsNull(state);
		}


		[Test]
		public void SubGraphPreviewPopup_OffsetFromOwnerNode_IsInitiallyZero()
		{
			var popup = new SubGraphPreviewPopup(_subGraph);

			Assert.AreEqual(Vector2.zero, popup.OffsetFromOwnerNode);
		}

		[Test]
		public void SubGraphPreviewPopup_GetGraphPosition_ReturnsCurrentPosition()
		{
			var popup = new SubGraphPreviewPopup(_subGraph);
			var parent = new UnityEngine.UIElements.VisualElement();

			// Set position manually
			popup.style.left = 150;
			popup.style.top = 250;
			parent.Add(popup);

			var position = popup.GetGraphPosition();

			Assert.AreEqual(150f, position.x, 0.01f);
			Assert.AreEqual(250f, position.y, 0.01f);
		}

	}
}
