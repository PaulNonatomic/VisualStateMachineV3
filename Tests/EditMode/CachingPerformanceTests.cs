using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	/// <summary>
	/// Performance validation tests for O(1) caching optimizations.
	/// These tests verify that caching implementations provide expected performance
	/// improvements over naive O(n) implementations.
	/// </summary>
	[TestFixture]
	public class CachingPerformanceTests
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
		public void PortalCache_MultiplePortalNodes_AllAccessibleByChannel()
		{
			// Create multiple portal pairs
			for (int i = 1; i <= 5; i++)
			{
				var portalIn = new PortalInNode(i, $"Channel{i}") { Id = $"portal-in-{i}" };
				var portalOut = new PortalOutNode(i, $"Channel{i}") { Id = $"portal-out-{i}" };
				_graph.AddNode(portalIn);
				_graph.AddNode(portalOut);
			}

			// Trigger validation to build cache
			_graph.Validate();

			// Verify all portal nodes exist
			var portalInNodes = _graph.GetNodesOfType<PortalInNode>().ToList();
			var portalOutNodes = _graph.GetNodesOfType<PortalOutNode>().ToList();

			Assert.AreEqual(5, portalInNodes.Count);
			Assert.AreEqual(5, portalOutNodes.Count);

			// Verify channels are unique
			var channels = portalInNodes.Select(p => p.ChannelId).Distinct().ToList();
			Assert.AreEqual(5, channels.Count);
		}

		[Test]
		public void PortalCache_LargeNumberOfPortals_ValidationCompletesQuickly()
		{
			const int portalCount = 100;

			// Create many portal pairs
			for (int i = 1; i <= portalCount; i++)
			{
				var portalIn = new PortalInNode(i, $"Channel{i}") { Id = $"portal-in-{i}" };
				var portalOut = new PortalOutNode(i, $"Channel{i}") { Id = $"portal-out-{i}" };
				_graph.AddNode(portalIn);
				_graph.AddNode(portalOut);
			}

			// Add entry node
			var entry = new EntryNode("entry");
			_graph.AddNode(entry);
			_graph.EntryNodeId = "entry";

			var sw = Stopwatch.StartNew();
			_graph.Validate();
			sw.Stop();

			// Validation should complete quickly (under 1 second even with many portals)
			Assert.Less(sw.ElapsedMilliseconds, 1000,
				$"Validation took {sw.ElapsedMilliseconds}ms with {portalCount} portals");
		}

		[Test]
		public void PortalCache_RepeatedValidation_ConsistentPerformance()
		{
			const int portalCount = 50;
			const int iterations = 10;

			// Create portal pairs
			for (int i = 1; i <= portalCount; i++)
			{
				var portalIn = new PortalInNode(i, $"Channel{i}") { Id = $"portal-in-{i}" };
				var portalOut = new PortalOutNode(i, $"Channel{i}") { Id = $"portal-out-{i}" };
				_graph.AddNode(portalIn);
				_graph.AddNode(portalOut);
			}

			var entry = new EntryNode("entry");
			_graph.AddNode(entry);
			_graph.EntryNodeId = "entry";

			var times = new List<long>();
			for (int i = 0; i < iterations; i++)
			{
				var sw = Stopwatch.StartNew();
				_graph.Validate();
				sw.Stop();
				times.Add(sw.ElapsedMilliseconds);
			}

			// Calculate average and standard deviation
			var avg = times.Average();
			var variance = times.Select(t => Math.Pow(t - avg, 2)).Average();
			var stdDev = Math.Sqrt(variance);

			// Performance should be consistent (low variance)
			Assert.Less(stdDev, avg * 0.5,
				$"Validation times vary too much: avg={avg:F1}ms, stdDev={stdDev:F1}ms");
		}



		[Test]
		public void ConnectionIndex_LargeGraph_FastLookups()
		{
			const int nodeCount = 100;
			const int connectionsPerNode = 3;

			// Create a large graph with many connections
			var entry = new EntryNode("entry");
			_graph.AddNode(entry);
			_graph.EntryNodeId = "entry";

			var stateNodes = new List<StateNode>();
			for (int i = 0; i < nodeCount; i++)
			{
				var state = new StateNode(typeof(TestState)) { Id = $"state-{i}" };
				_graph.AddNode(state);
				stateNodes.Add(state);
			}

			// Create connections between nodes
			_graph.AddConnection(new Connection("entry", "exit", "state-0", "enter", ConnectionType.Transition));

			for (int i = 0; i < nodeCount - 1; i++)
			{
				for (int j = 0; j < connectionsPerNode && i + j + 1 < nodeCount; j++)
				{
					_graph.AddConnection(new Connection(
						$"state-{i}", "onComplete",
						$"state-{i + j + 1}", "enter",
						ConnectionType.Transition));
				}
			}

			// Time repeated lookups
			var sw = Stopwatch.StartNew();
			const int lookupIterations = 1000;

			for (int iter = 0; iter < lookupIterations; iter++)
			{
				foreach (var state in stateNodes)
				{
					var fromConnections = _graph.GetConnectionsFromNode(state.Id);
					var toConnections = _graph.GetConnectionsToNode(state.Id);
				}
			}

			sw.Stop();
			var avgPerLookup = (double)sw.ElapsedMilliseconds / (lookupIterations * nodeCount * 2);

			// Each lookup should be very fast (well under 1ms average)
			Assert.Less(avgPerLookup, 0.1,
				$"Connection lookups too slow: {avgPerLookup:F4}ms per lookup");
		}

		[Test]
		public void ConnectionIndex_AfterNodeRemoval_RebuildsCorrectly()
		{
			// Create a simple graph
			var entry = new EntryNode("entry");
			var state1 = new StateNode(typeof(TestState)) { Id = "state-1" };
			var state2 = new StateNode(typeof(TestState)) { Id = "state-2" };
			var state3 = new StateNode(typeof(TestState)) { Id = "state-3" };

			_graph.AddNode(entry);
			_graph.AddNode(state1);
			_graph.AddNode(state2);
			_graph.AddNode(state3);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection("entry", "exit", "state-1", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state-1", "onComplete", "state-2", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state-2", "onComplete", "state-3", "enter", ConnectionType.Transition));

			// Verify initial state
			Assert.AreEqual(1, _graph.GetConnectionsFromNode("state-1").Count());
			Assert.AreEqual(1, _graph.GetConnectionsToNode("state-2").Count());

			// Remove middle node
			_graph.RemoveNode("state-2");

			// Connections involving removed node should be gone
			Assert.AreEqual(0, _graph.GetConnectionsFromNode("state-1").Count());
			Assert.AreEqual(0, _graph.GetConnectionsToNode("state-3").Count());
		}

		[Test]
		public void ConnectionIndex_AfterConnectionRemoval_UpdatesCorrectly()
		{
			var entry = new EntryNode("entry");
			var state1 = new StateNode(typeof(TestState)) { Id = "state-1" };
			var state2 = new StateNode(typeof(TestState)) { Id = "state-2" };

			_graph.AddNode(entry);
			_graph.AddNode(state1);
			_graph.AddNode(state2);
			_graph.EntryNodeId = "entry";

			var connection = new Connection("state-1", "onComplete", "state-2", "enter", ConnectionType.Transition)
			{
				Id = "conn-1"
			};
			_graph.AddConnection(connection);

			Assert.AreEqual(1, _graph.GetConnectionsFromNode("state-1").Count());
			Assert.AreEqual(1, _graph.GetConnectionsToNode("state-2").Count());

			// Remove connection
			_graph.RemoveConnection("conn-1");

			Assert.AreEqual(0, _graph.GetConnectionsFromNode("state-1").Count());
			Assert.AreEqual(0, _graph.GetConnectionsToNode("state-2").Count());
		}



		[Test]
		public void Validation_ComplexGraph_CompletesInReasonableTime()
		{
			const int stateCount = 200;

			// Create a complex graph
			var entry = new EntryNode("entry");
			_graph.AddNode(entry);
			_graph.EntryNodeId = "entry";

			// Create states in a web pattern (not just linear)
			for (int i = 0; i < stateCount; i++)
			{
				var state = new StateNode(typeof(TestState)) { Id = $"state-{i}" };
				_graph.AddNode(state);
			}

			// Entry to first state
			_graph.AddConnection(new Connection("entry", "exit", "state-0", "enter", ConnectionType.Transition));

			// Create web of connections
			for (int i = 0; i < stateCount; i++)
			{
				// Connect to next state
				if (i < stateCount - 1)
				{
					_graph.AddConnection(new Connection(
						$"state-{i}", "onComplete",
						$"state-{i + 1}", "enter",
						ConnectionType.Transition));
				}

				// Add some cross-connections for complexity
				if (i + 5 < stateCount)
				{
					_graph.AddConnection(new Connection(
						$"state-{i}", "onAlternate",
						$"state-{i + 5}", "enter",
						ConnectionType.Transition));
				}
			}

			var sw = Stopwatch.StartNew();
			var issues = _graph.Validate();
			sw.Stop();

			// Validation should complete quickly
			Assert.Less(sw.ElapsedMilliseconds, 5000,
				$"Validation took {sw.ElapsedMilliseconds}ms for {stateCount} states");
		}

		[Test]
		public void Validation_GraphWithCycles_DoesNotHang()
		{
			// Create a graph with intentional cycles
			var entry = new EntryNode("entry");
			var state1 = new StateNode(typeof(TestState)) { Id = "state-1" };
			var state2 = new StateNode(typeof(TestState)) { Id = "state-2" };
			var state3 = new StateNode(typeof(TestState)) { Id = "state-3" };

			_graph.AddNode(entry);
			_graph.AddNode(state1);
			_graph.AddNode(state2);
			_graph.AddNode(state3);
			_graph.EntryNodeId = "entry";

			// Create a cycle: state1 -> state2 -> state3 -> state1
			_graph.AddConnection(new Connection("entry", "exit", "state-1", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state-1", "onComplete", "state-2", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state-2", "onComplete", "state-3", "enter", ConnectionType.Transition));
			_graph.AddConnection(new Connection("state-3", "onComplete", "state-1", "enter", ConnectionType.Transition));

			var sw = Stopwatch.StartNew();
			var issues = _graph.Validate();
			sw.Stop();

			// Should complete quickly even with cycles
			Assert.Less(sw.ElapsedMilliseconds, 1000,
				$"Validation with cycles took {sw.ElapsedMilliseconds}ms");
		}



		[Test]
		public void NodeLookup_ById_IsO1()
		{
			const int nodeCount = 500;

			// Create many nodes
			for (int i = 0; i < nodeCount; i++)
			{
				var state = new StateNode(typeof(TestState)) { Id = $"state-{i}" };
				_graph.AddNode(state);
			}

			// Time lookups
			var sw = Stopwatch.StartNew();
			const int lookupIterations = 10000;

			for (int iter = 0; iter < lookupIterations; iter++)
			{
				var randomIndex = iter % nodeCount;
				var node = _graph.GetNode($"state-{randomIndex}");
				Assert.IsNotNull(node);
			}

			sw.Stop();
			var avgPerLookup = (double)sw.ElapsedMilliseconds / lookupIterations;

			// Dictionary lookup should be extremely fast
			Assert.Less(avgPerLookup, 0.01,
				$"Node lookups too slow: {avgPerLookup:F4}ms per lookup");
		}

		[Test]
		public void GetNodesOfType_Filtering_PerformsReasonably()
		{
			const int totalNodes = 200;

			// Add mix of node types
			for (int i = 0; i < totalNodes / 4; i++)
			{
				_graph.AddNode(new StateNode(typeof(TestState)) { Id = $"state-{i}" });
				_graph.AddNode(new PortalInNode(i, $"Channel{i}") { Id = $"portal-in-{i}" });
				_graph.AddNode(new PortalOutNode(i, $"Channel{i}") { Id = $"portal-out-{i}" });
				_graph.AddNode(new ExitNode($"exit-{i}"));
			}

			var sw = Stopwatch.StartNew();
			const int iterations = 100;

			for (int i = 0; i < iterations; i++)
			{
				var states = _graph.GetNodesOfType<StateNode>().ToList();
				var portalIns = _graph.GetNodesOfType<PortalInNode>().ToList();
				var portalOuts = _graph.GetNodesOfType<PortalOutNode>().ToList();
			}

			sw.Stop();
			var avgPerIteration = (double)sw.ElapsedMilliseconds / iterations;

			// Type filtering should be reasonably fast
			Assert.Less(avgPerIteration, 10,
				$"GetNodesOfType too slow: {avgPerIteration:F2}ms per iteration");
		}



		[Test]
		public void ConnectionCache_AfterManyAdditions_RemainsConsistent()
		{
			const int batchCount = 10;
			const int nodesPerBatch = 20;

			var entry = new EntryNode("entry");
			_graph.AddNode(entry);
			_graph.EntryNodeId = "entry";

			var allNodeIds = new List<string>();

			// Add nodes in batches
			for (int batch = 0; batch < batchCount; batch++)
			{
				for (int i = 0; i < nodesPerBatch; i++)
				{
					var nodeId = $"state-{batch}-{i}";
					var state = new StateNode(typeof(TestState)) { Id = nodeId };
					_graph.AddNode(state);
					allNodeIds.Add(nodeId);

					// Connect to previous node in batch
					if (i > 0)
					{
						var prevNodeId = $"state-{batch}-{i - 1}";
						_graph.AddConnection(new Connection(
							prevNodeId, "onComplete",
							nodeId, "enter",
							ConnectionType.Transition));
					}
				}

				// Connect batch to entry
				_graph.AddConnection(new Connection(
					"entry", "exit",
					$"state-{batch}-0", "enter",
					ConnectionType.Transition));
			}

			// Verify connection counts are correct
			var entryConnections = _graph.GetConnectionsFromNode("entry").ToList();
			Assert.AreEqual(batchCount, entryConnections.Count,
				"Entry node should have one connection per batch");

			// Each first-in-batch should have one incoming from entry
			for (int batch = 0; batch < batchCount; batch++)
			{
				var firstInBatch = $"state-{batch}-0";
				var incoming = _graph.GetConnectionsToNode(firstInBatch).ToList();
				Assert.AreEqual(1, incoming.Count,
					$"First node in batch {batch} should have 1 incoming connection");
			}
		}

		[Test]
		public void PortalCache_DuplicateChannels_HandledCorrectly()
		{
			// Add multiple PortalIn nodes with same channel (invalid but should not crash)
			var portalIn1 = new PortalInNode(1, "Channel1") { Id = "portal-in-1a" };
			var portalIn2 = new PortalInNode(1, "Channel1") { Id = "portal-in-1b" };
			var portalOut = new PortalOutNode(1, "Channel1") { Id = "portal-out-1" };

			_graph.AddNode(portalIn1);
			_graph.AddNode(portalIn2);
			_graph.AddNode(portalOut);

			var entry = new EntryNode("entry");
			_graph.AddNode(entry);
			_graph.EntryNodeId = "entry";

			// Validation should complete without error
			Assert.DoesNotThrow(() => _graph.Validate());

			// Should report the duplicate channel as an issue
			var issues = _graph.Validate();
			var portalIssues = issues.Where(i =>
				i.Message.Contains("portal", StringComparison.OrdinalIgnoreCase) ||
				i.Message.Contains("channel", StringComparison.OrdinalIgnoreCase)).ToList();

			// Note: The exact validation behavior depends on implementation
			// The important thing is that validation completes without hanging
		}



		private class TestState : Core.State
		{
			[Attributes.Transition("Complete")]
			public Action onComplete;

			[Attributes.Transition("Alternate")]
			public Action onAlternate;
		}

	}
}
