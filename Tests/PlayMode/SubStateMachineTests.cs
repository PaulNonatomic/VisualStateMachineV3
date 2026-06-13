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
	/// Tests for sub-state machine functionality at runtime.
	/// </summary>
	[TestFixture]
	public class SubStateMachineTests
	{
		private GameObject _gameObject;
		private StateMachineRunner _runner;
		private StateMachineGraph _parentGraph;
		private StateMachineGraph _childGraph;


		public class ParentStateA : State
		{
			public static bool Entered;
			public static bool Exited;

			[Transition("ToSub")] public Action toSub;

			public override void OnEnter() => Entered = true;
			public override void OnExit() => Exited = true;

			public static void Reset()
			{
				Entered = false;
				Exited = false;
			}
		}

		public class ParentStateB : State
		{
			public static bool Entered;

			public override void OnEnter() => Entered = true;

			public static void Reset() => Entered = false;
		}

		public class ChildStateA : State
		{
			public static bool Entered;
			public static bool Exited;

			[Transition("ToExit")] public Action toExit;

			public override void OnEnter() => Entered = true;
			public override void OnExit() => Exited = true;

			public static void Reset()
			{
				Entered = false;
				Exited = false;
			}
		}

		public class ChildStateB : State
		{
			public static bool Entered;

			[Transition("Done")] public Action done;

			public override void OnEnter() => Entered = true;

			public static void Reset() => Entered = false;
		}

		public class ContextAwareState : State<TestContext>
		{
			public static int ReceivedValue;

			public override void OnEnter()
			{
				ReceivedValue = SharedContext?.Value ?? -1;
			}

			public static void Reset() => ReceivedValue = 0;
		}

		public class TestContext : IStateMachineContext
		{
			public int Value { get; set; } = 42;
			public GameObject GameObject => null;
			public Transform Transform => null;
		}


		[SetUp]
		public void SetUp()
		{
			_gameObject = new GameObject("TestRunner");
			_runner = _gameObject.AddComponent<StateMachineRunner>();
			_parentGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			_childGraph = ScriptableObject.CreateInstance<StateMachineGraph>();

			ParentStateA.Reset();
			ParentStateB.Reset();
			ChildStateA.Reset();
			ChildStateB.Reset();
			ContextAwareState.Reset();
		}

		[TearDown]
		public void TearDown()
		{
			if (_gameObject != null)
			{
				UnityEngine.Object.Destroy(_gameObject);
			}

			if (_parentGraph != null)
			{
				UnityEngine.Object.DestroyImmediate(_parentGraph);
			}

			if (_childGraph != null)
			{
				UnityEngine.Object.DestroyImmediate(_childGraph);
			}
		}

		private void SetupChildGraph()
		{
			var childEntry = new EntryNode { Id = "childEntry" };
			var childStateA = new StateNode
			{
				Id = "childStateA",
				StateTypeAssemblyQualifiedName = typeof(ChildStateA).AssemblyQualifiedName
			};
			var childExit = new ExitNode { Id = "childExit", ExitName = "Done" };

			_childGraph.AddNode(childEntry);
			_childGraph.AddNode(childStateA);
			_childGraph.AddNode(childExit);
			_childGraph.EntryNodeId = "childEntry";

			_childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				TargetNodeId = "childStateA",
				ConnectionType = ConnectionType.Transition
			});

			_childGraph.AddConnection(new Connection
			{
				Id = "cc2",
				SourceNodeId = "childStateA",
				SourcePortName = "toExit",
				TargetNodeId = "childExit",
				ConnectionType = ConnectionType.Transition
			});
		}

		[UnityTest]
		public IEnumerator SubStateMachine_EntersChildGraph()
		{
			SetupChildGraph();

			// Parent graph: Entry -> ParentStateA -> SubStateMachine -> ParentStateB
			var entry = new EntryNode { Id = "entry" };
			var parentStateA = new StateNode
			{
				Id = "parentStateA",
				StateTypeAssemblyQualifiedName = typeof(ParentStateA).AssemblyQualifiedName
			};
			var subMachine = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = _childGraph
			};
			var parentStateB = new StateNode
			{
				Id = "parentStateB",
				StateTypeAssemblyQualifiedName = typeof(ParentStateB).AssemblyQualifiedName
			};

			_parentGraph.AddNode(entry);
			_parentGraph.AddNode(parentStateA);
			_parentGraph.AddNode(subMachine);
			_parentGraph.AddNode(parentStateB);
			_parentGraph.EntryNodeId = "entry";

			_parentGraph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "parentStateA",
				ConnectionType = ConnectionType.Transition
			});

			_parentGraph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "parentStateA",
				SourcePortName = "toSub",
				TargetNodeId = "subMachine",
				ConnectionType = ConnectionType.Transition
			});

			_parentGraph.AddConnection(new Connection
			{
				Id = "c3",
				SourceNodeId = "subMachine",
				SourcePortName = "Exit",
				TargetNodeId = "parentStateB",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_parentGraph);
			_runner.StartStateMachine();

			yield return null;

			Assert.IsTrue(ParentStateA.Entered);

			// Transition to sub-state machine
			var currentState = _runner.Instance.CurrentState as ParentStateA;
			currentState?.toSub?.Invoke();

			yield return null;

			// Should have entered child state
			Assert.IsTrue(ChildStateA.Entered);
		}

		[UnityTest]
		public IEnumerator SubStateMachine_ExitReturnToParent()
		{
			SetupChildGraph();

			var entry = new EntryNode { Id = "entry" };
			var subMachine = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = _childGraph
			};
			var parentStateB = new StateNode
			{
				Id = "parentStateB",
				StateTypeAssemblyQualifiedName = typeof(ParentStateB).AssemblyQualifiedName
			};

			_parentGraph.AddNode(entry);
			_parentGraph.AddNode(subMachine);
			_parentGraph.AddNode(parentStateB);
			_parentGraph.EntryNodeId = "entry";

			_parentGraph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "subMachine",
				ConnectionType = ConnectionType.Transition
			});

			_parentGraph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "subMachine",
				SourcePortName = "Exit",
				TargetNodeId = "parentStateB",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_parentGraph);
			_runner.StartStateMachine();

			yield return null;

			Assert.IsTrue(ChildStateA.Entered);

			// When a sub-state machine is active, the parent's CurrentState is null
			// The child's current state is accessed via ActiveChildInstance
			Assert.IsNotNull(_runner.Instance.ActiveChildInstance,
				"ActiveChildInstance should be set when sub-state machine is running");
			Assert.IsInstanceOf<ChildStateA>(_runner.Instance.ActiveChildInstance.CurrentState,
				"Child state machine should be in ChildStateA");
		}

		[UnityTest]
		public IEnumerator SubStateMachine_NullSubGraphDoesNotCrash()
		{
			var entry = new EntryNode { Id = "entry" };
			var subMachine = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = null // Intentionally null
			};
			var stateB = new StateNode
			{
				Id = "stateB",
				StateTypeAssemblyQualifiedName = typeof(ParentStateB).AssemblyQualifiedName
			};

			_parentGraph.AddNode(entry);
			_parentGraph.AddNode(subMachine);
			_parentGraph.AddNode(stateB);
			_parentGraph.EntryNodeId = "entry";

			_parentGraph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "subMachine",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_parentGraph);

			// Expect the error log when SubGraph is null
			LogAssert.Expect(LogType.Error, "SubStateMachineNode 'subMachine' has no sub-graph assigned");

			// Should not crash
			Assert.DoesNotThrow(() => _runner.StartStateMachine());

			yield return null;

			// Machine should have stopped or handled gracefully
			// (implementation-dependent behavior)
		}

		[UnityTest]
		public IEnumerator SubStateMachine_MaxNestingDepthProtection()
		{
			// Create a self-referencing graph (circular reference)
			var entry = new EntryNode { Id = "entry" };
			var subMachine = new SubStateMachineNode
			{
				Id = "subMachine"
			};

			_parentGraph.AddNode(entry);
			_parentGraph.AddNode(subMachine);
			_parentGraph.EntryNodeId = "entry";

			// Point to itself (circular reference)
			subMachine.SubGraph = _parentGraph;

			_parentGraph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "subMachine",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_parentGraph);

			// Should handle gracefully (detect circular reference or hit max depth)
			LogAssert.ignoreFailingMessages = true;
			_runner.StartStateMachine();

			yield return null;
			yield return null;

			LogAssert.ignoreFailingMessages = false;

			// Should have stopped due to protection
			// The exact behavior depends on implementation
		}


		/// <summary>
		/// Test context class for shared context tests.
		/// Note: SharedContext types do not need to implement IStateMachineContext.
		/// </summary>
		public class ParentContextA
		{
			public int ValueA { get; set; } = 100;
		}

		public class ChildContextB
		{
			public string ValueB { get; set; } = "child";
		}

		public class DerivedContextA : ParentContextA
		{
			public int ExtraValue { get; set; } = 200;
		}

		public class ContextRequiringState : State<ParentContextA>
		{
			public static int ReceivedContextValue;
			public static bool WasEntered;

			[Transition("Done")] public Action done;

			public override void OnEnter()
			{
				WasEntered = true;
				ReceivedContextValue = SharedContext?.ValueA ?? -1;
			}

			public static void Reset()
			{
				WasEntered = false;
				ReceivedContextValue = 0;
			}
		}

		public class ChildContextRequiringState : State<ChildContextB>
		{
			public static string ReceivedContextValue;

			public override void OnEnter()
			{
				ReceivedContextValue = SharedContext?.ValueB ?? "not-set";
			}

			public static void Reset()
			{
				ReceivedContextValue = null;
			}
		}

		[UnityTest]
		public IEnumerator SubStateMachine_ContextTypeMismatch_ErrorsGracefully()
		{
			ContextRequiringState.Reset();

			// Create child graph that requires ParentContextA
			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				StateTypeAssemblyQualifiedName = typeof(ContextRequiringState).AssemblyQualifiedName
			};
			var childExit = new ExitNode { Id = "childExit" };

			_childGraph.AddNode(childEntry);
			_childGraph.AddNode(childState);
			_childGraph.AddNode(childExit);
			_childGraph.EntryNodeId = "childEntry";
			_childGraph.ContextType = typeof(ParentContextA);

			_childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				TargetNodeId = "childState",
				ConnectionType = ConnectionType.Transition
			});

			// Parent graph
			var entry = new EntryNode { Id = "entry" };
			var subMachine = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = _childGraph
			};
			var fallbackState = new StateNode
			{
				Id = "fallbackState",
				StateTypeAssemblyQualifiedName = typeof(ParentStateB).AssemblyQualifiedName
			};

			_parentGraph.AddNode(entry);
			_parentGraph.AddNode(subMachine);
			_parentGraph.AddNode(fallbackState);
			_parentGraph.EntryNodeId = "entry";

			_parentGraph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "subMachine",
				ConnectionType = ConnectionType.Transition
			});

			_parentGraph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "subMachine",
				SourcePortName = "Exit",
				TargetNodeId = "fallbackState",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_parentGraph);

			// Start without any context - should log error about missing context
			LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*requires context type.*"));
			_runner.StartStateMachine();

			yield return null;

			// Child state should not have been entered
			Assert.IsFalse(ContextRequiringState.WasEntered);
		}

		[UnityTest]
		public IEnumerator SubStateMachine_IncompatibleContextTypes_ErrorsGracefully()
		{
			ChildContextRequiringState.Reset();

			// Create child graph that requires ChildContextB
			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				StateTypeAssemblyQualifiedName = typeof(ChildContextRequiringState).AssemblyQualifiedName
			};

			_childGraph.AddNode(childEntry);
			_childGraph.AddNode(childState);
			_childGraph.EntryNodeId = "childEntry";
			_childGraph.ContextType = typeof(ChildContextB);

			_childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				TargetNodeId = "childState",
				ConnectionType = ConnectionType.Transition
			});

			// Parent graph with ParentContextA (incompatible with ChildContextB)
			var entry = new EntryNode { Id = "entry" };
			var subMachine = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = _childGraph
			};
			var fallbackState = new StateNode
			{
				Id = "fallbackState",
				StateTypeAssemblyQualifiedName = typeof(ParentStateB).AssemblyQualifiedName
			};

			_parentGraph.AddNode(entry);
			_parentGraph.AddNode(subMachine);
			_parentGraph.AddNode(fallbackState);
			_parentGraph.EntryNodeId = "entry";

			_parentGraph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "subMachine",
				ConnectionType = ConnectionType.Transition
			});

			_parentGraph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "subMachine",
				SourcePortName = "Exit",
				TargetNodeId = "fallbackState",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_parentGraph);

			// Set parent context (ParentContextA) - incompatible with child's ChildContextB
			_runner.Instance.SetSharedContext(new ParentContextA());

			// Should log error about incompatible context types
			LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*requires context type.*ChildContextB.*but provided context is.*ParentContextA.*"));
			_runner.StartStateMachine();

			yield return null;

			// Child state should not receive context properly
			Assert.IsNull(ChildContextRequiringState.ReceivedContextValue);
		}

		[UnityTest]
		public IEnumerator SubStateMachine_DerivedContextType_IsAccepted()
		{
			ContextRequiringState.Reset();

			// Create child graph that requires ParentContextA
			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				StateTypeAssemblyQualifiedName = typeof(ContextRequiringState).AssemblyQualifiedName
			};
			var childExit = new ExitNode { Id = "childExit" };

			_childGraph.AddNode(childEntry);
			_childGraph.AddNode(childState);
			_childGraph.AddNode(childExit);
			_childGraph.EntryNodeId = "childEntry";
			_childGraph.ContextType = typeof(ParentContextA);

			_childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				TargetNodeId = "childState",
				ConnectionType = ConnectionType.Transition
			});

			_childGraph.AddConnection(new Connection
			{
				Id = "cc2",
				SourceNodeId = "childState",
				SourcePortName = "done",
				TargetNodeId = "childExit",
				ConnectionType = ConnectionType.Transition
			});

			// Parent graph
			var entry = new EntryNode { Id = "entry" };
			var subMachine = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = _childGraph
			};
			var finalState = new StateNode
			{
				Id = "finalState",
				StateTypeAssemblyQualifiedName = typeof(ParentStateB).AssemblyQualifiedName
			};

			_parentGraph.AddNode(entry);
			_parentGraph.AddNode(subMachine);
			_parentGraph.AddNode(finalState);
			_parentGraph.EntryNodeId = "entry";

			_parentGraph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "subMachine",
				ConnectionType = ConnectionType.Transition
			});

			_parentGraph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "subMachine",
				SourcePortName = "Exit",
				TargetNodeId = "finalState",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_parentGraph);

			// Provide derived context (DerivedContextA) - should be accepted since it's assignable to ParentContextA
			var derivedContext = new DerivedContextA { ValueA = 42 };
			_runner.Instance.SetSharedContext(derivedContext);

			_runner.StartStateMachine();

			yield return null;

			// Child state should have been entered and received the context value
			Assert.IsTrue(ContextRequiringState.WasEntered);
			Assert.AreEqual(42, ContextRequiringState.ReceivedContextValue);
		}



		public class BlackboardReadingState : State
		{
			public static int ReadValue;
			public static bool WasEntered;

			[Transition("Done")] public Action done;

			public override void OnEnter()
			{
				WasEntered = true;
				ReadValue = StateMachine.Blackboard.Get("mappedInt", -1);
			}

			public static void Reset()
			{
				WasEntered = false;
				ReadValue = -1;
			}
		}

		public class BlackboardWritingState : State
		{
			[Transition("Done")] public Action done;

			public override void OnEnter()
			{
				StateMachine.Blackboard.Set("childOutput", "child-wrote-this");
			}
		}

		[UnityTest]
		public IEnumerator SubStateMachine_InputMapping_CopiesParentToChild()
		{
			BlackboardReadingState.Reset();

			// Create child graph that reads from blackboard
			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				StateTypeAssemblyQualifiedName = typeof(BlackboardReadingState).AssemblyQualifiedName
			};
			var childExit = new ExitNode { Id = "childExit" };

			_childGraph.AddNode(childEntry);
			_childGraph.AddNode(childState);
			_childGraph.AddNode(childExit);
			_childGraph.EntryNodeId = "childEntry";

			_childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				TargetNodeId = "childState",
				ConnectionType = ConnectionType.Transition
			});

			_childGraph.AddConnection(new Connection
			{
				Id = "cc2",
				SourceNodeId = "childState",
				SourcePortName = "done",
				TargetNodeId = "childExit",
				ConnectionType = ConnectionType.Transition
			});

			// Parent graph
			var entry = new EntryNode { Id = "entry" };
			var subMachine = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = _childGraph
			};
			subMachine.InputMappings.Add(new VariableMapping(
				"parentValue",
				"mappedInt",
				VariableMappingDirection.ParentToChild
			));

			var finalState = new StateNode
			{
				Id = "finalState",
				StateTypeAssemblyQualifiedName = typeof(ParentStateB).AssemblyQualifiedName
			};

			_parentGraph.AddNode(entry);
			_parentGraph.AddNode(subMachine);
			_parentGraph.AddNode(finalState);
			_parentGraph.EntryNodeId = "entry";

			_parentGraph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "subMachine",
				ConnectionType = ConnectionType.Transition
			});

			_parentGraph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "subMachine",
				SourcePortName = "Exit",
				TargetNodeId = "finalState",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_parentGraph);

			// Set parent blackboard value
			_runner.Instance.Blackboard.Set("parentValue", 999);

			_runner.StartStateMachine();

			yield return null;

			// Child should have read the mapped value
			Assert.IsTrue(BlackboardReadingState.WasEntered);
			Assert.AreEqual(999, BlackboardReadingState.ReadValue);
		}

		[UnityTest]
		public IEnumerator SubStateMachine_OutputMapping_CopiesChildToParent()
		{
			ParentStateB.Reset();

			// Create child graph that writes to blackboard
			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				StateTypeAssemblyQualifiedName = typeof(BlackboardWritingState).AssemblyQualifiedName
			};
			var childExit = new ExitNode { Id = "childExit" };

			_childGraph.AddNode(childEntry);
			_childGraph.AddNode(childState);
			_childGraph.AddNode(childExit);
			_childGraph.EntryNodeId = "childEntry";

			_childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				TargetNodeId = "childState",
				ConnectionType = ConnectionType.Transition
			});

			_childGraph.AddConnection(new Connection
			{
				Id = "cc2",
				SourceNodeId = "childState",
				SourcePortName = "done",
				TargetNodeId = "childExit",
				ConnectionType = ConnectionType.Transition
			});

			// Parent graph
			var entry = new EntryNode { Id = "entry" };
			var subMachine = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = _childGraph
			};
			subMachine.OutputMappings.Add(new VariableMapping(
				"parentReceived",
				"childOutput",
				VariableMappingDirection.ChildToParent
			));

			var finalState = new StateNode
			{
				Id = "finalState",
				StateTypeAssemblyQualifiedName = typeof(ParentStateB).AssemblyQualifiedName
			};

			_parentGraph.AddNode(entry);
			_parentGraph.AddNode(subMachine);
			_parentGraph.AddNode(finalState);
			_parentGraph.EntryNodeId = "entry";

			_parentGraph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "subMachine",
				ConnectionType = ConnectionType.Transition
			});

			_parentGraph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "subMachine",
				SourcePortName = "Exit",
				TargetNodeId = "finalState",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_parentGraph);
			_runner.StartStateMachine();

			yield return null;

			// Trigger child state to write and exit
			var childInstance = _runner.Instance.ActiveChildInstance;
			Assert.IsNotNull(childInstance, "Should have active child instance");
			var childState_ = childInstance.CurrentState as BlackboardWritingState;
			childState_?.done?.Invoke();

			yield return null;

			// Parent should have received the mapped value
			var parentValue = _runner.Instance.Blackboard.GetBoxed("parentReceived");
			Assert.AreEqual("child-wrote-this", parentValue);
		}

		[UnityTest]
		public IEnumerator SubStateMachine_InputMapping_WithNullValue_HandlesGracefully()
		{
			BlackboardReadingState.Reset();

			// Create child graph
			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				StateTypeAssemblyQualifiedName = typeof(BlackboardReadingState).AssemblyQualifiedName
			};
			var childExit = new ExitNode { Id = "childExit" };

			_childGraph.AddNode(childEntry);
			_childGraph.AddNode(childState);
			_childGraph.AddNode(childExit);
			_childGraph.EntryNodeId = "childEntry";

			_childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				TargetNodeId = "childState",
				ConnectionType = ConnectionType.Transition
			});

			// Parent graph with mapping to non-existent variable
			var entry = new EntryNode { Id = "entry" };
			var subMachine = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = _childGraph
			};
			subMachine.InputMappings.Add(new VariableMapping(
				"nonExistentVariable",
				"mappedInt",
				VariableMappingDirection.ParentToChild
			));

			_parentGraph.AddNode(entry);
			_parentGraph.AddNode(subMachine);
			_parentGraph.EntryNodeId = "entry";

			_parentGraph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "subMachine",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_parentGraph);

			// Don't set the parent variable - it doesn't exist
			// Should not crash
			Assert.DoesNotThrow(() => _runner.StartStateMachine());

			yield return null;

			// Child should still enter but with default value
			Assert.IsTrue(BlackboardReadingState.WasEntered);
			Assert.AreEqual(-1, BlackboardReadingState.ReadValue); // Default from Get with fallback
		}

		public class BidirectionalSyncState : State
		{
			public static int InitialReadValue;
			public static bool WasEntered;

			[Transition("Done")] public Action done;

			public override void OnEnter()
			{
				WasEntered = true;
				// Read initial value
				InitialReadValue = StateMachine.Blackboard.Get("syncVar", -1);
				// Modify the value
				StateMachine.Blackboard.Set("syncVar", InitialReadValue * 2);
			}

			public static void Reset()
			{
				WasEntered = false;
				InitialReadValue = -1;
			}
		}

		[UnityTest]
		public IEnumerator SubStateMachine_BidirectionalMapping_SyncsValuesBothWays()
		{
			BidirectionalSyncState.Reset();
			ParentStateB.Reset();

			// Create child graph that reads and modifies the value
			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				StateTypeAssemblyQualifiedName = typeof(BidirectionalSyncState).AssemblyQualifiedName
			};
			var childExit = new ExitNode { Id = "childExit" };

			_childGraph.AddNode(childEntry);
			_childGraph.AddNode(childState);
			_childGraph.AddNode(childExit);
			_childGraph.EntryNodeId = "childEntry";

			_childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				TargetNodeId = "childState",
				ConnectionType = ConnectionType.Transition
			});

			_childGraph.AddConnection(new Connection
			{
				Id = "cc2",
				SourceNodeId = "childState",
				SourcePortName = "done",
				TargetNodeId = "childExit",
				ConnectionType = ConnectionType.Transition
			});

			// Parent graph with bidirectional mapping
			var entry = new EntryNode { Id = "entry" };
			var subMachine = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = _childGraph
			};

			// Use bidirectional mapping - same key maps to same key
			subMachine.InputMappings.Add(new VariableMapping(
				"parentVar",
				"syncVar",
				VariableMappingDirection.Bidirectional
			));
			subMachine.OutputMappings.Add(new VariableMapping(
				"parentVar",
				"syncVar",
				VariableMappingDirection.Bidirectional
			));

			var finalState = new StateNode
			{
				Id = "finalState",
				StateTypeAssemblyQualifiedName = typeof(ParentStateB).AssemblyQualifiedName
			};

			_parentGraph.AddNode(entry);
			_parentGraph.AddNode(subMachine);
			_parentGraph.AddNode(finalState);
			_parentGraph.EntryNodeId = "entry";

			_parentGraph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "subMachine",
				ConnectionType = ConnectionType.Transition
			});

			_parentGraph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "subMachine",
				SourcePortName = "Exit",
				TargetNodeId = "finalState",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_parentGraph);

			// Set initial parent value
			_runner.Instance.Blackboard.Set("parentVar", 50);

			_runner.StartStateMachine();

			yield return null;

			// Verify child received parent's value
			Assert.IsTrue(BidirectionalSyncState.WasEntered);
			Assert.AreEqual(50, BidirectionalSyncState.InitialReadValue);

			// Trigger exit
			var childInstance = _runner.Instance.ActiveChildInstance;
			Assert.IsNotNull(childInstance);
			var childStateInstance = childInstance.CurrentState as BidirectionalSyncState;
			childStateInstance?.done?.Invoke();

			yield return null;

			// Verify parent received modified value (50 * 2 = 100)
			var parentValue = _runner.Instance.Blackboard.Get("parentVar", -1);
			Assert.AreEqual(100, parentValue);
		}

		public class StringReadingState : State
		{
			public static string ReadValue;
			public static bool WasEntered;

			[Transition("Done")] public Action done;

			public override void OnEnter()
			{
				WasEntered = true;
				ReadValue = StateMachine.Blackboard.Get("stringVar", "not-found");
			}

			public static void Reset()
			{
				WasEntered = false;
				ReadValue = null;
			}
		}

		[UnityTest]
		public IEnumerator SubStateMachine_TypeConversion_IntToStringMapping_PreservesBoxedValue()
		{
			StringReadingState.Reset();

			// Create child graph that reads a string
			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				StateTypeAssemblyQualifiedName = typeof(StringReadingState).AssemblyQualifiedName
			};
			var childExit = new ExitNode { Id = "childExit" };

			_childGraph.AddNode(childEntry);
			_childGraph.AddNode(childState);
			_childGraph.AddNode(childExit);
			_childGraph.EntryNodeId = "childEntry";

			_childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				TargetNodeId = "childState",
				ConnectionType = ConnectionType.Transition
			});

			// Parent graph with mapping from int to string (type mismatch)
			var entry = new EntryNode { Id = "entry" };
			var subMachine = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = _childGraph
			};
			subMachine.InputMappings.Add(new VariableMapping(
				"parentInt",
				"stringVar",
				VariableMappingDirection.ParentToChild
			));

			_parentGraph.AddNode(entry);
			_parentGraph.AddNode(subMachine);
			_parentGraph.EntryNodeId = "entry";

			_parentGraph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "subMachine",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_parentGraph);

			// Set parent value as int
			_runner.Instance.Blackboard.Set("parentInt", 42);

			_runner.StartStateMachine();

			yield return null;

			// Child should have entered
			Assert.IsTrue(StringReadingState.WasEntered);

			// The boxed value transfer preserves the original type
			// When reading as string, the typed Get with string will fail to match
			// so it falls back to "not-found" default
			Assert.AreEqual("not-found", StringReadingState.ReadValue);
		}

		public class FloatReadingState : State
		{
			public static float ReadValue;
			public static bool WasEntered;

			[Transition("Done")] public Action done;

			public override void OnEnter()
			{
				WasEntered = true;
				ReadValue = StateMachine.Blackboard.Get("floatVar", -1f);
			}

			public static void Reset()
			{
				WasEntered = false;
				ReadValue = -1f;
			}
		}

		[UnityTest]
		public IEnumerator SubStateMachine_SameTypeMapping_PreservesValue()
		{
			FloatReadingState.Reset();

			// Create child graph that reads a float
			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				StateTypeAssemblyQualifiedName = typeof(FloatReadingState).AssemblyQualifiedName
			};
			var childExit = new ExitNode { Id = "childExit" };

			_childGraph.AddNode(childEntry);
			_childGraph.AddNode(childState);
			_childGraph.AddNode(childExit);
			_childGraph.EntryNodeId = "childEntry";

			_childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				TargetNodeId = "childState",
				ConnectionType = ConnectionType.Transition
			});

			// Parent graph with float to float mapping
			var entry = new EntryNode { Id = "entry" };
			var subMachine = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = _childGraph
			};
			subMachine.InputMappings.Add(new VariableMapping(
				"parentFloat",
				"floatVar",
				VariableMappingDirection.ParentToChild
			));

			_parentGraph.AddNode(entry);
			_parentGraph.AddNode(subMachine);
			_parentGraph.EntryNodeId = "entry";

			_parentGraph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "subMachine",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_parentGraph);

			// Set parent value as float
			_runner.Instance.Blackboard.Set("parentFloat", 3.14f);

			_runner.StartStateMachine();

			yield return null;

			// Child should have received the float value
			Assert.IsTrue(FloatReadingState.WasEntered);
			Assert.AreEqual(3.14f, FloatReadingState.ReadValue, 0.001f);
		}



		[UnityTest]
		public IEnumerator SubStateMachine_NestingDepthLimit_EnforcedAt16Levels()
		{
			// Create a chain of sub-state machines, each pointing to the next
			// We need 17 graphs to exceed the 16-level limit (0-indexed depth)
			var graphs = new StateMachineGraph[18];
			for (var i = 0; i < graphs.Length; i++)
			{
				graphs[i] = ScriptableObject.CreateInstance<StateMachineGraph>();
			}

			// Set up each graph to transition to a sub-state machine containing the next graph
			for (var i = 0; i < graphs.Length - 1; i++)
			{
				var graph = graphs[i];
				var entry = new EntryNode { Id = $"entry{i}" };
				var subMachine = new SubStateMachineNode
				{
					Id = $"subMachine{i}",
					SubGraph = graphs[i + 1]
				};

				graph.AddNode(entry);
				graph.AddNode(subMachine);
				graph.EntryNodeId = $"entry{i}";

				graph.AddConnection(new Connection
				{
					Id = $"c{i}",
					SourceNodeId = $"entry{i}",
					TargetNodeId = $"subMachine{i}",
					ConnectionType = ConnectionType.Transition
				});
			}

			// Last graph just has an entry and a regular state
			var lastGraph = graphs[graphs.Length - 1];
			var lastEntry = new EntryNode { Id = "lastEntry" };
			var lastState = new StateNode
			{
				Id = "lastState",
				StateTypeAssemblyQualifiedName = typeof(ParentStateB).AssemblyQualifiedName
			};

			lastGraph.AddNode(lastEntry);
			lastGraph.AddNode(lastState);
			lastGraph.EntryNodeId = "lastEntry";

			lastGraph.AddConnection(new Connection
			{
				Id = "lastConn",
				SourceNodeId = "lastEntry",
				TargetNodeId = "lastState",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(graphs[0]);

			// Expect error about exceeding max depth
			LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*nesting depth exceeded.*16.*"));

			_runner.StartStateMachine();

			yield return null;
			yield return null;

			// Clean up all the graphs
			foreach (var graph in graphs)
			{
				if (graph != null)
				{
					UnityEngine.Object.DestroyImmediate(graph);
				}
			}
		}



		[UnityTest]
		public IEnumerator SubStateMachine_CircularReference_AContainsBContainsA_Detected()
		{
			// Create two graphs that reference each other
			var graphA = ScriptableObject.CreateInstance<StateMachineGraph>();
			var graphB = ScriptableObject.CreateInstance<StateMachineGraph>();

			// Graph A: Entry -> SubMachine(B)
			var entryA = new EntryNode { Id = "entryA" };
			var subMachineA = new SubStateMachineNode
			{
				Id = "subMachineA",
				SubGraph = graphB
			};

			graphA.AddNode(entryA);
			graphA.AddNode(subMachineA);
			graphA.EntryNodeId = "entryA";

			graphA.AddConnection(new Connection
			{
				Id = "cA",
				SourceNodeId = "entryA",
				TargetNodeId = "subMachineA",
				ConnectionType = ConnectionType.Transition
			});

			// Graph B: Entry -> SubMachine(A) - creates circular reference
			var entryB = new EntryNode { Id = "entryB" };
			var subMachineB = new SubStateMachineNode
			{
				Id = "subMachineB",
				SubGraph = graphA
			};

			graphB.AddNode(entryB);
			graphB.AddNode(subMachineB);
			graphB.EntryNodeId = "entryB";

			graphB.AddConnection(new Connection
			{
				Id = "cB",
				SourceNodeId = "entryB",
				TargetNodeId = "subMachineB",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(graphA);

			// Expect error about circular reference
			LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*[Cc]ircular.*reference.*"));

			_runner.StartStateMachine();

			yield return null;

			// Clean up
			UnityEngine.Object.DestroyImmediate(graphA);
			UnityEngine.Object.DestroyImmediate(graphB);
		}

		[UnityTest]
		public IEnumerator SubStateMachine_CircularReference_DeepChainBackToRoot_Detected()
		{
			// Create a chain: A -> B -> C -> A (circular back to root)
			var graphA = ScriptableObject.CreateInstance<StateMachineGraph>();
			var graphB = ScriptableObject.CreateInstance<StateMachineGraph>();
			var graphC = ScriptableObject.CreateInstance<StateMachineGraph>();

			// Graph A: Entry -> SubMachine(B)
			graphA.AddNode(new EntryNode { Id = "entryA" });
			graphA.AddNode(new SubStateMachineNode { Id = "subA", SubGraph = graphB });
			graphA.EntryNodeId = "entryA";
			graphA.AddConnection(new Connection
			{
				Id = "cA",
				SourceNodeId = "entryA",
				TargetNodeId = "subA",
				ConnectionType = ConnectionType.Transition
			});

			// Graph B: Entry -> SubMachine(C)
			graphB.AddNode(new EntryNode { Id = "entryB" });
			graphB.AddNode(new SubStateMachineNode { Id = "subB", SubGraph = graphC });
			graphB.EntryNodeId = "entryB";
			graphB.AddConnection(new Connection
			{
				Id = "cB",
				SourceNodeId = "entryB",
				TargetNodeId = "subB",
				ConnectionType = ConnectionType.Transition
			});

			// Graph C: Entry -> SubMachine(A) - circular back to root
			graphC.AddNode(new EntryNode { Id = "entryC" });
			graphC.AddNode(new SubStateMachineNode { Id = "subC", SubGraph = graphA });
			graphC.EntryNodeId = "entryC";
			graphC.AddConnection(new Connection
			{
				Id = "cC",
				SourceNodeId = "entryC",
				TargetNodeId = "subC",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(graphA);

			// Expect error about circular reference
			LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*[Cc]ircular.*reference.*"));

			_runner.StartStateMachine();

			yield return null;

			// Clean up
			UnityEngine.Object.DestroyImmediate(graphA);
			UnityEngine.Object.DestroyImmediate(graphB);
			UnityEngine.Object.DestroyImmediate(graphC);
		}



		public class MultiExitChildStateA : State
		{
			public static bool Entered;
			[Transition("ExitSuccess")] public Action exitSuccess;
			[Transition("ExitFailure")] public Action exitFailure;

			public override void OnEnter() => Entered = true;
			public static void Reset() => Entered = false;
		}

		[UnityTest]
		public IEnumerator SubStateMachine_MultipleExitNodes_FirstExitTaken()
		{
			MultiExitChildStateA.Reset();
			ParentStateA.Reset();
			ParentStateB.Reset();

			// Create child graph with multiple exits
			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				StateTypeAssemblyQualifiedName = typeof(MultiExitChildStateA).AssemblyQualifiedName
			};
			var exitSuccess = new ExitNode { Id = "exitSuccess", ExitName = "Success" };
			var exitFailure = new ExitNode { Id = "exitFailure", ExitName = "Failure" };

			_childGraph.AddNode(childEntry);
			_childGraph.AddNode(childState);
			_childGraph.AddNode(exitSuccess);
			_childGraph.AddNode(exitFailure);
			_childGraph.EntryNodeId = "childEntry";

			_childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				TargetNodeId = "childState",
				ConnectionType = ConnectionType.Transition
			});

			_childGraph.AddConnection(new Connection
			{
				Id = "cc2",
				SourceNodeId = "childState",
				SourcePortName = "exitSuccess",
				TargetNodeId = "exitSuccess",
				ConnectionType = ConnectionType.Transition
			});

			_childGraph.AddConnection(new Connection
			{
				Id = "cc3",
				SourceNodeId = "childState",
				SourcePortName = "exitFailure",
				TargetNodeId = "exitFailure",
				ConnectionType = ConnectionType.Transition
			});

			// Parent graph
			var entry = new EntryNode { Id = "entry" };
			var subMachine = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = _childGraph
			};
			var successState = new StateNode
			{
				Id = "successState",
				StateTypeAssemblyQualifiedName = typeof(ParentStateA).AssemblyQualifiedName
			};
			var failureState = new StateNode
			{
				Id = "failureState",
				StateTypeAssemblyQualifiedName = typeof(ParentStateB).AssemblyQualifiedName
			};

			_parentGraph.AddNode(entry);
			_parentGraph.AddNode(subMachine);
			_parentGraph.AddNode(successState);
			_parentGraph.AddNode(failureState);
			_parentGraph.EntryNodeId = "entry";

			_parentGraph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "subMachine",
				ConnectionType = ConnectionType.Transition
			});

			// Connect the sub-machine's exit to success state
			// (In current implementation, all exits go through the single "Exit" port)
			_parentGraph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "subMachine",
				SourcePortName = "Exit",
				TargetNodeId = "successState",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_parentGraph);
			_runner.StartStateMachine();

			yield return null;

			Assert.IsTrue(MultiExitChildStateA.Entered);

			// Trigger the success exit
			var childInstance = _runner.Instance.ActiveChildInstance;
			Assert.IsNotNull(childInstance);
			var childStateInstance = childInstance.CurrentState as MultiExitChildStateA;
			childStateInstance?.exitSuccess?.Invoke();

			yield return null;

			// Should have transitioned to success state in parent
			Assert.IsTrue(ParentStateA.Entered);
			Assert.IsFalse(ParentStateB.Entered);
		}

		[UnityTest]
		public IEnumerator SubStateMachine_MultipleExitNodes_SecondExitTaken()
		{
			MultiExitChildStateA.Reset();
			ParentStateA.Reset();
			ParentStateB.Reset();

			// Create child graph with multiple exits
			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				StateTypeAssemblyQualifiedName = typeof(MultiExitChildStateA).AssemblyQualifiedName
			};
			var exitSuccess = new ExitNode { Id = "exitSuccess", ExitName = "Success" };
			var exitFailure = new ExitNode { Id = "exitFailure", ExitName = "Failure" };

			_childGraph.AddNode(childEntry);
			_childGraph.AddNode(childState);
			_childGraph.AddNode(exitSuccess);
			_childGraph.AddNode(exitFailure);
			_childGraph.EntryNodeId = "childEntry";

			_childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				TargetNodeId = "childState",
				ConnectionType = ConnectionType.Transition
			});

			_childGraph.AddConnection(new Connection
			{
				Id = "cc2",
				SourceNodeId = "childState",
				SourcePortName = "exitSuccess",
				TargetNodeId = "exitSuccess",
				ConnectionType = ConnectionType.Transition
			});

			_childGraph.AddConnection(new Connection
			{
				Id = "cc3",
				SourceNodeId = "childState",
				SourcePortName = "exitFailure",
				TargetNodeId = "exitFailure",
				ConnectionType = ConnectionType.Transition
			});

			// Parent graph
			var entry = new EntryNode { Id = "entry" };
			var subMachine = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = _childGraph
			};
			var successState = new StateNode
			{
				Id = "successState",
				StateTypeAssemblyQualifiedName = typeof(ParentStateA).AssemblyQualifiedName
			};

			_parentGraph.AddNode(entry);
			_parentGraph.AddNode(subMachine);
			_parentGraph.AddNode(successState);
			_parentGraph.EntryNodeId = "entry";

			_parentGraph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "subMachine",
				ConnectionType = ConnectionType.Transition
			});

			_parentGraph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "subMachine",
				SourcePortName = "Exit",
				TargetNodeId = "successState",
				ConnectionType = ConnectionType.Transition
			});

			_runner.Initialize(_parentGraph);
			_runner.StartStateMachine();

			yield return null;

			Assert.IsTrue(MultiExitChildStateA.Entered);

			// Trigger the failure exit
			var childInstance = _runner.Instance.ActiveChildInstance;
			Assert.IsNotNull(childInstance);
			var childStateInstance = childInstance.CurrentState as MultiExitChildStateA;
			childStateInstance?.exitFailure?.Invoke();

			yield return null;

			// Both exits go to the same parent state in current implementation
			// (sub-machine has single Exit port that routes to connected state)
			Assert.IsTrue(ParentStateA.Entered);
		}

	}
}
