using System.Reflection;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.States;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class SequenceGateStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateInfo = typeof(SequenceGateState).GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = typeof(SequenceGateState).GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.LimeGreen, stateStyle.Color);
			Assert.AreEqual(NodeIcon.ClipboardCheck, stateStyle.Icon);
		}

		[Test]
		public void HasConditionADataIn_Required()
		{
			var field = typeof(SequenceGateState).GetField("ConditionA");
			var attr = field.GetCustomAttribute<DataInAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Condition A", attr.DisplayName);
			Assert.IsTrue(attr.Required);
		}

		[Test]
		public void HasOnSuccessTransition()
		{
			var field = typeof(SequenceGateState).GetField("OnSuccess");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Success", attr.DisplayName);
		}

		[Test]
		public void HasOnFailureTransition()
		{
			var field = typeof(SequenceGateState).GetField("OnFailure");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Failure", attr.DisplayName);
		}

		[Test]
		public void ConditionBCD_DefaultToTrue()
		{
			var state = new SequenceGateState();

			Assert.IsTrue(state.ConditionB);
			Assert.IsTrue(state.ConditionC);
			Assert.IsTrue(state.ConditionD);
		}

		[Test]
		public void AllTrue_InvokesOnSuccess()
		{
			var state = new SequenceGateState
			{
				ConditionA = true,
				ConditionB = true,
				ConditionC = true,
				ConditionD = true
			};
			var success = false;
			state.OnSuccess = () => success = true;

			state.OnEnter();

			Assert.IsTrue(success);
			Assert.AreEqual(-1, state.FailedIndex);
		}

		[Test]
		public void FirstFalse_InvokesOnFailure_WithIndex()
		{
			var state = new SequenceGateState
			{
				ConditionA = true,
				ConditionB = false,
				ConditionC = true,
				ConditionD = true
			};
			var failure = false;
			state.OnFailure = () => failure = true;

			state.OnEnter();

			Assert.IsTrue(failure);
			Assert.AreEqual(1, state.FailedIndex);
		}

		[Test]
		public void ConditionA_False_FailsImmediately()
		{
			var state = new SequenceGateState { ConditionA = false };
			var failure = false;
			state.OnFailure = () => failure = true;

			state.OnEnter();

			Assert.IsTrue(failure);
			Assert.AreEqual(0, state.FailedIndex);
		}

		[Test]
		public void OnlyConditionA_True_Succeeds_BecauseDefaultsTrue()
		{
			var state = new SequenceGateState { ConditionA = true };
			var success = false;
			state.OnSuccess = () => success = true;

			state.OnEnter();

			Assert.IsTrue(success);
		}

		[Test]
		public void NullCallbacks_DoesNotThrow()
		{
			var state = new SequenceGateState
			{
				ConditionA = true,
				OnSuccess = null,
				OnFailure = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}
	}

	[TestFixture]
	public class SelectorGateStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateInfo = typeof(SelectorGateState).GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = typeof(SelectorGateState).GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.LimeGreen, stateStyle.Color);
		}

		[Test]
		public void HasOnATransition()
		{
			var field = typeof(SelectorGateState).GetField("OnA");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("A", attr.DisplayName);
		}

		[Test]
		public void HasOnNoneTransition()
		{
			var field = typeof(SelectorGateState).GetField("OnNone");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("None", attr.DisplayName);
		}

		[Test]
		public void AllFalse_InvokesOnNone()
		{
			var state = new SelectorGateState();
			var none = false;
			state.OnNone = () => none = true;

			state.OnEnter();

			Assert.IsTrue(none);
			Assert.AreEqual(-1, state.SelectedIndex);
		}

		[Test]
		public void ConditionA_True_SelectsA()
		{
			var state = new SelectorGateState
			{
				ConditionA = true,
				ConditionB = true
			};
			var a = false;
			state.OnA = () => a = true;

			state.OnEnter();

			Assert.IsTrue(a);
			Assert.AreEqual(0, state.SelectedIndex);
		}

		[Test]
		public void ConditionB_True_WhenA_False_SelectsB()
		{
			var state = new SelectorGateState
			{
				ConditionA = false,
				ConditionB = true
			};
			var b = false;
			state.OnB = () => b = true;

			state.OnEnter();

			Assert.IsTrue(b);
			Assert.AreEqual(1, state.SelectedIndex);
		}

		[Test]
		public void PriorityOrder_HighestFirst()
		{
			var state = new SelectorGateState
			{
				ConditionA = false,
				ConditionB = false,
				ConditionC = true,
				ConditionD = true
			};
			var c = false;
			state.OnC = () => c = true;

			state.OnEnter();

			Assert.IsTrue(c);
			Assert.AreEqual(2, state.SelectedIndex);
		}

		[Test]
		public void NullCallbacks_DoesNotThrow()
		{
			var state = new SelectorGateState
			{
				ConditionA = true,
				OnA = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}
	}

	[TestFixture]
	public class RepeaterStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateInfo = typeof(RepeaterState).GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = typeof(RepeaterState).GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.LimeGreen, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Restart, stateStyle.Icon);
		}

		[Test]
		public void RepeatCountDefaultsToThree()
		{
			var state = new RepeaterState();

			Assert.AreEqual(3, state.RepeatCount);
		}

		[Test]
		public void InfiniteDefaultsToFalse()
		{
			var state = new RepeaterState();

			Assert.IsFalse(state.Infinite);
		}

		[Test]
		public void HasOnLoopTransition()
		{
			var field = typeof(RepeaterState).GetField("OnLoop");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Loop", attr.DisplayName);
		}

		[Test]
		public void HasOnDoneTransition()
		{
			var field = typeof(RepeaterState).GetField("OnDone");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Done", attr.DisplayName);
		}

		[Test]
		public void FirstEntry_InvokesOnLoop()
		{
			var state = new RepeaterState { RepeatCount = 3 };
			var loop = false;
			state.OnLoop = () => loop = true;

			state.OnEnter();

			Assert.IsTrue(loop);
			Assert.AreEqual(1, state.CurrentIteration);
		}

		[Test]
		public void ThirdEntry_WithCountThree_InvokesOnDone()
		{
			var state = new RepeaterState { RepeatCount = 3 };
			var done = false;
			state.OnDone = () => done = true;
			state.OnLoop = () => { };

			state.OnEnter(); // 1
			state.OnEnter(); // 2
			state.OnEnter(); // 3

			Assert.IsTrue(done);
			Assert.AreEqual(3, state.CurrentIteration);
		}

		[Test]
		public void AfterDone_CounterResets()
		{
			var state = new RepeaterState { RepeatCount = 2 };
			var loopCount = 0;
			state.OnLoop = () => loopCount++;
			state.OnDone = () => { };

			state.OnEnter(); // 1 - loop
			state.OnEnter(); // 2 - done
			state.OnEnter(); // 1 again - loop

			Assert.AreEqual(2, loopCount);
		}

		[Test]
		public void Infinite_AlwaysLoops()
		{
			var state = new RepeaterState { Infinite = true, RepeatCount = 1 };
			var loopCount = 0;
			state.OnLoop = () => loopCount++;

			for (var i = 0; i < 10; i++)
			{
				state.OnEnter();
			}

			Assert.AreEqual(10, loopCount);
		}

		[Test]
		public void OnReset_ClearsCounter()
		{
			var state = new RepeaterState { RepeatCount = 3 };
			state.OnLoop = () => { };

			state.OnEnter();
			state.OnEnter();
			state.OnReset();
			state.OnEnter();

			Assert.AreEqual(1, state.CurrentIteration);
		}

		[Test]
		public void NullCallbacks_DoesNotThrow()
		{
			var state = new RepeaterState
			{
				RepeatCount = 1,
				OnLoop = null,
				OnDone = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}
	}

	[TestFixture]
	public class CooldownStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateInfo = typeof(CooldownState).GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = typeof(CooldownState).GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.LimeGreen, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Clock, stateStyle.Icon);
		}

		[Test]
		public void DurationDefaultsToOne()
		{
			var state = new CooldownState();

			Assert.AreEqual(1f, state.Duration);
		}

		[Test]
		public void HasOnReadyTransition()
		{
			var field = typeof(CooldownState).GetField("OnReady");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Ready", attr.DisplayName);
		}

		[Test]
		public void HasOnCooldownTransition()
		{
			var field = typeof(CooldownState).GetField("OnCooldown");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("On Cooldown", attr.DisplayName);
		}

		[Test]
		public void HasRemainingDataOut()
		{
			var field = typeof(CooldownState).GetField("Remaining");
			var attr = field.GetCustomAttribute<DataOutAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Remaining", attr.DisplayName);
		}

		[Test]
		public void NullCallbacks_DoesNotThrow()
		{
			var state = new CooldownState
			{
				OnReady = null,
				OnCooldown = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}
	}

	[TestFixture]
	public class TimeoutStateTests
	{
		[Test]
		public void HasCorrectAttributes()
		{
			var stateInfo = typeof(TimeoutState).GetCustomAttribute<StateInfoAttribute>();
			var stateStyle = typeof(TimeoutState).GetCustomAttribute<StateStyleAttribute>();

			Assert.IsNotNull(stateInfo);
			Assert.IsNotNull(stateStyle);
			Assert.AreEqual(StateColors.LimeGreen, stateStyle.Color);
			Assert.AreEqual(NodeIcon.Hourglass, stateStyle.Icon);
		}

		[Test]
		public void DurationDefaultsToFive()
		{
			var state = new TimeoutState();

			Assert.AreEqual(5f, state.Duration);
		}

		[Test]
		public void HasOnTimeoutTransition()
		{
			var field = typeof(TimeoutState).GetField("OnTimeout");
			var attr = field.GetCustomAttribute<TransitionAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Timeout", attr.DisplayName);
		}

		[Test]
		public void HasElapsedDataOut()
		{
			var field = typeof(TimeoutState).GetField("Elapsed");
			var attr = field.GetCustomAttribute<DataOutAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Elapsed", attr.DisplayName);
		}

		[Test]
		public void HasProgressDataOut()
		{
			var field = typeof(TimeoutState).GetField("Progress");
			var attr = field.GetCustomAttribute<DataOutAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Progress", attr.DisplayName);
		}

		[Test]
		public void ZeroDuration_InvokesOnTimeout()
		{
			var state = new TimeoutState { Duration = 0f };
			var timeout = false;
			state.OnTimeout = () => timeout = true;

			state.OnEnter();

			Assert.IsTrue(timeout);
			Assert.AreEqual(1f, state.Progress);
		}

		[Test]
		public void NegativeDuration_InvokesOnTimeout()
		{
			var state = new TimeoutState { Duration = -1f };
			var timeout = false;
			state.OnTimeout = () => timeout = true;

			state.OnEnter();

			Assert.IsTrue(timeout);
		}

		[Test]
		public void PositiveDuration_DoesNotTimeoutOnEnter()
		{
			var state = new TimeoutState { Duration = 5f };
			var timeout = false;
			state.OnTimeout = () => timeout = true;

			state.OnEnter();

			Assert.IsFalse(timeout);
			Assert.AreEqual(0f, state.Elapsed);
			Assert.AreEqual(0f, state.Progress);
		}

		[Test]
		public void NullCallbacks_DoesNotThrow()
		{
			var state = new TimeoutState
			{
				Duration = 0f,
				OnTimeout = null
			};

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void OnUpdate_BeforeEnter_DoesNotThrow()
		{
			var state = new TimeoutState();

			Assert.DoesNotThrow(() => state.OnUpdate());
		}
	}

	[TestFixture]
	public class RepeaterStateEdgeCaseTests
	{
		[Test]
		public void RepeatCount_Zero_ImmediatelyDone()
		{
			var state = new RepeaterState { RepeatCount = 0 };
			var done = false;
			state.OnDone = () => done = true;
			state.OnLoop = () => { };

			state.OnEnter();

			Assert.IsTrue(done, "RepeatCount=0 should immediately fire Done");
		}

		[Test]
		public void RepeatCount_One_ImmediatelyDone()
		{
			var state = new RepeaterState { RepeatCount = 1 };
			var done = false;
			state.OnDone = () => done = true;
			state.OnLoop = () => { };

			state.OnEnter();

			Assert.IsTrue(done, "RepeatCount=1 should immediately fire Done on first entry");
		}

		[Test]
		public void RepeatCount_Negative_ImmediatelyDone()
		{
			var state = new RepeaterState { RepeatCount = -5 };
			var done = false;
			state.OnDone = () => done = true;
			state.OnLoop = () => { };

			state.OnEnter();

			Assert.IsTrue(done, "Negative RepeatCount should immediately fire Done");
		}

		[Test]
		public void RepeatCount_Two_LoopsThenDone()
		{
			var state = new RepeaterState { RepeatCount = 2 };
			var looped = false;
			var done = false;
			state.OnLoop = () => looped = true;
			state.OnDone = () => done = true;

			state.OnEnter();
			Assert.IsTrue(looped, "First entry should loop");
			Assert.IsFalse(done, "First entry should not be done");

			looped = false;
			state.OnEnter();
			Assert.IsTrue(done, "Second entry should be done");
		}

		[Test]
		public void Infinite_IgnoresRepeatCount_Zero()
		{
			var state = new RepeaterState { Infinite = true, RepeatCount = 0 };
			var loopCount = 0;
			state.OnLoop = () => loopCount++;

			for (var i = 0; i < 5; i++)
			{
				state.OnEnter();
			}

			Assert.AreEqual(5, loopCount, "Infinite mode should loop regardless of RepeatCount=0");
		}

		[Test]
		public void Infinite_IgnoresRepeatCount_Negative()
		{
			var state = new RepeaterState { Infinite = true, RepeatCount = -1 };
			var loopCount = 0;
			state.OnLoop = () => loopCount++;

			for (var i = 0; i < 3; i++)
			{
				state.OnEnter();
			}

			Assert.AreEqual(3, loopCount, "Infinite mode should loop regardless of negative RepeatCount");
		}
	}

	[TestFixture]
	public class TimeoutStateFloatingPointTests
	{
		[Test]
		public void Duration_NaN_DoesNotTimeoutOnEnter()
		{
			// NaN <= 0f is false, so OnEnter should NOT trigger immediate timeout
			var state = new TimeoutState { Duration = float.NaN };
			var timeout = false;
			state.OnTimeout = () => timeout = true;

			state.OnEnter();

			Assert.IsFalse(timeout, "NaN duration should not trigger immediate timeout (NaN <= 0 is false)");
		}

		[Test]
		public void Duration_NaN_OnUpdate_DoesNotThrow()
		{
			var state = new TimeoutState { Duration = float.NaN };
			state.OnTimeout = () => { };

			state.OnEnter();

			// OnUpdate with NaN duration should not throw
			Assert.DoesNotThrow(() => state.OnUpdate());
		}

		[Test]
		public void Duration_NaN_Progress_IsNaN()
		{
			var state = new TimeoutState { Duration = float.NaN };
			state.OnTimeout = () => { };

			state.OnEnter();
			state.OnUpdate();

			// Clamp01 with NaN input returns 0 in Unity
			Assert.AreEqual(0f, state.Progress,
				"Progress should be 0 when duration is NaN (Clamp01 returns 0 for NaN)");
		}

		[Test]
		public void Duration_NegativeInfinity_TriggersImmediateTimeout()
		{
			var state = new TimeoutState { Duration = float.NegativeInfinity };
			var timeout = false;
			state.OnTimeout = () => timeout = true;

			state.OnEnter();

			Assert.IsTrue(timeout, "NegativeInfinity duration should trigger immediate timeout");
		}

		[Test]
		public void Duration_PositiveInfinity_DoesNotTimeout()
		{
			var state = new TimeoutState { Duration = float.PositiveInfinity };
			var timeout = false;
			state.OnTimeout = () => timeout = true;

			state.OnEnter();

			Assert.IsFalse(timeout, "PositiveInfinity duration should not trigger immediate timeout");
			Assert.AreEqual(0f, state.Progress);
		}

		[Test]
		public void Duration_Epsilon_DoesNotTriggerImmediateTimeout()
		{
			// float.Epsilon > 0, so Duration <= 0f check is false
			var state = new TimeoutState { Duration = float.Epsilon };
			var timeout = false;
			state.OnTimeout = () => timeout = true;

			state.OnEnter();

			Assert.IsFalse(timeout, "Epsilon duration should not trigger immediate timeout (Epsilon > 0)");
		}

		[Test]
		public void Duration_MaxValue_DoesNotTimeout()
		{
			var state = new TimeoutState { Duration = float.MaxValue };
			var timeout = false;
			state.OnTimeout = () => timeout = true;

			state.OnEnter();

			Assert.IsFalse(timeout);
			Assert.AreEqual(0f, state.Progress);
		}

		[Test]
		public void Duration_VerySmallNegative_TriggersImmediateTimeout()
		{
			var state = new TimeoutState { Duration = -float.Epsilon };
			var timeout = false;
			state.OnTimeout = () => timeout = true;

			state.OnEnter();

			Assert.IsTrue(timeout, "Negative epsilon should trigger immediate timeout");
			Assert.AreEqual(1f, state.Progress);
		}
	}

	[TestFixture]
	public class CooldownStateFloatingPointTests
	{
		[Test]
		public void Duration_Zero_AlwaysReady()
		{
			var state = new CooldownState { Duration = 0f };
			var ready = false;
			state.OnReady = () => ready = true;

			state.OnEnter();

			Assert.IsTrue(ready, "Duration=0 should always be ready (elapsed >= 0 is true)");
			Assert.AreEqual(0f, state.Remaining);
		}

		[Test]
		public void Duration_NaN_DoesNotThrow()
		{
			var state = new CooldownState { Duration = float.NaN };
			state.OnReady = () => { };
			state.OnCooldown = () => { };

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void Duration_NegativeInfinity_AlwaysReady()
		{
			// elapsed (Inf) >= NegInf is true
			var state = new CooldownState { Duration = float.NegativeInfinity };
			var ready = false;
			state.OnReady = () => ready = true;

			state.OnEnter();

			Assert.IsTrue(ready, "NegativeInfinity duration: elapsed is always >= NegInf");
		}

		[Test]
		public void Duration_Negative_AlwaysReady()
		{
			// Any negative duration means elapsed is always >= Duration
			var state = new CooldownState { Duration = -5f };
			var ready = false;
			state.OnReady = () => ready = true;

			state.OnEnter();

			Assert.IsTrue(ready, "Negative duration should always be ready");
		}

		[Test]
		public void OnReset_ClearsLastActivation()
		{
			var state = new CooldownState { Duration = 1f };
			var readyCount = 0;
			state.OnReady = () => readyCount++;

			state.OnEnter(); // First call always ready (_lastActivation = NegInf)
			state.OnReset();
			state.OnEnter(); // After reset, should be ready again

			Assert.AreEqual(2, readyCount, "After OnReset, cooldown should be cleared");
		}
	}

	[TestFixture]
	public class BehaviorTreeStatesCommonTests
	{
		[Test]
		public void AllBTStates_HaveLimeGreenColor()
		{
			var stateTypes = new[]
			{
				typeof(SequenceGateState),
				typeof(SelectorGateState),
				typeof(RepeaterState),
				typeof(CooldownState),
				typeof(TimeoutState)
			};

			foreach (var stateType in stateTypes)
			{
				var stateStyle = stateType.GetCustomAttribute<StateStyleAttribute>();
				Assert.IsNotNull(stateStyle, $"{stateType.Name} should have StateStyleAttribute");
				Assert.AreEqual(StateColors.LimeGreen, stateStyle.Color,
					$"{stateType.Name} should have LimeGreen color");
			}
		}

		[Test]
		public void AllBTStates_HaveStateInfoAttribute()
		{
			var stateTypes = new[]
			{
				typeof(SequenceGateState),
				typeof(SelectorGateState),
				typeof(RepeaterState),
				typeof(CooldownState),
				typeof(TimeoutState)
			};

			foreach (var stateType in stateTypes)
			{
				var stateInfo = stateType.GetCustomAttribute<StateInfoAttribute>();
				Assert.IsNotNull(stateInfo, $"{stateType.Name} should have StateInfoAttribute");
				Assert.IsFalse(string.IsNullOrEmpty(stateInfo.Tooltip),
					$"{stateType.Name} should have a tooltip");
			}
		}
	}
}
