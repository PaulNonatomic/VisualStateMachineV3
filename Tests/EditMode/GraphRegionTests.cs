using System;
using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class GraphRegionTests
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
				UnityEngine.Object.DestroyImmediate(_graph);
			}
		}

		[Test]
		public void GraphRegion_HasUniqueId_OnCreation()
		{
			var region1 = new GraphRegion();
			var region2 = new GraphRegion();

			Assert.IsNotNull(region1.Id);
			Assert.IsNotNull(region2.Id);
			Assert.AreNotEqual(region1.Id, region2.Id);
		}

		[Test]
		public void GraphRegion_HasDefaultTitle()
		{
			var region = new GraphRegion();

			Assert.AreEqual("New Region", region.Title);
		}

		[Test]
		public void GraphRegion_HasDefaultBounds()
		{
			var region = new GraphRegion();

			Assert.AreEqual(300, region.Bounds.width);
			Assert.AreEqual(200, region.Bounds.height);
		}

		[Test]
		public void GraphRegion_HasDefaultColor()
		{
			var region = new GraphRegion();

			Assert.IsTrue(region.Color.a > 0);
		}

		[Test]
		public void GraphRegion_CanSetTitle()
		{
			var region = new GraphRegion();
			region.Title = "My Region";

			Assert.AreEqual("My Region", region.Title);
		}

		[Test]
		public void GraphRegion_CanSetBounds()
		{
			var region = new GraphRegion();
			var bounds = new Rect(100, 200, 400, 300);
			region.Bounds = bounds;

			Assert.AreEqual(bounds, region.Bounds);
		}

		[Test]
		public void GraphRegion_CanSetColor()
		{
			var region = new GraphRegion();
			var color = new Color(1f, 0.5f, 0.25f, 0.5f);
			region.Color = color;

			Assert.AreEqual(color, region.Color);
		}

		[Test]
		public void GraphRegion_ConstructorWithTitleAndBounds()
		{
			var bounds = new Rect(50, 100, 500, 400);
			var region = new GraphRegion("Test Region", bounds);

			Assert.AreEqual("Test Region", region.Title);
			Assert.AreEqual(bounds, region.Bounds);
		}

		[Test]
		public void GraphRegion_ConstructorWithColor()
		{
			var bounds = new Rect(50, 100, 500, 400);
			var color = Color.red;
			var region = new GraphRegion("Colored Region", bounds, color);

			Assert.AreEqual("Colored Region", region.Title);
			Assert.AreEqual(bounds, region.Bounds);
			Assert.AreEqual(color, region.Color);
		}

		[Test]
		public void StateMachineGraph_AddRegion_AddsToGraph()
		{
			var region = new GraphRegion("Test Region", new Rect(0, 0, 300, 200));

			_graph.AddRegion(region);

			Assert.AreEqual(1, _graph.Regions.Count);
			Assert.AreEqual(region, _graph.Regions[0]);
		}

		[Test]
		public void StateMachineGraph_AddRegion_UpdatesExisting()
		{
			var region = new GraphRegion("Test Region", new Rect(0, 0, 300, 200));
			_graph.AddRegion(region);

			region.Title = "Updated Region";
			_graph.AddRegion(region);

			Assert.AreEqual(1, _graph.Regions.Count);
			Assert.AreEqual("Updated Region", _graph.Regions[0].Title);
		}

		[Test]
		public void StateMachineGraph_RemoveRegion_RemovesFromGraph()
		{
			var region = new GraphRegion("Test Region", new Rect(0, 0, 300, 200));
			_graph.AddRegion(region);

			_graph.RemoveRegion(region.Id);

			Assert.AreEqual(0, _graph.Regions.Count);
		}

		[Test]
		public void StateMachineGraph_GetRegion_ReturnsCorrectRegion()
		{
			var region1 = new GraphRegion("Region 1", new Rect(0, 0, 300, 200));
			var region2 = new GraphRegion("Region 2", new Rect(400, 0, 300, 200));
			_graph.AddRegion(region1);
			_graph.AddRegion(region2);

			var result = _graph.GetRegion(region2.Id);

			Assert.AreEqual(region2, result);
			Assert.AreEqual("Region 2", result.Title);
		}

		[Test]
		public void StateMachineGraph_GetRegion_NonExistent_ReturnsNull()
		{
			var result = _graph.GetRegion("nonexistent");

			Assert.IsNull(result);
		}

		[Test]
		public void StateMachineGraph_UpdateRegion_UpdatesExisting()
		{
			var region = new GraphRegion("Original", new Rect(0, 0, 300, 200));
			_graph.AddRegion(region);

			region.Title = "Updated";
			region.Bounds = new Rect(100, 100, 400, 300);
			_graph.UpdateRegion(region);

			var result = _graph.GetRegion(region.Id);
			Assert.AreEqual("Updated", result.Title);
			Assert.AreEqual(new Rect(100, 100, 400, 300), result.Bounds);
		}

		[Test]
		public void StateMachineGraph_Regions_InitiallyEmpty()
		{
			Assert.AreEqual(0, _graph.Regions.Count);
		}

		[Test]
		public void StateMachineGraph_MultipleRegions_CanBeAdded()
		{
			for (var i = 0; i < 5; i++)
			{
				_graph.AddRegion(new GraphRegion($"Region {i}", new Rect(i * 100, 0, 80, 60)));
			}

			Assert.AreEqual(5, _graph.Regions.Count);
		}


		[Test]
		public void GraphRegion_ContainedNodeIds_InitiallyEmpty()
		{
			var region = new GraphRegion();

			Assert.IsNotNull(region.ContainedNodeIds);
			Assert.AreEqual(0, region.ContainedNodeIds.Count);
		}

		[Test]
		public void GraphRegion_AddNode_AddsNodeId()
		{
			var region = new GraphRegion();

			region.AddNode("node-1");

			Assert.AreEqual(1, region.ContainedNodeIds.Count);
			Assert.IsTrue(region.ContainedNodeIds.Contains("node-1"));
		}

		[Test]
		public void GraphRegion_AddNode_DuplicateIgnored()
		{
			var region = new GraphRegion();

			region.AddNode("node-1");
			region.AddNode("node-1");

			Assert.AreEqual(1, region.ContainedNodeIds.Count);
		}

		[Test]
		public void GraphRegion_AddNode_NullIgnored()
		{
			var region = new GraphRegion();

			region.AddNode(null);

			Assert.AreEqual(0, region.ContainedNodeIds.Count);
		}

		[Test]
		public void GraphRegion_AddNode_EmptyStringIgnored()
		{
			var region = new GraphRegion();

			region.AddNode("");

			Assert.AreEqual(0, region.ContainedNodeIds.Count);
		}

		[Test]
		public void GraphRegion_AddNode_MultipleNodes()
		{
			var region = new GraphRegion();

			region.AddNode("node-1");
			region.AddNode("node-2");
			region.AddNode("node-3");

			Assert.AreEqual(3, region.ContainedNodeIds.Count);
		}

		[Test]
		public void GraphRegion_RemoveNode_RemovesNodeId()
		{
			var region = new GraphRegion();
			region.AddNode("node-1");
			region.AddNode("node-2");

			region.RemoveNode("node-1");

			Assert.AreEqual(1, region.ContainedNodeIds.Count);
			Assert.IsFalse(region.ContainedNodeIds.Contains("node-1"));
			Assert.IsTrue(region.ContainedNodeIds.Contains("node-2"));
		}

		[Test]
		public void GraphRegion_RemoveNode_NonExistentDoesNotThrow()
		{
			var region = new GraphRegion();

			Assert.DoesNotThrow(() => region.RemoveNode("nonexistent"));
		}

		[Test]
		public void GraphRegion_ContainsNode_ReturnsTrue()
		{
			var region = new GraphRegion();
			region.AddNode("node-1");

			Assert.IsTrue(region.ContainsNode("node-1"));
		}

		[Test]
		public void GraphRegion_ContainsNode_ReturnsFalse()
		{
			var region = new GraphRegion();

			Assert.IsFalse(region.ContainsNode("node-1"));
		}

		[Test]
		public void GraphRegion_ClearNodes_RemovesAllNodes()
		{
			var region = new GraphRegion();
			region.AddNode("node-1");
			region.AddNode("node-2");
			region.AddNode("node-3");

			region.ClearNodes();

			Assert.AreEqual(0, region.ContainedNodeIds.Count);
		}

		[Test]
		public void GraphRegion_Serialization_PreservesContainedNodes()
		{
			var region = new GraphRegion("Test", new Rect(0, 0, 300, 200));
			region.AddNode("node-1");
			region.AddNode("node-2");
			_graph.AddRegion(region);

			// Simulate serialization by accessing through graph
			var loadedRegion = _graph.GetRegion(region.Id);

			Assert.AreEqual(2, loadedRegion.ContainedNodeIds.Count);
			Assert.IsTrue(loadedRegion.ContainsNode("node-1"));
			Assert.IsTrue(loadedRegion.ContainsNode("node-2"));
		}

		[Test]
		public void StateMachineGraph_RemoveNode_CleansUpRegionContainment()
		{
			var node1 = new EntryNode("node-1");
			var node2 = new EntryNode("node-2");
			_graph.AddNode(node1);
			_graph.AddNode(node2);

			var region = new GraphRegion("Test", new Rect(0, 0, 300, 200));
			region.AddNode(node1.Id);
			region.AddNode(node2.Id);
			_graph.AddRegion(region);

			_graph.RemoveNode(node1.Id);

			var loadedRegion = _graph.GetRegion(region.Id);
			Assert.AreEqual(1, loadedRegion.ContainedNodeIds.Count);
			Assert.IsFalse(loadedRegion.ContainsNode(node1.Id));
			Assert.IsTrue(loadedRegion.ContainsNode(node2.Id));
		}

		[Test]
		public void StateMachineGraph_RemoveNode_CleansUpMultipleRegions()
		{
			var node = new EntryNode("shared-node");
			_graph.AddNode(node);

			var region1 = new GraphRegion("Region 1", new Rect(0, 0, 300, 200));
			var region2 = new GraphRegion("Region 2", new Rect(400, 0, 300, 200));
			region1.AddNode(node.Id);
			region2.AddNode(node.Id);
			_graph.AddRegion(region1);
			_graph.AddRegion(region2);

			_graph.RemoveNode(node.Id);

			Assert.IsFalse(_graph.GetRegion(region1.Id).ContainsNode(node.Id));
			Assert.IsFalse(_graph.GetRegion(region2.Id).ContainsNode(node.Id));
		}


		[Test]
		public void GraphRegion_ContainedNodes_PreservedAfterUpdateRegion()
		{
			var region = new GraphRegion("Test", new Rect(0, 0, 300, 200));
			_graph.AddRegion(region);

			// Simulate adding nodes to the region (as GraphRegionView.OnElementsAdded does)
			region.AddNode("node-1");
			region.AddNode("node-2");
			_graph.UpdateRegion(region);

			// Retrieve region and verify containment persisted
			var result = _graph.GetRegion(region.Id);
			Assert.AreEqual(2, result.ContainedNodeIds.Count);
			Assert.IsTrue(result.ContainsNode("node-1"));
			Assert.IsTrue(result.ContainsNode("node-2"));
		}

		[Test]
		public void GraphRegion_ContainedNodes_PreservedAfterRemovalAndUpdate()
		{
			var region = new GraphRegion("Test", new Rect(0, 0, 300, 200));
			region.AddNode("node-1");
			region.AddNode("node-2");
			region.AddNode("node-3");
			_graph.AddRegion(region);

			// Simulate removing a node (as GraphRegionView.OnElementsRemoved does)
			region.RemoveNode("node-2");
			_graph.UpdateRegion(region);

			var result = _graph.GetRegion(region.Id);
			Assert.AreEqual(2, result.ContainedNodeIds.Count);
			Assert.IsTrue(result.ContainsNode("node-1"));
			Assert.IsFalse(result.ContainsNode("node-2"));
			Assert.IsTrue(result.ContainsNode("node-3"));
		}


		[Test]
		public void GraphRegion_BoundsPosition_PreservedAfterUpdateRegion()
		{
			var bounds = new Rect(150, 250, 400, 300);
			var region = new GraphRegion("Positioned Region", bounds);
			_graph.AddRegion(region);

			// Simulate a move: update bounds and persist
			var newBounds = new Rect(500, 600, 400, 300);
			region.Bounds = newBounds;
			_graph.UpdateRegion(region);

			var result = _graph.GetRegion(region.Id);
			Assert.AreEqual(500, result.Bounds.x);
			Assert.AreEqual(600, result.Bounds.y);
			Assert.AreEqual(400, result.Bounds.width);
			Assert.AreEqual(300, result.Bounds.height);
		}

		[Test]
		public void GraphRegion_BoundsPosition_PreservedThroughMultipleUpdates()
		{
			var region = new GraphRegion("Moving Region", new Rect(0, 0, 300, 200));
			_graph.AddRegion(region);

			// Simulate multiple moves
			for (var i = 1; i <= 5; i++)
			{
				region.Bounds = new Rect(i * 100, i * 50, 300, 200);
				_graph.UpdateRegion(region);
			}

			var result = _graph.GetRegion(region.Id);
			Assert.AreEqual(500, result.Bounds.x);
			Assert.AreEqual(250, result.Bounds.y);
		}

		[Test]
		public void GraphRegion_BoundsSize_PreservedAfterResize()
		{
			var region = new GraphRegion("Resizable Region", new Rect(100, 100, 300, 200));
			_graph.AddRegion(region);

			// Simulate resize
			region.Bounds = new Rect(100, 100, 600, 400);
			_graph.UpdateRegion(region);

			var result = _graph.GetRegion(region.Id);
			Assert.AreEqual(600, result.Bounds.width);
			Assert.AreEqual(400, result.Bounds.height);
		}

		[Test]
		public void GraphRegion_BoundsAndNodePositions_IndependentThroughSerialization()
		{
			// Set up a region with known bounds containing nodes at absolute positions
			var regionBounds = new Rect(200, 300, 400, 300);
			var region = new GraphRegion("Positioned Region", regionBounds);

			var node1 = new EntryNode("node-1");
			node1.Position = new Vector2(250, 350);
			var node2 = new EntryNode("node-2");
			node2.Position = new Vector2(400, 450);

			_graph.AddNode(node1);
			_graph.AddNode(node2);
			region.AddNode(node1.Id);
			region.AddNode(node2.Id);
			_graph.AddRegion(region);

			// Trigger serialization round-trip
			_graph.OnBeforeSerialize();
			_graph.OnAfterDeserialize();

			// Verify region bounds preserved independently
			var loadedRegion = _graph.GetRegion(region.Id);
			Assert.AreEqual(200, loadedRegion.Bounds.x, "Region X position should be preserved");
			Assert.AreEqual(300, loadedRegion.Bounds.y, "Region Y position should be preserved");
			Assert.AreEqual(400, loadedRegion.Bounds.width, "Region width should be preserved");
			Assert.AreEqual(300, loadedRegion.Bounds.height, "Region height should be preserved");

			// Verify node positions preserved independently (absolute, not relative to region)
			var loadedNode1 = _graph.GetNode("node-1");
			var loadedNode2 = _graph.GetNode("node-2");
			Assert.AreEqual(250, loadedNode1.Position.x, "Node1 X should be preserved as absolute");
			Assert.AreEqual(350, loadedNode1.Position.y, "Node1 Y should be preserved as absolute");
			Assert.AreEqual(400, loadedNode2.Position.x, "Node2 X should be preserved as absolute");
			Assert.AreEqual(450, loadedNode2.Position.y, "Node2 Y should be preserved as absolute");

			// Verify region still contains the nodes
			Assert.IsTrue(loadedRegion.ContainsNode("node-1"));
			Assert.IsTrue(loadedRegion.ContainsNode("node-2"));
		}

		[Test]
		public void GraphRegion_BoundsNotAffectedByContainedNodePositions()
		{
			// Region bounds should remain exactly as set, regardless of contained node positions
			var regionBounds = new Rect(100, 100, 500, 400);
			var region = new GraphRegion("Fixed Region", regionBounds);

			// Add a node at a position OUTSIDE the region bounds
			var node = new EntryNode("outside-node");
			node.Position = new Vector2(900, 900);
			_graph.AddNode(node);
			region.AddNode(node.Id);
			_graph.AddRegion(region);

			// Trigger serialization round-trip
			_graph.OnBeforeSerialize();
			_graph.OnAfterDeserialize();

			// Region bounds must be unchanged - not recalculated from node positions
			var loadedRegion = _graph.GetRegion(region.Id);
			Assert.AreEqual(100, loadedRegion.Bounds.x, "Region bounds should not be recalculated from nodes");
			Assert.AreEqual(100, loadedRegion.Bounds.y);
			Assert.AreEqual(500, loadedRegion.Bounds.width);
			Assert.AreEqual(400, loadedRegion.Bounds.height);
		}

		[Test]
		public void GraphRegion_MovingRegionBounds_DoesNotAffectNodePositions()
		{
			var region = new GraphRegion("Movable Region", new Rect(100, 100, 300, 200));
			var node = new EntryNode("contained-node");
			node.Position = new Vector2(150, 150);
			_graph.AddNode(node);
			region.AddNode(node.Id);
			_graph.AddRegion(region);

			// Move the region to a new position
			region.Bounds = new Rect(500, 500, 300, 200);
			_graph.UpdateRegion(region);

			// Trigger serialization round-trip
			_graph.OnBeforeSerialize();
			_graph.OnAfterDeserialize();

			// Node position should be unchanged (absolute coordinates, not relative to region)
			var loadedNode = _graph.GetNode("contained-node");
			Assert.AreEqual(150, loadedNode.Position.x, "Node position should not move with region");
			Assert.AreEqual(150, loadedNode.Position.y, "Node position should not move with region");
		}

	}
}
