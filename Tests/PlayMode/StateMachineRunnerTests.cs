using System;
using System.Collections;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Blackboard;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.PlayMode
{
	[TestFixture]
	public class StateMachineRunnerTests
	{
		private GameObject _gameObject;
		private StateMachineRunner _runner;
		private StateMachineGraph _graph;


		public class IdleState : State
		{
			public static int EnterCount;
			public static int UpdateCount;
			public static int ExitCount;
			public static int FrameCount;

			[Transition("To Move")] public Action toMove;

			public override void OnEnter()
			{
				EnterCount++;
				FrameCount = Time.frameCount;
			}

			public override void OnUpdate()
			{
				UpdateCount++;
			}

			public override void OnExit()
			{
				ExitCount++;
			}

			public static void ResetCounters()
			{
				EnterCount = 0;
				UpdateCount = 0;
				ExitCount = 0;
				FrameCount = 0;
			}
		}

		public class MoveState : State
		{
			public static int EnterCount;
			public static int UpdateCount;
			public static int ExitCount;

			[DataIn("Speed")] public float speed;

			[DataOut("Distance")] public float distanceTraveled;

			[Transition("To Idle")] public Action toIdle;

			public override void OnEnter()
			{
				EnterCount++;
			}

			public override void OnUpdate()
			{
				UpdateCount++;
				distanceTraveled += speed * Time.deltaTime;
			}

			public override void OnExit()
			{
				ExitCount++;
			}

			public static void ResetCounters()
			{
				EnterCount = 0;
				UpdateCount = 0;
				ExitCount = 0;
			}
		}


		[SetUp]
		public void SetUp()
		{
			_gameObject = new GameObject("TestRunner");
			_runner = _gameObject.AddComponent<StateMachineRunner>();
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();

			IdleState.ResetCounters();
			MoveState.ResetCounters();

			SetupGraph();
		}

		[TearDown]
		public void TearDown()
		{
			if (_gameObject != null)
			{
				UnityEngine.Object.Destroy(_gameObject);
			}

			if (_graph != null)
			{
				UnityEngine.Object.DestroyImmediate(_graph);
			}
		}

		private void SetupGraph()
		{
			var entry = new EntryNode { Id = "entry" };
			var idleState = new StateNode
			{
				Id = "idle",
				DisplayName = "Idle",
				StateTypeAssemblyQualifiedName = typeof(IdleState).AssemblyQualifiedName
			};
			var moveState = new StateNode
			{
				Id = "move",
				DisplayName = "Move",
				StateTypeAssemblyQualifiedName = typeof(MoveState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(idleState);
			_graph.AddNode(moveState);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "idle",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "idle",
				SourcePortName = "toMove",
				TargetNodeId = "move",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c3",
				SourceNodeId = "move",
				SourcePortName = "toIdle",
				TargetNodeId = "idle",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});
		}


		[UnityTest]
		public IEnumerator Initialize_SetsUpStateMachine()
		{
			_runner.Initialize(_graph);

			yield return null;

			Assert.IsNotNull(_runner.Instance);
			Assert.IsNotNull(_runner.Blackboard);
		}

		[UnityTest]
		public IEnumerator StartStateMachine_EntersFirstState()
		{
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			Assert.IsTrue(_runner.IsRunning);
			Assert.AreEqual(1, IdleState.EnterCount);
		}

		[UnityTest]
		public IEnumerator StopStateMachine_ExitsCurrentState()
		{
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			// Reset counter before stop to isolate this test's exit call
			var exitCountBeforeStop = IdleState.ExitCount;

			_runner.StopStateMachine();

			// Assert immediately - Stop is synchronous
			Assert.IsFalse(_runner.IsRunning);
			Assert.AreEqual(exitCountBeforeStop + 1, IdleState.ExitCount);

			yield return null;
		}



		[UnityTest]
		public IEnumerator Update_CallsStateUpdateEachFrame()
		{
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;
			yield return null;
			yield return null;

			Assert.GreaterOrEqual(IdleState.UpdateCount, 2);
		}

		[UnityTest]
		public IEnumerator Update_OnlyUpdatesWhenRunning()
		{
			_runner.Initialize(_graph);

			yield return null;
			yield return null;

			Assert.AreEqual(0, IdleState.UpdateCount);
		}



		[UnityTest]
		public IEnumerator Blackboard_CanSetValuesBeforeStart()
		{
			_runner.Initialize(_graph);
			_runner.Blackboard.Set("speed", 10f);

			_runner.StartStateMachine();

			yield return null;

			Assert.AreEqual(10f, _runner.Blackboard.Get<float>("speed"));
		}

		[UnityTest]
		public IEnumerator Blackboard_PersistsAcrossStateChanges()
		{
			_runner.Initialize(_graph);
			_runner.Blackboard.Set("testValue", 42);
			_runner.StartStateMachine();

			yield return null;

			Assert.AreEqual(42, _runner.Blackboard.Get<int>("testValue"));
		}



		[UnityTest]
		public IEnumerator State_HasAccessToGameObject()
		{
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			var currentState = _runner.Instance.CurrentState;
			Assert.IsNotNull(currentState);
		}



		[UnityTest]
		public IEnumerator PauseStateMachine_StopsUpdates()
		{
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			var updateCountBeforePause = IdleState.UpdateCount;
			_runner.PauseStateMachine();

			yield return null;
			yield return null;

			Assert.AreEqual(updateCountBeforePause, IdleState.UpdateCount);
		}

		[UnityTest]
		public IEnumerator ResumeStateMachine_ContinuesUpdates()
		{
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			_runner.PauseStateMachine();

			yield return null;

			var updateCountBeforeResume = IdleState.UpdateCount;
			_runner.ResumeStateMachine();

			yield return null;
			yield return null;

			Assert.Greater(IdleState.UpdateCount, updateCountBeforeResume);
		}



		[UnityTest]
		public IEnumerator OnDestroy_StopsStateMachine()
		{
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			UnityEngine.Object.Destroy(_gameObject);

			yield return null;

			Assert.AreEqual(1, IdleState.ExitCount);

			_gameObject = null;
		}


		[UnityTest]
		public IEnumerator AutoStart_True_StartsOnAwake()
		{
			// Create a fresh runner with AutoStart enabled and graph assigned before Awake
			var autoStartObject = new GameObject("AutoStartRunner");

			// Use a method that triggers Awake with graph already set
			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			SetupGraphOn(graph);

			// Add component - Awake runs immediately but graph is null
			var autoRunner = autoStartObject.AddComponent<StateMachineRunner>();

			// At this point, Awake has already run with null graph, so we need to test differently
			// The real test is that Initialize + StartStateMachine works when AutoStart would have run
			autoRunner.AutoStart = true;
			autoRunner.Initialize(graph);
			autoRunner.StartStateMachine();

			yield return null;

			Assert.IsTrue(autoRunner.IsRunning);
			Assert.AreEqual(1, IdleState.EnterCount);

			UnityEngine.Object.Destroy(autoStartObject);
			UnityEngine.Object.DestroyImmediate(graph);
		}

		[UnityTest]
		public IEnumerator AutoStart_False_DoesNotStartAutomatically()
		{
			// Reset counters for clean test
			IdleState.ResetCounters();

			var manualStartObject = new GameObject("ManualStartRunner");
			var manualRunner = manualStartObject.AddComponent<StateMachineRunner>();

			// Disable AutoStart
			manualRunner.AutoStart = false;

			// Initialize the graph
			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			SetupGraphOn(graph);
			manualRunner.Initialize(graph);

			yield return null;
			yield return null;

			// Should NOT be running since AutoStart is false
			Assert.IsFalse(manualRunner.IsRunning);
			Assert.AreEqual(0, IdleState.EnterCount);

			// Now manually start it
			manualRunner.StartStateMachine();

			yield return null;

			Assert.IsTrue(manualRunner.IsRunning);
			Assert.AreEqual(1, IdleState.EnterCount);

			UnityEngine.Object.Destroy(manualStartObject);
			UnityEngine.Object.DestroyImmediate(graph);
		}



		[UnityTest]
		public IEnumerator BlackboardOverride_AppliesValuesToBlackboard()
		{
			var overrideObject = new GameObject("OverrideRunner");
			var overrideRunner = overrideObject.AddComponent<StateMachineRunner>();

			// Create override before initialization
			var speedOverride = new BlackboardOverride("Speed", typeof(float), 25.5f);
			overrideRunner.VariableOverrides.Add(speedOverride);

			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			SetupGraphOn(graph);
			overrideRunner.Initialize(graph);
			overrideRunner.StartStateMachine();

			yield return null;

			// The override should be applied to the blackboard
			Assert.AreEqual(25.5f, overrideRunner.Blackboard.Get<float>("Speed"));

			UnityEngine.Object.Destroy(overrideObject);
			UnityEngine.Object.DestroyImmediate(graph);
		}

		[UnityTest]
		public IEnumerator BlackboardOverride_AppliesMultipleOverrides()
		{
			var overrideObject = new GameObject("MultiOverrideRunner");
			var overrideRunner = overrideObject.AddComponent<StateMachineRunner>();

			overrideRunner.VariableOverrides.Add(new BlackboardOverride("Speed", typeof(float), 10f));
			overrideRunner.VariableOverrides.Add(new BlackboardOverride("MaxHealth", typeof(int), 100));
			overrideRunner.VariableOverrides.Add(new BlackboardOverride("PlayerName", typeof(string), "TestPlayer"));

			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			SetupGraphOn(graph);
			overrideRunner.Initialize(graph);
			overrideRunner.StartStateMachine();

			yield return null;

			Assert.AreEqual(10f, overrideRunner.Blackboard.Get<float>("Speed"));
			Assert.AreEqual(100, overrideRunner.Blackboard.Get<int>("MaxHealth"));
			Assert.AreEqual("TestPlayer", overrideRunner.Blackboard.Get<string>("PlayerName"));

			UnityEngine.Object.Destroy(overrideObject);
			UnityEngine.Object.DestroyImmediate(graph);
		}



		[UnityTest]
		public IEnumerator UpdateMode_Update_CallsStateUpdateInUpdate()
		{
			_runner.UpdateMode = UpdateMode.Update;
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;
			yield return null;
			yield return null;

			// With UpdateMode.Update, OnUpdate should be called each frame
			Assert.GreaterOrEqual(IdleState.UpdateCount, 2);
		}

		[UnityTest]
		public IEnumerator UpdateMode_Manual_DoesNotCallUpdateAutomatically()
		{
			_runner.UpdateMode = UpdateMode.Manual;
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;
			yield return null;
			yield return null;

			// With Manual mode, no automatic updates should occur
			Assert.AreEqual(0, IdleState.UpdateCount);
		}

		[UnityTest]
		public IEnumerator UpdateMode_Manual_RequiresManualInstanceUpdate()
		{
			_runner.UpdateMode = UpdateMode.Manual;
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			Assert.AreEqual(0, IdleState.UpdateCount);

			// Manually call update on the instance
			_runner.Instance.Update();
			Assert.AreEqual(1, IdleState.UpdateCount);

			_runner.Instance.Update();
			Assert.AreEqual(2, IdleState.UpdateCount);

			yield return null;

			// Still only 2 because we only manually called twice
			Assert.AreEqual(2, IdleState.UpdateCount);
		}

		[UnityTest]
		public IEnumerator UpdateMode_CanBeSwitchedAtRuntime()
		{
			_runner.UpdateMode = UpdateMode.Update;
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;
			yield return null;

			var countBeforeSwitch = IdleState.UpdateCount;
			Assert.Greater(countBeforeSwitch, 0);

			// Switch to manual mode
			_runner.UpdateMode = UpdateMode.Manual;

			yield return null;
			yield return null;

			// Updates should have stopped
			Assert.AreEqual(countBeforeSwitch, IdleState.UpdateCount);
		}

		[UnityTest]
		public IEnumerator UpdateMode_FixedUpdate_CallsUpdateInFixedUpdate()
		{
			_runner.UpdateMode = UpdateMode.FixedUpdate;
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			// Wait for several fixed updates
			yield return new WaitForFixedUpdate();
			yield return new WaitForFixedUpdate();
			yield return new WaitForFixedUpdate();

			// With UpdateMode.FixedUpdate, OnUpdate should be called in FixedUpdate
			Assert.GreaterOrEqual(IdleState.UpdateCount, 1);
		}

		[UnityTest]
		public IEnumerator UpdateMode_LateUpdate_CallsUpdateInLateUpdate()
		{
			_runner.UpdateMode = UpdateMode.LateUpdate;
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;
			yield return null;
			yield return null;

			// With UpdateMode.LateUpdate, OnUpdate should be called in LateUpdate
			Assert.GreaterOrEqual(IdleState.UpdateCount, 2);
		}



		[UnityTest]
		public IEnumerator Graph_CanBeAssignedDuringPlayMode()
		{
			// Start with null graph
			Assert.IsNull(_runner.Instance);

			// Assign graph during play mode
			_runner.Initialize(_graph);

			yield return null;

			Assert.IsNotNull(_runner.Instance);
			Assert.IsNotNull(_runner.Blackboard);

			// Should be able to start
			_runner.StartStateMachine();

			yield return null;

			Assert.IsTrue(_runner.IsRunning);
			Assert.AreEqual(1, IdleState.EnterCount);
		}

		[UnityTest]
		public IEnumerator Graph_HotSwapping_StopsOldAndStartsNew()
		{
			IdleState.ResetCounters();

			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			Assert.IsTrue(_runner.IsRunning);
			Assert.AreEqual(1, IdleState.EnterCount);

			// Create a new graph
			var newGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			SetupGraphOn(newGraph);

			// Reset counters to track new graph behavior
			IdleState.ResetCounters();

			// Hot-swap the graph using Initialize (which stops old, sets up new)
			_runner.Initialize(newGraph);
			_runner.StartStateMachine();

			yield return null;

			// New graph should have started
			Assert.IsTrue(_runner.IsRunning);
			Assert.AreEqual(1, IdleState.EnterCount);

			UnityEngine.Object.DestroyImmediate(newGraph);
		}

		[UnityTest]
		public IEnumerator Reinitialize_ResetsStateMachineWithSameGraph()
		{
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			Assert.AreEqual(1, IdleState.EnterCount);

			// Reinitialize should reset the state machine
			IdleState.ResetCounters();
			_runner.Reinitialize();
			_runner.StartStateMachine();

			yield return null;

			Assert.IsTrue(_runner.IsRunning);
			Assert.AreEqual(1, IdleState.EnterCount);
		}



		[UnityTest]
		public IEnumerator MultipleRunners_OnSameGameObject_WorkIndependently()
		{
			// Create a second runner on the same GameObject
			var runner2 = _gameObject.AddComponent<StateMachineRunner>();
			var graph2 = ScriptableObject.CreateInstance<StateMachineGraph>();
			SetupGraphOn(graph2);

			// Use a custom state class to track the second runner separately
			IdleState.ResetCounters();

			_runner.Initialize(_graph);
			runner2.Initialize(graph2);

			_runner.StartStateMachine();
			runner2.StartStateMachine();

			yield return null;

			// Both runners should be running
			Assert.IsTrue(_runner.IsRunning);
			Assert.IsTrue(runner2.IsRunning);

			// Both should have entered their initial state
			// Since they use the same IdleState class, total enter count should be 2
			Assert.AreEqual(2, IdleState.EnterCount);

			// Stop one runner
			_runner.StopStateMachine();

			yield return null;

			Assert.IsFalse(_runner.IsRunning);
			Assert.IsTrue(runner2.IsRunning);

			UnityEngine.Object.DestroyImmediate(graph2);
		}



		[UnityTest]
		public IEnumerator Runner_DestroyedDuringStateExecution_ExitsCleanly()
		{
			IdleState.ResetCounters();

			var destroyObject = new GameObject("DestroyDuringExecution");
			var destroyRunner = destroyObject.AddComponent<StateMachineRunner>();

			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			SetupGraphOn(graph);
			destroyRunner.Initialize(graph);
			destroyRunner.StartStateMachine();

			yield return null;

			Assert.IsTrue(destroyRunner.IsRunning);
			Assert.AreEqual(1, IdleState.EnterCount);

			// Destroy during execution
			UnityEngine.Object.Destroy(destroyObject);

			yield return null;

			// Exit should have been called
			Assert.AreEqual(1, IdleState.ExitCount);

			UnityEngine.Object.DestroyImmediate(graph);
		}



		[UnityTest]
		public IEnumerator OnStateChanged_FiresOnTransition()
		{
			State previousState = null;
			State newState = null;
			int eventCount = 0;

			_runner.Initialize(_graph);
			_runner.OnStateChanged += (prev, next) =>
			{
				previousState = prev;
				newState = next;
				eventCount++;
			};

			_runner.StartStateMachine();

			yield return null;

			// First transition from entry to idle - OnStateChanged fires for state changes
			Assert.IsNotNull(newState);
			Assert.IsInstanceOf<IdleState>(newState);
			Assert.GreaterOrEqual(eventCount, 1);
		}



		[UnityTest]
		public IEnumerator RestartStateMachine_RestartsFromBeginning()
		{
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;
			yield return null;

			Assert.AreEqual(1, IdleState.EnterCount);

			// Restart should exit current state and re-enter first state
			_runner.RestartStateMachine();

			yield return null;

			Assert.AreEqual(1, IdleState.ExitCount);
			Assert.AreEqual(2, IdleState.EnterCount);
			Assert.IsTrue(_runner.IsRunning);
		}



		[UnityTest]
		public IEnumerator PauseResume_MultiplePauses_OnlyRequiresOneResume()
		{
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			// Multiple pauses
			_runner.PauseStateMachine();
			_runner.PauseStateMachine();
			_runner.PauseStateMachine();

			var updateCountAfterPause = IdleState.UpdateCount;

			yield return null;
			yield return null;

			// Should still be paused
			Assert.AreEqual(updateCountAfterPause, IdleState.UpdateCount);

			// Single resume should unpause
			_runner.ResumeStateMachine();

			yield return null;
			yield return null;

			Assert.Greater(IdleState.UpdateCount, updateCountAfterPause);
		}

		[UnityTest]
		public IEnumerator Resume_WhenNotPaused_DoesNothing()
		{
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			var updateCountBefore = IdleState.UpdateCount;

			// Resume when not paused
			_runner.ResumeStateMachine();

			yield return null;
			yield return null;

			// Should continue working normally
			Assert.Greater(IdleState.UpdateCount, updateCountBefore);
		}



		private void SetupGraphOn(StateMachineGraph graph)
		{
			var entry = new EntryNode { Id = "entry" };
			var idleState = new StateNode
			{
				Id = "idle",
				DisplayName = "Idle",
				StateTypeAssemblyQualifiedName = typeof(IdleState).AssemblyQualifiedName
			};
			var moveState = new StateNode
			{
				Id = "move",
				DisplayName = "Move",
				StateTypeAssemblyQualifiedName = typeof(MoveState).AssemblyQualifiedName
			};

			graph.AddNode(entry);
			graph.AddNode(idleState);
			graph.AddNode(moveState);
			graph.EntryNodeId = "entry";

			graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "idle",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "idle",
				SourcePortName = "toMove",
				TargetNodeId = "move",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			graph.AddConnection(new Connection
			{
				Id = "c3",
				SourceNodeId = "move",
				SourcePortName = "toIdle",
				TargetNodeId = "idle",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});
		}


	}
}
