using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class PortalInNodeTests
	{
		[Test]
		public void PortalInNode_DefaultConstructor_GeneratesId()
		{
			var node = new PortalInNode();

			Assert.IsNotNull(node.Id);
			Assert.IsNotEmpty(node.Id);
		}

		[Test]
		public void PortalInNode_DefaultConstructor_SetsDisplayName()
		{
			var node = new PortalInNode();

			Assert.AreEqual("Portal In", node.DisplayName);
		}

		[Test]
		public void PortalInNode_DefaultConstructor_SetsEmptyLabel()
		{
			var node = new PortalInNode();

			Assert.AreEqual("", node.ChannelLabel);
		}

		[Test]
		public void PortalInNode_DefaultConstructor_SetsChannelIdToZero()
		{
			var node = new PortalInNode();

			Assert.AreEqual(0, node.ChannelId);
		}

		[Test]
		public void PortalInNode_IdConstructor_UsesProvidedId()
		{
			var node = new PortalInNode("custom-id");

			Assert.AreEqual("custom-id", node.Id);
		}

		[Test]
		public void PortalInNode_IdConstructor_NullId_GeneratesId()
		{
			var node = new PortalInNode(id: null);

			Assert.IsNotNull(node.Id);
			Assert.IsNotEmpty(node.Id);
		}

		[Test]
		public void PortalInNode_IdConstructor_SetsDisplayName()
		{
			var node = new PortalInNode("test-id");

			Assert.AreEqual("Portal In", node.DisplayName);
		}

		[Test]
		public void PortalInNode_ChannelConstructor_SetsChannelId()
		{
			var node = new PortalInNode(channelId: 5);

			Assert.AreEqual(5, node.ChannelId);
		}

		[Test]
		public void PortalInNode_ChannelConstructor_NullLabel_UsesDefaultLabel()
		{
			var node = new PortalInNode(channelId: 3);

			Assert.AreEqual("Channel 3", node.ChannelLabel);
		}

		[Test]
		public void PortalInNode_ChannelConstructor_CustomLabel_UsesProvidedLabel()
		{
			var node = new PortalInNode(channelId: 2, label: "My Portal");

			Assert.AreEqual("My Portal", node.ChannelLabel);
		}

		[Test]
		public void PortalInNode_ChannelConstructor_SetsDisplayName()
		{
			var node = new PortalInNode(channelId: 1);

			Assert.AreEqual("Portal In", node.DisplayName);
		}

		[Test]
		public void PortalInNode_NodeType_ReturnsPortalIn()
		{
			var node = new PortalInNode();

			Assert.AreEqual(GraphNodeType.PortalIn, node.NodeType);
		}

		[Test]
		public void PortalInNode_ChannelId_CanBeSetAndRetrieved()
		{
			var node = new PortalInNode();

			node.ChannelId = 7;

			Assert.AreEqual(7, node.ChannelId);
		}

		[Test]
		public void PortalInNode_ChannelLabel_CanBeSetAndRetrieved()
		{
			var node = new PortalInNode();

			node.ChannelLabel = "Updated Label";

			Assert.AreEqual("Updated Label", node.ChannelLabel);
		}

		[Test]
		public void PortalInNode_ChannelColor_ReturnsColorForChannel()
		{
			var node = new PortalInNode(channelId: 0);
			var expectedColor = PortalChannel.GetColor(0);

			Assert.AreEqual(expectedColor, node.ChannelColor);
		}

		[Test]
		public void PortalInNode_ChannelColor_ChangesWithChannelId()
		{
			var node = new PortalInNode();
			node.ChannelId = 0;
			var color0 = node.ChannelColor;

			node.ChannelId = 1;
			var color1 = node.ChannelColor;

			Assert.AreNotEqual(color0, color1);
		}

		[Test]
		public void PortalInNode_Position_CanBeSetAndRetrieved()
		{
			var node = new PortalInNode();

			node.Position = new Vector2(100, 200);

			Assert.AreEqual(new Vector2(100, 200), node.Position);
		}

		[Test]
		public void PortalInNode_Comment_CanBeSetAndRetrieved()
		{
			var node = new PortalInNode();

			node.Comment = "Test comment";

			Assert.AreEqual("Test comment", node.Comment);
		}

		[Test]
		public void PortalInNode_TwoInstances_HaveDifferentIds()
		{
			var node1 = new PortalInNode();
			var node2 = new PortalInNode();

			Assert.AreNotEqual(node1.Id, node2.Id);
		}

		[Test]
		public void PortalInNode_SerializationRoundTrip_PreservesProperties()
		{
			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			try
			{
				var original = new PortalInNode(channelId: 4, label: "Test Channel");
				original.Position = new Vector2(300, 400);
				original.Comment = "Serialization test";
				graph.AddNode(original);

				var serialized = SerializedNode.FromNode(original);
				var deserialized = serialized.ToNode() as PortalInNode;

				Assert.IsNotNull(deserialized);
				Assert.AreEqual(original.Id, deserialized.Id);
				Assert.AreEqual(original.ChannelId, deserialized.ChannelId);
				Assert.AreEqual(original.ChannelLabel, deserialized.ChannelLabel);
				Assert.AreEqual(original.Position, deserialized.Position);
				Assert.AreEqual(original.Comment, deserialized.Comment);
				Assert.AreEqual(GraphNodeType.PortalIn, deserialized.NodeType);
			}
			finally
			{
				Object.DestroyImmediate(graph);
			}
		}

		[Test]
		public void PortalInNode_AddToGraph_CanBeRetrieved()
		{
			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			try
			{
				var node = new PortalInNode(channelId: 2);
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
	}
}
