using System.Linq;
using NUnit.Framework;
using Nonatomic.VSM.Blackboard;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class StateMachineGraphNodeTests
	{
		private StateMachineGraph _graph;

		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			_graph.InitializeNewGraph();
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_graph);
		}

		[Test]
		public void InitializeNewGraph_CreatesEntryNode()
		{
			Assert.IsFalse(string.IsNullOrEmpty(_graph.EntryNodeId));
		}

		[Test]
		public void InitializeNewGraph_EntryNodeIsRetrievable()
		{
			var entry = _graph.GetEntryNode();

			Assert.IsNotNull(entry);
			Assert.AreEqual(_graph.EntryNodeId, entry.Id);
		}

		[Test]
		public void InitializeNewGraph_NodeCountIsOne()
		{
			Assert.AreEqual(1, _graph.NodeCount);
		}

		[Test]
		public void InitializeNewGraph_CalledTwice_DoesNotDuplicate()
		{
			var originalId = _graph.EntryNodeId;

			_graph.InitializeNewGraph();

			Assert.AreEqual(originalId, _graph.EntryNodeId);
			Assert.AreEqual(1, _graph.NodeCount);
		}

		[Test]
		public void AddNode_StateNode_IncreasesNodeCount()
		{
			var stateNode = new StateNode();

			_graph.AddNode(stateNode);

			Assert.AreEqual(2, _graph.NodeCount);
		}

		[Test]
		public void AddNode_NullNode_IsNoOp()
		{
			var countBefore = _graph.NodeCount;

			_graph.AddNode(null);

			Assert.AreEqual(countBefore, _graph.NodeCount);
		}

		[Test]
		public void AddNode_DuplicateId_OverwritesExisting()
		{
			var node1 = new StateNode();
			node1.DisplayName = "First";
			_graph.AddNode(node1);

			var node2 = new StateNode(node1.Id);
			node2.DisplayName = "Second";
			_graph.AddNode(node2);

			var retrieved = _graph.GetNode(node1.Id);
			Assert.AreEqual("Second", retrieved.DisplayName);
			Assert.AreEqual(2, _graph.NodeCount);
		}

		[Test]
		public void AddNode_EntryNode_SetsEntryNodeIdWhenEmpty()
		{
			var freshGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			var entry = new EntryNode();

			freshGraph.AddNode(entry);

			Assert.AreEqual(entry.Id, freshGraph.EntryNodeId);
			Object.DestroyImmediate(freshGraph);
		}

		[Test]
		public void GetNode_ExistingId_ReturnsNode()
		{
			var stateNode = new StateNode();
			_graph.AddNode(stateNode);

			var retrieved = _graph.GetNode(stateNode.Id);

			Assert.AreSame(stateNode, retrieved);
		}

		[Test]
		public void GetNode_NonExistentId_ReturnsNull()
		{
			var result = _graph.GetNode("non-existent-id");

			Assert.IsNull(result);
		}

		[Test]
		public void GetNodeTyped_CorrectType_ReturnsCast()
		{
			var exitNode = new ExitNode();
			_graph.AddNode(exitNode);

			var result = _graph.GetNode<ExitNode>(exitNode.Id);

			Assert.IsNotNull(result);
			Assert.AreSame(exitNode, result);
		}

		[Test]
		public void GetNodeTyped_WrongType_ReturnsNull()
		{
			var stateNode = new StateNode();
			_graph.AddNode(stateNode);

			var result = _graph.GetNode<ExitNode>(stateNode.Id);

			Assert.IsNull(result);
		}

		[Test]
		public void HasNode_ExistingNode_ReturnsTrue()
		{
			var stateNode = new StateNode();
			_graph.AddNode(stateNode);

			Assert.IsTrue(_graph.HasNode(stateNode.Id));
		}

		[Test]
		public void HasNode_NonExistentNode_ReturnsFalse()
		{
			Assert.IsFalse(_graph.HasNode("does-not-exist"));
		}

		[Test]
		public void GetAllNodes_ReturnsAllAddedNodes()
		{
			var state1 = new StateNode();
			var state2 = new StateNode();
			_graph.AddNode(state1);
			_graph.AddNode(state2);

			var allNodes = _graph.GetAllNodes().ToList();

			Assert.AreEqual(3, allNodes.Count);
			Assert.IsTrue(allNodes.Contains(state1));
			Assert.IsTrue(allNodes.Contains(state2));
		}

		[Test]
		public void GetNodesOfType_ReturnsOnlyMatchingType()
		{
			var state = new StateNode();
			var exit = new ExitNode();
			_graph.AddNode(state);
			_graph.AddNode(exit);

			var exitNodes = _graph.GetNodesOfType<ExitNode>().ToList();

			Assert.AreEqual(1, exitNodes.Count);
			Assert.AreSame(exit, exitNodes[0]);
		}

		[Test]
		public void GetNodesOfType_NoMatches_ReturnsEmpty()
		{
			var nodes = _graph.GetNodesOfType<ExitNode>().ToList();

			Assert.AreEqual(0, nodes.Count);
		}

		[Test]
		public void RemoveNode_ExistingNode_DecreasesCount()
		{
			var stateNode = new StateNode();
			_graph.AddNode(stateNode);
			Assert.AreEqual(2, _graph.NodeCount);

			_graph.RemoveNode(stateNode.Id);

			Assert.AreEqual(1, _graph.NodeCount);
		}

		[Test]
		public void RemoveNode_NonExistentNode_IsNoOp()
		{
			var countBefore = _graph.NodeCount;

			_graph.RemoveNode("non-existent-id");

			Assert.AreEqual(countBefore, _graph.NodeCount);
		}

		[Test]
		public void RemoveNode_AlsoRemovesRelatedConnections()
		{
			var state1 = new StateNode();
			var state2 = new StateNode();
			_graph.AddNode(state1);
			_graph.AddNode(state2);

			var connection = new Connection(state1.Id, "output", state2.Id, "input", ConnectionType.Transition);
			_graph.AddConnection(connection);
			Assert.AreEqual(1, _graph.ConnectionCount);

			_graph.RemoveNode(state1.Id);

			Assert.AreEqual(0, _graph.ConnectionCount);
		}

		[Test]
		public void RemoveNode_CleansUpRegionContainment()
		{
			var stateNode = new StateNode();
			_graph.AddNode(stateNode);

			var region = new GraphRegion("Test", new Rect(0, 0, 200, 200));
			region.AddNode(stateNode.Id);
			_graph.AddRegion(region);

			_graph.RemoveNode(stateNode.Id);

			var retrieved = _graph.GetRegion(region.Id);
			Assert.IsFalse(retrieved.ContainsNode(stateNode.Id));
		}
	}

	[TestFixture]
	public class StateMachineGraphConnectionTests
	{
		private StateMachineGraph _graph;
		private StateNode _stateA;
		private StateNode _stateB;
		private StateNode _stateC;

		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			_graph.InitializeNewGraph();

			_stateA = new StateNode();
			_stateB = new StateNode();
			_stateC = new StateNode();
			_graph.AddNode(_stateA);
			_graph.AddNode(_stateB);
			_graph.AddNode(_stateC);
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_graph);
		}

		[Test]
		public void AddConnection_IncreasesConnectionCount()
		{
			var conn = new Connection(_stateA.Id, "out", _stateB.Id, "in");

			_graph.AddConnection(conn);

			Assert.AreEqual(1, _graph.ConnectionCount);
		}

		[Test]
		public void AddConnection_NullConnection_IsNoOp()
		{
			_graph.AddConnection(null);

			Assert.AreEqual(0, _graph.ConnectionCount);
		}

		[Test]
		public void GetConnectionsFromNode_ReturnsMatchingConnections()
		{
			var conn1 = new Connection(_stateA.Id, "out1", _stateB.Id, "in");
			var conn2 = new Connection(_stateA.Id, "out2", _stateC.Id, "in");
			var conn3 = new Connection(_stateB.Id, "out", _stateC.Id, "in");
			_graph.AddConnection(conn1);
			_graph.AddConnection(conn2);
			_graph.AddConnection(conn3);

			var fromA = _graph.GetConnectionsFromNode(_stateA.Id).ToList();

			Assert.AreEqual(2, fromA.Count);
		}

		[Test]
		public void GetConnectionsFromNode_NonExistentNode_ReturnsEmpty()
		{
			var result = _graph.GetConnectionsFromNode("non-existent").ToList();

			Assert.AreEqual(0, result.Count);
		}

		[Test]
		public void GetFirstConnectionFromNode_ReturnsFirstConnection()
		{
			var conn = new Connection(_stateA.Id, "out", _stateB.Id, "in");
			_graph.AddConnection(conn);

			var result = _graph.GetFirstConnectionFromNode(_stateA.Id);

			Assert.IsNotNull(result);
			Assert.AreEqual(conn.Id, result.Id);
		}

		[Test]
		public void GetFirstConnectionFromNode_NoConnections_ReturnsNull()
		{
			var result = _graph.GetFirstConnectionFromNode(_stateA.Id);

			Assert.IsNull(result);
		}

		[Test]
		public void GetConnectionsToNode_ReturnsMatchingConnections()
		{
			var conn1 = new Connection(_stateA.Id, "out", _stateC.Id, "in1");
			var conn2 = new Connection(_stateB.Id, "out", _stateC.Id, "in2");
			_graph.AddConnection(conn1);
			_graph.AddConnection(conn2);

			var toC = _graph.GetConnectionsToNode(_stateC.Id).ToList();

			Assert.AreEqual(2, toC.Count);
		}

		[Test]
		public void GetConnectionToPort_FindsSpecificPort()
		{
			var conn = new Connection(_stateA.Id, "out", _stateB.Id, "myInput");
			_graph.AddConnection(conn);

			var result = _graph.GetConnectionToPort(_stateB.Id, "myInput");

			Assert.IsNotNull(result);
			Assert.AreEqual(conn.Id, result.Id);
		}

		[Test]
		public void GetConnectionToPort_WrongPortName_ReturnsNull()
		{
			var conn = new Connection(_stateA.Id, "out", _stateB.Id, "myInput");
			_graph.AddConnection(conn);

			var result = _graph.GetConnectionToPort(_stateB.Id, "otherInput");

			Assert.IsNull(result);
		}

		[Test]
		public void GetConnectionsFromPort_ReturnsMatchingPort()
		{
			var conn1 = new Connection(_stateA.Id, "portA", _stateB.Id, "in");
			var conn2 = new Connection(_stateA.Id, "portB", _stateC.Id, "in");
			_graph.AddConnection(conn1);
			_graph.AddConnection(conn2);

			var fromPortA = _graph.GetConnectionsFromPort(_stateA.Id, "portA").ToList();

			Assert.AreEqual(1, fromPortA.Count);
			Assert.AreEqual(conn1.Id, fromPortA[0].Id);
		}

		[Test]
		public void IsPortConnected_InputPort_Connected_ReturnsTrue()
		{
			var conn = new Connection(_stateA.Id, "out", _stateB.Id, "myInput");
			_graph.AddConnection(conn);

			Assert.IsTrue(_graph.IsPortConnected(_stateB.Id, "myInput", isInput: true));
		}

		[Test]
		public void IsPortConnected_InputPort_NotConnected_ReturnsFalse()
		{
			Assert.IsFalse(_graph.IsPortConnected(_stateB.Id, "myInput", isInput: true));
		}

		[Test]
		public void IsPortConnected_OutputPort_Connected_ReturnsTrue()
		{
			var conn = new Connection(_stateA.Id, "myOutput", _stateB.Id, "in");
			_graph.AddConnection(conn);

			Assert.IsTrue(_graph.IsPortConnected(_stateA.Id, "myOutput", isInput: false));
		}

		[Test]
		public void IsPortConnected_OutputPort_NotConnected_ReturnsFalse()
		{
			Assert.IsFalse(_graph.IsPortConnected(_stateA.Id, "myOutput", isInput: false));
		}

		[Test]
		public void RemoveConnection_DecreasesCount()
		{
			var conn = new Connection(_stateA.Id, "out", _stateB.Id, "in");
			_graph.AddConnection(conn);
			Assert.AreEqual(1, _graph.ConnectionCount);

			_graph.RemoveConnection(conn.Id);

			Assert.AreEqual(0, _graph.ConnectionCount);
		}

		[Test]
		public void RemoveConnection_NonExistentId_IsNoOp()
		{
			var conn = new Connection(_stateA.Id, "out", _stateB.Id, "in");
			_graph.AddConnection(conn);

			_graph.RemoveConnection("non-existent-id");

			Assert.AreEqual(1, _graph.ConnectionCount);
		}

		[Test]
		public void RemoveConnection_UpdatesSourceNodeIndex()
		{
			var conn = new Connection(_stateA.Id, "out", _stateB.Id, "in");
			_graph.AddConnection(conn);

			_graph.RemoveConnection(conn.Id);

			var fromA = _graph.GetConnectionsFromNode(_stateA.Id).ToList();
			Assert.AreEqual(0, fromA.Count);
		}

		[Test]
		public void RemoveConnection_UpdatesTargetNodeIndex()
		{
			var conn = new Connection(_stateA.Id, "out", _stateB.Id, "in");
			_graph.AddConnection(conn);

			_graph.RemoveConnection(conn.Id);

			var toB = _graph.GetConnectionsToNode(_stateB.Id).ToList();
			Assert.AreEqual(0, toB.Count);
		}

		[Test]
		public void Connections_ReadOnlyList_ReflectsAddedConnections()
		{
			var conn = new Connection(_stateA.Id, "out", _stateB.Id, "in");
			_graph.AddConnection(conn);

			Assert.AreEqual(1, _graph.Connections.Count);
			Assert.AreEqual(conn.Id, _graph.Connections[0].Id);
		}
	}

	[TestFixture]
	public class StateMachineGraphBlackboardTests
	{
		private StateMachineGraph _graph;

		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			_graph.InitializeNewGraph();
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_graph);
		}

		[Test]
		public void AddBlackboardVariable_AddsToDefinitions()
		{
			var def = new BlackboardVariableDefinition
			{
				Key = "health",
				ValueType = typeof(int)
			};

			_graph.AddBlackboardVariable(def);

			Assert.AreEqual(1, _graph.BlackboardDefinitions.Count);
		}

		[Test]
		public void AddBlackboardVariable_NullDefinition_IsNoOp()
		{
			_graph.AddBlackboardVariable(null);

			Assert.AreEqual(0, _graph.BlackboardDefinitions.Count);
		}

		[Test]
		public void AddBlackboardVariable_DuplicateKey_OverwritesExisting()
		{
			var def1 = new BlackboardVariableDefinition
			{
				Key = "score",
				ValueType = typeof(int)
			};
			var def2 = new BlackboardVariableDefinition
			{
				Key = "score",
				ValueType = typeof(float)
			};

			_graph.AddBlackboardVariable(def1);
			_graph.AddBlackboardVariable(def2);

			Assert.AreEqual(1, _graph.BlackboardDefinitions.Count);
			Assert.AreEqual(typeof(float), _graph.GetBlackboardVariable("score").ValueType);
		}

		[Test]
		public void GetBlackboardVariable_ExistingKey_ReturnsDefinition()
		{
			var def = new BlackboardVariableDefinition
			{
				Key = "speed",
				ValueType = typeof(float)
			};
			_graph.AddBlackboardVariable(def);

			var result = _graph.GetBlackboardVariable("speed");

			Assert.IsNotNull(result);
			Assert.AreEqual("speed", result.Key);
		}

		[Test]
		public void GetBlackboardVariable_NonExistentKey_ReturnsNull()
		{
			var result = _graph.GetBlackboardVariable("missing");

			Assert.IsNull(result);
		}

		[Test]
		public void RemoveBlackboardVariable_RemovesFromDefinitions()
		{
			var def = new BlackboardVariableDefinition
			{
				Key = "health",
				ValueType = typeof(int)
			};
			_graph.AddBlackboardVariable(def);

			_graph.RemoveBlackboardVariable("health");

			Assert.AreEqual(0, _graph.BlackboardDefinitions.Count);
			Assert.IsNull(_graph.GetBlackboardVariable("health"));
		}

		[Test]
		public void RemoveBlackboardVariable_NonExistentKey_IsNoOp()
		{
			var def = new BlackboardVariableDefinition
			{
				Key = "health",
				ValueType = typeof(int)
			};
			_graph.AddBlackboardVariable(def);

			_graph.RemoveBlackboardVariable("missing");

			Assert.AreEqual(1, _graph.BlackboardDefinitions.Count);
		}

		[Test]
		public void NotifyBlackboardChanged_FiresStaticEvent()
		{
			StateMachineGraph receivedGraph = null;
			string receivedKey = null;

			StateMachineGraph.OnBlackboardChanged += (g, k) =>
			{
				receivedGraph = g;
				receivedKey = k;
			};

			try
			{
				_graph.NotifyBlackboardChanged("health");

				Assert.AreSame(_graph, receivedGraph);
				Assert.AreEqual("health", receivedKey);
			}
			finally
			{
				// Clean up static event - use reflection since we can't set event to null directly
				var field = typeof(StateMachineGraph).GetField("OnBlackboardChanged",
					System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
				field?.SetValue(null, null);
			}
		}

		[Test]
		public void NotifyBlackboardVariableRenamed_FiresStaticEvent()
		{
			StateMachineGraph receivedGraph = null;
			string receivedOld = null;
			string receivedNew = null;

			StateMachineGraph.OnBlackboardVariableRenamed += (g, o, n) =>
			{
				receivedGraph = g;
				receivedOld = o;
				receivedNew = n;
			};

			try
			{
				_graph.NotifyBlackboardVariableRenamed("oldName", "newName");

				Assert.AreSame(_graph, receivedGraph);
				Assert.AreEqual("oldName", receivedOld);
				Assert.AreEqual("newName", receivedNew);
			}
			finally
			{
				var field = typeof(StateMachineGraph).GetField("OnBlackboardVariableRenamed",
					System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
				field?.SetValue(null, null);
			}
		}
	}

	[TestFixture]
	public class StateMachineGraphRegionTests
	{
		private StateMachineGraph _graph;

		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			_graph.InitializeNewGraph();
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_graph);
		}

		[Test]
		public void AddRegion_AddsToRegionsList()
		{
			var region = new GraphRegion("Test Region", new Rect(0, 0, 300, 200));

			_graph.AddRegion(region);

			Assert.AreEqual(1, _graph.Regions.Count);
		}

		[Test]
		public void AddRegion_NullRegion_IsNoOp()
		{
			_graph.AddRegion(null);

			Assert.AreEqual(0, _graph.Regions.Count);
		}

		[Test]
		public void AddRegion_DuplicateId_OverwritesExisting()
		{
			var region1 = new GraphRegion("First", new Rect(0, 0, 100, 100));
			_graph.AddRegion(region1);

			var region2 = new GraphRegion { Id = region1.Id, Title = "Updated" };
			_graph.AddRegion(region2);

			Assert.AreEqual(1, _graph.Regions.Count);
			Assert.AreEqual("Updated", _graph.GetRegion(region1.Id).Title);
		}

		[Test]
		public void GetRegion_ExistingId_ReturnsRegion()
		{
			var region = new GraphRegion("Test", new Rect(0, 0, 100, 100));
			_graph.AddRegion(region);

			var result = _graph.GetRegion(region.Id);

			Assert.IsNotNull(result);
			Assert.AreEqual("Test", result.Title);
		}

		[Test]
		public void GetRegion_NonExistentId_ReturnsNull()
		{
			var result = _graph.GetRegion("non-existent");

			Assert.IsNull(result);
		}

		[Test]
		public void RemoveRegion_RemovesFromList()
		{
			var region = new GraphRegion("Test", new Rect(0, 0, 100, 100));
			_graph.AddRegion(region);

			_graph.RemoveRegion(region.Id);

			Assert.AreEqual(0, _graph.Regions.Count);
			Assert.IsNull(_graph.GetRegion(region.Id));
		}

		[Test]
		public void RemoveRegion_NonExistentId_IsNoOp()
		{
			var region = new GraphRegion("Test", new Rect(0, 0, 100, 100));
			_graph.AddRegion(region);

			_graph.RemoveRegion("non-existent");

			Assert.AreEqual(1, _graph.Regions.Count);
		}

		[Test]
		public void UpdateRegion_ExistingRegion_UpdatesProperties()
		{
			var region = new GraphRegion("Original", new Rect(0, 0, 100, 100));
			_graph.AddRegion(region);

			region.Title = "Updated";
			_graph.UpdateRegion(region);

			Assert.AreEqual("Updated", _graph.GetRegion(region.Id).Title);
		}

		[Test]
		public void UpdateRegion_NonExistentRegion_IsNoOp()
		{
			var region = new GraphRegion("Ghost", new Rect(0, 0, 100, 100));

			_graph.UpdateRegion(region);

			Assert.IsNull(_graph.GetRegion(region.Id));
		}

		[Test]
		public void UpdateRegion_NullRegion_IsNoOp()
		{
			_graph.UpdateRegion(null);

			Assert.AreEqual(0, _graph.Regions.Count);
		}
	}

	[TestFixture]
	public class StateMachineGraphContextTypeTests
	{
		private StateMachineGraph _graph;

		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			_graph.InitializeNewGraph();
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_graph);
		}

		[Test]
		public void ContextType_DefaultsToNull()
		{
			Assert.IsNull(_graph.ContextType);
		}

		[Test]
		public void RequiresContext_WhenNull_ReturnsFalse()
		{
			Assert.IsFalse(_graph.RequiresContext);
		}

		[Test]
		public void ContextType_SetAndGet_RoundTrips()
		{
			_graph.ContextType = typeof(Transform);

			Assert.AreEqual(typeof(Transform), _graph.ContextType);
		}

		[Test]
		public void RequiresContext_WhenSet_ReturnsTrue()
		{
			_graph.ContextType = typeof(Transform);

			Assert.IsTrue(_graph.RequiresContext);
		}

		[Test]
		public void ContextType_SetsAssemblyQualifiedName()
		{
			_graph.ContextType = typeof(Transform);

			Assert.AreEqual(typeof(Transform).AssemblyQualifiedName, _graph.ContextTypeAssemblyQualifiedName);
		}

		[Test]
		public void ContextType_SetToNull_ClearsAssemblyQualifiedName()
		{
			_graph.ContextType = typeof(Transform);
			_graph.ContextType = null;

			Assert.IsNull(_graph.ContextTypeAssemblyQualifiedName);
			Assert.IsNull(_graph.ContextType);
		}

		[Test]
		public void IsVariant_DefaultsToFalse()
		{
			Assert.IsFalse(_graph.IsVariant);
		}
	}

	[TestFixture]
	public class StateMachineGraphSerializationTests
	{
		private StateMachineGraph _graph;

		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			_graph.InitializeNewGraph();
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_graph);
		}

		[Test]
		public void SerializationRoundTrip_PreservesNodes()
		{
			var stateNode = new StateNode();
			stateNode.DisplayName = "TestState";
			_graph.AddNode(stateNode);

			_graph.OnBeforeSerialize();
			_graph.OnAfterDeserialize();

			Assert.AreEqual(2, _graph.NodeCount);
			var retrieved = _graph.GetNode(stateNode.Id);
			Assert.IsNotNull(retrieved);
		}

		[Test]
		public void SerializationRoundTrip_PreservesConnections()
		{
			var state1 = new StateNode();
			var state2 = new StateNode();
			_graph.AddNode(state1);
			_graph.AddNode(state2);

			var conn = new Connection(state1.Id, "out", state2.Id, "in");
			_graph.AddConnection(conn);

			_graph.OnBeforeSerialize();
			_graph.OnAfterDeserialize();

			Assert.AreEqual(1, _graph.ConnectionCount);
			var fromState1 = _graph.GetConnectionsFromNode(state1.Id).ToList();
			Assert.AreEqual(1, fromState1.Count);
		}

		[Test]
		public void SerializationRoundTrip_PreservesConnectionIndices()
		{
			var state1 = new StateNode();
			var state2 = new StateNode();
			_graph.AddNode(state1);
			_graph.AddNode(state2);

			var conn = new Connection(state1.Id, "out", state2.Id, "in");
			_graph.AddConnection(conn);

			_graph.OnBeforeSerialize();
			_graph.OnAfterDeserialize();

			var toState2 = _graph.GetConnectionsToNode(state2.Id).ToList();
			Assert.AreEqual(1, toState2.Count);
		}

		[Test]
		public void SerializationRoundTrip_PreservesBlackboardDefinitions()
		{
			var def = new BlackboardVariableDefinition
			{
				Key = "health",
				ValueType = typeof(int)
			};
			_graph.AddBlackboardVariable(def);

			_graph.OnBeforeSerialize();
			_graph.OnAfterDeserialize();

			Assert.AreEqual(1, _graph.BlackboardDefinitions.Count);
			var retrieved = _graph.GetBlackboardVariable("health");
			Assert.IsNotNull(retrieved);
		}

		[Test]
		public void SerializationRoundTrip_PreservesRegions()
		{
			var region = new GraphRegion("TestGroup", new Rect(10, 20, 300, 200));
			_graph.AddRegion(region);

			_graph.OnBeforeSerialize();
			_graph.OnAfterDeserialize();

			Assert.AreEqual(1, _graph.Regions.Count);
			var retrieved = _graph.GetRegion(region.Id);
			Assert.IsNotNull(retrieved);
			Assert.AreEqual("TestGroup", retrieved.Title);
		}

		[Test]
		public void SerializationRoundTrip_PreservesEntryNodeId()
		{
			var entryId = _graph.EntryNodeId;

			_graph.OnBeforeSerialize();
			_graph.OnAfterDeserialize();

			Assert.AreEqual(entryId, _graph.EntryNodeId);
		}

		[Test]
		public void SerializationRoundTrip_PreservesContextType()
		{
			_graph.ContextType = typeof(Transform);

			_graph.OnBeforeSerialize();
			_graph.OnAfterDeserialize();

			Assert.AreEqual(typeof(Transform), _graph.ContextType);
		}

		[Test]
		public void OnAfterDeserialize_ClearsAndRebuilds()
		{
			var state = new StateNode();
			_graph.AddNode(state);
			var conn = new Connection(_graph.EntryNodeId, "out", state.Id, "in");
			_graph.AddConnection(conn);

			_graph.OnBeforeSerialize();
			_graph.OnAfterDeserialize();

			Assert.IsNotNull(_graph.GetNode(state.Id));
			Assert.AreEqual(1, _graph.GetConnectionsToNode(state.Id).Count());
		}
	}
}
