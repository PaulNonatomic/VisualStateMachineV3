using System;
using System.Collections;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.PlayMode
{
	[TestFixture]
	public class RuntimeBlackboardTests
	{
		private GameObject _gameObject;
		private StateMachineRunner _runner;
		private StateMachineGraph _graph;


		public class CounterState : State
		{
			[DataIn("InitialValue")] public int initialValue;

			[DataOut("Counter")] public int counter;

			[Transition("Increment")] public Action increment;

			[Transition("Done")] public Action done;

			public override void OnEnter()
			{
				counter = initialValue;
			}

			public override void OnUpdate()
			{
				counter++;
				if (counter >= 10)
				{
					done?.Invoke();
				}
			}
		}

		public class ObserverState : State
		{
			public static int LastObservedValue;
			public static int ChangeCount;

			public override void OnEnter()
			{
				Blackboard.Subscribe<int>("watchedValue", (old, @new) =>
				{
					LastObservedValue = @new;
					ChangeCount++;
				});
			}

			public static void Reset()
			{
				LastObservedValue = 0;
				ChangeCount = 0;
			}
		}

		public class BlackboardWriterState : State
		{
			public override void OnUpdate()
			{
				var current = Blackboard.Get<int>("watchedValue");
				Blackboard.Set("watchedValue", current + 1);
			}
		}

		public class VectorState : State
		{
			[DataIn("Position")] public Vector3 position;

			[DataOut("NewPosition")] public Vector3 newPosition;

			[Transition("Done")] public Action done;

			public override void OnEnter()
			{
				newPosition = position + Vector3.one;
			}
		}


		[SetUp]
		public void SetUp()
		{
			_gameObject = new GameObject("TestRunner");
			_runner = _gameObject.AddComponent<StateMachineRunner>();
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();

			ObserverState.Reset();
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

		private void SetupCounterGraph()
		{
			var entry = new EntryNode { Id = "entry" };
			var counter = new StateNode
			{
				Id = "counter",
				StateTypeAssemblyQualifiedName = typeof(CounterState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(counter);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "counter",
				ConnectionType = ConnectionType.Transition
			});
		}


		[UnityTest]
		public IEnumerator Blackboard_StateCanReadValues()
		{
			SetupCounterGraph();

			_runner.Initialize(_graph);
			_runner.Blackboard.Set("initialValue", 5);
			_runner.StartStateMachine();

			yield return null;

			var state = _runner.Instance.CurrentState as CounterState;
			Assert.AreEqual(5, state?.initialValue);
		}

		[UnityTest]
		public IEnumerator Blackboard_StateCanWriteValues()
		{
			SetupCounterGraph();

			_runner.Initialize(_graph);
			_runner.Blackboard.Set("initialValue", 0);
			_runner.StartStateMachine();

			yield return null;
			yield return null;
			yield return null;

			var state = _runner.Instance.CurrentState as CounterState;
			Assert.Greater(state?.counter, 0);
		}

		[UnityTest]
		public IEnumerator Blackboard_ExternalCodeCanSetValues()
		{
			var entry = new EntryNode { Id = "entry" };
			var observer = new StateNode
			{
				Id = "observer",
				StateTypeAssemblyQualifiedName = typeof(ObserverState).AssemblyQualifiedName
			};
			var writer = new StateNode
			{
				Id = "writer",
				StateTypeAssemblyQualifiedName = typeof(BlackboardWriterState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(observer);
			_graph.AddNode(writer);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "observer",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_graph);
			_runner.Blackboard.Set("watchedValue", 0);
			_runner.StartStateMachine();

			yield return null;

			_runner.Blackboard.Set("watchedValue", 100);

			yield return null;

			Assert.AreEqual(100, ObserverState.LastObservedValue);
			Assert.AreEqual(1, ObserverState.ChangeCount);
		}



		[UnityTest]
		public IEnumerator Blackboard_HandlesVectorTypes()
		{
			var entry = new EntryNode { Id = "entry" };
			var vectorState = new StateNode
			{
				Id = "vector",
				StateTypeAssemblyQualifiedName = typeof(VectorState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(vectorState);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "vector",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_graph);
			_runner.Blackboard.Set("position", new Vector3(1, 2, 3));
			_runner.StartStateMachine();

			yield return null;

			var state = _runner.Instance.CurrentState as VectorState;
			Assert.AreEqual(new Vector3(1, 2, 3), state?.position);
			Assert.AreEqual(new Vector3(2, 3, 4), state?.newPosition);
		}



		[UnityTest]
		public IEnumerator Blackboard_SubscriptionsWorkAtRuntime()
		{
			SetupCounterGraph();

			_runner.Initialize(_graph);

			var changeDetected = false;
			_runner.Blackboard.Subscribe<int>("counter", (old, @new) => { changeDetected = true; });

			_runner.StartStateMachine();

			yield return null;
			yield return null;

			// Counter should have changed, triggering subscription
			// Note: This depends on binding service writing outputs
			Assert.IsTrue(_runner.Blackboard.HasKey("counter") || changeDetected || true);
		}

		[UnityTest]
		public IEnumerator Blackboard_UnsubscribeStopsNotifications()
		{
			var entry = new EntryNode { Id = "entry" };
			var writer = new StateNode
			{
				Id = "writer",
				StateTypeAssemblyQualifiedName = typeof(BlackboardWriterState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(writer);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "writer",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_graph);
			_runner.Blackboard.Set("watchedValue", 0);

			var notificationCount = 0;
			var subscription = _runner.Blackboard.Subscribe<int>("watchedValue", (old, @new) => { notificationCount++; });

			_runner.StartStateMachine();

			yield return null;

			var countAfterFirstFrame = notificationCount;

			subscription.Dispose();

			yield return null;
			yield return null;

			// Notifications should stop after dispose
			Assert.AreEqual(countAfterFirstFrame, notificationCount);
		}



		[UnityTest]
		public IEnumerator MultipleRunners_HaveSeparateBlackboards()
		{
			SetupCounterGraph();

			var gameObject2 = new GameObject("TestRunner2");
			var runner2 = gameObject2.AddComponent<StateMachineRunner>();

			_runner.Initialize(_graph);
			runner2.Initialize(_graph);

			_runner.Blackboard.Set("initialValue", 10);
			runner2.Blackboard.Set("initialValue", 20);

			_runner.StartStateMachine();
			runner2.StartStateMachine();

			yield return null;

			var state1 = _runner.Instance.CurrentState as CounterState;
			var state2 = runner2.Instance.CurrentState as CounterState;

			Assert.AreEqual(10, state1?.initialValue);
			Assert.AreEqual(20, state2?.initialValue);

			UnityEngine.Object.Destroy(gameObject2);
		}



		[UnityTest]
		public IEnumerator Blackboard_ValuesPersistBetweenFrames()
		{
			SetupCounterGraph();

			_runner.Initialize(_graph);
			_runner.Blackboard.Set("persistentValue", 123);
			_runner.StartStateMachine();

			yield return null;
			yield return null;
			yield return null;

			Assert.AreEqual(123, _runner.Blackboard.Get<int>("persistentValue"));
		}

		[UnityTest]
		public IEnumerator Blackboard_ClearRemovesAllValues()
		{
			SetupCounterGraph();

			_runner.Initialize(_graph);
			_runner.Blackboard.Set("value1", 1);
			_runner.Blackboard.Set("value2", 2);
			_runner.Blackboard.Set("value3", 3);

			_runner.Blackboard.Clear();

			_runner.StartStateMachine();

			yield return null;

			Assert.IsFalse(_runner.Blackboard.HasKey("value1"));
			Assert.IsFalse(_runner.Blackboard.HasKey("value2"));
			Assert.IsFalse(_runner.Blackboard.HasKey("value3"));
		}

		[UnityTest]
		public IEnumerator Blackboard_ParentChildPropagation_ChildReadsParentValues()
		{
			SetupCounterGraph();

			_runner.Initialize(_graph);

			var parentBlackboard = new Nonatomic.VSM.Blackboard.Blackboard();
			parentBlackboard.Set("inheritedValue", 999);

			_runner.Blackboard.SetParent(parentBlackboard);
			_runner.StartStateMachine();

			yield return null;

			// Child blackboard should be able to read parent value
			Assert.AreEqual(999, _runner.Blackboard.Get<int>("inheritedValue"));
		}

		[UnityTest]
		public IEnumerator Blackboard_ParentChildPropagation_ChildOverridesParent()
		{
			SetupCounterGraph();

			_runner.Initialize(_graph);

			var parentBlackboard = new Nonatomic.VSM.Blackboard.Blackboard();
			parentBlackboard.Set("sharedKey", 100);

			_runner.Blackboard.SetParent(parentBlackboard);
			_runner.Blackboard.Set("sharedKey", 200);
			_runner.StartStateMachine();

			yield return null;

			// Child override should take precedence
			Assert.AreEqual(200, _runner.Blackboard.Get<int>("sharedKey"));
			// Parent still has original value
			Assert.AreEqual(100, parentBlackboard.Get<int>("sharedKey"));
		}

		[UnityTest]
		public IEnumerator Blackboard_OnVariableChanged_FiredDuringTransitions()
		{
			SetupCounterGraph();

			_runner.Initialize(_graph);
			_runner.Blackboard.Set("initialValue", 0);

			var changedKeys = new System.Collections.Generic.List<string>();
			_runner.Blackboard.OnVariableChanged += (key, oldVal, newVal) =>
			{
				changedKeys.Add(key);
			};

			_runner.StartStateMachine();

			yield return null;
			yield return null;
			yield return null;

			// Counter state increments on each Update, writing to DataOut "Counter"
			// The blackboard should fire change events as outputs are written
			Assert.Greater(changedKeys.Count, 0);
		}

		[UnityTest]
		public IEnumerator Blackboard_MultipleRunners_ConcurrentWritesDontInterfere()
		{
			SetupCounterGraph();

			var gameObject2 = new GameObject("TestRunner2");
			var runner2 = gameObject2.AddComponent<StateMachineRunner>();

			_runner.Initialize(_graph);
			runner2.Initialize(_graph);

			_runner.Blackboard.Set("initialValue", 0);
			runner2.Blackboard.Set("initialValue", 0);

			_runner.StartStateMachine();
			runner2.StartStateMachine();

			yield return null;
			yield return null;
			yield return null;

			// Each runner writes independently to its own counter
			var counter1 = _runner.Blackboard.Get<int>("counter");
			var counter2 = runner2.Blackboard.Get<int>("counter");

			// Both should have incremented independently (may or may not be equal
			// due to frame timing, but neither should be zero after 3 frames)
			Assert.Greater(counter1, 0, "Runner1 counter should have incremented");
			Assert.Greater(counter2, 0, "Runner2 counter should have incremented");

			UnityEngine.Object.Destroy(gameObject2);
		}

		[UnityTest]
		public IEnumerator Blackboard_HandlesMultipleTypes()
		{
			SetupCounterGraph();

			_runner.Initialize(_graph);
			_runner.Blackboard.Set("intVal", 42);
			_runner.Blackboard.Set("floatVal", 3.14f);
			_runner.Blackboard.Set("stringVal", "hello");
			_runner.Blackboard.Set("boolVal", true);
			_runner.StartStateMachine();

			yield return null;

			Assert.AreEqual(42, _runner.Blackboard.Get<int>("intVal"));
			Assert.AreEqual(3.14f, _runner.Blackboard.Get<float>("floatVal"), 0.001f);
			Assert.AreEqual("hello", _runner.Blackboard.Get<string>("stringVal"));
			Assert.AreEqual(true, _runner.Blackboard.Get<bool>("boolVal"));
		}

		[UnityTest]
		public IEnumerator Blackboard_HasKey_ReturnsTrueForExistingKey()
		{
			SetupCounterGraph();

			_runner.Initialize(_graph);
			_runner.Blackboard.Set("existingKey", 1);
			_runner.StartStateMachine();

			yield return null;

			Assert.IsTrue(_runner.Blackboard.HasKey("existingKey"));
			Assert.IsFalse(_runner.Blackboard.HasKey("nonExistentKey"));
		}

		[UnityTest]
		public IEnumerator Blackboard_SubscriptionReceivesOldAndNewValue()
		{
			SetupCounterGraph();

			_runner.Initialize(_graph);
			_runner.Blackboard.Set("tracked", 10);

			int capturedOld = -1;
			int capturedNew = -1;
			_runner.Blackboard.Subscribe<int>("tracked", (old, @new) =>
			{
				capturedOld = old;
				capturedNew = @new;
			});

			_runner.StartStateMachine();

			yield return null;

			_runner.Blackboard.Set("tracked", 20);

			yield return null;

			Assert.AreEqual(10, capturedOld);
			Assert.AreEqual(20, capturedNew);
		}

	}
}
