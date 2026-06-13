using NUnit.Framework;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class StateMachineRunnerOfTTests
	{
		private class TestContext
		{
			public int Health { get; set; }
			public float Speed { get; set; }
		}

		private class TestRunner : StateMachineRunner<TestContext>
		{
			public int CreateSharedContextCallCount { get; private set; }

			protected override TestContext CreateSharedContext()
			{
				CreateSharedContextCallCount++;
				return new TestContext { Health = 100, Speed = 5.0f };
			}
		}

		private class CustomContextRunner : StateMachineRunner<TestContext>
		{
			public TestContext ContextToReturn { get; set; }

			protected override TestContext CreateSharedContext()
			{
				return ContextToReturn;
			}
		}

		private GameObject _gameObject;

		[TearDown]
		public void TearDown()
		{
			if (_gameObject != null)
			{
				Object.DestroyImmediate(_gameObject);
				_gameObject = null;
			}
		}

		[Test]
		public void SharedContext_BeforeInitialize_IsNull()
		{
			_gameObject = new GameObject("TestRunner");
			var runner = _gameObject.AddComponent<TestRunner>();
			runner.AutoStart = false;

			Assert.IsNull(runner.SharedContext);
		}

		[Test]
		public void SetSharedContext_SetsContext()
		{
			_gameObject = new GameObject("TestRunner");
			var runner = _gameObject.AddComponent<TestRunner>();
			runner.AutoStart = false;

			var context = new TestContext { Health = 50, Speed = 3.0f };
			runner.SetSharedContext(context);

			Assert.AreSame(context, runner.SharedContext);
		}

		[Test]
		public void SetSharedContext_UpdatesContextValues()
		{
			_gameObject = new GameObject("TestRunner");
			var runner = _gameObject.AddComponent<TestRunner>();
			runner.AutoStart = false;

			var context = new TestContext { Health = 75, Speed = 10.0f };
			runner.SetSharedContext(context);

			Assert.AreEqual(75, runner.SharedContext.Health);
			Assert.AreEqual(10.0f, runner.SharedContext.Speed, 0.001f);
		}

		[Test]
		public void SetSharedContext_CalledTwice_ReplacesContext()
		{
			_gameObject = new GameObject("TestRunner");
			var runner = _gameObject.AddComponent<TestRunner>();
			runner.AutoStart = false;

			var context1 = new TestContext { Health = 50 };
			var context2 = new TestContext { Health = 200 };

			runner.SetSharedContext(context1);
			runner.SetSharedContext(context2);

			Assert.AreSame(context2, runner.SharedContext);
			Assert.AreEqual(200, runner.SharedContext.Health);
		}

		[Test]
		public void SetSharedContext_WithNull_SetsContextToNull()
		{
			_gameObject = new GameObject("TestRunner");
			var runner = _gameObject.AddComponent<TestRunner>();
			runner.AutoStart = false;

			runner.SetSharedContext(new TestContext());
			runner.SetSharedContext(null);

			Assert.IsNull(runner.SharedContext);
		}

		[Test]
		public void SetSharedContext_BeforeInstanceCreated_DoesNotThrow()
		{
			_gameObject = new GameObject("TestRunner");
			var runner = _gameObject.AddComponent<TestRunner>();
			runner.AutoStart = false;

			Assert.DoesNotThrow(() => runner.SetSharedContext(new TestContext()));
		}

		[Test]
		public void Initialize_WithGraph_CreatesInstanceWithSharedContext()
		{
			_gameObject = new GameObject("TestRunner");
			var runner = _gameObject.AddComponent<TestRunner>();
			runner.AutoStart = false;

			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			graph.InitializeNewGraph();

			runner.Initialize(graph);

			Assert.IsNotNull(runner.Instance);
			Assert.IsNotNull(runner.SharedContext);
			Assert.AreEqual(100, runner.SharedContext.Health);
			Assert.AreEqual(5.0f, runner.SharedContext.Speed, 0.001f);

			Object.DestroyImmediate(graph);
		}

		[Test]
		public void Initialize_CallsCreateSharedContext()
		{
			_gameObject = new GameObject("TestRunner");
			var runner = _gameObject.AddComponent<TestRunner>();
			runner.AutoStart = false;

			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			graph.InitializeNewGraph();

			runner.Initialize(graph);

			Assert.AreEqual(1, runner.CreateSharedContextCallCount);

			Object.DestroyImmediate(graph);
		}

		[Test]
		public void Initialize_WithPresetContext_UsesPresetContext()
		{
			_gameObject = new GameObject("TestRunner");
			var runner = _gameObject.AddComponent<TestRunner>();
			runner.AutoStart = false;

			var presetContext = new TestContext { Health = 999, Speed = 99.0f };
			runner.SetSharedContext(presetContext);

			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			graph.InitializeNewGraph();

			runner.Initialize(graph);

			Assert.AreSame(presetContext, runner.SharedContext);
			Assert.AreEqual(999, runner.SharedContext.Health);

			Object.DestroyImmediate(graph);
		}

		[Test]
		public void Initialize_WithPresetContext_DoesNotCallCreateSharedContext()
		{
			_gameObject = new GameObject("TestRunner");
			var runner = _gameObject.AddComponent<TestRunner>();
			runner.AutoStart = false;

			runner.SetSharedContext(new TestContext());

			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			graph.InitializeNewGraph();

			runner.Initialize(graph);

			Assert.AreEqual(0, runner.CreateSharedContextCallCount);

			Object.DestroyImmediate(graph);
		}

		[Test]
		public void Initialize_InstanceHasSharedContext()
		{
			_gameObject = new GameObject("TestRunner");
			var runner = _gameObject.AddComponent<TestRunner>();
			runner.AutoStart = false;

			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			graph.InitializeNewGraph();

			runner.Initialize(graph);

			Assert.IsTrue(runner.Instance.HasSharedContext);

			Object.DestroyImmediate(graph);
		}

		[Test]
		public void CustomContextRunner_UsesProvidedContext()
		{
			_gameObject = new GameObject("TestRunner");
			var runner = _gameObject.AddComponent<CustomContextRunner>();
			runner.AutoStart = false;

			var customContext = new TestContext { Health = 42, Speed = 7.5f };
			runner.ContextToReturn = customContext;

			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			graph.InitializeNewGraph();

			runner.Initialize(graph);

			Assert.AreSame(customContext, runner.SharedContext);
			Assert.AreEqual(42, runner.SharedContext.Health);

			Object.DestroyImmediate(graph);
		}
	}
}
