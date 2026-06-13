using NUnit.Framework;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class StateMachineContextTests
	{
		[Test]
		public void DefaultConstructor_GameObject_IsNull()
		{
			var context = new StateMachineContext();

			Assert.IsNull(context.GameObject);
		}

		[Test]
		public void DefaultConstructor_Transform_IsNull()
		{
			var context = new StateMachineContext();

			Assert.IsNull(context.Transform);
		}

		[Test]
		public void Constructor_WithGameObject_SetsGameObject()
		{
			var go = new GameObject("TestContext");
			try
			{
				var context = new StateMachineContext(go);

				Assert.AreSame(go, context.GameObject);
			}
			finally
			{
				Object.DestroyImmediate(go);
			}
		}

		[Test]
		public void Constructor_WithGameObject_SetsTransform()
		{
			var go = new GameObject("TestContext");
			try
			{
				var context = new StateMachineContext(go);

				Assert.AreSame(go.transform, context.Transform);
			}
			finally
			{
				Object.DestroyImmediate(go);
			}
		}

		[Test]
		public void Constructor_WithNull_GameObjectIsNull()
		{
			var context = new StateMachineContext(null);

			Assert.IsNull(context.GameObject);
		}

		[Test]
		public void Constructor_WithNull_TransformIsNull()
		{
			var context = new StateMachineContext(null);

			Assert.IsNull(context.Transform);
		}

		[Test]
		public void ImplementsIStateMachineContext()
		{
			var context = new StateMachineContext();

			Assert.IsInstanceOf<IStateMachineContext>(context);
		}

		[Test]
		public void Transform_MatchesGameObjectTransform()
		{
			var go = new GameObject("TestContext");
			try
			{
				var context = new StateMachineContext(go);

				Assert.AreEqual(context.GameObject.transform, context.Transform);
			}
			finally
			{
				Object.DestroyImmediate(go);
			}
		}
	}

	[TestFixture]
	public class StateMachineContextOfTTests
	{
		private class TestData
		{
			public int Health { get; set; }
			public string Name { get; set; }
		}

		[Test]
		public void Constructor_WithGameObject_SetsGameObject()
		{
			var go = new GameObject("TestContext");
			try
			{
				var context = new StateMachineContext<TestData>(go);

				Assert.AreSame(go, context.GameObject);
			}
			finally
			{
				Object.DestroyImmediate(go);
			}
		}

		[Test]
		public void Constructor_WithGameObject_DataIsDefault()
		{
			var go = new GameObject("TestContext");
			try
			{
				var context = new StateMachineContext<TestData>(go);

				Assert.IsNull(context.Data);
			}
			finally
			{
				Object.DestroyImmediate(go);
			}
		}

		[Test]
		public void Constructor_WithGameObjectAndData_SetsData()
		{
			var go = new GameObject("TestContext");
			try
			{
				var data = new TestData { Health = 100, Name = "Player" };
				var context = new StateMachineContext<TestData>(go, data);

				Assert.AreSame(data, context.Data);
				Assert.AreEqual(100, context.Data.Health);
				Assert.AreEqual("Player", context.Data.Name);
			}
			finally
			{
				Object.DestroyImmediate(go);
			}
		}

		[Test]
		public void Data_SetProperty_UpdatesValue()
		{
			var go = new GameObject("TestContext");
			try
			{
				var context = new StateMachineContext<TestData>(go);
				var data = new TestData { Health = 50 };

				context.Data = data;

				Assert.AreSame(data, context.Data);
				Assert.AreEqual(50, context.Data.Health);
			}
			finally
			{
				Object.DestroyImmediate(go);
			}
		}

		[Test]
		public void Data_SetToNull_ClearsData()
		{
			var go = new GameObject("TestContext");
			try
			{
				var data = new TestData { Health = 100 };
				var context = new StateMachineContext<TestData>(go, data);
				Assert.IsNotNull(context.Data);

				context.Data = null;

				Assert.IsNull(context.Data);
			}
			finally
			{
				Object.DestroyImmediate(go);
			}
		}

		[Test]
		public void Data_ReplaceExisting_UpdatesReference()
		{
			var go = new GameObject("TestContext");
			try
			{
				var data1 = new TestData { Health = 100 };
				var data2 = new TestData { Health = 200 };
				var context = new StateMachineContext<TestData>(go, data1);

				context.Data = data2;

				Assert.AreSame(data2, context.Data);
				Assert.AreEqual(200, context.Data.Health);
			}
			finally
			{
				Object.DestroyImmediate(go);
			}
		}

		[Test]
		public void Data_MutationsAreVisible()
		{
			var go = new GameObject("TestContext");
			try
			{
				var data = new TestData { Health = 100 };
				var context = new StateMachineContext<TestData>(go, data);

				data.Health = 50;

				Assert.AreEqual(50, context.Data.Health);
			}
			finally
			{
				Object.DestroyImmediate(go);
			}
		}

		[Test]
		public void InheritsFromStateMachineContext()
		{
			var go = new GameObject("TestContext");
			try
			{
				var context = new StateMachineContext<TestData>(go);

				Assert.IsInstanceOf<StateMachineContext>(context);
			}
			finally
			{
				Object.DestroyImmediate(go);
			}
		}

		[Test]
		public void ImplementsIStateMachineContext()
		{
			var go = new GameObject("TestContext");
			try
			{
				var context = new StateMachineContext<TestData>(go);

				Assert.IsInstanceOf<IStateMachineContext>(context);
			}
			finally
			{
				Object.DestroyImmediate(go);
			}
		}

		[Test]
		public void WithValueType_DefaultData_IsDefault()
		{
			var go = new GameObject("TestContext");
			try
			{
				var context = new StateMachineContext<int>(go);

				Assert.AreEqual(0, context.Data);
			}
			finally
			{
				Object.DestroyImmediate(go);
			}
		}

		[Test]
		public void WithValueType_SetsData()
		{
			var go = new GameObject("TestContext");
			try
			{
				var context = new StateMachineContext<int>(go, 42);

				Assert.AreEqual(42, context.Data);
			}
			finally
			{
				Object.DestroyImmediate(go);
			}
		}
	}
}
