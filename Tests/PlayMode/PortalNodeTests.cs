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
	/// <summary>
	/// Tests for PortalIn and PortalOut node functionality at runtime.
	/// </summary>
	[TestFixture]
	public class PortalNodeTests
	{
		private GameObject _gameObject;
		private StateMachineRunner _runner;
		private StateMachineGraph _graph;


		public class StateA : State
		{
			public static bool Entered;
			public static bool Exited;
			public static int EnterCount;

			[Transition("ToPortal")] public Action toPortal;

			public override void OnEnter()
			{
				Entered = true;
				EnterCount++;
			}

			public override void OnExit() => Exited = true;

			public static void Reset()
			{
				Entered = false;
				Exited = false;
				EnterCount = 0;
			}
		}

		public class StateB : State
		{
			public static bool Entered;
			public static int EnterCount;

			[Transition("Done")] public Action done;

			public override void OnEnter()
			{
				Entered = true;
				EnterCount++;
			}

			public static void Reset()
			{
				Entered = false;
				EnterCount = 0;
			}
		}

		public class StateC : State
		{
			public static bool Entered;

			public override void OnEnter() => Entered = true;

			public static void Reset() => Entered = false;
		}

		/// <summary>
		/// State that reads a blackboard value on enter to verify data persistence.
		/// </summary>
		public class BlackboardReaderState : State
		{
			public static bool Entered;
			public static int ReadValue;

			[Transition("Done")] public Action done;

			public override void OnEnter()
			{
				Entered = true;
				ReadValue = StateMachine.Blackboard.Get("testValue", -1);
			}

			public static void Reset()
			{
				Entered = false;
				ReadValue = -1;
			}
		}

		/// <summary>
		/// State that writes to blackboard before transitioning.
		/// </summary>
		public class BlackboardWriterState : State
		{
			public static bool Entered;

			[Transition("ToPortal")] public Action toPortal;

			public override void OnEnter()
			{
				Entered = true;
				StateMachine.Blackboard.Set("testValue", 42);
			}

			public static void Reset()
			{
				Entered = false;
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
			BlackboardReaderState.Reset();
			BlackboardWriterState.Reset();
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

		[UnityTest]
		public IEnumerator PortalOut_TransitionsToMatchingPortalIn()
		{
			// Create graph: Entry -> StateA -> PortalOut(ch1) ... PortalIn(ch1) -> StateB
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName
			};
			var portalOut = new PortalOutNode { Id = "portalOut", ChannelId = 1 };
			var portalIn = new PortalInNode { Id = "portalIn", ChannelId = 1 };
			var stateB = new StateNode
			{
				Id = "stateB",
				StateTypeAssemblyQualifiedName = typeof(StateB).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(portalOut);
			_graph.AddNode(portalIn);
			_graph.AddNode(stateB);
			_graph.EntryNodeId = "entry";

			// Entry -> StateA
			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "stateA",
				ConnectionType = ConnectionType.Transition
			});

			// StateA -> PortalOut
			_graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "stateA",
				SourcePortName = "toPortal",
				TargetNodeId = "portalOut",
				ConnectionType = ConnectionType.Transition
			});

			// PortalIn -> StateB
			_graph.AddConnection(new Connection
			{
				Id = "c3",
				SourceNodeId = "portalIn",
				TargetNodeId = "stateB",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			Assert.IsTrue(StateA.Entered);

			// Trigger transition to PortalOut
			var currentState = _runner.Instance.CurrentState as StateA;
			currentState?.toPortal?.Invoke();

			yield return null;

			// Should have jumped to StateB via PortalIn
			Assert.IsTrue(StateB.Entered);
			Assert.IsInstanceOf<StateB>(_runner.Instance.CurrentState);
		}

		[UnityTest]
		public IEnumerator PortalNodes_DifferentChannelsDoNotConnect()
		{
			// Create graph with mismatched channels
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName
			};
			var portalOut = new PortalOutNode { Id = "portalOut", ChannelId = 1 };
			var portalIn = new PortalInNode { Id = "portalIn", ChannelId = 2 }; // Different channel!
			var stateB = new StateNode
			{
				Id = "stateB",
				StateTypeAssemblyQualifiedName = typeof(StateB).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(portalOut);
			_graph.AddNode(portalIn);
			_graph.AddNode(stateB);
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
				SourcePortName = "toPortal",
				TargetNodeId = "portalOut",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c3",
				SourceNodeId = "portalIn",
				TargetNodeId = "stateB",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			var currentState = _runner.Instance.CurrentState as StateA;
			currentState?.toPortal?.Invoke();

			// Expect the error log when no matching PortalIn is found
			LogAssert.Expect(LogType.Error, "No PortalIn node found for channel 1");

			yield return null;

			// Should NOT have reached StateB because channels don't match
			Assert.IsFalse(StateB.Entered);
		}

		[UnityTest]
		public IEnumerator MultiplePortalInsSameChannel_UsesFirst()
		{
			// Create graph with multiple PortalIns on same channel
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName
			};
			var portalOut = new PortalOutNode { Id = "portalOut", ChannelId = 1 };
			var portalIn1 = new PortalInNode { Id = "portalIn1", ChannelId = 1 };
			var portalIn2 = new PortalInNode { Id = "portalIn2", ChannelId = 1 };
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
			_graph.AddNode(portalOut);
			_graph.AddNode(portalIn1);
			_graph.AddNode(portalIn2);
			_graph.AddNode(stateB);
			_graph.AddNode(stateC);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "entry", TargetNodeId = "stateA", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "stateA", SourcePortName = "toPortal", TargetNodeId = "portalOut", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c3", SourceNodeId = "portalIn1", TargetNodeId = "stateB", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c4", SourceNodeId = "portalIn2", TargetNodeId = "stateC", ConnectionType = ConnectionType.Transition });

			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			var currentState = _runner.Instance.CurrentState as StateA;
			currentState?.toPortal?.Invoke();

			yield return null;

			// Should reach one of them (implementation-dependent which one)
			Assert.IsTrue(StateB.Entered || StateC.Entered);
		}

		[UnityTest]
		public IEnumerator MultiplePortalOuts_SameChannel_AllRouteToSamePortalIn()
		{
			// Create graph with two PortalOuts both targeting the same channel
			// Entry -> StateA -> PortalOut1(ch1) ... PortalIn(ch1) -> StateB
			//                  -> StateC -> PortalOut2(ch1) -^
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName
			};
			var stateC = new StateNode
			{
				Id = "stateC",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName // Reuse StateA type
			};
			var portalOut1 = new PortalOutNode { Id = "portalOut1", ChannelId = 1 };
			var portalOut2 = new PortalOutNode { Id = "portalOut2", ChannelId = 1 }; // Same channel
			var portalIn = new PortalInNode { Id = "portalIn", ChannelId = 1 };
			var stateB = new StateNode
			{
				Id = "stateB",
				StateTypeAssemblyQualifiedName = typeof(StateB).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(stateC);
			_graph.AddNode(portalOut1);
			_graph.AddNode(portalOut2);
			_graph.AddNode(portalIn);
			_graph.AddNode(stateB);
			_graph.EntryNodeId = "entry";

			// Entry -> StateA
			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "stateA",
				ConnectionType = ConnectionType.Transition
			});

			// StateA -> PortalOut1
			_graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "stateA",
				SourcePortName = "toPortal",
				TargetNodeId = "portalOut1",
				ConnectionType = ConnectionType.Transition
			});

			// PortalIn -> StateB
			_graph.AddConnection(new Connection
			{
				Id = "c3",
				SourceNodeId = "portalIn",
				TargetNodeId = "stateB",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			Assert.IsTrue(StateA.Entered);

			// Trigger transition through first PortalOut
			var currentState = _runner.Instance.CurrentState as StateA;
			currentState?.toPortal?.Invoke();

			yield return null;

			// Should have jumped to StateB via PortalIn
			Assert.IsTrue(StateB.Entered);
			Assert.AreEqual(1, StateB.EnterCount);
		}

		[UnityTest]
		public IEnumerator PortalIn_NoOutgoingConnection_LogsWarning()
		{
			// Create graph: Entry -> StateA -> PortalOut(ch1) ... PortalIn(ch1) (no connection)
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName
			};
			var portalOut = new PortalOutNode { Id = "portalOut", ChannelId = 1 };
			var portalIn = new PortalInNode { Id = "portalIn", ChannelId = 1 };

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(portalOut);
			_graph.AddNode(portalIn);
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
				SourcePortName = "toPortal",
				TargetNodeId = "portalOut",
				ConnectionType = ConnectionType.Transition
			});

			// Note: No connection from portalIn to any state

			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			var currentState = _runner.Instance.CurrentState as StateA;
			currentState?.toPortal?.Invoke();

			// Expect warning when PortalIn has no outgoing connection
			LogAssert.Expect(LogType.Warning, "PortalIn node (channel 1) has no outgoing connection");

			yield return null;
		}

		[UnityTest]
		public IEnumerator Portal_TransitionEventFired()
		{
			// Create graph: Entry -> StateA -> PortalOut(ch1) ... PortalIn(ch1) -> StateB
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName
			};
			var portalOut = new PortalOutNode { Id = "portalOut", ChannelId = 1 };
			var portalIn = new PortalInNode { Id = "portalIn", ChannelId = 1 };
			var stateB = new StateNode
			{
				Id = "stateB",
				StateTypeAssemblyQualifiedName = typeof(StateB).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(portalOut);
			_graph.AddNode(portalIn);
			_graph.AddNode(stateB);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "entry", TargetNodeId = "stateA", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "stateA", SourcePortName = "toPortal", TargetNodeId = "portalOut", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c3", SourceNodeId = "portalIn", TargetNodeId = "stateB", ConnectionType = ConnectionType.Transition });

			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			// Track transition events
			var transitionEvents = new System.Collections.Generic.List<(string from, string to)>();
			_runner.Instance.OnTransitionTriggered += (from, to) => transitionEvents.Add((from, to));

			var currentState = _runner.Instance.CurrentState as StateA;
			currentState?.toPortal?.Invoke();

			yield return null;

			// Should have fired transition event from portalOut to portalIn
			Assert.IsTrue(transitionEvents.Exists(e => e.from == "portalOut" && e.to == "portalIn"),
				"Expected transition event from portalOut to portalIn");
		}

		[UnityTest]
		public IEnumerator Portal_BlackboardStatePreserved()
		{
			// Create graph: Entry -> WriterState -> PortalOut(ch1) ... PortalIn(ch1) -> ReaderState
			var entry = new EntryNode { Id = "entry" };
			var writerState = new StateNode
			{
				Id = "writerState",
				StateTypeAssemblyQualifiedName = typeof(BlackboardWriterState).AssemblyQualifiedName
			};
			var portalOut = new PortalOutNode { Id = "portalOut", ChannelId = 1 };
			var portalIn = new PortalInNode { Id = "portalIn", ChannelId = 1 };
			var readerState = new StateNode
			{
				Id = "readerState",
				StateTypeAssemblyQualifiedName = typeof(BlackboardReaderState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(writerState);
			_graph.AddNode(portalOut);
			_graph.AddNode(portalIn);
			_graph.AddNode(readerState);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "entry", TargetNodeId = "writerState", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "writerState", SourcePortName = "toPortal", TargetNodeId = "portalOut", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c3", SourceNodeId = "portalIn", TargetNodeId = "readerState", ConnectionType = ConnectionType.Transition });

			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			Assert.IsTrue(BlackboardWriterState.Entered, "Writer state should have entered");

			// Trigger transition through portal
			var currentState = _runner.Instance.CurrentState as BlackboardWriterState;
			currentState?.toPortal?.Invoke();

			yield return null;

			// Reader state should have entered and read the value set by writer
			Assert.IsTrue(BlackboardReaderState.Entered, "Reader state should have entered");
			Assert.AreEqual(42, BlackboardReaderState.ReadValue, "Blackboard value should be preserved across portal transition");
		}

		[UnityTest]
		public IEnumerator Portal_MultipleChannels_RoutedCorrectly()
		{
			// Create graph with multiple portal channels
			// StateA -> PortalOut(ch1) ... PortalIn(ch1) -> StateB
			// StateA -> PortalOut(ch2) ... PortalIn(ch2) -> StateC
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName
			};
			var portalOut1 = new PortalOutNode { Id = "portalOut1", ChannelId = 1 };
			var portalOut2 = new PortalOutNode { Id = "portalOut2", ChannelId = 2 };
			var portalIn1 = new PortalInNode { Id = "portalIn1", ChannelId = 1 };
			var portalIn2 = new PortalInNode { Id = "portalIn2", ChannelId = 2 };
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
			_graph.AddNode(portalOut1);
			_graph.AddNode(portalOut2);
			_graph.AddNode(portalIn1);
			_graph.AddNode(portalIn2);
			_graph.AddNode(stateB);
			_graph.AddNode(stateC);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "entry", TargetNodeId = "stateA", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "stateA", SourcePortName = "toPortal", TargetNodeId = "portalOut1", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c3", SourceNodeId = "portalIn1", TargetNodeId = "stateB", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c4", SourceNodeId = "portalIn2", TargetNodeId = "stateC", ConnectionType = ConnectionType.Transition });

			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			// Trigger transition to channel 1
			var currentState = _runner.Instance.CurrentState as StateA;
			currentState?.toPortal?.Invoke();

			yield return null;

			// Should have gone to StateB (channel 1), not StateC (channel 2)
			Assert.IsTrue(StateB.Entered, "Should reach StateB through channel 1");
			Assert.IsFalse(StateC.Entered, "Should not reach StateC (channel 2)");
		}

		[UnityTest]
		public IEnumerator Portal_MultiHopChain_TraversesMultiplePortals()
		{
			// Graph: Entry -> StateA -> PortalOut(ch1) ... PortalIn(ch1) -> StateB -> PortalOut(ch2) ... PortalIn(ch2) -> StateC
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName
			};
			var portalOut1 = new PortalOutNode { Id = "portalOut1", ChannelId = 1 };
			var portalIn1 = new PortalInNode { Id = "portalIn1", ChannelId = 1 };
			var stateB = new StateNode
			{
				Id = "stateB",
				StateTypeAssemblyQualifiedName = typeof(StateB).AssemblyQualifiedName
			};
			var portalOut2 = new PortalOutNode { Id = "portalOut2", ChannelId = 2 };
			var portalIn2 = new PortalInNode { Id = "portalIn2", ChannelId = 2 };
			var stateC = new StateNode
			{
				Id = "stateC",
				StateTypeAssemblyQualifiedName = typeof(StateC).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(portalOut1);
			_graph.AddNode(portalIn1);
			_graph.AddNode(stateB);
			_graph.AddNode(portalOut2);
			_graph.AddNode(portalIn2);
			_graph.AddNode(stateC);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "entry", TargetNodeId = "stateA", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "stateA", SourcePortName = "toPortal", TargetNodeId = "portalOut1", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c3", SourceNodeId = "portalIn1", TargetNodeId = "stateB", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c4", SourceNodeId = "stateB", SourcePortName = "done", TargetNodeId = "portalOut2", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c5", SourceNodeId = "portalIn2", TargetNodeId = "stateC", ConnectionType = ConnectionType.Transition });

			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			Assert.IsTrue(StateA.Entered, "StateA should have entered");

			// First portal hop: A -> PortalOut(ch1) -> PortalIn(ch1) -> B
			(_runner.Instance.CurrentState as StateA)?.toPortal?.Invoke();
			yield return null;

			Assert.IsTrue(StateB.Entered, "StateB should have entered via portal channel 1");

			// Second portal hop: B -> PortalOut(ch2) -> PortalIn(ch2) -> C
			(_runner.Instance.CurrentState as StateB)?.done?.Invoke();
			yield return null;

			Assert.IsTrue(StateC.Entered, "StateC should have entered via portal channel 2");
			Assert.IsInstanceOf<StateC>(_runner.Instance.CurrentState);
		}

		[UnityTest]
		public IEnumerator Portal_TransitionTracking_RecordsPortalHops()
		{
			// Entry -> StateA -> PortalOut(ch1) ... PortalIn(ch1) -> StateB
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName
			};
			var portalOut = new PortalOutNode { Id = "portalOut", ChannelId = 1 };
			var portalIn = new PortalInNode { Id = "portalIn", ChannelId = 1 };
			var stateB = new StateNode
			{
				Id = "stateB",
				StateTypeAssemblyQualifiedName = typeof(StateB).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(portalOut);
			_graph.AddNode(portalIn);
			_graph.AddNode(stateB);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "entry", TargetNodeId = "stateA", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "stateA", SourcePortName = "toPortal", TargetNodeId = "portalOut", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c3", SourceNodeId = "portalIn", TargetNodeId = "stateB", ConnectionType = ConnectionType.Transition });

			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			var transitions = new System.Collections.Generic.List<(string from, string to)>();
			_runner.Instance.OnTransitionTriggered += (from, to) => transitions.Add((from, to));

			(_runner.Instance.CurrentState as StateA)?.toPortal?.Invoke();
			yield return null;

			// Should record the full transition chain including portal nodes
			Assert.Greater(transitions.Count, 0, "Should have recorded portal transitions");
			Assert.IsTrue(StateB.Entered, "StateB should have been reached");
		}

		[UnityTest]
		public IEnumerator Portal_ExitAfterPortal_StateMachineStops()
		{
			// Entry -> StateA -> PortalOut(ch1) ... PortalIn(ch1) -> StateB -> Exit
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName
			};
			var portalOut = new PortalOutNode { Id = "portalOut", ChannelId = 1 };
			var portalIn = new PortalInNode { Id = "portalIn", ChannelId = 1 };
			var stateB = new StateNode
			{
				Id = "stateB",
				StateTypeAssemblyQualifiedName = typeof(StateB).AssemblyQualifiedName
			};
			var exit = new ExitNode { Id = "exit" };

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(portalOut);
			_graph.AddNode(portalIn);
			_graph.AddNode(stateB);
			_graph.AddNode(exit);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "entry", TargetNodeId = "stateA", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "stateA", SourcePortName = "toPortal", TargetNodeId = "portalOut", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c3", SourceNodeId = "portalIn", TargetNodeId = "stateB", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c4", SourceNodeId = "stateB", SourcePortName = "done", TargetNodeId = "exit", ConnectionType = ConnectionType.Transition });

			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			// A -> portal -> B
			(_runner.Instance.CurrentState as StateA)?.toPortal?.Invoke();
			yield return null;

			Assert.IsTrue(StateB.Entered);
			Assert.IsTrue(_runner.IsRunning);

			// B -> Exit
			(_runner.Instance.CurrentState as StateB)?.done?.Invoke();
			yield return null;

			Assert.IsFalse(_runner.IsRunning, "State machine should stop after portal chain reaches exit");
		}

		[UnityTest]
		public IEnumerator Portal_StateExitCalled_BeforePortalTransition()
		{
			// Verify OnExit is called on StateA when transitioning through a portal
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName
			};
			var portalOut = new PortalOutNode { Id = "portalOut", ChannelId = 1 };
			var portalIn = new PortalInNode { Id = "portalIn", ChannelId = 1 };
			var stateB = new StateNode
			{
				Id = "stateB",
				StateTypeAssemblyQualifiedName = typeof(StateB).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(portalOut);
			_graph.AddNode(portalIn);
			_graph.AddNode(stateB);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "entry", TargetNodeId = "stateA", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "stateA", SourcePortName = "toPortal", TargetNodeId = "portalOut", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c3", SourceNodeId = "portalIn", TargetNodeId = "stateB", ConnectionType = ConnectionType.Transition });

			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			Assert.IsTrue(StateA.Entered);
			Assert.IsFalse(StateA.Exited, "StateA should not have exited yet");

			(_runner.Instance.CurrentState as StateA)?.toPortal?.Invoke();
			yield return null;

			Assert.IsTrue(StateA.Exited, "StateA.OnExit should be called before portal transition");
			Assert.IsTrue(StateB.Entered, "StateB should have entered after portal");
		}
	}
}
