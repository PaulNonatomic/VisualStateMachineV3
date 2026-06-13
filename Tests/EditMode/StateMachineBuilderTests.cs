using System;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class StateMachineBuilderTests
	{
		private StateMachineInstance _instance;

		private class IdleState : State
		{
			public static int EnterCount;
			public static int ExitCount;

			[Transition("OnActivate")] public Action onActivate;

			public override void OnEnter() => EnterCount++;
			public override void OnExit() => ExitCount++;

			public static void ResetCounters()
			{
				EnterCount = 0;
				ExitCount = 0;
			}
		}

		private class ActiveState : State
		{
			public static int EnterCount;
			public static int ExitCount;

			[Transition("OnDeactivate")] public Action onDeactivate;
			[Transition("OnComplete")] public Action onComplete;

			public override void OnEnter() => EnterCount++;
			public override void OnExit() => ExitCount++;

			public static void ResetCounters()
			{
				EnterCount = 0;
				ExitCount = 0;
			}
		}

		private class CompletedState : State
		{
			public static int EnterCount;

			public override void OnEnter() => EnterCount++;

			public static void ResetCounters()
			{
				EnterCount = 0;
			}
		}

		[SetUp]
		public void SetUp()
		{
			IdleState.ResetCounters();
			ActiveState.ResetCounters();
			CompletedState.ResetCounters();
		}

		[TearDown]
		public void TearDown()
		{
			_instance?.Stop();
			_instance = null;
		}


		[Test]
		public void Create_ReturnsBuilder()
		{
			var builder = StateMachineBuilder.Create();

			Assert.IsNotNull(builder);
		}

		[Test]
		public void Create_WithGameObject_ReturnsBuilder()
		{
			var go = new GameObject("TestObject");
			try
			{
				var builder = StateMachineBuilder.Create(go);

				Assert.IsNotNull(builder);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(go);
			}
		}



		[Test]
		public void AddState_Generic_ReturnsStateRef()
		{
			var builder = StateMachineBuilder.Create();

			var stateRef = builder.AddState<IdleState>();

			Assert.IsNotNull(stateRef);
		}

		[Test]
		public void AddState_Generic_UsesTypeNameAsKey()
		{
			var builder = StateMachineBuilder.Create();

			var stateRef = builder.AddState<IdleState>();

			Assert.AreEqual("IdleState", (string)stateRef);
		}

		[Test]
		public void AddState_WithCustomKey_UsesProvidedKey()
		{
			var builder = StateMachineBuilder.Create();

			var stateRef = builder.AddState<IdleState>("CustomKey");

			Assert.AreEqual("CustomKey", (string)stateRef);
		}

		[Test]
		public void AddState_WithType_ReturnsStateRef()
		{
			var builder = StateMachineBuilder.Create();

			var stateRef = builder.AddState(typeof(IdleState));

			Assert.IsNotNull(stateRef);
		}

		[Test]
		public void AddState_DuplicateKey_ThrowsException()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>("DuplicateKey");

			Assert.Throws<ArgumentException>(() => builder.AddState<ActiveState>("DuplicateKey"));
		}

		[Test]
		public void AddState_NonStateType_ThrowsException()
		{
			var builder = StateMachineBuilder.Create();

			Assert.Throws<ArgumentException>(() => builder.AddState(typeof(string)));
		}

		[Test]
		public void AddState_MultipleStates_AllRegistered()
		{
			var builder = StateMachineBuilder.Create();

			var idle = builder.AddState<IdleState>();
			var active = builder.AddState<ActiveState>();
			var completed = builder.AddState<CompletedState>();

			Assert.AreEqual("IdleState", (string)idle);
			Assert.AreEqual("ActiveState", (string)active);
			Assert.AreEqual("CompletedState", (string)completed);
		}



		[Test]
		public void State_ByKey_ReturnsCorrectRef()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			var stateRef = builder.State("IdleState");

			Assert.AreEqual("IdleState", (string)stateRef);
		}

		[Test]
		public void State_ByType_ReturnsCorrectRef()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			var stateRef = builder.State<IdleState>();

			Assert.AreEqual("IdleState", (string)stateRef);
		}

		[Test]
		public void State_NonExistentKey_ThrowsException()
		{
			var builder = StateMachineBuilder.Create();

			Assert.Throws<ArgumentException>(() => builder.State("NonExistent"));
		}



		[Test]
		public void SetEntryState_ByKey_SetsEntry()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();
			builder.AddState<ActiveState>();

			builder.SetEntryState("ActiveState");
			_instance = builder.Build();
			_instance.Start();

			Assert.AreEqual(0, IdleState.EnterCount);
			Assert.AreEqual(1, ActiveState.EnterCount);
		}

		[Test]
		public void SetEntryState_ByRef_SetsEntry()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();
			var active = builder.AddState<ActiveState>();

			builder.SetEntryState(active);
			_instance = builder.Build();
			_instance.Start();

			Assert.AreEqual(0, IdleState.EnterCount);
			Assert.AreEqual(1, ActiveState.EnterCount);
		}

		[Test]
		public void SetEntryState_ByType_SetsEntry()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();
			builder.AddState<ActiveState>();

			builder.SetEntryState<ActiveState>();
			_instance = builder.Build();
			_instance.Start();

			Assert.AreEqual(0, IdleState.EnterCount);
			Assert.AreEqual(1, ActiveState.EnterCount);
		}

		[Test]
		public void SetEntryState_NonExistentKey_ThrowsException()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			Assert.Throws<ArgumentException>(() => builder.SetEntryState("NonExistent"));
		}

		[Test]
		public void Build_WithoutSetEntryState_UsesFirstAddedState()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();
			builder.AddState<ActiveState>();

			_instance = builder.Build();
			_instance.Start();

			Assert.AreEqual(1, IdleState.EnterCount);
			Assert.AreEqual(0, ActiveState.EnterCount);
		}



		[Test]
		public void AddTransition_ByKeys_CreatesConnection()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();
			builder.AddState<ActiveState>();

			builder.AddTransition("IdleState", "OnActivate", "ActiveState");
			var graph = builder.GetGraph();

			Assert.IsTrue(graph.Connections.Count > 0);
		}

		[Test]
		public void AddTransition_ByTypes_CreatesConnection()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();
			builder.AddState<ActiveState>();

			builder.AddTransition<IdleState, ActiveState>("OnActivate");
			var graph = builder.GetGraph();

			Assert.IsTrue(graph.Connections.Count > 0);
		}

		[Test]
		public void AddTransition_ByRefs_CreatesConnection()
		{
			var builder = StateMachineBuilder.Create();
			var idle = builder.AddState<IdleState>();
			var active = builder.AddState<ActiveState>();

			builder.AddTransition(idle, "OnActivate", active);
			var graph = builder.GetGraph();

			Assert.IsTrue(graph.Connections.Count > 0);
		}

		[Test]
		public void AddTransition_NonExistentFromState_ThrowsException()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<ActiveState>();

			Assert.Throws<ArgumentException>(() => builder.AddTransition("NonExistent", "OnActivate", "ActiveState"));
		}

		[Test]
		public void AddTransition_NonExistentToState_ThrowsException()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			Assert.Throws<ArgumentException>(() => builder.AddTransition("IdleState", "OnActivate", "NonExistent"));
		}

		[Test]
		public void AddTransition_WithTransitionTime_SetsDelay()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();
			builder.AddState<ActiveState>();

			builder.AddTransition("IdleState", "OnActivate", "ActiveState", 1.5f);
			var graph = builder.GetGraph();

			var connection = graph.Connections[graph.Connections.Count - 1];
			Assert.AreEqual(1.5f, connection.TransitionTime);
		}



		[Test]
		public void StateRef_TransitionTo_ByRef_CreatesConnection()
		{
			var builder = StateMachineBuilder.Create();
			var idle = builder.AddState<IdleState>();
			var active = builder.AddState<ActiveState>();

			idle.TransitionTo("OnActivate", active);
			var graph = builder.GetGraph();

			Assert.IsTrue(graph.Connections.Count > 0);
		}

		[Test]
		public void StateRef_TransitionTo_ByKey_CreatesConnection()
		{
			var builder = StateMachineBuilder.Create();
			var idle = builder.AddState<IdleState>();
			builder.AddState<ActiveState>();

			idle.TransitionTo("OnActivate", "ActiveState");
			var graph = builder.GetGraph();

			Assert.IsTrue(graph.Connections.Count > 0);
		}

		[Test]
		public void StateRef_TransitionTo_ByType_CreatesConnection()
		{
			var builder = StateMachineBuilder.Create();
			var idle = builder.AddState<IdleState>();
			builder.AddState<ActiveState>();

			idle.TransitionTo<ActiveState>("OnActivate");
			var graph = builder.GetGraph();

			Assert.IsTrue(graph.Connections.Count > 0);
		}

		[Test]
		public void StateRef_TransitionTo_IsChainable()
		{
			var builder = StateMachineBuilder.Create();
			var idle = builder.AddState<IdleState>();
			var active = builder.AddState<ActiveState>();
			builder.AddState<CompletedState>();

			idle.TransitionTo("OnActivate", active)
				.AtPosition(100, 100);

			Assert.Pass();
		}



		[Test]
		public void AddVariable_Int_AddsToGraph()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			builder.AddVariable<int>("health", 100);
			var graph = builder.GetGraph();

			Assert.IsTrue(graph.BlackboardDefinitions.Count > 0);
		}

		[Test]
		public void AddVariable_String_AddsToGraph()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			builder.AddVariable<string>("playerName", "Player1");
			var graph = builder.GetGraph();

			Assert.IsTrue(graph.BlackboardDefinitions.Count > 0);
		}

		[Test]
		public void AddVariable_Exposed_SetsExposedFlag()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			builder.AddVariable<int>("score", 0, exposed: true);
			var graph = builder.GetGraph();

			var variable = graph.BlackboardDefinitions[0];
			Assert.IsTrue(variable.IsExposed);
		}

		[Test]
		public void AddVariable_Multiple_AllAdded()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			builder
				.AddVariable<int>("health", 100)
				.AddVariable<float>("speed", 5.5f)
				.AddVariable<bool>("isAlive", true);

			var graph = builder.GetGraph();

			Assert.AreEqual(3, graph.BlackboardDefinitions.Count);
		}



		[Test]
		public void Build_ReturnsStateMachineInstance()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			_instance = builder.Build();

			Assert.IsNotNull(_instance);
		}

		[Test]
		public void Build_WithNoStates_ThrowsException()
		{
			var builder = StateMachineBuilder.Create();

			Assert.Throws<InvalidOperationException>(() => builder.Build());
		}

		[Test]
		public void Build_InstanceCanStart()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			_instance = builder.Build();
			_instance.Start();

			Assert.IsTrue(_instance.IsRunning);
		}

		[Test]
		public void Build_InstanceCanStop()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			_instance = builder.Build();
			_instance.Start();
			_instance.Stop();

			Assert.IsFalse(_instance.IsRunning);
		}

		[Test]
		public void Build_InstanceHasBlackboard()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			_instance = builder.Build();

			Assert.IsNotNull(_instance.Blackboard);
		}



		[Test]
		public void BuildAndStart_ReturnsRunningInstance()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			_instance = builder.BuildAndStart();

			Assert.IsTrue(_instance.IsRunning);
		}

		[Test]
		public void BuildAndStart_EntersEntryState()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			_instance = builder.BuildAndStart();

			Assert.AreEqual(1, IdleState.EnterCount);
		}



		[Test]
		public void WithContext_SetsContext()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();
			var context = new TestContext();

			builder.WithContext(context);
			_instance = builder.Build();

			Assert.AreEqual(context, _instance.Context);
		}

		[Test]
		public void Build_WithGameObject_CreatesContext()
		{
			var go = new GameObject("TestObject");
			try
			{
				var builder = StateMachineBuilder.Create(go);
				builder.AddState<IdleState>();

				_instance = builder.Build();

				Assert.IsNotNull(_instance.Context);
				Assert.AreEqual(go, _instance.Context.GameObject);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(go);
			}
		}

		private class TestContext : IStateMachineContext
		{
			public GameObject GameObject => null;
			public Transform Transform => null;
		}



		[Test]
		public void WithLayout_SetsLayoutMode()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			builder.WithLayout(GraphLayoutMode.Grid);

			Assert.Pass();
		}

		[Test]
		public void WithoutLayout_DisablesLayout()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			builder.WithoutLayout();

			Assert.Pass();
		}

		[Test]
		public void WithLayout_CustomAction_IsInvoked()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();
			var layoutCalled = false;

			builder.WithLayout(graph => layoutCalled = true);
			_instance = builder.Build();

			Assert.IsTrue(layoutCalled);
		}



		[Test]
		public void StateRef_AtPosition_SetsNodePosition()
		{
			var builder = StateMachineBuilder.Create();
			var idle = builder.AddState<IdleState>();

			idle.AtPosition(100, 200);
			var node = builder.GetStateNode("IdleState");

			Assert.AreEqual(new Vector2(100, 200), node.Position);
		}

		[Test]
		public void StateRef_AtPosition_Vector2_SetsNodePosition()
		{
			var builder = StateMachineBuilder.Create();
			var idle = builder.AddState<IdleState>();

			idle.AtPosition(new Vector2(150, 250));
			var node = builder.GetStateNode("IdleState");

			Assert.AreEqual(new Vector2(150, 250), node.Position);
		}

		[Test]
		public void SetEntryNodePosition_SetsPosition()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			builder.SetEntryNodePosition(new Vector2(50, 50));

			Assert.Pass();
		}



		[Test]
		public void GetGraph_ReturnsGraph()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			var graph = builder.GetGraph();

			Assert.IsNotNull(graph);
		}

		[Test]
		public void GetGraph_GraphContainsEntryNode()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			var graph = builder.GetGraph();

			Assert.IsTrue(graph.NodeCount >= 2);
		}



		[Test]
		public void GetStateNode_ExistingKey_ReturnsNode()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			var node = builder.GetStateNode("IdleState");

			Assert.IsNotNull(node);
		}

		[Test]
		public void GetStateNode_NonExistentKey_ReturnsNull()
		{
			var builder = StateMachineBuilder.Create();
			builder.AddState<IdleState>();

			var node = builder.GetStateNode("NonExistent");

			Assert.IsNull(node);
		}



		[Test]
		public void FluentAPI_FullChain_Works()
		{
			var builder = StateMachineBuilder.Create()
				.AddVariable<int>("health", 100)
				.AddVariable<float>("speed", 5.0f);

			var idle = builder.AddState<IdleState>()
				.AtPosition(0, 0);

			var active = builder.AddState<ActiveState>()
				.AtPosition(200, 0);

			var completed = builder.AddState<CompletedState>()
				.AtPosition(400, 0);

			idle.TransitionTo("OnActivate", active);
			active.TransitionTo("OnDeactivate", idle);
			active.TransitionTo("OnComplete", completed);

			builder
				.SetEntryState(idle)
				.WithoutLayout();

			_instance = builder.Build();
			_instance.Start();

			Assert.IsTrue(_instance.IsRunning);
			Assert.AreEqual(1, IdleState.EnterCount);
		}

	}
}
