using System;
using NUnit.Framework;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class StateOfTTests
	{

		private class TestContext
		{
			public string Name { get; set; }
			public int Value { get; set; }
		}

		private class DerivedContext : TestContext
		{
			public float Extra { get; set; }
		}

		private class OtherContext
		{
			public bool Flag { get; set; }
		}

		private class TestContextState : State<TestContext>
		{
			public TestContext ExposedContext => SharedContext;

			public override void OnEnter() { }
		}

		private class DerivedContextState : State<DerivedContext>
		{
			public DerivedContext ExposedContext => SharedContext;

			public override void OnEnter() { }
		}



		[Test]
		public void RequiredContextType_ReturnsGenericTypeArgument()
		{
			var state = new TestContextState();

			Assert.AreEqual(typeof(TestContext), state.RequiredContextType);
		}

		[Test]
		public void RequiredContextType_DerivedContext_ReturnsDerivedType()
		{
			var state = new DerivedContextState();

			Assert.AreEqual(typeof(DerivedContext), state.RequiredContextType);
		}



		[Test]
		public void ImplementsIContextReceiver()
		{
			var state = new TestContextState();

			Assert.IsInstanceOf<IContextReceiver>(state);
		}

		[Test]
		public void IContextReceiver_RequiredContextType_MatchesGenericArg()
		{
			IContextReceiver receiver = new TestContextState();

			Assert.AreEqual(typeof(TestContext), receiver.RequiredContextType);
		}



		[Test]
		public void SetContextUntyped_CompatibleType_SetsContext()
		{
			var state = new TestContextState();
			var context = new TestContext { Name = "test", Value = 42 };

			state.SetContextUntyped(context);

			Assert.IsNotNull(state.ExposedContext);
			Assert.AreEqual("test", state.ExposedContext.Name);
			Assert.AreEqual(42, state.ExposedContext.Value);
		}

		[Test]
		public void SetContextUntyped_DerivedType_SetsContext()
		{
			var state = new TestContextState();
			var context = new DerivedContext { Name = "derived", Value = 10, Extra = 3.14f };

			state.SetContextUntyped(context);

			Assert.IsNotNull(state.ExposedContext);
			Assert.AreEqual("derived", state.ExposedContext.Name);
		}

		[Test]
		public void SetContextUntyped_IncompatibleType_SetsNull()
		{
			var state = new TestContextState();
			var context = new OtherContext { Flag = true };

			state.SetContextUntyped(context);

			Assert.IsNull(state.ExposedContext);
		}

		[Test]
		public void SetContextUntyped_Null_SetsNull()
		{
			var state = new TestContextState();
			var context = new TestContext { Name = "initial" };
			state.SetContextUntyped(context);
			Assert.IsNotNull(state.ExposedContext);

			state.SetContextUntyped(null);

			Assert.IsNull(state.ExposedContext);
		}

		[Test]
		public void SetContextUntyped_StringObject_SetsNull()
		{
			// string is a class but not TContext
			var state = new TestContextState();

			state.SetContextUntyped("not a context");

			Assert.IsNull(state.ExposedContext);
		}



		[Test]
		public void SetSharedContext_SetsContext()
		{
			var state = new TestContextState();
			var context = new TestContext { Name = "internal", Value = 99 };

			state.SetSharedContext(context);

			Assert.IsNotNull(state.ExposedContext);
			Assert.AreEqual("internal", state.ExposedContext.Name);
			Assert.AreEqual(99, state.ExposedContext.Value);
		}

		[Test]
		public void SetSharedContext_Null_ClearsContext()
		{
			var state = new TestContextState();
			state.SetSharedContext(new TestContext { Name = "initial" });

			state.SetSharedContext(null);

			Assert.IsNull(state.ExposedContext);
		}

		[Test]
		public void SetSharedContext_ReplacesExistingContext()
		{
			var state = new TestContextState();
			state.SetSharedContext(new TestContext { Name = "first" });

			state.SetSharedContext(new TestContext { Name = "second" });

			Assert.AreEqual("second", state.ExposedContext.Name);
		}



		[Test]
		public void HasValidContext_NoContextSet_ReturnsFalse()
		{
			var state = new TestContextState();

			Assert.IsFalse(state.HasValidContext);
		}

		[Test]
		public void HasValidContext_ContextSet_ReturnsTrue()
		{
			var state = new TestContextState();
			state.SetSharedContext(new TestContext());

			Assert.IsTrue(state.HasValidContext);
		}

		[Test]
		public void HasValidContext_ContextCleared_ReturnsFalse()
		{
			var state = new TestContextState();
			state.SetSharedContext(new TestContext());
			Assert.IsTrue(state.HasValidContext);

			state.SetSharedContext(null);

			Assert.IsFalse(state.HasValidContext);
		}

		[Test]
		public void HasValidContext_AfterSetContextUntyped_Incompatible_ReturnsFalse()
		{
			var state = new TestContextState();

			state.SetContextUntyped(new OtherContext());

			Assert.IsFalse(state.HasValidContext);
		}

		[Test]
		public void HasValidContext_AfterSetContextUntyped_Compatible_ReturnsTrue()
		{
			var state = new TestContextState();

			state.SetContextUntyped(new TestContext());

			Assert.IsTrue(state.HasValidContext);
		}



		[Test]
		public void SharedContext_IsProtected_AccessibleInDerivedClass()
		{
			// Verify that ExposedContext (which accesses protected SharedContext) works
			var state = new TestContextState();
			var context = new TestContext { Name = "protected-test" };
			state.SetSharedContext(context);

			Assert.AreEqual("protected-test", state.ExposedContext.Name);
		}

		[Test]
		public void SharedContext_MutationsAreShared()
		{
			// Verify that context is shared by reference, not copied
			var state = new TestContextState();
			var context = new TestContext { Name = "original", Value = 0 };
			state.SetSharedContext(context);

			// Mutate via the original reference
			context.Value = 100;

			// State should see the mutation
			Assert.AreEqual(100, state.ExposedContext.Value);
		}

		[Test]
		public void SharedContext_SameInstance_SharedBetweenStates()
		{
			var state1 = new TestContextState();
			var state2 = new TestContextState();
			var context = new TestContext { Name = "shared" };

			state1.SetSharedContext(context);
			state2.SetSharedContext(context);

			// Mutate via state1
			state1.ExposedContext.Value = 42;

			// state2 should see the same change
			Assert.AreEqual(42, state2.ExposedContext.Value);
			Assert.AreSame(state1.ExposedContext, state2.ExposedContext);
		}



		[Test]
		public void InheritsFromState()
		{
			var state = new TestContextState();

			Assert.IsInstanceOf<State>(state);
		}

		[Test]
		public void DerivedContextState_RequiredContextType_ReturnsDerivedType()
		{
			var state = new DerivedContextState();

			Assert.AreEqual(typeof(DerivedContext), state.RequiredContextType);
		}

		[Test]
		public void DerivedContextState_SetContextUntyped_WithBaseType_SetsNull()
		{
			// DerivedContextState requires DerivedContext, not just TestContext
			var state = new DerivedContextState();
			var baseContext = new TestContext { Name = "base" };

			state.SetContextUntyped(baseContext);

			// TestContext is NOT assignable to DerivedContext (upcast fails)
			Assert.IsNull(state.ExposedContext);
		}

		[Test]
		public void DerivedContextState_SetContextUntyped_WithDerivedType_Works()
		{
			var state = new DerivedContextState();
			var context = new DerivedContext { Name = "derived", Extra = 1.5f };

			state.SetContextUntyped(context);

			Assert.IsNotNull(state.ExposedContext);
			Assert.AreEqual(1.5f, state.ExposedContext.Extra, 0.001f);
		}

	}
}
