using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class GraphDataModelTests
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
		public void AddNode_StateNode_AddsToGraph()
		{
			// Note: Graph auto-creates an entry node in Reset(), so we account for that
			var initialCount = _graph.GetAllNodes().Count();
			var node = new StateNode
			{
				Id = Guid.NewGuid().ToString(),
				DisplayName = "TestState"
			};

			_graph.AddNode(node);

			Assert.AreEqual(initialCount + 1, _graph.GetAllNodes().Count());
			Assert.AreEqual(node, _graph.GetNode(node.Id));
		}

		[Test]
		public void AddNode_EntryNode_AddsToGraph()
		{
			// Note: Graph auto-creates an entry node in Reset(), so we account for that
			var initialCount = _graph.GetAllNodes().Count();
			var node = new EntryNode
			{
				Id = Guid.NewGuid().ToString()
			};

			_graph.AddNode(node);

			Assert.AreEqual(initialCount + 1, _graph.GetAllNodes().Count());
		}

		[Test]
		public void AddNode_ExitNode_AddsToGraph()
		{
			// Note: Graph auto-creates an entry node in Reset(), so we account for that
			var initialCount = _graph.GetAllNodes().Count();
			var node = new ExitNode
			{
				Id = Guid.NewGuid().ToString(),
				ExitName = "Success",
				ExitCode = 0
			};

			_graph.AddNode(node);

			Assert.AreEqual(initialCount + 1, _graph.GetAllNodes().Count());
		}

		[Test]
		public void RemoveNode_RemovesFromGraph()
		{
			var initialCount = _graph.GetAllNodes().Count();
			var node = new StateNode { Id = Guid.NewGuid().ToString() };
			_graph.AddNode(node);

			_graph.RemoveNode(node.Id);

			Assert.AreEqual(initialCount, _graph.GetAllNodes().Count());
			Assert.IsNull(_graph.GetNode(node.Id));
		}

		[Test]
		public void GetNode_ReturnsCorrectNode()
		{
			var node1 = new StateNode { Id = "node1", DisplayName = "State1" };
			var node2 = new StateNode { Id = "node2", DisplayName = "State2" };
			_graph.AddNode(node1);
			_graph.AddNode(node2);

			var result = _graph.GetNode("node2");

			Assert.AreEqual(node2, result);
			Assert.AreEqual("State2", result.DisplayName);
		}

		[Test]
		public void GetNode_NonExistent_ReturnsNull()
		{
			var result = _graph.GetNode("nonexistent");

			Assert.IsNull(result);
		}



		[Test]
		public void AddConnection_AddsToGraph()
		{
			var connection = new Connection
			{
				Id = Guid.NewGuid().ToString(),
				SourceNodeId = "node1",
				SourcePortName = "output",
				TargetNodeId = "node2",
				TargetPortName = "input",
				ConnectionType = ConnectionType.Transition
			};

			_graph.AddConnection(connection);

			Assert.AreEqual(1, _graph.Connections.Count);
		}

		[Test]
		public void RemoveConnection_RemovesFromGraph()
		{
			var connection = new Connection
			{
				Id = "conn1",
				SourceNodeId = "node1",
				TargetNodeId = "node2"
			};
			_graph.AddConnection(connection);

			_graph.RemoveConnection(connection.Id);

			Assert.AreEqual(0, _graph.Connections.Count);
		}

		[Test]
		public void GetConnectionsFromNode_ReturnsCorrectConnections()
		{
			var conn1 = new Connection { Id = "c1", SourceNodeId = "node1", TargetNodeId = "node2" };
			var conn2 = new Connection { Id = "c2", SourceNodeId = "node1", TargetNodeId = "node3" };
			var conn3 = new Connection { Id = "c3", SourceNodeId = "node2", TargetNodeId = "node3" };

			_graph.AddConnection(conn1);
			_graph.AddConnection(conn2);
			_graph.AddConnection(conn3);

			var connections = _graph.GetConnectionsFromNode("node1");

			Assert.AreEqual(2, connections.Count());
		}

		[Test]
		public void GetFirstConnectionFromNode_ReturnsFirstConnection()
		{
			var conn1 = new Connection { Id = "c1", SourceNodeId = "node1", TargetNodeId = "node2" };
			var conn2 = new Connection { Id = "c2", SourceNodeId = "node1", TargetNodeId = "node3" };

			_graph.AddConnection(conn1);
			_graph.AddConnection(conn2);

			var first = _graph.GetFirstConnectionFromNode("node1");

			Assert.IsNotNull(first);
			Assert.AreEqual("c1", first.Id);
		}

		[Test]
		public void GetFirstConnectionFromNode_NoConnections_ReturnsNull()
		{
			var result = _graph.GetFirstConnectionFromNode("nonexistent");

			Assert.IsNull(result);
		}

		[Test]
		public void GetFirstConnectionFromNode_SingleConnection_ReturnsThatConnection()
		{
			var conn = new Connection { Id = "c1", SourceNodeId = "node1", TargetNodeId = "node2" };
			_graph.AddConnection(conn);

			var first = _graph.GetFirstConnectionFromNode("node1");

			Assert.IsNotNull(first);
			Assert.AreEqual("c1", first.Id);
			Assert.AreEqual("node2", first.TargetNodeId);
		}

		[Test]
		public void GetConnectionsToNode_ReturnsCorrectConnections()
		{
			var conn1 = new Connection { Id = "c1", SourceNodeId = "node1", TargetNodeId = "node3" };
			var conn2 = new Connection { Id = "c2", SourceNodeId = "node2", TargetNodeId = "node3" };
			var conn3 = new Connection { Id = "c3", SourceNodeId = "node1", TargetNodeId = "node2" };

			_graph.AddConnection(conn1);
			_graph.AddConnection(conn2);
			_graph.AddConnection(conn3);

			var connections = _graph.GetConnectionsToNode("node3");

			Assert.AreEqual(2, connections.Count());
		}



		[Test]
		public void AddBlackboardVariable_AddsToGraph()
		{
			var definition = new Blackboard.BlackboardVariableDefinition
			{
				Key = "health",
				ValueType = typeof(int),
				IsExposed = true
			};

			_graph.AddBlackboardVariable(definition);

			Assert.IsNotNull(_graph.GetBlackboardVariable("health"));
		}

		[Test]
		public void RemoveBlackboardVariable_RemovesFromGraph()
		{
			var definition = new Blackboard.BlackboardVariableDefinition
			{
				Key = "health",
				ValueType = typeof(int)
			};
			_graph.AddBlackboardVariable(definition);

			_graph.RemoveBlackboardVariable("health");

			Assert.IsNull(_graph.GetBlackboardVariable("health"));
		}

		[Test]
		public void GetBlackboardVariable_ReturnsCorrectDefinition()
		{
			var definition = new Blackboard.BlackboardVariableDefinition
			{
				Key = "speed",
				ValueType = typeof(float),
				Category = "Movement"
			};
			_graph.AddBlackboardVariable(definition);

			var result = _graph.GetBlackboardVariable("speed");

			Assert.AreEqual(typeof(float), result.ValueType);
			Assert.AreEqual("Movement", result.Category);
		}



		[Test]
		public void Validate_GraphWithAutoCreatedEntry_ReturnsNoOutgoingTransitionWarning()
		{
			// Graph auto-creates an entry node in Reset(), so we validate
			// that the entry node has no outgoing transitions (expected warning)
			var issues = _graph.Validate();

			// Should have warning about entry node having no outgoing transition
			Assert.IsTrue(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Warning &&
				i.Message.Contains("outgoing transition")));
		}

		[Test]
		public void Validate_GraphWithNoEntryNode_ReturnsError()
		{
			// Remove the auto-created entry node to test validation
			var entryNode = _graph.GetEntryNode();
			if (entryNode != null)
			{
				_graph.RemoveNode(entryNode.Id);
			}
			_graph.EntryNodeId = null;

			var issues = _graph.Validate();

			Assert.IsTrue(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.Message.Contains("entry")));
		}

		[Test]
		public void Validate_GraphWithEntry_NoEntryWarning()
		{
			var entry = new EntryNode { Id = "entry" };
			_graph.AddNode(entry);
			_graph.EntryNodeId = "entry";

			var issues = _graph.Validate();

			Assert.IsFalse(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.Message.Contains("entry")));
		}

		[Test]
		public void Validate_DisconnectedState_ReturnsWarning()
		{
			var entry = new EntryNode { Id = "entry" };
			var state = new StateNode { Id = "state1", DisplayName = "Disconnected" };

			_graph.AddNode(entry);
			_graph.AddNode(state);
			_graph.EntryNodeId = "entry";

			var issues = _graph.Validate();

			Assert.IsTrue(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Warning &&
				i.NodeId == "state1"));
		}



		[Test]
		public void StateNode_HasCorrectNodeType()
		{
			var node = new StateNode();

			Assert.AreEqual(GraphNodeType.State, node.NodeType);
		}

		[Test]
		public void EntryNode_HasCorrectNodeType()
		{
			var node = new EntryNode();

			Assert.AreEqual(GraphNodeType.Entry, node.NodeType);
		}

		[Test]
		public void ExitNode_HasCorrectNodeType()
		{
			var node = new ExitNode();

			Assert.AreEqual(GraphNodeType.Exit, node.NodeType);
		}

		[Test]
		public void BlackboardVariableNode_HasCorrectNodeType()
		{
			var node = new BlackboardVariableNode();

			Assert.AreEqual(GraphNodeType.BlackboardVariable, node.NodeType);
		}

		[Test]
		public void SubStateMachineNode_HasCorrectNodeType()
		{
			var node = new SubStateMachineNode();

			Assert.AreEqual(GraphNodeType.SubStateMachine, node.NodeType);
		}



		[Test]
		public void Connection_TransitionType_IsSet()
		{
			var connection = new Connection
			{
				ConnectionType = ConnectionType.Transition
			};

			Assert.AreEqual(ConnectionType.Transition, connection.ConnectionType);
		}

		[Test]
		public void Connection_DataType_IsSet()
		{
			var connection = new Connection
			{
				ConnectionType = ConnectionType.Data
			};

			Assert.AreEqual(ConnectionType.Data, connection.ConnectionType);
		}

		[Test]
		public void Connection_TransitionTime_CanBeSet()
		{
			var connection = new Connection
			{
				TransitionTime = 0.5f
			};

			Assert.AreEqual(0.5f, connection.TransitionTime);
		}




		[Test]
		public void Validate_DirectCircularReference_ReturnsError()
		{
			// Create parent graph
			var parentGraph = ScriptableObject.CreateInstance<StateMachineGraph>();

			// Create sub-graph that references parent (circular)
			var subGraph = ScriptableObject.CreateInstance<StateMachineGraph>();

			// Add SubStateMachineNode to parent that points to subGraph
			var subNode = new SubStateMachineNode
			{
				Id = "subNode1",
				DisplayName = "Sub1",
				SubGraph = subGraph
			};
			parentGraph.AddNode(subNode);

			// Add SubStateMachineNode to subGraph that points back to parent (circular!)
			var circularNode = new SubStateMachineNode
			{
				Id = "circularNode",
				DisplayName = "CircularBack",
				SubGraph = parentGraph
			};
			subGraph.AddNode(circularNode);

			// Validate should detect circular reference
			var issues = parentGraph.Validate();

			Assert.IsTrue(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.Message.Contains("circular")),
				"Should detect circular sub-graph reference");

			UnityEngine.Object.DestroyImmediate(parentGraph);
			UnityEngine.Object.DestroyImmediate(subGraph);
		}

		[Test]
		public void Validate_IndirectCircularReference_ReturnsError()
		{
			// Create chain: A -> B -> C -> A (circular)
			var graphA = ScriptableObject.CreateInstance<StateMachineGraph>();
			var graphB = ScriptableObject.CreateInstance<StateMachineGraph>();
			var graphC = ScriptableObject.CreateInstance<StateMachineGraph>();

			// A contains SubStateMachine pointing to B
			var subNodeAtoB = new SubStateMachineNode
			{
				Id = "subAtoB",
				DisplayName = "AtoB",
				SubGraph = graphB
			};
			graphA.AddNode(subNodeAtoB);

			// B contains SubStateMachine pointing to C
			var subNodeBtoC = new SubStateMachineNode
			{
				Id = "subBtoC",
				DisplayName = "BtoC",
				SubGraph = graphC
			};
			graphB.AddNode(subNodeBtoC);

			// C contains SubStateMachine pointing back to A (circular!)
			var subNodeCtoA = new SubStateMachineNode
			{
				Id = "subCtoA",
				DisplayName = "CtoA",
				SubGraph = graphA
			};
			graphC.AddNode(subNodeCtoA);

			// Validate A should detect the circular reference through the chain
			var issues = graphA.Validate();

			Assert.IsTrue(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.Message.Contains("circular")),
				"Should detect indirect circular sub-graph reference through chain");

			UnityEngine.Object.DestroyImmediate(graphA);
			UnityEngine.Object.DestroyImmediate(graphB);
			UnityEngine.Object.DestroyImmediate(graphC);
		}

		[Test]
		public void Validate_SelfReference_ReturnsError()
		{
			// Graph references itself
			var selfRefGraph = ScriptableObject.CreateInstance<StateMachineGraph>();

			var selfNode = new SubStateMachineNode
			{
				Id = "selfRef",
				DisplayName = "SelfReference",
				SubGraph = selfRefGraph // Points to itself!
			};
			selfRefGraph.AddNode(selfNode);

			var issues = selfRefGraph.Validate();

			Assert.IsTrue(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.Message.Contains("circular")),
				"Should detect self-referencing sub-graph");

			UnityEngine.Object.DestroyImmediate(selfRefGraph);
		}

		[Test]
		public void Validate_ValidSubGraphChain_NoCircularError()
		{
			// Valid chain: A -> B -> C (no circular reference)
			var graphA = ScriptableObject.CreateInstance<StateMachineGraph>();
			var graphB = ScriptableObject.CreateInstance<StateMachineGraph>();
			var graphC = ScriptableObject.CreateInstance<StateMachineGraph>();

			var subNodeAtoB = new SubStateMachineNode
			{
				Id = "subAtoB",
				SubGraph = graphB
			};
			graphA.AddNode(subNodeAtoB);

			var subNodeBtoC = new SubStateMachineNode
			{
				Id = "subBtoC",
				SubGraph = graphC
			};
			graphB.AddNode(subNodeBtoC);

			var issues = graphA.Validate();

			Assert.IsFalse(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.Message.Contains("circular")),
				"Valid sub-graph chain should not trigger circular reference error");

			UnityEngine.Object.DestroyImmediate(graphA);
			UnityEngine.Object.DestroyImmediate(graphB);
			UnityEngine.Object.DestroyImmediate(graphC);
		}



		[Test]
		public void SerializedNode_NullJsonData_ToNode_ReturnsNull()
		{
			var serialized = new SerializedNode();
			// _jsonData is null by default

			var node = serialized.ToNode();

			Assert.IsNull(node);
		}

		[Test]
		public void SerializedNode_EmptyJsonData_ToNode_ReturnsNull()
		{
			// Create a SerializedNode with empty JSON via reflection or by serializing null
			var emptyNode = SerializedNode.FromNode(null);

			Assert.IsNull(emptyNode, "FromNode(null) should return null");
		}

		[Test]
		public void SerializedNode_ValidStateNode_RoundTrip_PreservesData()
		{
			var original = new StateNode
			{
				Id = "test-state-123",
				DisplayName = "TestState",
				Position = new Vector2(100, 200)
			};

			var serialized = SerializedNode.FromNode(original);
			var restored = serialized.ToNode() as StateNode;

			Assert.IsNotNull(restored);
			Assert.AreEqual(original.Id, restored.Id);
			Assert.AreEqual(original.DisplayName, restored.DisplayName);
			Assert.AreEqual(original.Position, restored.Position);
		}

		[Test]
		public void SerializedNode_NodeId_AvailableWithoutDeserialization()
		{
			// The NodeId property allows ID comparison without expensive JSON deserialization
			var stateNode = new StateNode
			{
				Id = "state-test-id",
				DisplayName = "TestState"
			};

			var serialized = SerializedNode.FromNode(stateNode);

			// NodeId should be accessible directly without calling ToNode()
			Assert.AreEqual("state-test-id", serialized.NodeId);
		}

		[Test]
		public void SerializedNode_NodeId_AllNodeTypes_StoredCorrectly()
		{
			// Verify NodeId is stored for all node types
			var entryNode = new EntryNode { Id = "entry-id" };
			var exitNode = new ExitNode { Id = "exit-id" };
			var stateNode = new StateNode { Id = "state-id" };
			var portalOut = new PortalOutNode { Id = "portal-out-id" };
			var portalIn = new PortalInNode { Id = "portal-in-id" };
			var subNode = new SubStateMachineNode { Id = "sub-id" };

			Assert.AreEqual("entry-id", SerializedNode.FromNode(entryNode).NodeId);
			Assert.AreEqual("exit-id", SerializedNode.FromNode(exitNode).NodeId);
			Assert.AreEqual("state-id", SerializedNode.FromNode(stateNode).NodeId);
			Assert.AreEqual("portal-out-id", SerializedNode.FromNode(portalOut).NodeId);
			Assert.AreEqual("portal-in-id", SerializedNode.FromNode(portalIn).NodeId);
			Assert.AreEqual("sub-id", SerializedNode.FromNode(subNode).NodeId);
		}

		[Test]
		public void SerializedNode_AllNodeTypes_RoundTrip_Works()
		{
			// Test Entry Node
			var entryNode = new EntryNode { Id = "entry-1" };
			var entryRestored = SerializedNode.FromNode(entryNode).ToNode();
			Assert.IsInstanceOf<EntryNode>(entryRestored);
			Assert.AreEqual("entry-1", entryRestored.Id);

			// Test Exit Node
			var exitNode = new ExitNode { Id = "exit-1", ExitName = "Success", ExitCode = 0 };
			var exitRestored = SerializedNode.FromNode(exitNode).ToNode() as ExitNode;
			Assert.IsInstanceOf<ExitNode>(exitRestored);
			Assert.AreEqual("Success", exitRestored.ExitName);

			// Test SubStateMachine Node
			var subNode = new SubStateMachineNode { Id = "sub-1", DisplayName = "SubMachine" };
			var subRestored = SerializedNode.FromNode(subNode).ToNode() as SubStateMachineNode;
			Assert.IsInstanceOf<SubStateMachineNode>(subRestored);
			Assert.AreEqual("SubMachine", subRestored.DisplayName);

			// Test PortalOut Node
			var portalOut = new PortalOutNode { Id = "portal-out-1", ChannelId = 5 };
			var portalOutRestored = SerializedNode.FromNode(portalOut).ToNode() as PortalOutNode;
			Assert.IsInstanceOf<PortalOutNode>(portalOutRestored);
			Assert.AreEqual(5, portalOutRestored.ChannelId);

			// Test PortalIn Node
			var portalIn = new PortalInNode { Id = "portal-in-1", ChannelId = 5 };
			var portalInRestored = SerializedNode.FromNode(portalIn).ToNode() as PortalInNode;
			Assert.IsInstanceOf<PortalInNode>(portalInRestored);
			Assert.AreEqual(5, portalInRestored.ChannelId);
		}



		[Test]
		public void ConnectionIndices_AfterAddingConnections_CorrectlyIndexed()
		{
			var node1 = new StateNode { Id = "node1" };
			var node2 = new StateNode { Id = "node2" };
			var node3 = new StateNode { Id = "node3" };

			_graph.AddNode(node1);
			_graph.AddNode(node2);
			_graph.AddNode(node3);

			// Add connections
			var conn1 = new Connection
			{
				Id = "c1",
				SourceNodeId = "node1",
				SourcePortName = "output",
				TargetNodeId = "node2",
				TargetPortName = "input"
			};
			var conn2 = new Connection
			{
				Id = "c2",
				SourceNodeId = "node1",
				SourcePortName = "output2",
				TargetNodeId = "node3",
				TargetPortName = "input"
			};

			_graph.AddConnection(conn1);
			_graph.AddConnection(conn2);

			// Verify indices
			var fromNode1 = _graph.GetConnectionsFromNode("node1").ToList();
			Assert.AreEqual(2, fromNode1.Count);

			var toNode2 = _graph.GetConnectionsToNode("node2").ToList();
			Assert.AreEqual(1, toNode2.Count);

			var toNode3 = _graph.GetConnectionsToNode("node3").ToList();
			Assert.AreEqual(1, toNode3.Count);
		}

		[Test]
		public void ConnectionIndices_AfterRemovingNode_ConnectionsRemoved()
		{
			var node1 = new StateNode { Id = "node1" };
			var node2 = new StateNode { Id = "node2" };

			_graph.AddNode(node1);
			_graph.AddNode(node2);

			var conn = new Connection
			{
				Id = "c1",
				SourceNodeId = "node1",
				TargetNodeId = "node2"
			};
			_graph.AddConnection(conn);

			// Remove source node - connection should be removed
			_graph.RemoveNode("node1");

			Assert.AreEqual(0, _graph.Connections.Count);
			Assert.AreEqual(0, _graph.GetConnectionsFromNode("node1").Count());
			Assert.AreEqual(0, _graph.GetConnectionsToNode("node2").Count());
		}

		[Test]
		public void ConnectionIndices_AfterRemovingConnection_IndicesUpdated()
		{
			var node1 = new StateNode { Id = "node1" };
			var node2 = new StateNode { Id = "node2" };

			_graph.AddNode(node1);
			_graph.AddNode(node2);

			var conn1 = new Connection { Id = "c1", SourceNodeId = "node1", TargetNodeId = "node2" };
			var conn2 = new Connection { Id = "c2", SourceNodeId = "node1", TargetNodeId = "node2" };

			_graph.AddConnection(conn1);
			_graph.AddConnection(conn2);

			Assert.AreEqual(2, _graph.GetConnectionsFromNode("node1").Count());

			_graph.RemoveConnection("c1");

			Assert.AreEqual(1, _graph.GetConnectionsFromNode("node1").Count());
			Assert.AreEqual("c2", _graph.GetConnectionsFromNode("node1").First().Id);
		}

		[Test]
		public void GetConnectionsFromPort_ReturnsOnlyMatchingPort()
		{
			var node1 = new StateNode { Id = "node1" };
			var node2 = new StateNode { Id = "node2" };
			var node3 = new StateNode { Id = "node3" };

			_graph.AddNode(node1);
			_graph.AddNode(node2);
			_graph.AddNode(node3);

			var conn1 = new Connection
			{
				Id = "c1",
				SourceNodeId = "node1",
				SourcePortName = "outputA",
				TargetNodeId = "node2"
			};
			var conn2 = new Connection
			{
				Id = "c2",
				SourceNodeId = "node1",
				SourcePortName = "outputB",
				TargetNodeId = "node3"
			};

			_graph.AddConnection(conn1);
			_graph.AddConnection(conn2);

			var fromPortA = _graph.GetConnectionsFromPort("node1", "outputA").ToList();
			var fromPortB = _graph.GetConnectionsFromPort("node1", "outputB").ToList();

			Assert.AreEqual(1, fromPortA.Count);
			Assert.AreEqual("c1", fromPortA[0].Id);

			Assert.AreEqual(1, fromPortB.Count);
			Assert.AreEqual("c2", fromPortB[0].Id);
		}

		[Test]
		public void GetConnectionToPort_ReturnsMatchingConnection()
		{
			var node1 = new StateNode { Id = "node1" };
			var node2 = new StateNode { Id = "node2" };

			_graph.AddNode(node1);
			_graph.AddNode(node2);

			var conn = new Connection
			{
				Id = "c1",
				SourceNodeId = "node1",
				TargetNodeId = "node2",
				TargetPortName = "inputPort"
			};
			_graph.AddConnection(conn);

			var result = _graph.GetConnectionToPort("node2", "inputPort");

			Assert.IsNotNull(result);
			Assert.AreEqual("c1", result.Id);
		}

		[Test]
		public void GetConnectionToPort_NonExistentPort_ReturnsNull()
		{
			var node1 = new StateNode { Id = "node1" };
			_graph.AddNode(node1);

			var result = _graph.GetConnectionToPort("node1", "nonexistent");

			Assert.IsNull(result);
		}

		[Test]
		public void IsPortConnected_InputPort_ReturnsCorrectly()
		{
			var node1 = new StateNode { Id = "node1" };
			var node2 = new StateNode { Id = "node2" };

			_graph.AddNode(node1);
			_graph.AddNode(node2);

			var conn = new Connection
			{
				Id = "c1",
				SourceNodeId = "node1",
				TargetNodeId = "node2",
				TargetPortName = "input"
			};
			_graph.AddConnection(conn);

			Assert.IsTrue(_graph.IsPortConnected("node2", "input", isInput: true));
			Assert.IsFalse(_graph.IsPortConnected("node2", "other", isInput: true));
		}

		[Test]
		public void IsPortConnected_OutputPort_ReturnsCorrectly()
		{
			var node1 = new StateNode { Id = "node1" };
			var node2 = new StateNode { Id = "node2" };

			_graph.AddNode(node1);
			_graph.AddNode(node2);

			var conn = new Connection
			{
				Id = "c1",
				SourceNodeId = "node1",
				SourcePortName = "output",
				TargetNodeId = "node2"
			};
			_graph.AddConnection(conn);

			Assert.IsTrue(_graph.IsPortConnected("node1", "output", isInput: false));
			Assert.IsFalse(_graph.IsPortConnected("node1", "other", isInput: false));
		}



		[Test]
		public void AddNode_NullId_DoesNotAdd()
		{
			var initialCount = _graph.GetAllNodes().Count();
			var node = new StateNode { Id = null };

			_graph.AddNode(node);

			Assert.AreEqual(initialCount, _graph.GetAllNodes().Count());
		}

		[Test]
		public void AddNode_EmptyId_DoesNotAdd()
		{
			var initialCount = _graph.GetAllNodes().Count();
			var node = new StateNode { Id = "" };

			_graph.AddNode(node);

			Assert.AreEqual(initialCount, _graph.GetAllNodes().Count());
		}

		[Test]
		public void AddNode_NullNode_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => _graph.AddNode(null));
		}

		[Test]
		public void AddConnection_NullConnection_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => _graph.AddConnection(null));
			Assert.AreEqual(0, _graph.Connections.Count);
		}



		[Test]
		public void StateNode_RestoreType_WithInvalidTypeName_StateTypeIsNull()
		{
			var stateNode = new StateNode
			{
				Id = "test-node",
				DisplayName = "OldState"
			};

			// Simulate a type name from an older version that no longer exists
			stateNode.StateTypeAssemblyQualifiedName = "OldNamespace.OldStateType, OldAssembly, Version=1.0.0.0";

			stateNode.RestoreType();

			Assert.IsNull(stateNode.StateType, "Type should be null when assembly qualified name cannot be resolved");
		}

		[Test]
		public void StateNode_RestoreType_WithRenamedAssembly_StateTypeIsNull()
		{
			var stateNode = new StateNode
			{
				Id = "test-node",
				DisplayName = "TestState"
			};

			// A valid type name but with wrong assembly (simulating assembly rename)
			stateNode.StateTypeAssemblyQualifiedName = "Nonatomic.VSM.Core.State, RenamedAssembly, Version=1.0.0.0";

			stateNode.RestoreType();

			Assert.IsNull(stateNode.StateType, "Type should be null when assembly has been renamed");
		}



		[Test]
		public void Validate_StateReachableThroughPortal_NotUnreachable()
		{
			// Create graph: Entry -> PortalOut(ch1) ... PortalIn(ch1) -> StateA
			// StateA should be considered reachable
			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();

			var entry = new EntryNode { Id = "entry" };
			var portalOut = new PortalOutNode { Id = "portalOut", ChannelId = 1 };
			var portalIn = new PortalInNode { Id = "portalIn", ChannelId = 1 };
			var stateA = new StateNode { Id = "stateA", DisplayName = "ReachableState" };

			graph.AddNode(entry);
			graph.AddNode(portalOut);
			graph.AddNode(portalIn);
			graph.AddNode(stateA);
			graph.EntryNodeId = "entry";

			// Entry -> PortalOut
			graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "portalOut",
				ConnectionType = ConnectionType.Transition
			});

			// PortalIn -> StateA
			graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "portalIn",
				TargetNodeId = "stateA",
				ConnectionType = ConnectionType.Transition
			});

			var issues = graph.Validate();

			// StateA should NOT be flagged as unreachable (it's reachable through the portal)
			Assert.IsFalse(issues.Exists(i =>
				i.NodeId == "stateA" &&
				i.Message.Contains("unreachable")),
				"State reachable through portal should not be flagged as unreachable");

			UnityEngine.Object.DestroyImmediate(graph);
		}

		[Test]
		public void Validate_StateNotReachableThroughMismatchedPortal_IsUnreachable()
		{
			// Create graph with mismatched channels
			// Entry -> PortalOut(ch1) ... PortalIn(ch2) -> StateA
			// StateA should be unreachable since channels don't match
			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();

			var entry = new EntryNode { Id = "entry" };
			var portalOut = new PortalOutNode { Id = "portalOut", ChannelId = 1 };
			var portalIn = new PortalInNode { Id = "portalIn", ChannelId = 2 }; // Different channel!
			var stateA = new StateNode { Id = "stateA", DisplayName = "UnreachableState" };

			graph.AddNode(entry);
			graph.AddNode(portalOut);
			graph.AddNode(portalIn);
			graph.AddNode(stateA);
			graph.EntryNodeId = "entry";

			graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "portalOut",
				ConnectionType = ConnectionType.Transition
			});

			graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "portalIn",
				TargetNodeId = "stateA",
				ConnectionType = ConnectionType.Transition
			});

			var issues = graph.Validate();

			// StateA should be flagged as unreachable (channels don't match)
			Assert.IsTrue(issues.Exists(i =>
				i.NodeId == "stateA" &&
				i.Message.Contains("unreachable")),
				"State behind mismatched portal channel should be flagged as unreachable");

			UnityEngine.Object.DestroyImmediate(graph);
		}

		[Test]
		public void Validate_MultiplePortalOutsSameChannel_AllReachStates()
		{
			// Multiple PortalOuts pointing to the same PortalIn channel
			// All states connected from that PortalIn should be reachable
			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();

			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode { Id = "stateA", DisplayName = "StateA" };
			var portalOut1 = new PortalOutNode { Id = "portalOut1", ChannelId = 5 };
			var portalOut2 = new PortalOutNode { Id = "portalOut2", ChannelId = 5 };
			var portalIn = new PortalInNode { Id = "portalIn", ChannelId = 5 };
			var stateB = new StateNode { Id = "stateB", DisplayName = "ReachableFromPortal" };

			graph.AddNode(entry);
			graph.AddNode(stateA);
			graph.AddNode(portalOut1);
			graph.AddNode(portalOut2);
			graph.AddNode(portalIn);
			graph.AddNode(stateB);
			graph.EntryNodeId = "entry";

			// Entry -> StateA
			graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "entry", TargetNodeId = "stateA", ConnectionType = ConnectionType.Transition });
			// StateA -> PortalOut1
			graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "stateA", TargetNodeId = "portalOut1", ConnectionType = ConnectionType.Transition });
			// PortalIn -> StateB
			graph.AddConnection(new Connection { Id = "c3", SourceNodeId = "portalIn", TargetNodeId = "stateB", ConnectionType = ConnectionType.Transition });

			var issues = graph.Validate();

			// StateB should be reachable through the portal
			Assert.IsFalse(issues.Exists(i =>
				i.NodeId == "stateB" &&
				i.Message.Contains("unreachable")),
				"State reachable through portal should not be flagged as unreachable");

			UnityEngine.Object.DestroyImmediate(graph);
		}



		[Test]
		public void AddRegion_AddsToGraph()
		{
			var region = new GraphRegion
			{
				Id = "region1",
				Title = "Test Region",
				Bounds = new Rect(0, 0, 200, 150)
			};

			_graph.AddRegion(region);

			Assert.AreEqual(1, _graph.Regions.Count);
			Assert.AreEqual(region, _graph.GetRegion("region1"));
		}

		[Test]
		public void AddRegion_DuplicateId_UpdatesExisting()
		{
			var region1 = new GraphRegion { Id = "region1", Title = "Original" };
			var region2 = new GraphRegion { Id = "region1", Title = "Updated" };

			_graph.AddRegion(region1);
			_graph.AddRegion(region2);

			Assert.AreEqual(1, _graph.Regions.Count);
			Assert.AreEqual("Updated", _graph.GetRegion("region1").Title);
		}

		[Test]
		public void AddRegion_NullRegion_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => _graph.AddRegion(null));
			Assert.AreEqual(0, _graph.Regions.Count);
		}

		[Test]
		public void RemoveRegion_RemovesFromGraph()
		{
			var region = new GraphRegion { Id = "region1", Title = "Test Region" };
			_graph.AddRegion(region);

			_graph.RemoveRegion("region1");

			Assert.AreEqual(0, _graph.Regions.Count);
			Assert.IsNull(_graph.GetRegion("region1"));
		}

		[Test]
		public void RemoveRegion_NonExistent_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => _graph.RemoveRegion("nonexistent"));
		}

		[Test]
		public void GetRegion_ReturnsCorrectRegion()
		{
			var region1 = new GraphRegion { Id = "r1", Title = "Region One" };
			var region2 = new GraphRegion { Id = "r2", Title = "Region Two" };

			_graph.AddRegion(region1);
			_graph.AddRegion(region2);

			var result = _graph.GetRegion("r2");

			Assert.AreEqual("Region Two", result.Title);
		}

		[Test]
		public void GetRegion_NonExistent_ReturnsNull()
		{
			var result = _graph.GetRegion("nonexistent");

			Assert.IsNull(result);
		}

		[Test]
		public void UpdateRegion_UpdatesExisting()
		{
			var region = new GraphRegion { Id = "region1", Title = "Original", Color = Color.red };
			_graph.AddRegion(region);

			var updated = new GraphRegion { Id = "region1", Title = "Updated", Color = Color.blue };
			_graph.UpdateRegion(updated);

			var result = _graph.GetRegion("region1");
			Assert.AreEqual("Updated", result.Title);
			Assert.AreEqual(Color.blue, result.Color);
		}

		[Test]
		public void UpdateRegion_NonExistent_DoesNotAdd()
		{
			var region = new GraphRegion { Id = "nonexistent", Title = "New" };

			_graph.UpdateRegion(region);

			Assert.AreEqual(0, _graph.Regions.Count);
		}

		[Test]
		public void UpdateRegion_NullRegion_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => _graph.UpdateRegion(null));
		}



		[Test]
		public void GetNodesOfType_ReturnsOnlyMatchingTypes()
		{
			var state1 = new StateNode { Id = "state1" };
			var state2 = new StateNode { Id = "state2" };
			var exit1 = new ExitNode { Id = "exit1" };
			var portalOut = new PortalOutNode { Id = "portalOut1" };

			_graph.AddNode(state1);
			_graph.AddNode(state2);
			_graph.AddNode(exit1);
			_graph.AddNode(portalOut);

			var stateNodes = _graph.GetNodesOfType<StateNode>().ToList();
			var exitNodes = _graph.GetNodesOfType<ExitNode>().ToList();
			var portalOutNodes = _graph.GetNodesOfType<PortalOutNode>().ToList();

			Assert.AreEqual(2, stateNodes.Count);
			Assert.AreEqual(1, exitNodes.Count);
			Assert.AreEqual(1, portalOutNodes.Count);
		}

		[Test]
		public void GetNodesOfType_NoMatchingNodes_ReturnsEmpty()
		{
			var state = new StateNode { Id = "state1" };
			_graph.AddNode(state);

			var portalNodes = _graph.GetNodesOfType<PortalInNode>().ToList();

			Assert.AreEqual(0, portalNodes.Count);
		}

		[Test]
		public void HasNode_ExistingNode_ReturnsTrue()
		{
			var node = new StateNode { Id = "test-node" };
			_graph.AddNode(node);

			Assert.IsTrue(_graph.HasNode("test-node"));
		}

		[Test]
		public void HasNode_NonExistentNode_ReturnsFalse()
		{
			Assert.IsFalse(_graph.HasNode("nonexistent"));
		}

		[Test]
		public void HasNode_AfterRemoval_ReturnsFalse()
		{
			var node = new StateNode { Id = "test-node" };
			_graph.AddNode(node);
			_graph.RemoveNode("test-node");

			Assert.IsFalse(_graph.HasNode("test-node"));
		}

		[Test]
		public void GetEntryNode_WithValidEntry_ReturnsEntryNode()
		{
			var entry = new EntryNode { Id = "entry-node" };
			_graph.AddNode(entry);
			_graph.EntryNodeId = "entry-node";

			var result = _graph.GetEntryNode();

			Assert.IsNotNull(result);
			Assert.AreEqual("entry-node", result.Id);
		}

		[Test]
		public void GetEntryNode_WithNullEntryId_ReturnsNull()
		{
			_graph.EntryNodeId = null;

			var result = _graph.GetEntryNode();

			Assert.IsNull(result);
		}

		[Test]
		public void GetEntryNode_WithEmptyEntryId_ReturnsNull()
		{
			_graph.EntryNodeId = "";

			var result = _graph.GetEntryNode();

			Assert.IsNull(result);
		}



		[Test]
		public void NodeCount_ReflectsActualCount()
		{
			var initialCount = _graph.NodeCount;

			var node1 = new StateNode { Id = "node1" };
			var node2 = new StateNode { Id = "node2" };
			_graph.AddNode(node1);
			_graph.AddNode(node2);

			Assert.AreEqual(initialCount + 2, _graph.NodeCount);
		}

		[Test]
		public void NodeCount_AfterRemoval_Decreases()
		{
			var node = new StateNode { Id = "node1" };
			_graph.AddNode(node);
			var countAfterAdd = _graph.NodeCount;

			_graph.RemoveNode("node1");

			Assert.AreEqual(countAfterAdd - 1, _graph.NodeCount);
		}

		[Test]
		public void ConnectionCount_ReflectsActualCount()
		{
			Assert.AreEqual(0, _graph.ConnectionCount);

			var conn1 = new Connection { Id = "c1", SourceNodeId = "n1", TargetNodeId = "n2" };
			var conn2 = new Connection { Id = "c2", SourceNodeId = "n2", TargetNodeId = "n3" };

			_graph.AddConnection(conn1);
			Assert.AreEqual(1, _graph.ConnectionCount);

			_graph.AddConnection(conn2);
			Assert.AreEqual(2, _graph.ConnectionCount);
		}

		[Test]
		public void ConnectionCount_AfterRemoval_Decreases()
		{
			var conn = new Connection { Id = "c1", SourceNodeId = "n1", TargetNodeId = "n2" };
			_graph.AddConnection(conn);

			_graph.RemoveConnection("c1");

			Assert.AreEqual(0, _graph.ConnectionCount);
		}



		[Test]
		public void ContextType_SetAndGet_PreservesType()
		{
			_graph.ContextType = typeof(TestContextClass);

			Assert.AreEqual(typeof(TestContextClass), _graph.ContextType);
		}

		[Test]
		public void ContextType_SetNull_ReturnsNull()
		{
			_graph.ContextType = typeof(TestContextClass);
			_graph.ContextType = null;

			Assert.IsNull(_graph.ContextType);
		}

		[Test]
		public void RequiresContext_WithContextType_ReturnsTrue()
		{
			_graph.ContextType = typeof(TestContextClass);

			Assert.IsTrue(_graph.RequiresContext);
		}

		[Test]
		public void RequiresContext_WithoutContextType_ReturnsFalse()
		{
			_graph.ContextType = null;

			Assert.IsFalse(_graph.RequiresContext);
		}

		[Test]
		public void ContextTypeAssemblyQualifiedName_ReturnsCorrectName()
		{
			_graph.ContextType = typeof(TestContextClass);

			Assert.IsNotNull(_graph.ContextTypeAssemblyQualifiedName);
			Assert.IsTrue(_graph.ContextTypeAssemblyQualifiedName.Contains("TestContextClass"));
		}

		// Test context class for context type tests
		private class TestContextClass { }



		[Test]
		public void GraphViewData_ViewPosition_SetAndGet()
		{
			_graph.GraphViewData.ViewPosition = new Vector3(100, 200, 0);

			Assert.AreEqual(new Vector3(100, 200, 0), _graph.GraphViewData.ViewPosition);
		}

		[Test]
		public void GraphViewData_HasBeenSet_TrueAfterPositionSet()
		{
			Assert.IsFalse(_graph.GraphViewData.HasBeenSet);

			_graph.GraphViewData.ViewPosition = new Vector3(10, 20, 0);

			Assert.IsTrue(_graph.GraphViewData.HasBeenSet);
		}

		[Test]
		public void GraphViewData_Zoom_SetAndGet()
		{
			_graph.GraphViewData.Zoom = 1.5f;

			Assert.AreEqual(1.5f, _graph.GraphViewData.Zoom);
		}

		[Test]
		public void GraphViewData_ViewScale_SetAndGet()
		{
			_graph.GraphViewData.ViewScale = new Vector3(2f, 2f, 1f);

			Assert.AreEqual(new Vector3(2f, 2f, 1f), _graph.GraphViewData.ViewScale);
		}

		[Test]
		public void GraphViewData_AddBookmark_AddsToList()
		{
			var bookmark = new GraphBookmark("Test", Vector3.zero, Vector3.one, 1f);

			_graph.GraphViewData.AddBookmark(bookmark);

			Assert.AreEqual(1, _graph.GraphViewData.Bookmarks.Count);
		}

		[Test]
		public void GraphViewData_RemoveBookmark_RemovesFromList()
		{
			var bookmark = new GraphBookmark("Test", Vector3.zero, Vector3.one, 1f);
			_graph.GraphViewData.AddBookmark(bookmark);

			_graph.GraphViewData.RemoveBookmark(bookmark.Id);

			Assert.AreEqual(0, _graph.GraphViewData.Bookmarks.Count);
		}

		[Test]
		public void GraphViewData_GetBookmark_ReturnsCorrectBookmark()
		{
			var bookmark1 = new GraphBookmark("Bookmark1", Vector3.zero, Vector3.one, 1f);
			var bookmark2 = new GraphBookmark("Bookmark2", new Vector3(100, 100, 0), Vector3.one, 2f);

			_graph.GraphViewData.AddBookmark(bookmark1);
			_graph.GraphViewData.AddBookmark(bookmark2);

			var result = _graph.GraphViewData.GetBookmark(bookmark2.Id);

			Assert.IsNotNull(result);
			Assert.AreEqual("Bookmark2", result.Name);
		}

		[Test]
		public void GraphViewData_UpdateBookmark_UpdatesExisting()
		{
			var bookmark = new GraphBookmark("Original", Vector3.zero, Vector3.one, 1f);
			_graph.GraphViewData.AddBookmark(bookmark);

			bookmark.Name = "Updated";
			bookmark.Zoom = 2.5f;
			_graph.GraphViewData.UpdateBookmark(bookmark);

			var result = _graph.GraphViewData.GetBookmark(bookmark.Id);
			Assert.AreEqual("Updated", result.Name);
			Assert.AreEqual(2.5f, result.Zoom);
		}

		[Test]
		public void GraphViewData_SetPeekPreviewState_AddsState()
		{
			_graph.GraphViewData.SetPeekPreviewState("node1", new Vector2(100, 100), Vector2.zero, 1f);

			var state = _graph.GraphViewData.GetPeekPreviewState("node1");

			Assert.IsNotNull(state);
			Assert.AreEqual("node1", state.NodeId);
			Assert.AreEqual(new Vector2(100, 100), state.Position);
		}

		[Test]
		public void GraphViewData_SetPeekPreviewState_UpdatesExisting()
		{
			_graph.GraphViewData.SetPeekPreviewState("node1", new Vector2(100, 100), Vector2.zero, 1f);
			_graph.GraphViewData.SetPeekPreviewState("node1", new Vector2(200, 200), new Vector2(10, 10), 2f);

			var state = _graph.GraphViewData.GetPeekPreviewState("node1");

			Assert.AreEqual(new Vector2(200, 200), state.Position);
			Assert.AreEqual(new Vector2(10, 10), state.PanOffset);
			Assert.AreEqual(2f, state.ZoomLevel);
		}

		[Test]
		public void GraphViewData_RemovePeekPreviewState_RemovesState()
		{
			_graph.GraphViewData.SetPeekPreviewState("node1", new Vector2(100, 100), Vector2.zero, 1f);

			_graph.GraphViewData.RemovePeekPreviewState("node1");

			Assert.IsNull(_graph.GraphViewData.GetPeekPreviewState("node1"));
		}



		[Test]
		public void Validate_SubStateMachine_NoSubGraph_ReturnsWarning()
		{
			var subNode = new SubStateMachineNode
			{
				Id = "subNode",
				DisplayName = "EmptySub",
				SubGraph = null
			};
			_graph.AddNode(subNode);

			var issues = _graph.Validate();

			Assert.IsTrue(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Warning &&
				i.NodeId == "subNode" &&
				i.Message.Contains("no sub-graph")));
		}

		[Test]
		public void Validate_SubStateMachine_SubGraphMissingEntry_ReturnsError()
		{
			var subGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			// Remove the auto-created entry node
			var entryNode = subGraph.GetEntryNode();
			if (entryNode != null) subGraph.RemoveNode(entryNode.Id);
			subGraph.EntryNodeId = null;

			var subNode = new SubStateMachineNode
			{
				Id = "subNode",
				DisplayName = "SubWithNoEntry",
				SubGraph = subGraph
			};
			_graph.AddNode(subNode);

			var issues = _graph.Validate();

			Assert.IsTrue(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.NodeId == "subNode" &&
				i.Message.Contains("no entry node")));

			UnityEngine.Object.DestroyImmediate(subGraph);
		}

		[Test]
		public void Validate_SubStateMachine_RequiresContextButParentHasNone_ReturnsError()
		{
			var subGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			subGraph.ContextType = typeof(TestContextClass);

			var subNode = new SubStateMachineNode
			{
				Id = "subNode",
				DisplayName = "SubRequiresContext",
				SubGraph = subGraph
			};

			// Parent has no context type
			_graph.ContextType = null;
			_graph.AddNode(subNode);

			var issues = _graph.Validate();

			Assert.IsTrue(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.NodeId == "subNode" &&
				i.Message.Contains("requires context type")));

			UnityEngine.Object.DestroyImmediate(subGraph);
		}

		[Test]
		public void Validate_SubStateMachine_NoExitConnection_ReturnsWarning()
		{
			var subGraph = ScriptableObject.CreateInstance<StateMachineGraph>();

			var subNode = new SubStateMachineNode
			{
				Id = "subNode",
				DisplayName = "SubNoExit",
				SubGraph = subGraph
			};
			_graph.AddNode(subNode);

			// No connection from subNode

			var issues = _graph.Validate();

			Assert.IsTrue(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Warning &&
				i.NodeId == "subNode" &&
				i.Message.Contains("no exit connection")));

			UnityEngine.Object.DestroyImmediate(subGraph);
		}

		[Test]
		public void Validate_SubStateMachine_WithExitConnection_NoExitWarning()
		{
			var subGraph = ScriptableObject.CreateInstance<StateMachineGraph>();

			var subNode = new SubStateMachineNode
			{
				Id = "subNode",
				DisplayName = "SubWithExit",
				SubGraph = subGraph
			};
			var nextState = new StateNode { Id = "nextState" };

			_graph.AddNode(subNode);
			_graph.AddNode(nextState);
			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "subNode",
				TargetNodeId = "nextState",
				ConnectionType = ConnectionType.Transition
			});

			var issues = _graph.Validate();

			Assert.IsFalse(issues.Exists(i =>
				i.NodeId == "subNode" &&
				i.Message.Contains("no exit connection")));

			UnityEngine.Object.DestroyImmediate(subGraph);
		}



		[Test]
		public void Validate_PortalOut_NoMatchingPortalIn_ReturnsWarning()
		{
			var portalOut = new PortalOutNode { Id = "portalOut", ChannelId = 99 };
			_graph.AddNode(portalOut);

			var issues = _graph.Validate();

			Assert.IsTrue(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Warning &&
				i.NodeId == "portalOut" &&
				i.Message.Contains("no matching PortalIn")));
		}

		[Test]
		public void Validate_DuplicatePortalIn_SameChannel_ReturnsError()
		{
			var portalIn1 = new PortalInNode { Id = "portalIn1", ChannelId = 5 };
			var portalIn2 = new PortalInNode { Id = "portalIn2", ChannelId = 5 };

			_graph.AddNode(portalIn1);
			_graph.AddNode(portalIn2);

			var issues = _graph.Validate();

			Assert.IsTrue(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Error &&
				i.Message.Contains("Multiple PortalIn")));
		}

		[Test]
		public void Validate_PortalIn_NoPortalOuts_ReturnsInfo()
		{
			var portalIn = new PortalInNode { Id = "portalIn", ChannelId = 10 };
			_graph.AddNode(portalIn);

			// Add an outgoing connection to avoid "no outgoing connection" warning
			var state = new StateNode { Id = "state1" };
			_graph.AddNode(state);
			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "portalIn",
				TargetNodeId = "state1",
				ConnectionType = ConnectionType.Transition
			});

			var issues = _graph.Validate();

			Assert.IsTrue(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Info &&
				i.NodeId == "portalIn" &&
				i.Message.Contains("no PortalOut")));
		}

		[Test]
		public void Validate_PortalIn_NoOutgoingConnection_ReturnsWarning()
		{
			var portalIn = new PortalInNode { Id = "portalIn", ChannelId = 5 };
			var portalOut = new PortalOutNode { Id = "portalOut", ChannelId = 5 };

			_graph.AddNode(portalIn);
			_graph.AddNode(portalOut);
			// No connection from portalIn

			var issues = _graph.Validate();

			Assert.IsTrue(issues.Exists(i =>
				i.Severity == GraphValidationSeverity.Warning &&
				i.NodeId == "portalIn" &&
				i.Message.Contains("no outgoing connection")));
		}



		[Test]
		public void InitializeNewGraph_CreatesEntryNode()
		{
			var newGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			// Clear any auto-created entry
			var existingEntry = newGraph.GetEntryNode();
			if (existingEntry != null) newGraph.RemoveNode(existingEntry.Id);
			newGraph.EntryNodeId = null;

			newGraph.InitializeNewGraph();

			Assert.IsNotNull(newGraph.EntryNodeId);
			Assert.IsNotNull(newGraph.GetEntryNode());

			UnityEngine.Object.DestroyImmediate(newGraph);
		}

		[Test]
		public void InitializeNewGraph_DoesNotDuplicateEntry()
		{
			// Graph should already have an entry node from Reset()
			var entryCountBefore = _graph.GetNodesOfType<EntryNode>().Count();

			_graph.InitializeNewGraph();

			var entryCountAfter = _graph.GetNodesOfType<EntryNode>().Count();
			Assert.AreEqual(entryCountBefore, entryCountAfter);
		}



		[Test]
		public void GraphBookmark_Constructor_GeneratesId()
		{
			var bookmark = new GraphBookmark();

			Assert.IsNotNull(bookmark.Id);
			Assert.IsFalse(string.IsNullOrEmpty(bookmark.Id));
		}

		[Test]
		public void GraphBookmark_ParameterizedConstructor_SetsValues()
		{
			var position = new Vector3(100, 200, 0);
			var scale = new Vector3(1.5f, 1.5f, 1f);
			var bookmark = new GraphBookmark("TestBookmark", position, scale, 2f);

			Assert.AreEqual("TestBookmark", bookmark.Name);
			Assert.AreEqual(position, bookmark.ViewPosition);
			Assert.AreEqual(scale, bookmark.ViewScale);
			Assert.AreEqual(2f, bookmark.Zoom);
		}

		[Test]
		public void GraphBookmark_Slot_CanBeSet()
		{
			var bookmark = new GraphBookmark();
			bookmark.Slot = 3;

			Assert.AreEqual(3, bookmark.Slot);
		}



		[Test]
		public void OnBeforeSerialize_OnAfterDeserialize_PreservesNodes()
		{
			var state = new StateNode { Id = "state1", DisplayName = "TestState", Position = new Vector2(50, 100) };
			_graph.AddNode(state);

			_graph.OnBeforeSerialize();
			_graph.OnAfterDeserialize();

			var restored = _graph.GetNode("state1");
			Assert.IsNotNull(restored);
			Assert.AreEqual("TestState", restored.DisplayName);
		}

		[Test]
		public void OnBeforeSerialize_OnAfterDeserialize_PreservesConnections()
		{
			var conn = new Connection
			{
				Id = "c1",
				SourceNodeId = "node1",
				SourcePortName = "output",
				TargetNodeId = "node2",
				TargetPortName = "input",
				ConnectionType = ConnectionType.Transition
			};
			_graph.AddConnection(conn);

			_graph.OnBeforeSerialize();
			_graph.OnAfterDeserialize();

			var fromNode1 = _graph.GetConnectionsFromNode("node1").ToList();
			Assert.AreEqual(1, fromNode1.Count);
			Assert.AreEqual("c1", fromNode1[0].Id);

			var toNode2 = _graph.GetConnectionsToNode("node2").ToList();
			Assert.AreEqual(1, toNode2.Count);
		}

		[Test]
		public void OnBeforeSerialize_OnAfterDeserialize_PreservesBlackboardDefinitions()
		{
			var definition = new Blackboard.BlackboardVariableDefinition
			{
				Key = "health",
				ValueType = typeof(int),
				IsExposed = true,
				Category = "Stats"
			};
			_graph.AddBlackboardVariable(definition);

			_graph.OnBeforeSerialize();
			_graph.OnAfterDeserialize();

			var restored = _graph.GetBlackboardVariable("health");
			Assert.IsNotNull(restored);
			Assert.AreEqual(typeof(int), restored.ValueType);
			Assert.IsTrue(restored.IsExposed);
			Assert.AreEqual("Stats", restored.Category);
		}

		[Test]
		public void OnBeforeSerialize_OnAfterDeserialize_PreservesRegions()
		{
			var region = new GraphRegion { Id = "r1", Title = "My Region" };
			_graph.AddRegion(region);

			_graph.OnBeforeSerialize();
			_graph.OnAfterDeserialize();

			var restored = _graph.GetRegion("r1");
			Assert.IsNotNull(restored);
			Assert.AreEqual("My Region", restored.Title);
		}

		[Test]
		public void OnBeforeSerialize_OnAfterDeserialize_PreservesContextType()
		{
			_graph.ContextType = typeof(string);

			_graph.OnBeforeSerialize();
			_graph.OnAfterDeserialize();

			Assert.AreEqual(typeof(string), _graph.ContextType);
		}



		[Test]
		public void NotifyBlackboardChanged_FiresEvent()
		{
			StateMachineGraph receivedGraph = null;
			string receivedKey = null;

			Action<StateMachineGraph, string> handler = (graph, key) =>
			{
				receivedGraph = graph;
				receivedKey = key;
			};

			StateMachineGraph.OnBlackboardChanged += handler;
			try
			{
				_graph.NotifyBlackboardChanged("testVar");

				Assert.AreEqual(_graph, receivedGraph);
				Assert.AreEqual("testVar", receivedKey);
			}
			finally
			{
				StateMachineGraph.OnBlackboardChanged -= handler;
			}
		}

		[Test]
		public void NotifyBlackboardChanged_WithNullKey_FiresEvent()
		{
			StateMachineGraph receivedGraph = null;
			string receivedKey = "initial";

			Action<StateMachineGraph, string> handler = (graph, key) =>
			{
				receivedGraph = graph;
				receivedKey = key;
			};

			StateMachineGraph.OnBlackboardChanged += handler;
			try
			{
				_graph.NotifyBlackboardChanged(null);

				Assert.AreEqual(_graph, receivedGraph);
				Assert.IsNull(receivedKey);
			}
			finally
			{
				StateMachineGraph.OnBlackboardChanged -= handler;
			}
		}

		[Test]
		public void NotifyBlackboardVariableRenamed_FiresEvent()
		{
			StateMachineGraph receivedGraph = null;
			string receivedOldKey = null;
			string receivedNewKey = null;

			Action<StateMachineGraph, string, string> handler = (graph, oldKey, newKey) =>
			{
				receivedGraph = graph;
				receivedOldKey = oldKey;
				receivedNewKey = newKey;
			};

			StateMachineGraph.OnBlackboardVariableRenamed += handler;
			try
			{
				_graph.NotifyBlackboardVariableRenamed("oldName", "newName");

				Assert.AreEqual(_graph, receivedGraph);
				Assert.AreEqual("oldName", receivedOldKey);
				Assert.AreEqual("newName", receivedNewKey);
			}
			finally
			{
				StateMachineGraph.OnBlackboardVariableRenamed -= handler;
			}
		}



		[Test]
		public void GetNodeGeneric_ReturnsTypedNode()
		{
			var entry = new EntryNode { Id = "entry1" };
			_graph.AddNode(entry);

			var result = _graph.GetNode<EntryNode>("entry1");

			Assert.IsNotNull(result);
			Assert.IsInstanceOf<EntryNode>(result);
		}

		[Test]
		public void GetNodeGeneric_WrongType_ReturnsNull()
		{
			var state = new StateNode { Id = "state1" };
			_graph.AddNode(state);

			var result = _graph.GetNode<EntryNode>("state1");

			Assert.IsNull(result);
		}

		[Test]
		public void GetNodeGeneric_NonExistent_ReturnsNull()
		{
			var result = _graph.GetNode<StateNode>("nonexistent");

			Assert.IsNull(result);
		}



		[Test]
		public void AddBlackboardVariable_DuplicateKey_UpdatesExisting()
		{
			var def1 = new Blackboard.BlackboardVariableDefinition
			{
				Key = "health",
				ValueType = typeof(int),
				Category = "Original"
			};
			var def2 = new Blackboard.BlackboardVariableDefinition
			{
				Key = "health",
				ValueType = typeof(float),
				Category = "Updated"
			};

			_graph.AddBlackboardVariable(def1);
			_graph.AddBlackboardVariable(def2);

			Assert.AreEqual(1, _graph.BlackboardDefinitions.Count);
			var result = _graph.GetBlackboardVariable("health");
			Assert.AreEqual("Updated", result.Category);
		}

		[Test]
		public void AddBlackboardVariable_NullDefinition_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => _graph.AddBlackboardVariable(null));
			Assert.AreEqual(0, _graph.BlackboardDefinitions.Count);
		}

		[Test]
		public void BlackboardDefinitions_ReturnsAllDefinitions()
		{
			var def1 = new Blackboard.BlackboardVariableDefinition { Key = "health", ValueType = typeof(int) };
			var def2 = new Blackboard.BlackboardVariableDefinition { Key = "speed", ValueType = typeof(float) };

			_graph.AddBlackboardVariable(def1);
			_graph.AddBlackboardVariable(def2);

			Assert.AreEqual(2, _graph.BlackboardDefinitions.Count);
		}



		[Test]
		public void RemoveNode_CleansUpRegionContainment()
		{
			var node = new StateNode { Id = "node1" };
			_graph.AddNode(node);

			var region = new GraphRegion { Id = "r1", Title = "Region" };
			region.AddNode("node1");
			_graph.AddRegion(region);

			_graph.RemoveNode("node1");

			var result = _graph.GetRegion("r1");
			Assert.IsFalse(result.ContainedNodeIds.Contains("node1"));
		}

		[Test]
		public void RemoveNode_CleansUpConnectionsFromAndTo()
		{
			var node1 = new StateNode { Id = "node1" };
			var node2 = new StateNode { Id = "node2" };
			var node3 = new StateNode { Id = "node3" };

			_graph.AddNode(node1);
			_graph.AddNode(node2);
			_graph.AddNode(node3);

			_graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "node1", TargetNodeId = "node2" });
			_graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "node2", TargetNodeId = "node3" });
			_graph.AddConnection(new Connection { Id = "c3", SourceNodeId = "node1", TargetNodeId = "node3" });

			_graph.RemoveNode("node2");

			// Connections involving node2 should be removed
			Assert.AreEqual(1, _graph.Connections.Count);
			Assert.AreEqual("c3", _graph.Connections[0].Id);
		}



		[Test]
		public void GraphValidationIssue_Constructor_SetsProperties()
		{
			var issue = new GraphValidationIssue(
				GraphValidationSeverity.Error,
				"Test error message",
				"node123"
			);

			Assert.AreEqual(GraphValidationSeverity.Error, issue.Severity);
			Assert.AreEqual("Test error message", issue.Message);
			Assert.AreEqual("node123", issue.NodeId);
		}

		[Test]
		public void GraphValidationIssue_NullNodeId_IsAllowed()
		{
			var issue = new GraphValidationIssue(
				GraphValidationSeverity.Warning,
				"Graph-wide issue",
				null
			);

			Assert.IsNull(issue.NodeId);
		}



		[Test]
		public void AddNode_DuplicateId_OverwritesExisting()
		{
			var node1 = new StateNode { Id = "dup", DisplayName = "First" };
			var node2 = new StateNode { Id = "dup", DisplayName = "Second" };

			_graph.AddNode(node1);
			_graph.AddNode(node2);

			var result = _graph.GetNode("dup");
			Assert.AreEqual("Second", result.DisplayName);
		}

		[Test]
		public void AddNode_EntryNode_SetsEntryNodeId_WhenEmpty()
		{
			// Clear the auto-created entry
			var existing = _graph.GetEntryNode();
			if (existing != null) _graph.RemoveNode(existing.Id);
			_graph.EntryNodeId = null;

			var entry = new EntryNode { Id = "new-entry" };
			_graph.AddNode(entry);

			Assert.AreEqual("new-entry", _graph.EntryNodeId);
		}

		[Test]
		public void AddNode_EntryNode_DoesNotOverrideExistingEntryId()
		{
			// _graph already has an entry from Reset()
			var originalEntryId = _graph.EntryNodeId;

			var newEntry = new EntryNode { Id = "new-entry" };
			_graph.AddNode(newEntry);

			Assert.AreEqual(originalEntryId, _graph.EntryNodeId);
		}



		[Test]
		public void RemoveConnection_NonExistentId_IsNoOp()
		{
			_graph.RemoveConnection("nonexistent");

			Assert.AreEqual(0, _graph.ConnectionCount);
		}

		[Test]
		public void RemoveNode_NonExistentId_IsNoOp()
		{
			var initialCount = _graph.GetAllNodes().Count();

			_graph.RemoveNode("nonexistent");

			Assert.AreEqual(initialCount, _graph.GetAllNodes().Count());
		}

		[Test]
		public void RemoveBlackboardVariable_NonExistentKey_IsNoOp()
		{
			_graph.RemoveBlackboardVariable("nonexistent");

			Assert.IsNull(_graph.GetBlackboardVariable("nonexistent"));
		}

		[Test]
		public void GetConnectionsFromNode_NoConnections_ReturnsEmpty()
		{
			var connections = _graph.GetConnectionsFromNode("nonexistent");

			Assert.IsNotNull(connections);
			Assert.AreEqual(0, connections.Count());
		}

		[Test]
		public void GetConnectionsToNode_NoConnections_ReturnsEmpty()
		{
			var connections = _graph.GetConnectionsToNode("nonexistent");

			Assert.IsNotNull(connections);
			Assert.AreEqual(0, connections.Count());
		}

		[Test]
		public void GetConnectionsFromPort_NoMatchingPort_ReturnsEmpty()
		{
			var conn = new Connection
			{
				Id = "c1",
				SourceNodeId = "node1",
				SourcePortName = "output",
				TargetNodeId = "node2",
				TargetPortName = "input"
			};
			_graph.AddConnection(conn);

			var results = _graph.GetConnectionsFromPort("node1", "nonexistent").ToList();

			Assert.AreEqual(0, results.Count);
		}

		[Test]
		public void IsVariant_BaseGraph_ReturnsFalse()
		{
			Assert.IsFalse(_graph.IsVariant);
		}

		[Test]
		public void Connections_Property_ReturnsAllConnections()
		{
			var conn1 = new Connection { Id = "c1", SourceNodeId = "n1", TargetNodeId = "n2" };
			var conn2 = new Connection { Id = "c2", SourceNodeId = "n2", TargetNodeId = "n3" };
			_graph.AddConnection(conn1);
			_graph.AddConnection(conn2);

			var connections = _graph.Connections;

			Assert.AreEqual(2, connections.Count);
			Assert.IsTrue(connections.Any(c => c.Id == "c1"));
			Assert.IsTrue(connections.Any(c => c.Id == "c2"));
		}

		[Test]
		public void ContextType_FromAssemblyQualifiedName_RestoresAfterDeserialization()
		{
			_graph.ContextType = typeof(Transform);
			_graph.OnBeforeSerialize();
			_graph.OnAfterDeserialize();

			Assert.AreEqual(typeof(Transform), _graph.ContextType);
			Assert.AreEqual(typeof(Transform).AssemblyQualifiedName, _graph.ContextTypeAssemblyQualifiedName);
		}

		[Test]
		public void ContextType_InvalidAssemblyQualifiedName_ReturnsNull()
		{
			var aqnField = typeof(StateMachineGraph).GetField(
				"_contextTypeAssemblyQualifiedName",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var typeField = typeof(StateMachineGraph).GetField(
				"_contextType",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

			aqnField.SetValue(_graph, "Some.Nonexistent.Type, FakeAssembly");
			typeField.SetValue(_graph, null);

			Assert.IsNull(_graph.ContextType);
		}

		[Test]
		public void AddConnection_MultipleToSameTarget_AllIndexed()
		{
			var conn1 = new Connection { Id = "c1", SourceNodeId = "n1", TargetNodeId = "target" };
			var conn2 = new Connection { Id = "c2", SourceNodeId = "n2", TargetNodeId = "target" };
			var conn3 = new Connection { Id = "c3", SourceNodeId = "n3", TargetNodeId = "target" };
			_graph.AddConnection(conn1);
			_graph.AddConnection(conn2);
			_graph.AddConnection(conn3);

			var toTarget = _graph.GetConnectionsToNode("target").ToList();

			Assert.AreEqual(3, toTarget.Count);
		}

		[Test]
		public void RemoveNode_RemovesAllAssociatedConnections()
		{
			var node1 = new StateNode { Id = "n1" };
			var node2 = new StateNode { Id = "n2" };
			var node3 = new StateNode { Id = "n3" };
			_graph.AddNode(node1);
			_graph.AddNode(node2);
			_graph.AddNode(node3);

			var connOut = new Connection { Id = "c1", SourceNodeId = "n1", TargetNodeId = "n2" };
			var connIn = new Connection { Id = "c2", SourceNodeId = "n3", TargetNodeId = "n1" };
			_graph.AddConnection(connOut);
			_graph.AddConnection(connIn);

			_graph.RemoveNode("n1");

			Assert.AreEqual(0, _graph.ConnectionCount);
			Assert.AreEqual(0, _graph.GetConnectionsFromNode("n1").Count());
			Assert.AreEqual(0, _graph.GetConnectionsToNode("n1").Count());
		}

		[Test]
		public void AddBlackboardVariable_MultipleVariables_AllAccessible()
		{
			var def1 = new Blackboard.BlackboardVariableDefinition { Key = "health" };
			var def2 = new Blackboard.BlackboardVariableDefinition { Key = "speed" };
			var def3 = new Blackboard.BlackboardVariableDefinition { Key = "name" };
			_graph.AddBlackboardVariable(def1);
			_graph.AddBlackboardVariable(def2);
			_graph.AddBlackboardVariable(def3);

			Assert.AreEqual(3, _graph.BlackboardDefinitions.Count);
			Assert.IsNotNull(_graph.GetBlackboardVariable("health"));
			Assert.IsNotNull(_graph.GetBlackboardVariable("speed"));
			Assert.IsNotNull(_graph.GetBlackboardVariable("name"));
		}

		[Test]
		public void PeekPreviewState_DefaultZoomLevel_IsOne()
		{
			var state = new PeekPreviewState();

			Assert.AreEqual(1f, state.ZoomLevel);
		}

		[Test]
		public void PeekPreviewState_SetProperties_PreservesValues()
		{
			var state = new PeekPreviewState
			{
				NodeId = "node1",
				Position = new Vector2(50, 75),
				PanOffset = new Vector2(10, 20),
				ZoomLevel = 1.5f
			};

			Assert.AreEqual("node1", state.NodeId);
			Assert.AreEqual(new Vector2(50, 75), state.Position);
			Assert.AreEqual(new Vector2(10, 20), state.PanOffset);
			Assert.AreEqual(1.5f, state.ZoomLevel);
		}


	}
}
