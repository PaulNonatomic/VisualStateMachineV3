using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class StateTests
	{

		private class TestContext
		{
			public string Name { get; set; }
			public int Value { get; set; }
		}

		private class DerivedTestContext : TestContext
		{
			public float ExtraValue { get; set; }
		}

		private class IncompatibleContext
		{
			public string Data { get; set; }
		}



		private class ContextualState : State<TestContext>
		{
			public bool OnEnterCalled { get; private set; }
			public string CapturedName { get; private set; }
			public int CapturedValue { get; private set; }

			public override void OnEnter()
			{
				OnEnterCalled = true;
				if (SharedContext != null)
				{
					CapturedName = SharedContext.Name;
					CapturedValue = SharedContext.Value;
				}
			}

			// Expose protected SharedContext for testing
			public TestContext GetSharedContext() => SharedContext;
		}

		private class DerivedContextState : State<DerivedTestContext>
		{
			public DerivedTestContext GetSharedContext() => SharedContext;
		}


		private class SimpleState : State
		{
			public bool OnEnterCalled { get; private set; }
			public bool OnUpdateCalled { get; private set; }
			public bool OnExitCalled { get; private set; }
			public int UpdateCount { get; private set; }

			public override void OnEnter()
			{
				OnEnterCalled = true;
			}

			public override void OnUpdate()
			{
				OnUpdateCalled = true;
				UpdateCount++;
			}

			public override void OnExit()
			{
				OnExitCalled = true;
			}
		}

		private class AsyncState : State
		{
			public bool OnEnterAsyncCalled { get; private set; }
			public bool OnExitAsyncCalled { get; private set; }
			public bool OnEnterAsyncCompleted { get; private set; }
			public bool OnExitAsyncCompleted { get; private set; }

			public override async Task OnEnterAsync()
			{
				OnEnterAsyncCalled = true;
				await Task.Delay(10);
				OnEnterAsyncCompleted = true;
			}

			public override async Task OnExitAsync()
			{
				OnExitAsyncCalled = true;
				await Task.Delay(10);
				OnExitAsyncCompleted = true;
			}
		}

		private class StateWithInputs : State
		{
			[DataIn("Speed")] public float speed;

			[DataIn("Name", Required = true)] public string playerName;

			[DataIn] public int count;
		}

		private class StateWithOutputs : State
		{
			[DataOut("Result")] public float result;

			[DataOut] public string message;
		}

		private class StateWithTransitions : State
		{
			[Transition("On Complete")] public Action onComplete;

			[Transition("On Fail")] public Action onFail;

			public bool TransitionTriggered { get; private set; }

			public void TriggerComplete()
			{
				TransitionTriggered = true;
				onComplete?.Invoke();
			}
		}



		[Test]
		public void State_OnEnter_IsCalled()
		{
			var state = new SimpleState();

			state.OnEnter();

			Assert.IsTrue(state.OnEnterCalled);
		}

		[Test]
		public void State_OnUpdate_IsCalled()
		{
			var state = new SimpleState();

			state.OnUpdate();

			Assert.IsTrue(state.OnUpdateCalled);
			Assert.AreEqual(1, state.UpdateCount);
		}

		[Test]
		public void State_OnUpdate_CanBeCalledMultipleTimes()
		{
			var state = new SimpleState();

			state.OnUpdate();
			state.OnUpdate();
			state.OnUpdate();

			Assert.AreEqual(3, state.UpdateCount);
		}

		[Test]
		public void State_OnExit_IsCalled()
		{
			var state = new SimpleState();

			state.OnExit();

			Assert.IsTrue(state.OnExitCalled);
		}

		[Test]
		public async Task State_OnEnterAsync_IsCalled()
		{
			var state = new AsyncState();

			await state.OnEnterAsync();

			Assert.IsTrue(state.OnEnterAsyncCalled);
			Assert.IsTrue(state.OnEnterAsyncCompleted);
		}

		[Test]
		public async Task State_OnExitAsync_IsCalled()
		{
			var state = new AsyncState();

			await state.OnExitAsync();

			Assert.IsTrue(state.OnExitAsyncCalled);
			Assert.IsTrue(state.OnExitAsyncCompleted);
		}



		[Test]
		public void State_WithInputs_HasInputFields()
		{
			var state = new StateWithInputs();
			var type = state.GetType();

			var speedField = type.GetField("speed");
			var nameField = type.GetField("playerName");
			var countField = type.GetField("count");

			Assert.IsNotNull(speedField);
			Assert.IsNotNull(nameField);
			Assert.IsNotNull(countField);
		}

		[Test]
		public void State_DataInAttribute_HasCorrectProperties()
		{
			var type = typeof(StateWithInputs);
			var speedField = type.GetField("speed");
			var attribute = (DataInAttribute)Attribute.GetCustomAttribute(speedField, typeof(DataInAttribute));

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Speed", attribute.DisplayName);
		}

		[Test]
		public void State_DataInAttribute_RequiredProperty()
		{
			var type = typeof(StateWithInputs);
			var nameField = type.GetField("playerName");
			var attribute = (DataInAttribute)Attribute.GetCustomAttribute(nameField, typeof(DataInAttribute));

			Assert.IsNotNull(attribute);
			Assert.IsTrue(attribute.Required);
		}



		[Test]
		public void State_WithOutputs_HasOutputFields()
		{
			var state = new StateWithOutputs();
			var type = state.GetType();

			var resultField = type.GetField("result");
			var messageField = type.GetField("message");

			Assert.IsNotNull(resultField);
			Assert.IsNotNull(messageField);
		}

		[Test]
		public void State_DataOutAttribute_HasCorrectProperties()
		{
			var type = typeof(StateWithOutputs);
			var resultField = type.GetField("result");
			var attribute = (DataOutAttribute)Attribute.GetCustomAttribute(resultField, typeof(DataOutAttribute));

			Assert.IsNotNull(attribute);
			Assert.AreEqual("Result", attribute.DisplayName);
		}



		[Test]
		public void State_WithTransitions_HasTransitionFields()
		{
			var state = new StateWithTransitions();
			var type = state.GetType();

			var completeField = type.GetField("onComplete");
			var failField = type.GetField("onFail");

			Assert.IsNotNull(completeField);
			Assert.IsNotNull(failField);
		}

		[Test]
		public void State_TransitionAttribute_HasCorrectProperties()
		{
			var type = typeof(StateWithTransitions);
			var completeField = type.GetField("onComplete");
			var attribute = (TransitionAttribute)Attribute.GetCustomAttribute(completeField, typeof(TransitionAttribute));

			Assert.IsNotNull(attribute);
			Assert.AreEqual("On Complete", attribute.DisplayName);
		}

		[Test]
		public void State_Transition_CanBeTriggered()
		{
			var state = new StateWithTransitions();
			var transitionCalled = false;

			state.onComplete = () => transitionCalled = true;
			state.TriggerComplete();

			Assert.IsTrue(state.TransitionTriggered);
			Assert.IsTrue(transitionCalled);
		}

		[Test]
		public void State_Transition_NullAction_DoesNotThrow()
		{
			var state = new StateWithTransitions();

			Assert.DoesNotThrow(() => state.TriggerComplete());
			Assert.IsTrue(state.TransitionTriggered);
		}



		[Test]
		public void State_InputFields_CanBeSet()
		{
			var state = new StateWithInputs();

			state.speed = 10f;
			state.playerName = "Test";
			state.count = 5;

			Assert.AreEqual(10f, state.speed);
			Assert.AreEqual("Test", state.playerName);
			Assert.AreEqual(5, state.count);
		}

		[Test]
		public void State_OutputFields_CanBeSet()
		{
			var state = new StateWithOutputs();

			state.result = 42f;
			state.message = "Success";

			Assert.AreEqual(42f, state.result);
			Assert.AreEqual("Success", state.message);
		}




		[Test]
		public void StateOfT_SharedContext_IsNullBeforeInjection()
		{
			var state = new ContextualState();

			Assert.IsNull(state.GetSharedContext());
		}

		[Test]
		public void StateOfT_SharedContext_IsSetAfterInjection()
		{
			var state = new ContextualState();
			var context = new TestContext { Name = "Test", Value = 42 };

			state.SetContextUntyped(context);

			Assert.IsNotNull(state.GetSharedContext());
			Assert.AreEqual("Test", state.GetSharedContext().Name);
			Assert.AreEqual(42, state.GetSharedContext().Value);
		}

		[Test]
		public void StateOfT_RequiredContextType_ReturnsCorrectType()
		{
			var state = new ContextualState();

			Assert.AreEqual(typeof(TestContext), state.RequiredContextType);
		}

		[Test]
		public void StateOfT_RequiredContextType_ReturnsCorrectDerivedType()
		{
			var state = new DerivedContextState();

			Assert.AreEqual(typeof(DerivedTestContext), state.RequiredContextType);
		}

		[Test]
		public void StateOfT_SetContextUntyped_WithCompatibleType_SetsContext()
		{
			var state = new ContextualState();
			var context = new TestContext { Name = "Compatible", Value = 100 };

			state.SetContextUntyped(context);

			Assert.IsNotNull(state.GetSharedContext());
			Assert.AreEqual("Compatible", state.GetSharedContext().Name);
		}

		[Test]
		public void StateOfT_SetContextUntyped_WithDerivedType_SetsContext()
		{
			var state = new ContextualState();
			var derivedContext = new DerivedTestContext { Name = "Derived", Value = 50, ExtraValue = 3.14f };

			state.SetContextUntyped(derivedContext);

			Assert.IsNotNull(state.GetSharedContext());
			Assert.AreEqual("Derived", state.GetSharedContext().Name);
			Assert.AreEqual(50, state.GetSharedContext().Value);
		}

		[Test]
		public void StateOfT_SetContextUntyped_WithIncompatibleType_DoesNotSetContext()
		{
			var state = new ContextualState();
			var incompatible = new IncompatibleContext { Data = "Wrong Type" };

			state.SetContextUntyped(incompatible);

			Assert.IsNull(state.GetSharedContext());
		}

		[Test]
		public void StateOfT_SetContextUntyped_WithNull_SetsContextToNull()
		{
			var state = new ContextualState();
			var context = new TestContext { Name = "Initial", Value = 1 };
			state.SetContextUntyped(context);

			state.SetContextUntyped(null);

			Assert.IsNull(state.GetSharedContext());
		}

		[Test]
		public void StateOfT_HasValidContext_FalseBeforeInjection()
		{
			var state = new ContextualState();

			Assert.IsFalse(state.HasValidContext);
		}

		[Test]
		public void StateOfT_HasValidContext_TrueAfterInjection()
		{
			var state = new ContextualState();
			var context = new TestContext { Name = "Test", Value = 1 };

			state.SetContextUntyped(context);

			Assert.IsTrue(state.HasValidContext);
		}

		[Test]
		public void StateOfT_HasValidContext_FalseAfterNullInjection()
		{
			var state = new ContextualState();
			state.SetContextUntyped(new TestContext { Name = "Test", Value = 1 });

			state.SetContextUntyped(null);

			Assert.IsFalse(state.HasValidContext);
		}

		[Test]
		public void StateOfT_HasValidContext_FalseAfterIncompatibleInjection()
		{
			var state = new ContextualState();

			state.SetContextUntyped(new IncompatibleContext { Data = "Wrong" });

			Assert.IsFalse(state.HasValidContext);
		}

		[Test]
		public void StateOfT_ImplementsIContextReceiver()
		{
			var state = new ContextualState();

			Assert.IsTrue(state is IContextReceiver);
		}

		[Test]
		public void StateOfT_IContextReceiver_RequiredContextType_Matches()
		{
			IContextReceiver receiver = new ContextualState();

			Assert.AreEqual(typeof(TestContext), receiver.RequiredContextType);
		}

		[Test]
		public void StateOfT_IContextReceiver_SetContextUntyped_Works()
		{
			IContextReceiver receiver = new ContextualState();
			var context = new TestContext { Name = "Interface", Value = 99 };

			receiver.SetContextUntyped(context);

			var state = (ContextualState)receiver;
			Assert.IsNotNull(state.GetSharedContext());
			Assert.AreEqual("Interface", state.GetSharedContext().Name);
		}

		[Test]
		public void StateOfT_OnEnter_CanAccessSharedContext()
		{
			var state = new ContextualState();
			var context = new TestContext { Name = "OnEnterTest", Value = 123 };
			state.SetContextUntyped(context);

			state.OnEnter();

			Assert.IsTrue(state.OnEnterCalled);
			Assert.AreEqual("OnEnterTest", state.CapturedName);
			Assert.AreEqual(123, state.CapturedValue);
		}

		[Test]
		public void StateOfT_OnEnter_WithoutContext_DoesNotThrow()
		{
			var state = new ContextualState();

			Assert.DoesNotThrow(() => state.OnEnter());
			Assert.IsTrue(state.OnEnterCalled);
			Assert.IsNull(state.CapturedName);
			Assert.AreEqual(0, state.CapturedValue);
		}

		[Test]
		public void StateOfT_ContextCanBeReplaced()
		{
			var state = new ContextualState();
			var context1 = new TestContext { Name = "First", Value = 1 };
			var context2 = new TestContext { Name = "Second", Value = 2 };

			state.SetContextUntyped(context1);
			Assert.AreEqual("First", state.GetSharedContext().Name);

			state.SetContextUntyped(context2);
			Assert.AreEqual("Second", state.GetSharedContext().Name);
		}

		[Test]
		public void StateOfT_ContextMutations_AreVisible()
		{
			var state = new ContextualState();
			var context = new TestContext { Name = "Mutable", Value = 10 };
			state.SetContextUntyped(context);

			context.Name = "Changed";
			context.Value = 20;

			Assert.AreEqual("Changed", state.GetSharedContext().Name);
			Assert.AreEqual(20, state.GetSharedContext().Value);
		}

		[Test]
		public void StateOfT_RequiresReferenceType_Constraint()
		{
			// This test verifies the where TContext : class constraint
			// The test passes by compilation - if you could create State<int> it would fail to compile
			var type = typeof(State<>);
			var constraints = type.GetGenericArguments()[0].GetGenericParameterConstraints();

			// TContext : class means it must be a reference type
			Assert.IsTrue(type.GetGenericArguments()[0].GenericParameterAttributes
				.HasFlag(System.Reflection.GenericParameterAttributes.ReferenceTypeConstraint));
		}


	}
}
