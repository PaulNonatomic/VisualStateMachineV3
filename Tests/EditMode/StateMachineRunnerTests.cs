using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Blackboard;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class StateMachineRunnerTests
	{
		private StateMachineGraph _graph;
		private GameObject _testGameObject;
		private StateMachineRunner _runner;

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

		private class CustomRunner : StateMachineRunner
		{
			public bool CreateContextCalled;
			public bool CreateInstanceCalled;
			public IStateMachineContext CustomContext;

			protected override IStateMachineContext CreateContext()
			{
				CreateContextCalled = true;
				CustomContext = base.CreateContext();
				return CustomContext;
			}

			protected override StateMachineInstance CreateInstance(StateMachineGraph graph, IStateMachineContext context)
			{
				CreateInstanceCalled = true;
				return base.CreateInstance(graph, context);
			}
		}

		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			_testGameObject = new GameObject("TestRunnerObject");
			TestStateA.ResetCounters();
			TestStateB.ResetCounters();

			StateMachineControllerCallbacks.Reset();
		}

		[TearDown]
		public void TearDown()
		{
			if (_runner != null)
			{
				_runner.StopStateMachine();
			}

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

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "stateA",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "stateA",
				SourcePortName = "toB",
				TargetNodeId = "stateB",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

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

		private StateMachineRunner CreateRunner(bool autoStart = false)
		{
			_runner = _testGameObject.AddComponent<StateMachineRunner>();
			_runner.Graph = _graph;
			_runner.AutoStart = autoStart;
			return _runner;
		}

		private void InitializeRunnerManually()
		{
			_runner.Initialize(_graph);
		}


		[Test]
		public void Graph_SetAndGet_Works()
		{
			SetupSimpleGraph();
			_runner = _testGameObject.AddComponent<StateMachineRunner>();

			_runner.Graph = _graph;

			Assert.AreSame(_graph, _runner.Graph);
		}

		[Test]
		public void AutoStart_DefaultIsTrue()
		{
			_runner = _testGameObject.AddComponent<StateMachineRunner>();

			Assert.IsTrue(_runner.AutoStart);
		}

		[Test]
		public void AutoStart_CanBeSet()
		{
			_runner = _testGameObject.AddComponent<StateMachineRunner>();

			_runner.AutoStart = false;

			Assert.IsFalse(_runner.AutoStart);
		}

		[Test]
		public void UseAsyncMethods_DefaultIsFalse()
		{
			_runner = _testGameObject.AddComponent<StateMachineRunner>();

			Assert.IsFalse(_runner.UseAsyncMethods);
		}

		[Test]
		public void UseAsyncMethods_CanBeSet()
		{
			_runner = _testGameObject.AddComponent<StateMachineRunner>();

			_runner.UseAsyncMethods = true;

			Assert.IsTrue(_runner.UseAsyncMethods);
		}

		[Test]
		public void UpdateMode_DefaultIsUpdate()
		{
			_runner = _testGameObject.AddComponent<StateMachineRunner>();

			Assert.AreEqual(UpdateMode.Update, _runner.UpdateMode);
		}

		[Test]
		public void UpdateMode_CanBeSet()
		{
			_runner = _testGameObject.AddComponent<StateMachineRunner>();

			_runner.UpdateMode = UpdateMode.FixedUpdate;

			Assert.AreEqual(UpdateMode.FixedUpdate, _runner.UpdateMode);
		}

		[Test]
		public void IsRunning_BeforeInitialize_ReturnsFalse()
		{
			SetupSimpleGraph();
			CreateRunner(false);

			Assert.IsFalse(_runner.IsRunning);
		}

		[Test]
		public void IsRunning_AfterStart_ReturnsTrue()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();

			_runner.StartStateMachine();

			Assert.IsTrue(_runner.IsRunning);
		}

		[Test]
		public void CurrentState_BeforeStart_ReturnsNull()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();

			Assert.IsNull(_runner.CurrentState);
		}

		[Test]
		public void CurrentState_AfterStart_ReturnsActiveState()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();

			_runner.StartStateMachine();

			Assert.IsNotNull(_runner.CurrentState);
			Assert.IsInstanceOf<TestStateA>(_runner.CurrentState);
		}

		[Test]
		public void Blackboard_AfterInitialize_IsNotNull()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();

			Assert.IsNotNull(_runner.Blackboard);
		}

		[Test]
		public void Instance_AfterInitialize_IsNotNull()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();

			Assert.IsNotNull(_runner.Instance);
		}

		[Test]
		public void Context_AfterInitialize_IsNotNull()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();

			Assert.IsNotNull(_runner.Context);
		}

		[Test]
		public void VariableOverrides_IsNotNull()
		{
			_runner = _testGameObject.AddComponent<StateMachineRunner>();

			Assert.IsNotNull(_runner.VariableOverrides);
		}



		[Test]
		public void Initialize_WithGraph_CreatesInstance()
		{
			SetupSimpleGraph();
			CreateRunner(false);

			_runner.Initialize(_graph);

			Assert.IsNotNull(_runner.Instance);
		}

		[Test]
		public void Initialize_WithNullGraph_SetsInstanceToNull()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();

			_runner.Initialize(null);

			Assert.IsNull(_runner.Instance);
		}

		[Test]
		public void Initialize_WithContext_UsesProvidedContext()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			var customContext = new StateMachineContext(_testGameObject);

			_runner.Initialize(_graph, customContext);

			Assert.AreSame(customContext, _runner.Context);
		}

		[Test]
		public void Initialize_CalledTwice_StopsPreviousInstance()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();
			_runner.StartStateMachine();
			Assert.AreEqual(1, TestStateA.EnterCount);

			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			Assert.AreEqual(1, TestStateA.ExitCount);
			Assert.AreEqual(2, TestStateA.EnterCount);
		}



		[Test]
		public void Reinitialize_RecreatesInstance()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();
			var firstInstance = _runner.Instance;

			_runner.Reinitialize();

			Assert.IsNotNull(_runner.Instance);
			Assert.AreNotSame(firstInstance, _runner.Instance);
		}

		[Test]
		public void Reinitialize_WithNullGraph_SetsInstanceToNull()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();

			_runner.Graph = null;
			_runner.Reinitialize();

			Assert.IsNull(_runner.Instance);
		}

		[Test]
		public void Reinitialize_StopsPreviousInstance()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();
			_runner.StartStateMachine();

			_runner.Reinitialize();

			Assert.AreEqual(1, TestStateA.ExitCount);
		}



		[Test]
		public void StartStateMachine_EntersFirstState()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();

			_runner.StartStateMachine();

			Assert.AreEqual(1, TestStateA.EnterCount);
		}

		[Test]
		public void StartStateMachine_WithNullInstance_IsNoOp()
		{
			CreateRunner(false);

			_runner.StartStateMachine();

			Assert.IsNull(_runner.Instance);
			Assert.IsFalse(_runner.IsRunning);
		}

		[Test]
		public void StopStateMachine_ExitsCurrentState()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();
			_runner.StartStateMachine();

			_runner.StopStateMachine();

			Assert.AreEqual(1, TestStateA.ExitCount);
		}

		[Test]
		public void StopStateMachine_WithNullInstance_IsNoOp()
		{
			CreateRunner(false);

			_runner.StopStateMachine();

			Assert.IsNull(_runner.Instance);
			Assert.IsFalse(_runner.IsRunning);
		}

		[Test]
		public void RestartStateMachine_StopsAndStarts()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();
			_runner.StartStateMachine();

			_runner.RestartStateMachine();

			Assert.AreEqual(1, TestStateA.ExitCount);
			Assert.AreEqual(2, TestStateA.EnterCount);
		}



		[Test]
		public void PauseStateMachine_PausesInstance()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();
			_runner.StartStateMachine();

			_runner.PauseStateMachine();

			Assert.IsTrue(_runner.Instance.IsPaused);
		}

		[Test]
		public void PauseStateMachine_WithNullInstance_IsNoOp()
		{
			CreateRunner(false);

			_runner.PauseStateMachine();

			Assert.IsNull(_runner.Instance);
		}

		[Test]
		public void ResumeStateMachine_ResumesInstance()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();
			_runner.StartStateMachine();
			_runner.PauseStateMachine();

			_runner.ResumeStateMachine();

			Assert.IsFalse(_runner.Instance.IsPaused);
		}

		[Test]
		public void ResumeStateMachine_WithNullInstance_IsNoOp()
		{
			CreateRunner(false);

			_runner.ResumeStateMachine();

			Assert.IsNull(_runner.Instance);
		}



		[Test]
		public void SetBlackboardValue_SetsValue()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();

			_runner.SetBlackboardValue("testKey", 42);

			Assert.AreEqual(42, _runner.Blackboard.Get<int>("testKey"));
		}

		[Test]
		public void SetBlackboardValue_WithNullInstance_IsNoOp()
		{
			CreateRunner(false);

			_runner.SetBlackboardValue("key", 1);

			Assert.IsNull(_runner.Instance);
			Assert.IsNull(_runner.Blackboard);
		}

		[Test]
		public void GetBlackboardValue_RetrievesValue()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();
			_runner.Blackboard.Set("testKey", "testValue");

			var value = _runner.GetBlackboardValue<string>("testKey");

			Assert.AreEqual("testValue", value);
		}

		[Test]
		public void GetBlackboardValue_KeyNotFound_ReturnsDefault()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();

			var value = _runner.GetBlackboardValue("nonExistent", 99);

			Assert.AreEqual(99, value);
		}

		[Test]
		public void GetBlackboardValue_WithNullInstance_ReturnsDefault()
		{
			CreateRunner(false);

			var value = _runner.GetBlackboardValue("key", 42);

			Assert.AreEqual(42, value);
		}



		[Test]
		public void UpdateMode_Update_UpdatesOnUpdate()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			_runner.UpdateMode = UpdateMode.Update;
			InitializeRunnerManually();
			_runner.StartStateMachine();

			// Simulate Update call
			_runner.SendMessage("Update");

			Assert.AreEqual(1, TestStateA.UpdateCount);
		}

		[Test]
		public void UpdateMode_FixedUpdate_UpdatesOnFixedUpdate()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			_runner.UpdateMode = UpdateMode.FixedUpdate;
			InitializeRunnerManually();
			_runner.StartStateMachine();

			// Simulate FixedUpdate call
			_runner.SendMessage("FixedUpdate");

			Assert.AreEqual(1, TestStateA.UpdateCount);
		}

		[Test]
		public void UpdateMode_LateUpdate_UpdatesOnLateUpdate()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			_runner.UpdateMode = UpdateMode.LateUpdate;
			InitializeRunnerManually();
			_runner.StartStateMachine();

			// Simulate LateUpdate call
			_runner.SendMessage("LateUpdate");

			Assert.AreEqual(1, TestStateA.UpdateCount);
		}

		[Test]
		public void UpdateMode_Manual_DoesNotAutoUpdate()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			_runner.UpdateMode = UpdateMode.Manual;
			InitializeRunnerManually();
			_runner.StartStateMachine();

			// Simulate all update calls
			_runner.SendMessage("Update");
			_runner.SendMessage("FixedUpdate");
			_runner.SendMessage("LateUpdate");

			Assert.AreEqual(0, TestStateA.UpdateCount);
		}

		[Test]
		public void FixedUpdate_AlwaysCallsStateFixedUpdate()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			_runner.UpdateMode = UpdateMode.Manual;
			InitializeRunnerManually();
			_runner.StartStateMachine();

			_runner.SendMessage("FixedUpdate");

			Assert.AreEqual(1, TestStateA.FixedUpdateCount);
		}

		[Test]
		public void LateUpdate_AlwaysCallsStateLateUpdate()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			_runner.UpdateMode = UpdateMode.Manual;
			InitializeRunnerManually();
			_runner.StartStateMachine();

			_runner.SendMessage("LateUpdate");

			Assert.AreEqual(1, TestStateA.LateUpdateCount);
		}



		[Test]
		public void ApplyVariableOverrides_AppliesOverridesToBlackboard()
		{
			SetupSimpleGraph();
			CreateRunner(false);

			var overrideValue = new BlackboardOverride
			{
				Key = "testVar",
				ValueType = typeof(int),
				Value = 123
			};
			_runner.VariableOverrides.Add(overrideValue);

			InitializeRunnerManually();

			Assert.AreEqual(123, _runner.Blackboard.Get<int>("testVar"));
		}

		[Test]
		public void ApplyVariableOverrides_SkipsEmptyKeys()
		{
			SetupSimpleGraph();
			CreateRunner(false);

			var overrideValue = new BlackboardOverride
			{
				Key = "",
				ValueType = typeof(int),
				Value = 123
			};
			_runner.VariableOverrides.Add(overrideValue);

			InitializeRunnerManually();

			Assert.IsNotNull(_runner.Instance);
			Assert.IsFalse(_runner.Blackboard.HasVariable(""));
		}

		[Test]
		public void ApplyVariableOverrides_SkipsNullValues()
		{
			SetupSimpleGraph();
			CreateRunner(false);

			var overrideValue = new BlackboardOverride
			{
				Key = "testVar",
				ValueType = typeof(string)
			};
			// Value is null by default
			_runner.VariableOverrides.Add(overrideValue);

			InitializeRunnerManually();

			Assert.IsNotNull(_runner.Instance);
		}



		[Test]
		public void CreateContext_CanBeOverridden()
		{
			SetupSimpleGraph();
			var customRunner = _testGameObject.AddComponent<CustomRunner>();
			customRunner.Graph = _graph;
			customRunner.AutoStart = false;
			_runner = customRunner;

			customRunner.Initialize(_graph);

			Assert.IsTrue(customRunner.CreateContextCalled);
			Assert.IsNotNull(customRunner.CustomContext);
		}

		[Test]
		public void CreateInstance_CanBeOverridden()
		{
			SetupSimpleGraph();
			var customRunner = _testGameObject.AddComponent<CustomRunner>();
			customRunner.Graph = _graph;
			customRunner.AutoStart = false;
			_runner = customRunner;

			customRunner.Initialize(_graph);

			Assert.IsTrue(customRunner.CreateInstanceCalled);
		}



		[Test]
		public void OnStateChanged_FiresOnTransition()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();
			State previousState = null;
			State newState = null;
			_runner.OnStateChanged += (prev, next) =>
			{
				previousState = prev;
				newState = next;
			};

			_runner.StartStateMachine();

			Assert.IsNull(previousState);
			Assert.IsInstanceOf<TestStateA>(newState);
		}



		[Test]
		public void OnControllerCreated_NotCalledDuringInitialize()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			var callbackCalled = false;
			StateMachineControllerCallbacks.OnControllerCreated += (instance, graph, owner) =>
			{
				callbackCalled = true;
			};

			InitializeRunnerManually();

			// OnControllerCreated is called in Awake, not Initialize
			Assert.IsFalse(callbackCalled);
		}



		[Test]
		public void StartStateMachine_CalledTwice_DoesNotReenterState()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();

			_runner.StartStateMachine();
			_runner.StartStateMachine();

			Assert.AreEqual(1, TestStateA.EnterCount);
		}

		[Test]
		public void StopStateMachine_WhenNotRunning_IsNoOp()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();

			_runner.StopStateMachine();

			Assert.IsFalse(_runner.IsRunning);
			Assert.IsNull(_runner.CurrentState);
		}

		[Test]
		public void PauseStateMachine_WhenAlreadyPaused_RemainsIdempotent()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();
			_runner.StartStateMachine();
			_runner.PauseStateMachine();

			_runner.PauseStateMachine();

			Assert.IsTrue(_runner.Instance.IsPaused);
		}

		[Test]
		public void ResumeStateMachine_WhenNotPaused_RemainsRunning()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();
			_runner.StartStateMachine();

			_runner.ResumeStateMachine();

			Assert.IsFalse(_runner.Instance.IsPaused);
			Assert.IsTrue(_runner.IsRunning);
		}



		[Test]
		public void UpdateMode_HasExpectedValues()
		{
			Assert.AreEqual(0, (int)UpdateMode.Update);
			Assert.AreEqual(1, (int)UpdateMode.FixedUpdate);
			Assert.AreEqual(2, (int)UpdateMode.LateUpdate);
			Assert.AreEqual(3, (int)UpdateMode.Manual);
		}



		private class AsyncTestState : State
		{
			public static int EnterAsyncCount;
			public static int UpdateAsyncCount;
			public static int ExitAsyncCount;

			[Transition("Done")] public Action done;

			public override Task OnEnterAsync(CancellationToken ct)
			{
				EnterAsyncCount++;
				return Task.CompletedTask;
			}

			public override Task OnUpdateAsync(CancellationToken ct)
			{
				UpdateAsyncCount++;
				return Task.CompletedTask;
			}

			public override Task OnExitAsync(CancellationToken ct)
			{
				ExitAsyncCount++;
				return Task.CompletedTask;
			}

			public static void ResetCounters()
			{
				EnterAsyncCount = 0;
				UpdateAsyncCount = 0;
				ExitAsyncCount = 0;
			}
		}

		private class ThrowingAsyncState : State
		{
			[Transition("Done")] public Action done;

			public override Task OnEnterAsync(CancellationToken ct)
			{
				throw new InvalidOperationException("Test exception in async enter");
			}
		}

		private void SetupAsyncGraph<TState>() where TState : State
		{
			var entry = new EntryNode { Id = "entry" };
			var stateNode = new StateNode
			{
				Id = "asyncState",
				DisplayName = "Async State",
				StateTypeAssemblyQualifiedName = typeof(TState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateNode);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "asyncState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});
		}

		[Test]
		public void AsyncMode_StartStateMachine_BeginsRunning()
		{
			SetupAsyncGraph<AsyncTestState>();
			AsyncTestState.ResetCounters();
			CreateRunner(false);
			_runner.UseAsyncMethods = true;
			InitializeRunnerManually();

			_runner.StartStateMachine();

			Assert.IsTrue(_runner.IsRunning);
			Assert.IsNotNull(_runner.CurrentState);
		}

		[Test]
		public void AsyncMode_StopStateMachine_StopsRunning()
		{
			SetupAsyncGraph<AsyncTestState>();
			AsyncTestState.ResetCounters();
			CreateRunner(false);
			_runner.UseAsyncMethods = true;
			InitializeRunnerManually();
			_runner.StartStateMachine();

			_runner.StopStateMachine();

			Assert.IsFalse(_runner.IsRunning);
		}

		[Test]
		public void AsyncMode_UpdateStateMachine_RemainsRunning()
		{
			SetupAsyncGraph<AsyncTestState>();
			AsyncTestState.ResetCounters();
			CreateRunner(false);
			_runner.UseAsyncMethods = true;
			_runner.UpdateMode = UpdateMode.Update;
			InitializeRunnerManually();
			_runner.StartStateMachine();

			_runner.SendMessage("Update");

			Assert.IsTrue(_runner.IsRunning);
		}

		[Test]
		public void AsyncMode_StartStateMachine_EntersState()
		{
			SetupAsyncGraph<AsyncTestState>();
			AsyncTestState.ResetCounters();
			CreateRunner(false);
			_runner.UseAsyncMethods = true;
			InitializeRunnerManually();

			_runner.StartStateMachine();

			Assert.IsTrue(_runner.IsRunning);
			Assert.AreEqual(1, AsyncTestState.EnterAsyncCount);
		}

		[Test]
		public void AsyncMode_ThrowingState_RunnerSurvivesException()
		{
			SetupAsyncGraph<ThrowingAsyncState>();
			CreateRunner(false);
			_runner.UseAsyncMethods = true;
			InitializeRunnerManually();

			LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Exception in OnEnterAsync"));

			_runner.StartStateMachine();

			Assert.IsNotNull(_runner.Instance);
		}

		[Test]
		public void AsyncMode_RestartStateMachine_RemainsRunning()
		{
			SetupAsyncGraph<AsyncTestState>();
			AsyncTestState.ResetCounters();
			CreateRunner(false);
			_runner.UseAsyncMethods = true;
			InitializeRunnerManually();
			_runner.StartStateMachine();

			_runner.RestartStateMachine();

			Assert.IsTrue(_runner.IsRunning);
			Assert.IsNotNull(_runner.CurrentState);
		}



		private class TestContextComponent : MonoBehaviour
		{
			public int Value { get; set; }
		}

		private class ContextRequiringState : State<TestContextComponent>
		{
			public static bool EnteredWithContext;
			public static TestContextComponent ReceivedContext;

			[Transition("Done")] public Action done;

			public override void OnEnter()
			{
				EnteredWithContext = HasValidContext;
				ReceivedContext = SharedContext;
			}

			public static void ResetCounters()
			{
				EnteredWithContext = false;
				ReceivedContext = null;
			}
		}

		private void SetupContextGraph()
		{
			var entry = new EntryNode { Id = "entry" };
			var stateNode = new StateNode
			{
				Id = "contextState",
				DisplayName = "Context State",
				StateTypeAssemblyQualifiedName = typeof(ContextRequiringState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateNode);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "contextState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});
		}

		[Test]
		public void AutoResolveSharedContext_FindsSiblingComponent()
		{
			SetupContextGraph();
			ContextRequiringState.ResetCounters();
			var contextComponent = _testGameObject.AddComponent<TestContextComponent>();
			contextComponent.Value = 42;
			CreateRunner(false);

			InitializeRunnerManually();
			_runner.StartStateMachine();

			Assert.IsTrue(ContextRequiringState.EnteredWithContext);
			Assert.AreSame(contextComponent, ContextRequiringState.ReceivedContext);
		}

		[Test]
		public void AutoResolveSharedContext_NoMatchingComponent_InitializesWithoutContext()
		{
			SetupContextGraph();
			ContextRequiringState.ResetCounters();
			CreateRunner(false);

			InitializeRunnerManually();

			Assert.IsNotNull(_runner.Instance);
			Assert.IsFalse(_runner.Instance.HasSharedContext);
		}

		[Test]
		public void AutoResolveSharedContext_SkipsWhenNoContextRequired()
		{
			SetupSimpleGraph();
			CreateRunner(false);

			InitializeRunnerManually();

			Assert.IsNotNull(_runner.Instance);
			Assert.IsFalse(_runner.Instance.HasSharedContext);
		}

		[Test]
		public void SharedContextObject_SerializedField_UsedAtRuntime()
		{
			SetupContextGraph();
			ContextRequiringState.ResetCounters();
			var contextComponent = _testGameObject.AddComponent<TestContextComponent>();
			contextComponent.Value = 99;
			CreateRunner(false);

			// Explicitly set the serialized field (as the editor would)
			_runner.SharedContextObject = contextComponent;

			InitializeRunnerManually();
			_runner.StartStateMachine();

			Assert.IsTrue(ContextRequiringState.EnteredWithContext);
			Assert.AreSame(contextComponent, ContextRequiringState.ReceivedContext);
		}

		[Test]
		public void SharedContextObject_PrefersSerializedOverSibling()
		{
			SetupContextGraph();
			ContextRequiringState.ResetCounters();

			// Add two context components
			var siblingContext = _testGameObject.AddComponent<TestContextComponent>();
			siblingContext.Value = 1;

			// Create a second GameObject with a context component for explicit assignment
			var otherGo = new GameObject("OtherContext");
			var explicitContext = otherGo.AddComponent<TestContextComponent>();
			explicitContext.Value = 2;

			CreateRunner(false);
			_runner.SharedContextObject = explicitContext;

			InitializeRunnerManually();
			_runner.StartStateMachine();

			Assert.AreSame(explicitContext, ContextRequiringState.ReceivedContext);

			UnityEngine.Object.DestroyImmediate(otherGo);
		}

		private class SlowAsyncTestState : State
		{
			public static int UpdateAsyncCallCount;
			public static TaskCompletionSource<bool> UpdateTcs;

			[Transition("Done")] public Action done;

			public override Task OnEnterAsync(CancellationToken ct)
			{
				return Task.CompletedTask;
			}

			public override Task OnUpdateAsync(CancellationToken ct)
			{
				UpdateAsyncCallCount++;
				return UpdateTcs?.Task ?? Task.CompletedTask;
			}

			public static void ResetCounters()
			{
				UpdateAsyncCallCount = 0;
				UpdateTcs = new TaskCompletionSource<bool>();
			}
		}

		[Test]
		public void AsyncMode_PendingUpdate_SkipsSubsequentUpdates()
		{
			SetupAsyncGraph<SlowAsyncTestState>();
			SlowAsyncTestState.ResetCounters();
			CreateRunner(false);
			_runner.UseAsyncMethods = true;
			_runner.UpdateMode = UpdateMode.Update;
			InitializeRunnerManually();
			_runner.StartStateMachine();

			// First Update triggers UpdateAsync
			_runner.SendMessage("Update");
			Assert.AreEqual(1, SlowAsyncTestState.UpdateAsyncCallCount);

			// Subsequent Updates while task is pending should be skipped
			_runner.SendMessage("Update");
			_runner.SendMessage("Update");
			Assert.AreEqual(1, SlowAsyncTestState.UpdateAsyncCallCount);
		}

		[Test]
		public void AsyncMode_CompletedUpdate_AllowsNextUpdate()
		{
			SetupAsyncGraph<SlowAsyncTestState>();
			SlowAsyncTestState.ResetCounters();
			CreateRunner(false);
			_runner.UseAsyncMethods = true;
			_runner.UpdateMode = UpdateMode.Update;
			InitializeRunnerManually();
			_runner.StartStateMachine();

			// First Update triggers UpdateAsync
			_runner.SendMessage("Update");
			Assert.AreEqual(1, SlowAsyncTestState.UpdateAsyncCallCount);

			// Complete the pending task
			SlowAsyncTestState.UpdateTcs.SetResult(true);

			// Next Update should trigger a new UpdateAsync
			_runner.SendMessage("Update");
			Assert.AreEqual(2, SlowAsyncTestState.UpdateAsyncCallCount);
		}

		[Test]
		public void FixedUpdate_WhenUpdateModeIsUpdate_StillForwardsToState()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();
			_runner.StartStateMachine();
			TestStateA.FixedUpdateCount = 0;

			_runner.SendMessage("FixedUpdate");

			Assert.AreEqual(1, TestStateA.FixedUpdateCount);
		}

		[Test]
		public void LateUpdate_WhenUpdateModeIsUpdate_StillForwardsToState()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			InitializeRunnerManually();
			_runner.StartStateMachine();
			TestStateA.LateUpdateCount = 0;

			_runner.SendMessage("LateUpdate");

			Assert.AreEqual(1, TestStateA.LateUpdateCount);
		}

		[Test]
		public void FixedUpdate_WhenUpdateModeIsFixedUpdate_UpdatesAndForwards()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			_runner.UpdateMode = UpdateMode.FixedUpdate;
			InitializeRunnerManually();
			_runner.StartStateMachine();
			TestStateA.UpdateCount = 0;
			TestStateA.FixedUpdateCount = 0;

			_runner.SendMessage("FixedUpdate");

			Assert.AreEqual(1, TestStateA.UpdateCount, "State machine update should fire on FixedUpdate");
			Assert.AreEqual(1, TestStateA.FixedUpdateCount, "FixedUpdate should still forward to state");
		}

		[Test]
		public void Update_WhenUpdateModeIsFixedUpdate_DoesNotCallStateUpdate()
		{
			SetupSimpleGraph();
			CreateRunner(false);
			_runner.UpdateMode = UpdateMode.FixedUpdate;
			InitializeRunnerManually();
			_runner.StartStateMachine();
			TestStateA.UpdateCount = 0;

			_runner.SendMessage("Update");

			Assert.AreEqual(0, TestStateA.UpdateCount, "State machine update should not fire on Update when mode is FixedUpdate");
		}

	}
}
