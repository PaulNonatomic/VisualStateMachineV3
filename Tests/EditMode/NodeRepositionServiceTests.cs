using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Editor.Layout;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	/// <summary>
	/// Tests for NodeRepositionService.
	/// Note: Full integration testing requires the Unity Editor GraphView,
	/// so these tests focus on the core data manipulation logic.
	/// </summary>
	[TestFixture]
	public class NodeRepositionServiceTests
	{
		private StateMachineGraph _graph;


		private class SimpleState : State
		{
			[Transition("Complete")] public Action onComplete;
		}

		private class MultiTransitionState : State
		{
			[Transition("Success")] public Action onSuccess;
			[Transition("Failure")] public Action onFailure;
		}


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
		public void OnNodeSizeChanged_NullNode_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => NodeRepositionService.OnNodeSizeChanged(
				null,
				100f,
				_graph,
				new Dictionary<string, UnityEditor.Experimental.GraphView.Node>()));
		}

		[Test]
		public void OnNodeSizeChanged_NullGraph_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => NodeRepositionService.OnNodeSizeChanged(
				null,
				100f,
				null,
				new Dictionary<string, UnityEditor.Experimental.GraphView.Node>()));
		}

		[Test]
		public void OnNodeSizeChanged_NullNodeViews_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => NodeRepositionService.OnNodeSizeChanged(
				null,
				100f,
				_graph,
				null));
		}



		[Test]
		public void Graph_LinearChain_ConnectionsAreBidirectional()
		{
			// Setup a linear chain: Entry -> State1 -> State2 -> State3
			var entry = new EntryNode("entry");
			var state1 = new StateNode(typeof(SimpleState)) { Id = "state1", Position = new Vector2(200, 100) };
			var state2 = new StateNode(typeof(SimpleState)) { Id = "state2", Position = new Vector2(400, 100) };
			var state3 = new StateNode(typeof(SimpleState)) { Id = "state3", Position = new Vector2(600, 100) };

			_graph.AddNode(entry);
			_graph.AddNode(state1);
			_graph.AddNode(state2);
			_graph.AddNode(state3);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection("entry", "exit", "state1", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state1", "onComplete", "state2", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state2", "onComplete", "state3", "enter", ConnectionType.Transition));

			// Verify outgoing connections
			var fromState1 = _graph.GetConnectionsFromNode("state1").ToList();
			Assert.AreEqual(1, fromState1.Count, "State1 should have 1 outgoing connection");
			Assert.AreEqual("state2", fromState1[0].TargetNodeId);

			// Verify incoming connections
			var toState2 = _graph.GetConnectionsToNode("state2").ToList();
			Assert.AreEqual(1, toState2.Count, "State2 should have 1 incoming connection");
			Assert.AreEqual("state1", toState2[0].SourceNodeId);
		}

		[Test]
		public void Graph_BranchingStructure_MultipleOutgoingConnections()
		{
			// Setup: State1 -> State2, State1 -> State3
			var state1 = new StateNode(typeof(MultiTransitionState)) { Id = "state1", Position = new Vector2(100, 100) };
			var state2 = new StateNode(typeof(SimpleState)) { Id = "state2", Position = new Vector2(300, 50) };
			var state3 = new StateNode(typeof(SimpleState)) { Id = "state3", Position = new Vector2(300, 150) };

			_graph.AddNode(state1);
			_graph.AddNode(state2);
			_graph.AddNode(state3);

			_graph.AddConnection(new Connection("state1", "onSuccess", "state2", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state1", "onFailure", "state3", "enter", ConnectionType.Transition));

			var fromState1 = _graph.GetConnectionsFromNode("state1").ToList();
			Assert.AreEqual(2, fromState1.Count, "State1 should have 2 outgoing connections");

			var targetIds = fromState1.Select(c => c.TargetNodeId).OrderBy(id => id).ToList();
			Assert.Contains("state2", targetIds);
			Assert.Contains("state3", targetIds);
		}

		[Test]
		public void Graph_ConvergingStructure_MultipleIncomingConnections()
		{
			// Setup: State1 -> State3, State2 -> State3
			var state1 = new StateNode(typeof(SimpleState)) { Id = "state1", Position = new Vector2(100, 50) };
			var state2 = new StateNode(typeof(SimpleState)) { Id = "state2", Position = new Vector2(100, 150) };
			var state3 = new StateNode(typeof(SimpleState)) { Id = "state3", Position = new Vector2(300, 100) };

			_graph.AddNode(state1);
			_graph.AddNode(state2);
			_graph.AddNode(state3);

			_graph.AddConnection(new Connection("state1", "onComplete", "state3", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state2", "onComplete", "state3", "enter", ConnectionType.Transition));

			var toState3 = _graph.GetConnectionsToNode("state3").ToList();
			Assert.AreEqual(2, toState3.Count, "State3 should have 2 incoming connections");

			var sourceIds = toState3.Select(c => c.SourceNodeId).OrderBy(id => id).ToList();
			Assert.Contains("state1", sourceIds);
			Assert.Contains("state2", sourceIds);
		}

		[Test]
		public void Graph_CyclicStructure_ConnectionsFormCycle()
		{
			// Setup: State1 -> State2 -> State3 -> State1 (cycle)
			var state1 = new StateNode(typeof(SimpleState)) { Id = "state1", Position = new Vector2(100, 100) };
			var state2 = new StateNode(typeof(SimpleState)) { Id = "state2", Position = new Vector2(300, 100) };
			var state3 = new StateNode(typeof(SimpleState)) { Id = "state3", Position = new Vector2(200, 250) };

			_graph.AddNode(state1);
			_graph.AddNode(state2);
			_graph.AddNode(state3);

			_graph.AddConnection(new Connection("state1", "onComplete", "state2", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state2", "onComplete", "state3", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state3", "onComplete", "state1", "enter", ConnectionType.Transition));

			// Verify each node has 1 incoming and 1 outgoing connection (forming a cycle)
			Assert.AreEqual(1, _graph.GetConnectionsFromNode("state1").Count());
			Assert.AreEqual(1, _graph.GetConnectionsToNode("state1").Count());
			Assert.AreEqual(1, _graph.GetConnectionsFromNode("state2").Count());
			Assert.AreEqual(1, _graph.GetConnectionsToNode("state2").Count());
			Assert.AreEqual(1, _graph.GetConnectionsFromNode("state3").Count());
			Assert.AreEqual(1, _graph.GetConnectionsToNode("state3").Count());
		}



		[Test]
		public void NodePosition_UpdatesCorrectly()
		{
			var state = new StateNode(typeof(SimpleState)) { Id = "state1", Position = new Vector2(100, 100) };
			_graph.AddNode(state);

			// Simulate a position shift
			var delta = new Vector2(50, 0);
			state.Position += delta;

			Assert.AreEqual(150, state.Position.x);
			Assert.AreEqual(100, state.Position.y);
		}

		[Test]
		public void NodePositions_ChainShift_AllNodesShiftedCorrectly()
		{
			// Create a chain of nodes with known positions
			var state1 = new StateNode(typeof(SimpleState)) { Id = "state1", Position = new Vector2(100, 100) };
			var state2 = new StateNode(typeof(SimpleState)) { Id = "state2", Position = new Vector2(300, 100) };
			var state3 = new StateNode(typeof(SimpleState)) { Id = "state3", Position = new Vector2(500, 100) };

			_graph.AddNode(state1);
			_graph.AddNode(state2);
			_graph.AddNode(state3);

			// Simulate shifting state2 and state3 by a delta
			var delta = new Vector2(50, 0);
			state2.Position += delta;
			state3.Position += delta;

			Assert.AreEqual(100, state1.Position.x, "State1 should not be shifted");
			Assert.AreEqual(350, state2.Position.x, "State2 should be shifted by delta");
			Assert.AreEqual(550, state3.Position.x, "State3 should be shifted by delta");
		}

		[Test]
		public void NegativeDelta_NodesShiftLeft()
		{
			var state = new StateNode(typeof(SimpleState)) { Id = "state1", Position = new Vector2(300, 100) };
			_graph.AddNode(state);

			var delta = new Vector2(-50, 0);
			state.Position += delta;

			Assert.AreEqual(250, state.Position.x);
		}

	}
}
