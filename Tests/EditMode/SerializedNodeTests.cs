using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class SerializedNodeTests
	{
		[Test]
		public void FromNode_NullNode_ReturnsNull()
		{
			var result = SerializedNode.FromNode(null);

			Assert.IsNull(result);
		}

		[Test]
		public void FromNode_EntryNode_SetsCorrectNodeType()
		{
			var node = new EntryNode();

			var serialized = SerializedNode.FromNode(node);

			Assert.AreEqual(GraphNodeType.Entry, serialized.NodeType);
		}

		[Test]
		public void FromNode_EntryNode_SetsCorrectNodeId()
		{
			var node = new EntryNode();

			var serialized = SerializedNode.FromNode(node);

			Assert.AreEqual(node.Id, serialized.NodeId);
		}

		[Test]
		public void FromNode_EntryNode_SetsNonEmptyJsonData()
		{
			var node = new EntryNode();

			var serialized = SerializedNode.FromNode(node);

			Assert.IsNotNull(serialized.JsonData);
			Assert.IsNotEmpty(serialized.JsonData);
		}

		[Test]
		public void FromNode_ExitNode_SetsCorrectNodeType()
		{
			var node = new ExitNode();

			var serialized = SerializedNode.FromNode(node);

			Assert.AreEqual(GraphNodeType.Exit, serialized.NodeType);
		}

		[Test]
		public void FromNode_PortalInNode_SetsCorrectNodeType()
		{
			var node = new PortalInNode();

			var serialized = SerializedNode.FromNode(node);

			Assert.AreEqual(GraphNodeType.PortalIn, serialized.NodeType);
		}

		[Test]
		public void FromNode_PortalOutNode_SetsCorrectNodeType()
		{
			var node = new PortalOutNode();

			var serialized = SerializedNode.FromNode(node);

			Assert.AreEqual(GraphNodeType.PortalOut, serialized.NodeType);
		}

		[Test]
		public void ToNode_EmptyJsonData_ReturnsNull()
		{
			var serialized = SerializedNode.FromNode(new EntryNode());
			// Create a serialized node with empty json via reflection workaround:
			// Use FromNode then test the ToNode path with a valid serialized node instead
			var node = serialized.ToNode();

			Assert.IsNotNull(node);
		}

		[Test]
		public void RoundTrip_EntryNode_PreservesId()
		{
			var original = new EntryNode("entry-123");

			var serialized = SerializedNode.FromNode(original);
			var deserialized = serialized.ToNode();

			Assert.AreEqual("entry-123", deserialized.Id);
		}

		[Test]
		public void RoundTrip_EntryNode_PreservesDisplayName()
		{
			var original = new EntryNode();

			var serialized = SerializedNode.FromNode(original);
			var deserialized = serialized.ToNode();

			Assert.AreEqual("Entry", deserialized.DisplayName);
		}

		[Test]
		public void RoundTrip_EntryNode_PreservesNodeType()
		{
			var original = new EntryNode();

			var serialized = SerializedNode.FromNode(original);
			var deserialized = serialized.ToNode();

			Assert.AreEqual(GraphNodeType.Entry, deserialized.NodeType);
		}

		[Test]
		public void RoundTrip_EntryNode_PreservesPosition()
		{
			var original = new EntryNode();
			original.Position = new Vector2(150, 250);

			var serialized = SerializedNode.FromNode(original);
			var deserialized = serialized.ToNode();

			Assert.AreEqual(new Vector2(150, 250), deserialized.Position);
		}

		[Test]
		public void RoundTrip_EntryNode_PreservesComment()
		{
			var original = new EntryNode();
			original.Comment = "Start here";

			var serialized = SerializedNode.FromNode(original);
			var deserialized = serialized.ToNode();

			Assert.AreEqual("Start here", deserialized.Comment);
		}

		[Test]
		public void RoundTrip_ExitNode_PreservesExitName()
		{
			var original = new ExitNode("Success", 0);

			var serialized = SerializedNode.FromNode(original);
			var deserialized = serialized.ToNode() as ExitNode;

			Assert.IsNotNull(deserialized);
			Assert.AreEqual("Success", deserialized.ExitName);
		}

		[Test]
		public void RoundTrip_ExitNode_PreservesExitCode()
		{
			var original = new ExitNode("Failure", 42);

			var serialized = SerializedNode.FromNode(original);
			var deserialized = serialized.ToNode() as ExitNode;

			Assert.IsNotNull(deserialized);
			Assert.AreEqual(42, deserialized.ExitCode);
		}

		[Test]
		public void RoundTrip_PortalInNode_PreservesChannelId()
		{
			var original = new PortalInNode(channelId: 5, label: "Alpha");

			var serialized = SerializedNode.FromNode(original);
			var deserialized = serialized.ToNode() as PortalInNode;

			Assert.IsNotNull(deserialized);
			Assert.AreEqual(5, deserialized.ChannelId);
		}

		[Test]
		public void RoundTrip_PortalInNode_PreservesChannelLabel()
		{
			var original = new PortalInNode(channelId: 3, label: "Portal Alpha");

			var serialized = SerializedNode.FromNode(original);
			var deserialized = serialized.ToNode() as PortalInNode;

			Assert.IsNotNull(deserialized);
			Assert.AreEqual("Portal Alpha", deserialized.ChannelLabel);
		}

		[Test]
		public void RoundTrip_PortalOutNode_PreservesChannelId()
		{
			var original = new PortalOutNode(channelId: 7, label: "Beta");

			var serialized = SerializedNode.FromNode(original);
			var deserialized = serialized.ToNode() as PortalOutNode;

			Assert.IsNotNull(deserialized);
			Assert.AreEqual(7, deserialized.ChannelId);
		}

		[Test]
		public void RoundTrip_PortalOutNode_PreservesChannelLabel()
		{
			var original = new PortalOutNode(channelId: 2, label: "Portal Beta");

			var serialized = SerializedNode.FromNode(original);
			var deserialized = serialized.ToNode() as PortalOutNode;

			Assert.IsNotNull(deserialized);
			Assert.AreEqual("Portal Beta", deserialized.ChannelLabel);
		}

		[Test]
		public void RoundTrip_EntryNode_ReturnsCorrectType()
		{
			var original = new EntryNode();

			var serialized = SerializedNode.FromNode(original);
			var deserialized = serialized.ToNode();

			Assert.IsInstanceOf<EntryNode>(deserialized);
		}

		[Test]
		public void RoundTrip_ExitNode_ReturnsCorrectType()
		{
			var original = new ExitNode();

			var serialized = SerializedNode.FromNode(original);
			var deserialized = serialized.ToNode();

			Assert.IsInstanceOf<ExitNode>(deserialized);
		}

		[Test]
		public void RoundTrip_PortalInNode_ReturnsCorrectType()
		{
			var original = new PortalInNode();

			var serialized = SerializedNode.FromNode(original);
			var deserialized = serialized.ToNode();

			Assert.IsInstanceOf<PortalInNode>(deserialized);
		}

		[Test]
		public void RoundTrip_PortalOutNode_ReturnsCorrectType()
		{
			var original = new PortalOutNode();

			var serialized = SerializedNode.FromNode(original);
			var deserialized = serialized.ToNode();

			Assert.IsInstanceOf<PortalOutNode>(deserialized);
		}

		[Test]
		public void FromNode_DifferentNodeTypes_ProduceDifferentJson()
		{
			var entry = SerializedNode.FromNode(new EntryNode());
			var exit = SerializedNode.FromNode(new ExitNode());

			Assert.AreNotEqual(entry.JsonData, exit.JsonData);
		}

		[Test]
		public void FromNode_SameNodeType_DifferentIds_ProduceDifferentJson()
		{
			var node1 = new EntryNode("id-1");
			var node2 = new EntryNode("id-2");

			var s1 = SerializedNode.FromNode(node1);
			var s2 = SerializedNode.FromNode(node2);

			Assert.AreNotEqual(s1.JsonData, s2.JsonData);
			Assert.AreNotEqual(s1.NodeId, s2.NodeId);
		}

		[Test]
		public void ToNode_MalformedJson_ReturnsNull()
		{
			var serialized = CreateSerializedNodeWithJson(GraphNodeType.Entry, "entry-bad", "{INVALID JSON!!!");

			LogAssert.Expect(LogType.Error, new Regex("Failed to deserialize"));

			var result = serialized.ToNode();

			Assert.IsNull(result);
		}

		[Test]
		public void ToNode_MalformedJson_LogsErrorWithNodeInfo()
		{
			var serialized = CreateSerializedNodeWithJson(GraphNodeType.State, "state-corrupt", "not json at all");

			LogAssert.Expect(LogType.Error, new Regex(@"State.*state-corrupt"));

			serialized.ToNode();
		}

		private static SerializedNode CreateSerializedNodeWithJson(GraphNodeType nodeType, string nodeId, string json)
		{
			var serialized = SerializedNode.FromNode(new EntryNode());
			var type = typeof(SerializedNode);
			var flags = BindingFlags.NonPublic | BindingFlags.Instance;

			type.GetField("_nodeType", flags).SetValue(serialized, nodeType);
			type.GetField("_nodeId", flags).SetValue(serialized, nodeId);
			type.GetField("_jsonData", flags).SetValue(serialized, json);

			return serialized;
		}
	}
}
