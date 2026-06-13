using System;
using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	/// <summary>
	/// Concrete test subclass to test GraphNode abstract base class.
	/// </summary>
	[Serializable]
	public class TestGraphNode : GraphNode
	{
		public override GraphNodeType NodeType => GraphNodeType.State;

		public TestGraphNode() : base() { }

		public TestGraphNode(string id) : base(id) { }
	}

	[TestFixture]
	public class GraphNodeBaseTests
	{
		[Test]
		public void DefaultConstructor_GeneratesNonNullId()
		{
			var node = new TestGraphNode();

			Assert.IsNotNull(node.Id);
			Assert.IsFalse(string.IsNullOrEmpty(node.Id));
		}

		[Test]
		public void DefaultConstructor_GeneratesValidGuid()
		{
			var node = new TestGraphNode();

			Assert.IsTrue(Guid.TryParse(node.Id, out _));
		}

		[Test]
		public void DefaultConstructor_GeneratesUniqueIds()
		{
			var node1 = new TestGraphNode();
			var node2 = new TestGraphNode();

			Assert.AreNotEqual(node1.Id, node2.Id);
		}

		[Test]
		public void ParameterizedConstructor_UsesProvidedId()
		{
			var node = new TestGraphNode("custom-id");

			Assert.AreEqual("custom-id", node.Id);
		}

		[Test]
		public void ParameterizedConstructor_NullId_GeneratesGuid()
		{
			var node = new TestGraphNode(null);

			Assert.IsNotNull(node.Id);
			Assert.IsTrue(Guid.TryParse(node.Id, out _));
		}

		[Test]
		public void Id_CanBeOverwritten()
		{
			var node = new TestGraphNode();
			node.Id = "overwritten-id";

			Assert.AreEqual("overwritten-id", node.Id);
		}

		[Test]
		public void Position_DefaultIsZero()
		{
			var node = new TestGraphNode();

			Assert.AreEqual(Vector2.zero, node.Position);
		}

		[Test]
		public void Position_SetAndGet()
		{
			var node = new TestGraphNode();
			node.Position = new Vector2(150.5f, 275.3f);

			Assert.AreEqual(new Vector2(150.5f, 275.3f), node.Position);
		}

		[Test]
		public void DisplayName_DefaultIsNull()
		{
			var node = new TestGraphNode();

			Assert.IsNull(node.DisplayName);
		}

		[Test]
		public void DisplayName_SetAndGet()
		{
			var node = new TestGraphNode();
			node.DisplayName = "My Node";

			Assert.AreEqual("My Node", node.DisplayName);
		}

		[Test]
		public void Comment_DefaultIsNull()
		{
			var node = new TestGraphNode();

			Assert.IsNull(node.Comment);
		}

		[Test]
		public void Comment_SetAndGet()
		{
			var node = new TestGraphNode();
			node.Comment = "This is a comment";

			Assert.AreEqual("This is a comment", node.Comment);
		}

		[Test]
		public void Comment_CanBeSetToEmpty()
		{
			var node = new TestGraphNode();
			node.Comment = "";

			Assert.AreEqual("", node.Comment);
		}

		[Test]
		public void NodeType_ReturnsExpectedType()
		{
			var node = new TestGraphNode();

			Assert.AreEqual(GraphNodeType.State, node.NodeType);
		}

		[Test]
		public void AllConcreteNodeTypes_InheritFromGraphNode()
		{
			Assert.IsTrue(typeof(GraphNode).IsAssignableFrom(typeof(StateNode)));
			Assert.IsTrue(typeof(GraphNode).IsAssignableFrom(typeof(EntryNode)));
			Assert.IsTrue(typeof(GraphNode).IsAssignableFrom(typeof(ExitNode)));
			Assert.IsTrue(typeof(GraphNode).IsAssignableFrom(typeof(SubStateMachineNode)));
			Assert.IsTrue(typeof(GraphNode).IsAssignableFrom(typeof(PortalInNode)));
			Assert.IsTrue(typeof(GraphNode).IsAssignableFrom(typeof(PortalOutNode)));
			Assert.IsTrue(typeof(GraphNode).IsAssignableFrom(typeof(BlackboardVariableNode)));
		}

		[Test]
		public void SerializedNode_RoundTrip_PreservesBaseProperties()
		{
			var original = new StateNode
			{
				Id = "test-id",
				Position = new Vector2(100, 200),
				DisplayName = "TestName",
				Comment = "Test comment"
			};

			var serialized = SerializedNode.FromNode(original);
			var restored = serialized.ToNode();

			Assert.AreEqual(original.Id, restored.Id);
			Assert.AreEqual(original.Position, restored.Position);
			Assert.AreEqual(original.DisplayName, restored.DisplayName);
			Assert.AreEqual(original.Comment, restored.Comment);
		}
	}
}
