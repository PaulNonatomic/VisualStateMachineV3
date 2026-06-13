using System.Linq;
using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class GraphValidatorTests
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
		public void Validate_ValidGraphWithEntryAndConnection_NoErrors()
		{
			var stateNode = new StateNode("state-1");
			_graph.AddNode(stateNode);

			var connection = new Connection
			{
				Id = "conn-1",
				SourceNodeId = _graph.EntryNodeId,
				SourcePortName = "output",
				TargetNodeId = stateNode.Id,
				TargetPortName = "input",
				ConnectionType = ConnectionType.Transition
			};
			_graph.AddConnection(connection);

			var issues = GraphValidator.Validate(_graph);

			var errors = issues.Where(i => i.Severity == GraphValidationSeverity.Error).ToList();
			Assert.AreEqual(0, errors.Count, "Valid graph should have no errors");
		}

		[Test]
		public void Validate_MissingEntryNode_ReportsError()
		{
			var entryNodeId = _graph.EntryNodeId;
			_graph.RemoveNode(entryNodeId);
			_graph.EntryNodeId = null;

			var issues = GraphValidator.Validate(_graph);

			Assert.IsTrue(issues.Any(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.Message.Contains("no entry node")));
		}

		[Test]
		public void Validate_EntryNodeIdSetButNodeMissing_ReportsError()
		{
			var entryNodeId = _graph.EntryNodeId;
			_graph.RemoveNode(entryNodeId);

			var issues = GraphValidator.Validate(_graph);

			Assert.IsTrue(issues.Any(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.Message.Contains("no entry node")));
		}

		[Test]
		public void Validate_EntryNodeNoOutgoing_ReportsWarning()
		{
			var issues = GraphValidator.Validate(_graph);

			Assert.IsTrue(issues.Any(i =>
				i.Severity == GraphValidationSeverity.Warning &&
				i.Message.Contains("Entry node has no outgoing transition")));
		}

		[Test]
		public void Validate_EntryNodeWithOutgoing_NoDisconnectedWarning()
		{
			var stateNode = new StateNode("state-1");
			_graph.AddNode(stateNode);

			var connection = new Connection
			{
				Id = "conn-1",
				SourceNodeId = _graph.EntryNodeId,
				TargetNodeId = stateNode.Id,
				ConnectionType = ConnectionType.Transition
			};
			_graph.AddConnection(connection);

			var issues = GraphValidator.Validate(_graph);

			Assert.IsFalse(issues.Any(i =>
				i.Message.Contains("Entry node has no outgoing transition")));
		}



		[Test]
		public void Validate_UnreachableState_ReportsWarning()
		{
			var reachableState = new StateNode("reachable");
			reachableState.DisplayName = "ReachableState";
			_graph.AddNode(reachableState);

			var unreachableState = new StateNode("unreachable");
			unreachableState.DisplayName = "UnreachableState";
			_graph.AddNode(unreachableState);

			var connection = new Connection
			{
				Id = "conn-1",
				SourceNodeId = _graph.EntryNodeId,
				TargetNodeId = reachableState.Id,
				ConnectionType = ConnectionType.Transition
			};
			_graph.AddConnection(connection);

			var issues = GraphValidator.Validate(_graph);

			Assert.IsTrue(issues.Any(i =>
				i.Severity == GraphValidationSeverity.Warning &&
				i.Message.Contains("unreachable") &&
				i.NodeId == unreachableState.Id));
		}

		[Test]
		public void Validate_AllStatesReachable_NoUnreachableWarning()
		{
			var state1 = new StateNode("state-1");
			var state2 = new StateNode("state-2");
			_graph.AddNode(state1);
			_graph.AddNode(state2);

			_graph.AddConnection(new Connection
			{
				Id = "conn-1",
				SourceNodeId = _graph.EntryNodeId,
				TargetNodeId = state1.Id,
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "conn-2",
				SourceNodeId = state1.Id,
				TargetNodeId = state2.Id,
				ConnectionType = ConnectionType.Transition
			});

			var issues = GraphValidator.Validate(_graph);

			Assert.IsFalse(issues.Any(i =>
				i.Message.Contains("unreachable")));
		}

		[Test]
		public void Validate_StateReachableThroughPortal_NotReportedUnreachable()
		{
			var portalOut = new PortalOutNode(1, "Channel 1");
			var portalIn = new PortalInNode(1, "Channel 1");
			var stateAfterPortal = new StateNode("portal-dest");
			stateAfterPortal.DisplayName = "PortalDest";

			_graph.AddNode(portalOut);
			_graph.AddNode(portalIn);
			_graph.AddNode(stateAfterPortal);

			// Entry -> PortalOut (channel 1)
			_graph.AddConnection(new Connection
			{
				Id = "conn-1",
				SourceNodeId = _graph.EntryNodeId,
				TargetNodeId = portalOut.Id,
				ConnectionType = ConnectionType.Transition
			});

			// PortalIn (channel 1) -> State
			_graph.AddConnection(new Connection
			{
				Id = "conn-2",
				SourceNodeId = portalIn.Id,
				TargetNodeId = stateAfterPortal.Id,
				ConnectionType = ConnectionType.Transition
			});

			var issues = GraphValidator.Validate(_graph);

			Assert.IsFalse(issues.Any(i =>
				i.Severity == GraphValidationSeverity.Warning &&
				i.Message.Contains("unreachable") &&
				i.NodeId == stateAfterPortal.Id),
				"State reachable through portal should not be reported as unreachable");
		}

		[Test]
		public void Validate_MultipleUnreachableStates_ReportsAllOfThem()
		{
			var orphan1 = new StateNode("orphan-1");
			orphan1.DisplayName = "Orphan1";
			var orphan2 = new StateNode("orphan-2");
			orphan2.DisplayName = "Orphan2";

			_graph.AddNode(orphan1);
			_graph.AddNode(orphan2);

			var issues = GraphValidator.Validate(_graph);

			var unreachableIssues = issues.Where(i =>
				i.Severity == GraphValidationSeverity.Warning &&
				i.Message.Contains("unreachable")).ToList();

			Assert.AreEqual(2, unreachableIssues.Count);
		}



		[Test]
		public void Validate_OrphanedPortalOut_ReportsWarning()
		{
			var portalOut = new PortalOutNode(5, "Orphaned");
			_graph.AddNode(portalOut);

			var issues = GraphValidator.Validate(_graph);

			Assert.IsTrue(issues.Any(i =>
				i.Severity == GraphValidationSeverity.Warning &&
				i.Message.Contains("PortalOut") &&
				i.Message.Contains("no matching PortalIn") &&
				i.NodeId == portalOut.Id));
		}

		[Test]
		public void Validate_DuplicatePortalInsOnSameChannel_ReportsError()
		{
			var portalIn1 = new PortalInNode(3, "Channel 3");
			var portalIn2 = new PortalInNode(3, "Channel 3");
			_graph.AddNode(portalIn1);
			_graph.AddNode(portalIn2);

			var issues = GraphValidator.Validate(_graph);

			var duplicateIssues = issues.Where(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.Message.Contains("Multiple PortalIn") &&
				i.Message.Contains("channel 3")).ToList();

			Assert.AreEqual(2, duplicateIssues.Count, "Both duplicate PortalIn nodes should be reported");
		}

		[Test]
		public void Validate_OrphanedPortalIn_ReportsInfo()
		{
			var portalIn = new PortalInNode(7, "Orphaned");
			_graph.AddNode(portalIn);

			var issues = GraphValidator.Validate(_graph);

			Assert.IsTrue(issues.Any(i =>
				i.Severity == GraphValidationSeverity.Info &&
				i.Message.Contains("PortalIn") &&
				i.Message.Contains("no PortalOut") &&
				i.NodeId == portalIn.Id));
		}

		[Test]
		public void Validate_PortalInNoOutgoing_ReportsWarning()
		{
			var portalIn = new PortalInNode(2, "Dead End");
			var portalOut = new PortalOutNode(2, "Dead End");
			_graph.AddNode(portalIn);
			_graph.AddNode(portalOut);

			var issues = GraphValidator.Validate(_graph);

			Assert.IsTrue(issues.Any(i =>
				i.Severity == GraphValidationSeverity.Warning &&
				i.Message.Contains("PortalIn") &&
				i.Message.Contains("no outgoing connection") &&
				i.NodeId == portalIn.Id));
		}

		[Test]
		public void Validate_ValidPortalPair_NoOrphanWarnings()
		{
			var portalOut = new PortalOutNode(1, "Channel 1");
			var portalIn = new PortalInNode(1, "Channel 1");
			var stateNode = new StateNode("target");
			_graph.AddNode(portalOut);
			_graph.AddNode(portalIn);
			_graph.AddNode(stateNode);

			// PortalIn has outgoing connection
			_graph.AddConnection(new Connection
			{
				Id = "conn-portal",
				SourceNodeId = portalIn.Id,
				TargetNodeId = stateNode.Id,
				ConnectionType = ConnectionType.Transition
			});

			var issues = GraphValidator.Validate(_graph);

			Assert.IsFalse(issues.Any(i =>
				i.Message.Contains("no matching PortalIn") &&
				i.NodeId == portalOut.Id),
				"Paired PortalOut should not be reported as orphaned");

			Assert.IsFalse(issues.Any(i =>
				i.Message.Contains("no outgoing connection") &&
				i.NodeId == portalIn.Id),
				"PortalIn with outgoing should not be reported");
		}

		[Test]
		public void Validate_PortalOutOnDifferentChannel_ReportsOrphaned()
		{
			var portalOut = new PortalOutNode(1, "Channel 1");
			var portalIn = new PortalInNode(2, "Channel 2");
			_graph.AddNode(portalOut);
			_graph.AddNode(portalIn);

			var issues = GraphValidator.Validate(_graph);

			Assert.IsTrue(issues.Any(i =>
				i.Severity == GraphValidationSeverity.Warning &&
				i.Message.Contains("PortalOut") &&
				i.Message.Contains("channel 1") &&
				i.Message.Contains("no matching PortalIn")));
		}



		[Test]
		public void Validate_SubStateMachineNoSubGraph_ReportsWarning()
		{
			var subNode = new SubStateMachineNode("sub-1");
			subNode.DisplayName = "MySubMachine";
			_graph.AddNode(subNode);

			// Connect entry to sub node to avoid unreachable warnings
			_graph.AddConnection(new Connection
			{
				Id = "conn-1",
				SourceNodeId = _graph.EntryNodeId,
				TargetNodeId = subNode.Id,
				ConnectionType = ConnectionType.Transition
			});

			var issues = GraphValidator.Validate(_graph);

			Assert.IsTrue(issues.Any(i =>
				i.Severity == GraphValidationSeverity.Warning &&
				i.Message.Contains("no sub-graph assigned") &&
				i.NodeId == subNode.Id));
		}

		[Test]
		public void Validate_SubStateMachineSubGraphNoEntryNode_ReportsError()
		{
			var subGraph = ScriptableObject.CreateInstance<StateMachineGraph>();

			try
			{
				// Remove the auto-created entry node
				var entryId = subGraph.EntryNodeId;
				subGraph.RemoveNode(entryId);
				subGraph.EntryNodeId = null;

				var subNode = new SubStateMachineNode("sub-1");
				subNode.SubGraph = subGraph;
				_graph.AddNode(subNode);

				_graph.AddConnection(new Connection
				{
					Id = "conn-1",
					SourceNodeId = _graph.EntryNodeId,
					TargetNodeId = subNode.Id,
					ConnectionType = ConnectionType.Transition
				});

				var issues = GraphValidator.Validate(_graph);

				Assert.IsTrue(issues.Any(i =>
					i.Severity == GraphValidationSeverity.Error &&
					i.Message.Contains("sub-graph has no entry node") &&
					i.NodeId == subNode.Id));
			}
			finally
			{
				Object.DestroyImmediate(subGraph);
			}
		}

		[Test]
		public void Validate_SubStateMachineNoExitConnection_ReportsWarning()
		{
			var subGraph = ScriptableObject.CreateInstance<StateMachineGraph>();

			try
			{
				var subNode = new SubStateMachineNode("sub-1");
				subNode.SubGraph = subGraph;
				_graph.AddNode(subNode);

				_graph.AddConnection(new Connection
				{
					Id = "conn-1",
					SourceNodeId = _graph.EntryNodeId,
					TargetNodeId = subNode.Id,
					ConnectionType = ConnectionType.Transition
				});

				var issues = GraphValidator.Validate(_graph);

				Assert.IsTrue(issues.Any(i =>
					i.Severity == GraphValidationSeverity.Warning &&
					i.Message.Contains("no exit connection") &&
					i.NodeId == subNode.Id));
			}
			finally
			{
				Object.DestroyImmediate(subGraph);
			}
		}

		[Test]
		public void Validate_SubStateMachineWithExitConnection_NoExitWarning()
		{
			var subGraph = ScriptableObject.CreateInstance<StateMachineGraph>();

			try
			{
				var subNode = new SubStateMachineNode("sub-1");
				subNode.SubGraph = subGraph;
				_graph.AddNode(subNode);

				var nextState = new StateNode("next");
				_graph.AddNode(nextState);

				_graph.AddConnection(new Connection
				{
					Id = "conn-entry",
					SourceNodeId = _graph.EntryNodeId,
					TargetNodeId = subNode.Id,
					ConnectionType = ConnectionType.Transition
				});

				_graph.AddConnection(new Connection
				{
					Id = "conn-exit",
					SourceNodeId = subNode.Id,
					TargetNodeId = nextState.Id,
					ConnectionType = ConnectionType.Transition
				});

				var issues = GraphValidator.Validate(_graph);

				Assert.IsFalse(issues.Any(i =>
					i.Message.Contains("no exit connection") &&
					i.NodeId == subNode.Id));
			}
			finally
			{
				Object.DestroyImmediate(subGraph);
			}
		}



		[Test]
		public void HasCircularSubGraphReference_NullGraph_ReturnsFalse()
		{
			var visited = new System.Collections.Generic.HashSet<StateMachineGraph>();
			var result = GraphValidator.HasCircularSubGraphReference(null, visited);

			Assert.IsFalse(result);
		}

		[Test]
		public void HasCircularSubGraphReference_GraphInVisited_ReturnsTrue()
		{
			var visited = new System.Collections.Generic.HashSet<StateMachineGraph> { _graph };
			var result = GraphValidator.HasCircularSubGraphReference(_graph, visited);

			Assert.IsTrue(result);
		}

		[Test]
		public void HasCircularSubGraphReference_NoSubNodes_ReturnsFalse()
		{
			var visited = new System.Collections.Generic.HashSet<StateMachineGraph>();
			var result = GraphValidator.HasCircularSubGraphReference(_graph, visited);

			Assert.IsFalse(result);
		}

		[Test]
		public void HasCircularSubGraphReference_DirectSelfReference_ReturnsTrue()
		{
			// Graph A has a SubStateMachineNode that references Graph A itself
			var subNode = new SubStateMachineNode("self-ref");
			subNode.SubGraph = _graph;
			_graph.AddNode(subNode);

			var visited = new System.Collections.Generic.HashSet<StateMachineGraph>();
			var result = GraphValidator.HasCircularSubGraphReference(_graph, visited);

			Assert.IsTrue(result, "Direct self-reference should be detected as circular");
		}

		[Test]
		public void HasCircularSubGraphReference_IndirectTwoHopCycle_ReturnsTrue()
		{
			// Graph A -> Graph B -> Graph A
			var graphB = ScriptableObject.CreateInstance<StateMachineGraph>();

			try
			{
				var subNodeInA = new SubStateMachineNode("sub-in-a");
				subNodeInA.SubGraph = graphB;
				_graph.AddNode(subNodeInA);

				var subNodeInB = new SubStateMachineNode("sub-in-b");
				subNodeInB.SubGraph = _graph;
				graphB.AddNode(subNodeInB);

				var visited = new System.Collections.Generic.HashSet<StateMachineGraph>();
				var result = GraphValidator.HasCircularSubGraphReference(_graph, visited);

				Assert.IsTrue(result, "Two-hop cycle (A -> B -> A) should be detected");
			}
			finally
			{
				Object.DestroyImmediate(graphB);
			}
		}

		[Test]
		public void HasCircularSubGraphReference_DeepThreeHopCycle_ReturnsTrue()
		{
			// Graph A -> Graph B -> Graph C -> Graph A
			var graphB = ScriptableObject.CreateInstance<StateMachineGraph>();
			var graphC = ScriptableObject.CreateInstance<StateMachineGraph>();

			try
			{
				var subNodeInA = new SubStateMachineNode("sub-in-a");
				subNodeInA.SubGraph = graphB;
				_graph.AddNode(subNodeInA);

				var subNodeInB = new SubStateMachineNode("sub-in-b");
				subNodeInB.SubGraph = graphC;
				graphB.AddNode(subNodeInB);

				var subNodeInC = new SubStateMachineNode("sub-in-c");
				subNodeInC.SubGraph = _graph;
				graphC.AddNode(subNodeInC);

				var visited = new System.Collections.Generic.HashSet<StateMachineGraph>();
				var result = GraphValidator.HasCircularSubGraphReference(_graph, visited);

				Assert.IsTrue(result, "Three-hop cycle (A -> B -> C -> A) should be detected");
			}
			finally
			{
				Object.DestroyImmediate(graphB);
				Object.DestroyImmediate(graphC);
			}
		}

		[Test]
		public void HasCircularSubGraphReference_NonCircularDeepNesting_ReturnsFalse()
		{
			// Graph A -> Graph B -> Graph C (no cycle)
			var graphB = ScriptableObject.CreateInstance<StateMachineGraph>();
			var graphC = ScriptableObject.CreateInstance<StateMachineGraph>();

			try
			{
				var subNodeInA = new SubStateMachineNode("sub-in-a");
				subNodeInA.SubGraph = graphB;
				_graph.AddNode(subNodeInA);

				var subNodeInB = new SubStateMachineNode("sub-in-b");
				subNodeInB.SubGraph = graphC;
				graphB.AddNode(subNodeInB);

				var visited = new System.Collections.Generic.HashSet<StateMachineGraph>();
				var result = GraphValidator.HasCircularSubGraphReference(_graph, visited);

				Assert.IsFalse(result, "Linear chain without cycle should not be detected as circular");
			}
			finally
			{
				Object.DestroyImmediate(graphB);
				Object.DestroyImmediate(graphC);
			}
		}

		[Test]
		public void HasCircularSubGraphReference_MultipleSubNodesOnlyCycleDetected_ReturnsTrue()
		{
			// Graph A has two sub-nodes: one points to Graph B (safe), one points to Graph C -> Graph A (cycle)
			var graphB = ScriptableObject.CreateInstance<StateMachineGraph>();
			var graphC = ScriptableObject.CreateInstance<StateMachineGraph>();

			try
			{
				var safeSubNode = new SubStateMachineNode("safe-sub");
				safeSubNode.SubGraph = graphB;
				_graph.AddNode(safeSubNode);

				var cyclicSubNode = new SubStateMachineNode("cyclic-sub");
				cyclicSubNode.SubGraph = graphC;
				_graph.AddNode(cyclicSubNode);

				var subNodeInC = new SubStateMachineNode("sub-in-c");
				subNodeInC.SubGraph = _graph;
				graphC.AddNode(subNodeInC);

				var visited = new System.Collections.Generic.HashSet<StateMachineGraph>();
				var result = GraphValidator.HasCircularSubGraphReference(_graph, visited);

				Assert.IsTrue(result, "Cycle through one of multiple sub-nodes should be detected");
			}
			finally
			{
				Object.DestroyImmediate(graphB);
				Object.DestroyImmediate(graphC);
			}
		}

		[Test]
		public void HasCircularSubGraphReference_SubNodeWithNullSubGraph_ReturnsFalse()
		{
			var subNode = new SubStateMachineNode("null-sub");
			subNode.SubGraph = null;
			_graph.AddNode(subNode);

			var visited = new System.Collections.Generic.HashSet<StateMachineGraph>();
			var result = GraphValidator.HasCircularSubGraphReference(_graph, visited);

			Assert.IsFalse(result, "Sub-node with null SubGraph should not cause circular detection");
		}

		[Test]
		public void Validate_DirectCircularReference_ReportsError()
		{
			var subNode = new SubStateMachineNode("self-ref");
			subNode.DisplayName = "SelfRefSub";
			subNode.SubGraph = _graph;
			_graph.AddNode(subNode);

			_graph.AddConnection(new Connection
			{
				Id = "conn-1",
				SourceNodeId = _graph.EntryNodeId,
				TargetNodeId = subNode.Id,
				ConnectionType = ConnectionType.Transition
			});

			var issues = GraphValidator.Validate(_graph);

			Assert.IsTrue(issues.Any(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.Message.Contains("circular reference") &&
				i.NodeId == subNode.Id),
				"Direct self-reference should produce a circular reference error");
		}

		[Test]
		public void Validate_IndirectCircularReference_ReportsError()
		{
			var graphB = ScriptableObject.CreateInstance<StateMachineGraph>();

			try
			{
				// Graph A contains SubNode -> Graph B, Graph B contains SubNode -> Graph A
				var subNodeInA = new SubStateMachineNode("sub-in-a");
				subNodeInA.DisplayName = "SubToB";
				subNodeInA.SubGraph = graphB;
				_graph.AddNode(subNodeInA);

				var subNodeInB = new SubStateMachineNode("sub-in-b");
				subNodeInB.SubGraph = _graph;
				graphB.AddNode(subNodeInB);

				_graph.AddConnection(new Connection
				{
					Id = "conn-1",
					SourceNodeId = _graph.EntryNodeId,
					TargetNodeId = subNodeInA.Id,
					ConnectionType = ConnectionType.Transition
				});

				var issues = GraphValidator.Validate(_graph);

				Assert.IsTrue(issues.Any(i =>
					i.Severity == GraphValidationSeverity.Error &&
					i.Message.Contains("circular reference") &&
					i.NodeId == subNodeInA.Id),
					"Indirect circular reference (A -> B -> A) should produce a circular reference error");
			}
			finally
			{
				Object.DestroyImmediate(graphB);
			}
		}

		[Test]
		public void Validate_NonCircularSubGraph_NoCircularError()
		{
			var graphB = ScriptableObject.CreateInstance<StateMachineGraph>();

			try
			{
				var subNode = new SubStateMachineNode("sub-1");
				subNode.DisplayName = "SafeSub";
				subNode.SubGraph = graphB;
				_graph.AddNode(subNode);

				_graph.AddConnection(new Connection
				{
					Id = "conn-1",
					SourceNodeId = _graph.EntryNodeId,
					TargetNodeId = subNode.Id,
					ConnectionType = ConnectionType.Transition
				});

				var issues = GraphValidator.Validate(_graph);

				Assert.IsFalse(issues.Any(i =>
					i.Message.Contains("circular reference")),
					"Non-circular sub-graph should not produce circular reference error");
			}
			finally
			{
				Object.DestroyImmediate(graphB);
			}
		}



		[Test]
		public void Validate_StateNodeWithMissingType_ReportsError()
		{
			var stateNode = new StateNode("broken-state");
			stateNode.DisplayName = "BrokenState";
			_graph.AddNode(stateNode);

			_graph.AddConnection(new Connection
			{
				Id = "conn-1",
				SourceNodeId = _graph.EntryNodeId,
				TargetNodeId = stateNode.Id,
				ConnectionType = ConnectionType.Transition
			});

			var issues = GraphValidator.Validate(_graph);

			Assert.IsTrue(issues.Any(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.Message.Contains("missing type") &&
				i.NodeId == stateNode.Id));
		}



		[Test]
		public void Validate_EmptyGraphAfterRemovingEntry_ReportsMissingEntry()
		{
			var entryId = _graph.EntryNodeId;
			_graph.RemoveNode(entryId);
			_graph.EntryNodeId = null;

			var issues = GraphValidator.Validate(_graph);

			Assert.IsTrue(issues.Any(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.Message.Contains("no entry node")));
		}

		[Test]
		public void Validate_GraphWithMultipleIssueTypes_ReportsAll()
		{
			// Add an orphaned portal out (warning)
			var portalOut = new PortalOutNode(99, "Orphaned");
			_graph.AddNode(portalOut);

			// Add an unreachable state (warning)
			var orphanState = new StateNode("orphan");
			orphanState.DisplayName = "OrphanState";
			_graph.AddNode(orphanState);

			// Entry has no outgoing (warning)
			var issues = GraphValidator.Validate(_graph);

			// Should have at least: disconnected entry, unreachable state, orphaned portal
			Assert.IsTrue(issues.Count >= 3,
				$"Expected at least 3 issues but got {issues.Count}: " +
				string.Join(", ", issues.Select(i => i.Message)));
		}

		[Test]
		public void Validate_IssuesContainCorrectNodeIds()
		{
			var orphanState = new StateNode("specific-id-123");
			orphanState.DisplayName = "OrphanNode";
			_graph.AddNode(orphanState);

			var issues = GraphValidator.Validate(_graph);

			var unreachableIssue = issues.FirstOrDefault(i =>
				i.Message.Contains("unreachable") &&
				i.NodeId == "specific-id-123");

			Assert.IsNotNull(unreachableIssue, "Unreachable issue should reference the correct node ID");
		}

		[Test]
		public void Validate_EntryNodeDisconnectedWarning_ContainsEntryNodeId()
		{
			var issues = GraphValidator.Validate(_graph);

			var entryWarning = issues.FirstOrDefault(i =>
				i.Message.Contains("Entry node has no outgoing transition"));

			Assert.IsNotNull(entryWarning);
			Assert.AreEqual(_graph.EntryNodeId, entryWarning.NodeId);
		}

	}
}
