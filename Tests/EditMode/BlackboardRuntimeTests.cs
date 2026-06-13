using System;
using NUnit.Framework;
using Nonatomic.VSM.Blackboard;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class BlackboardRuntimeTests
	{

		[Test]
		public void BlackboardVariable_Create_CreatesCorrectType()
		{
			var variable = BlackboardVariable.Create("test", typeof(int), 42);

			Assert.IsNotNull(variable);
			Assert.AreEqual("test", variable.Key);
			Assert.AreEqual(typeof(int), variable.ValueType);
			Assert.AreEqual(42, variable.BoxedValue);
		}

		[Test]
		public void BlackboardVariable_Create_WithNullValue_CreatesWithDefault()
		{
			var variable = BlackboardVariable.Create("test", typeof(int));

			Assert.IsNotNull(variable);
			Assert.AreEqual(0, variable.BoxedValue);
		}

		[Test]
		public void BlackboardVariable_Create_SupportsStringType()
		{
			var variable = BlackboardVariable.Create("name", typeof(string), "TestValue");

			Assert.AreEqual(typeof(string), variable.ValueType);
			Assert.AreEqual("TestValue", variable.BoxedValue);
		}

		[Test]
		public void BlackboardVariable_Create_SupportsFloatType()
		{
			var variable = BlackboardVariable.Create("speed", typeof(float), 5.5f);

			Assert.AreEqual(typeof(float), variable.ValueType);
			Assert.AreEqual(5.5f, variable.BoxedValue);
		}

		[Test]
		public void BlackboardVariable_Create_SupportsBoolType()
		{
			var variable = BlackboardVariable.Create("active", typeof(bool), true);

			Assert.AreEqual(typeof(bool), variable.ValueType);
			Assert.AreEqual(true, variable.BoxedValue);
		}

		[Test]
		public void BlackboardVariable_Create_SupportsVector3Type()
		{
			var expected = new Vector3(1, 2, 3);
			var variable = BlackboardVariable.Create("position", typeof(Vector3), expected);

			Assert.AreEqual(typeof(Vector3), variable.ValueType);
			Assert.AreEqual(expected, variable.BoxedValue);
		}

		[Test]
		public void BlackboardVariableGeneric_Constructor_SetsKeyAndValue()
		{
			var variable = new BlackboardVariable<int>("health", 100);

			Assert.AreEqual("health", variable.Key);
			Assert.AreEqual(100, variable.Value);
		}

		[Test]
		public void BlackboardVariableGeneric_ConstructorKeyOnly_SetsDefault()
		{
			var variable = new BlackboardVariable<int>("counter");

			Assert.AreEqual("counter", variable.Key);
			Assert.AreEqual(0, variable.Value);
		}

		[Test]
		public void BlackboardVariableGeneric_SetValue_UpdatesValue()
		{
			var variable = new BlackboardVariable<int>("test", 10);

			variable.SetValue(20);

			Assert.AreEqual(20, variable.Value);
			Assert.AreEqual(20, variable.BoxedValue);
		}

		[Test]
		public void BlackboardVariableGeneric_SetBoxedValue_UpdatesValue()
		{
			var variable = new BlackboardVariable<int>("test", 10);

			variable.SetBoxedValue(30);

			Assert.AreEqual(30, variable.Value);
		}

		[Test]
		public void BlackboardVariableGeneric_SetBoxedValue_WithConvertibleType_Converts()
		{
			var variable = new BlackboardVariable<float>("test", 1.0f);

			variable.SetBoxedValue(5); // int to float

			Assert.AreEqual(5.0f, variable.Value);
		}

		[Test]
		public void BlackboardVariableGeneric_SetBoxedValue_WithNull_SetsDefault()
		{
			var variable = new BlackboardVariable<int>("test", 100);

			variable.SetBoxedValue(null);

			Assert.AreEqual(0, variable.Value);
		}

		[Test]
		public void BlackboardVariableGeneric_TryGetValue_CorrectType_ReturnsTrue()
		{
			var variable = new BlackboardVariable<int>("test", 42);

			var success = variable.TryGetValue<int>(out var value);

			Assert.IsTrue(success);
			Assert.AreEqual(42, value);
		}

		[Test]
		public void BlackboardVariableGeneric_TryGetValue_WrongType_ReturnsFalse()
		{
			var variable = new BlackboardVariable<int>("test", 42);

			var success = variable.TryGetValue<string>(out var value);

			Assert.IsFalse(success);
			Assert.IsNull(value);
		}

		[Test]
		public void BlackboardVariable_MetadataProperties_CanBeSet()
		{
			var variable = new BlackboardVariable<int>("test", 0);

			variable.IsExposed = true;
			variable.IsExposedInGraph = true;
			variable.Category = "Stats";
			variable.Tooltip = "Player health";
			variable.Order = 5;

			Assert.IsTrue(variable.IsExposed);
			Assert.IsTrue(variable.IsExposedInGraph);
			Assert.AreEqual("Stats", variable.Category);
			Assert.AreEqual("Player health", variable.Tooltip);
			Assert.AreEqual(5, variable.Order);
		}

		[Test]
		public void BlackboardVariable_Key_CanBeChanged()
		{
			var variable = new BlackboardVariable<int>("original", 10);

			variable.Key = "renamed";

			Assert.AreEqual("renamed", variable.Key);
		}



		[Test]
		public void BlackboardVariable_ToSerializedData_CreatesValidData()
		{
			var variable = new BlackboardVariable<int>("health", 100)
			{
				IsExposed = true,
				Category = "Stats",
				Tooltip = "Player health",
				Order = 1
			};

			var data = variable.ToSerializedData();

			Assert.IsNotNull(data);
			Assert.AreEqual("health", data.Key);
			Assert.IsTrue(data.TypeAssemblyQualifiedName.Contains("Int32"));
			Assert.IsTrue(data.IsExposed);
			Assert.AreEqual("Stats", data.Category);
			Assert.AreEqual("Player health", data.Tooltip);
			Assert.AreEqual(1, data.Order);
		}

		[Test]
		public void BlackboardVariable_FromSerializedData_RestoresVariable()
		{
			var original = new BlackboardVariable<float>("speed", 5.5f)
			{
				IsExposed = true,
				Category = "Movement"
			};

			var data = original.ToSerializedData();
			var restored = BlackboardVariable.FromSerializedData(data);

			Assert.IsNotNull(restored);
			Assert.AreEqual("speed", restored.Key);
			Assert.AreEqual(typeof(float), restored.ValueType);
			Assert.AreEqual(5.5f, (float)restored.BoxedValue, 0.001f);
			Assert.IsTrue(restored.IsExposed);
			Assert.AreEqual("Movement", restored.Category);
		}

		[Test]
		public void BlackboardVariable_FromSerializedData_WithNullData_ReturnsNull()
		{
			var result = BlackboardVariable.FromSerializedData(null);

			Assert.IsNull(result);
		}

		[Test]
		public void BlackboardVariable_FromSerializedData_WithEmptyTypeName_ReturnsNull()
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
		public void BlackboardVariable_RoundTrip_PreservesStringValue()
		{
			var original = new BlackboardVariable<string>("name", "TestPlayer");
			var data = original.ToSerializedData();
			var restored = BlackboardVariable.FromSerializedData(data);

			Assert.AreEqual("TestPlayer", restored.BoxedValue);
		}

		[Test]
		public void BlackboardVariable_RoundTrip_PreservesBoolValue()
		{
			var original = new BlackboardVariable<bool>("active", true);
			var data = original.ToSerializedData();
			var restored = BlackboardVariable.FromSerializedData(data);

			Assert.AreEqual(true, restored.BoxedValue);
		}

		[Test]
		public void BlackboardVariable_RoundTrip_PreservesVector3Value()
		{
			var expected = new Vector3(1.5f, 2.5f, 3.5f);
			var original = new BlackboardVariable<Vector3>("position", expected);
			var data = original.ToSerializedData();
			var restored = BlackboardVariable.FromSerializedData(data);

			Assert.AreEqual(expected, restored.BoxedValue);
		}



		[Test]
		public void BlackboardVariableData_Properties_CanBeSetAndGet()
		{
			var data = new BlackboardVariableData
			{
				Key = "testKey",
				TypeAssemblyQualifiedName = typeof(int).AssemblyQualifiedName,
				JsonValue = "42",
				IsExposed = true,
				Category = "TestCategory",
				Tooltip = "TestTooltip",
				Order = 10
			};

			Assert.AreEqual("testKey", data.Key);
			Assert.IsTrue(data.TypeAssemblyQualifiedName.Contains("Int32"));
			Assert.AreEqual("42", data.JsonValue);
			Assert.IsTrue(data.IsExposed);
			Assert.AreEqual("TestCategory", data.Category);
			Assert.AreEqual("TestTooltip", data.Tooltip);
			Assert.AreEqual(10, data.Order);
		}



		[Test]
		public void BlackboardVariableDefinition_ValueType_SetAndGet()
		{
			var definition = new BlackboardVariableDefinition
			{
				Key = "health",
				ValueType = typeof(int)
			};

			Assert.AreEqual(typeof(int), definition.ValueType);
			Assert.IsTrue(definition.TypeAssemblyQualifiedName.Contains("Int32"));
		}

		[Test]
		public void BlackboardVariableDefinition_RestoreType_RestoresFromAssemblyQualifiedName()
		{
			var definition = new BlackboardVariableDefinition
			{
				Key = "test",
				TypeAssemblyQualifiedName = typeof(float).AssemblyQualifiedName
			};

			definition.RestoreType();

			Assert.AreEqual(typeof(float), definition.ValueType);
		}

		[Test]
		public void BlackboardVariableDefinition_CacheTypeName_StoresTypeName()
		{
			var definition = new BlackboardVariableDefinition
			{
				Key = "test",
				ValueType = typeof(string)
			};

			definition.CacheTypeName();

			Assert.IsTrue(definition.TypeAssemblyQualifiedName.Contains("String"));
		}

		[Test]
		public void BlackboardVariableDefinition_DefaultValue_SetAndGet_Int()
		{
			var definition = new BlackboardVariableDefinition
			{
				Key = "health",
				ValueType = typeof(int),
				DefaultValue = 100
			};

			Assert.AreEqual(100, definition.DefaultValue);
		}

		[Test]
		public void BlackboardVariableDefinition_DefaultValue_SetAndGet_String()
		{
			var definition = new BlackboardVariableDefinition
			{
				Key = "name",
				ValueType = typeof(string),
				DefaultValue = "Player"
			};

			Assert.AreEqual("Player", definition.DefaultValue);
		}

		[Test]
		public void BlackboardVariableDefinition_MetadataProperties_CanBeSet()
		{
			var definition = new BlackboardVariableDefinition
			{
				Key = "test",
				IsExposed = true,
				IsExposedInGraph = true,
				Category = "General",
				Tooltip = "Test tooltip",
				GraphPosition = new Vector2(100, 200)
			};

			Assert.IsTrue(definition.IsExposed);
			Assert.IsTrue(definition.IsExposedInGraph);
			Assert.AreEqual("General", definition.Category);
			Assert.AreEqual("Test tooltip", definition.Tooltip);
			Assert.AreEqual(new Vector2(100, 200), definition.GraphPosition);
		}



		[Test]
		public void BlackboardOverride_Constructor_SetsProperties()
		{
			var overrideVar = new BlackboardOverride("health", typeof(int), 100);

			Assert.AreEqual("health", overrideVar.Key);
			Assert.AreEqual(typeof(int), overrideVar.ValueType);
			Assert.AreEqual(100, overrideVar.Value);
		}

		[Test]
		public void BlackboardOverride_DefaultConstructor_CreatesEmptyOverride()
		{
			var overrideVar = new BlackboardOverride();

			Assert.IsNull(overrideVar.Key);
			Assert.IsNull(overrideVar.ValueType);
		}

		[Test]
		public void BlackboardOverride_Value_SetAndGet_Int()
		{
			var overrideVar = new BlackboardOverride
			{
				Key = "score",
				ValueType = typeof(int)
			};

			overrideVar.Value = 500;

			Assert.AreEqual(500, overrideVar.Value);
		}

		[Test]
		public void BlackboardOverride_Value_SetAndGet_Float()
		{
			var overrideVar = new BlackboardOverride
			{
				Key = "speed",
				ValueType = typeof(float)
			};

			overrideVar.Value = 5.5f;

			Assert.AreEqual(5.5f, overrideVar.Value);
		}

		[Test]
		public void BlackboardOverride_Value_SetAndGet_Bool()
		{
			var overrideVar = new BlackboardOverride
			{
				Key = "active",
				ValueType = typeof(bool)
			};

			overrideVar.Value = true;

			Assert.AreEqual(true, overrideVar.Value);
		}

		[Test]
		public void BlackboardOverride_Value_SetAndGet_String()
		{
			var overrideVar = new BlackboardOverride
			{
				Key = "name",
				ValueType = typeof(string)
			};

			overrideVar.Value = "TestPlayer";

			Assert.AreEqual("TestPlayer", overrideVar.Value);
		}

		[Test]
		public void BlackboardOverride_Value_SetAndGet_Double()
		{
			var overrideVar = new BlackboardOverride
			{
				Key = "precision",
				ValueType = typeof(double)
			};

			overrideVar.Value = 3.14159265359;

			Assert.AreEqual(3.14159265359, overrideVar.Value);
		}

		[Test]
		public void BlackboardOverride_Value_SetAndGet_Long()
		{
			var overrideVar = new BlackboardOverride
			{
				Key = "bigNumber",
				ValueType = typeof(long)
			};

			overrideVar.Value = 9999999999L;

			Assert.AreEqual(9999999999L, overrideVar.Value);
		}

		[Test]
		public void BlackboardOverride_Value_SetAndGet_Vector3()
		{
			var overrideVar = new BlackboardOverride
			{
				Key = "position",
				ValueType = typeof(Vector3)
			};

			var expected = new Vector3(1, 2, 3);
			overrideVar.Value = expected;

			Assert.AreEqual(expected, overrideVar.Value);
		}

		[Test]
		public void BlackboardOverride_InvalidateCache_ForcesRedeserialization()
		{
			var overrideVar = new BlackboardOverride("test", typeof(int), 100);

			// Access value to cache it
			var _ = overrideVar.Value;

			// Manually change JsonValue
			overrideVar.JsonValue = "200";

			// Without invalidation, should return cached value
			// (This tests the caching behavior)

			// After invalidation, should return new value
			overrideVar.InvalidateCache();
			Assert.AreEqual(200, overrideVar.Value);
		}

		[Test]
		public void BlackboardOverride_ValueType_ClearsCache_OnSet()
		{
			var overrideVar = new BlackboardOverride();
			overrideVar.ValueType = typeof(int);

			Assert.AreEqual(typeof(int), overrideVar.ValueType);

			overrideVar.TypeAssemblyQualifiedName = typeof(float).AssemblyQualifiedName;

			Assert.AreEqual(typeof(float), overrideVar.ValueType);
		}

		[Test]
		public void BlackboardOverride_NullValue_SetsJsonValueToNull()
		{
			var overrideVar = new BlackboardOverride("test", typeof(int), 100);

			overrideVar.Value = null;
			overrideVar.InvalidateCache();

			// After setting null and invalidating, value should be default (null for ref types, 0 for int)
			Assert.AreEqual(null, overrideVar.Value);
		}

		[Test]
		public void BlackboardOverride_NullType_ReturnsNullValue()
		{
			var overrideVar = new BlackboardOverride
			{
				Key = "test",
				ValueType = null
			};

			Assert.IsNull(overrideVar.Value);
		}



		[Test]
		public void Blackboard_DefineVariable_CreatesVariableFromDefinition()
		{
			var blackboard = new Blackboard.Blackboard();
			var definition = new BlackboardVariableDefinition
			{
				Key = "health",
				ValueType = typeof(int),
				DefaultValue = 100,
				IsExposed = true,
				Category = "Stats"
			};
			definition.CacheTypeName();

			blackboard.DefineVariable(definition);

			Assert.IsTrue(blackboard.HasKey("health"));
			Assert.AreEqual(100, blackboard.Get<int>("health"));
		}

		[Test]
		public void Blackboard_DefineVariable_NullDefinition_DoesNothing()
		{
			var blackboard = new Blackboard.Blackboard();

			Assert.DoesNotThrow(() => blackboard.DefineVariable(null));
			Assert.AreEqual(0, blackboard.Count);
		}

		[Test]
		public void Blackboard_DefineVariable_EmptyKey_DoesNothing()
		{
			var blackboard = new Blackboard.Blackboard();
			var definition = new BlackboardVariableDefinition
			{
				Key = "",
				ValueType = typeof(int)
			};

			blackboard.DefineVariable(definition);

			Assert.AreEqual(0, blackboard.Count);
		}

		[Test]
		public void Blackboard_DefineVariable_ExistingKey_DoesNotOverwrite()
		{
			var blackboard = new Blackboard.Blackboard();
			blackboard.Set("health", 50);

			var definition = new BlackboardVariableDefinition
			{
				Key = "health",
				ValueType = typeof(int),
				DefaultValue = 100
			};
			definition.CacheTypeName();

			blackboard.DefineVariable(definition);

			// Should keep existing value, not overwrite
			Assert.AreEqual(50, blackboard.Get<int>("health"));
		}

		[Test]
		public void Blackboard_DefineVariable_SetsMetadata()
		{
			var blackboard = new Blackboard.Blackboard();
			var definition = new BlackboardVariableDefinition
			{
				Key = "speed",
				ValueType = typeof(float),
				DefaultValue = 5.0f,
				IsExposed = true,
				IsExposedInGraph = true,
				Category = "Movement",
				Tooltip = "Player speed"
			};
			definition.CacheTypeName();

			blackboard.DefineVariable(definition);

			var variable = blackboard.GetVariable("speed");
			Assert.IsTrue(variable.IsExposed);
			Assert.IsTrue(variable.IsExposedInGraph);
			Assert.AreEqual("Movement", variable.Category);
			Assert.AreEqual("Player speed", variable.Tooltip);
		}



		[Test]
		public void Blackboard_GetExposedVariables_ReturnsOnlyExposed()
		{
			var blackboard = new Blackboard.Blackboard();

			var exposedDef = new BlackboardVariableDefinition
			{
				Key = "exposed",
				ValueType = typeof(int),
				DefaultValue = 1,
				IsExposed = true
			};
			exposedDef.CacheTypeName();

			var hiddenDef = new BlackboardVariableDefinition
			{
				Key = "hidden",
				ValueType = typeof(int),
				DefaultValue = 2,
				IsExposed = false
			};
			hiddenDef.CacheTypeName();

			blackboard.DefineVariable(exposedDef);
			blackboard.DefineVariable(hiddenDef);

			var exposed = new System.Collections.Generic.List<BlackboardVariable>(blackboard.GetExposedVariables());

			Assert.AreEqual(1, exposed.Count);
			Assert.AreEqual("exposed", exposed[0].Key);
		}



		[Test]
		public void Blackboard_Clone_CreatesIndependentCopy()
		{
			var original = new Blackboard.Blackboard();
			original.Set("value", 100);

			var clone = original.Clone();

			// Modify original
			original.Set("value", 200);

			// Clone should be unaffected
			Assert.AreEqual(100, clone.Get<int>("value"));
		}

		[Test]
		public void Blackboard_Clone_DoesNotCopyParent()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("parentVal", 50);

			var original = new Blackboard.Blackboard();
			original.SetParent(parent);
			original.Set("localVal", 100);

			var clone = original.Clone();

			// Clone should have local value but not parent
			Assert.IsTrue(clone.HasKey("localVal"));
			Assert.IsFalse(clone.HasKey("parentVal"));
			Assert.IsNull(clone.Parent);
		}

		[Test]
		public void Blackboard_CopyFrom_CopiesAllVariables()
		{
			var source = new Blackboard.Blackboard();
			source.Set("a", 1);
			source.Set("b", 2);
			source.Set("c", 3);

			var target = new Blackboard.Blackboard();

			target.CopyFrom(source);

			Assert.AreEqual(1, target.Get<int>("a"));
			Assert.AreEqual(2, target.Get<int>("b"));
			Assert.AreEqual(3, target.Get<int>("c"));
		}

		[Test]
		public void Blackboard_CopyFrom_NullSource_DoesNothing()
		{
			var target = new Blackboard.Blackboard();
			target.Set("existing", 100);

			Assert.DoesNotThrow(() => target.CopyFrom(null));
			Assert.AreEqual(100, target.Get<int>("existing"));
		}

		[Test]
		public void Blackboard_CopyFrom_OverwritesExisting()
		{
			var source = new Blackboard.Blackboard();
			source.Set("value", 200);

			var target = new Blackboard.Blackboard();
			target.Set("value", 100);

			target.CopyFrom(source);

			Assert.AreEqual(200, target.Get<int>("value"));
		}



		[Test]
		public void Blackboard_SetBoxed_CreatesNewVariable()
		{
			var blackboard = new Blackboard.Blackboard();

			blackboard.SetBoxed("value", 42, typeof(int));

			Assert.IsTrue(blackboard.HasKey("value"));
			Assert.AreEqual(42, blackboard.GetBoxed("value"));
		}

		[Test]
		public void Blackboard_SetBoxed_UpdatesExistingVariable()
		{
			var blackboard = new Blackboard.Blackboard();
			blackboard.SetBoxed("value", 10, typeof(int));

			blackboard.SetBoxed("value", 20, typeof(int));

			Assert.AreEqual(20, blackboard.GetBoxed("value"));
		}

		[Test]
		public void Blackboard_SetBoxed_FiresOnVariableAdded()
		{
			var blackboard = new Blackboard.Blackboard();
			var addedKey = "";
			object addedValue = null;

			blackboard.OnVariableAdded += (key, value) =>
			{
				addedKey = key;
				addedValue = value;
			};

			blackboard.SetBoxed("newVar", 100, typeof(int));

			Assert.AreEqual("newVar", addedKey);
			Assert.AreEqual(100, addedValue);
		}

		[Test]
		public void Blackboard_SetBoxed_FiresOnVariableChanged()
		{
			var blackboard = new Blackboard.Blackboard();
			blackboard.SetBoxed("existing", 10, typeof(int));

			var changedKey = "";
			object oldVal = null;
			object newVal = null;

			blackboard.OnVariableChanged += (key, old, @new) =>
			{
				changedKey = key;
				oldVal = old;
				newVal = @new;
			};

			blackboard.SetBoxed("existing", 20, typeof(int));

			Assert.AreEqual("existing", changedKey);
			Assert.AreEqual(10, oldVal);
			Assert.AreEqual(20, newVal);
		}



		[Test]
		public void Blackboard_SubscribeAll_ReceivesAllChanges()
		{
			var blackboard = new Blackboard.Blackboard();
			var changeCount = 0;

			blackboard.Set("a", 1);
			blackboard.Set("b", 2);

			blackboard.SubscribeAll((key, old, @new) => changeCount++);

			blackboard.Set("a", 10);
			blackboard.Set("b", 20);

			Assert.AreEqual(2, changeCount);
		}

		[Test]
		public void Blackboard_SubscribeAll_DisposableUnsubscribes()
		{
			var blackboard = new Blackboard.Blackboard();
			var changeCount = 0;

			blackboard.Set("value", 1);

			var subscription = blackboard.SubscribeAll((key, old, @new) => changeCount++);

			blackboard.Set("value", 2);
			Assert.AreEqual(1, changeCount);

			subscription.Dispose();

			blackboard.Set("value", 3);
			Assert.AreEqual(1, changeCount); // Should not have incremented
		}



		[Test]
		public void Blackboard_Remove_FiresOnVariableRemoved()
		{
			var blackboard = new Blackboard.Blackboard();
			blackboard.Set("toRemove", 100);

			var removedKey = "";
			blackboard.OnVariableRemoved += key => removedKey = key;

			blackboard.Remove("toRemove");

			Assert.AreEqual("toRemove", removedKey);
		}

		[Test]
		public void Blackboard_Clear_FiresOnVariableRemovedForEach()
		{
			var blackboard = new Blackboard.Blackboard();
			blackboard.Set("a", 1);
			blackboard.Set("b", 2);
			blackboard.Set("c", 3);

			var removedKeys = new System.Collections.Generic.List<string>();
			blackboard.OnVariableRemoved += key => removedKeys.Add(key);

			blackboard.Clear();

			Assert.AreEqual(3, removedKeys.Count);
			Assert.Contains("a", removedKeys);
			Assert.Contains("b", removedKeys);
			Assert.Contains("c", removedKeys);
		}

	}
}
