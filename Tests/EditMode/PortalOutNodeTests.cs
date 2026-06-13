using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class PortalOutNodeTests
	{
		[Test]
		public void PortalOutNode_DefaultConstructor_GeneratesId()
		{
			var node = new PortalOutNode();

			Assert.IsNotNull(node.Id);
			Assert.IsNotEmpty(node.Id);
		}

		[Test]
		public void PortalOutNode_DefaultConstructor_SetsDisplayName()
		{
			var node = new PortalOutNode();

			Assert.AreEqual("Portal Out", node.DisplayName);
		}

		[Test]
		public void PortalOutNode_DefaultConstructor_SetsEmptyLabel()
		{
			var node = new PortalOutNode();

			Assert.AreEqual("", node.ChannelLabel);
		}

		[Test]
		public void PortalOutNode_DefaultConstructor_SetsChannelIdToZero()
		{
			var node = new PortalOutNode();

			Assert.AreEqual(0, node.ChannelId);
		}

		[Test]
		public void PortalOutNode_IdConstructor_UsesProvidedId()
		{
			var node = new PortalOutNode("custom-id");

			Assert.AreEqual("custom-id", node.Id);
		}

		[Test]
		public void PortalOutNode_IdConstructor_NullId_GeneratesId()
		{
			var node = new PortalOutNode(id: null);

			Assert.IsNotNull(node.Id);
			Assert.IsNotEmpty(node.Id);
		}

		[Test]
		public void PortalOutNode_IdConstructor_SetsDisplayName()
		{
			var node = new PortalOutNode("test-id");

			Assert.AreEqual("Portal Out", node.DisplayName);
		}

		[Test]
		public void PortalOutNode_ChannelConstructor_SetsChannelId()
		{
			var node = new PortalOutNode(channelId: 5);

			Assert.AreEqual(5, node.ChannelId);
		}

		[Test]
		public void PortalOutNode_ChannelConstructor_NullLabel_UsesDefaultLabel()
		{
			var node = new PortalOutNode(channelId: 3);

			Assert.AreEqual("Channel 3", node.ChannelLabel);
		}

		[Test]
		public void PortalOutNode_ChannelConstructor_CustomLabel_UsesProvidedLabel()
		{
			var node = new PortalOutNode(channelId: 2, label: "Exit Portal");

			Assert.AreEqual("Exit Portal", node.ChannelLabel);
		}

		[Test]
		public void PortalOutNode_ChannelConstructor_SetsDisplayName()
		{
			var node = new PortalOutNode(channelId: 1);

			Assert.AreEqual("Portal Out", node.DisplayName);
		}

		[Test]
		public void PortalOutNode_NodeType_ReturnsPortalOut()
		{
			var node = new PortalOutNode();

			Assert.AreEqual(GraphNodeType.PortalOut, node.NodeType);
		}

		[Test]
		public void PortalOutNode_ChannelId_CanBeSetAndRetrieved()
		{
			var node = new PortalOutNode();

			node.ChannelId = 7;

			Assert.AreEqual(7, node.ChannelId);
		}

		[Test]
		public void PortalOutNode_ChannelLabel_CanBeSetAndRetrieved()
		{
			var node = new PortalOutNode();

			node.ChannelLabel = "Updated Label";

			Assert.AreEqual("Updated Label", node.ChannelLabel);
		}

		[Test]
		public void PortalOutNode_ChannelColor_ReturnsColorForChannel()
		{
			var node = new PortalOutNode(channelId: 0);
			var expectedColor = PortalChannel.GetColor(0);

			Assert.AreEqual(expectedColor, node.ChannelColor);
		}

		[Test]
		public void PortalOutNode_ChannelColor_ChangesWithChannelId()
		{
			var node = new PortalOutNode();
			node.ChannelId = 0;
			var color0 = node.ChannelColor;

			node.ChannelId = 1;
			var color1 = node.ChannelColor;

			Assert.AreNotEqual(color0, color1);
		}

		[Test]
		public void PortalOutNode_Position_CanBeSetAndRetrieved()
		{
			var node = new PortalOutNode();

			node.Position = new Vector2(100, 200);

			Assert.AreEqual(new Vector2(100, 200), node.Position);
		}

		[Test]
		public void PortalOutNode_Comment_CanBeSetAndRetrieved()
		{
			var node = new PortalOutNode();

			node.Comment = "Test comment";

			Assert.AreEqual("Test comment", node.Comment);
		}

		[Test]
		public void PortalOutNode_TwoInstances_HaveDifferentIds()
		{
			var node1 = new PortalOutNode();
			var node2 = new PortalOutNode();

			Assert.AreNotEqual(node1.Id, node2.Id);
		}

		[Test]
		public void PortalOutNode_SerializationRoundTrip_PreservesProperties()
		{
			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			try
			{
				var original = new PortalOutNode(channelId: 4, label: "Test Channel");
				original.Position = new Vector2(300, 400);
				original.Comment = "Serialization test";
				graph.AddNode(original);

				var serialized = SerializedNode.FromNode(original);
				var deserialized = serialized.ToNode() as PortalOutNode;

				Assert.IsNotNull(deserialized);
				Assert.AreEqual(original.Id, deserialized.Id);
				Assert.AreEqual(original.ChannelId, deserialized.ChannelId);
				Assert.AreEqual(original.ChannelLabel, deserialized.ChannelLabel);
				Assert.AreEqual(original.Position, deserialized.Position);
				Assert.AreEqual(original.Comment, deserialized.Comment);
				Assert.AreEqual(GraphNodeType.PortalOut, deserialized.NodeType);
			}
			finally
			{
				Object.DestroyImmediate(graph);
			}
		}

		[Test]
		public void PortalOutNode_AddToGraph_CanBeRetrieved()
		{
			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			try
			{
				var node = new PortalOutNode(channelId: 2);
				graph.AddNode(node);

				var retrieved = graph.GetNode(node.Id);

				Assert.IsNotNull(retrieved);
				Assert.AreSame(node, retrieved);
			}
			finally
			{
				Object.DestroyImmediate(graph);
			}
		}

		[Test]
		public void PortalOutNode_SameChannelAsPortalIn_MatchesChannelId()
		{
			var portalIn = new PortalInNode(channelId: 5);
			var portalOut = new PortalOutNode(channelId: 5);

			Assert.AreEqual(portalIn.ChannelId, portalOut.ChannelId);
		}

		[Test]
		public void PortalOutNode_SameChannel_HasSameColorAsPortalIn()
		{
			var portalIn = new PortalInNode(channelId: 3);
			var portalOut = new PortalOutNode(channelId: 3);

			Assert.AreEqual(portalIn.ChannelColor, portalOut.ChannelColor);
		}

		[Test]
		public void PortalOutNode_MultipleOnSameChannel_AllShareChannelId()
		{
			var portalOut1 = new PortalOutNode(channelId: 2);
			var portalOut2 = new PortalOutNode(channelId: 2);
			var portalOut3 = new PortalOutNode(channelId: 2);

			Assert.AreEqual(2, portalOut1.ChannelId);
			Assert.AreEqual(2, portalOut2.ChannelId);
			Assert.AreEqual(2, portalOut3.ChannelId);
			Assert.AreNotEqual(portalOut1.Id, portalOut2.Id);
			Assert.AreNotEqual(portalOut2.Id, portalOut3.Id);
		}
	}
}
