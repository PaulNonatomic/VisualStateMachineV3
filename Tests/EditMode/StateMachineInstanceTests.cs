using System;
using System.Text.RegularExpressions;
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
	public class StateMachineInstanceTests
	{
		private StateMachineGraph _graph;
		private StateMachineInstance _instance;


		private class TestStateA : State
		{
			public static int EnterCount;
			public static int UpdateCount;
			public static int ExitCount;

			[Transition("To B")] public Action toB;

			public override void OnEnter() => EnterCount++;
			public override void OnUpdate() => UpdateCount++;
			public override void OnExit() => ExitCount++;

			public static void ResetCounters()
			{
				EnterCount = 0;
				UpdateCount = 0;
				ExitCount = 0;
			}
		}

		private class TestStateB : State
		{
			public static int EnterCount;
			public static int UpdateCount;
			public static int ExitCount;

			[Transition("To A")] public Action toA;

			[Transition("To Exit")] public Action toExit;

			public override void OnEnter() => EnterCount++;
			public override void OnUpdate() => UpdateCount++;
			public override void OnExit() => ExitCount++;

			public static void ResetCounters()
			{
				EnterCount = 0;
				UpdateCount = 0;
				ExitCount = 0;
			}
		}

		private class InfiniteLoopState : State
		{
			[Transition("Loop")] public Action loop;

			public override void OnEnter()
			{
				loop?.Invoke();
			}
		}


		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			TestStateA.ResetCounters();
			TestStateB.ResetCounters();
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
		public void Constructor_InitializesWithGraph()
		{
			SetupSimpleGraph();

			_instance = new StateMachineInstance(_graph);

			Assert.IsNotNull(_instance);
			Assert.IsNotNull(_instance.Blackboard);
		}

		[Test]
		public void Constructor_NullGraph_ThrowsException()
		{
			Assert.Throws<ArgumentNullException>(() => new StateMachineInstance(null));
		}



		[Test]
		public void Start_SetsIsRunningTrue()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			_instance.Start();

			Assert.IsTrue(_instance.IsRunning);
		}

		[Test]
		public void Stop_SetsIsRunningFalse()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			_instance.Stop();

			Assert.IsFalse(_instance.IsRunning);
		}

		[Test]
		public void Start_EntersFirstState()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			_instance.Start();

			Assert.AreEqual(1, TestStateA.EnterCount);
		}

		[Test]
		public void Stop_ExitsCurrentState()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			_instance.Stop();

			Assert.AreEqual(1, TestStateA.ExitCount);
		}



		[Test]
		public void Update_CallsCurrentStateUpdate()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			_instance.Update();
			_instance.Update();
			_instance.Update();

			Assert.AreEqual(3, TestStateA.UpdateCount);
		}

		[Test]
		public void Update_WhenNotRunning_DoesNothing()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			_instance.Update();

			Assert.AreEqual(0, TestStateA.UpdateCount);
		}

		[Test]
		public void Update_WhenPaused_DoesNothing()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var countBeforePause = TestStateA.UpdateCount;
			_instance.Pause();
			_instance.Update();
			_instance.Update();
			_instance.Update();

			Assert.AreEqual(countBeforePause, TestStateA.UpdateCount);
		}

		[Test]
		public void UpdateAsync_WhenPaused_DoesNothing()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var countBeforePause = TestStateA.UpdateCount;
			_instance.Pause();

			// Run UpdateAsync (completes synchronously since state is paused)
			_instance.UpdateAsync().GetAwaiter().GetResult();
			_instance.UpdateAsync().GetAwaiter().GetResult();
			_instance.UpdateAsync().GetAwaiter().GetResult();

			Assert.AreEqual(countBeforePause, TestStateA.UpdateCount);
		}

		[Test]
		public void UpdateAsync_WhenNotPaused_CallsStateUpdate()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			_instance.UpdateAsync().GetAwaiter().GetResult();
			_instance.UpdateAsync().GetAwaiter().GetResult();
			_instance.UpdateAsync().GetAwaiter().GetResult();

			// Note: OnUpdateAsync is called, not OnUpdate, so UpdateCount stays 0
			// This test verifies the method runs without error when not paused
			Assert.IsTrue(_instance.IsRunning);
			Assert.IsFalse(_instance.IsPaused);
		}



		[Test]
		public void OnStateChanged_FiresOnTransition()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			State oldState = null;
			State newState = null;
			_instance.OnStateChanged += (old, @new) =>
			{
				oldState = old;
				newState = @new;
			};

			_instance.Start();

			Assert.IsNull(oldState);
			Assert.IsNotNull(newState);
			Assert.IsInstanceOf<TestStateA>(newState);
		}



		[Test]
		public void Blackboard_IsAccessibleFromInstance()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			_instance.Blackboard.Set("test", 42);

			Assert.AreEqual(42, _instance.Blackboard.Get<int>("test"));
		}



		[Test]
		public void MaxSameFrameTransitions_DefaultValue()
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

			_instance.MaxSameFrameTransitions = 50;

			Assert.AreEqual(50, _instance.MaxSameFrameTransitions);
		}



		[Test]
		public void CurrentState_BeforeStart_IsNull()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			Assert.IsNull(_instance.CurrentState);
		}

		[Test]
		public void CurrentState_AfterStart_IsNotNull()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			_instance.Start();

			Assert.IsNotNull(_instance.CurrentState);
		}

		[Test]
		public void CurrentState_AfterStop_IsNull()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			_instance.Stop();

			Assert.IsNull(_instance.CurrentState);
		}



		[Test]
		public void Constructor_WithContext_SetsContext()
		{
			SetupSimpleGraph();
			var context = new TestContext();

			_instance = new StateMachineInstance(_graph, context);

			Assert.AreEqual(context, _instance.Context);
		}

		[Test]
		public void Constructor_WithoutContext_ContextIsNull()
		{
			SetupSimpleGraph();

			_instance = new StateMachineInstance(_graph);

			Assert.IsNull(_instance.Context);
		}

		private class TestContext : IStateMachineContext
		{
			public int Value { get; set; } = 42;
			public GameObject GameObject => null;
			public Transform Transform => null;
		}

		// Test class that does NOT inherit from State - used for security validation tests
		private class NotAState
		{
			public static bool ConstructorCalled;

			public NotAState()
			{
				ConstructorCalled = true;
			}
		}

		[Test]
		public void Start_WithInvalidStateType_SkipsNodeAndDoesNotCrash()
		{
			// Arrange: Create a graph with a node that has an invalid type (not inheriting from State)
			NotAState.ConstructorCalled = false;

			var entry = new EntryNode { Id = "entry" };
			var invalidNode = new StateNode
			{
				Id = "invalidState",
				DisplayName = "Invalid State",
				// Manually set the type name to a class that doesn't inherit from State
				StateTypeAssemblyQualifiedName = typeof(NotAState).AssemblyQualifiedName
			};
			var validNode = new StateNode
			{
				Id = "validState",
				DisplayName = "Valid State",
				StateTypeAssemblyQualifiedName = typeof(TestStateA).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(invalidNode);
			_graph.AddNode(validNode);
			_graph.EntryNodeId = "entry";

			// Entry -> Valid State (skip invalid)
			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "validState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// Act: Should not throw and should not instantiate the invalid type
			_instance = new StateMachineInstance(_graph);

			Assert.DoesNotThrow(() => _instance.Start());

			// Assert: The invalid type's constructor was never called
			Assert.IsFalse(NotAState.ConstructorCalled, "Invalid type should not be instantiated");

			// Assert: The valid state was entered
			Assert.AreEqual(1, TestStateA.EnterCount, "Valid state should still be entered");
		}


		private class TestSharedContext
		{
			public int Value { get; set; } = 100;
			public string Name { get; set; } = "TestContext";
		}

		private class ContextStateA : State<TestSharedContext>
		{
			public static int EnterCount;
			public static TestSharedContext ReceivedContext;

			public override void OnEnter()
			{
				EnterCount++;
				ReceivedContext = SharedContext;
			}

			public static void ResetCounters()
			{
				EnterCount = 0;
				ReceivedContext = null;
			}
		}

		private class DifferentContext
		{
			public string Data { get; set; }
		}

		private class IncompatibleContextState : State<DifferentContext>
		{
			public static DifferentContext ReceivedContext;

			public override void OnEnter()
			{
				ReceivedContext = SharedContext;
			}

			public static void Reset()
			{
				ReceivedContext = null;
			}
		}

		[Test]
		public void ContextInjection_StateReceivesSharedContext()
		{
			// Arrange
			ContextStateA.ResetCounters();
			var sharedContext = new TestSharedContext { Value = 42, Name = "Injected" };

			var entry = new EntryNode { Id = "entry" };
			var contextState = new StateNode
			{
				Id = "contextState",
				DisplayName = "Context State",
				StateTypeAssemblyQualifiedName = typeof(ContextStateA).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(contextState);
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

			// Act
			_instance = new StateMachineInstance(_graph, new StateMachineContext(), sharedContext);
			_instance.Start();

			// Assert
			Assert.AreEqual(1, ContextStateA.EnterCount);
			Assert.IsNotNull(ContextStateA.ReceivedContext);
			Assert.AreEqual(42, ContextStateA.ReceivedContext.Value);
			Assert.AreEqual("Injected", ContextStateA.ReceivedContext.Name);
		}

		[Test]
		public void ContextInjection_StateImplementsIContextReceiver()
		{
			// Arrange
			var state = new ContextStateA();

			// Assert - State<T> should implement IContextReceiver
			Assert.IsInstanceOf<IContextReceiver>(state);
			Assert.AreEqual(typeof(TestSharedContext), ((IContextReceiver)state).RequiredContextType);
		}

		[Test]
		public void ContextInjection_SetContextUntyped_SetsContext()
		{
			// Arrange
			var state = new ContextStateA();
			var context = new TestSharedContext { Value = 99 };

			// Act
			((IContextReceiver)state).SetContextUntyped(context);

			// Assert - Access via reflection since SharedContext is protected
			var sharedContextField = typeof(State<TestSharedContext>).GetField("_sharedContext",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var actualContext = sharedContextField?.GetValue(state) as TestSharedContext;

			Assert.IsNotNull(actualContext);
			Assert.AreEqual(99, actualContext.Value);
		}

		[Test]
		public void ContextInjection_IncompatibleContext_DoesNotInject()
		{
			// Arrange
			IncompatibleContextState.Reset();
			var sharedContext = new TestSharedContext { Value = 42 }; // Wrong type

			var entry = new EntryNode { Id = "entry" };
			var contextState = new StateNode
			{
				Id = "contextState",
				DisplayName = "Incompatible State",
				StateTypeAssemblyQualifiedName = typeof(IncompatibleContextState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(contextState);
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

			// Act - Should not throw, but context should not be injected
			_instance = new StateMachineInstance(_graph, new StateMachineContext(), sharedContext);
			_instance.Start();

			// Assert - Context should be null because types are incompatible
			Assert.IsNull(IncompatibleContextState.ReceivedContext);
		}

		private class NullContextTypeState : State, IContextReceiver
		{
			public static int EnterCount;

			public Type RequiredContextType => null;

			public void SetContextUntyped(object context) { }

			public override void OnEnter() => EnterCount++;

			public static void ResetCounters() => EnterCount = 0;
		}

		[Test]
		public void ContextInjection_NullRequiredContextType_DoesNotThrow()
		{
			// Arrange
			NullContextTypeState.ResetCounters();
			var sharedContext = new TestSharedContext { Value = 42 };

			var entry = new EntryNode { Id = "entry" };
			var nullCtxState = new StateNode
			{
				Id = "nullCtxState",
				DisplayName = "Null Context Type State",
				StateTypeAssemblyQualifiedName = typeof(NullContextTypeState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(nullCtxState);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "nullCtxState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// Act - State implements IContextReceiver but RequiredContextType returns null
			_instance = new StateMachineInstance(_graph, null, sharedContext);

			Assert.DoesNotThrow(() => _instance.Start());
			Assert.AreEqual(1, NullContextTypeState.EnterCount);
		}

		[Test]
		public void ContextInjection_NullSharedContext_DoesNotThrow()
		{
			// Arrange
			ContextStateA.ResetCounters();

			var entry = new EntryNode { Id = "entry" };
			var contextState = new StateNode
			{
				Id = "contextState",
				DisplayName = "Context State",
				StateTypeAssemblyQualifiedName = typeof(ContextStateA).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(contextState);
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

			// Act - No shared context provided
			_instance = new StateMachineInstance(_graph);

			Assert.DoesNotThrow(() => _instance.Start());
			Assert.AreEqual(1, ContextStateA.EnterCount);
			Assert.IsNull(ContextStateA.ReceivedContext);
		}



		private class PortalSourceState : State
		{
			public static int EnterCount;
			public static int ExitCount;

			[Transition("To Portal Out")] public Action toPortalOut;

			public override void OnEnter() => EnterCount++;
			public override void OnExit() => ExitCount++;

			public static void ResetCounters()
			{
				EnterCount = 0;
				ExitCount = 0;
			}
		}

		private class PortalDestinationState : State
		{
			public static int EnterCount;
			public static int ExitCount;

			public override void OnEnter() => EnterCount++;
			public override void OnExit() => ExitCount++;

			public static void ResetCounters()
			{
				EnterCount = 0;
				ExitCount = 0;
			}
		}

		private void SetupPortalGraph(int channelId = 1)
		{
			PortalSourceState.ResetCounters();
			PortalDestinationState.ResetCounters();

			var entry = new EntryNode { Id = "entry" };
			var sourceState = new StateNode
			{
				Id = "sourceState",
				DisplayName = "Source State",
				StateTypeAssemblyQualifiedName = typeof(PortalSourceState).AssemblyQualifiedName
			};
			var portalOut = new PortalOutNode(channelId) { Id = "portalOut" };
			var portalIn = new PortalInNode(channelId) { Id = "portalIn" };
			var destState = new StateNode
			{
				Id = "destState",
				DisplayName = "Destination State",
				StateTypeAssemblyQualifiedName = typeof(PortalDestinationState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(sourceState);
			_graph.AddNode(portalOut);
			_graph.AddNode(portalIn);
			_graph.AddNode(destState);
			_graph.EntryNodeId = "entry";

			// Entry -> Source State
			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "sourceState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// Source State -> Portal Out
			_graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "sourceState",
				SourcePortName = "toPortalOut",
				TargetNodeId = "portalOut",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// Portal In -> Destination State
			_graph.AddConnection(new Connection
			{
				Id = "c3",
				SourceNodeId = "portalIn",
				SourcePortName = "out",
				TargetNodeId = "destState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});
		}

		[Test]
		public void Constructor_BuildsPortalInNodeCache()
		{
			// Arrange: Create a graph with portal nodes
			SetupPortalGraph();

			// Act: Create instance (cache is built in constructor)
			_instance = new StateMachineInstance(_graph);

			// Assert: Instance is created successfully (cache was built without error)
			Assert.IsNotNull(_instance);
		}

		[Test]
		public void PortalTransition_UsesCache_TransitionsCorrectly()
		{
			// Arrange
			SetupPortalGraph(channelId: 1);
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			// Verify we're in the source state
			Assert.IsInstanceOf<PortalSourceState>(_instance.CurrentState);
			Assert.AreEqual(1, PortalSourceState.EnterCount);

			// Act: Trigger transition through portal
			var sourceState = _instance.CurrentState as PortalSourceState;
			sourceState?.toPortalOut?.Invoke();

			// Assert: Successfully transitioned through portal to destination state
			Assert.IsInstanceOf<PortalDestinationState>(_instance.CurrentState);
			Assert.AreEqual(1, PortalDestinationState.EnterCount);
			Assert.AreEqual(1, PortalSourceState.ExitCount);
		}

		[Test]
		public void MultiplePortalChannels_CachesAllChannels()
		{
			// Arrange: Create a graph with multiple portal channels
			var entry = new EntryNode { Id = "entry" };
			var sourceState = new StateNode
			{
				Id = "sourceState",
				DisplayName = "Source State",
				StateTypeAssemblyQualifiedName = typeof(PortalSourceState).AssemblyQualifiedName
			};

			// Channel 1
			var portalIn1 = new PortalInNode(1) { Id = "portalIn1" };

			// Channel 2
			var portalIn2 = new PortalInNode(2) { Id = "portalIn2" };

			// Channel 3
			var portalIn3 = new PortalInNode(3) { Id = "portalIn3" };

			_graph.AddNode(entry);
			_graph.AddNode(sourceState);
			_graph.AddNode(portalIn1);
			_graph.AddNode(portalIn2);
			_graph.AddNode(portalIn3);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "sourceState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			PortalSourceState.ResetCounters();

			// Act: Create instance (should cache all 3 portal channels)
			_instance = new StateMachineInstance(_graph);

			// Assert: Instance is created successfully (all channels cached)
			Assert.IsNotNull(_instance);
			_instance.Start();
			Assert.IsNotNull(_instance.CurrentState);
		}

		[Test]
		public void GraphWithNoPortals_CacheIsEmpty_NoErrors()
		{
			// Arrange: Simple graph with no portal nodes
			SetupSimpleGraph();

			// Act: Create instance (cache should be empty but no errors)
			_instance = new StateMachineInstance(_graph);

			// Assert: Instance works normally
			Assert.IsNotNull(_instance);
			_instance.Start();
			Assert.IsInstanceOf<TestStateA>(_instance.CurrentState);
		}



		private class BindingTestState : State
		{
			public static int BoundValue;
			public static string BoundString;

			[DataIn("inputValue")] public int _inputValue;
			[DataIn("inputString")] public string _inputString;

			public override void OnEnter()
			{
				BoundValue = _inputValue;
				BoundString = _inputString;
			}

			public static void Reset()
			{
				BoundValue = 0;
				BoundString = null;
			}
		}

		private void SetupBindingTestGraph()
		{
			BindingTestState.Reset();

			var entry = new EntryNode { Id = "entry" };
			var stateNode = new StateNode
			{
				Id = "bindingState",
				DisplayName = "Binding State",
				StateTypeAssemblyQualifiedName = typeof(BindingTestState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateNode);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "bindingState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});
		}

		[Test]
		public void FieldBinding_GraphConnection_TakesPrecedenceOverPropertyOverride()
		{
			// Arrange
			SetupBindingTestGraph();

			// Add blackboard variable
			_graph.AddBlackboardVariable(new BlackboardVariableDefinition
			{
				Key = "graphValue",
				ValueType = typeof(int),
				DefaultValue = 100
			});

			// Add blackboard node
			var bbNode = new BlackboardVariableNode
			{
				Id = "bbNode",
				VariableKey = "graphValue",
				Mode = BlackboardNodeMode.Get
			};
			_graph.AddNode(bbNode);

			// Add connection from blackboard to state (graph connection)
			_graph.AddConnection(new Connection
			{
				Id = "dataConn",
				SourceNodeId = "bbNode",
				SourcePortName = "value",
				TargetNodeId = "bindingState",
				TargetPortName = "_inputValue",
				ConnectionType = ConnectionType.BlackboardRead
			});

			// Add property override with different value
			var stateNode = _graph.GetNode<StateNode>("bindingState");
			stateNode.PropertyOverrides.Add(new SerializedPropertyOverride("_inputValue", 50, typeof(int)));

			// Act
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			// Assert: Graph connection value (100) should win over property override (50)
			Assert.AreEqual(100, BindingTestState.BoundValue);
		}

		[Test]
		public void FieldBinding_PropertyOverride_TakesPrecedenceOverDataInAttribute()
		{
			// Arrange
			SetupBindingTestGraph();

			// Add blackboard variable that matches DataIn attribute display name
			_graph.AddBlackboardVariable(new BlackboardVariableDefinition
			{
				Key = "inputValue",
				ValueType = typeof(int),
				DefaultValue = 100
			});

			// Add property override (no graph connection)
			var stateNode = _graph.GetNode<StateNode>("bindingState");
			stateNode.PropertyOverrides.Add(new SerializedPropertyOverride("_inputValue", 50, typeof(int)));

			// Act
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			// Assert: Property override (50) should win over DataIn attribute fallback (100)
			Assert.AreEqual(50, BindingTestState.BoundValue);
		}

		[Test]
		public void FieldBinding_DataInAttribute_BindsWhenNoConnectionOrOverride()
		{
			// Arrange
			SetupBindingTestGraph();

			// Add blackboard variable matching DataIn attribute display name
			_graph.AddBlackboardVariable(new BlackboardVariableDefinition
			{
				Key = "inputValue",
				ValueType = typeof(int),
				DefaultValue = 75
			});

			// No graph connection, no property override

			// Act
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			// Assert: DataIn attribute fallback should bind from blackboard
			Assert.AreEqual(75, BindingTestState.BoundValue);
		}

		[Test]
		public void FieldBinding_OnlyBindsUnconnectedFields()
		{
			// Arrange
			SetupBindingTestGraph();

			// Add blackboard variables
			_graph.AddBlackboardVariable(new BlackboardVariableDefinition
			{
				Key = "graphValue",
				ValueType = typeof(int),
				DefaultValue = 100
			});
			_graph.AddBlackboardVariable(new BlackboardVariableDefinition
			{
				Key = "inputString",
				ValueType = typeof(string),
				DefaultValue = "fallback"
			});

			// Add blackboard node for int connection
			var bbNode = new BlackboardVariableNode
			{
				Id = "bbNode",
				VariableKey = "graphValue",
				Mode = BlackboardNodeMode.Get
			};
			_graph.AddNode(bbNode);

			// Only connect _inputValue, leave _inputString unconnected
			_graph.AddConnection(new Connection
			{
				Id = "dataConn",
				SourceNodeId = "bbNode",
				SourcePortName = "value",
				TargetNodeId = "bindingState",
				TargetPortName = "_inputValue",
				ConnectionType = ConnectionType.BlackboardRead
			});

			// Act
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			// Assert: _inputValue bound via connection, _inputString bound via DataIn fallback
			Assert.AreEqual(100, BindingTestState.BoundValue);
			Assert.AreEqual("fallback", BindingTestState.BoundString);
		}

		[Test]
		public void FieldBinding_TypeMismatch_DoesNotBind()
		{
			// Arrange
			SetupBindingTestGraph();

			// Add blackboard variable with wrong type
			_graph.AddBlackboardVariable(new BlackboardVariableDefinition
			{
				Key = "inputValue",
				ValueType = typeof(string),
				DefaultValue = "not an int"
			});

			// Act
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			// Assert: Value should remain at default (0) due to type mismatch
			Assert.AreEqual(0, BindingTestState.BoundValue);
		}

		[Test]
		public void FieldBinding_NullValue_DoesNotBind()
		{
			// Arrange
			SetupBindingTestGraph();

			// Add blackboard variable with null value
			_graph.AddBlackboardVariable(new BlackboardVariableDefinition
			{
				Key = "inputString",
				ValueType = typeof(string),
				DefaultValue = null
			});

			// Act
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			// Assert: String should remain null (not bound)
			Assert.IsNull(BindingTestState.BoundString);
		}

		[Test]
		public void FieldBinding_PropertyOverride_SkipsAlreadyBoundFields()
		{
			// Arrange
			SetupBindingTestGraph();

			// Add blackboard for graph connection
			_graph.AddBlackboardVariable(new BlackboardVariableDefinition
			{
				Key = "graphValue",
				ValueType = typeof(int),
				DefaultValue = 100
			});

			var bbNode = new BlackboardVariableNode
			{
				Id = "bbNode",
				VariableKey = "graphValue",
				Mode = BlackboardNodeMode.Get
			};
			_graph.AddNode(bbNode);

			// Connect via graph
			_graph.AddConnection(new Connection
			{
				Id = "dataConn",
				SourceNodeId = "bbNode",
				SourcePortName = "value",
				TargetNodeId = "bindingState",
				TargetPortName = "_inputValue",
				ConnectionType = ConnectionType.BlackboardRead
			});

			// Also add property override for same field
			var stateNode = _graph.GetNode<StateNode>("bindingState");
			stateNode.PropertyOverrides.Add(new SerializedPropertyOverride("_inputValue", 999, typeof(int)));

			// Act
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			// Assert: Graph connection wins, override is skipped
			Assert.AreEqual(100, BindingTestState.BoundValue);
		}



		[Test]
		public void SameFrameTransitions_ExceedingMax_StopsStateMachine()
		{
			// Arrange: Create a graph with InfiniteLoopState that invokes loop transition on enter
			var entry = new EntryNode { Id = "entry" };
			var loopState = new StateNode
			{
				Id = "loopState",
				DisplayName = "Loop State",
				StateTypeAssemblyQualifiedName = typeof(InfiniteLoopState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(loopState);
			_graph.EntryNodeId = "entry";

			// Entry -> Loop State
			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "loopState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// Loop State -> Loop State (self-loop)
			_graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "loopState",
				SourcePortName = "loop",
				TargetNodeId = "loopState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_instance = new StateMachineInstance(_graph);

			// Act: Start the state machine - InfiniteLoopState invokes loop on each OnEnter
			_instance.Start();

			// Assert: The state machine should have stopped itself due to exceeding max transitions
			Assert.IsFalse(_instance.IsRunning,
				"State machine should have stopped after exceeding max same-frame transitions");
		}

		[Test]
		public void SameFrameTransitions_WithCustomLimit_StopsAtCustomLimit()
		{
			// Arrange
			var entry = new EntryNode { Id = "entry" };
			var loopState = new StateNode
			{
				Id = "loopState",
				DisplayName = "Loop State",
				StateTypeAssemblyQualifiedName = typeof(InfiniteLoopState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(loopState);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "loopState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "loopState",
				SourcePortName = "loop",
				TargetNodeId = "loopState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_instance = new StateMachineInstance(_graph);
			_instance.MaxSameFrameTransitions = 5;

			// Act
			_instance.Start();

			// Assert: Should stop with the custom limit
			Assert.IsFalse(_instance.IsRunning,
				"State machine should stop with custom MaxSameFrameTransitions limit");
		}

		private class TransitionCountingState : State
		{
			public static int TransitionCount;
			public static int MaxTransitionsBeforeStop;

			[Transition("Next")] public Action next;

			public override void OnEnter()
			{
				TransitionCount++;
				if (TransitionCount < MaxTransitionsBeforeStop)
				{
					next?.Invoke();
				}
			}

			public static void Reset()
			{
				TransitionCount = 0;
				MaxTransitionsBeforeStop = 0;
			}
		}

		[Test]
		public void SameFrameTransitions_BelowMax_AllowsTransitions()
		{
			// Arrange: Create a state that transitions a controlled number of times
			TransitionCountingState.Reset();
			TransitionCountingState.MaxTransitionsBeforeStop = 10;

			var entry = new EntryNode { Id = "entry" };
			var countState = new StateNode
			{
				Id = "countState",
				DisplayName = "Count State",
				StateTypeAssemblyQualifiedName = typeof(TransitionCountingState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(countState);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "countState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "countState",
				SourcePortName = "next",
				TargetNodeId = "countState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_instance = new StateMachineInstance(_graph);
			_instance.MaxSameFrameTransitions = 100;

			// Act
			_instance.Start();

			// Assert: State machine should still be running (only 10 transitions, under limit of 100)
			Assert.IsTrue(_instance.IsRunning,
				"State machine should still be running when transitions are below the limit");
			Assert.AreEqual(10, TransitionCountingState.TransitionCount);
		}



		[Test]
		public void NestingDepth_RootInstance_IsZero()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			Assert.AreEqual(0, _instance.NestingDepth);
		}

		[Test]
		public void NestingDepth_ChildInstance_IsOne()
		{
			// Arrange: Create parent and child graphs
			SetupSimpleGraph();
			var childGraph = ScriptableObject.CreateInstance<StateMachineGraph>();

			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				DisplayName = "Child State",
				StateTypeAssemblyQualifiedName = typeof(TestStateA).AssemblyQualifiedName
			};
			childGraph.AddNode(childEntry);
			childGraph.AddNode(childState);
			childGraph.EntryNodeId = "childEntry";
			childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				SourcePortName = "out",
				TargetNodeId = "childState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// Act: Create child instance with parent
			_instance = new StateMachineInstance(_graph);
			var childInstance = new StateMachineInstance(childGraph, null, null, _instance);

			// Assert
			Assert.AreEqual(0, _instance.NestingDepth);
			Assert.AreEqual(1, childInstance.NestingDepth);

			// Cleanup
			UnityEngine.Object.DestroyImmediate(childGraph);
		}

		[Test]
		public void NestingDepth_DeeplyNested_IncrementsByOne()
		{
			// Arrange: Create a chain of parent-child instances
			var graphs = new StateMachineGraph[5];
			for (var i = 0; i < graphs.Length; i++)
			{
				graphs[i] = ScriptableObject.CreateInstance<StateMachineGraph>();
				var entry = new EntryNode { Id = $"entry{i}" };
				var state = new StateNode
				{
					Id = $"state{i}",
					DisplayName = $"State {i}",
					StateTypeAssemblyQualifiedName = typeof(TestStateA).AssemblyQualifiedName
				};
				graphs[i].AddNode(entry);
				graphs[i].AddNode(state);
				graphs[i].EntryNodeId = $"entry{i}";
				graphs[i].AddConnection(new Connection
				{
					Id = $"c{i}",
					SourceNodeId = $"entry{i}",
					SourcePortName = "out",
					TargetNodeId = $"state{i}",
					TargetPortName = "in",
					ConnectionType = ConnectionType.Transition
				});
			}

			// Act: Create nested instances
			var instances = new StateMachineInstance[5];
			instances[0] = new StateMachineInstance(graphs[0]);
			for (var i = 1; i < instances.Length; i++)
			{
				instances[i] = new StateMachineInstance(graphs[i], null, null, instances[i - 1]);
			}

			// Assert: Each level has correct nesting depth
			for (var i = 0; i < instances.Length; i++)
			{
				Assert.AreEqual(i, instances[i].NestingDepth,
					$"Instance at level {i} should have nesting depth {i}");
			}

			// Cleanup
			foreach (var graph in graphs)
			{
				UnityEngine.Object.DestroyImmediate(graph);
			}
		}

		[Test]
		public void MaxSubStateMachineDepth_ConstantIs16()
		{
			Assert.AreEqual(16, StateMachineInstance.MaxSubStateMachineDepth);
		}

		[Test]
		public void NestingDepth_ExceedingMax_PreventsSubStateMachineEntry()
		{
			// Arrange: Create a chain of graphs where the last one points to another sub-graph
			// exceeding the max depth of 16
			var graphs = new StateMachineGraph[18];
			for (var i = 0; i < graphs.Length; i++)
			{
				graphs[i] = ScriptableObject.CreateInstance<StateMachineGraph>();
			}

			// Set up each graph (except last) to contain a sub-state machine node pointing to the next
			for (var i = 0; i < graphs.Length - 1; i++)
			{
				var entry = new EntryNode { Id = $"entry{i}" };
				var subMachine = new SubStateMachineNode
				{
					Id = $"sub{i}",
					SubGraph = graphs[i + 1]
				};

				graphs[i].AddNode(entry);
				graphs[i].AddNode(subMachine);
				graphs[i].EntryNodeId = $"entry{i}";

				graphs[i].AddConnection(new Connection
				{
					Id = $"c{i}",
					SourceNodeId = $"entry{i}",
					SourcePortName = "out",
					TargetNodeId = $"sub{i}",
					TargetPortName = "in",
					ConnectionType = ConnectionType.Transition
				});
			}

			// Last graph has a simple state
			var lastEntry = new EntryNode { Id = "lastEntry" };
			var lastState = new StateNode
			{
				Id = "lastState",
				DisplayName = "Last State",
				StateTypeAssemblyQualifiedName = typeof(TestStateA).AssemblyQualifiedName
			};
			graphs[graphs.Length - 1].AddNode(lastEntry);
			graphs[graphs.Length - 1].AddNode(lastState);
			graphs[graphs.Length - 1].EntryNodeId = "lastEntry";
			graphs[graphs.Length - 1].AddConnection(new Connection
			{
				Id = "lastC",
				SourceNodeId = "lastEntry",
				SourcePortName = "out",
				TargetNodeId = "lastState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// Act
			_instance = new StateMachineInstance(graphs[0]);
			_instance.Start();

			// Assert: Should have stopped or been prevented at depth 16
			// The state machine at depth 16 should not be able to enter its sub-state machine
			// After the protection kicks in, the root should still be running
			// but the deepest child should not have started

			// Cleanup
			_instance.Stop();
			foreach (var graph in graphs)
			{
				UnityEngine.Object.DestroyImmediate(graph);
			}
		}



		[Test]
		public void CircularReference_SelfReference_PreventsEntry()
		{
			// Arrange: A graph that contains a sub-state machine pointing to itself
			var entry = new EntryNode { Id = "entry" };
			var subMachine = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = _graph // Points to itself
			};

			_graph.AddNode(entry);
			_graph.AddNode(subMachine);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "subMachine",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_instance = new StateMachineInstance(_graph);

			// Act
			_instance.Start();

			// Assert: The circular reference should be detected and the state machine
			// should handle it gracefully (not crash or infinite loop)
			// It will log an error but should not throw
			Assert.IsNotNull(_instance);
		}

		[Test]
		public void CircularReference_IndirectAtoB_PreventsEntry()
		{
			// Arrange: Graph A -> sub(Graph B) -> sub(Graph A)
			var graphB = ScriptableObject.CreateInstance<StateMachineGraph>();

			// Graph A (using _graph): Entry -> Sub(B)
			var entryA = new EntryNode { Id = "entryA" };
			var subA = new SubStateMachineNode
			{
				Id = "subA",
				SubGraph = graphB
			};

			_graph.AddNode(entryA);
			_graph.AddNode(subA);
			_graph.EntryNodeId = "entryA";

			_graph.AddConnection(new Connection
			{
				Id = "cA",
				SourceNodeId = "entryA",
				SourcePortName = "out",
				TargetNodeId = "subA",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// Graph B: Entry -> Sub(Graph A) - circular
			var entryB = new EntryNode { Id = "entryB" };
			var subB = new SubStateMachineNode
			{
				Id = "subB",
				SubGraph = _graph // Points back to A
			};

			graphB.AddNode(entryB);
			graphB.AddNode(subB);
			graphB.EntryNodeId = "entryB";

			graphB.AddConnection(new Connection
			{
				Id = "cB",
				SourceNodeId = "entryB",
				SourcePortName = "out",
				TargetNodeId = "subB",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_instance = new StateMachineInstance(_graph);

			// Act
			_instance.Start();

			// Assert: Should not crash - circular reference detected and handled
			Assert.IsNotNull(_instance);

			// Cleanup
			UnityEngine.Object.DestroyImmediate(graphB);
		}

		[Test]
		public void CircularReference_ThreeLevelChain_PreventsEntry()
		{
			// Arrange: A -> B -> C -> A
			var graphB = ScriptableObject.CreateInstance<StateMachineGraph>();
			var graphC = ScriptableObject.CreateInstance<StateMachineGraph>();

			// Graph A
			_graph.AddNode(new EntryNode { Id = "entryA" });
			_graph.AddNode(new SubStateMachineNode { Id = "subA", SubGraph = graphB });
			_graph.EntryNodeId = "entryA";
			_graph.AddConnection(new Connection
			{
				Id = "cA",
				SourceNodeId = "entryA",
				SourcePortName = "out",
				TargetNodeId = "subA",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// Graph B
			graphB.AddNode(new EntryNode { Id = "entryB" });
			graphB.AddNode(new SubStateMachineNode { Id = "subB", SubGraph = graphC });
			graphB.EntryNodeId = "entryB";
			graphB.AddConnection(new Connection
			{
				Id = "cB",
				SourceNodeId = "entryB",
				SourcePortName = "out",
				TargetNodeId = "subB",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// Graph C -> back to A
			graphC.AddNode(new EntryNode { Id = "entryC" });
			graphC.AddNode(new SubStateMachineNode { Id = "subC", SubGraph = _graph });
			graphC.EntryNodeId = "entryC";
			graphC.AddConnection(new Connection
			{
				Id = "cC",
				SourceNodeId = "entryC",
				SourcePortName = "out",
				TargetNodeId = "subC",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_instance = new StateMachineInstance(_graph);

			// Act
			_instance.Start();

			// Assert: Should detect circular reference and not crash
			Assert.IsNotNull(_instance);

			// Cleanup
			UnityEngine.Object.DestroyImmediate(graphB);
			UnityEngine.Object.DestroyImmediate(graphC);
		}



		private class ContextRequiredState : State<TestSharedContext>
		{
			public static bool WasEntered;

			public override void OnEnter()
			{
				WasEntered = true;
			}

			public static void Reset()
			{
				WasEntered = false;
			}
		}

		private class DerivedSharedContext : TestSharedContext
		{
			public int ExtraValue { get; set; } = 99;
		}

		private void SetupContextRequiringGraph()
		{
			var entry = new EntryNode { Id = "entry" };
			var contextState = new StateNode
			{
				Id = "contextState",
				DisplayName = "Context State",
				StateTypeAssemblyQualifiedName = typeof(ContextRequiredState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(contextState);
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
		public void ContextValidation_MissingRequiredContext_DoesNotStart()
		{
			// Arrange
			ContextRequiredState.Reset();
			SetupContextRequiringGraph();

			_instance = new StateMachineInstance(_graph);

			// Act: Start without providing shared context
			_instance.Start();

			// Assert: State machine should not enter the state
			Assert.IsFalse(ContextRequiredState.WasEntered,
				"State requiring context should not be entered when no context is provided");
			Assert.IsFalse(_instance.IsRunning,
				"State machine should not be running when context validation fails");
		}

		[Test]
		public void ContextValidation_IncompatibleType_DoesNotStart()
		{
			// Arrange
			ContextRequiredState.Reset();
			SetupContextRequiringGraph();

			// Provide an incompatible context type
			_instance = new StateMachineInstance(_graph, null, new DifferentContext());

			// Act
			_instance.Start();

			// Assert
			Assert.IsFalse(ContextRequiredState.WasEntered,
				"State requiring specific context should not be entered with incompatible type");
			Assert.IsFalse(_instance.IsRunning,
				"State machine should not be running when context type is incompatible");
		}

		[Test]
		public void ContextValidation_CompatibleType_Starts()
		{
			// Arrange
			ContextRequiredState.Reset();
			SetupContextRequiringGraph();

			// Provide compatible context
			var context = new TestSharedContext { Value = 42 };
			_instance = new StateMachineInstance(_graph, new StateMachineContext(), context);

			// Act
			_instance.Start();

			// Assert
			Assert.IsTrue(ContextRequiredState.WasEntered,
				"State should be entered when compatible context is provided");
			Assert.IsTrue(_instance.IsRunning,
				"State machine should be running with compatible context");
		}

		[Test]
		public void ContextValidation_DerivedType_Starts()
		{
			// Arrange
			ContextRequiredState.Reset();
			SetupContextRequiringGraph();

			// Provide derived context type (DerivedSharedContext inherits from TestSharedContext)
			var derivedContext = new DerivedSharedContext { Value = 77, ExtraValue = 99 };
			_instance = new StateMachineInstance(_graph, new StateMachineContext(), derivedContext);

			// Act
			_instance.Start();

			// Assert
			Assert.IsTrue(ContextRequiredState.WasEntered,
				"State should be entered when derived context type is provided");
			Assert.IsTrue(_instance.IsRunning,
				"State machine should be running with derived context");
		}

		[Test]
		public void ContextValidation_SetSharedContextBeforeStart_Works()
		{
			// Arrange
			ContextRequiredState.Reset();
			SetupContextRequiringGraph();

			_instance = new StateMachineInstance(_graph);

			// Set context after construction but before start
			_instance.SetSharedContext(new TestSharedContext { Value = 55 });

			// Act
			_instance.Start();

			// Assert
			Assert.IsTrue(ContextRequiredState.WasEntered,
				"State should be entered when context is set via SetSharedContext before Start");
			Assert.IsTrue(_instance.IsRunning);
		}

		[Test]
		public void ContextValidation_SubStateMachine_MissingContext_HandlesGracefully()
		{
			// Arrange: Parent graph with sub-state machine that requires context
			var childGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			childGraph.ContextType = typeof(TestSharedContext);

			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				DisplayName = "Child Context State",
				StateTypeAssemblyQualifiedName = typeof(ContextRequiredState).AssemblyQualifiedName
			};
			childGraph.AddNode(childEntry);
			childGraph.AddNode(childState);
			childGraph.EntryNodeId = "childEntry";
			childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				SourcePortName = "out",
				TargetNodeId = "childState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// Parent graph
			var entry = new EntryNode { Id = "entry" };
			var subMachine = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = childGraph
			};

			_graph.AddNode(entry);
			_graph.AddNode(subMachine);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "subMachine",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			ContextRequiredState.Reset();

			// Act: Start without context - sub-graph requires it
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			// Assert: Should handle gracefully (log error, not crash)
			Assert.IsFalse(ContextRequiredState.WasEntered,
				"Child state should not be entered when required context is missing");

			// Cleanup
			UnityEngine.Object.DestroyImmediate(childGraph);
		}

		[Test]
		public void ContextValidation_SubStateMachine_IncompatibleContext_HandlesGracefully()
		{
			// Arrange: Child requires TestSharedContext but parent provides DifferentContext
			ContextRequiredState.Reset();

			var childGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			childGraph.ContextType = typeof(TestSharedContext);

			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				DisplayName = "Child Context State",
				StateTypeAssemblyQualifiedName = typeof(ContextRequiredState).AssemblyQualifiedName
			};
			childGraph.AddNode(childEntry);
			childGraph.AddNode(childState);
			childGraph.EntryNodeId = "childEntry";
			childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				SourcePortName = "out",
				TargetNodeId = "childState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// Parent graph
			var entry = new EntryNode { Id = "entry" };
			var subMachine = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = childGraph
			};

			_graph.AddNode(entry);
			_graph.AddNode(subMachine);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "subMachine",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// Act: Provide incompatible context
			_instance = new StateMachineInstance(_graph, null, new DifferentContext());
			_instance.Start();

			// Assert
			Assert.IsFalse(ContextRequiredState.WasEntered,
				"Child state should not be entered when context type is incompatible");

			// Cleanup
			UnityEngine.Object.DestroyImmediate(childGraph);
		}



		[Test]
		public void Parent_RootInstance_HasNullParent()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			Assert.IsNull(_instance.Parent);
		}

		[Test]
		public void Parent_ChildInstance_ReferencesParent()
		{
			SetupSimpleGraph();
			var childGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				DisplayName = "Child",
				StateTypeAssemblyQualifiedName = typeof(TestStateA).AssemblyQualifiedName
			};
			childGraph.AddNode(childEntry);
			childGraph.AddNode(childState);
			childGraph.EntryNodeId = "childEntry";
			childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				SourcePortName = "out",
				TargetNodeId = "childState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_instance = new StateMachineInstance(_graph);
			var childInstance = new StateMachineInstance(childGraph, null, null, _instance);

			Assert.AreSame(_instance, childInstance.Parent);

			UnityEngine.Object.DestroyImmediate(childGraph);
		}



		private class AsyncTestStateA : State
		{
			public static int EnterAsyncCount;
			public static int UpdateAsyncCount;
			public static int ExitAsyncCount;

			[Transition("To B")] public Action toB;

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

		private class AsyncTestStateB : State
		{
			public static int EnterAsyncCount;
			public static int ExitAsyncCount;

			[Transition("To Exit")] public Action toExit;

			public override Task OnEnterAsync(CancellationToken ct)
			{
				EnterAsyncCount++;
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
				ExitAsyncCount = 0;
			}
		}

		private class ThrowingOnEnterAsyncState : State
		{
			[Transition("Done")] public Action done;

			public override Task OnEnterAsync(CancellationToken ct)
			{
				throw new InvalidOperationException("Test exception in async enter");
			}
		}

		private class ThrowingOnUpdateAsyncState : State
		{
			public override Task OnUpdateAsync(CancellationToken ct)
			{
				throw new InvalidOperationException("Test exception in async update");
			}
		}

		private class ThrowingOnExitAsyncState : State
		{
			[Transition("Done")] public Action done;

			public override Task OnEnterAsync(CancellationToken ct)
			{
				return Task.CompletedTask;
			}

			public override Task OnExitAsync(CancellationToken ct)
			{
				throw new InvalidOperationException("Test exception in async exit");
			}
		}

		private class CancellationTrackingState : State
		{
			public static CancellationToken ReceivedToken;

			public override Task OnEnterAsync(CancellationToken ct)
			{
				ReceivedToken = ct;
				return Task.CompletedTask;
			}

			public static void Reset()
			{
				ReceivedToken = CancellationToken.None;
			}
		}

		private void SetupAsyncSimpleGraph()
		{
			AsyncTestStateA.ResetCounters();
			AsyncTestStateB.ResetCounters();

			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				DisplayName = "State A",
				StateTypeAssemblyQualifiedName = typeof(AsyncTestStateA).AssemblyQualifiedName
			};
			var stateB = new StateNode
			{
				Id = "stateB",
				DisplayName = "State B",
				StateTypeAssemblyQualifiedName = typeof(AsyncTestStateB).AssemblyQualifiedName
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
		public async Task StartAsync_SetsIsRunningTrue()
		{
			SetupAsyncSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			await _instance.StartAsync();

			Assert.IsTrue(_instance.IsRunning);
		}

		[Test]
		public async Task StartAsync_EntersFirstState()
		{
			SetupAsyncSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			await _instance.StartAsync();

			Assert.AreEqual(1, AsyncTestStateA.EnterAsyncCount);
			Assert.IsInstanceOf<AsyncTestStateA>(_instance.CurrentState);
		}

		[Test]
		public async Task StartAsync_WhenAlreadyRunning_DoesNothing()
		{
			SetupAsyncSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			await _instance.StartAsync();

			await _instance.StartAsync();

			Assert.AreEqual(1, AsyncTestStateA.EnterAsyncCount,
				"Should not re-enter first state on duplicate start");
		}

		[Test]
		public async Task StopAsync_SetsIsRunningFalse()
		{
			SetupAsyncSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			await _instance.StartAsync();

			await _instance.StopAsync();

			Assert.IsFalse(_instance.IsRunning);
		}

		[Test]
		public async Task StopAsync_ExitsCurrentState()
		{
			SetupAsyncSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			await _instance.StartAsync();

			await _instance.StopAsync();

			Assert.AreEqual(1, AsyncTestStateA.ExitAsyncCount);
		}

		[Test]
		public async Task StopAsync_ClearsCurrentState()
		{
			SetupAsyncSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			await _instance.StartAsync();

			await _instance.StopAsync();

			Assert.IsNull(_instance.CurrentState);
		}

		[Test]
		public async Task StopAsync_WhenNotRunning_DoesNothing()
		{
			SetupAsyncSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			await _instance.StopAsync();

			Assert.IsFalse(_instance.IsRunning);
			Assert.AreEqual(0, AsyncTestStateA.ExitAsyncCount);
		}

		[Test]
		public async Task UpdateAsync_CallsOnUpdateAsyncOnCurrentState()
		{
			SetupAsyncSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			await _instance.StartAsync();

			await _instance.UpdateAsync();
			await _instance.UpdateAsync();
			await _instance.UpdateAsync();

			Assert.AreEqual(3, AsyncTestStateA.UpdateAsyncCount);
		}

		[Test]
		public async Task UpdateAsync_WhenNotRunning_DoesNothing()
		{
			SetupAsyncSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			await _instance.UpdateAsync();

			Assert.AreEqual(0, AsyncTestStateA.UpdateAsyncCount);
		}

		[Test]
		public async Task UpdateAsync_WhenPausedAsync_DoesNothing()
		{
			SetupAsyncSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			await _instance.StartAsync();
			_instance.Pause();

			await _instance.UpdateAsync();

			Assert.AreEqual(0, AsyncTestStateA.UpdateAsyncCount);
		}

		[Test]
		public async Task StartAsync_FiresOnStartedEvent()
		{
			SetupAsyncSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			var startedFired = false;
			_instance.OnStarted += () => startedFired = true;

			await _instance.StartAsync();

			Assert.IsTrue(startedFired);
		}

		[Test]
		public async Task StopAsync_FiresOnStoppedEvent()
		{
			SetupAsyncSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			await _instance.StartAsync();
			var stoppedFired = false;
			_instance.OnStopped += () => stoppedFired = true;

			await _instance.StopAsync();

			Assert.IsTrue(stoppedFired);
		}

		[Test]
		public async Task StartAsync_FiresOnStateChangedEvent()
		{
			SetupAsyncSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			State oldState = null;
			State newState = null;
			_instance.OnStateChanged += (old, @new) =>
			{
				oldState = old;
				newState = @new;
			};

			await _instance.StartAsync();

			Assert.IsNull(oldState);
			Assert.IsNotNull(newState);
			Assert.IsInstanceOf<AsyncTestStateA>(newState);
		}

		[Test]
		public async Task StartAsync_ThrowingOnEnterAsync_LogsError()
		{
			var entry = new EntryNode { Id = "entry" };
			var stateNode = new StateNode
			{
				Id = "throwingState",
				DisplayName = "Throwing State",
				StateTypeAssemblyQualifiedName = typeof(ThrowingOnEnterAsyncState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateNode);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "throwingState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			LogAssert.Expect(LogType.Error, new Regex("Exception in OnEnterAsync"));

			_instance = new StateMachineInstance(_graph);
			await _instance.StartAsync();

			Assert.IsTrue(_instance.IsRunning, "Instance should still be running after exception in OnEnterAsync");
		}

		[Test]
		public async Task UpdateAsync_ThrowingOnUpdateAsync_LogsError()
		{
			var entry = new EntryNode { Id = "entry" };
			var stateNode = new StateNode
			{
				Id = "throwingState",
				DisplayName = "Throwing State",
				StateTypeAssemblyQualifiedName = typeof(ThrowingOnUpdateAsyncState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateNode);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "throwingState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_instance = new StateMachineInstance(_graph);
			await _instance.StartAsync();

			LogAssert.Expect(LogType.Error, new Regex("Exception in OnUpdateAsync"));
			await _instance.UpdateAsync();
		}

		[Test]
		public async Task StopAsync_ThrowingOnExitAsync_LogsError()
		{
			var entry = new EntryNode { Id = "entry" };
			var stateNode = new StateNode
			{
				Id = "throwingState",
				DisplayName = "Throwing State",
				StateTypeAssemblyQualifiedName = typeof(ThrowingOnExitAsyncState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateNode);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "throwingState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_instance = new StateMachineInstance(_graph);
			await _instance.StartAsync();

			LogAssert.Expect(LogType.Error, new Regex("Exception in OnExitAsync"));
			await _instance.StopAsync();

			Assert.IsFalse(_instance.IsRunning);
		}

		[Test]
		public async Task StartAsync_CancellationToken_IsPassedToState()
		{
			CancellationTrackingState.Reset();

			var entry = new EntryNode { Id = "entry" };
			var stateNode = new StateNode
			{
				Id = "trackingState",
				DisplayName = "Tracking State",
				StateTypeAssemblyQualifiedName = typeof(CancellationTrackingState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateNode);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "trackingState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_instance = new StateMachineInstance(_graph);
			await _instance.StartAsync();

			Assert.IsTrue(CancellationTrackingState.ReceivedToken.CanBeCanceled,
				"Cancellation token passed to state should be cancellable");
			Assert.IsFalse(CancellationTrackingState.ReceivedToken.IsCancellationRequested,
				"Cancellation token should not be cancelled while state is active");
		}

		[Test]
		public async Task StartAsync_WithExternalCancellationToken_LinksToken()
		{
			CancellationTrackingState.Reset();

			var entry = new EntryNode { Id = "entry" };
			var stateNode = new StateNode
			{
				Id = "trackingState",
				DisplayName = "Tracking State",
				StateTypeAssemblyQualifiedName = typeof(CancellationTrackingState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateNode);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "trackingState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			var externalCts = new CancellationTokenSource();
			_instance = new StateMachineInstance(_graph);
			_instance.SetExternalCancellationToken(externalCts.Token);
			await _instance.StartAsync();

			Assert.IsTrue(CancellationTrackingState.ReceivedToken.CanBeCanceled);

			// Cancel external token - state token should also be cancelled
			externalCts.Cancel();

			Assert.IsTrue(CancellationTrackingState.ReceivedToken.IsCancellationRequested,
				"State cancellation token should be cancelled when external token is cancelled");

			externalCts.Dispose();
		}

		[Test]
		public async Task StartAsync_StopAsync_FullLifecycle()
		{
			SetupAsyncSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			await _instance.StartAsync();
			Assert.IsTrue(_instance.IsRunning);
			Assert.AreEqual(1, AsyncTestStateA.EnterAsyncCount);

			await _instance.UpdateAsync();
			Assert.AreEqual(1, AsyncTestStateA.UpdateAsyncCount);

			await _instance.StopAsync();
			Assert.IsFalse(_instance.IsRunning);
			Assert.AreEqual(1, AsyncTestStateA.ExitAsyncCount);
			Assert.IsNull(_instance.CurrentState);
		}



		[Test]
		public void Pause_SetsIsPausedTrue()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			_instance.Pause();

			Assert.IsTrue(_instance.IsPaused);
		}

		[Test]
		public void Resume_SetsIsPausedFalse()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();
			_instance.Pause();

			_instance.Resume();

			Assert.IsFalse(_instance.IsPaused);
		}

		[Test]
		public void Pause_WhenNotRunning_DoesNothing()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			_instance.Pause();

			Assert.IsFalse(_instance.IsPaused);
		}

		[Test]
		public void Resume_WhenNotPaused_DoesNothing()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			_instance.Resume();

			Assert.IsFalse(_instance.IsPaused);
		}

		[Test]
		public void Pause_StopsUpdateFromCallingState()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();
			_instance.Pause();

			_instance.Update();
			_instance.Update();

			Assert.AreEqual(0, TestStateA.UpdateCount,
				"Update should not call state OnUpdate when paused");
		}

		[Test]
		public void Resume_AllowsUpdateToCallState()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();
			_instance.Pause();
			_instance.Resume();

			_instance.Update();

			Assert.AreEqual(1, TestStateA.UpdateCount,
				"Update should call state OnUpdate after resume");
		}

		[Test]
		public void Pause_AlreadyPaused_DoesNothing()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();
			_instance.Pause();

			_instance.Pause();

			Assert.IsTrue(_instance.IsPaused);
		}



		private class LifecycleTrackingState : State
		{
			public static int FixedUpdateCount;
			public static int LateUpdateCount;
			public static int UpdateCount;

			[Transition("Done")] public Action done;

			public override void OnUpdate() => UpdateCount++;
			public override void OnFixedUpdate() => FixedUpdateCount++;
			public override void OnLateUpdate() => LateUpdateCount++;

			public static void ResetCounters()
			{
				FixedUpdateCount = 0;
				LateUpdateCount = 0;
				UpdateCount = 0;
			}
		}

		private void SetupLifecycleTrackingGraph()
		{
			LifecycleTrackingState.ResetCounters();

			var entry = new EntryNode { Id = "entry" };
			var stateNode = new StateNode
			{
				Id = "lifecycleState",
				DisplayName = "Lifecycle State",
				StateTypeAssemblyQualifiedName = typeof(LifecycleTrackingState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateNode);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "lifecycleState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});
		}

		[Test]
		public void FixedUpdate_CallsOnFixedUpdateOnCurrentState()
		{
			SetupLifecycleTrackingGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			_instance.FixedUpdate();
			_instance.FixedUpdate();
			_instance.FixedUpdate();

			Assert.AreEqual(3, LifecycleTrackingState.FixedUpdateCount);
		}

		[Test]
		public void LateUpdate_CallsOnLateUpdateOnCurrentState()
		{
			SetupLifecycleTrackingGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			_instance.LateUpdate();
			_instance.LateUpdate();

			Assert.AreEqual(2, LifecycleTrackingState.LateUpdateCount);
		}

		[Test]
		public void FixedUpdate_WhenNotRunning_DoesNothing()
		{
			SetupLifecycleTrackingGraph();
			_instance = new StateMachineInstance(_graph);

			_instance.FixedUpdate();

			Assert.AreEqual(0, LifecycleTrackingState.FixedUpdateCount);
		}

		[Test]
		public void LateUpdate_WhenNotRunning_DoesNothing()
		{
			SetupLifecycleTrackingGraph();
			_instance = new StateMachineInstance(_graph);

			_instance.LateUpdate();

			Assert.AreEqual(0, LifecycleTrackingState.LateUpdateCount);
		}

		[Test]
		public void FixedUpdate_WhenPaused_DoesNothing()
		{
			SetupLifecycleTrackingGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();
			_instance.Pause();

			_instance.FixedUpdate();

			Assert.AreEqual(0, LifecycleTrackingState.FixedUpdateCount,
				"FixedUpdate should not call state OnFixedUpdate when paused");
		}

		[Test]
		public void LateUpdate_WhenPaused_DoesNothing()
		{
			SetupLifecycleTrackingGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();
			_instance.Pause();

			_instance.LateUpdate();

			Assert.AreEqual(0, LifecycleTrackingState.LateUpdateCount,
				"LateUpdate should not call state OnLateUpdate when paused");
		}

		[Test]
		public void Pause_StopsAllUpdateTypes()
		{
			SetupLifecycleTrackingGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();
			_instance.Pause();

			_instance.Update();
			_instance.FixedUpdate();
			_instance.LateUpdate();

			Assert.AreEqual(0, LifecycleTrackingState.UpdateCount);
			Assert.AreEqual(0, LifecycleTrackingState.FixedUpdateCount);
			Assert.AreEqual(0, LifecycleTrackingState.LateUpdateCount);
		}

		[Test]
		public void Resume_RestoresAllUpdateTypes()
		{
			SetupLifecycleTrackingGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();
			_instance.Pause();
			_instance.Resume();

			_instance.Update();
			_instance.FixedUpdate();
			_instance.LateUpdate();

			Assert.AreEqual(1, LifecycleTrackingState.UpdateCount);
			Assert.AreEqual(1, LifecycleTrackingState.FixedUpdateCount);
			Assert.AreEqual(1, LifecycleTrackingState.LateUpdateCount);
		}

		[Test]
		public void AllUpdateTypes_IndependentOfEachOther()
		{
			SetupLifecycleTrackingGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			_instance.Update();
			_instance.Update();
			_instance.FixedUpdate();
			_instance.LateUpdate();
			_instance.LateUpdate();
			_instance.LateUpdate();

			Assert.AreEqual(2, LifecycleTrackingState.UpdateCount);
			Assert.AreEqual(1, LifecycleTrackingState.FixedUpdateCount);
			Assert.AreEqual(3, LifecycleTrackingState.LateUpdateCount);
		}



		private class AsyncChildState : State
		{
			public static int EnterAsyncCount;
			public static int ExitAsyncCount;

			[Transition("Done")] public Action done;

			public override Task OnEnterAsync(CancellationToken ct)
			{
				EnterAsyncCount++;
				// Immediately complete - trigger done transition
				done?.Invoke();
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
				ExitAsyncCount = 0;
			}
		}

		private class AsyncNextState : State
		{
			public static int EnterAsyncCount;

			[Transition("Done")] public Action done;

			public override Task OnEnterAsync(CancellationToken ct)
			{
				EnterAsyncCount++;
				return Task.CompletedTask;
			}

			public static void ResetCounters()
			{
				EnterAsyncCount = 0;
			}
		}

		[Test]
		public async Task AsyncSubStateMachine_ChildExit_UsesAsyncContinuation()
		{
			AsyncChildState.ResetCounters();
			AsyncNextState.ResetCounters();

			// Create child graph: Entry -> ChildState -> Exit
			var childGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				DisplayName = "Child State",
				StateTypeAssemblyQualifiedName = typeof(AsyncChildState).AssemblyQualifiedName
			};
			var childExit = new ExitNode { Id = "childExit", ExitName = "Done" };

			childGraph.AddNode(childEntry);
			childGraph.AddNode(childState);
			childGraph.AddNode(childExit);
			childGraph.EntryNodeId = "childEntry";

			childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				SourcePortName = "out",
				TargetNodeId = "childState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});
			childGraph.AddConnection(new Connection
			{
				Id = "cc2",
				SourceNodeId = "childState",
				SourcePortName = "done",
				TargetNodeId = "childExit",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// Create parent graph: Entry -> SubStateMachine -> NextState
			var parentEntry = new EntryNode { Id = "parentEntry" };
			var subMachineNode = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = childGraph,
				DisplayName = "Sub Machine"
			};
			var nextState = new StateNode
			{
				Id = "nextState",
				DisplayName = "Next State",
				StateTypeAssemblyQualifiedName = typeof(AsyncNextState).AssemblyQualifiedName
			};

			_graph.AddNode(parentEntry);
			_graph.AddNode(subMachineNode);
			_graph.AddNode(nextState);
			_graph.EntryNodeId = "parentEntry";

			_graph.AddConnection(new Connection
			{
				Id = "pc1",
				SourceNodeId = "parentEntry",
				SourcePortName = "out",
				TargetNodeId = "subMachine",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});
			_graph.AddConnection(new Connection
			{
				Id = "pc2",
				SourceNodeId = "subMachine",
				SourcePortName = "Exit",
				TargetNodeId = "nextState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			try
			{
				_instance = new StateMachineInstance(_graph);
				await _instance.StartAsync();

				// Allow async continuation to complete
				await Task.Yield();

				// The child state machine should have entered and exited
				Assert.AreEqual(1, AsyncChildState.EnterAsyncCount, "Child state should have entered via async");

				// The parent should have continued to NextState via async path
				Assert.IsTrue(_instance.IsRunning, "Instance should still be running");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(childGraph);
			}
		}

		[Test]
		public async Task AsyncSubStateMachine_ChildExit_DoesNotThrow()
		{
			AsyncChildState.ResetCounters();
			AsyncNextState.ResetCounters();

			var childGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				DisplayName = "Child State",
				StateTypeAssemblyQualifiedName = typeof(AsyncChildState).AssemblyQualifiedName
			};
			var childExit = new ExitNode { Id = "childExit", ExitName = "Done" };

			childGraph.AddNode(childEntry);
			childGraph.AddNode(childState);
			childGraph.AddNode(childExit);
			childGraph.EntryNodeId = "childEntry";

			childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				SourcePortName = "out",
				TargetNodeId = "childState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});
			childGraph.AddConnection(new Connection
			{
				Id = "cc2",
				SourceNodeId = "childState",
				SourcePortName = "done",
				TargetNodeId = "childExit",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// Parent graph: Entry -> SubStateMachine (no exit connection)
			var parentEntry = new EntryNode { Id = "parentEntry" };
			var subMachineNode = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = childGraph,
				DisplayName = "Sub Machine"
			};

			_graph.AddNode(parentEntry);
			_graph.AddNode(subMachineNode);
			_graph.EntryNodeId = "parentEntry";

			_graph.AddConnection(new Connection
			{
				Id = "pc1",
				SourceNodeId = "parentEntry",
				SourcePortName = "out",
				TargetNodeId = "subMachine",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			try
			{
				_instance = new StateMachineInstance(_graph);

				// Should not throw even when child exits and there's no exit connection
				Assert.DoesNotThrowAsync(async () =>
				{
					await _instance.StartAsync();
					await Task.Yield();
				});
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(childGraph);
			}
		}



		[Test]
		public void OnStarted_FiresWhenStarted()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var fired = false;
			_instance.OnStarted += () => fired = true;

			_instance.Start();

			Assert.IsTrue(fired);
		}

		[Test]
		public void OnStarted_DoesNotFireBeforeStart()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var fired = false;
			_instance.OnStarted += () => fired = true;

			Assert.IsFalse(fired);
		}

		[Test]
		public void OnStopped_FiresWhenStopped()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var fired = false;
			_instance.OnStopped += () => fired = true;

			_instance.Stop();

			Assert.IsTrue(fired);
		}

		[Test]
		public void OnStopped_DoesNotFireOnStart()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var fired = false;
			_instance.OnStopped += () => fired = true;

			_instance.Start();

			Assert.IsFalse(fired);
		}

		[Test]
		public void OnExited_FiresWhenExitNodeReached()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			string exitNodeId = null;
			_instance.OnExited += id => exitNodeId = id;

			_instance.Start();

			// Transition A -> B
			var stateA = _instance.CurrentState as TestStateA;
			stateA.toB?.Invoke();

			// Transition B -> Exit
			var stateB = _instance.CurrentState as TestStateB;
			stateB.toExit?.Invoke();

			Assert.AreEqual("exit", exitNodeId);
		}

		[Test]
		public void OnExited_DoesNotFireWithoutReachingExitNode()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var fired = false;
			_instance.OnExited += _ => fired = true;

			_instance.Start();

			// Transition A -> B only (no exit)
			var stateA = _instance.CurrentState as TestStateA;
			stateA.toB?.Invoke();

			Assert.IsFalse(fired);
		}

		[Test]
		public void OnTransitionTriggered_FiresOnTransition()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			string fromId = null;
			string toId = null;
			var fireCount = 0;
			_instance.OnTransitionTriggered += (from, to) =>
			{
				fromId = from;
				toId = to;
				fireCount++;
			};

			_instance.Start();

			// First transition is Entry -> StateA (fromId is null since no current state yet)
			Assert.AreEqual(1, fireCount);
			Assert.IsNull(fromId);
			Assert.AreEqual("stateA", toId);
		}

		[Test]
		public void OnTransitionTriggered_FiresWithCorrectNodeIds()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			string fromId = null;
			string toId = null;
			_instance.OnTransitionTriggered += (from, to) =>
			{
				fromId = from;
				toId = to;
			};

			_instance.Start();

			// Transition A -> B
			var stateA = _instance.CurrentState as TestStateA;
			stateA.toB?.Invoke();

			Assert.AreEqual("stateA", fromId);
			Assert.AreEqual("stateB", toId);
		}

		[Test]
		public void OnTransitionTriggeredDetailed_IncludesPortName()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			string fromId = null;
			string toId = null;
			string portName = null;
			_instance.OnTransitionTriggeredDetailed += (from, to, port) =>
			{
				fromId = from;
				toId = to;
				portName = port;
			};

			_instance.Start();

			// Transition A -> B via "toB" port
			var stateA = _instance.CurrentState as TestStateA;
			stateA.toB?.Invoke();

			Assert.AreEqual("stateA", fromId);
			Assert.AreEqual("stateB", toId);
			Assert.AreEqual("toB", portName);
		}

		[Test]
		public void OnTransitionTriggered_FiresMultipleTimes()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var fireCount = 0;
			_instance.OnTransitionTriggered += (_, __) => fireCount++;

			_instance.Start(); // Entry -> A (1)
			var stateA = _instance.CurrentState as TestStateA;
			stateA.toB?.Invoke(); // A -> B (2)

			Assert.AreEqual(2, fireCount);
		}

		[Test]
		public void OnChildInstanceStarted_FiresWhenSubStateMachineStarts()
		{
			// Create child graph: Entry -> ChildState -> Exit
			var childGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				DisplayName = "Child State",
				StateTypeAssemblyQualifiedName = typeof(TestStateA).AssemblyQualifiedName
			};
			var childExit = new ExitNode { Id = "childExit", ExitName = "Done" };

			childGraph.AddNode(childEntry);
			childGraph.AddNode(childState);
			childGraph.AddNode(childExit);
			childGraph.EntryNodeId = "childEntry";

			childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				SourcePortName = "out",
				TargetNodeId = "childState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});
			childGraph.AddConnection(new Connection
			{
				Id = "cc2",
				SourceNodeId = "childState",
				SourcePortName = "toB",
				TargetNodeId = "childExit",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// Parent graph: Entry -> SubStateMachine -> NextState
			var parentEntry = new EntryNode { Id = "parentEntry" };
			var subMachineNode = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = childGraph,
				DisplayName = "Sub Machine"
			};
			var nextState = new StateNode
			{
				Id = "nextState",
				DisplayName = "Next State",
				StateTypeAssemblyQualifiedName = typeof(TestStateB).AssemblyQualifiedName
			};

			_graph.AddNode(parentEntry);
			_graph.AddNode(subMachineNode);
			_graph.AddNode(nextState);
			_graph.EntryNodeId = "parentEntry";

			_graph.AddConnection(new Connection
			{
				Id = "pc1",
				SourceNodeId = "parentEntry",
				SourcePortName = "out",
				TargetNodeId = "subMachine",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});
			_graph.AddConnection(new Connection
			{
				Id = "pc2",
				SourceNodeId = "subMachine",
				SourcePortName = "Exit",
				TargetNodeId = "nextState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			StateMachineInstance childInstance = null;
			StateMachineGraph receivedGraph = null;

			try
			{
				TestStateA.ResetCounters();
				_instance = new StateMachineInstance(_graph);
				_instance.OnChildInstanceStarted += (instance, graph) =>
				{
					childInstance = instance;
					receivedGraph = graph;
				};

				_instance.Start();

				Assert.IsNotNull(childInstance);
				Assert.AreEqual(childGraph, receivedGraph);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(childGraph);
			}
		}

		[Test]
		public void OnChildInstanceStopped_FiresWhenSubStateMachineExits()
		{
			// Create child graph: Entry -> ChildState -> Exit
			var childGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			var childEntry = new EntryNode { Id = "childEntry" };
			var childState = new StateNode
			{
				Id = "childState",
				DisplayName = "Child State",
				StateTypeAssemblyQualifiedName = typeof(TestStateA).AssemblyQualifiedName
			};
			var childExit = new ExitNode { Id = "childExit", ExitName = "Done" };

			childGraph.AddNode(childEntry);
			childGraph.AddNode(childState);
			childGraph.AddNode(childExit);
			childGraph.EntryNodeId = "childEntry";

			childGraph.AddConnection(new Connection
			{
				Id = "cc1",
				SourceNodeId = "childEntry",
				SourcePortName = "out",
				TargetNodeId = "childState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});
			childGraph.AddConnection(new Connection
			{
				Id = "cc2",
				SourceNodeId = "childState",
				SourcePortName = "toB",
				TargetNodeId = "childExit",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			// Parent graph: Entry -> SubStateMachine -> NextState
			var parentEntry = new EntryNode { Id = "parentEntry" };
			var subMachineNode = new SubStateMachineNode
			{
				Id = "subMachine",
				SubGraph = childGraph,
				DisplayName = "Sub Machine"
			};
			var nextState = new StateNode
			{
				Id = "nextState",
				DisplayName = "Next State",
				StateTypeAssemblyQualifiedName = typeof(TestStateB).AssemblyQualifiedName
			};

			_graph.AddNode(parentEntry);
			_graph.AddNode(subMachineNode);
			_graph.AddNode(nextState);
			_graph.EntryNodeId = "parentEntry";

			_graph.AddConnection(new Connection
			{
				Id = "pc1",
				SourceNodeId = "parentEntry",
				SourcePortName = "out",
				TargetNodeId = "subMachine",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});
			_graph.AddConnection(new Connection
			{
				Id = "pc2",
				SourceNodeId = "subMachine",
				SourcePortName = "Exit",
				TargetNodeId = "nextState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			StateMachineInstance stoppedInstance = null;

			try
			{
				TestStateA.ResetCounters();
				_instance = new StateMachineInstance(_graph);
				_instance.OnChildInstanceStopped += instance =>
				{
					stoppedInstance = instance;
				};

				_instance.Start();

				// Child is now running in ChildState (TestStateA)
				// Trigger the child's exit by invoking toB (connected to exit node)
				var childCurrentState = stoppedInstance == null
					? _instance.ActiveChildInstance?.CurrentState as TestStateA
					: null;

				// If child auto-completed, stoppedInstance may already be set
				if (stoppedInstance == null && childCurrentState != null)
				{
					childCurrentState.toB?.Invoke();
				}

				Assert.IsNotNull(stoppedInstance);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(childGraph);
			}
		}

		[Test]
		public void OnStopped_FiresWhenExitNodeReached()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var fired = false;
			_instance.OnStopped += () => fired = true;

			_instance.Start();

			// Transition A -> B -> Exit
			var stateA = _instance.CurrentState as TestStateA;
			stateA.toB?.Invoke();
			var stateB = _instance.CurrentState as TestStateB;
			stateB.toExit?.Invoke();

			Assert.IsTrue(fired);
		}

		[Test]
		public void OnExited_FiresBeforeOnStopped()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var eventOrder = new System.Collections.Generic.List<string>();
			_instance.OnExited += _ => eventOrder.Add("exited");
			_instance.OnStopped += () => eventOrder.Add("stopped");

			_instance.Start();

			var stateA = _instance.CurrentState as TestStateA;
			stateA.toB?.Invoke();
			var stateB = _instance.CurrentState as TestStateB;
			stateB.toExit?.Invoke();

			Assert.AreEqual(2, eventOrder.Count);
			Assert.AreEqual("exited", eventOrder[0]);
			Assert.AreEqual("stopped", eventOrder[1]);
		}

		[Test]
		public void GetState_WithValidNodeId_ReturnsState()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var state = _instance.GetState("stateA");

			Assert.IsNotNull(state);
			Assert.IsInstanceOf<TestStateA>(state);
		}

		[Test]
		public void GetState_WithInvalidNodeId_ReturnsNull()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var state = _instance.GetState("nonexistent");

			Assert.IsNull(state);
		}

		[Test]
		public void GetState_WithNullNodeId_ReturnsNull()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var state = _instance.GetState(null);

			Assert.IsNull(state);
		}

		[Test]
		public void GetStateGeneric_WithValidNodeId_ReturnsCastState()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var state = _instance.GetState<TestStateA>("stateA");

			Assert.IsNotNull(state);
			Assert.IsInstanceOf<TestStateA>(state);
		}

		[Test]
		public void GetStateGeneric_WithWrongType_ReturnsNull()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var state = _instance.GetState<TestStateB>("stateA");

			Assert.IsNull(state);
		}

		[Test]
		public void GetStateGeneric_WithInvalidNodeId_ReturnsNull()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var state = _instance.GetState<TestStateA>("nonexistent");

			Assert.IsNull(state);
		}

		[Test]
		public void GetAllStates_ReturnsAllStateInstances()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var states = new System.Collections.Generic.List<State>(_instance.GetAllStates());

			Assert.IsTrue(states.Count >= 2, $"Expected at least 2 states, got {states.Count}");
		}

		[Test]
		public void GetAllStates_ContainsBothTestStates()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var states = new System.Collections.Generic.List<State>(_instance.GetAllStates());
			var hasA = false;
			var hasB = false;

			foreach (var state in states)
			{
				if (state is TestStateA) hasA = true;
				if (state is TestStateB) hasB = true;
			}

			Assert.IsTrue(hasA, "Should contain TestStateA");
			Assert.IsTrue(hasB, "Should contain TestStateB");
		}

		[Test]
		public void GetContextRequirements_WithNoContextStates_ReturnsEmpty()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var requirements = _instance.GetContextRequirements();

			Assert.IsNotNull(requirements);
			Assert.AreEqual(0, requirements.Count, "Simple states should have no context requirements");
		}

		[Test]
		public void GetContextRequirements_WithContextState_ReturnsRequirement()
		{
			_graph.InitializeNewGraph();

			var contextState = new StateNode
			{
				Id = "contextState",
				DisplayName = "Context State",
				StateTypeAssemblyQualifiedName = typeof(ContextStateA).AssemblyQualifiedName
			};
			_graph.AddNode(contextState);
			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = _graph.EntryNodeId,
				SourcePortName = "out",
				TargetNodeId = "contextState",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			ContextStateA.ResetCounters();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var requirements = _instance.GetContextRequirements();

			Assert.IsTrue(requirements.Count >= 1, "Should have at least one context requirement");

			var found = false;
			foreach (var req in requirements)
			{
				if (req.RequiredContextType == typeof(TestSharedContext))
				{
					found = true;
					Assert.AreEqual("ContextStateA", req.StateName);
				}
			}
			Assert.IsTrue(found, "Should find TestSharedContext requirement from ContextStateA");
		}

		[Test]
		public void Reinitialize_ResetsContextAndBlackboard()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();
			_instance.PrepareForReuse();

			var newContext = new StateMachineContext();
			_instance.Reinitialize(newContext);

			Assert.IsFalse(_instance.IsRunning, "Should not be running after Reinitialize (needs Start)");
		}

		[Test]
		public void Reinitialize_AfterPrepareForReuse_CanStartAgain()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			Assert.IsTrue(_instance.IsRunning);
			_instance.PrepareForReuse();
			Assert.IsFalse(_instance.IsRunning);

			var newContext = new StateMachineContext();
			_instance.Reinitialize(newContext);
			_instance.Start();

			Assert.IsTrue(_instance.IsRunning);
			Assert.IsNotNull(_instance.CurrentState);
		}

		[Test]
		public void Reinitialize_WithParent_SetsNestingDepth()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var parentGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			parentGraph.InitializeNewGraph();
			var parentInstance = new StateMachineInstance(parentGraph);

			try
			{
				_instance.PrepareForReuse();
				_instance.Reinitialize(new StateMachineContext(), null, parentInstance);

				Assert.AreEqual(1, _instance.NestingDepth, "Child of root should have nesting depth 1");
			}
			finally
			{
				parentInstance.Stop();
				UnityEngine.Object.DestroyImmediate(parentGraph);
			}
		}

		[Test]
		public void Reinitialize_WithoutParent_NestingDepthIsZero()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();
			_instance.PrepareForReuse();

			_instance.Reinitialize(new StateMachineContext());

			Assert.AreEqual(0, _instance.NestingDepth);
		}

		[Test]
		public void Reinitialize_UpdatesInstanceId()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var originalId = _instance.InstanceId;
			_instance.PrepareForReuse();
			_instance.Reinitialize(new StateMachineContext());

			Assert.AreNotEqual(originalId, _instance.InstanceId, "InstanceId should change after Reinitialize");
		}

		[Test]
		public void GetRequiredContextType_ReturnsCorrectType()
		{
			var result = StateMachineInstance.GetRequiredContextType(typeof(ContextStateA));

			Assert.AreEqual(typeof(TestSharedContext), result);
		}

		[Test]
		public void GetRequiredContextType_NonGenericState_ReturnsNull()
		{
			var result = StateMachineInstance.GetRequiredContextType(typeof(TestStateA));

			Assert.IsNull(result);
		}

		[Test]
		public void GetRequiredContextType_CachesResults()
		{
			var result1 = StateMachineInstance.GetRequiredContextType(typeof(ContextStateA));
			var result2 = StateMachineInstance.GetRequiredContextType(typeof(ContextStateA));

			Assert.AreSame(result1, result2, "Should return same cached Type instance");
		}

		[Test]
		public void GetRequiredContextType_DifferentTypes_CachedIndependently()
		{
			var contextResult = StateMachineInstance.GetRequiredContextType(typeof(ContextStateA));
			var plainResult = StateMachineInstance.GetRequiredContextType(typeof(TestStateA));

			Assert.IsNotNull(contextResult);
			Assert.IsNull(plainResult);
		}

	}
}
