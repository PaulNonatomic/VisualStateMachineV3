using System;
using System.Collections.Generic;
using NUnit.Framework;
using Nonatomic.VSM.Blackboard;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class BlackboardTests
	{
		private Blackboard.Blackboard _blackboard;

		[SetUp]
		public void SetUp()
		{
			_blackboard = new Blackboard.Blackboard();
		}

		[TearDown]
		public void TearDown()
		{
			_blackboard = null;
		}


		[Test]
		public void Set_And_Get_Int_ReturnsCorrectValue()
		{
			_blackboard.Set("health", 100);

			var result = _blackboard.Get<int>("health");

			Assert.AreEqual(100, result);
		}

		[Test]
		public void Set_And_Get_Float_ReturnsCorrectValue()
		{
			_blackboard.Set("speed", 5.5f);

			var result = _blackboard.Get<float>("speed");

			Assert.AreEqual(5.5f, result, 0.001f);
		}

		[Test]
		public void Set_And_Get_String_ReturnsCorrectValue()
		{
			_blackboard.Set("playerName", "TestPlayer");

			var result = _blackboard.Get<string>("playerName");

			Assert.AreEqual("TestPlayer", result);
		}

		[Test]
		public void Set_And_Get_Bool_ReturnsCorrectValue()
		{
			_blackboard.Set("isAlive", true);

			var result = _blackboard.Get<bool>("isAlive");

			Assert.IsTrue(result);
		}

		[Test]
		public void Set_And_Get_Vector3_ReturnsCorrectValue()
		{
			var position = new Vector3(1f, 2f, 3f);
			_blackboard.Set("position", position);

			var result = _blackboard.Get<Vector3>("position");

			Assert.AreEqual(position, result);
		}

		[Test]
		public void Get_NonExistentKey_ThrowsException()
		{
			Assert.Throws<KeyNotFoundException>(() => _blackboard.Get<int>("nonexistent"));
		}

		[Test]
		public void Get_WithDefaultValue_ReturnsDefaultWhenKeyMissing()
		{
			var result = _blackboard.Get("nonexistent", 42);

			Assert.AreEqual(42, result);
		}

		[Test]
		public void Set_OverwritesExistingValue()
		{
			_blackboard.Set("counter", 1);
			_blackboard.Set("counter", 2);

			var result = _blackboard.Get<int>("counter");

			Assert.AreEqual(2, result);
		}



		[Test]
		public void TryGet_ExistingKey_ReturnsTrueAndValue()
		{
			_blackboard.Set("health", 100);

			var success = _blackboard.TryGet<int>("health", out var value);

			Assert.IsTrue(success);
			Assert.AreEqual(100, value);
		}

		[Test]
		public void TryGet_NonExistentKey_ReturnsFalse()
		{
			var success = _blackboard.TryGet<int>("nonexistent", out var value);

			Assert.IsFalse(success);
			Assert.AreEqual(default(int), value);
		}



		[Test]
		public void HasKey_ExistingKey_ReturnsTrue()
		{
			_blackboard.Set("test", 1);

			var result = _blackboard.HasKey("test");

			Assert.IsTrue(result);
		}

		[Test]
		public void HasKey_NonExistentKey_ReturnsFalse()
		{
			var result = _blackboard.HasKey("nonexistent");

			Assert.IsFalse(result);
		}



		[Test]
		public void Remove_ExistingKey_RemovesValue()
		{
			_blackboard.Set("test", 100);

			_blackboard.Remove("test");

			Assert.IsFalse(_blackboard.HasKey("test"));
		}

		[Test]
		public void Remove_NonExistentKey_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => _blackboard.Remove("nonexistent"));
		}



		[Test]
		public void Clear_RemovesAllValues()
		{
			_blackboard.Set("a", 1);
			_blackboard.Set("b", 2);
			_blackboard.Set("c", 3);

			_blackboard.Clear();

			Assert.IsFalse(_blackboard.HasKey("a"));
			Assert.IsFalse(_blackboard.HasKey("b"));
			Assert.IsFalse(_blackboard.HasKey("c"));
		}



		[Test]
		public void GetAllKeys_ReturnsAllKeys()
		{
			_blackboard.Set("a", 1);
			_blackboard.Set("b", 2);
			_blackboard.Set("c", 3);

			var keys = _blackboard.GetAllKeys();

			Assert.Contains("a", new System.Collections.Generic.List<string>(keys));
			Assert.Contains("b", new System.Collections.Generic.List<string>(keys));
			Assert.Contains("c", new System.Collections.Generic.List<string>(keys));
		}



		[Test]
		public void Subscribe_CallsCallbackOnChange()
		{
			var callbackCalled = false;
			int oldValue = 0;
			int newValue = 0;

			_blackboard.Set("health", 100);
			_blackboard.Subscribe<int>("health", (old, @new) =>
			{
				callbackCalled = true;
				oldValue = old;
				newValue = @new;
			});

			_blackboard.Set("health", 50);

			Assert.IsTrue(callbackCalled);
			Assert.AreEqual(100, oldValue);
			Assert.AreEqual(50, newValue);
		}

		[Test]
		public void Subscribe_DoesNotCallCallbackForSameValue()
		{
			var callCount = 0;

			_blackboard.Set("health", 100);
			_blackboard.Subscribe<int>("health", (old, @new) => callCount++);

			_blackboard.Set("health", 100);

			Assert.AreEqual(0, callCount);
		}

		[Test]
		public void Subscribe_DisposableUnsubscribes()
		{
			var callCount = 0;

			_blackboard.Set("health", 100);
			var subscription = _blackboard.Subscribe<int>("health", (old, @new) => callCount++);

			subscription.Dispose();
			_blackboard.Set("health", 50);

			Assert.AreEqual(0, callCount);
		}

		[Test]
		public void OnVariableChanged_EventFires()
		{
			var eventFired = false;
			string changedKey = null;

			// First set - this fires OnVariableAdded, not OnVariableChanged
			_blackboard.Set("test", 42);

			_blackboard.OnVariableChanged += (key, oldVal, newVal) =>
			{
				eventFired = true;
				changedKey = key;
			};

			// Second set - this should fire OnVariableChanged
			_blackboard.Set("test", 100);

			Assert.IsTrue(eventFired);
			Assert.AreEqual("test", changedKey);
		}



		[Test]
		public void SetParent_ChildCanAccessParentValues()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("parentValue", 100);

			_blackboard.SetParent(parent);

			var result = _blackboard.Get<int>("parentValue");

			Assert.AreEqual(100, result);
		}

		[Test]
		public void SetParent_ChildValueOverridesParent()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("value", 100);

			_blackboard.SetParent(parent);
			_blackboard.Set("value", 200);

			var result = _blackboard.Get<int>("value");

			Assert.AreEqual(200, result);
		}

		[Test]
		public void SetParent_HasKeyChecksParent()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("parentOnly", 100);

			_blackboard.SetParent(parent);

			Assert.IsTrue(_blackboard.HasKey("parentOnly"));
		}

		[Test]
		public void SetParent_GrandparentChaining()
		{
			var grandparent = new Blackboard.Blackboard();
			var parent = new Blackboard.Blackboard();

			grandparent.Set("ancestorValue", 42);
			parent.SetParent(grandparent);
			_blackboard.SetParent(parent);

			var result = _blackboard.Get<int>("ancestorValue");

			Assert.AreEqual(42, result);
		}



		[Test]
		public void GetValueType_ReturnsCorrectType()
		{
			_blackboard.Set("intValue", 42);
			_blackboard.Set("stringValue", "test");

			Assert.AreEqual(typeof(int), _blackboard.GetValueType("intValue"));
			Assert.AreEqual(typeof(string), _blackboard.GetValueType("stringValue"));
		}

		[Test]
		public void GetValueType_NonExistentKey_ReturnsNull()
		{
			var result = _blackboard.GetValueType("nonexistent");

			Assert.IsNull(result);
		}




		[Test]
		public void SetParent_Null_DisconnectsHierarchy()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("parentValue", 100);
			_blackboard.SetParent(parent);

			// Verify parent is accessible
			Assert.IsTrue(_blackboard.HasKey("parentValue"));

			// Disconnect hierarchy
			_blackboard.SetParent(null);

			// Parent value should no longer be accessible
			Assert.IsFalse(_blackboard.HasKey("parentValue"));
			Assert.Throws<KeyNotFoundException>(() => _blackboard.Get<int>("parentValue"));
		}

		[Test]
		public void SetParent_Null_PreservesLocalVariables()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("parentValue", 100);
			_blackboard.SetParent(parent);
			_blackboard.Set("localValue", 50);

			_blackboard.SetParent(null);

			// Local value should still be accessible
			Assert.IsTrue(_blackboard.HasKey("localValue"));
			Assert.AreEqual(50, _blackboard.Get<int>("localValue"));
		}

		[Test]
		public void DeepHierarchy_FourLevels_ResolvesFromAncestor()
		{
			var greatGrandparent = new Blackboard.Blackboard();
			var grandparent = new Blackboard.Blackboard();
			var parent = new Blackboard.Blackboard();

			greatGrandparent.Set("level0Value", 0);
			grandparent.Set("level1Value", 1);
			parent.Set("level2Value", 2);
			_blackboard.Set("level3Value", 3);

			grandparent.SetParent(greatGrandparent);
			parent.SetParent(grandparent);
			_blackboard.SetParent(parent);

			// Can access all levels
			Assert.AreEqual(0, _blackboard.Get<int>("level0Value"));
			Assert.AreEqual(1, _blackboard.Get<int>("level1Value"));
			Assert.AreEqual(2, _blackboard.Get<int>("level2Value"));
			Assert.AreEqual(3, _blackboard.Get<int>("level3Value"));
		}

		[Test]
		public void DeepHierarchy_LocalOverridesAllAncestors()
		{
			var grandparent = new Blackboard.Blackboard();
			var parent = new Blackboard.Blackboard();

			grandparent.Set("shared", "grandparent");
			parent.Set("shared", "parent");
			_blackboard.Set("shared", "local");

			parent.SetParent(grandparent);
			_blackboard.SetParent(parent);

			Assert.AreEqual("local", _blackboard.Get<string>("shared"));
		}

		[Test]
		public void DeepHierarchy_ParentOverridesGrandparent()
		{
			var grandparent = new Blackboard.Blackboard();
			var parent = new Blackboard.Blackboard();

			grandparent.Set("shared", "grandparent");
			parent.Set("shared", "parent");

			parent.SetParent(grandparent);
			_blackboard.SetParent(parent);

			// Local doesn't have "shared", so should get from parent
			Assert.AreEqual("parent", _blackboard.Get<string>("shared"));
		}

		[Test]
		public void HasVariable_ChecksEntireParentChain()
		{
			var grandparent = new Blackboard.Blackboard();
			var parent = new Blackboard.Blackboard();

			grandparent.Set("grandparentOnly", 1);
			parent.Set("parentOnly", 2);
			_blackboard.Set("localOnly", 3);

			parent.SetParent(grandparent);
			_blackboard.SetParent(parent);

			Assert.IsTrue(_blackboard.HasVariable("grandparentOnly"));
			Assert.IsTrue(_blackboard.HasVariable("parentOnly"));
			Assert.IsTrue(_blackboard.HasVariable("localOnly"));
			Assert.IsFalse(_blackboard.HasVariable("nonexistent"));
		}

		[Test]
		public void HasLocalVariable_DoesNotCheckParent()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("parentValue", 100);
			_blackboard.Set("localValue", 50);
			_blackboard.SetParent(parent);

			Assert.IsTrue(_blackboard.HasLocalVariable("localValue"));
			Assert.IsFalse(_blackboard.HasLocalVariable("parentValue"));
		}

		[Test]
		public void Count_DoesNotIncludeParentVariables()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("a", 1);
			parent.Set("b", 2);
			_blackboard.Set("c", 3);
			_blackboard.SetParent(parent);

			Assert.AreEqual(1, _blackboard.Count);
		}

		[Test]
		public void Keys_DoesNotIncludeParentKeys()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("parentKey", 1);
			_blackboard.Set("localKey", 2);
			_blackboard.SetParent(parent);

			var keys = new List<string>(_blackboard.Keys);

			Assert.AreEqual(1, keys.Count);
			Assert.Contains("localKey", keys);
			Assert.IsFalse(keys.Contains("parentKey"));
		}

		[Test]
		public void GetAllVariables_IncludesParentVariables()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("parentVar", 100);
			_blackboard.Set("localVar", 200);
			_blackboard.SetParent(parent);

			var allVars = new List<BlackboardVariable>(_blackboard.GetAllVariables());

			Assert.AreEqual(2, allVars.Count);
			Assert.IsTrue(allVars.Exists(v => v.Key == "localVar"));
			Assert.IsTrue(allVars.Exists(v => v.Key == "parentVar"));
		}

		[Test]
		public void GetAllVariables_LocalVariablesShadowParent()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("shared", 100);
			_blackboard.Set("shared", 200);
			_blackboard.SetParent(parent);

			var allVars = new List<BlackboardVariable>(_blackboard.GetAllVariables());

			// Should only have one "shared" variable (the local one)
			var sharedVars = allVars.FindAll(v => v.Key == "shared");
			Assert.AreEqual(1, sharedVars.Count);
			Assert.AreEqual(200, sharedVars[0].BoxedValue);
		}

		[Test]
		public void GetVariable_ChecksParentChain()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("parentVar", 100);
			_blackboard.SetParent(parent);

			var variable = _blackboard.GetVariable("parentVar");

			Assert.IsNotNull(variable);
			Assert.AreEqual(100, variable.BoxedValue);
		}

		[Test]
		public void GetVariable_PrefersLocalOverParent()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("shared", 100);
			_blackboard.Set("shared", 200);
			_blackboard.SetParent(parent);

			var variable = _blackboard.GetVariable("shared");

			Assert.AreEqual(200, variable.BoxedValue);
		}

		[Test]
		public void GetBoxed_ChecksParentChain()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("parentValue", 42);
			_blackboard.SetParent(parent);

			var result = _blackboard.GetBoxed("parentValue");

			Assert.AreEqual(42, result);
		}

		[Test]
		public void GetBoxed_PrefersLocalOverParent()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("shared", 100);
			_blackboard.Set("shared", 200);
			_blackboard.SetParent(parent);

			var result = _blackboard.GetBoxed("shared");

			Assert.AreEqual(200, result);
		}

		[Test]
		public void GetValueType_ChecksParentChain()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("parentString", "hello");
			_blackboard.SetParent(parent);

			var type = _blackboard.GetValueType("parentString");

			Assert.AreEqual(typeof(string), type);
		}

		[Test]
		public void TryGet_ChecksParentChain()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("parentValue", 42);
			_blackboard.SetParent(parent);

			var success = _blackboard.TryGet<int>("parentValue", out var value);

			Assert.IsTrue(success);
			Assert.AreEqual(42, value);
		}

		[Test]
		public void Parent_Property_ReturnsCurrentParent()
		{
			var parent = new Blackboard.Blackboard();
			_blackboard.SetParent(parent);

			Assert.AreSame(parent, _blackboard.Parent);
		}

		[Test]
		public void Parent_Property_ReturnsNullWhenNoParent()
		{
			Assert.IsNull(_blackboard.Parent);
		}

		[Test]
		public void SetParent_CanChangeParent()
		{
			var parent1 = new Blackboard.Blackboard();
			var parent2 = new Blackboard.Blackboard();
			parent1.Set("fromParent1", 1);
			parent2.Set("fromParent2", 2);

			_blackboard.SetParent(parent1);
			Assert.IsTrue(_blackboard.HasKey("fromParent1"));
			Assert.IsFalse(_blackboard.HasKey("fromParent2"));

			_blackboard.SetParent(parent2);
			Assert.IsFalse(_blackboard.HasKey("fromParent1"));
			Assert.IsTrue(_blackboard.HasKey("fromParent2"));
		}

		[Test]
		public void Clear_DoesNotAffectParent()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("parentValue", 100);
			_blackboard.Set("localValue", 200);
			_blackboard.SetParent(parent);

			_blackboard.Clear();

			Assert.IsFalse(_blackboard.HasLocalVariable("localValue"));
			Assert.IsTrue(_blackboard.HasKey("parentValue"));
			Assert.AreEqual(100, _blackboard.Get<int>("parentValue"));
		}

		[Test]
		public void Remove_DoesNotAffectParent()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("shared", 100);
			_blackboard.Set("shared", 200);
			_blackboard.SetParent(parent);

			_blackboard.Remove("shared");

			// After removing local, should fall back to parent
			Assert.AreEqual(100, _blackboard.Get<int>("shared"));
		}

		[Test]
		public void Set_SameValue_DoesNotFireChangeEvent()
		{
			_blackboard.Set("health", 100);

			var eventFired = false;
			_blackboard.OnVariableChanged += (key, oldVal, newVal) => eventFired = true;

			_blackboard.Set("health", 100);

			Assert.IsFalse(eventFired);
		}

		[Test]
		public void Set_DifferentValue_FiresChangeEventWithCorrectValues()
		{
			_blackboard.Set("health", 100);

			object capturedOld = null;
			object capturedNew = null;
			_blackboard.OnVariableChanged += (key, oldVal, newVal) =>
			{
				capturedOld = oldVal;
				capturedNew = newVal;
			};

			_blackboard.Set("health", 200);

			Assert.AreEqual(100, capturedOld);
			Assert.AreEqual(200, capturedNew);
		}

		[Test]
		public void Set_SameStringValue_DoesNotFireChangeEvent()
		{
			_blackboard.Set("name", "Alice");

			var eventFired = false;
			_blackboard.OnVariableChanged += (key, oldVal, newVal) => eventFired = true;

			_blackboard.Set("name", "Alice");

			Assert.IsFalse(eventFired);
		}

		[Test]
		public void Set_TypeMismatch_FallsBackToBoxedEquality()
		{
			// Set as int first
			_blackboard.SetBoxed("value", 42, typeof(int));

			var eventFired = false;
			_blackboard.OnVariableChanged += (key, oldVal, newVal) => eventFired = true;

			// Set as float - type mismatch triggers boxed fallback path
			_blackboard.Set("value", 42f);

			Assert.IsTrue(eventFired);
		}

		[Test]
		public void GetAllVariables_NoParent_ReturnsLocalVariables()
		{
			_blackboard.Set("a", 1);
			_blackboard.Set("b", 2);

			var allVars = new List<BlackboardVariable>(_blackboard.GetAllVariables());

			Assert.AreEqual(2, allVars.Count);
		}

		[Test]
		public void GetAllVariables_NoParent_ReturnsEnumerableDirectly()
		{
			_blackboard.Set("a", 1);

			// Should return _variables.Values directly (no HashSet allocation)
			var result1 = _blackboard.GetAllVariables();
			var result2 = _blackboard.GetAllVariables();

			// Both calls should return the same collection instance
			Assert.AreSame(result1, result2);
		}

		[Test]
		public void SetBoxed_SameValue_DoesNotFireChangeEvent()
		{
			_blackboard.SetBoxed("health", 100, typeof(int));

			var eventFired = false;
			_blackboard.OnVariableChanged += (key, oldVal, newVal) => eventFired = true;

			_blackboard.SetBoxed("health", 100, typeof(int));

			Assert.IsFalse(eventFired);
		}

		[Test]
		public void SetBoxed_DifferentValue_FiresChangeEvent()
		{
			_blackboard.SetBoxed("health", 100, typeof(int));

			object capturedOld = null;
			object capturedNew = null;
			_blackboard.OnVariableChanged += (key, oldVal, newVal) =>
			{
				capturedOld = oldVal;
				capturedNew = newVal;
			};

			_blackboard.SetBoxed("health", 200, typeof(int));

			Assert.AreEqual(100, capturedOld);
			Assert.AreEqual(200, capturedNew);
		}

		[Test]
		public void SetBoxed_SameStringValue_DoesNotFireChangeEvent()
		{
			_blackboard.SetBoxed("name", "Alice", typeof(string));

			var eventFired = false;
			_blackboard.OnVariableChanged += (key, oldVal, newVal) => eventFired = true;

			_blackboard.SetBoxed("name", "Alice", typeof(string));

			Assert.IsFalse(eventFired);
		}

		[Test]
		public void SetBoxed_NullToValue_FiresChangeEvent()
		{
			_blackboard.SetBoxed("name", null, typeof(string));

			var eventFired = false;
			_blackboard.OnVariableChanged += (key, oldVal, newVal) => eventFired = true;

			_blackboard.SetBoxed("name", "Alice", typeof(string));

			Assert.IsTrue(eventFired);
		}

		[Test]
		public void SetBoxed_ValueToNull_FiresChangeEvent()
		{
			_blackboard.SetBoxed("name", "Alice", typeof(string));

			var eventFired = false;
			_blackboard.OnVariableChanged += (key, oldVal, newVal) => eventFired = true;

			_blackboard.SetBoxed("name", null, typeof(string));

			Assert.IsTrue(eventFired);
		}

		[Test]
		public void SetBoxed_NewKey_FiresAddedEvent()
		{
			string addedKey = null;
			_blackboard.OnVariableAdded += (key, val) => addedKey = key;

			_blackboard.SetBoxed("newVar", 42, typeof(int));

			Assert.AreEqual("newVar", addedKey);
		}

		[Test]
		public void Clear_WithNoSubscribers_ClearsVariables()
		{
			_blackboard.Set("a", 1);
			_blackboard.Set("b", 2);

			_blackboard.Clear();

			Assert.AreEqual(0, _blackboard.Count);
			Assert.IsFalse(_blackboard.HasKey("a"));
			Assert.IsFalse(_blackboard.HasKey("b"));
		}

		[Test]
		public void Clear_WithSubscribers_FiresRemovedForEachKey()
		{
			_blackboard.Set("a", 1);
			_blackboard.Set("b", 2);

			var removedKeys = new List<string>();
			_blackboard.OnVariableRemoved += key => removedKeys.Add(key);

			_blackboard.Clear();

			Assert.AreEqual(2, removedKeys.Count);
			Assert.Contains("a", removedKeys);
			Assert.Contains("b", removedKeys);
		}

		[Test]
		public void TryGetBoxed_ExistingKey_ReturnsTrueAndValue()
		{
			_blackboard.Set("health", 42);

			var found = _blackboard.TryGetBoxed("health", out var value);

			Assert.IsTrue(found);
			Assert.AreEqual(42, value);
		}

		[Test]
		public void TryGetBoxed_NonExistentKey_ReturnsFalse()
		{
			var found = _blackboard.TryGetBoxed("missing", out var value);

			Assert.IsFalse(found);
			Assert.IsNull(value);
		}

		[Test]
		public void TryGetBoxed_KeyInParent_ReturnsTrueAndValue()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("parentVar", "hello");
			var child = new Blackboard.Blackboard();
			child.SetParent(parent);

			var found = child.TryGetBoxed("parentVar", out var value);

			Assert.IsTrue(found);
			Assert.AreEqual("hello", value);
		}

		[Test]
		public void TryGetBoxed_ChildOverridesParent_ReturnsChildValue()
		{
			var parent = new Blackboard.Blackboard();
			parent.Set("shared", 10);
			var child = new Blackboard.Blackboard();
			child.SetParent(parent);
			child.Set("shared", 20);

			var found = child.TryGetBoxed("shared", out var value);

			Assert.IsTrue(found);
			Assert.AreEqual(20, value);
		}

	}
}
