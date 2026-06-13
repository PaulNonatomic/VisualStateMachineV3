using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class EntryExitNodeTests
	{

		[Test]
		public void EntryNode_DefaultConstructor_GeneratesId()
		{
			var node = new EntryNode();

			Assert.IsFalse(string.IsNullOrEmpty(node.Id));
		}

		[Test]
		public void EntryNode_DefaultConstructor_DisplayNameIsEntry()
		{
			var node = new EntryNode();

			Assert.AreEqual("Entry", node.DisplayName);
		}

		[Test]
		public void EntryNode_DefaultConstructor_NodeTypeIsEntry()
		{
			var node = new EntryNode();

			Assert.AreEqual(GraphNodeType.Entry, node.NodeType);
		}

		[Test]
		public void EntryNode_IdConstructor_UsesProvidedId()
		{
			var node = new EntryNode("custom-entry-id");

			Assert.AreEqual("custom-entry-id", node.Id);
		}

		[Test]
		public void EntryNode_IdConstructor_NullId_GeneratesId()
		{
			var node = new EntryNode(null);

			Assert.IsFalse(string.IsNullOrEmpty(node.Id));
		}

		[Test]
		public void EntryNode_IdConstructor_DisplayNameIsEntry()
		{
			var node = new EntryNode("my-id");

			Assert.AreEqual("Entry", node.DisplayName);
		}

		[Test]
		public void EntryNode_TwoInstances_HaveUniqueIds()
		{
			var node1 = new EntryNode();
			var node2 = new EntryNode();

			Assert.AreNotEqual(node1.Id, node2.Id);
		}

		[Test]
		public void EntryNode_Position_DefaultIsZero()
		{
			var node = new EntryNode();

			Assert.AreEqual(Vector2.zero, node.Position);
		}

		[Test]
		public void EntryNode_Position_SetAndGet()
		{
			var node = new EntryNode();

			node.Position = new Vector2(100f, 200f);

			Assert.AreEqual(new Vector2(100f, 200f), node.Position);
		}

		[Test]
		public void EntryNode_Comment_DefaultIsNull()
		{
			var node = new EntryNode();

			Assert.IsNull(node.Comment);
		}

		[Test]
		public void EntryNode_Comment_SetAndGet()
		{
			var node = new EntryNode();

			node.Comment = "Start here";

			Assert.AreEqual("Start here", node.Comment);
		}



		[Test]
		public void ExitNode_DefaultConstructor_GeneratesId()
		{
			var node = new ExitNode();

			Assert.IsFalse(string.IsNullOrEmpty(node.Id));
		}

		[Test]
		public void ExitNode_DefaultConstructor_DisplayNameIsExit()
		{
			var node = new ExitNode();

			Assert.AreEqual("Exit", node.DisplayName);
		}

		[Test]
		public void ExitNode_DefaultConstructor_ExitNameIsExit()
		{
			var node = new ExitNode();

			Assert.AreEqual("Exit", node.ExitName);
		}

		[Test]
		public void ExitNode_DefaultConstructor_NodeTypeIsExit()
		{
			var node = new ExitNode();

			Assert.AreEqual(GraphNodeType.Exit, node.NodeType);
		}

		[Test]
		public void ExitNode_DefaultConstructor_ExitCodeIsZero()
		{
			var node = new ExitNode();

			Assert.AreEqual(0, node.ExitCode);
		}

		[Test]
		public void ExitNode_IdConstructor_UsesProvidedId()
		{
			var node = new ExitNode("custom-exit-id");

			Assert.AreEqual("custom-exit-id", node.Id);
		}

		[Test]
		public void ExitNode_IdConstructor_NullId_GeneratesId()
		{
			var node = new ExitNode((string)null);

			Assert.IsFalse(string.IsNullOrEmpty(node.Id));
		}

		[Test]
		public void ExitNode_IdConstructor_DisplayNameIsExit()
		{
			var node = new ExitNode("my-id");

			Assert.AreEqual("Exit", node.DisplayName);
		}

		[Test]
		public void ExitNode_IdConstructor_ExitNameIsExit()
		{
			var node = new ExitNode("my-id");

			Assert.AreEqual("Exit", node.ExitName);
		}

		[Test]
		public void ExitNode_NamedConstructor_SetsExitName()
		{
			var node = new ExitNode("Success", 0);

			Assert.AreEqual("Success", node.ExitName);
		}

		[Test]
		public void ExitNode_NamedConstructor_SetsExitCode()
		{
			var node = new ExitNode("Failure", 1);

			Assert.AreEqual(1, node.ExitCode);
		}

		[Test]
		public void ExitNode_NamedConstructor_DisplayNameMatchesExitName()
		{
			var node = new ExitNode("Timeout", 2);

			Assert.AreEqual("Timeout", node.DisplayName);
		}

		[Test]
		public void ExitNode_NamedConstructor_GeneratesId()
		{
			var node = new ExitNode("Success", 0);

			Assert.IsFalse(string.IsNullOrEmpty(node.Id));
		}

		[Test]
		public void ExitNode_ExitName_SetAndGet()
		{
			var node = new ExitNode();

			node.ExitName = "Cancelled";

			Assert.AreEqual("Cancelled", node.ExitName);
		}

		[Test]
		public void ExitNode_ExitCode_SetAndGet()
		{
			var node = new ExitNode();

			node.ExitCode = 42;

			Assert.AreEqual(42, node.ExitCode);
		}

		[Test]
		public void ExitNode_ExitCode_NegativeValue_Stores()
		{
			var node = new ExitNode();

			node.ExitCode = -1;

			Assert.AreEqual(-1, node.ExitCode);
		}

		[Test]
		public void ExitNode_TwoInstances_HaveUniqueIds()
		{
			var node1 = new ExitNode();
			var node2 = new ExitNode();

			Assert.AreNotEqual(node1.Id, node2.Id);
		}

		[Test]
		public void ExitNode_Position_SetAndGet()
		{
			var node = new ExitNode();

			node.Position = new Vector2(300f, 400f);

			Assert.AreEqual(new Vector2(300f, 400f), node.Position);
		}



		[Test]
		public void EntryNode_JsonSerialization_PreservesProperties()
		{
			var original = new EntryNode("entry-123");
			original.Position = new Vector2(50f, 75f);
			original.Comment = "Graph start";

			var json = JsonUtility.ToJson(original);
			var restored = JsonUtility.FromJson<EntryNode>(json);

			Assert.AreEqual("entry-123", restored.Id);
			Assert.AreEqual("Entry", restored.DisplayName);
			Assert.AreEqual(new Vector2(50f, 75f), restored.Position);
			Assert.AreEqual("Graph start", restored.Comment);
		}

		[Test]
		public void ExitNode_JsonSerialization_PreservesProperties()
		{
			var original = new ExitNode("Success", 0);
			original.Id = "exit-456";
			original.Position = new Vector2(500f, 200f);

			var json = JsonUtility.ToJson(original);
			var restored = JsonUtility.FromJson<ExitNode>(json);

			Assert.AreEqual("exit-456", restored.Id);
			Assert.AreEqual("Success", restored.ExitName);
			Assert.AreEqual(0, restored.ExitCode);
			Assert.AreEqual(new Vector2(500f, 200f), restored.Position);
		}

		[Test]
		public void ExitNode_JsonSerialization_PreservesExitCode()
		{
			var original = new ExitNode("Error", 99);
			original.Id = "exit-err";

			var json = JsonUtility.ToJson(original);
			var restored = JsonUtility.FromJson<ExitNode>(json);

			Assert.AreEqual(99, restored.ExitCode);
			Assert.AreEqual("Error", restored.ExitName);
		}



		[Test]
		public void EntryNode_DisplayName_CanBeOverwritten()
		{
			var node = new EntryNode();

			node.DisplayName = "Custom Entry";

			Assert.AreEqual("Custom Entry", node.DisplayName);
		}

		[Test]
		public void ExitNode_DisplayName_CanBeOverwritten()
		{
			var node = new ExitNode();

			node.DisplayName = "Custom Exit";

			Assert.AreEqual("Custom Exit", node.DisplayName);
		}

		[Test]
		public void ExitNode_NamedConstructor_NullExitName_Stores()
		{
			var node = new ExitNode(null, 0);

			Assert.IsNull(node.ExitName);
			Assert.IsNull(node.DisplayName);
		}

		[Test]
		public void ExitNode_NamedConstructor_EmptyExitName_Stores()
		{
			var node = new ExitNode("", 0);

			Assert.AreEqual("", node.ExitName);
			Assert.AreEqual("", node.DisplayName);
		}

		[Test]
		public void EntryNode_Id_CanBeOverwritten()
		{
			var node = new EntryNode();
			var originalId = node.Id;

			node.Id = "new-id";

			Assert.AreNotEqual(originalId, node.Id);
			Assert.AreEqual("new-id", node.Id);
		}

		[Test]
		public void ExitNode_MultipleExitConditions_IndependentState()
		{
			var success = new ExitNode("Success", 0);
			var failure = new ExitNode("Failure", 1);
			var timeout = new ExitNode("Timeout", 2);

			Assert.AreEqual("Success", success.ExitName);
			Assert.AreEqual("Failure", failure.ExitName);
			Assert.AreEqual("Timeout", timeout.ExitName);
			Assert.AreEqual(0, success.ExitCode);
			Assert.AreEqual(1, failure.ExitCode);
			Assert.AreEqual(2, timeout.ExitCode);
			Assert.AreNotEqual(success.Id, failure.Id);
			Assert.AreNotEqual(failure.Id, timeout.Id);
		}

	}
}
