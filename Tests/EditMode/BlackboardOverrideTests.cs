using System;
using NUnit.Framework;
using Nonatomic.VSM.Blackboard;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class BlackboardOverrideTests
	{

		[Test]
		public void DefaultConstructor_CreatesEmptyOverride()
		{
			var overrideObj = new BlackboardOverride();

			Assert.IsNull(overrideObj.Key);
			Assert.IsNull(overrideObj.ValueType);
			Assert.IsNull(overrideObj.JsonValue);
			Assert.IsNull(overrideObj.ObjectReference);
		}

		[Test]
		public void ParameterizedConstructor_SetsKeyTypeAndValue()
		{
			var overrideObj = new BlackboardOverride("health", typeof(int), 100);

			Assert.AreEqual("health", overrideObj.Key);
			Assert.AreEqual(typeof(int), overrideObj.ValueType);
			Assert.AreEqual(100, overrideObj.Value);
		}

		[Test]
		public void ParameterizedConstructor_SetsTypeAssemblyQualifiedName()
		{
			var overrideObj = new BlackboardOverride("speed", typeof(float), 5.5f);

			Assert.AreEqual(typeof(float).AssemblyQualifiedName, overrideObj.TypeAssemblyQualifiedName);
		}

		[Test]
		public void Key_SetAndGet_ReturnsCorrectValue()
		{
			var overrideObj = new BlackboardOverride();

			overrideObj.Key = "myKey";

			Assert.AreEqual("myKey", overrideObj.Key);
		}

		[Test]
		public void Key_SetNull_ReturnsNull()
		{
			var overrideObj = new BlackboardOverride();

			overrideObj.Key = null;

			Assert.IsNull(overrideObj.Key);
		}

		[Test]
		public void ValueType_Set_CachesType()
		{
			var overrideObj = new BlackboardOverride();

			overrideObj.ValueType = typeof(int);

			Assert.AreEqual(typeof(int), overrideObj.ValueType);
		}

		[Test]
		public void ValueType_Set_UpdatesAssemblyQualifiedName()
		{
			var overrideObj = new BlackboardOverride();

			overrideObj.ValueType = typeof(string);

			Assert.AreEqual(typeof(string).AssemblyQualifiedName, overrideObj.TypeAssemblyQualifiedName);
		}

		[Test]
		public void ValueType_SetNull_ClearsAssemblyQualifiedName()
		{
			var overrideObj = new BlackboardOverride();
			overrideObj.ValueType = typeof(int);

			overrideObj.ValueType = null;

			Assert.IsNull(overrideObj.TypeAssemblyQualifiedName);
		}

		[Test]
		public void ValueType_GetFromAssemblyQualifiedName_ResolvesType()
		{
			var overrideObj = new BlackboardOverride();
			overrideObj.TypeAssemblyQualifiedName = typeof(float).AssemblyQualifiedName;

			var result = overrideObj.ValueType;

			Assert.AreEqual(typeof(float), result);
		}

		[Test]
		public void ValueType_EmptyAssemblyQualifiedName_ReturnsNull()
		{
			var overrideObj = new BlackboardOverride();
			overrideObj.TypeAssemblyQualifiedName = "";

			Assert.IsNull(overrideObj.ValueType);
		}

		[Test]
		public void ValueType_NullAssemblyQualifiedName_ReturnsNull()
		{
			var overrideObj = new BlackboardOverride();
			overrideObj.TypeAssemblyQualifiedName = null;

			Assert.IsNull(overrideObj.ValueType);
		}

		[Test]
		public void TypeAssemblyQualifiedName_Set_InvalidatesCachedType()
		{
			var overrideObj = new BlackboardOverride();
			overrideObj.ValueType = typeof(int);
			Assert.AreEqual(typeof(int), overrideObj.ValueType);

			overrideObj.TypeAssemblyQualifiedName = typeof(string).AssemblyQualifiedName;

			Assert.AreEqual(typeof(string), overrideObj.ValueType);
		}

		[Test]
		public void Value_SetInt_SerializesToJson()
		{
			var overrideObj = new BlackboardOverride("counter", typeof(int), 42);

			Assert.AreEqual("42", overrideObj.JsonValue);
		}

		[Test]
		public void Value_GetInt_DeserializesCorrectly()
		{
			var overrideObj = new BlackboardOverride("counter", typeof(int), 42);

			overrideObj.InvalidateCache();
			var result = overrideObj.Value;

			Assert.AreEqual(42, result);
		}

		[Test]
		public void Value_SetNegativeInt_RoundTrips()
		{
			var overrideObj = new BlackboardOverride("score", typeof(int), -100);

			overrideObj.InvalidateCache();

			Assert.AreEqual(-100, overrideObj.Value);
		}

		[Test]
		public void Value_SetZeroInt_RoundTrips()
		{
			var overrideObj = new BlackboardOverride("zero", typeof(int), 0);

			overrideObj.InvalidateCache();

			Assert.AreEqual(0, overrideObj.Value);
		}

		[Test]
		public void Value_InvalidIntJson_ReturnsZero()
		{
			var overrideObj = new BlackboardOverride();
			overrideObj.ValueType = typeof(int);
			overrideObj.JsonValue = "not_a_number";

			overrideObj.InvalidateCache();

			Assert.AreEqual(0, overrideObj.Value);
		}

		[Test]
		public void Value_SetFloat_SerializesToJson()
		{
			var overrideObj = new BlackboardOverride("speed", typeof(float), 3.14f);

			Assert.IsNotNull(overrideObj.JsonValue);
		}

		[Test]
		public void Value_SetFloat_RoundTrips()
		{
			var overrideObj = new BlackboardOverride("speed", typeof(float), 3.14f);

			overrideObj.InvalidateCache();

			Assert.AreEqual(3.14f, (float)overrideObj.Value, 0.001f);
		}

		[Test]
		public void Value_SetNegativeFloat_RoundTrips()
		{
			var overrideObj = new BlackboardOverride("temp", typeof(float), -27.5f);

			overrideObj.InvalidateCache();

			Assert.AreEqual(-27.5f, (float)overrideObj.Value, 0.001f);
		}

		[Test]
		public void Value_InvalidFloatJson_ReturnsZero()
		{
			var overrideObj = new BlackboardOverride();
			overrideObj.ValueType = typeof(float);
			overrideObj.JsonValue = "invalid";

			overrideObj.InvalidateCache();

			Assert.AreEqual(0f, overrideObj.Value);
		}

		[Test]
		public void Value_SetTrue_RoundTrips()
		{
			var overrideObj = new BlackboardOverride("isAlive", typeof(bool), true);

			overrideObj.InvalidateCache();

			Assert.AreEqual(true, overrideObj.Value);
		}

		[Test]
		public void Value_SetFalse_RoundTrips()
		{
			var overrideObj = new BlackboardOverride("isDead", typeof(bool), false);

			overrideObj.InvalidateCache();

			Assert.AreEqual(false, overrideObj.Value);
		}

		[Test]
		public void Value_InvalidBoolJson_ReturnsFalse()
		{
			var overrideObj = new BlackboardOverride();
			overrideObj.ValueType = typeof(bool);
			overrideObj.JsonValue = "not_a_bool";

			overrideObj.InvalidateCache();

			Assert.AreEqual(false, overrideObj.Value);
		}

		[Test]
		public void Value_SetDouble_RoundTrips()
		{
			var overrideObj = new BlackboardOverride("precision", typeof(double), 3.14159265358979);

			overrideObj.InvalidateCache();

			Assert.AreEqual(3.14159265358979, (double)overrideObj.Value, 0.0000001);
		}

		[Test]
		public void Value_InvalidDoubleJson_ReturnsZero()
		{
			var overrideObj = new BlackboardOverride();
			overrideObj.ValueType = typeof(double);
			overrideObj.JsonValue = "invalid";

			overrideObj.InvalidateCache();

			Assert.AreEqual(0.0, overrideObj.Value);
		}

		[Test]
		public void Value_SetLong_RoundTrips()
		{
			var overrideObj = new BlackboardOverride("bigNumber", typeof(long), 9876543210L);

			overrideObj.InvalidateCache();

			Assert.AreEqual(9876543210L, overrideObj.Value);
		}

		[Test]
		public void Value_SetNegativeLong_RoundTrips()
		{
			var overrideObj = new BlackboardOverride("negativeLong", typeof(long), -9876543210L);

			overrideObj.InvalidateCache();

			Assert.AreEqual(-9876543210L, overrideObj.Value);
		}

		[Test]
		public void Value_InvalidLongJson_ReturnsZero()
		{
			var overrideObj = new BlackboardOverride();
			overrideObj.ValueType = typeof(long);
			overrideObj.JsonValue = "invalid";

			overrideObj.InvalidateCache();

			Assert.AreEqual(0L, overrideObj.Value);
		}

		[Test]
		public void Value_SetString_StoredDirectly()
		{
			var overrideObj = new BlackboardOverride("name", typeof(string), "TestPlayer");

			Assert.AreEqual("TestPlayer", overrideObj.JsonValue);
		}

		[Test]
		public void Value_SetString_RoundTrips()
		{
			var overrideObj = new BlackboardOverride("name", typeof(string), "TestPlayer");

			overrideObj.InvalidateCache();

			Assert.AreEqual("TestPlayer", overrideObj.Value);
		}

		[Test]
		public void Value_SetEmptyString_RoundTrips()
		{
			var overrideObj = new BlackboardOverride("empty", typeof(string), "");

			overrideObj.InvalidateCache();

			// Empty string becomes null because IsNullOrEmpty check in DeserializeValue
			Assert.IsNull(overrideObj.Value);
		}

		[Test]
		public void Value_SetStringWithSpecialChars_RoundTrips()
		{
			var overrideObj = new BlackboardOverride("special", typeof(string), "hello world! @#$%");

			overrideObj.InvalidateCache();

			Assert.AreEqual("hello world! @#$%", overrideObj.Value);
		}

		[Test]
		public void Value_SetVector3_RoundTrips()
		{
			var position = new Vector3(1f, 2f, 3f);
			var overrideObj = new BlackboardOverride("position", typeof(Vector3), position);

			overrideObj.InvalidateCache();

			Assert.AreEqual(position, overrideObj.Value);
		}

		[Test]
		public void Value_SetVector2_RoundTrips()
		{
			var size = new Vector2(10f, 20f);
			var overrideObj = new BlackboardOverride("size", typeof(Vector2), size);

			overrideObj.InvalidateCache();

			Assert.AreEqual(size, overrideObj.Value);
		}

		[Test]
		public void Value_SetColor_RoundTrips()
		{
			var color = new Color(1f, 0.5f, 0.25f, 1f);
			var overrideObj = new BlackboardOverride("color", typeof(Color), color);

			overrideObj.InvalidateCache();

			Assert.AreEqual(color, overrideObj.Value);
		}

		[Test]
		public void Value_SetNull_WithType_SetsJsonToNull()
		{
			var overrideObj = new BlackboardOverride();
			overrideObj.ValueType = typeof(int);

			overrideObj.Value = null;

			Assert.IsNull(overrideObj.JsonValue);
		}

		[Test]
		public void Value_GetWithNullType_ReturnsNull()
		{
			var overrideObj = new BlackboardOverride();

			Assert.IsNull(overrideObj.Value);
		}

		[Test]
		public void Value_GetWithNullJsonAndNonObjectType_ReturnsNull()
		{
			var overrideObj = new BlackboardOverride();
			overrideObj.ValueType = typeof(int);
			overrideObj.JsonValue = null;

			overrideObj.InvalidateCache();

			Assert.IsNull(overrideObj.Value);
		}

		[Test]
		public void Value_SetNullWithNullType_ClearsJsonAndObjectRef()
		{
			var overrideObj = new BlackboardOverride();

			overrideObj.Value = null;

			Assert.IsNull(overrideObj.JsonValue);
			Assert.IsNull(overrideObj.ObjectReference);
		}

		[Test]
		public void Value_SetUnityObject_StoresAsObjectReference()
		{
			var material = new Material(Shader.Find("Standard"));
			var overrideObj = new BlackboardOverride("mat", typeof(Material), material);

			Assert.AreSame(material, overrideObj.ObjectReference);
			Assert.IsNull(overrideObj.JsonValue);

			UnityEngine.Object.DestroyImmediate(material);
		}

		[Test]
		public void Value_GetUnityObject_ReturnsObjectReference()
		{
			var material = new Material(Shader.Find("Standard"));
			var overrideObj = new BlackboardOverride("mat", typeof(Material), material);

			overrideObj.InvalidateCache();

			Assert.AreSame(material, overrideObj.Value);

			UnityEngine.Object.DestroyImmediate(material);
		}

		[Test]
		public void Value_SetNullUnityObject_StoresNullReference()
		{
			var overrideObj = new BlackboardOverride("mat", typeof(Material), null);

			Assert.IsNull(overrideObj.ObjectReference);
			Assert.IsNull(overrideObj.JsonValue);
		}

		[Test]
		public void Value_SetGameObject_StoresAsObjectReference()
		{
			var go = new GameObject("TestObject");
			var overrideObj = new BlackboardOverride("target", typeof(GameObject), go);

			Assert.AreSame(go, overrideObj.ObjectReference);
			Assert.IsNull(overrideObj.JsonValue);

			UnityEngine.Object.DestroyImmediate(go);
		}

		[Test]
		public void InvalidateCache_ForcesReDeserialization()
		{
			var overrideObj = new BlackboardOverride("counter", typeof(int), 42);

			// Access value to cache it
			var firstAccess = overrideObj.Value;
			Assert.AreEqual(42, firstAccess);

			// Manually change the JSON
			overrideObj.JsonValue = "99";

			// Without invalidating, still returns cached value
			Assert.AreEqual(42, overrideObj.Value);

			// After invalidating, returns new value
			overrideObj.InvalidateCache();
			Assert.AreEqual(99, overrideObj.Value);
		}

		[Test]
		public void InvalidateCache_ClearsCachedType()
		{
			var overrideObj = new BlackboardOverride("test", typeof(int), 42);

			// Access type to cache it
			var firstType = overrideObj.ValueType;
			Assert.AreEqual(typeof(int), firstType);

			overrideObj.InvalidateCache();

			// Type should still resolve from AssemblyQualifiedName
			Assert.AreEqual(typeof(int), overrideObj.ValueType);
		}

		[Test]
		public void Value_MultipleGets_ReturnsCachedValue()
		{
			var overrideObj = new BlackboardOverride("counter", typeof(int), 42);

			var first = overrideObj.Value;
			var second = overrideObj.Value;

			Assert.AreEqual(first, second);
			Assert.AreEqual(42, first);
		}

		[Test]
		public void Value_SetNewValue_UpdatesCache()
		{
			var overrideObj = new BlackboardOverride("counter", typeof(int), 42);

			Assert.AreEqual(42, overrideObj.Value);

			overrideObj.Value = 100;

			Assert.AreEqual(100, overrideObj.Value);
		}

		[Test]
		public void Value_SetNewValue_UpdatesJson()
		{
			var overrideObj = new BlackboardOverride("counter", typeof(int), 42);

			overrideObj.Value = 100;

			Assert.AreEqual("100", overrideObj.JsonValue);
		}

		[Test]
		public void Value_InvalidJson_ForComplexType_ReturnsNull()
		{
			var overrideObj = new BlackboardOverride();
			overrideObj.ValueType = typeof(Vector3);
			overrideObj.JsonValue = "not valid json";

			overrideObj.InvalidateCache();

			Assert.IsNull(overrideObj.Value);
		}

		[Test]
		public void Value_IntMaxValue_RoundTrips()
		{
			var overrideObj = new BlackboardOverride("max", typeof(int), int.MaxValue);

			overrideObj.InvalidateCache();

			Assert.AreEqual(int.MaxValue, overrideObj.Value);
		}

		[Test]
		public void Value_IntMinValue_RoundTrips()
		{
			var overrideObj = new BlackboardOverride("min", typeof(int), int.MinValue);

			overrideObj.InvalidateCache();

			Assert.AreEqual(int.MinValue, overrideObj.Value);
		}

		[Test]
		public void Value_LongMaxValue_RoundTrips()
		{
			var overrideObj = new BlackboardOverride("max", typeof(long), long.MaxValue);

			overrideObj.InvalidateCache();

			Assert.AreEqual(long.MaxValue, overrideObj.Value);
		}

		[Test]
		public void ChangeType_AfterConstruction_AffectsDeserialization()
		{
			var overrideObj = new BlackboardOverride("value", typeof(int), 42);
			Assert.AreEqual("42", overrideObj.JsonValue);

			// Change type to float - the JSON "42" should now parse as float
			overrideObj.ValueType = typeof(float);
			overrideObj.InvalidateCache();

			Assert.AreEqual(42f, (float)overrideObj.Value, 0.001f);
		}

		[Test]
		public void ObjectReference_Property_CanBeSetDirectly()
		{
			var material = new Material(Shader.Find("Standard"));
			var overrideObj = new BlackboardOverride();

			overrideObj.ObjectReference = material;

			Assert.AreSame(material, overrideObj.ObjectReference);

			UnityEngine.Object.DestroyImmediate(material);
		}

		[Test]
		public void JsonValue_Property_CanBeSetDirectly()
		{
			var overrideObj = new BlackboardOverride();

			overrideObj.JsonValue = "42";

			Assert.AreEqual("42", overrideObj.JsonValue);
		}

	}
}
