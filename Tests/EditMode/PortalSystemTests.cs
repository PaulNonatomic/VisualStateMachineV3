using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class PortalNodePairingTests
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
		public void PortalInAndOut_SameChannel_HaveSameColor()
		{
			var portalIn = new PortalInNode(5);
			var portalOut = new PortalOutNode(5);

			Assert.AreEqual(portalIn.ChannelColor, portalOut.ChannelColor);
		}

		[Test]
		public void PortalInAndOut_DifferentChannels_HaveDifferentColors()
		{
			var portalIn = new PortalInNode(1);
			var portalOut = new PortalOutNode(2);

			Assert.AreNotEqual(portalIn.ChannelColor, portalOut.ChannelColor);
		}

		[Test]
		public void PortalInAndOut_SameChannelDifferentLabels_SameColor()
		{
			var portalIn = new PortalInNode(3, "Entry Point");
			var portalOut = new PortalOutNode(3, "Exit Point");

			Assert.AreEqual(portalIn.ChannelColor, portalOut.ChannelColor);
			Assert.AreNotEqual(portalIn.ChannelLabel, portalOut.ChannelLabel);
		}

		[Test]
		public void PortalInAndOut_Channel0_MatchCorrectly()
		{
			var portalIn = new PortalInNode(0);
			var portalOut = new PortalOutNode(0);

			Assert.AreEqual(portalIn.ChannelId, portalOut.ChannelId);
			Assert.AreEqual(portalIn.ChannelColor, portalOut.ChannelColor);
		}

		[Test]
		public void PortalInAndOut_HighChannel_ColorCycles()
		{
			var portalIn = new PortalInNode(16);
			var portalOut = new PortalOutNode(0);

			// Channel 16 cycles to same color as channel 0
			Assert.AreEqual(portalIn.ChannelColor, portalOut.ChannelColor);
		}

		[Test]
		public void ChannelReassignment_UpdatesPairing()
		{
			var portalIn = new PortalInNode(1);
			var portalOut = new PortalOutNode(1);

			Assert.AreEqual(portalIn.ChannelId, portalOut.ChannelId);

			portalOut.ChannelId = 2;

			Assert.AreNotEqual(portalIn.ChannelId, portalOut.ChannelId);
			Assert.AreNotEqual(portalIn.ChannelColor, portalOut.ChannelColor);
		}



		[Test]
		public void AddPortalNodesToGraph_CanQueryByType()
		{
			var portalIn1 = new PortalInNode(1) { Id = "in1" };
			var portalIn2 = new PortalInNode(2) { Id = "in2" };
			var portalOut1 = new PortalOutNode(1) { Id = "out1" };
			var portalOut2 = new PortalOutNode(1) { Id = "out2" };
			var portalOut3 = new PortalOutNode(2) { Id = "out3" };

			_graph.AddNode(portalIn1);
			_graph.AddNode(portalIn2);
			_graph.AddNode(portalOut1);
			_graph.AddNode(portalOut2);
			_graph.AddNode(portalOut3);

			var portalIns = _graph.GetNodesOfType<PortalInNode>();
			var portalOuts = _graph.GetNodesOfType<PortalOutNode>();

			Assert.AreEqual(2, portalIns.Count());
			Assert.AreEqual(3, portalOuts.Count());
		}

		[Test]
		public void PortalNodes_CanFindMatchingChannels()
		{
			var portalIn = new PortalInNode(5) { Id = "in5" };
			var portalOut1 = new PortalOutNode(5) { Id = "out5a" };
			var portalOut2 = new PortalOutNode(5) { Id = "out5b" };
			var portalOutOther = new PortalOutNode(3) { Id = "out3" };

			_graph.AddNode(portalIn);
			_graph.AddNode(portalOut1);
			_graph.AddNode(portalOut2);
			_graph.AddNode(portalOutOther);

			var matchingOuts = _graph.GetNodesOfType<PortalOutNode>()
				.Where(p => p.ChannelId == portalIn.ChannelId);

			Assert.AreEqual(2, matchingOuts.Count());
		}

		[Test]
		public void EmptyGraph_PortalQuery_ReturnsEmpty()
		{
			var portalIns = _graph.GetNodesOfType<PortalInNode>();
			var portalOuts = _graph.GetNodesOfType<PortalOutNode>();

			Assert.AreEqual(0, portalIns.Count());
			Assert.AreEqual(0, portalOuts.Count());
		}

		[Test]
		public void Graph_WithMixedNodeTypes_PortalQueryReturnsOnlyPortals()
		{
			var entry = new EntryNode { Id = "entry" };
			var exit = new ExitNode { Id = "exit" };
			var portalIn = new PortalInNode(1) { Id = "portalIn" };
			var portalOut = new PortalOutNode(1) { Id = "portalOut" };

			_graph.AddNode(entry);
			_graph.AddNode(exit);
			_graph.AddNode(portalIn);
			_graph.AddNode(portalOut);

			var portalIns = _graph.GetNodesOfType<PortalInNode>().ToList();
			var portalOuts = _graph.GetNodesOfType<PortalOutNode>().ToList();

			Assert.AreEqual(1, portalIns.Count);
			Assert.AreEqual(1, portalOuts.Count);
			Assert.AreEqual("portalIn", portalIns[0].Id);
			Assert.AreEqual("portalOut", portalOuts[0].Id);
		}

		[Test]
		public void MultipleChannels_EachChannelPairedIndependently()
		{
			var portalIn1 = new PortalInNode(1) { Id = "in1" };
			var portalIn2 = new PortalInNode(2) { Id = "in2" };
			var portalIn3 = new PortalInNode(3) { Id = "in3" };

			var portalOut1a = new PortalOutNode(1) { Id = "out1a" };
			var portalOut1b = new PortalOutNode(1) { Id = "out1b" };
			var portalOut2a = new PortalOutNode(2) { Id = "out2a" };
			var portalOut3a = new PortalOutNode(3) { Id = "out3a" };
			var portalOut3b = new PortalOutNode(3) { Id = "out3b" };
			var portalOut3c = new PortalOutNode(3) { Id = "out3c" };

			_graph.AddNode(portalIn1);
			_graph.AddNode(portalIn2);
			_graph.AddNode(portalIn3);
			_graph.AddNode(portalOut1a);
			_graph.AddNode(portalOut1b);
			_graph.AddNode(portalOut2a);
			_graph.AddNode(portalOut3a);
			_graph.AddNode(portalOut3b);
			_graph.AddNode(portalOut3c);

			var allOuts = _graph.GetNodesOfType<PortalOutNode>().ToList();

			var channel1Outs = allOuts.Where(p => p.ChannelId == 1).ToList();
			var channel2Outs = allOuts.Where(p => p.ChannelId == 2).ToList();
			var channel3Outs = allOuts.Where(p => p.ChannelId == 3).ToList();

			Assert.AreEqual(2, channel1Outs.Count);
			Assert.AreEqual(1, channel2Outs.Count);
			Assert.AreEqual(3, channel3Outs.Count);
		}

		[Test]
		public void PortalInLookupByChannel_FindsCorrectNode()
		{
			var portalIn1 = new PortalInNode(1) { Id = "in1" };
			var portalIn2 = new PortalInNode(2) { Id = "in2" };
			var portalIn5 = new PortalInNode(5) { Id = "in5" };

			_graph.AddNode(portalIn1);
			_graph.AddNode(portalIn2);
			_graph.AddNode(portalIn5);

			// Build a channel → PortalIn lookup (mirrors StateMachineInstance._portalInNodeCache)
			var portalInByChannel = new Dictionary<int, PortalInNode>();
			foreach (var node in _graph.GetNodesOfType<PortalInNode>())
			{
				portalInByChannel[node.ChannelId] = node;
			}

			Assert.IsTrue(portalInByChannel.ContainsKey(1));
			Assert.IsTrue(portalInByChannel.ContainsKey(2));
			Assert.IsTrue(portalInByChannel.ContainsKey(5));
			Assert.IsFalse(portalInByChannel.ContainsKey(3));

			Assert.AreEqual("in1", portalInByChannel[1].Id);
			Assert.AreEqual("in2", portalInByChannel[2].Id);
			Assert.AreEqual("in5", portalInByChannel[5].Id);
		}

		[Test]
		public void ManyToOne_MultiplePortalOutsRouteToSinglePortalIn()
		{
			var portalIn = new PortalInNode(1) { Id = "in1" };
			var portalOut1 = new PortalOutNode(1) { Id = "out1" };
			var portalOut2 = new PortalOutNode(1) { Id = "out2" };
			var portalOut3 = new PortalOutNode(1) { Id = "out3" };

			_graph.AddNode(portalIn);
			_graph.AddNode(portalOut1);
			_graph.AddNode(portalOut2);
			_graph.AddNode(portalOut3);

			// Build lookup cache
			var portalInByChannel = new Dictionary<int, PortalInNode>();
			foreach (var node in _graph.GetNodesOfType<PortalInNode>())
			{
				portalInByChannel[node.ChannelId] = node;
			}

			// All PortalOuts on channel 1 should resolve to the same PortalIn
			foreach (var portalOut in _graph.GetNodesOfType<PortalOutNode>())
			{
				Assert.IsTrue(portalInByChannel.TryGetValue(portalOut.ChannelId, out var matchingIn));
				Assert.AreEqual("in1", matchingIn.Id);
			}
		}

		[Test]
		public void PortalNodeRemoval_UpdatesQueryResults()
		{
			var portalIn1 = new PortalInNode(1) { Id = "in1" };
			var portalIn2 = new PortalInNode(2) { Id = "in2" };

			_graph.AddNode(portalIn1);
			_graph.AddNode(portalIn2);

			Assert.AreEqual(2, _graph.GetNodesOfType<PortalInNode>().Count());

			_graph.RemoveNode("in1");

			var remaining = _graph.GetNodesOfType<PortalInNode>().ToList();
			Assert.AreEqual(1, remaining.Count);
			Assert.AreEqual("in2", remaining[0].Id);
		}

		[Test]
		public void PortalOutWithNoMatchingPortalIn_CanBeDetected()
		{
			var portalOut = new PortalOutNode(5) { Id = "orphanedOut" };
			_graph.AddNode(portalOut);

			var portalIns = _graph.GetNodesOfType<PortalInNode>()
				.Where(p => p.ChannelId == portalOut.ChannelId);

			Assert.AreEqual(0, portalIns.Count());
		}

		[Test]
		public void DuplicatePortalInsOnSameChannel_BothFoundInQuery()
		{
			var portalIn1 = new PortalInNode(1) { Id = "in1a" };
			var portalIn2 = new PortalInNode(1) { Id = "in1b" };

			_graph.AddNode(portalIn1);
			_graph.AddNode(portalIn2);

			var channel1Ins = _graph.GetNodesOfType<PortalInNode>()
				.Where(p => p.ChannelId == 1)
				.ToList();

			Assert.AreEqual(2, channel1Ins.Count);
		}

		[Test]
		public void PortalInCacheBuild_LastWins_ForDuplicateChannels()
		{
			var portalIn1 = new PortalInNode(1) { Id = "in1-first" };
			var portalIn2 = new PortalInNode(1) { Id = "in1-second" };

			_graph.AddNode(portalIn1);
			_graph.AddNode(portalIn2);

			// Build cache mimicking StateMachineInstance.BuildPortalInNodeCache (last-write-wins)
			var portalInByChannel = new Dictionary<int, PortalInNode>();
			foreach (var node in _graph.GetNodesOfType<PortalInNode>())
			{
				portalInByChannel[node.ChannelId] = node;
			}

			Assert.AreEqual(1, portalInByChannel.Count);
			Assert.IsTrue(portalInByChannel.ContainsKey(1));
			Assert.AreEqual("in1-second", portalInByChannel[1].Id);
		}



		[Test]
		public void PortalIn_WithOutgoingConnection_IsConnected()
		{
			var portalIn = new PortalInNode(1) { Id = "portalIn" };
			var stateNode = new StateNode { Id = "state" };

			_graph.AddNode(portalIn);
			_graph.AddNode(stateNode);

			_graph.AddConnection(new Connection
			{
				Id = "conn1",
				SourceNodeId = "portalIn",
				TargetNodeId = "state",
				ConnectionType = ConnectionType.Transition
			});

			var connections = _graph.GetConnectionsFromNode("portalIn");
			Assert.AreEqual(1, connections.Count());
		}

		[Test]
		public void PortalOut_WithIncomingTransition_IsConnected()
		{
			var stateNode = new StateNode { Id = "state" };
			var portalOut = new PortalOutNode(1) { Id = "portalOut" };

			_graph.AddNode(stateNode);
			_graph.AddNode(portalOut);

			_graph.AddConnection(new Connection
			{
				Id = "conn1",
				SourceNodeId = "state",
				SourcePortName = "transition",
				TargetNodeId = "portalOut",
				ConnectionType = ConnectionType.Transition
			});

			var connections = _graph.GetConnectionsToNode("portalOut");
			Assert.AreEqual(1, connections.Count());
		}

		[Test]
		public void PortalPair_WithFullConnectionChain_FormsContinuousPath()
		{
			var entry = new EntryNode { Id = "entry" };
			var portalOut = new PortalOutNode(1) { Id = "portalOut" };
			var portalIn = new PortalInNode(1) { Id = "portalIn" };
			var stateNode = new StateNode { Id = "state" };

			_graph.AddNode(entry);
			_graph.AddNode(portalOut);
			_graph.AddNode(portalIn);
			_graph.AddNode(stateNode);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "portalOut",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "portalIn",
				TargetNodeId = "state",
				ConnectionType = ConnectionType.Transition
			});

			// Verify the chain: entry connects to portalOut
			var entryConnections = _graph.GetConnectionsFromNode("entry").ToList();
			Assert.AreEqual(1, entryConnections.Count);
			Assert.AreEqual("portalOut", entryConnections[0].TargetNodeId);

			// Verify: portalIn connects to state
			var portalInConnections = _graph.GetConnectionsFromNode("portalIn").ToList();
			Assert.AreEqual(1, portalInConnections.Count);
			Assert.AreEqual("state", portalInConnections[0].TargetNodeId);

			// Verify: portalOut and portalIn share the same channel (logical link)
			Assert.AreEqual(portalOut.ChannelId, portalIn.ChannelId);
		}

	}
}
