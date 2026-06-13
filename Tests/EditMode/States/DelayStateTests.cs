using System;
using NUnit.Framework;
using Nonatomic.VSM.States;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class DelayStateTests
	{
		[Test]
		public void DelayState_DefaultDuration_IsOne()
		{
			var state = new DelayState();

			Assert.AreEqual(1f, state.Duration, "Default duration should be 1 second");
		}

		[Test]
		public void DelayState_DefaultUseUnscaledTime_IsTrue()
		{
			var state = new DelayState();

			Assert.IsTrue(state.UseUnscaledTime, "Default UseUnscaledTime should be true");
		}

		[Test]
		public void DelayState_OnEnter_InitializesElapsedTimeToZero()
		{
			var state = new DelayState();
			state.Duration = 5f;
			state.ElapsedTime = 10f;

			state.OnEnter();

			Assert.AreEqual(0f, state.ElapsedTime, "ElapsedTime should be reset to 0 on enter");
		}

		[Test]
		public void DelayState_OnEnter_InitializesProgressToZero()
		{
			var state = new DelayState();
			state.Duration = 5f;
			state.Progress = 0.5f;

			state.OnEnter();

			Assert.AreEqual(0f, state.Progress, "Progress should be reset to 0 on enter");
		}

		[Test]
		public void DelayState_WhenDurationIsZero_CompletesImmediately()
		{
			var state = new DelayState();
			state.Duration = 0f;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.IsTrue(completeCalled, "OnComplete should be called immediately when duration is 0");
		}

		[Test]
		public void DelayState_WhenDurationIsNegative_CompletesImmediately()
		{
			var state = new DelayState();
			state.Duration = -1f;
			var completeCalled = false;
			state.OnComplete = () => completeCalled = true;

			state.OnEnter();

			Assert.IsTrue(completeCalled, "OnComplete should be called immediately when duration is negative");
		}

		[Test]
		public void DelayState_WhenDurationIsZero_SetsProgressToOne()
		{
			var state = new DelayState();
			state.Duration = 0f;

			state.OnEnter();

			Assert.AreEqual(1f, state.Progress, "Progress should be 1 when duration is 0");
		}

		[Test]
		public void DelayState_WhenDurationIsZero_OnUpdate_DoesNothing()
		{
			var state = new DelayState();
			state.Duration = 0f;
			var onCompleteCalled = false;
			state.OnComplete = () => onCompleteCalled = true;

			state.OnEnter();
			onCompleteCalled = false;

			state.OnUpdate();

			Assert.IsFalse(onCompleteCalled, "OnUpdate should do nothing when duration is 0");
		}

		[Test]
		public void DelayState_WithNullOnComplete_DoesNotThrow()
		{
			var state = new DelayState();
			state.Duration = 0f;
			state.OnComplete = null;

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void DelayState_ElapsedTimeOutput_IsAccessible()
		{
			var state = new DelayState();

			Assert.AreEqual(0f, state.ElapsedTime);
			state.ElapsedTime = 1.5f;
			Assert.AreEqual(1.5f, state.ElapsedTime);
		}

		[Test]
		public void DelayState_ProgressOutput_IsAccessible()
		{
			var state = new DelayState();

			Assert.AreEqual(0f, state.Progress);
			state.Progress = 0.5f;
			Assert.AreEqual(0.5f, state.Progress);
		}
	}
}
