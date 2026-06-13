using System;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Editor.Layout;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class NodeSizeEstimatorTests
	{
		private class SimpleState : State
		{
			[Transition("Done")]
			public Action OnComplete;

			public override void OnEnter() { }
		}

		private class MultiPortState : State
		{
			[DataIn("Health")]
			public int Health;

			[DataIn("Name")]
			public string Name;

			[DataIn("Speed")]
			public float Speed;

			[Transition("Done")]
			public Action OnDone;

			[Transition("Failed")]
			public Action OnFailed;

			[DataOut("Result")]
			public bool Result;

			public override void OnEnter() { }
		}

		[Test]
		public void EstimateSize_Null_ReturnsDefaultSize()
		{
			var size = NodeSizeEstimator.EstimateSize(null);

			Assert.AreEqual(280f, size.x);
			Assert.AreEqual(120f, size.y);
		}

		[Test]
		public void EstimateSize_EntryNode_ReturnsSmallWidth()
		{
			var node = new EntryNode();

			var size = NodeSizeEstimator.EstimateSize(node);

			Assert.AreEqual(120f, size.x);
			Assert.Greater(size.y, 0f);
		}

		[Test]
		public void EstimateSize_ExitNode_ReturnsSameSizeAsEntry()
		{
			var entryNode = new EntryNode();
			var exitNode = new ExitNode();

			var entrySize = NodeSizeEstimator.EstimateSize(entryNode);
			var exitSize = NodeSizeEstimator.EstimateSize(exitNode);

			Assert.AreEqual(entrySize.x, exitSize.x);
			Assert.AreEqual(entrySize.y, exitSize.y);
		}

		[Test]
		public void EstimateSize_StateNode_NullStateType_ReturnsDefaultStateSize()
		{
			var node = new StateNode();

			var size = NodeSizeEstimator.EstimateSize(node);

			Assert.AreEqual(280f, size.x);
			Assert.AreEqual(120f, size.y);
		}

		[Test]
		public void EstimateSize_StateNode_WithSimpleState_ReturnsSize()
		{
			var node = new StateNode();
			node.StateType = typeof(SimpleState);

			var size = NodeSizeEstimator.EstimateSize(node);

			Assert.GreaterOrEqual(size.x, 280f, "Width should be at least the minimum state width");
			Assert.Greater(size.y, 0f, "Height should be positive");
		}

		[Test]
		public void EstimateSize_StateNode_WithMultiPortState_TallerThanSimple()
		{
			var simpleNode = new StateNode();
			simpleNode.StateType = typeof(SimpleState);

			var multiNode = new StateNode();
			multiNode.StateType = typeof(MultiPortState);

			var simpleSize = NodeSizeEstimator.EstimateSize(simpleNode);
			var multiSize = NodeSizeEstimator.EstimateSize(multiNode);

			Assert.Greater(multiSize.y, simpleSize.y,
				"Multi-port state should be taller than simple state");
		}

		[Test]
		public void EstimateSize_BlackboardVariableNode_ReturnsBlackboardWidth()
		{
			var node = new BlackboardVariableNode();

			var size = NodeSizeEstimator.EstimateSize(node);

			Assert.AreEqual(160f, size.x);
			Assert.Greater(size.y, 0f);
		}

		[Test]
		public void EstimateSize_SubStateMachineNode_ReturnsSubStateMachineWidth()
		{
			var node = new SubStateMachineNode();

			var size = NodeSizeEstimator.EstimateSize(node);

			Assert.AreEqual(280f, size.x);
			Assert.Greater(size.y, 0f);
		}

		[Test]
		public void EstimateSize_SubStateMachineNode_TallerThanEntryNode()
		{
			var subNode = new SubStateMachineNode();
			var entryNode = new EntryNode();

			var subSize = NodeSizeEstimator.EstimateSize(subNode);
			var entrySize = NodeSizeEstimator.EstimateSize(entryNode);

			Assert.Greater(subSize.y, entrySize.y,
				"SubStateMachine should be taller than entry node");
		}

		[Test]
		public void EstimateSize_PortalInNode_ReturnsPortalWidth()
		{
			var node = new PortalInNode();

			var size = NodeSizeEstimator.EstimateSize(node);

			Assert.AreEqual(180f, size.x);
			Assert.Greater(size.y, 0f);
		}

		[Test]
		public void EstimateSize_PortalOutNode_ReturnsPortalWidth()
		{
			var node = new PortalOutNode();

			var size = NodeSizeEstimator.EstimateSize(node);

			Assert.AreEqual(180f, size.x);
			Assert.Greater(size.y, 0f);
		}

		[Test]
		public void EstimateSize_PortalIn_And_PortalOut_SameSize()
		{
			var portalIn = new PortalInNode();
			var portalOut = new PortalOutNode();

			var inSize = NodeSizeEstimator.EstimateSize(portalIn);
			var outSize = NodeSizeEstimator.EstimateSize(portalOut);

			Assert.AreEqual(inSize.x, outSize.x);
			Assert.AreEqual(inSize.y, outSize.y);
		}

		[Test]
		public void EstimateSize_EntryNode_SmallerThanStateNode()
		{
			var entryNode = new EntryNode();
			var stateNode = new StateNode();
			stateNode.StateType = typeof(SimpleState);

			var entrySize = NodeSizeEstimator.EstimateSize(entryNode);
			var stateSize = NodeSizeEstimator.EstimateSize(stateNode);

			Assert.Less(entrySize.x, stateSize.x,
				"Entry node should be narrower than state node");
		}

		[Test]
		public void EstimateSize_AllNodeTypes_ReturnPositiveDimensions()
		{
			var nodes = new GraphNode[]
			{
				new EntryNode(),
				new ExitNode(),
				new StateNode(),
				new SubStateMachineNode(),
				new BlackboardVariableNode(),
				new PortalInNode(),
				new PortalOutNode()
			};

			foreach (var node in nodes)
			{
				var size = NodeSizeEstimator.EstimateSize(node);

				Assert.Greater(size.x, 0f, $"{node.NodeType} width should be positive");
				Assert.Greater(size.y, 0f, $"{node.NodeType} height should be positive");
			}
		}
	}
}
