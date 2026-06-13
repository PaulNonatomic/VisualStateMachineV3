using NUnit.Framework;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class DestroyStateTests
	{
		[Test]
		public void DestroyState_DefaultTarget_IsNull()
		{
			var state = new DestroyState();

			Assert.IsNull(state.Target);
		}

		[Test]
		public void DestroyState_DefaultDelay_IsZero()
		{
			var state = new DestroyState();

			Assert.AreEqual(0f, state.Delay);
		}

		[Test]
		public void DestroyState_DefaultOnComplete_IsNull()
		{
			var state = new DestroyState();

			Assert.IsNull(state.OnComplete);
		}

		[Test]
		public void DestroyState_WithNullTargetAndNoContext_LogsWarningAndTransitions()
		{
			var completeCalled = false;
			var state = new DestroyState
			{
				Target = null,
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[DestroyState] No target GameObject to destroy");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void DestroyState_WithNullTargetAndNoContext_NullOnComplete_DoesNotThrow()
		{
			var state = new DestroyState
			{
				Target = null,
				OnComplete = null
			};

			LogAssert.Expect(LogType.Warning, "[DestroyState] No target GameObject to destroy");
			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void DestroyState_WithExplicitTarget_DestroysTarget()
		{
			var target = new GameObject("DestroyStateTestTarget");
			var completeCalled = false;
			var state = new DestroyState
			{
				Target = target,
				OnComplete = () => completeCalled = true
			};

			state.OnEnter();

			Assert.IsTrue(completeCalled);
			// In EditMode, Destroy is deferred. The object is marked for destruction.
			// Use DestroyImmediate in teardown to clean up if Destroy hasn't run yet.
			Object.DestroyImmediate(target);
		}

		[Test]
		public void DestroyState_WithDelay_InvokesOnCompleteImmediately()
		{
			var target = new GameObject("DestroyStateDelayTarget");
			var completeCalled = false;
			var state = new DestroyState
			{
				Target = target,
				Delay = 5f,
				OnComplete = () => completeCalled = true
			};

			state.OnEnter();

			Assert.IsTrue(completeCalled, "OnComplete fires immediately even with delay");
			Object.DestroyImmediate(target);
		}

		[Test]
		public void DestroyState_WithZeroDelay_UsesImmediateDestroy()
		{
			var target = new GameObject("DestroyStateZeroDelayTarget");
			var state = new DestroyState
			{
				Target = target,
				Delay = 0f
			};

			state.OnEnter();

			// Cleanup
			if (target != null) Object.DestroyImmediate(target);
		}

		[Test]
		public void DestroyState_WithNullOnComplete_DoesNotThrow()
		{
			var target = new GameObject("DestroyStateNullOnCompleteTarget");
			var state = new DestroyState
			{
				Target = target,
				OnComplete = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
			Object.DestroyImmediate(target);
		}
	}

	[TestFixture]
	public class DestroyImmediateStateTests
	{
		[Test]
		public void DestroyImmediateState_DefaultTarget_IsNull()
		{
			var state = new DestroyImmediateState();

			Assert.IsNull(state.Target);
		}

		[Test]
		public void DestroyImmediateState_DefaultOnComplete_IsNull()
		{
			var state = new DestroyImmediateState();

			Assert.IsNull(state.OnComplete);
		}

		[Test]
		public void DestroyImmediateState_WithNullTargetAndNoContext_LogsWarningAndTransitions()
		{
			var completeCalled = false;
			var state = new DestroyImmediateState
			{
				Target = null,
				OnComplete = () => completeCalled = true
			};

			LogAssert.Expect(LogType.Warning, "[DestroyImmediateState] No target GameObject to destroy");
			state.OnEnter();

			Assert.IsTrue(completeCalled);
		}

		[Test]
		public void DestroyImmediateState_WithExplicitTarget_DestroysTargetImmediately()
		{
			var target = new GameObject("DestroyImmediateTestTarget");
			var completeCalled = false;
			var state = new DestroyImmediateState
			{
				Target = target,
				OnComplete = () => completeCalled = true
			};

			state.OnEnter();

			Assert.IsTrue(completeCalled);
			Assert.IsTrue(target == null, "Target should be destroyed immediately");
		}

		[Test]
		public void DestroyImmediateState_WithNullOnComplete_DoesNotThrow()
		{
			var target = new GameObject("DestroyImmediateNullOnCompleteTarget");
			var state = new DestroyImmediateState
			{
				Target = target,
				OnComplete = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void DestroyImmediateState_WithNullTargetAndNoContext_NullOnComplete_DoesNotThrow()
		{
			var state = new DestroyImmediateState
			{
				Target = null,
				OnComplete = null
			};

			LogAssert.Expect(LogType.Warning, "[DestroyImmediateState] No target GameObject to destroy");
			Assert.DoesNotThrow(() => state.OnEnter());
		}
	}
}
