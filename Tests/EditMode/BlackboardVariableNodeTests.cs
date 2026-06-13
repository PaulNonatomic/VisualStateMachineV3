using System;
using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class BlackboardVariableNodeTests
	{
		[Test]
		public void DefaultConstructor_GeneratesId()
		{
			var node = new BlackboardVariableNode();

			Assert.IsFalse(string.IsNullOrEmpty(node.Id));
		}

		[Test]
		public void DefaultConstructor_ModeIsGet()
		{
			var node = new BlackboardVariableNode();

			Assert.AreEqual(BlackboardNodeMode.Get, node.Mode);
		}

		[Test]
		public void DefaultConstructor_VariableKeyIsNull()
		{
			var node = new BlackboardVariableNode();

			Assert.IsNull(node.VariableKey);
		}

		[Test]
		public void DefaultConstructor_TypeAssemblyQualifiedNameIsNull()
		{
			var node = new BlackboardVariableNode();

			Assert.IsNull(node.TypeAssemblyQualifiedName);
		}

		[Test]
		public void DefaultConstructor_VariableTypeIsNull()
		{
			var node = new BlackboardVariableNode();

			Assert.IsNull(node.VariableType);
		}

		[Test]
		public void DefaultConstructor_NodeTypeIsBlackboardVariable()
		{
			var node = new BlackboardVariableNode();

			Assert.AreEqual(GraphNodeType.BlackboardVariable, node.NodeType);
		}

		[Test]
		public void DefaultConstructor_PositionIsZero()
		{
			var node = new BlackboardVariableNode();

			Assert.AreEqual(Vector2.zero, node.Position);
		}

		[Test]
		public void IdConstructor_UsesProvidedId()
		{
			var node = new BlackboardVariableNode("custom-bb-id");

			Assert.AreEqual("custom-bb-id", node.Id);
		}

		[Test]
		public void IdConstructor_NullId_GeneratesId()
		{
			var node = new BlackboardVariableNode((string)null);

			Assert.IsFalse(string.IsNullOrEmpty(node.Id));
		}

		[Test]
		public void IdConstructor_ModeIsGet()
		{
			var node = new BlackboardVariableNode("some-id");

			Assert.AreEqual(BlackboardNodeMode.Get, node.Mode);
		}

		[Test]
		public void FullConstructor_SetsVariableKey()
		{
			var node = new BlackboardVariableNode("Health", typeof(float), BlackboardNodeMode.Get);

			Assert.AreEqual("Health", node.VariableKey);
		}

		[Test]
		public void FullConstructor_SetsVariableType()
		{
			var node = new BlackboardVariableNode("Health", typeof(float), BlackboardNodeMode.Get);

			Assert.AreEqual(typeof(float), node.VariableType);
		}

		[Test]
		public void FullConstructor_SetsTypeAssemblyQualifiedName()
		{
			var node = new BlackboardVariableNode("Health", typeof(float), BlackboardNodeMode.Get);

			Assert.AreEqual(typeof(float).AssemblyQualifiedName, node.TypeAssemblyQualifiedName);
		}

		[Test]
		public void FullConstructor_SetsMode()
		{
			var node = new BlackboardVariableNode("Health", typeof(float), BlackboardNodeMode.Set);

			Assert.AreEqual(BlackboardNodeMode.Set, node.Mode);
		}

		[Test]
		public void FullConstructor_SetsDisplayNameToVariableKey()
		{
			var node = new BlackboardVariableNode("PlayerName", typeof(string), BlackboardNodeMode.Get);

			Assert.AreEqual("PlayerName", node.DisplayName);
		}

		[Test]
		public void FullConstructor_GeneratesId()
		{
			var node = new BlackboardVariableNode("Score", typeof(int), BlackboardNodeMode.Get);

			Assert.IsFalse(string.IsNullOrEmpty(node.Id));
		}

		[Test]
		public void FullConstructor_NodeTypeIsBlackboardVariable()
		{
			var node = new BlackboardVariableNode("Key", typeof(int), BlackboardNodeMode.Set);

			Assert.AreEqual(GraphNodeType.BlackboardVariable, node.NodeType);
		}

		[Test]
		public void TwoInstances_HaveUniqueIds()
		{
			var node1 = new BlackboardVariableNode();
			var node2 = new BlackboardVariableNode();

			Assert.AreNotEqual(node1.Id, node2.Id);
		}

		[Test]
		public void VariableKey_SetAndGet()
		{
			var node = new BlackboardVariableNode();

			node.VariableKey = "Mana";

			Assert.AreEqual("Mana", node.VariableKey);
		}

		[Test]
		public void Mode_SetAndGet()
		{
			var node = new BlackboardVariableNode();

			node.Mode = BlackboardNodeMode.Set;

			Assert.AreEqual(BlackboardNodeMode.Set, node.Mode);
		}

		[Test]
		public void VariableType_Set_UpdatesTypeAssemblyQualifiedName()
		{
			var node = new BlackboardVariableNode();

			node.VariableType = typeof(string);

			Assert.AreEqual(typeof(string).AssemblyQualifiedName, node.TypeAssemblyQualifiedName);
		}

		[Test]
		public void VariableType_SetNull_ClearsTypeAssemblyQualifiedName()
		{
			var node = new BlackboardVariableNode();
			node.VariableType = typeof(int);

			node.VariableType = null;

			Assert.IsNull(node.TypeAssemblyQualifiedName);
			Assert.IsNull(node.VariableType);
		}

		[Test]
		public void VariableType_Get_RestoresFromAssemblyQualifiedName()
		{
			var node = new BlackboardVariableNode();
			node.TypeAssemblyQualifiedName = typeof(int).AssemblyQualifiedName;

			var result = node.VariableType;

			Assert.AreEqual(typeof(int), result);
		}

		[Test]
		public void VariableType_Get_EmptyAssemblyQualifiedName_ReturnsNull()
		{
			var node = new BlackboardVariableNode();
			node.TypeAssemblyQualifiedName = "";

			Assert.IsNull(node.VariableType);
		}

		[Test]
		public void VariableType_Get_InvalidAssemblyQualifiedName_ReturnsNull()
		{
			var node = new BlackboardVariableNode();
			node.TypeAssemblyQualifiedName = "NonExistent.Type, FakeAssembly";

			Assert.IsNull(node.VariableType);
		}

		[Test]
		public void VariableType_Get_CachesResolvedType()
		{
			var node = new BlackboardVariableNode();
			node.TypeAssemblyQualifiedName = typeof(float).AssemblyQualifiedName;

			var first = node.VariableType;
			var second = node.VariableType;

			Assert.AreSame(first, second);
		}

		[Test]
		public void RestoreType_WithValidAssemblyQualifiedName_RestoresType()
		{
			var node = new BlackboardVariableNode();
			node.TypeAssemblyQualifiedName = typeof(bool).AssemblyQualifiedName;

			node.RestoreType();

			Assert.AreEqual(typeof(bool), node.VariableType);
		}

		[Test]
		public void RestoreType_WithNullAssemblyQualifiedName_VariableTypeRemainsNull()
		{
			var node = new BlackboardVariableNode();

			node.RestoreType();

			Assert.IsNull(node.VariableType);
		}

		[Test]
		public void RestoreType_WithEmptyAssemblyQualifiedName_VariableTypeRemainsNull()
		{
			var node = new BlackboardVariableNode();
			node.TypeAssemblyQualifiedName = "";

			node.RestoreType();

			Assert.IsNull(node.VariableType);
		}

		[Test]
		public void RestoreType_WithInvalidAssemblyQualifiedName_VariableTypeRemainsNull()
		{
			var node = new BlackboardVariableNode();
			node.TypeAssemblyQualifiedName = "Does.Not.Exist, NoAssembly";

			node.RestoreType();

			Assert.IsNull(node.VariableType);
		}

		[Test]
		public void Position_SetAndGet()
		{
			var node = new BlackboardVariableNode();

			node.Position = new Vector2(150f, 250f);

			Assert.AreEqual(new Vector2(150f, 250f), node.Position);
		}

		[Test]
		public void Comment_SetAndGet()
		{
			var node = new BlackboardVariableNode();

			node.Comment = "Player health variable";

			Assert.AreEqual("Player health variable", node.Comment);
		}

		[Test]
		public void DisplayName_CanBeOverwritten()
		{
			var node = new BlackboardVariableNode("Health", typeof(float), BlackboardNodeMode.Get);

			node.DisplayName = "HP";

			Assert.AreEqual("HP", node.DisplayName);
		}

		[Test]
		public void FullConstructor_WithUnityType_SetsVariableType()
		{
			var node = new BlackboardVariableNode("Offset", typeof(Vector3), BlackboardNodeMode.Get);

			Assert.AreEqual(typeof(Vector3), node.VariableType);
			Assert.AreEqual(typeof(Vector3).AssemblyQualifiedName, node.TypeAssemblyQualifiedName);
		}

		[Test]
		public void JsonSerialization_PreservesKeyAndMode()
		{
			var original = new BlackboardVariableNode("Speed", typeof(float), BlackboardNodeMode.Set);
			original.Id = "bb-node-001";
			original.Position = new Vector2(300f, 400f);

			var json = JsonUtility.ToJson(original);
			var restored = JsonUtility.FromJson<BlackboardVariableNode>(json);

			Assert.AreEqual("bb-node-001", restored.Id);
			Assert.AreEqual("Speed", restored.VariableKey);
			Assert.AreEqual(BlackboardNodeMode.Set, restored.Mode);
			Assert.AreEqual(new Vector2(300f, 400f), restored.Position);
		}

		[Test]
		public void JsonSerialization_PreservesTypeAssemblyQualifiedName()
		{
			var original = new BlackboardVariableNode("Count", typeof(int), BlackboardNodeMode.Get);

			var json = JsonUtility.ToJson(original);
			var restored = JsonUtility.FromJson<BlackboardVariableNode>(json);

			Assert.AreEqual(typeof(int).AssemblyQualifiedName, restored.TypeAssemblyQualifiedName);
		}

		[Test]
		public void JsonSerialization_RestoreType_ResolvesVariableType()
		{
			var original = new BlackboardVariableNode("IsAlive", typeof(bool), BlackboardNodeMode.Get);

			var json = JsonUtility.ToJson(original);
			var restored = JsonUtility.FromJson<BlackboardVariableNode>(json);
			restored.RestoreType();

			Assert.AreEqual(typeof(bool), restored.VariableType);
		}

		[Test]
		public void JsonSerialization_PreservesDisplayName()
		{
			var original = new BlackboardVariableNode("PlayerName", typeof(string), BlackboardNodeMode.Get);
			original.DisplayName = "Custom Label";

			var json = JsonUtility.ToJson(original);
			var restored = JsonUtility.FromJson<BlackboardVariableNode>(json);

			Assert.AreEqual("Custom Label", restored.DisplayName);
		}

		[Test]
		public void BlackboardNodeMode_GetAndSet_AreDifferentValues()
		{
			Assert.AreNotEqual(BlackboardNodeMode.Get, BlackboardNodeMode.Set);
		}

		[Test]
		public void FullConstructor_NullVariableType_TypeAssemblyQualifiedNameIsNull()
		{
			var node = new BlackboardVariableNode("Key", null, BlackboardNodeMode.Get);

			Assert.IsNull(node.VariableType);
			Assert.IsNull(node.TypeAssemblyQualifiedName);
		}

		[Test]
		public void Id_CanBeOverwritten()
		{
			var node = new BlackboardVariableNode();
			var originalId = node.Id;

			node.Id = "new-bb-id";

			Assert.AreNotEqual(originalId, node.Id);
			Assert.AreEqual("new-bb-id", node.Id);
		}
	}
}
