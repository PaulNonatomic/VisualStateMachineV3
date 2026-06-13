using System;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class StateNodeTests
	{

		private class SimpleState : State
		{
			public override void OnEnter() { }
		}

		private class AnotherState : State
		{
			[DataIn("Speed")] public float speed;
			[DataOut("Result")] public int result;
			[Transition("Done")] public Action onDone;

			public override void OnEnter() { }
		}

		private class NotAState
		{
		}



		[Test]
		public void DefaultConstructor_GeneratesUniqueId()
		{
			var node = new StateNode();

			Assert.IsNotNull(node.Id);
			Assert.IsNotEmpty(node.Id);
		}

		[Test]
		public void DefaultConstructor_TwoNodes_HaveDifferentIds()
		{
			var node1 = new StateNode();
			var node2 = new StateNode();

			Assert.AreNotEqual(node1.Id, node2.Id);
		}

		[Test]
		public void StringConstructor_SetsId()
		{
			var node = new StateNode("my-id");

			Assert.AreEqual("my-id", node.Id);
		}

		[Test]
		public void StringConstructor_NullId_GeneratesId()
		{
			var node = new StateNode((string)null);

			Assert.IsNotNull(node.Id);
			Assert.IsNotEmpty(node.Id);
		}

		[Test]
		public void TypeConstructor_SetsStateType()
		{
			var node = new StateNode(typeof(SimpleState));

			Assert.AreEqual(typeof(SimpleState), node.StateType);
		}

		[Test]
		public void TypeConstructor_SetsDisplayNameToTypeName()
		{
			var node = new StateNode(typeof(SimpleState));

			Assert.AreEqual("SimpleState", node.DisplayName);
		}

		[Test]
		public void TypeConstructor_SetsAssemblyQualifiedName()
		{
			var node = new StateNode(typeof(SimpleState));

			Assert.AreEqual(typeof(SimpleState).AssemblyQualifiedName, node.StateTypeAssemblyQualifiedName);
		}

		[Test]
		public void TypeConstructor_NullType_SetsDisplayNameToUnknown()
		{
			var node = new StateNode((Type)null);

			Assert.AreEqual("Unknown State", node.DisplayName);
			Assert.IsNull(node.StateType);
		}



		[Test]
		public void NodeType_ReturnsState()
		{
			var node = new StateNode();

			Assert.AreEqual(GraphNodeType.State, node.NodeType);
		}



		[Test]
		public void StateType_SetType_UpdatesAssemblyQualifiedName()
		{
			var node = new StateNode();

			node.StateType = typeof(AnotherState);

			Assert.AreEqual(typeof(AnotherState).AssemblyQualifiedName, node.StateTypeAssemblyQualifiedName);
		}

		[Test]
		public void StateType_SetNull_ClearsAssemblyQualifiedName()
		{
			var node = new StateNode(typeof(SimpleState));

			node.StateType = null;

			Assert.IsNull(node.StateTypeAssemblyQualifiedName);
			Assert.IsNull(node.StateType);
		}

		[Test]
		public void StateType_LazyRestoresFromAssemblyQualifiedName()
		{
			var node = new StateNode();
			node.StateTypeAssemblyQualifiedName = typeof(SimpleState).AssemblyQualifiedName;

			// Accessing StateType should trigger lazy RestoreType
			var stateType = node.StateType;

			Assert.AreEqual(typeof(SimpleState), stateType);
		}

		[Test]
		public void StateType_EmptyAssemblyQualifiedName_ReturnsNull()
		{
			var node = new StateNode();
			node.StateTypeAssemblyQualifiedName = "";

			Assert.IsNull(node.StateType);
		}



		[Test]
		public void RestoreType_ValidTypeName_RestoresType()
		{
			var node = new StateNode();
			node.StateTypeAssemblyQualifiedName = typeof(SimpleState).AssemblyQualifiedName;

			node.RestoreType();

			Assert.AreEqual(typeof(SimpleState), node.StateType);
		}

		[Test]
		public void RestoreType_InvalidTypeName_LeavesTypeNull()
		{
			var node = new StateNode();
			node.StateTypeAssemblyQualifiedName = "NonExistent.Type, NonExistent.Assembly";

			node.RestoreType();

			// Access the backing field indirectly by checking the property doesn't throw
			Assert.IsNull(node.StateType);
		}

		[Test]
		public void RestoreType_NonStateType_SetsTypeToNull()
		{
			var node = new StateNode();
			node.StateTypeAssemblyQualifiedName = typeof(NotAState).AssemblyQualifiedName;

			node.RestoreType();

			// RestoreType validates the type inherits from State
			Assert.IsNull(node.StateType);
		}

		[Test]
		public void RestoreType_NullAssemblyQualifiedName_DoesNotThrow()
		{
			var node = new StateNode();
			node.StateTypeAssemblyQualifiedName = null;

			Assert.DoesNotThrow(() => node.RestoreType());
		}

		[Test]
		public void RestoreType_EmptyAssemblyQualifiedName_DoesNotThrow()
		{
			var node = new StateNode();
			node.StateTypeAssemblyQualifiedName = "";

			Assert.DoesNotThrow(() => node.RestoreType());
		}



		[Test]
		public void CacheTypeName_WithStateType_StoresAssemblyQualifiedName()
		{
			var node = new StateNode(typeof(SimpleState));
			// Clear the AQN to simulate deserialization scenario
			node.StateTypeAssemblyQualifiedName = null;

			node.CacheTypeName();

			Assert.AreEqual(typeof(SimpleState).AssemblyQualifiedName, node.StateTypeAssemblyQualifiedName);
		}

		[Test]
		public void CacheTypeName_WithoutStateType_DoesNotModifyAQN()
		{
			var node = new StateNode();
			node.StateTypeAssemblyQualifiedName = "original";

			node.CacheTypeName();

			// No _stateType set, so AQN should remain unchanged
			Assert.AreEqual("original", node.StateTypeAssemblyQualifiedName);
		}



		[Test]
		public void SetPropertyOverride_AddsNewOverride()
		{
			var node = new StateNode(typeof(AnotherState));

			node.SetPropertyOverride("speed", 42.5f, typeof(float));

			Assert.AreEqual(1, node.PropertyOverrides.Count);
		}

		[Test]
		public void SetPropertyOverride_SameFieldTwice_UpdatesExisting()
		{
			var node = new StateNode(typeof(AnotherState));

			node.SetPropertyOverride("speed", 10f, typeof(float));
			node.SetPropertyOverride("speed", 20f, typeof(float));

			Assert.AreEqual(1, node.PropertyOverrides.Count);
			var value = node.GetPropertyOverride("speed").GetValue();
			Assert.AreEqual(20f, (float)value, 0.001f);
		}

		[Test]
		public void SetPropertyOverride_MultipleFields_AddsAll()
		{
			var node = new StateNode(typeof(AnotherState));

			node.SetPropertyOverride("speed", 10f, typeof(float));
			node.SetPropertyOverride("result", 42, typeof(int));

			Assert.AreEqual(2, node.PropertyOverrides.Count);
		}

		[Test]
		public void GetPropertyOverride_ExistingField_ReturnsOverride()
		{
			var node = new StateNode(typeof(AnotherState));
			node.SetPropertyOverride("speed", 55.5f, typeof(float));

			var result = node.GetPropertyOverride("speed");

			Assert.IsNotNull(result);
			Assert.AreEqual("speed", result.FieldName);
		}

		[Test]
		public void GetPropertyOverride_NonExistentField_ReturnsNull()
		{
			var node = new StateNode(typeof(AnotherState));

			var result = node.GetPropertyOverride("nonExistent");

			Assert.IsNull(result);
		}

		[Test]
		public void RemovePropertyOverride_ExistingField_RemovesIt()
		{
			var node = new StateNode(typeof(AnotherState));
			node.SetPropertyOverride("speed", 10f, typeof(float));
			Assert.AreEqual(1, node.PropertyOverrides.Count);

			node.RemovePropertyOverride("speed");

			Assert.AreEqual(0, node.PropertyOverrides.Count);
		}

		[Test]
		public void RemovePropertyOverride_NonExistentField_DoesNotThrow()
		{
			var node = new StateNode(typeof(AnotherState));

			Assert.DoesNotThrow(() => node.RemovePropertyOverride("nonExistent"));
		}

		[Test]
		public void PropertyOverrides_InitiallyEmpty()
		{
			var node = new StateNode();

			Assert.IsNotNull(node.PropertyOverrides);
			Assert.AreEqual(0, node.PropertyOverrides.Count);
		}



		[Test]
		public void IsEntryState_DefaultsFalse()
		{
			var node = new StateNode();

			Assert.IsFalse(node.IsEntryState);
		}

		[Test]
		public void IsEntryState_CanBeSet()
		{
			var node = new StateNode();

			node.IsEntryState = true;

			Assert.IsTrue(node.IsEntryState);
		}



		[Test]
		public void Position_DefaultsToZero()
		{
			var node = new StateNode();

			Assert.AreEqual(Vector2.zero, node.Position);
		}

		[Test]
		public void Position_CanBeSet()
		{
			var node = new StateNode();
			var pos = new Vector2(100, 200);

			node.Position = pos;

			Assert.AreEqual(pos, node.Position);
		}

		[Test]
		public void Comment_DefaultsToNull()
		{
			var node = new StateNode();

			Assert.IsNull(node.Comment);
		}

		[Test]
		public void Comment_CanBeSet()
		{
			var node = new StateNode();

			node.Comment = "This is a test node";

			Assert.AreEqual("This is a test node", node.Comment);
		}



		[Test]
		public void SerializedPropertyOverride_FloatRoundTrip()
		{
			var po = new SerializedPropertyOverride("field", 3.14f, typeof(float));

			var result = po.GetValue();

			Assert.IsInstanceOf<float>(result);
			Assert.AreEqual(3.14f, (float)result, 0.001f);
		}

		[Test]
		public void SerializedPropertyOverride_IntRoundTrip()
		{
			var po = new SerializedPropertyOverride("field", 42, typeof(int));

			var result = po.GetValue();

			Assert.IsInstanceOf<int>(result);
			Assert.AreEqual(42, (int)result);
		}

		[Test]
		public void SerializedPropertyOverride_BoolRoundTrip()
		{
			var po = new SerializedPropertyOverride("field", true, typeof(bool));

			var result = po.GetValue();

			Assert.IsInstanceOf<bool>(result);
			Assert.AreEqual(true, (bool)result);
		}

		[Test]
		public void SerializedPropertyOverride_StringRoundTrip()
		{
			var po = new SerializedPropertyOverride("field", "hello world", typeof(string));

			var result = po.GetValue();

			Assert.IsInstanceOf<string>(result);
			Assert.AreEqual("hello world", (string)result);
		}

		[Test]
		public void SerializedPropertyOverride_DoubleRoundTrip()
		{
			var po = new SerializedPropertyOverride("field", 2.718281828, typeof(double));

			var result = po.GetValue();

			Assert.IsInstanceOf<double>(result);
			Assert.AreEqual(2.718281828, (double)result, 0.000001);
		}

		[Test]
		public void SerializedPropertyOverride_LongRoundTrip()
		{
			var po = new SerializedPropertyOverride("field", 9876543210L, typeof(long));

			var result = po.GetValue();

			Assert.IsInstanceOf<long>(result);
			Assert.AreEqual(9876543210L, (long)result);
		}

		[Test]
		public void SerializedPropertyOverride_NullValue_ReturnsNull()
		{
			var po = new SerializedPropertyOverride("field", null, typeof(string));

			var result = po.GetValue();

			Assert.IsNull(result);
		}

		[Test]
		public void SerializedPropertyOverride_SetValue_UpdatesExistingOverride()
		{
			var po = new SerializedPropertyOverride("field", 10f, typeof(float));

			po.SetValue(20f, typeof(float));

			var result = po.GetValue();
			Assert.AreEqual(20f, (float)result, 0.001f);
		}

		[Test]
		public void SerializedPropertyOverride_EmptyJsonValue_ReturnsNull()
		{
			var po = new SerializedPropertyOverride();
			po.FieldName = "field";
			po.JsonValue = "";
			po.TypeAssemblyQualifiedName = typeof(float).AssemblyQualifiedName;

			var result = po.GetValue();

			Assert.IsNull(result);
		}

		[Test]
		public void SerializedPropertyOverride_EmptyTypeAQN_ReturnsNull()
		{
			var po = new SerializedPropertyOverride();
			po.FieldName = "field";
			po.JsonValue = "42";
			po.TypeAssemblyQualifiedName = "";

			var result = po.GetValue();

			Assert.IsNull(result);
		}

		[Test]
		public void SerializedPropertyOverride_InvalidTypeAQN_ReturnsNull()
		{
			var po = new SerializedPropertyOverride();
			po.FieldName = "field";
			po.JsonValue = "42";
			po.TypeAssemblyQualifiedName = "NonExistent.Type, NonExistent.Assembly";

			var result = po.GetValue();

			Assert.IsNull(result);
		}

		[Test]
		public void SerializedPropertyOverride_Vector3RoundTrip()
		{
			var vec = new Vector3(1.5f, 2.5f, 3.5f);
			var po = new SerializedPropertyOverride("field", vec, typeof(Vector3));

			var result = po.GetValue();

			Assert.IsInstanceOf<Vector3>(result);
			var resultVec = (Vector3)result;
			Assert.AreEqual(1.5f, resultVec.x, 0.001f);
			Assert.AreEqual(2.5f, resultVec.y, 0.001f);
			Assert.AreEqual(3.5f, resultVec.z, 0.001f);
		}

		[Test]
		public void SerializedPropertyOverride_BoolFalse_RoundTrip()
		{
			var po = new SerializedPropertyOverride("field", false, typeof(bool));

			var result = po.GetValue();

			Assert.IsInstanceOf<bool>(result);
			Assert.AreEqual(false, (bool)result);
		}

		[Test]
		public void SerializedPropertyOverride_NegativeFloat_RoundTrip()
		{
			var po = new SerializedPropertyOverride("field", -99.99f, typeof(float));

			var result = po.GetValue();

			Assert.IsInstanceOf<float>(result);
			Assert.AreEqual(-99.99f, (float)result, 0.01f);
		}

	}
}
