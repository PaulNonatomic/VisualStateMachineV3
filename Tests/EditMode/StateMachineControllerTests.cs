using System;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class StateMachineControllerTests
	{
		private StateMachineGraph _graph;
		private StateMachineController<TestContext> _controller;
		private GameObject _testGameObject;

		private class TestContext
		{
			public int Value { get; set; } = 42;
			public string Name { get; set; } = "TestContext";
		}

		private class MonoBehaviourContext : MonoBehaviour
		{
			public int Health { get; set; } = 100;
		}

		private class TestStateA : State
		{
			public static int EnterCount;
			public static int UpdateCount;
			public static int FixedUpdateCount;
			public static int LateUpdateCount;
			public static int ExitCount;

			[Transition("To B")] public Action toB;

			public override void OnEnter() => EnterCount++;
			public override void OnUpdate() => UpdateCount++;
			public override void OnFixedUpdate() => FixedUpdateCount++;
			public override void OnLateUpdate() => LateUpdateCount++;
			public override void OnExit() => ExitCount++;

			public static void ResetCounters()
			{
				EnterCount = 0;
				UpdateCount = 0;
				FixedUpdateCount = 0;
				LateUpdateCount = 0;
				ExitCount = 0;
			}
		}

		private class TestStateB : State
		{
			public static int EnterCount;

			[Transition("To Exit")] public Action toExit;

			public override void OnEnter() => EnterCount++;

			public static void ResetCounters()
			{
				EnterCount = 0;
			}
		}

		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			_testGameObject = new GameObject("TestObject");
			TestStateA.ResetCounters();
			TestStateB.ResetCounters();

			// Clear static callbacks
			StateMachineControllerCallbacks.Reset();
		}

		[TearDown]
		public void TearDown()
		{
			_controller?.Dispose();
			_controller = null;

			if (_graph != null)
			{
				UnityEngine.Object.DestroyImmediate(_graph);
			}

			if (_testGameObject != null)
			{
				UnityEngine.Object.DestroyImmediate(_testGameObject);
			}

			StateMachineControllerCallbacks.Reset();
		}

		private void SetupSimpleGraph()
		{
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				DisplayName = "State A",
				StateTypeAssemblyQualifiedName = typeof(TestStateA).AssemblyQualifiedName
			};
			var stateB = new StateNode
			{
				Id = "stateB",
				DisplayName = "State B",
				StateTypeAssemblyQualifiedName = typeof(TestStateB).AssemblyQualifiedName
			};
			var exit = new ExitNode { Id = "exit", ExitName = "Done" };

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(stateB);
			_graph.AddNode(exit);
			_graph.EntryNodeId = "entry";

			// Entry -> State A
			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "stateA",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// State A -> State B
			_graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "stateA",
				SourcePortName = "toB",
				TargetNodeId = "stateB",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// State B -> Exit
			_graph.AddConnection(new Connection
			{
				Id = "c3",
				SourceNodeId = "stateB",
				SourcePortName = "toExit",
				TargetNodeId = "exit",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});
		}


		[Test]
		public void Constructor_NullGraph_ThrowsArgumentNullException()
		{
			var context = new TestContext();

			Assert.Throws<ArgumentNullException>(() =>
				new StateMachineController<TestContext>(null, context, _testGameObject));
		}

		[Test]
		public void Constructor_NullContext_ThrowsArgumentNullException()
		{
			SetupSimpleGraph();

			Assert.Throws<ArgumentNullException>(() =>
				new StateMachineController<TestContext>(_graph, null, _testGameObject));
		}

		[Test]
		public void Constructor_ValidArguments_CreatesController()
		{
			SetupSimpleGraph();
			var context = new TestContext();

			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			Assert.IsNotNull(_controller);
			Assert.IsNotNull(_controller.Instance);
		}

		[Test]
		public void Constructor_ComponentContext_UsesComponentGameObject()
		{
			SetupSimpleGraph();
			var component = _testGameObject.AddComponent<MonoBehaviourContext>();

			var controller = new StateMachineController<MonoBehaviourContext>(_graph, component);

			Assert.IsNotNull(controller);
			Assert.IsNotNull(controller.Instance);
			controller.Dispose();
		}

		[Test]
		public void Constructor_NullOwnerWithNonComponentContext_DoesNotThrow()
		{
			SetupSimpleGraph();
			var context = new TestContext();

			Assert.DoesNotThrow(() =>
			{
				_controller = new StateMachineController<TestContext>(_graph, context, null);
			});
		}



		[Test]
		public void Instance_ReturnsStateMachineInstance()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			Assert.IsNotNull(_controller.Instance);
			Assert.IsInstanceOf<StateMachineInstance>(_controller.Instance);
		}

		[Test]
		public void Context_ReturnsProvidedContext()
		{
			SetupSimpleGraph();
			var context = new TestContext { Value = 123 };
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			Assert.AreEqual(context, _controller.Context);
			Assert.AreEqual(123, _controller.Context.Value);
		}

		[Test]
		public void IsRunning_BeforeStart_ReturnsFalse()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			Assert.IsFalse(_controller.IsRunning);
		}

		[Test]
		public void IsRunning_AfterStart_ReturnsTrue()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			_controller.Start();

			Assert.IsTrue(_controller.IsRunning);
		}

		[Test]
		public void IsRunning_AfterStop_ReturnsFalse()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();

			_controller.Stop();

			Assert.IsFalse(_controller.IsRunning);
		}

		[Test]
		public void IsPaused_BeforePause_ReturnsFalse()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();

			Assert.IsFalse(_controller.IsPaused);
		}

		[Test]
		public void IsPaused_AfterPause_ReturnsTrue()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();

			_controller.Pause();

			Assert.IsTrue(_controller.IsPaused);
		}

		[Test]
		public void IsPaused_AfterResume_ReturnsFalse()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();
			_controller.Pause();

			_controller.Resume();

			Assert.IsFalse(_controller.IsPaused);
		}

		[Test]
		public void CurrentState_BeforeStart_ReturnsNull()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			Assert.IsNull(_controller.CurrentState);
		}

		[Test]
		public void CurrentState_AfterStart_ReturnsActiveState()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			_controller.Start();

			Assert.IsNotNull(_controller.CurrentState);
			Assert.IsInstanceOf<TestStateA>(_controller.CurrentState);
		}

		[Test]
		public void Blackboard_ReturnsInstanceBlackboard()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			Assert.IsNotNull(_controller.Blackboard);
			Assert.AreSame(_controller.Instance.Blackboard, _controller.Blackboard);
		}



		[Test]
		public void Start_EntersFirstState()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			_controller.Start();

			Assert.AreEqual(1, TestStateA.EnterCount);
		}

		[Test]
		public void Stop_ExitsCurrentState()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();

			_controller.Stop();

			Assert.AreEqual(1, TestStateA.ExitCount);
		}

		[Test]
		public void Pause_StopsUpdates()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();
			_controller.Update();
			var countBeforePause = TestStateA.UpdateCount;

			_controller.Pause();
			_controller.Update();

			Assert.AreEqual(countBeforePause, TestStateA.UpdateCount);
		}

		[Test]
		public void Resume_ResumesUpdates()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();
			_controller.Pause();
			var countBeforeResume = TestStateA.UpdateCount;

			_controller.Resume();
			_controller.Update();

			Assert.AreEqual(countBeforeResume + 1, TestStateA.UpdateCount);
		}

		[Test]
		public void Restart_StopsAndStartsStateMachine()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();
			Assert.AreEqual(1, TestStateA.EnterCount);

			_controller.Restart();

			Assert.AreEqual(1, TestStateA.ExitCount);
			Assert.AreEqual(2, TestStateA.EnterCount);
		}



		[Test]
		public void Update_CallsCurrentStateUpdate()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();

			_controller.Update();
			_controller.Update();
			_controller.Update();

			Assert.AreEqual(3, TestStateA.UpdateCount);
		}

		[Test]
		public void Update_WhenNotRunning_DoesNothing()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			_controller.Update();

			Assert.AreEqual(0, TestStateA.UpdateCount);
		}

		[Test]
		public void FixedUpdate_CallsCurrentStateFixedUpdate()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();

			_controller.FixedUpdate();
			_controller.FixedUpdate();

			Assert.AreEqual(2, TestStateA.FixedUpdateCount);
		}

		[Test]
		public void LateUpdate_CallsCurrentStateLateUpdate()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();

			_controller.LateUpdate();
			_controller.LateUpdate();

			Assert.AreEqual(2, TestStateA.LateUpdateCount);
		}



		[Test]
		public void SetBlackboardValue_SetsValue()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			_controller.SetBlackboardValue("testKey", 42);

			Assert.AreEqual(42, _controller.Blackboard.Get<int>("testKey"));
		}

		[Test]
		public void GetBlackboardValue_RetrievesValue()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Blackboard.Set("testKey", "testValue");

			var value = _controller.GetBlackboardValue<string>("testKey");

			Assert.AreEqual("testValue", value);
		}

		[Test]
		public void GetBlackboardValue_KeyNotFound_ReturnsDefaultValue()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			var value = _controller.GetBlackboardValue("nonExistent", 99);

			Assert.AreEqual(99, value);
		}

		[Test]
		public void SetBlackboardValue_BeforeStart_Works()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			_controller.SetBlackboardValue("earlyKey", 123);

			Assert.AreEqual(123, _controller.GetBlackboardValue<int>("earlyKey"));
		}



		[Test]
		public void OnStarted_FiresOnStart()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			var eventFired = false;
			_controller.OnStarted += () => eventFired = true;

			_controller.Start();

			Assert.IsTrue(eventFired);
		}

		[Test]
		public void OnStopped_FiresOnStop()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();
			var eventFired = false;
			_controller.OnStopped += () => eventFired = true;

			_controller.Stop();

			Assert.IsTrue(eventFired);
		}

		[Test]
		public void OnStateChanged_FiresOnTransition()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			State previousState = null;
			State newState = null;
			_controller.OnStateChanged += (prev, next) =>
			{
				previousState = prev;
				newState = next;
			};

			_controller.Start();

			// First transition (from null to TestStateA)
			Assert.IsNull(previousState);
			Assert.IsInstanceOf<TestStateA>(newState);
		}



		[Test]
		public void Dispose_StopsStateMachine()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();

			_controller.Dispose();

			Assert.IsFalse(_controller.IsRunning);
		}

		[Test]
		public void Dispose_SetsInstanceToNull()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();

			_controller.Dispose();

			Assert.IsNull(_controller.Instance);
		}

		[Test]
		public void Dispose_CalledTwice_DoesNotThrow()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();

			_controller.Dispose();

			Assert.DoesNotThrow(() => _controller.Dispose());
		}

		[Test]
		public void Dispose_ExitsCurrentState()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();

			_controller.Dispose();

			Assert.AreEqual(1, TestStateA.ExitCount);
		}



		[Test]
		public void OnControllerCreated_CalledDuringConstruction()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			StateMachineInstance capturedInstance = null;
			StateMachineGraph capturedGraph = null;
			GameObject capturedOwner = null;

			StateMachineControllerCallbacks.OnControllerCreated += (instance, graph, owner) =>
			{
				capturedInstance = instance;
				capturedGraph = graph;
				capturedOwner = owner;
			};

			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			Assert.IsNotNull(capturedInstance);
			Assert.AreSame(_graph, capturedGraph);
			Assert.AreSame(_testGameObject, capturedOwner);
		}

		[Test]
		public void OnControllerDisposed_CalledDuringDispose()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			StateMachineInstance capturedInstance = null;

			StateMachineControllerCallbacks.OnControllerDisposed += instance =>
			{
				capturedInstance = instance;
			};

			var originalInstance = _controller.Instance;
			_controller.Dispose();

			Assert.AreSame(originalInstance, capturedInstance);
		}



		[Test]
		public void OnControllerCreated_MultipleSubscribers_AllNotified()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			var callCount1 = 0;
			var callCount2 = 0;

			StateMachineControllerCallbacks.OnControllerCreated += (instance, graph, owner) => callCount1++;
			StateMachineControllerCallbacks.OnControllerCreated += (instance, graph, owner) => callCount2++;

			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			Assert.AreEqual(1, callCount1);
			Assert.AreEqual(1, callCount2);
		}

		[Test]
		public void Reset_RemovesAllSubscribers()
		{
			var callbackCalled = false;
			StateMachineControllerCallbacks.OnControllerCreated += (instance, graph, owner) => callbackCalled = true;

			StateMachineControllerCallbacks.Reset();

			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			Assert.IsFalse(callbackCalled);
		}

		[Test]
		public void NonGenericController_CanBeCreated()
		{
			SetupSimpleGraph();

			var controller = new StateMachineController(_graph, _testGameObject);

			Assert.IsNotNull(controller);
			Assert.IsNotNull(controller.Instance);
			controller.Dispose();
		}

		[Test]
		public void NonGenericController_StartsAndStops()
		{
			SetupSimpleGraph();
			var controller = new StateMachineController(_graph, _testGameObject);

			controller.Start();
			Assert.IsTrue(controller.IsRunning);
			Assert.AreEqual(1, TestStateA.EnterCount);

			controller.Stop();
			Assert.IsFalse(controller.IsRunning);

			controller.Dispose();
		}



		[Test]
		public void Start_CalledTwice_DoesNotRestartStateMachine()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			_controller.Start();
			_controller.Start();

			// Should only enter once
			Assert.AreEqual(1, TestStateA.EnterCount);
		}

		[Test]
		public void Stop_WhenNotRunning_DoesNotThrow()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			Assert.DoesNotThrow(() => _controller.Stop());
		}

		[Test]
		public void Pause_WhenNotRunning_DoesNotThrow()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);

			Assert.DoesNotThrow(() => _controller.Pause());
		}

		[Test]
		public void Resume_WhenNotPaused_DoesNotThrow()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();

			Assert.DoesNotThrow(() => _controller.Resume());
		}

		[Test]
		public void Update_AfterDispose_DoesNotThrow()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();
			_controller.Dispose();

			Assert.DoesNotThrow(() => _controller.Update());
		}

		[Test]
		public void FixedUpdate_AfterDispose_DoesNotThrow()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();
			_controller.Dispose();

			Assert.DoesNotThrow(() => _controller.FixedUpdate());
		}

		[Test]
		public void LateUpdate_AfterDispose_DoesNotThrow()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Start();
			_controller.Dispose();

			Assert.DoesNotThrow(() => _controller.LateUpdate());
		}

		[Test]
		public void SetBlackboardValue_AfterDispose_DoesNotThrow()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Dispose();

			Assert.DoesNotThrow(() => _controller.SetBlackboardValue("key", 1));
		}

		[Test]
		public void GetBlackboardValue_AfterDispose_ReturnsDefault()
		{
			SetupSimpleGraph();
			var context = new TestContext();
			_controller = new StateMachineController<TestContext>(_graph, context, _testGameObject);
			_controller.Dispose();

			var value = _controller.GetBlackboardValue("key", 99);

			Assert.AreEqual(99, value);
		}

	}
}
