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
	[TestFixture]
	public class GraphLayoutServiceTests
	{
		private StateMachineGraph _graph;


		private class SimpleState : State
		{
			[Transition("Complete")] public Action onComplete;
		}

		private class MultiTransitionState : State
		{
			[Transition("Success", Priority = 1)] public Action onSuccess;
			[Transition("Failure", Priority = 2)] public Action onFailure;
			[Transition("Cancel", Priority = 3)] public Action onCancel;
		}

		private class InputOutputState : State
		{
			[DataIn] public float speed;
			[DataIn] public Vector3 target;
			[DataOut] public float result;
			[Transition] public Action onDone;
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
		public void AutoLayout_NullGraph_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => GraphLayoutService.AutoLayout(null));
		}

		[Test]
		public void AutoLayout_EmptyGraph_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => GraphLayoutService.AutoLayout(_graph));
		}

		[Test]
		public void AutoLayout_GraphWithNoEntryNode_DoesNotThrow()
		{
			var state = new StateNode(typeof(SimpleState)) { Id = "state1" };
			_graph.AddNode(state);

			Assert.DoesNotThrow(() => GraphLayoutService.AutoLayout(_graph));
		}



		[Test]
		public void AutoLayout_SingleEntryNode_PositionsAtStart()
		{
			var entry = new EntryNode("entry");
			_graph.AddNode(entry);
			_graph.EntryNodeId = "entry";

			GraphLayoutService.AutoLayout(_graph);

			// Entry node should be positioned at starting coordinates
			Assert.AreEqual(100f, entry.Position.x, 1f);
		}

		[Test]
		public void AutoLayout_EntryConnectedToState_StateIsRightOfEntry()
		{
			var entry = new EntryNode("entry");
			var state = new StateNode(typeof(SimpleState)) { Id = "state1" };

			_graph.AddNode(entry);
			_graph.AddNode(state);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection("entry", "exit", "state1", "enter", ConnectionType.Transition));

			GraphLayoutService.AutoLayout(_graph);

			// State should be to the right of entry
			Assert.Greater(state.Position.x, entry.Position.x);
		}

		[Test]
		public void AutoLayout_LinearChain_NodesArrangedLeftToRight()
		{
			var entry = new EntryNode("entry");
			var state1 = new StateNode(typeof(SimpleState)) { Id = "state1" };
			var state2 = new StateNode(typeof(SimpleState)) { Id = "state2" };
			var exit = new ExitNode("exit");

			_graph.AddNode(entry);
			_graph.AddNode(state1);
			_graph.AddNode(state2);
			_graph.AddNode(exit);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection("entry", "exit", "state1", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state1", "onComplete", "state2", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state2", "onComplete", "exit", "enter", ConnectionType.Transition));

			GraphLayoutService.AutoLayout(_graph);

			// Verify left-to-right ordering
			Assert.Less(entry.Position.x, state1.Position.x);
			Assert.Less(state1.Position.x, state2.Position.x);
			Assert.Less(state2.Position.x, exit.Position.x);
		}

		[Test]
		public void AutoLayout_BranchingStates_ChildrenArrangedVertically()
		{
			var entry = new EntryNode("entry");
			var branchState = new StateNode(typeof(MultiTransitionState)) { Id = "branch" };
			var successState = new StateNode(typeof(SimpleState)) { Id = "success" };
			var failState = new StateNode(typeof(SimpleState)) { Id = "fail" };

			_graph.AddNode(entry);
			_graph.AddNode(branchState);
			_graph.AddNode(successState);
			_graph.AddNode(failState);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection("entry", "exit", "branch", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("branch", "onSuccess", "success", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("branch", "onFailure", "fail", "enter", ConnectionType.Transition));

			GraphLayoutService.AutoLayout(_graph);

			// Both child states should be to the right of branch
			Assert.Greater(successState.Position.x, branchState.Position.x);
			Assert.Greater(failState.Position.x, branchState.Position.x);

			// Child states should be at approximately the same X (same layer)
			Assert.AreEqual(successState.Position.x, failState.Position.x, 1f);

			// Child states should have different Y positions (stacked vertically)
			Assert.AreNotEqual(successState.Position.y, failState.Position.y);
		}



		[Test]
		public void AutoLayout_DisconnectedState_PlacedAfterLastLayer()
		{
			var entry = new EntryNode("entry");
			var connectedState = new StateNode(typeof(SimpleState)) { Id = "connected" };
			var disconnectedState = new StateNode(typeof(SimpleState)) { Id = "disconnected" };

			_graph.AddNode(entry);
			_graph.AddNode(connectedState);
			_graph.AddNode(disconnectedState);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection("entry", "exit", "connected", "enter", ConnectionType.Transition));

			GraphLayoutService.AutoLayout(_graph);

			// Disconnected state should be placed after connected state
			Assert.Greater(disconnectedState.Position.x, connectedState.Position.x);
		}

		[Test]
		public void AutoLayout_BlackboardNode_PlacedToLeftOfEntry()
		{
			var entry = new EntryNode("entry");
			var blackboardNode = new BlackboardVariableNode("speed", typeof(float), BlackboardNodeMode.Get)
			{
				Id = "bb-speed"
			};

			_graph.AddNode(entry);
			_graph.AddNode(blackboardNode);
			_graph.EntryNodeId = "entry";

			GraphLayoutService.AutoLayout(_graph);

			// Blackboard node should be to the left of entry (layer -1)
			Assert.Less(blackboardNode.Position.x, entry.Position.x);
		}

		[Test]
		public void AutoLayout_MultipleBlackboardNodes_GroupedTogether()
		{
			var entry = new EntryNode("entry");
			var bb1 = new BlackboardVariableNode("var1", typeof(float), BlackboardNodeMode.Get) { Id = "bb1" };
			var bb2 = new BlackboardVariableNode("var2", typeof(int), BlackboardNodeMode.Get) { Id = "bb2" };
			var bb3 = new BlackboardVariableNode("var3", typeof(string), BlackboardNodeMode.Get) { Id = "bb3" };

			_graph.AddNode(entry);
			_graph.AddNode(bb1);
			_graph.AddNode(bb2);
			_graph.AddNode(bb3);
			_graph.EntryNodeId = "entry";

			GraphLayoutService.AutoLayout(_graph);

			// All blackboard nodes should have the same X position (same layer)
			Assert.AreEqual(bb1.Position.x, bb2.Position.x, 1f);
			Assert.AreEqual(bb2.Position.x, bb3.Position.x, 1f);

			// All should be left of entry
			Assert.Less(bb1.Position.x, entry.Position.x);
		}



		[Test]
		public void AutoLayout_PortalNodes_PortalInFollowsPortalOut()
		{
			var entry = new EntryNode("entry");
			var state1 = new StateNode(typeof(SimpleState)) { Id = "state1" };
			var portalOut = new PortalOutNode(1, "Channel1") { Id = "portal-out" };
			var portalIn = new PortalInNode(1, "Channel1") { Id = "portal-in" };
			var state2 = new StateNode(typeof(SimpleState)) { Id = "state2" };

			_graph.AddNode(entry);
			_graph.AddNode(state1);
			_graph.AddNode(portalOut);
			_graph.AddNode(portalIn);
			_graph.AddNode(state2);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection("entry", "exit", "state1", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state1", "onComplete", "portal-out", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("portal-in", "exit", "state2", "enter", ConnectionType.Transition));

			GraphLayoutService.AutoLayout(_graph);

			// Portal-in should be to the right of portal-out (next layer)
			Assert.Greater(portalIn.Position.x, portalOut.Position.x);
		}



		[Test]
		public void GridLayout_NullGraph_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => GraphLayoutService.GridLayout(null));
		}

		[Test]
		public void GridLayout_EmptyGraph_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => GraphLayoutService.GridLayout(_graph));
		}

		[Test]
		public void GridLayout_SingleNode_PositionsAtStart()
		{
			var entry = new EntryNode("entry");
			_graph.AddNode(entry);

			GraphLayoutService.GridLayout(_graph);

			Assert.AreEqual(100f, entry.Position.x, 1f);
			Assert.AreEqual(300f, entry.Position.y, 1f);
		}

		[Test]
		public void GridLayout_FourNodes_ArrangedInGrid()
		{
			var node1 = new StateNode(typeof(SimpleState)) { Id = "node1", DisplayName = "A" };
			var node2 = new StateNode(typeof(SimpleState)) { Id = "node2", DisplayName = "B" };
			var node3 = new StateNode(typeof(SimpleState)) { Id = "node3", DisplayName = "C" };
			var node4 = new StateNode(typeof(SimpleState)) { Id = "node4", DisplayName = "D" };

			_graph.AddNode(node1);
			_graph.AddNode(node2);
			_graph.AddNode(node3);
			_graph.AddNode(node4);

			GraphLayoutService.GridLayout(_graph, columns: 2);

			// First row
			Assert.AreEqual(node1.Position.y, node2.Position.y, 1f);
			// Second row
			Assert.AreEqual(node3.Position.y, node4.Position.y, 1f);
			// Rows are different
			Assert.AreNotEqual(node1.Position.y, node3.Position.y);
		}

		[Test]
		public void GridLayout_SortsEntryFirst()
		{
			var state = new StateNode(typeof(SimpleState)) { Id = "state", DisplayName = "State" };
			var entry = new EntryNode("entry");
			var exit = new ExitNode("exit");

			// Add in non-sorted order
			_graph.AddNode(state);
			_graph.AddNode(exit);
			_graph.AddNode(entry);

			GraphLayoutService.GridLayout(_graph, columns: 10);

			// Entry should be first (leftmost)
			Assert.Less(entry.Position.x, state.Position.x);
			Assert.Less(entry.Position.x, exit.Position.x);
		}

		[Test]
		public void GridLayout_CustomColumnCount_RespectsColumns()
		{
			for (int i = 0; i < 6; i++)
			{
				_graph.AddNode(new StateNode(typeof(SimpleState)) { Id = $"node{i}", DisplayName = $"Node{i}" });
			}

			GraphLayoutService.GridLayout(_graph, columns: 3);

			var nodes = _graph.GetAllNodes().ToList();

			// With 3 columns and 6 nodes, should have 2 rows
			// Nodes at index 0, 1, 2 should be first row
			// Nodes at index 3, 4, 5 should be second row
			var row1Y = nodes[0].Position.y;
			var row2Y = nodes[3].Position.y;

			Assert.AreNotEqual(row1Y, row2Y);
		}



		[Test]
		public void AutoLayout_CyclicGraph_DoesNotInfiniteLoop()
		{
			var entry = new EntryNode("entry");
			var state1 = new StateNode(typeof(SimpleState)) { Id = "state1" };
			var state2 = new StateNode(typeof(SimpleState)) { Id = "state2" };

			_graph.AddNode(entry);
			_graph.AddNode(state1);
			_graph.AddNode(state2);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection("entry", "exit", "state1", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state1", "onComplete", "state2", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state2", "onComplete", "state1", "enter", ConnectionType.Transition)); // Cycle

			// Should not hang or throw
			Assert.DoesNotThrow(() => GraphLayoutService.AutoLayout(_graph));

			// Both states should have valid positions
			Assert.IsFalse(float.IsNaN(state1.Position.x));
			Assert.IsFalse(float.IsNaN(state2.Position.x));
		}

		[Test]
		public void AutoLayout_LargeGraph_CompletesInReasonableTime()
		{
			var entry = new EntryNode("entry");
			_graph.AddNode(entry);
			_graph.EntryNodeId = "entry";

			// Create a large linear chain
			var prevNodeId = "entry";
			for (int i = 0; i < 100; i++)
			{
				var state = new StateNode(typeof(SimpleState)) { Id = $"state{i}" };
				_graph.AddNode(state);
				_graph.AddConnection(new Connection(prevNodeId, "onComplete", $"state{i}", "enter", ConnectionType.Transition));
				prevNodeId = $"state{i}";
			}

			var startTime = System.Diagnostics.Stopwatch.StartNew();
			GraphLayoutService.AutoLayout(_graph);
			startTime.Stop();

			// Should complete in under 5 seconds (generous for slow machines)
			Assert.Less(startTime.ElapsedMilliseconds, 5000);
		}

		[Test]
		public void AutoLayout_NoDataConnections_OnlyTransitionsAffectLayout()
		{
			var entry = new EntryNode("entry");
			var state1 = new StateNode(typeof(InputOutputState)) { Id = "state1" };
			var state2 = new StateNode(typeof(InputOutputState)) { Id = "state2" };
			var bbNode = new BlackboardVariableNode("speed", typeof(float), BlackboardNodeMode.Get) { Id = "bb" };

			_graph.AddNode(entry);
			_graph.AddNode(state1);
			_graph.AddNode(state2);
			_graph.AddNode(bbNode);
			_graph.EntryNodeId = "entry";

			// Transition connections
			_graph.AddConnection(new Connection("entry", "exit", "state1", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state1", "onDone", "state2", "enter", ConnectionType.Transition));

			// Data connection (should not affect hierarchical layout)
			_graph.AddConnection(new Connection("bb", "value", "state1", "speed", ConnectionType.BlackboardRead));

			GraphLayoutService.AutoLayout(_graph);

			// Layout should still be hierarchical based on transitions
			Assert.Less(entry.Position.x, state1.Position.x);
			Assert.Less(state1.Position.x, state2.Position.x);
		}

		[Test]
		public void AutoLayout_DiamondPattern_ConvergenceNodeAtCorrectDepth()
		{
			// Entry → A → B → D
			//           ↘ C → D
			// D should be at the same layer as if it's at the end of the longest path
			var entry = new EntryNode("entry");
			var stateA = new StateNode(typeof(MultiTransitionState)) { Id = "a" };
			var stateB = new StateNode(typeof(SimpleState)) { Id = "b" };
			var stateC = new StateNode(typeof(SimpleState)) { Id = "c" };
			var stateD = new StateNode(typeof(SimpleState)) { Id = "d" };

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(stateB);
			_graph.AddNode(stateC);
			_graph.AddNode(stateD);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection("entry", "exit", "a", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("a", "onSuccess", "b", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("a", "onFailure", "c", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("b", "onComplete", "d", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("c", "onComplete", "d", "enter", ConnectionType.Transition));

			GraphLayoutService.AutoLayout(_graph);

			// D should be to the right of both B and C (at the convergence layer)
			Assert.Greater(stateD.Position.x, stateB.Position.x);
			Assert.Greater(stateD.Position.x, stateC.Position.x);

			// B and C should be at the same layer (same X)
			Assert.AreEqual(stateB.Position.x, stateC.Position.x, 1f);
		}

		[Test]
		public void AutoLayout_MultiplePathLengths_UsesLongestPath()
		{
			// Entry → A → B → C → D (long path, length 4)
			// Entry → A → D         (short path, length 2)
			// D should be at layer 4 (longest path), not layer 2
			var entry = new EntryNode("entry");
			var stateA = new StateNode(typeof(MultiTransitionState)) { Id = "a" };
			var stateB = new StateNode(typeof(SimpleState)) { Id = "b" };
			var stateC = new StateNode(typeof(SimpleState)) { Id = "c" };
			var stateD = new StateNode(typeof(SimpleState)) { Id = "d" };

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(stateB);
			_graph.AddNode(stateC);
			_graph.AddNode(stateD);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection("entry", "exit", "a", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("a", "onSuccess", "b", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("b", "onComplete", "c", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("c", "onComplete", "d", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("a", "onFailure", "d", "enter", ConnectionType.Transition)); // Short path

			GraphLayoutService.AutoLayout(_graph);

			// D must be to the right of C (longest path determines layer)
			Assert.Greater(stateD.Position.x, stateC.Position.x);

			// The chain Entry → A → B → C → D should be strictly left-to-right
			Assert.Less(entry.Position.x, stateA.Position.x);
			Assert.Less(stateA.Position.x, stateB.Position.x);
			Assert.Less(stateB.Position.x, stateC.Position.x);
			Assert.Less(stateC.Position.x, stateD.Position.x);
		}

		[Test]
		public void AutoLayout_ConnectedBlackboardGetter_PositionedNearTarget()
		{
			var entry = new EntryNode("entry");
			var state1 = new StateNode(typeof(SimpleState)) { Id = "state1" };
			var state2 = new StateNode(typeof(InputOutputState)) { Id = "state2" };
			var bbGetter = new BlackboardVariableNode("speed", typeof(float), BlackboardNodeMode.Get) { Id = "bb-getter" };

			_graph.AddNode(entry);
			_graph.AddNode(state1);
			_graph.AddNode(state2);
			_graph.AddNode(bbGetter);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection("entry", "exit", "state1", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state1", "onComplete", "state2", "enter", ConnectionType.Transition));
			// Blackboard getter connected to state2's input
			_graph.AddConnection(new Connection("bb-getter", "value", "state2", "speed", ConnectionType.BlackboardRead));

			GraphLayoutService.AutoLayout(_graph);

			// Getter should be positioned just to the left of state2 (companion placement)
			Assert.Less(bbGetter.Position.x, state2.Position.x);

			// Should be close to state2, not far away at a different layer
			var gap = state2.Position.x - bbGetter.Position.x;
			Assert.Less(gap, 300f, "Getter should be close to its target, not in a distant layer");
		}

		[Test]
		public void AutoLayout_ConnectedBlackboardSetter_PositionedNearSource()
		{
			var entry = new EntryNode("entry");
			var state1 = new StateNode(typeof(InputOutputState)) { Id = "state1" };
			var state2 = new StateNode(typeof(SimpleState)) { Id = "state2" };
			var bbSetter = new BlackboardVariableNode("result", typeof(float), BlackboardNodeMode.Set) { Id = "bb-setter" };

			_graph.AddNode(entry);
			_graph.AddNode(state1);
			_graph.AddNode(state2);
			_graph.AddNode(bbSetter);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection("entry", "exit", "state1", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state1", "onDone", "state2", "enter", ConnectionType.Transition));
			// Blackboard setter receives data from state1's output
			_graph.AddConnection(new Connection("state1", "result", "bb-setter", "value", ConnectionType.BlackboardWrite));

			GraphLayoutService.AutoLayout(_graph);

			// Setter should be positioned just to the right of state1 (companion placement)
			Assert.Greater(bbSetter.Position.x, state1.Position.x);

			// Should be close to state1, not far away at a different layer
			var gap = bbSetter.Position.x - state1.Position.x;
			Assert.Less(gap, 400f, "Setter should be close to its source, not in a distant layer");
		}

		[Test]
		public void AutoLayout_SelfLoop_NodePositionedCorrectly()
		{
			var entry = new EntryNode("entry");
			var state1 = new StateNode(typeof(MultiTransitionState)) { Id = "state1" };
			var exit = new ExitNode("exit");

			_graph.AddNode(entry);
			_graph.AddNode(state1);
			_graph.AddNode(exit);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection("entry", "exit", "state1", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state1", "onSuccess", "exit", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state1", "onFailure", "state1", "enter", ConnectionType.Transition)); // Self-loop

			Assert.DoesNotThrow(() => GraphLayoutService.AutoLayout(_graph));

			// state1 should be between entry and exit
			Assert.Less(entry.Position.x, state1.Position.x);
			Assert.Less(state1.Position.x, exit.Position.x);
			Assert.IsFalse(float.IsNaN(state1.Position.x));
		}

	}
}

