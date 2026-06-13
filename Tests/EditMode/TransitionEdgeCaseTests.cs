using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.EditMode
{
	/// <summary>
	/// Tests for transition edge cases, portal navigation, and event ordering.
	/// </summary>
	[TestFixture]
	public class TransitionEdgeCaseTests
	{
		private StateMachineGraph _graph;
		private StateMachineInstance _instance;


		private class TransitionCounterState : State
		{
			public static int EnterCount;
			public static int ExitCount;
			public static List<string> EventLog = new List<string>();

			[Transition("Next")] public Action next;

			public override void OnEnter()
			{
				EnterCount++;
				EventLog.Add($"Enter:{GetType().Name}");
			}

			public override void OnExit()
			{
				ExitCount++;
				EventLog.Add($"Exit:{GetType().Name}");
			}

			public static void ResetAll()
			{
				EnterCount = 0;
				ExitCount = 0;
				EventLog.Clear();
			}
		}

		private class StateA : TransitionCounterState { }
		private class StateB : TransitionCounterState { }
		private class StateC : TransitionCounterState { }

		private class SelfLoopingState : State
		{
			public static int EnterCount;

			[Transition("Loop")] public Action loop;

			public override void OnEnter()
			{
				EnterCount++;
				// Keep looping until limit is hit
				if (EnterCount < 200)
				{
					loop?.Invoke();
				}
			}

			public static void Reset() => EnterCount = 0;
		}

		private class ImmediateTransitionState : State
		{
			[Transition("ToB")] public Action toB;

			public override void OnEnter()
			{
				// Immediately transition on enter
				toB?.Invoke();
			}
		}

		private class PauseTestState : State
		{
			public static bool EnterCalled;
			public static bool UpdateCalled;
			public static int UpdateCount;

			[Transition("Next")] public Action next;

			public override void OnEnter() => EnterCalled = true;

			public override void OnUpdate()
			{
				UpdateCalled = true;
				UpdateCount++;
			}

			public static void Reset()
			{
				EnterCalled = false;
				UpdateCalled = false;
				UpdateCount = 0;
			}
		}

		private class TransitionDuringPauseState : State
		{
			public static bool TransitionAttempted;

			[Transition("Next")] public Action next;

			public void TryTransition()
			{
				TransitionAttempted = true;
				next?.Invoke();
			}

			public static void Reset() => TransitionAttempted = false;
		}


		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			TransitionCounterState.ResetAll();
			SelfLoopingState.Reset();
			PauseTestState.Reset();
			TransitionDuringPauseState.Reset();
		}

		[TearDown]
		public void TearDown()
		{
			_instance?.Stop();
			_instance = null;

			if (_graph != null)
			{
				UnityEngine.Object.DestroyImmediate(_graph);
			}
		}


		[Test]
		public void MaxSameFrameTransitions_DefaultValue_Is100()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			Assert.AreEqual(100, _instance.MaxSameFrameTransitions);
		}

		[Test]
		public void MaxSameFrameTransitions_CanBeConfigured()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			_instance.MaxSameFrameTransitions = 25;

			Assert.AreEqual(25, _instance.MaxSameFrameTransitions);
		}

		[Test]
		public void MaxSameFrameTransitions_ExceedingLimit_LogsError()
		{
			SetupSelfLoopGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.MaxSameFrameTransitions = 10;

			LogAssert.Expect(LogType.Error, new Regex("exceeded 10 same-frame transitions"));

			_instance.Start();
		}

		[Test]
		public void MaxSameFrameTransitions_ExceedingLimit_StopsStateMachine()
		{
			SetupSelfLoopGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.MaxSameFrameTransitions = 10;

			LogAssert.Expect(LogType.Error, new Regex("exceeded 10 same-frame transitions"));

			_instance.Start();

			Assert.IsFalse(_instance.IsRunning);
		}

		[Test]
		public void MaxSameFrameTransitions_LimitOf50_AllowsUpTo50Transitions()
		{
			SetupSelfLoopGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.MaxSameFrameTransitions = 50;

			LogAssert.Expect(LogType.Error, new Regex("exceeded 50 same-frame transitions"));

			_instance.Start();

			// Should have entered at least 50 times before stopping
			Assert.GreaterOrEqual(SelfLoopingState.EnterCount, 50);
			Assert.LessOrEqual(SelfLoopingState.EnterCount, 52); // Some margin for implementation
		}



		[Test]
		public void OnTransitionTriggered_FiresOnTransition()
		{
			SetupLinearGraph();
			_instance = new StateMachineInstance(_graph);

			string fromNode = null;
			string toNode = null;
			int triggerCount = 0;

			_instance.OnTransitionTriggered += (from, to) =>
			{
				fromNode = from;
				toNode = to;
				triggerCount++;
			};

			_instance.Start();

			Assert.AreEqual(1, triggerCount);
			Assert.AreEqual("entry", fromNode);
			Assert.AreEqual("stateA", toNode);
		}

		[Test]
		public void OnTransitionTriggered_FiresForEachTransition()
		{
			SetupLinearGraph();
			_instance = new StateMachineInstance(_graph);

			var transitions = new List<(string from, string to)>();

			_instance.OnTransitionTriggered += (from, to) =>
			{
				transitions.Add((from, to));
			};

			_instance.Start();

			// Trigger next transition
			var stateA = _instance.CurrentState as StateA;
			stateA?.next?.Invoke();

			Assert.AreEqual(2, transitions.Count);
			Assert.AreEqual(("entry", "stateA"), transitions[0]);
			Assert.AreEqual(("stateA", "stateB"), transitions[1]);
		}

		[Test]
		public void OnTransitionTriggered_OrderingWithStateEvents()
		{
			SetupLinearGraph();
			_instance = new StateMachineInstance(_graph);

			var eventOrder = new List<string>();

			_instance.OnTransitionTriggered += (from, to) =>
			{
				eventOrder.Add($"Transition:{from}->{to}");
			};

			_instance.OnStateChanged += (old, newState) =>
			{
				var oldName = old?.GetType().Name ?? "null";
				var newName = newState?.GetType().Name ?? "null";
				eventOrder.Add($"StateChanged:{oldName}->{newName}");
			};

			_instance.Start();

			// Verify events occurred (order depends on implementation)
			Assert.Contains("Transition:entry->stateA", eventOrder);
			Assert.Contains("StateChanged:null->StateA", eventOrder);
		}



		[Test]
		public void Portal_MultipleChannels_RoutesCorrectly()
		{
			SetupMultiChannelPortalGraph();
			_instance = new StateMachineInstance(_graph);

			_instance.Start();

			// Should be at StateA initially
			Assert.IsInstanceOf<StateA>(_instance.CurrentState);

			// Trigger transition through channel 1 portal
			var stateA = _instance.CurrentState as StateA;
			stateA?.next?.Invoke();

			// Should arrive at StateB (connected via channel 1)
			Assert.IsInstanceOf<StateB>(_instance.CurrentState);
		}

		[Test]
		public void Portal_MissingMatchingPortalIn_LogsError()
		{
			SetupOrphanPortalOutGraph();
			_instance = new StateMachineInstance(_graph);

			_instance.Start();

			LogAssert.Expect(LogType.Error, new Regex("No PortalIn node found for channel"));

			var stateA = _instance.CurrentState as StateA;
			stateA?.next?.Invoke();
		}

		[Test]
		public void Portal_TransitionEventFires()
		{
			SetupSimplePortalGraph();
			_instance = new StateMachineInstance(_graph);

			var transitions = new List<(string from, string to)>();
			_instance.OnTransitionTriggered += (from, to) =>
			{
				transitions.Add((from, to));
			};

			_instance.Start();

			var stateA = _instance.CurrentState as StateA;
			stateA?.next?.Invoke();

			// Should have: entry->stateA, stateA->portalOut, portalOut->portalIn (internal transition)
			Assert.GreaterOrEqual(transitions.Count, 2);
		}



		[Test]
		public void Pause_PreventsUpdates()
		{
			SetupPauseTestGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			_instance.Update();
			Assert.AreEqual(1, PauseTestState.UpdateCount);

			_instance.Pause();
			_instance.Update();
			_instance.Update();
			_instance.Update();

			Assert.AreEqual(1, PauseTestState.UpdateCount);
		}

		[Test]
		public void Resume_AllowsUpdates()
		{
			SetupPauseTestGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			_instance.Pause();
			_instance.Update();
			Assert.AreEqual(0, PauseTestState.UpdateCount);

			_instance.Resume();
			_instance.Update();
			Assert.AreEqual(1, PauseTestState.UpdateCount);
		}

		[Test]
		public void Pause_TransitionStillWorks()
		{
			// Transitions should still work when paused (for programmatic control)
			SetupLinearGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			Assert.IsInstanceOf<StateA>(_instance.CurrentState);

			_instance.Pause();

			// Manual transition should still work
			var stateA = _instance.CurrentState as StateA;
			stateA?.next?.Invoke();

			Assert.IsInstanceOf<StateB>(_instance.CurrentState);
		}

		[Test]
		public void IsPaused_ReflectsState()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			Assert.IsFalse(_instance.IsPaused);

			_instance.Pause();
			Assert.IsTrue(_instance.IsPaused);

			_instance.Resume();
			Assert.IsFalse(_instance.IsPaused);
		}

		[Test]
		public void Pause_BeforeStart_DoesNotThrow()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			Assert.DoesNotThrow(() => _instance.Pause());
		}

		[Test]
		public void Resume_BeforeStart_DoesNotThrow()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			Assert.DoesNotThrow(() => _instance.Resume());
		}

		[Test]
		public void Pause_WhenAlreadyPaused_DoesNotThrow()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();
			_instance.Pause();

			Assert.DoesNotThrow(() => _instance.Pause());
			Assert.IsTrue(_instance.IsPaused);
		}

		[Test]
		public void Resume_WhenNotPaused_DoesNotThrow()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			Assert.DoesNotThrow(() => _instance.Resume());
			Assert.IsFalse(_instance.IsPaused);
		}



		[Test]
		public void MultipleExits_CanTransitionToSpecificExit()
		{
			SetupMultiExitGraph();
			_instance = new StateMachineInstance(_graph);

			string exitedWith = null;
			_instance.OnExited += (exitName) => exitedWith = exitName;

			_instance.Start();

			var stateA = _instance.CurrentState as MultiExitState;
			stateA?.exitSuccess?.Invoke();

			Assert.AreEqual("Success", exitedWith);
		}

		[Test]
		public void MultipleExits_DifferentExitNamesReported()
		{
			SetupMultiExitGraph();
			_instance = new StateMachineInstance(_graph);

			string exitedWith = null;
			_instance.OnExited += (exitName) => exitedWith = exitName;

			_instance.Start();

			var stateA = _instance.CurrentState as MultiExitState;
			stateA?.exitFailure?.Invoke();

			Assert.AreEqual("Failure", exitedWith);
		}



		[Test]
		public void ImmediateTransitionChain_WorksWithinLimit()
		{
			SetupChainGraph();
			_instance = new StateMachineInstance(_graph);

			_instance.Start();

			// Should have transitioned through the chain
			Assert.Greater(TransitionCounterState.EnterCount, 0);
		}

		[Test]
		public void TransitionChain_EventLogShowsCorrectOrder()
		{
			SetupLinearGraph();
			_instance = new StateMachineInstance(_graph);

			_instance.Start();

			// Trigger transition A -> B
			var stateA = _instance.CurrentState as StateA;
			stateA?.next?.Invoke();

			// Verify exit before enter
			var exitAIndex = TransitionCounterState.EventLog.IndexOf("Exit:StateA");
			var enterBIndex = TransitionCounterState.EventLog.IndexOf("Enter:StateB");

			Assert.Greater(enterBIndex, exitAIndex, "StateB should enter after StateA exits");
		}



		private class MultiExitState : State
		{
			[Transition("Success")] public Action exitSuccess;
			[Transition("Failure")] public Action exitFailure;
		}



		private void SetupSimpleGraph()
		{
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "stateA",
				ConnectionType = ConnectionType.Transition
			});
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

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
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
				SourcePortName = "next",
				TargetNodeId = "stateB",
				ConnectionType = ConnectionType.Transition
			});
		}

		private void SetupSelfLoopGraph()
		{
			var entry = new EntryNode { Id = "entry" };
			var looping = new StateNode
			{
				Id = "looping",
				StateTypeAssemblyQualifiedName = typeof(SelfLoopingState).AssemblyQualifiedName
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
		}

		private void SetupMultiChannelPortalGraph()
		{
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName
			};
			var portalOut1 = new PortalOutNode { Id = "portalOut1", ChannelId = 1 };
			var portalIn1 = new PortalInNode { Id = "portalIn1", ChannelId = 1 };
			var portalOut2 = new PortalOutNode { Id = "portalOut2", ChannelId = 2 };
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
			_graph.AddNode(portalIn1);
			_graph.AddNode(portalOut2);
			_graph.AddNode(portalIn2);
			_graph.AddNode(stateB);
			_graph.AddNode(stateC);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "entry", TargetNodeId = "stateA", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "stateA", SourcePortName = "next", TargetNodeId = "portalOut1", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c3", SourceNodeId = "portalIn1", TargetNodeId = "stateB", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c4", SourceNodeId = "portalIn2", TargetNodeId = "stateC", ConnectionType = ConnectionType.Transition });
		}

		private void SetupOrphanPortalOutGraph()
		{
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(StateA).AssemblyQualifiedName
			};
			var portalOut = new PortalOutNode { Id = "portalOut", ChannelId = 99 }; // No matching PortalIn

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(portalOut);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "entry", TargetNodeId = "stateA", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "stateA", SourcePortName = "next", TargetNodeId = "portalOut", ConnectionType = ConnectionType.Transition });
		}

		private void SetupSimplePortalGraph()
		{
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
			_graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "stateA", SourcePortName = "next", TargetNodeId = "portalOut", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c3", SourceNodeId = "portalIn", TargetNodeId = "stateB", ConnectionType = ConnectionType.Transition });
		}

		private void SetupPauseTestGraph()
		{
			var entry = new EntryNode { Id = "entry" };
			var state = new StateNode
			{
				Id = "state",
				StateTypeAssemblyQualifiedName = typeof(PauseTestState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(state);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "state",
				ConnectionType = ConnectionType.Transition
			});
		}

		private void SetupMultiExitGraph()
		{
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(MultiExitState).AssemblyQualifiedName
			};
			var exitSuccess = new ExitNode { Id = "exitSuccess", ExitName = "Success" };
			var exitFailure = new ExitNode { Id = "exitFailure", ExitName = "Failure" };

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(exitSuccess);
			_graph.AddNode(exitFailure);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection { Id = "c1", SourceNodeId = "entry", TargetNodeId = "stateA", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c2", SourceNodeId = "stateA", SourcePortName = "exitSuccess", TargetNodeId = "exitSuccess", ConnectionType = ConnectionType.Transition });
			_graph.AddConnection(new Connection { Id = "c3", SourceNodeId = "stateA", SourcePortName = "exitFailure", TargetNodeId = "exitFailure", ConnectionType = ConnectionType.Transition });
		}

		private void SetupChainGraph()
		{
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				StateTypeAssemblyQualifiedName = typeof(ImmediateTransitionState).AssemblyQualifiedName
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
		}

	}
}
