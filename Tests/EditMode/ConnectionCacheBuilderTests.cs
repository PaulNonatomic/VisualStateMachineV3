using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Nonatomic.VSM.Editor.Layout;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class ConnectionCacheBuilderTests
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
		public void Build_EmptyGraph_ReturnsEmptyEntriesForDefaultNodes()
		{
			// Graph has an auto-created entry node from Reset()
			var cache = ConnectionCacheBuilder.Build(_graph, isOutgoing: true);

			Assert.IsNotNull(cache);
			foreach (var entry in cache)
			{
				Assert.IsEmpty(entry.Value, $"Node {entry.Key} should have no connections");
			}
		}

		[Test]
		public void Build_Outgoing_IndexesBySourceNodeId()
		{
			var nodeA = new StateNode { Id = "nodeA", DisplayName = "A" };
			var nodeB = new StateNode { Id = "nodeB", DisplayName = "B" };
			_graph.AddNode(nodeA);
			_graph.AddNode(nodeB);

			var connection = new Connection("nodeA", "OnComplete", "nodeB", "OnEnter");
			_graph.AddConnection(connection);

			var cache = ConnectionCacheBuilder.Build(_graph, isOutgoing: true);

			Assert.AreEqual(1, cache["nodeA"].Count);
			Assert.AreEqual(0, cache["nodeB"].Count);
			Assert.AreSame(connection, cache["nodeA"][0]);
		}

		[Test]
		public void Build_Incoming_IndexesByTargetNodeId()
		{
			var nodeA = new StateNode { Id = "nodeA", DisplayName = "A" };
			var nodeB = new StateNode { Id = "nodeB", DisplayName = "B" };
			_graph.AddNode(nodeA);
			_graph.AddNode(nodeB);

			var connection = new Connection("nodeA", "OnComplete", "nodeB", "OnEnter");
			_graph.AddConnection(connection);

			var cache = ConnectionCacheBuilder.Build(_graph, isOutgoing: false);

			Assert.AreEqual(0, cache["nodeA"].Count);
			Assert.AreEqual(1, cache["nodeB"].Count);
			Assert.AreSame(connection, cache["nodeB"][0]);
		}

		[Test]
		public void Build_MultipleConnectionsFromSameNode_GroupedTogether()
		{
			var nodeA = new StateNode { Id = "nodeA", DisplayName = "A" };
			var nodeB = new StateNode { Id = "nodeB", DisplayName = "B" };
			var nodeC = new StateNode { Id = "nodeC", DisplayName = "C" };
			_graph.AddNode(nodeA);
			_graph.AddNode(nodeB);
			_graph.AddNode(nodeC);

			var conn1 = new Connection("nodeA", "OnComplete", "nodeB", "OnEnter");
			var conn2 = new Connection("nodeA", "OnFail", "nodeC", "OnEnter");
			_graph.AddConnection(conn1);
			_graph.AddConnection(conn2);

			var cache = ConnectionCacheBuilder.Build(_graph, isOutgoing: true);

			Assert.AreEqual(2, cache["nodeA"].Count);
			Assert.Contains(conn1, cache["nodeA"]);
			Assert.Contains(conn2, cache["nodeA"]);
		}

		[Test]
		public void Build_MultipleConnectionsToSameNode_GroupedTogether()
		{
			var nodeA = new StateNode { Id = "nodeA", DisplayName = "A" };
			var nodeB = new StateNode { Id = "nodeB", DisplayName = "B" };
			var nodeC = new StateNode { Id = "nodeC", DisplayName = "C" };
			_graph.AddNode(nodeA);
			_graph.AddNode(nodeB);
			_graph.AddNode(nodeC);

			var conn1 = new Connection("nodeA", "OnComplete", "nodeC", "OnEnter");
			var conn2 = new Connection("nodeB", "OnComplete", "nodeC", "OnEnter");
			_graph.AddConnection(conn1);
			_graph.AddConnection(conn2);

			var cache = ConnectionCacheBuilder.Build(_graph, isOutgoing: false);

			Assert.AreEqual(2, cache["nodeC"].Count);
			Assert.Contains(conn1, cache["nodeC"]);
			Assert.Contains(conn2, cache["nodeC"]);
		}

		[Test]
		public void Build_AllNodesHaveCacheEntries_EvenWithNoConnections()
		{
			var nodeA = new StateNode { Id = "nodeA", DisplayName = "A" };
			var nodeB = new StateNode { Id = "nodeB", DisplayName = "B" };
			var nodeC = new StateNode { Id = "nodeC", DisplayName = "C" };
			_graph.AddNode(nodeA);
			_graph.AddNode(nodeB);
			_graph.AddNode(nodeC);

			var cache = ConnectionCacheBuilder.Build(_graph, isOutgoing: true);

			Assert.IsTrue(cache.ContainsKey("nodeA"));
			Assert.IsTrue(cache.ContainsKey("nodeB"));
			Assert.IsTrue(cache.ContainsKey("nodeC"));
			Assert.AreEqual(0, cache["nodeA"].Count);
			Assert.AreEqual(0, cache["nodeB"].Count);
			Assert.AreEqual(0, cache["nodeC"].Count);
		}

		[Test]
		public void Build_ChainTopology_CorrectOutgoingCache()
		{
			// A -> B -> C
			var nodeA = new StateNode { Id = "nodeA", DisplayName = "A" };
			var nodeB = new StateNode { Id = "nodeB", DisplayName = "B" };
			var nodeC = new StateNode { Id = "nodeC", DisplayName = "C" };
			_graph.AddNode(nodeA);
			_graph.AddNode(nodeB);
			_graph.AddNode(nodeC);

			var connAB = new Connection("nodeA", "OnComplete", "nodeB", "OnEnter");
			var connBC = new Connection("nodeB", "OnComplete", "nodeC", "OnEnter");
			_graph.AddConnection(connAB);
			_graph.AddConnection(connBC);

			var cache = ConnectionCacheBuilder.Build(_graph, isOutgoing: true);

			Assert.AreEqual(1, cache["nodeA"].Count);
			Assert.AreEqual(1, cache["nodeB"].Count);
			Assert.AreEqual(0, cache["nodeC"].Count);
		}

		[Test]
		public void Build_ChainTopology_CorrectIncomingCache()
		{
			// A -> B -> C
			var nodeA = new StateNode { Id = "nodeA", DisplayName = "A" };
			var nodeB = new StateNode { Id = "nodeB", DisplayName = "B" };
			var nodeC = new StateNode { Id = "nodeC", DisplayName = "C" };
			_graph.AddNode(nodeA);
			_graph.AddNode(nodeB);
			_graph.AddNode(nodeC);

			var connAB = new Connection("nodeA", "OnComplete", "nodeB", "OnEnter");
			var connBC = new Connection("nodeB", "OnComplete", "nodeC", "OnEnter");
			_graph.AddConnection(connAB);
			_graph.AddConnection(connBC);

			var cache = ConnectionCacheBuilder.Build(_graph, isOutgoing: false);

			Assert.AreEqual(0, cache["nodeA"].Count);
			Assert.AreEqual(1, cache["nodeB"].Count);
			Assert.AreEqual(1, cache["nodeC"].Count);
		}

		[Test]
		public void Build_SelfLoop_AppearsInCorrectCache()
		{
			var nodeA = new StateNode { Id = "nodeA", DisplayName = "A" };
			_graph.AddNode(nodeA);

			var selfConn = new Connection("nodeA", "OnRetry", "nodeA", "OnEnter");
			_graph.AddConnection(selfConn);

			var outgoing = ConnectionCacheBuilder.Build(_graph, isOutgoing: true);
			var incoming = ConnectionCacheBuilder.Build(_graph, isOutgoing: false);

			Assert.AreEqual(1, outgoing["nodeA"].Count);
			Assert.AreEqual(1, incoming["nodeA"].Count);
			Assert.AreSame(selfConn, outgoing["nodeA"][0]);
			Assert.AreSame(selfConn, incoming["nodeA"][0]);
		}

		[Test]
		public void Build_DataConnections_IncludedInCache()
		{
			var nodeA = new StateNode { Id = "nodeA", DisplayName = "A" };
			var nodeB = new StateNode { Id = "nodeB", DisplayName = "B" };
			_graph.AddNode(nodeA);
			_graph.AddNode(nodeB);

			var dataConn = new Connection("nodeA", "OutputValue", "nodeB", "InputValue", ConnectionType.Data);
			_graph.AddConnection(dataConn);

			var cache = ConnectionCacheBuilder.Build(_graph, isOutgoing: true);

			Assert.AreEqual(1, cache["nodeA"].Count);
			Assert.AreEqual(ConnectionType.Data, cache["nodeA"][0].ConnectionType);
		}

		[Test]
		public void Build_MixedConnectionTypes_AllIncluded()
		{
			var nodeA = new StateNode { Id = "nodeA", DisplayName = "A" };
			var nodeB = new StateNode { Id = "nodeB", DisplayName = "B" };
			_graph.AddNode(nodeA);
			_graph.AddNode(nodeB);

			var transConn = new Connection("nodeA", "OnComplete", "nodeB", "OnEnter", ConnectionType.Transition);
			var dataConn = new Connection("nodeA", "OutputValue", "nodeB", "InputValue", ConnectionType.Data);
			_graph.AddConnection(transConn);
			_graph.AddConnection(dataConn);

			var cache = ConnectionCacheBuilder.Build(_graph, isOutgoing: true);

			Assert.AreEqual(2, cache["nodeA"].Count);
		}

		[Test]
		public void Build_OutgoingAndIncoming_ReturnDifferentResults()
		{
			var nodeA = new StateNode { Id = "nodeA", DisplayName = "A" };
			var nodeB = new StateNode { Id = "nodeB", DisplayName = "B" };
			_graph.AddNode(nodeA);
			_graph.AddNode(nodeB);

			var connection = new Connection("nodeA", "OnComplete", "nodeB", "OnEnter");
			_graph.AddConnection(connection);

			var outgoing = ConnectionCacheBuilder.Build(_graph, isOutgoing: true);
			var incoming = ConnectionCacheBuilder.Build(_graph, isOutgoing: false);

			Assert.AreEqual(1, outgoing["nodeA"].Count);
			Assert.AreEqual(0, outgoing["nodeB"].Count);
			Assert.AreEqual(0, incoming["nodeA"].Count);
			Assert.AreEqual(1, incoming["nodeB"].Count);
		}

		[Test]
		public void Build_FanOutTopology_AllConnectionsFromSourceGrouped()
		{
			// A -> B, A -> C, A -> D
			var nodeA = new StateNode { Id = "nodeA", DisplayName = "A" };
			var nodeB = new StateNode { Id = "nodeB", DisplayName = "B" };
			var nodeC = new StateNode { Id = "nodeC", DisplayName = "C" };
			var nodeD = new StateNode { Id = "nodeD", DisplayName = "D" };
			_graph.AddNode(nodeA);
			_graph.AddNode(nodeB);
			_graph.AddNode(nodeC);
			_graph.AddNode(nodeD);

			_graph.AddConnection(new Connection("nodeA", "OnBranch1", "nodeB", "OnEnter"));
			_graph.AddConnection(new Connection("nodeA", "OnBranch2", "nodeC", "OnEnter"));
			_graph.AddConnection(new Connection("nodeA", "OnBranch3", "nodeD", "OnEnter"));

			var cache = ConnectionCacheBuilder.Build(_graph, isOutgoing: true);

			Assert.AreEqual(3, cache["nodeA"].Count);
			Assert.AreEqual(0, cache["nodeB"].Count);
			Assert.AreEqual(0, cache["nodeC"].Count);
			Assert.AreEqual(0, cache["nodeD"].Count);
		}

		[Test]
		public void Build_FanInTopology_AllConnectionsToTargetGrouped()
		{
			// B -> A, C -> A, D -> A
			var nodeA = new StateNode { Id = "nodeA", DisplayName = "A" };
			var nodeB = new StateNode { Id = "nodeB", DisplayName = "B" };
			var nodeC = new StateNode { Id = "nodeC", DisplayName = "C" };
			var nodeD = new StateNode { Id = "nodeD", DisplayName = "D" };
			_graph.AddNode(nodeA);
			_graph.AddNode(nodeB);
			_graph.AddNode(nodeC);
			_graph.AddNode(nodeD);

			_graph.AddConnection(new Connection("nodeB", "OnComplete", "nodeA", "OnEnter"));
			_graph.AddConnection(new Connection("nodeC", "OnComplete", "nodeA", "OnEnter"));
			_graph.AddConnection(new Connection("nodeD", "OnComplete", "nodeA", "OnEnter"));

			var cache = ConnectionCacheBuilder.Build(_graph, isOutgoing: false);

			Assert.AreEqual(3, cache["nodeA"].Count);
			Assert.AreEqual(0, cache["nodeB"].Count);
			Assert.AreEqual(0, cache["nodeC"].Count);
			Assert.AreEqual(0, cache["nodeD"].Count);
		}
	}
}
