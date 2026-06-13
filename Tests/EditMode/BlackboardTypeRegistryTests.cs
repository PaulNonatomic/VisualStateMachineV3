using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Nonatomic.VSM.Blackboard;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class BlackboardTypeRegistryTests
	{
		[TearDown]
		public void TearDown()
		{
			// Clean up any custom handlers registered during tests
			// This ensures test isolation between test methods
			BlackboardTypeRegistry.Unregister(typeof(TestCustomType));
		}

		[Test]
		public void HasHandler_RegisteredType_ReturnsTrue()
		{
			Assert.IsTrue(BlackboardTypeRegistry.HasHandler(typeof(int)));
			Assert.IsTrue(BlackboardTypeRegistry.HasHandler(typeof(float)));
			Assert.IsTrue(BlackboardTypeRegistry.HasHandler(typeof(bool)));
			Assert.IsTrue(BlackboardTypeRegistry.HasHandler(typeof(string)));
		}

		[Test]
		public void HasHandler_UnregisteredType_ReturnsFalse()
		{
			Assert.IsFalse(BlackboardTypeRegistry.HasHandler(typeof(DateTime)));
		}

		[Test]
		public void GetHandler_RegisteredType_ReturnsHandler()
		{
			var handler = BlackboardTypeRegistry.GetHandler(typeof(int));

			Assert.IsNotNull(handler);
			Assert.AreEqual(typeof(int), handler.HandledType);
		}

		[Test]
		public void GetHandler_UnregisteredType_ReturnsNull()
		{
			var handler = BlackboardTypeRegistry.GetHandler(typeof(DateTime));

			Assert.IsNull(handler);
		}

		[Test]
		public void Serialize_Int_ReturnsString()
		{
			var result = BlackboardTypeRegistry.Serialize(42, typeof(int));

			Assert.AreEqual("42", result);
		}

		[Test]
		public void Deserialize_Int_ReturnsValue()
		{
			var result = BlackboardTypeRegistry.Deserialize("42", typeof(int));

			Assert.AreEqual(42, result);
		}

		[Test]
		public void Serialize_Float_ReturnsString()
		{
			var result = BlackboardTypeRegistry.Serialize(3.14f, typeof(float));

			Assert.IsNotNull(result);
			Assert.IsTrue(result.Contains("3.14") || result.Contains("3,14")); // Handle different locales
		}

		[Test]
		public void Deserialize_Float_ReturnsValue()
		{
			var result = BlackboardTypeRegistry.Deserialize("3.14", typeof(float));

			Assert.AreEqual(3.14f, (float)result, 0.001f);
		}

		[Test]
		public void Serialize_Bool_True_ReturnsString()
		{
			var result = BlackboardTypeRegistry.Serialize(true, typeof(bool));

			Assert.AreEqual("True", result);
		}

		[Test]
		public void Deserialize_Bool_True_ReturnsValue()
		{
			var result = BlackboardTypeRegistry.Deserialize("True", typeof(bool));

			Assert.AreEqual(true, result);
		}

		[Test]
		public void Serialize_String_ReturnsString()
		{
			var result = BlackboardTypeRegistry.Serialize("hello", typeof(string));

			Assert.AreEqual("hello", result);
		}

		[Test]
		public void Deserialize_String_ReturnsValue()
		{
			var result = BlackboardTypeRegistry.Deserialize("hello", typeof(string));

			Assert.AreEqual("hello", result);
		}

		[Test]
		public void Serialize_Vector3_ReturnsJson()
		{
			var vec = new Vector3(1f, 2f, 3f);
			var result = BlackboardTypeRegistry.Serialize(vec, typeof(Vector3));

			Assert.IsNotNull(result);
			Assert.IsTrue(result.Contains("1") && result.Contains("2") && result.Contains("3"));
		}

		[Test]
		public void Deserialize_Vector3_ReturnsValue()
		{
			var json = "{\"x\":1.0,\"y\":2.0,\"z\":3.0}";
			var result = BlackboardTypeRegistry.Deserialize(json, typeof(Vector3));

			Assert.IsInstanceOf<Vector3>(result);
			var vec = (Vector3)result;
			Assert.AreEqual(1f, vec.x, 0.001f);
			Assert.AreEqual(2f, vec.y, 0.001f);
			Assert.AreEqual(3f, vec.z, 0.001f);
		}

		[Test]
		public void Serialize_Color_ReturnsJson()
		{
			var color = new Color(1f, 0.5f, 0.25f, 1f);
			var result = BlackboardTypeRegistry.Serialize(color, typeof(Color));

			Assert.IsNotNull(result);
		}

		[Test]
		public void Deserialize_Color_ReturnsValue()
		{
			var json = "{\"r\":1.0,\"g\":0.5,\"b\":0.25,\"a\":1.0}";
			var result = BlackboardTypeRegistry.Deserialize(json, typeof(Color));

			Assert.IsInstanceOf<Color>(result);
			var color = (Color)result;
			Assert.AreEqual(1f, color.r, 0.001f);
			Assert.AreEqual(0.5f, color.g, 0.001f);
			Assert.AreEqual(0.25f, color.b, 0.001f);
		}

		[Test]
		public void Serialize_Null_ReturnsNull()
		{
			var result = BlackboardTypeRegistry.Serialize(null, typeof(int));

			Assert.IsNull(result);
		}

		[Test]
		public void Deserialize_EmptyString_ReturnsNull()
		{
			var result = BlackboardTypeRegistry.Deserialize("", typeof(int));

			Assert.IsNull(result);
		}

		[Test]
		public void Deserialize_NullString_ReturnsNull()
		{
			var result = BlackboardTypeRegistry.Deserialize(null, typeof(int));

			Assert.IsNull(result);
		}

		[Test]
		public void GetDefaultValue_Int_ReturnsZero()
		{
			var result = BlackboardTypeRegistry.GetDefaultValue(typeof(int));

			Assert.AreEqual(0, result);
		}

		[Test]
		public void GetDefaultValue_Float_ReturnsZero()
		{
			var result = BlackboardTypeRegistry.GetDefaultValue(typeof(float));

			Assert.AreEqual(0f, result);
		}

		[Test]
		public void GetDefaultValue_Bool_ReturnsFalse()
		{
			var result = BlackboardTypeRegistry.GetDefaultValue(typeof(bool));

			Assert.AreEqual(false, result);
		}

		[Test]
		public void GetDefaultValue_String_ReturnsEmpty()
		{
			var result = BlackboardTypeRegistry.GetDefaultValue(typeof(string));

			Assert.AreEqual(string.Empty, result);
		}

		[Test]
		public void GetDefaultValue_Vector3_ReturnsZero()
		{
			var result = BlackboardTypeRegistry.GetDefaultValue(typeof(Vector3));

			Assert.AreEqual(Vector3.zero, result);
		}

		[Test]
		public void GetDefaultValue_Color_ReturnsWhite()
		{
			var result = BlackboardTypeRegistry.GetDefaultValue(typeof(Color));

			Assert.AreEqual(Color.white, result);
		}

		[Test]
		public void Handlers_ContainsAllPrimitiveTypes()
		{
			Assert.IsTrue(BlackboardTypeRegistry.HasHandler(typeof(int)));
			Assert.IsTrue(BlackboardTypeRegistry.HasHandler(typeof(float)));
			Assert.IsTrue(BlackboardTypeRegistry.HasHandler(typeof(bool)));
			Assert.IsTrue(BlackboardTypeRegistry.HasHandler(typeof(string)));
			Assert.IsTrue(BlackboardTypeRegistry.HasHandler(typeof(double)));
			Assert.IsTrue(BlackboardTypeRegistry.HasHandler(typeof(long)));
		}

		[Test]
		public void Handlers_ContainsMathTypes()
		{
			Assert.IsTrue(BlackboardTypeRegistry.HasHandler(typeof(Vector2)));
			Assert.IsTrue(BlackboardTypeRegistry.HasHandler(typeof(Vector3)));
			Assert.IsTrue(BlackboardTypeRegistry.HasHandler(typeof(Vector4)));
			Assert.IsTrue(BlackboardTypeRegistry.HasHandler(typeof(Quaternion)));
			Assert.IsTrue(BlackboardTypeRegistry.HasHandler(typeof(Color)));
			Assert.IsTrue(BlackboardTypeRegistry.HasHandler(typeof(Rect)));
			Assert.IsTrue(BlackboardTypeRegistry.HasHandler(typeof(Bounds)));
		}

		[Test]
		public void Handler_HasCorrectDisplayName()
		{
			var handler = BlackboardTypeRegistry.GetHandler(typeof(int));

			Assert.AreEqual("Int", handler.DisplayName);
		}

		[Test]
		public void Handler_HasCorrectCategory()
		{
			var intHandler = BlackboardTypeRegistry.GetHandler(typeof(int));
			var vectorHandler = BlackboardTypeRegistry.GetHandler(typeof(Vector3));

			Assert.AreEqual("Primitives", intHandler.Category);
			Assert.AreEqual("Math", vectorHandler.Category);
		}

		[Test]
		public void Register_CustomHandler_CanBeRetrieved()
		{
			var customHandler = new TestCustomHandler();

			BlackboardTypeRegistry.Register(customHandler);
			var retrieved = BlackboardTypeRegistry.GetHandler(typeof(TestCustomType));

			Assert.IsNotNull(retrieved);
			Assert.AreEqual(typeof(TestCustomType), retrieved.HandledType);
		}

		[Test]
		public void Serialize_Quaternion_ReturnsJson()
		{
			var quat = Quaternion.Euler(45f, 90f, 0f);
			var result = BlackboardTypeRegistry.Serialize(quat, typeof(Quaternion));

			Assert.IsNotNull(result);
		}

		[Test]
		public void Deserialize_InvalidJson_ReturnsNull()
		{
			var result = BlackboardTypeRegistry.Deserialize("not valid json", typeof(Vector3));

			// Should return null or default for invalid JSON
			Assert.IsTrue(result == null || result.Equals(default(Vector3)));
		}

		[Test]
		public void Register_ConcurrentAccess_DoesNotThrow()
		{
			var barrier = new Barrier(4);
			var tasks = new Task[4];

			for (var i = 0; i < 4; i++)
			{
				var index = i;
				tasks[i] = Task.Run(() =>
				{
					barrier.SignalAndWait();
					for (var j = 0; j < 100; j++)
					{
						BlackboardTypeRegistry.Register(new TestCustomHandler());
						BlackboardTypeRegistry.HasHandler(typeof(TestCustomType));
						BlackboardTypeRegistry.GetHandler(typeof(TestCustomType));
						BlackboardTypeRegistry.Unregister(typeof(TestCustomType));
					}
				});
			}

			Assert.DoesNotThrow(() => Task.WaitAll(tasks));
		}

		[Test]
		public void GetHandler_ConcurrentReads_ReturnConsistentResults()
		{
			BlackboardTypeRegistry.Register(new TestCustomHandler());

			var tasks = new Task<IBlackboardTypeHandler>[8];
			var barrier = new Barrier(8);

			for (var i = 0; i < 8; i++)
			{
				tasks[i] = Task.Run(() =>
				{
					barrier.SignalAndWait();
					return BlackboardTypeRegistry.GetHandler(typeof(TestCustomType));
				});
			}

			Task.WaitAll(tasks);

			foreach (var task in tasks)
			{
				Assert.IsNotNull(task.Result);
				Assert.AreEqual(typeof(TestCustomType), task.Result.HandledType);
			}
		}

		[Test]
		public void Unregister_RegisteredType_ReturnsTrue()
		{
			BlackboardTypeRegistry.Register(new TestCustomHandler());

			var result = BlackboardTypeRegistry.Unregister(typeof(TestCustomType));

			Assert.IsTrue(result);
			Assert.IsFalse(BlackboardTypeRegistry.HasHandler(typeof(TestCustomType)));
		}

		[Test]
		public void Unregister_UnregisteredType_ReturnsFalse()
		{
			// Ensure it's not registered
			BlackboardTypeRegistry.Unregister(typeof(TestCustomType));

			var result = BlackboardTypeRegistry.Unregister(typeof(TestCustomType));

			Assert.IsFalse(result);
		}

		[Test]
		public void Unregister_NullType_ReturnsFalse()
		{
			var result = BlackboardTypeRegistry.Unregister(null);

			Assert.IsFalse(result);
		}

		[Test]
		public void Register_NullHandler_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => BlackboardTypeRegistry.Register(null));
		}

		[Test]
		public void GetHandler_NullType_ReturnsNull()
		{
			var result = BlackboardTypeRegistry.GetHandler(null);

			Assert.IsNull(result);
		}

		[Test]
		public void HasHandler_NullType_ReturnsFalse()
		{
			var result = BlackboardTypeRegistry.HasHandler(null);

			Assert.IsFalse(result);
		}

		private struct TestCustomType
		{
			public int Value;
		}

		private class TestCustomHandler : IBlackboardTypeHandler
		{
			public Type HandledType => typeof(TestCustomType);
			public string DisplayName => "Test Custom";
			public string Category => "Custom";
			public int Order => 0;

			public string Serialize(object value)
			{
				if (value is TestCustomType custom)
				{
					return custom.Value.ToString();
				}
				return null;
			}

			public object Deserialize(string json)
			{
				if (int.TryParse(json, out var val))
				{
					return new TestCustomType { Value = val };
				}
				return default(TestCustomType);
			}

			public object GetDefaultValue()
			{
				return new TestCustomType { Value = 0 };
			}
		}
	}
}
