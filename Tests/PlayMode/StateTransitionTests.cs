using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.PlayMode
{
	[TestFixture]
	public class StateTransitionTests
	{
		private GameObject _gameObject;
		private StateMachineRunner _runner;
		private StateMachineGraph _graph;


		public class StateA : State
		{
			public static bool Entered;
			public static bool Exited;

			[Transition("To B")] public Action toB;

			public override void OnEnter() => Entered = true;
			public override void OnExit() => Exited = true;

			public static void Reset()
			{
				Entered = false;
				Exited = false;
			}
		}

		public class StateB : State
		{
			public static bool Entered;
			public static bool Exited;

			[Transition("To A")] public Action toA;

			[Transition("To C")] public Action toC;

			public override void OnEnter() => Entered = true;
			public override void OnExit() => Exited = true;

			public static void Reset()
			{
				Entered = false;
				Exited = false;
			}
		}

		public class StateC : State
		{
			public static bool Entered;

			public override void OnEnter() => Entered = true;

			public static void Reset()
			{
				Entered = false;
			}
		}

		public class AsyncEnterState : State
		{
			public static bool EnterStarted;
			public static bool EnterCompleted;

			[Transition("Done")] public Action done;

			public override async Task OnEnterAsync()
			{
				EnterStarted = true;
				// Use Task.Yield for Unity-friendly async that completes on next frame
				await Task.Yield();
				EnterCompleted = true;
			}

			public static void Reset()
			{
				EnterStarted = false;
				EnterCompleted = false;
			}
		}

		public class AsyncExitState : State
		{
			public static bool ExitStarted;
			public static bool ExitCompleted;

			[Transition("Done")] public Action done;

			public override async Task OnExitAsync()
			{
				ExitStarted = true;
				await Task.Delay(100);
				ExitCompleted = true;
			}

			public static void Reset()
			{
				ExitStarted = false;
				ExitCompleted = false;
			}
		}

		public class DataProducerState : State
		{
			[DataOut("Result")] public int result;

			[Transition("Done")] public Action done;

			public override void OnEnter()
			{
				result = 42;
			}
		}

		public class DataConsumerState : State
		{
			[DataIn("Result")] public int inputResult;

			public static int ReceivedValue;

			public override void OnEnter()
			{
				ReceivedValue = inputResult;
			}

			public static void Reset()
			{
				ReceivedValue = 0;
			}
		}


		[SetUp]
		public void SetUp()
		{
			_gameObject = new GameObject("TestRunner");
			_runner = _gameObject.AddComponent<StateMachineRunner>();
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();

			StateA.Reset();
			StateB.Reset();
			StateC.Reset();
			AsyncEnterState.Reset();
			AsyncExitState.Reset();
			DataConsumerState.Reset();
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

		private void SetupLinearGraph()
		{
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName
			};
			var stateB = new StateNode
			{
				Id = "stateB",
				StateTypeAssemblyQualifiedName = typeof(StateB).AssemblyQualifiedName
			};
			var stateC = new StateNode
			{
				Id = "stateC",
				StateTypeAssemblyQualifiedName = typeof(StateC).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(stateB);
			_graph.AddNode(stateC);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "stateA",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "stateA",
				SourcePortName = "toB",
				TargetNodeId = "stateB",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c3",
				SourceNodeId = "stateB",
				SourcePortName = "toC",
				TargetNodeId = "stateC",
				ConnectionType = ConnectionType.Transition
			});
		}


		[UnityTest]
		public IEnumerator Transition_ExitsPreviousState()
		{
			SetupLinearGraph();
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			Assert.IsTrue(StateA.Entered);
			Assert.IsFalse(StateA.Exited);

			// Trigger transition to B
			var stateA = _runner.Instance.CurrentState as StateA;
			stateA?.toB?.Invoke();

			yield return null;

			Assert.IsTrue(StateA.Exited);
		}

		[UnityTest]
		public IEnumerator Transition_EntersNewState()
		{
			SetupLinearGraph();
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			var stateA = _runner.Instance.CurrentState as StateA;
			stateA?.toB?.Invoke();

			yield return null;

			Assert.IsTrue(StateB.Entered);
		}

		[UnityTest]
		public IEnumerator Transition_ChangesCurrentState()
		{
			SetupLinearGraph();
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			Assert.IsInstanceOf<StateA>(_runner.Instance.CurrentState);

			var stateA = _runner.Instance.CurrentState as StateA;
			stateA?.toB?.Invoke();

			yield return null;

			Assert.IsInstanceOf<StateB>(_runner.Instance.CurrentState);
		}

		[UnityTest]
		public IEnumerator MultipleTransitions_WorkCorrectly()
		{
			SetupLinearGraph();
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			// A -> B
			(_runner.Instance.CurrentState as StateA)?.toB?.Invoke();
			yield return null;

			// B -> C
			(_runner.Instance.CurrentState as StateB)?.toC?.Invoke();
			yield return null;

			Assert.IsTrue(StateC.Entered);
			Assert.IsInstanceOf<StateC>(_runner.Instance.CurrentState);
		}



		[UnityTest]
		public IEnumerator OnStateChanged_FiresOnTransition()
		{
			SetupLinearGraph();
			_runner.Initialize(_graph);

			State oldState = null;
			State newState = null;
			int changeCount = 0;

			_runner.Instance.OnStateChanged += (old, @new) =>
			{
				oldState = old;
				newState = @new;
				changeCount++;
			};

			_runner.StartStateMachine();

			yield return null;

			Assert.AreEqual(1, changeCount);
			Assert.IsNull(oldState);
			Assert.IsInstanceOf<StateA>(newState);

			(_runner.Instance.CurrentState as StateA)?.toB?.Invoke();

			yield return null;

			Assert.AreEqual(2, changeCount);
			Assert.IsInstanceOf<StateA>(oldState);
			Assert.IsInstanceOf<StateB>(newState);
		}



		[UnityTest]
		public IEnumerator AsyncEnter_WaitsForCompletion()
		{
			var entry = new EntryNode { Id = "entry" };
			var asyncState = new StateNode
			{
				Id = "async",
				StateTypeAssemblyQualifiedName = typeof(AsyncEnterState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(asyncState);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "async",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_graph);
			_runner.UseAsyncMethods = true;
			_runner.StartStateMachine();

			// Wait a frame for async to start
			yield return null;

			Assert.IsTrue(AsyncEnterState.EnterStarted);

			// Wait additional frames for async completion
			yield return null;
			yield return null;

			Assert.IsTrue(AsyncEnterState.EnterCompleted);
		}



		[UnityTest]
		public IEnumerator DataFlow_OutputToBlackboard()
		{
			var entry = new EntryNode { Id = "entry" };
			var producer = new StateNode
			{
				Id = "producer",
				StateTypeAssemblyQualifiedName = typeof(DataProducerState).AssemblyQualifiedName
			};
			var consumer = new StateNode
			{
				Id = "consumer",
				StateTypeAssemblyQualifiedName = typeof(DataConsumerState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(producer);
			_graph.AddNode(consumer);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "producer",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "producer",
				SourcePortName = "done",
				TargetNodeId = "consumer",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			// Trigger transition to consumer
			(_runner.Instance.CurrentState as DataProducerState)?.done?.Invoke();

			yield return null;

			Assert.AreEqual(42, DataConsumerState.ReceivedValue);
		}



		public class LoopingState : State
		{
			public static int EnterCount;

			[Transition("Loop")] public Action loop;

			public override void OnEnter()
			{
				EnterCount++;
				if (EnterCount < 200)
				{
					loop?.Invoke();
				}
			}

			public static void Reset() => EnterCount = 0;
		}

		[UnityTest]
		public IEnumerator SameFrameProtection_LimitsTransitions()
		{
			LoopingState.Reset();

			var entry = new EntryNode { Id = "entry" };
			var looping = new StateNode
			{
				Id = "looping",
				StateTypeAssemblyQualifiedName = typeof(LoopingState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(looping);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "looping",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "looping",
				SourcePortName = "loop",
				TargetNodeId = "looping",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_graph);
			_runner.Instance.MaxSameFrameTransitions = 50;

			LogAssert.Expect(LogType.Error, new Regex("Possible infinite loop detected"));

			_runner.StartStateMachine();

			yield return null;

			// Should be limited by MaxSameFrameTransitions
			Assert.LessOrEqual(LoopingState.EnterCount, 52);
			Assert.IsFalse(_runner.IsRunning);
		}

		[UnityTest]
		public IEnumerator MultiHopTransition_AllStatesEnteredAndExited()
		{
			SetupLinearGraph();
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			Assert.IsTrue(StateA.Entered);

			// A -> B
			(_runner.Instance.CurrentState as StateA)?.toB?.Invoke();
			yield return null;

			Assert.IsTrue(StateA.Exited, "StateA should have exited");
			Assert.IsTrue(StateB.Entered, "StateB should have entered");
			Assert.IsFalse(StateB.Exited, "StateB should not have exited yet");

			// B -> C
			(_runner.Instance.CurrentState as StateB)?.toC?.Invoke();
			yield return null;

			Assert.IsTrue(StateB.Exited, "StateB should have exited");
			Assert.IsTrue(StateC.Entered, "StateC should have entered");
			Assert.IsInstanceOf<StateC>(_runner.Instance.CurrentState);
		}

		[UnityTest]
		public IEnumerator BackAndForthTransition_WorksCorrectly()
		{
			// Setup graph: Entry -> A <-> B
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName
			};
			var stateB = new StateNode
			{
				Id = "stateB",
				StateTypeAssemblyQualifiedName = typeof(StateB).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(stateB);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "entry", TargetNodeId = "stateA", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "stateA", SourcePortName = "toB", TargetNodeId = "stateB", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c3", SourceNodeId = "stateB", SourcePortName = "toA", TargetNodeId = "stateA", ConnectionType = ConnectionType.Transition });

			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			Assert.IsInstanceOf<StateA>(_runner.Instance.CurrentState);

			// A -> B
			(_runner.Instance.CurrentState as StateA)?.toB?.Invoke();
			yield return null;
			Assert.IsInstanceOf<StateB>(_runner.Instance.CurrentState);

			// B -> A
			StateA.Reset();
			(_runner.Instance.CurrentState as StateB)?.toA?.Invoke();
			yield return null;
			Assert.IsInstanceOf<StateA>(_runner.Instance.CurrentState);
			Assert.IsTrue(StateA.Entered, "StateA should have been re-entered");
		}

		[UnityTest]
		public IEnumerator TransitionEvents_TrackFullChain()
		{
			SetupLinearGraph();
			_runner.Initialize(_graph);

			var transitions = new System.Collections.Generic.List<(string from, string to)>();
			_runner.Instance.OnTransitionTriggered += (from, to) => transitions.Add((from, to));

			_runner.StartStateMachine();
			yield return null;

			// A -> B
			(_runner.Instance.CurrentState as StateA)?.toB?.Invoke();
			yield return null;

			// B -> C
			(_runner.Instance.CurrentState as StateB)?.toC?.Invoke();
			yield return null;

			// Should have recorded at least transitions to stateA, stateB, and stateC
			Assert.GreaterOrEqual(transitions.Count, 2, "Should have recorded multiple transitions");

			// Verify the chain includes transitions to stateB and stateC
			Assert.IsTrue(transitions.Exists(t => t.to == "stateB"), "Should have transition to stateB");
			Assert.IsTrue(transitions.Exists(t => t.to == "stateC"), "Should have transition to stateC");
		}

		[UnityTest]
		public IEnumerator DataFlow_ThroughMultipleStates()
		{
			// Entry -> Producer -> Consumer (via transition)
			var entry = new EntryNode { Id = "entry" };
			var producer = new StateNode
			{
				Id = "producer",
				StateTypeAssemblyQualifiedName = typeof(DataProducerState).AssemblyQualifiedName
			};
			var consumer = new StateNode
			{
				Id = "consumer",
				StateTypeAssemblyQualifiedName = typeof(DataConsumerState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(producer);
			_graph.AddNode(consumer);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "entry", TargetNodeId = "producer", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "producer", SourcePortName = "done", TargetNodeId = "consumer", ConnectionType = ConnectionType.Transition });

			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			(_runner.Instance.CurrentState as DataProducerState)?.done?.Invoke();
			yield return null;

			// Consumer should receive the value produced by Producer via blackboard
			Assert.AreEqual(42, DataConsumerState.ReceivedValue, "Data should flow from producer to consumer");
			Assert.IsInstanceOf<DataConsumerState>(_runner.Instance.CurrentState);
		}

		[UnityTest]
		public IEnumerator AsyncExit_WaitsForCompletion()
		{
			var entry = new EntryNode { Id = "entry" };
			var asyncExit = new StateNode
			{
				Id = "asyncExit",
				StateTypeAssemblyQualifiedName = typeof(AsyncExitState).AssemblyQualifiedName
			};
			var stateC = new StateNode
			{
				Id = "stateC",
				StateTypeAssemblyQualifiedName = typeof(StateC).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(asyncExit);
			_graph.AddNode(stateC);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "entry", TargetNodeId = "asyncExit", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "asyncExit", SourcePortName = "done", TargetNodeId = "stateC", ConnectionType = ConnectionType.Transition });

			_runner.Initialize(_graph);
			_runner.UseAsyncMethods = true;
			_runner.StartStateMachine();

			yield return null;

			// Trigger transition which calls async OnExitAsync
			(_runner.Instance.CurrentState as AsyncExitState)?.done?.Invoke();

			yield return null;

			Assert.IsTrue(AsyncExitState.ExitStarted, "Async exit should have started");

			// Wait for async to complete (Task.Delay(100) ~= a few frames)
			yield return new WaitForSeconds(0.2f);

			Assert.IsTrue(AsyncExitState.ExitCompleted, "Async exit should have completed");
		}

		[UnityTest]
		public IEnumerator Transition_ToExitNode_StopsStateMachine()
		{
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName
			};
			var exit = new ExitNode { Id = "exit" };

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(exit);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "entry", TargetNodeId = "stateA", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "stateA", SourcePortName = "toB", TargetNodeId = "exit", ConnectionType = ConnectionType.Transition });

			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			Assert.IsTrue(_runner.IsRunning);

			(_runner.Instance.CurrentState as StateA)?.toB?.Invoke();
			yield return null;

			Assert.IsFalse(_runner.IsRunning, "State machine should stop after reaching exit node");
		}

		[UnityTest]
		public IEnumerator OnStopped_EventFires_WhenReachingExitNode()
		{
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName
			};
			var exit = new ExitNode { Id = "exit" };

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(exit);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "entry", TargetNodeId = "stateA", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "stateA", SourcePortName = "toB", TargetNodeId = "exit", ConnectionType = ConnectionType.Transition });

			_runner.Initialize(_graph);

			var stoppedFired = false;
			_runner.Instance.OnStopped += () => stoppedFired = true;

			_runner.StartStateMachine();
			yield return null;

			(_runner.Instance.CurrentState as StateA)?.toB?.Invoke();
			yield return null;

			Assert.IsTrue(stoppedFired, "OnStopped should fire when reaching exit node");
		}

	}
}
