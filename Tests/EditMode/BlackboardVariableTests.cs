using System;
using NUnit.Framework;
using Nonatomic.VSM.Blackboard;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class BlackboardVariableTests
	{

		[Test]
		public void Constructor_WithKey_SetsKeyAndDefaultValue()
		{
			var variable = new BlackboardVariable<int>("health");

			Assert.AreEqual("health", variable.Key);
			Assert.AreEqual(0, variable.Value);
		}

		[Test]
		public void Constructor_WithKeyAndValue_SetsBoth()
		{
			var variable = new BlackboardVariable<float>("speed", 5.5f);

			Assert.AreEqual("speed", variable.Key);
			Assert.AreEqual(5.5f, variable.Value, 0.001f);
		}

		[Test]
		public void Constructor_Default_HasNullKeyAndDefault()
		{
			var variable = new BlackboardVariable<int>();

			Assert.IsNull(variable.Key);
			Assert.AreEqual(0, variable.Value);
		}

		[Test]
		public void Constructor_StringType_DefaultsToNull()
		{
			var variable = new BlackboardVariable<string>("name");

			Assert.AreEqual("name", variable.Key);
			Assert.IsNull(variable.Value);
		}



		[Test]
		public void ValueType_Int_ReturnsIntType()
		{
			var variable = new BlackboardVariable<int>("test");

			Assert.AreEqual(typeof(int), variable.ValueType);
		}

		[Test]
		public void ValueType_String_ReturnsStringType()
		{
			var variable = new BlackboardVariable<string>("test");

			Assert.AreEqual(typeof(string), variable.ValueType);
		}

		[Test]
		public void ValueType_Bool_ReturnsBoolType()
		{
			var variable = new BlackboardVariable<bool>("test");

			Assert.AreEqual(typeof(bool), variable.ValueType);
		}

		[Test]
		public void ValueType_Vector3_ReturnsVector3Type()
		{
			var variable = new BlackboardVariable<Vector3>("pos");

			Assert.AreEqual(typeof(Vector3), variable.ValueType);
		}



		[Test]
		public void Value_SetAndGet_ReturnsCorrectValue()
		{
			var variable = new BlackboardVariable<int>("health");

			variable.Value = 42;

			Assert.AreEqual(42, variable.Value);
		}

		[Test]
		public void BoxedValue_ReturnsCurrentValue()
		{
			var variable = new BlackboardVariable<int>("health", 100);

			Assert.AreEqual(100, variable.BoxedValue);
		}

		[Test]
		public void BoxedValue_AfterSet_ReturnsUpdatedValue()
		{
			var variable = new BlackboardVariable<float>("speed", 1.0f);
			variable.Value = 9.8f;

			Assert.AreEqual(9.8f, (float)variable.BoxedValue, 0.001f);
		}

		[Test]
		public void BoxedValue_StringType_ReturnsString()
		{
			var variable = new BlackboardVariable<string>("name", "test");

			Assert.AreEqual("test", variable.BoxedValue);
		}



		[Test]
		public void SetValue_UpdatesValue()
		{
			var variable = new BlackboardVariable<int>("count", 0);

			variable.SetValue(99);

			Assert.AreEqual(99, variable.Value);
		}

		[Test]
		public void SetValue_StringToNull_SetsNull()
		{
			var variable = new BlackboardVariable<string>("name", "hello");

			variable.SetValue(null);

			Assert.IsNull(variable.Value);
		}



		[Test]
		public void SetBoxedValue_CorrectType_SetsValue()
		{
			var variable = new BlackboardVariable<int>("health");

			variable.SetBoxedValue(42);

			Assert.AreEqual(42, variable.Value);
		}

		[Test]
		public void SetBoxedValue_ConvertibleType_ConvertsAndSets()
		{
			var variable = new BlackboardVariable<float>("speed");

			variable.SetBoxedValue(10);

			Assert.AreEqual(10f, variable.Value, 0.001f);
		}

		[Test]
		public void SetBoxedValue_Null_SetsDefault()
		{
			var variable = new BlackboardVariable<int>("health", 100);

			variable.SetBoxedValue(null);

			Assert.AreEqual(0, variable.Value);
		}

		[Test]
		public void SetBoxedValue_IncompatibleType_SetsDefault()
		{
			var variable = new BlackboardVariable<int>("health", 50);

			variable.SetBoxedValue("not a number that can be parsed");

			Assert.AreEqual(0, variable.Value);
		}

		[Test]
		public void SetBoxedValue_DoubleToInt_Converts()
		{
			var variable = new BlackboardVariable<int>("count");

			variable.SetBoxedValue(3.7);

			Assert.AreEqual(3, variable.Value);
		}



		[Test]
		public void TryGetValue_CorrectType_ReturnsTrueWithValue()
		{
			var variable = new BlackboardVariable<int>("health", 100);

			var result = variable.TryGetValue<int>(out var value);

			Assert.IsTrue(result);
			Assert.AreEqual(100, value);
		}

		[Test]
		public void TryGetValue_WrongType_ReturnsFalseWithDefault()
		{
			var variable = new BlackboardVariable<int>("health", 100);

			var result = variable.TryGetValue<string>(out var value);

			Assert.IsFalse(result);
			Assert.IsNull(value);
		}

		[Test]
		public void TryGetValue_ObjectType_ReturnsTrueWithBoxed()
		{
			var variable = new BlackboardVariable<int>("health", 42);

			var result = variable.TryGetValue<object>(out var value);

			Assert.IsTrue(result);
			Assert.AreEqual(42, value);
		}



		[Test]
		public void Key_SetViaProperty_Updates()
		{
			var variable = new BlackboardVariable<int>("original");

			variable.Key = "renamed";

			Assert.AreEqual("renamed", variable.Key);
		}



		[Test]
		public void IsExposed_DefaultFalse()
		{
			var variable = new BlackboardVariable<int>("test");

			Assert.IsFalse(variable.IsExposed);
		}

		[Test]
		public void IsExposed_SetAndGet()
		{
			var variable = new BlackboardVariable<int>("test");

			variable.IsExposed = true;

			Assert.IsTrue(variable.IsExposed);
		}

		[Test]
		public void IsExposedInGraph_SetAndGet()
		{
			var variable = new BlackboardVariable<int>("test");

			variable.IsExposedInGraph = true;

			Assert.IsTrue(variable.IsExposedInGraph);
		}

		[Test]
		public void Category_SetAndGet()
		{
			var variable = new BlackboardVariable<int>("test");

			variable.Category = "Combat";

			Assert.AreEqual("Combat", variable.Category);
		}

		[Test]
		public void Tooltip_SetAndGet()
		{
			var variable = new BlackboardVariable<int>("test");

			variable.Tooltip = "Player health value";

			Assert.AreEqual("Player health value", variable.Tooltip);
		}

		[Test]
		public void Order_SetAndGet()
		{
			var variable = new BlackboardVariable<int>("test");

			variable.Order = 5;

			Assert.AreEqual(5, variable.Order);
		}



		[Test]
		public void Create_WithType_ReturnsCorrectTypedVariable()
		{
			var variable = BlackboardVariable.Create("health", typeof(int));

			Assert.IsNotNull(variable);
			Assert.AreEqual(typeof(int), variable.ValueType);
			Assert.AreEqual("health", variable.Key);
		}

		[Test]
		public void Create_WithValue_SetsValue()
		{
			var variable = BlackboardVariable.Create("speed", typeof(float), 3.14f);

			Assert.AreEqual(3.14f, (float)variable.BoxedValue, 0.001f);
		}

		[Test]
		public void Create_WithoutValue_UsesDefault()
		{
			var variable = BlackboardVariable.Create("count", typeof(int));

			Assert.AreEqual(0, variable.BoxedValue);
		}

		[Test]
		public void Create_StringType_Works()
		{
			var variable = BlackboardVariable.Create("name", typeof(string), "hello");

			Assert.AreEqual("hello", variable.BoxedValue);
			Assert.AreEqual(typeof(string), variable.ValueType);
		}

		[Test]
		public void Create_BoolType_Works()
		{
			var variable = BlackboardVariable.Create("flag", typeof(bool), true);

			Assert.AreEqual(true, variable.BoxedValue);
		}



		[Test]
		public void ToSerializedData_PreservesKey()
		{
			var variable = new BlackboardVariable<int>("health", 100);

			var data = variable.ToSerializedData();

			Assert.AreEqual("health", data.Key);
		}

		[Test]
		public void ToSerializedData_PreservesTypeInfo()
		{
			var variable = new BlackboardVariable<float>("speed", 5.0f);

			var data = variable.ToSerializedData();

			Assert.AreEqual(typeof(float).AssemblyQualifiedName, data.TypeAssemblyQualifiedName);
		}

		[Test]
		public void ToSerializedData_PreservesMetadata()
		{
			var variable = new BlackboardVariable<int>("health", 100);
			variable.IsExposed = true;
			variable.Category = "Stats";
			variable.Tooltip = "Current HP";
			variable.Order = 3;

			var data = variable.ToSerializedData();

			Assert.IsTrue(data.IsExposed);
			Assert.AreEqual("Stats", data.Category);
			Assert.AreEqual("Current HP", data.Tooltip);
			Assert.AreEqual(3, data.Order);
		}

		[Test]
		public void FromSerializedData_RestoresIntVariable()
		{
			var original = new BlackboardVariable<int>("health", 42);
			var data = original.ToSerializedData();

			var restored = BlackboardVariable.FromSerializedData(data);

			Assert.IsNotNull(restored);
			Assert.AreEqual("health", restored.Key);
			Assert.AreEqual(typeof(int), restored.ValueType);
			Assert.AreEqual(42, restored.BoxedValue);
		}

		[Test]
		public void FromSerializedData_RestoresFloatVariable()
		{
			var original = new BlackboardVariable<float>("speed", 9.81f);
			var data = original.ToSerializedData();

			var restored = BlackboardVariable.FromSerializedData(data);

			Assert.AreEqual(9.81f, (float)restored.BoxedValue, 0.01f);
		}

		[Test]
		public void FromSerializedData_RestoresStringVariable()
		{
			var original = new BlackboardVariable<string>("name", "TestPlayer");
			var data = original.ToSerializedData();

			var restored = BlackboardVariable.FromSerializedData(data);

			Assert.AreEqual("TestPlayer", restored.BoxedValue);
		}

		[Test]
		public void FromSerializedData_RestoresBoolVariable()
		{
			var original = new BlackboardVariable<bool>("alive", true);
			var data = original.ToSerializedData();

			var restored = BlackboardVariable.FromSerializedData(data);

			Assert.AreEqual(true, restored.BoxedValue);
		}

		[Test]
		public void FromSerializedData_RestoresMetadata()
		{
			var original = new BlackboardVariable<int>("health", 100);
			original.IsExposed = true;
			original.Category = "Stats";
			original.Tooltip = "HP";
			original.Order = 2;
			var data = original.ToSerializedData();

			var restored = BlackboardVariable.FromSerializedData(data);

			Assert.IsTrue(restored.IsExposed);
			Assert.AreEqual("Stats", restored.Category);
			Assert.AreEqual("HP", restored.Tooltip);
			Assert.AreEqual(2, restored.Order);
		}

		[Test]
		public void FromSerializedData_NullData_ReturnsNull()
		{
			var result = BlackboardVariable.FromSerializedData(null);

			Assert.IsNull(result);
		}

		[Test]
		public void FromSerializedData_EmptyTypeName_ReturnsNull()
		{
			var data = new BlackboardVariableData
			{
				Key = "test",
				TypeAssemblyQualifiedName = ""
			};

			var result = BlackboardVariable.FromSerializedData(data);

			Assert.IsNull(result);
		}

		[Test]
		public void FromSerializedData_InvalidTypeName_ReturnsNull()
		{
			var data = new BlackboardVariableData
			{
				Key = "test",
				TypeAssemblyQualifiedName = "NonExistent.Type, FakeAssembly"
			};

			var result = BlackboardVariable.FromSerializedData(data);

			Assert.IsNull(result);
		}



		[Test]
		public void BlackboardVariableData_Key_SetAndGet()
		{
			var data = new BlackboardVariableData();

			data.Key = "health";

			Assert.AreEqual("health", data.Key);
		}

		[Test]
		public void BlackboardVariableData_TypeAssemblyQualifiedName_SetAndGet()
		{
			var data = new BlackboardVariableData();

			data.TypeAssemblyQualifiedName = typeof(int).AssemblyQualifiedName;

			Assert.AreEqual(typeof(int).AssemblyQualifiedName, data.TypeAssemblyQualifiedName);
		}

		[Test]
		public void BlackboardVariableData_JsonValue_SetAndGet()
		{
			var data = new BlackboardVariableData();

			data.JsonValue = "42";

			Assert.AreEqual("42", data.JsonValue);
		}

		[Test]
		public void BlackboardVariableData_IsExposed_SetAndGet()
		{
			var data = new BlackboardVariableData();

			data.IsExposed = true;

			Assert.IsTrue(data.IsExposed);
		}

		[Test]
		public void BlackboardVariableData_Category_SetAndGet()
		{
			var data = new BlackboardVariableData();

			data.Category = "Combat";

			Assert.AreEqual("Combat", data.Category);
		}

		[Test]
		public void BlackboardVariableData_Tooltip_SetAndGet()
		{
			var data = new BlackboardVariableData();

			data.Tooltip = "Help text";

			Assert.AreEqual("Help text", data.Tooltip);
		}

		[Test]
		public void BlackboardVariableData_Order_SetAndGet()
		{
			var data = new BlackboardVariableData();

			data.Order = 7;

			Assert.AreEqual(7, data.Order);
		}

		[Test]
		public void BlackboardVariableData_DefaultValues()
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
		public void Definition_Key_SetAndGet()
		{
			var definition = new BlackboardVariableDefinition();

			definition.Key = "health";

			Assert.AreEqual("health", definition.Key);
		}

		[Test]
		public void Definition_ValueType_SetSetsAssemblyQualifiedName()
		{
			var definition = new BlackboardVariableDefinition();

			definition.ValueType = typeof(float);

			Assert.AreEqual(typeof(float), definition.ValueType);
			Assert.AreEqual(typeof(float).AssemblyQualifiedName, definition.TypeAssemblyQualifiedName);
		}

		[Test]
		public void Definition_ValueType_SetNull_ClearsName()
		{
			var definition = new BlackboardVariableDefinition();
			definition.ValueType = typeof(int);

			definition.ValueType = null;

			Assert.IsNull(definition.ValueType);
			Assert.IsNull(definition.TypeAssemblyQualifiedName);
		}

		[Test]
		public void Definition_ValueType_ResolvesFromAssemblyQualifiedName()
		{
			var definition = new BlackboardVariableDefinition();
			definition.TypeAssemblyQualifiedName = typeof(int).AssemblyQualifiedName;

			var type = definition.ValueType;

			Assert.AreEqual(typeof(int), type);
		}

		[Test]
		public void Definition_DefaultValue_SetAndGet_Float()
		{
			var definition = new BlackboardVariableDefinition();
			definition.ValueType = typeof(float);

			definition.DefaultValue = 3.14f;

			Assert.AreEqual(3.14f, (float)definition.DefaultValue, 0.001f);
		}

		[Test]
		public void Definition_DefaultValue_SetAndGet_String()
		{
			var definition = new BlackboardVariableDefinition();
			definition.ValueType = typeof(string);

			definition.DefaultValue = "hello";

			Assert.AreEqual("hello", definition.DefaultValue);
		}

		[Test]
		public void Definition_DefaultValue_SetAndGet_Int()
		{
			var definition = new BlackboardVariableDefinition();
			definition.ValueType = typeof(int);

			definition.DefaultValue = 42;

			Assert.AreEqual(42, definition.DefaultValue);
		}

		[Test]
		public void Definition_IsExposed_SetAndGet()
		{
			var definition = new BlackboardVariableDefinition();

			definition.IsExposed = true;

			Assert.IsTrue(definition.IsExposed);
		}

		[Test]
		public void Definition_IsExposedInGraph_SetAndGet()
		{
			var definition = new BlackboardVariableDefinition();

			definition.IsExposedInGraph = true;

			Assert.IsTrue(definition.IsExposedInGraph);
		}

		[Test]
		public void Definition_Category_SetAndGet()
		{
			var definition = new BlackboardVariableDefinition();

			definition.Category = "Movement";

			Assert.AreEqual("Movement", definition.Category);
		}

		[Test]
		public void Definition_Tooltip_SetAndGet()
		{
			var definition = new BlackboardVariableDefinition();

			definition.Tooltip = "Movement speed";

			Assert.AreEqual("Movement speed", definition.Tooltip);
		}

		[Test]
		public void Definition_GraphPosition_SetAndGet()
		{
			var definition = new BlackboardVariableDefinition();

			definition.GraphPosition = new Vector2(100, 200);

			Assert.AreEqual(new Vector2(100, 200), definition.GraphPosition);
		}

		[Test]
		public void Definition_RestoreType_ResolvesFromStoredName()
		{
			var definition = new BlackboardVariableDefinition();
			definition.TypeAssemblyQualifiedName = typeof(bool).AssemblyQualifiedName;

			definition.RestoreType();

			Assert.AreEqual(typeof(bool), definition.ValueType);
		}

		[Test]
		public void Definition_RestoreType_NullName_DoesNotThrow()
		{
			var definition = new BlackboardVariableDefinition();

			Assert.DoesNotThrow(() => definition.RestoreType());
		}

		[Test]
		public void Definition_CacheTypeName_StoresCurrentType()
		{
			var definition = new BlackboardVariableDefinition();
			definition.ValueType = typeof(double);

			definition.TypeAssemblyQualifiedName = null;
			definition.CacheTypeName();

			Assert.AreEqual(typeof(double).AssemblyQualifiedName, definition.TypeAssemblyQualifiedName);
		}

		[Test]
		public void Definition_DefaultValue_NullType_ReturnsNull()
		{
			var definition = new BlackboardVariableDefinition();

			Assert.IsNull(definition.DefaultValue);
		}

		[Test]
		public void Definition_DefaultValue_SetNull_ClearsJson()
		{
			var definition = new BlackboardVariableDefinition();
			definition.ValueType = typeof(int);
			definition.DefaultValue = 42;

			definition.DefaultValue = null;

			Assert.IsNull(definition.DefaultValueJson);
		}

		[Test]
		public void ValueEquals_SameIntValue_ReturnsTrue()
		{
			var variable = new BlackboardVariable<int>("health", 100);

			Assert.IsTrue(variable.ValueEquals(100));
		}

		[Test]
		public void ValueEquals_DifferentIntValue_ReturnsFalse()
		{
			var variable = new BlackboardVariable<int>("health", 100);

			Assert.IsFalse(variable.ValueEquals(50));
		}

		[Test]
		public void ValueEquals_SameStringValue_ReturnsTrue()
		{
			var variable = new BlackboardVariable<string>("name", "Alice");

			Assert.IsTrue(variable.ValueEquals("Alice"));
		}

		[Test]
		public void ValueEquals_DifferentStringValue_ReturnsFalse()
		{
			var variable = new BlackboardVariable<string>("name", "Alice");

			Assert.IsFalse(variable.ValueEquals("Bob"));
		}

		[Test]
		public void ValueEquals_NullStringValues_ReturnsTrue()
		{
			var variable = new BlackboardVariable<string>("name");

			Assert.IsTrue(variable.ValueEquals(null));
		}

		[Test]
		public void ValueEquals_NullVsNonNull_ReturnsFalse()
		{
			var variable = new BlackboardVariable<string>("name");

			Assert.IsFalse(variable.ValueEquals("Alice"));
		}

		[Test]
		public void ValueEquals_SameFloatValue_ReturnsTrue()
		{
			var variable = new BlackboardVariable<float>("speed", 5.5f);

			Assert.IsTrue(variable.ValueEquals(5.5f));
		}

		[Test]
		public void ValueEquals_SameBoolValue_ReturnsTrue()
		{
			var variable = new BlackboardVariable<bool>("flag", true);

			Assert.IsTrue(variable.ValueEquals(true));
		}

		[Test]
		public void ValueEquals_DefaultIntValue_ReturnsTrue()
		{
			var variable = new BlackboardVariable<int>("counter");

			Assert.IsTrue(variable.ValueEquals(0));
		}

	}
}
