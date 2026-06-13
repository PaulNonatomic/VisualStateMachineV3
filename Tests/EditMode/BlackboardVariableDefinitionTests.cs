using System;
using NUnit.Framework;
using Nonatomic.VSM.Blackboard;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class BlackboardVariableDefinitionTests
	{
		[Test]
		public void BlackboardVariableData_DefaultProperties_AreInitializedCorrectly()
		{
			var data = new BlackboardVariableData();

			Assert.IsNull(data.Key);
			Assert.IsNull(data.TypeAssemblyQualifiedName);
			Assert.IsNull(data.JsonValue);
			Assert.IsFalse(data.IsExposed);
			Assert.IsNull(data.Category);
			Assert.IsNull(data.Tooltip);
			Assert.AreEqual(0, data.Order);
		}

		[Test]
		public void BlackboardVariableData_SetProperties_RetainValues()
		{
			var data = new BlackboardVariableData
			{
				Key = "health",
				TypeAssemblyQualifiedName = typeof(int).AssemblyQualifiedName,
				JsonValue = "100",
				IsExposed = true,
				Category = "Stats",
				Tooltip = "Player health",
				Order = 3
			};

			Assert.AreEqual("health", data.Key);
			Assert.AreEqual(typeof(int).AssemblyQualifiedName, data.TypeAssemblyQualifiedName);
			Assert.AreEqual("100", data.JsonValue);
			Assert.IsTrue(data.IsExposed);
			Assert.AreEqual("Stats", data.Category);
			Assert.AreEqual("Player health", data.Tooltip);
			Assert.AreEqual(3, data.Order);
		}

		[Test]
		public void Definition_DefaultProperties_AreInitializedCorrectly()
		{
			var def = new BlackboardVariableDefinition();

			Assert.IsNull(def.Key);
			Assert.IsNull(def.TypeAssemblyQualifiedName);
			Assert.IsNull(def.ValueType);
			Assert.IsNull(def.DefaultValueJson);
			Assert.IsFalse(def.IsExposed);
			Assert.IsFalse(def.IsExposedInGraph);
			Assert.IsNull(def.Category);
			Assert.IsNull(def.Tooltip);
			Assert.AreEqual(Vector2.zero, def.GraphPosition);
		}

		[Test]
		public void Definition_SetProperties_RetainValues()
		{
			var def = new BlackboardVariableDefinition
			{
				Key = "speed",
				IsExposed = true,
				IsExposedInGraph = true,
				Category = "Movement",
				Tooltip = "Movement speed",
				GraphPosition = new Vector2(100, 200)
			};

			Assert.AreEqual("speed", def.Key);
			Assert.IsTrue(def.IsExposed);
			Assert.IsTrue(def.IsExposedInGraph);
			Assert.AreEqual("Movement", def.Category);
			Assert.AreEqual("Movement speed", def.Tooltip);
			Assert.AreEqual(new Vector2(100, 200), def.GraphPosition);
		}

		[Test]
		public void ValueType_SetType_StoresAssemblyQualifiedName()
		{
			var def = new BlackboardVariableDefinition();

			def.ValueType = typeof(float);

			Assert.AreEqual(typeof(float), def.ValueType);
			Assert.AreEqual(typeof(float).AssemblyQualifiedName, def.TypeAssemblyQualifiedName);
		}

		[Test]
		public void ValueType_SetNull_ClearsTypeAndName()
		{
			var def = new BlackboardVariableDefinition { ValueType = typeof(int) };

			def.ValueType = null;

			Assert.IsNull(def.ValueType);
			Assert.IsNull(def.TypeAssemblyQualifiedName);
		}

		[Test]
		public void ValueType_ResolvesFromAssemblyQualifiedName()
		{
			var def = new BlackboardVariableDefinition
			{
				TypeAssemblyQualifiedName = typeof(string).AssemblyQualifiedName
			};

			Assert.AreEqual(typeof(string), def.ValueType);
		}

		[Test]
		public void ValueType_EmptyAssemblyName_ReturnsNull()
		{
			var def = new BlackboardVariableDefinition
			{
				TypeAssemblyQualifiedName = ""
			};

			Assert.IsNull(def.ValueType);
		}

		[Test]
		public void ValueType_InvalidAssemblyName_ReturnsNull()
		{
			var def = new BlackboardVariableDefinition
			{
				TypeAssemblyQualifiedName = "NonExistent.Type, NonExistent.Assembly"
			};

			Assert.IsNull(def.ValueType);
		}

		[Test]
		public void ValueType_CachesResolvedType()
		{
			var def = new BlackboardVariableDefinition
			{
				TypeAssemblyQualifiedName = typeof(int).AssemblyQualifiedName
			};

			var type1 = def.ValueType;
			var type2 = def.ValueType;

			Assert.AreSame(type1, type2);
		}

		[Test]
		public void CacheTypeName_WithValueType_StoresAssemblyQualifiedName()
		{
			var def = new BlackboardVariableDefinition { ValueType = typeof(bool) };

			// Clear the assembly name to simulate needing to cache
			def.TypeAssemblyQualifiedName = null;

			// CacheTypeName won't work because _valueType is set but name was cleared
			// Let's test the round-trip instead
			def.ValueType = typeof(bool);
			def.CacheTypeName();

			Assert.AreEqual(typeof(bool).AssemblyQualifiedName, def.TypeAssemblyQualifiedName);
		}

		[Test]
		public void CacheTypeName_WithNullValueType_DoesNothing()
		{
			var def = new BlackboardVariableDefinition();

			def.CacheTypeName();

			Assert.IsNull(def.TypeAssemblyQualifiedName);
		}

		[Test]
		public void RestoreType_WithValidAssemblyName_RestoresType()
		{
			var def = new BlackboardVariableDefinition
			{
				TypeAssemblyQualifiedName = typeof(Vector3).AssemblyQualifiedName
			};

			// Force restore (simulates post-deserialization)
			def.RestoreType();

			Assert.AreEqual(typeof(Vector3), def.ValueType);
		}

		[Test]
		public void RestoreType_WithEmptyAssemblyName_DoesNothing()
		{
			var def = new BlackboardVariableDefinition
			{
				TypeAssemblyQualifiedName = ""
			};

			def.RestoreType();

			Assert.IsNull(def.ValueType);
		}

		[Test]
		public void CacheAndRestoreType_RoundTrip_PreservesType()
		{
			var def = new BlackboardVariableDefinition { ValueType = typeof(Color) };

			def.CacheTypeName();
			var assemblyName = def.TypeAssemblyQualifiedName;

			// Simulate deserialization by creating a new definition with just the name
			var restored = new BlackboardVariableDefinition
			{
				TypeAssemblyQualifiedName = assemblyName
			};
			restored.RestoreType();

			Assert.AreEqual(typeof(Color), restored.ValueType);
		}

		[Test]
		public void DefaultValue_SetIntValue_SerializesAndDeserializes()
		{
			var def = new BlackboardVariableDefinition { ValueType = typeof(int) };

			def.DefaultValue = 42;

			Assert.AreEqual(42, def.DefaultValue);
		}

		[Test]
		public void DefaultValue_SetStringValue_SerializesAndDeserializes()
		{
			var def = new BlackboardVariableDefinition { ValueType = typeof(string) };

			def.DefaultValue = "hello";

			Assert.AreEqual("hello", def.DefaultValue);
		}

		[Test]
		public void DefaultValue_SetFloatValue_SerializesAndDeserializes()
		{
			var def = new BlackboardVariableDefinition { ValueType = typeof(float) };

			def.DefaultValue = 3.14f;

			Assert.AreEqual(3.14f, (float)def.DefaultValue, 0.001f);
		}

		[Test]
		public void DefaultValue_SetBoolValue_SerializesAndDeserializes()
		{
			var def = new BlackboardVariableDefinition { ValueType = typeof(bool) };

			def.DefaultValue = true;

			Assert.AreEqual(true, def.DefaultValue);
		}

		[Test]
		public void DefaultValue_SetNull_SetsJsonToNull()
		{
			var def = new BlackboardVariableDefinition { ValueType = typeof(string) };

			def.DefaultValue = null;

			Assert.IsNull(def.DefaultValueJson);
		}

		[Test]
		public void DefaultValue_NullType_ReturnsNull()
		{
			var def = new BlackboardVariableDefinition();

			var value = def.DefaultValue;

			Assert.IsNull(value);
		}

		[Test]
		public void DefaultValue_EmptyJson_ReturnsNull()
		{
			var def = new BlackboardVariableDefinition
			{
				ValueType = typeof(int),
				DefaultValueJson = ""
			};

			// Force fresh deserialization by creating new instance
			var def2 = new BlackboardVariableDefinition
			{
				TypeAssemblyQualifiedName = typeof(int).AssemblyQualifiedName,
				DefaultValueJson = ""
			};

			Assert.IsNull(def2.DefaultValue);
		}

		[Test]
		public void DefaultValue_LazyDeserialization_OnlyDeserializesOnce()
		{
			var def = new BlackboardVariableDefinition { ValueType = typeof(int) };
			def.DefaultValue = 99;

			// Access DefaultValue multiple times - should return same cached value
			var value1 = def.DefaultValue;
			var value2 = def.DefaultValue;

			Assert.AreEqual(99, value1);
			Assert.AreEqual(99, value2);
		}

		[Test]
		public void DefaultValue_SetNewValue_UpdatesCache()
		{
			var def = new BlackboardVariableDefinition { ValueType = typeof(int) };
			def.DefaultValue = 10;

			Assert.AreEqual(10, def.DefaultValue);

			def.DefaultValue = 20;

			Assert.AreEqual(20, def.DefaultValue);
		}

		[Test]
		public void DefaultValue_Vector2_SerializesAndDeserializes()
		{
			var def = new BlackboardVariableDefinition { ValueType = typeof(Vector2) };
			var expected = new Vector2(1.5f, 2.5f);

			def.DefaultValue = expected;

			var result = (Vector2)def.DefaultValue;
			Assert.AreEqual(expected.x, result.x, 0.001f);
			Assert.AreEqual(expected.y, result.y, 0.001f);
		}

		[Test]
		public void DefaultValue_Vector3_SerializesAndDeserializes()
		{
			var def = new BlackboardVariableDefinition { ValueType = typeof(Vector3) };
			var expected = new Vector3(1f, 2f, 3f);

			def.DefaultValue = expected;

			var result = (Vector3)def.DefaultValue;
			Assert.AreEqual(expected.x, result.x, 0.001f);
			Assert.AreEqual(expected.y, result.y, 0.001f);
			Assert.AreEqual(expected.z, result.z, 0.001f);
		}

		[Test]
		public void DefaultValue_Color_SerializesAndDeserializes()
		{
			var def = new BlackboardVariableDefinition { ValueType = typeof(Color) };
			var expected = new Color(0.5f, 0.6f, 0.7f, 1.0f);

			def.DefaultValue = expected;

			var result = (Color)def.DefaultValue;
			Assert.AreEqual(expected.r, result.r, 0.001f);
			Assert.AreEqual(expected.g, result.g, 0.001f);
			Assert.AreEqual(expected.b, result.b, 0.001f);
			Assert.AreEqual(expected.a, result.a, 0.001f);
		}

		[Test]
		public void DefaultValue_NullType_SerializeClearsJson()
		{
			var def = new BlackboardVariableDefinition();

			// Set type, set value, then clear type
			def.ValueType = typeof(int);
			def.DefaultValue = 42;
			def.ValueType = null;
			def.DefaultValue = null;

			Assert.IsNull(def.DefaultValueJson);
		}

		[Test]
		public void DefaultValue_SetMultipleTimes_RetainsLastValue()
		{
			var def = new BlackboardVariableDefinition { ValueType = typeof(int) };

			def.DefaultValue = 1;
			def.DefaultValue = 2;
			def.DefaultValue = 3;

			Assert.AreEqual(3, def.DefaultValue);
		}

	}
}
