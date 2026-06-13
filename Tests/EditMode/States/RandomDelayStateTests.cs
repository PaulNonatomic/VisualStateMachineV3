using System;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.States;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class RandomDelayStateTests
	{
		[Test]
		public void DefaultMinDuration_IsHalfSecond()
		{
			var state = new RandomDelayState();

			Assert.AreEqual(0.5f, state.MinDuration, 0.001f);
		}

		[Test]
		public void DefaultMaxDuration_IsTwoSeconds()
		{
			var state = new RandomDelayState();

			Assert.AreEqual(2f, state.MaxDuration, 0.001f);
		}

		[Test]
		public void DefaultUseUnscaledTime_IsTrue()
		{
			var state = new RandomDelayState();

			Assert.IsTrue(state.UseUnscaledTime);
		}

		[Test]
		public void DefaultSelectedDuration_IsZero()
		{
			var state = new RandomDelayState();

			Assert.AreEqual(0f, state.SelectedDuration, 0.001f);
		}

		[Test]
		public void DefaultElapsedTime_IsZero()
		{
			var state = new RandomDelayState();

			Assert.AreEqual(0f, state.ElapsedTime, 0.001f);
		}

		[Test]
		public void DefaultProgress_IsZero()
		{
			var state = new RandomDelayState();

			Assert.AreEqual(0f, state.Progress, 0.001f);
		}

		[Test]
		public void OnEnter_ResetsElapsedTime()
		{
			var state = new RandomDelayState();
			state.MinDuration = 1f;
			state.MaxDuration = 2f;
			state.ElapsedTime = 99f;

			state.OnEnter();

			Assert.AreEqual(0f, state.ElapsedTime, 0.001f);
		}

		[Test]
		public void OnEnter_ResetsProgress()
		{
			var state = new RandomDelayState();
			state.MinDuration = 1f;
			state.MaxDuration = 2f;
			state.Progress = 0.75f;

			state.OnEnter();

			Assert.AreEqual(0f, state.Progress, 0.001f);
		}

		[Test]
		public void OnEnter_SelectedDuration_WithinRange()
		{
			var state = new RandomDelayState();
			state.MinDuration = 1f;
			state.MaxDuration = 3f;

			for (var i = 0; i < 100; i++)
			{
				state.OnEnter();

				Assert.GreaterOrEqual(state.SelectedDuration, 1f,
					$"Iteration {i}: SelectedDuration should be >= MinDuration");
				Assert.LessOrEqual(state.SelectedDuration, 3f,
					$"Iteration {i}: SelectedDuration should be <= MaxDuration");
			}
		}

		[Test]
		public void OnEnter_SwappedMinMax_StillWorksCorrectly()
		{
			var state = new RandomDelayState();
			state.MinDuration = 5f;
			state.MaxDuration = 1f;

			state.OnEnter();

			Assert.GreaterOrEqual(state.SelectedDuration, 1f);
			Assert.LessOrEqual(state.SelectedDuration, 5f);
		}

		[Test]
		public void OnEnter_EqualMinMax_SelectsExactDuration()
		{
			var state = new RandomDelayState();
			state.MinDuration = 2f;
			state.MaxDuration = 2f;

			state.OnEnter();

			Assert.AreEqual(2f, state.SelectedDuration, 0.001f);
		}

		[Test]
		public void OnEnter_ProducesVariedDurations()
		{
			var durations = new float[50];

			for (var i = 0; i < durations.Length; i++)
			{
				var state = new RandomDelayState();
				state.MinDuration = 1f;
				state.MaxDuration = 5f;

				state.OnEnter();
				durations[i] = state.SelectedDuration;
			}

			var minSelected = float.MaxValue;
			var maxSelected = float.MinValue;
			foreach (var d in durations)
			{
				if (d < minSelected) minSelected = d;
				if (d > maxSelected) maxSelected = d;
			}

			var range = maxSelected - minSelected;
			Assert.Greater(range, 0.5f, "Random delays should have reasonable variation");
		}

		[Test]
		public void OnEnter_ZeroDuration_InvokesComplete()
		{
			var state = new RandomDelayState();
			state.MinDuration = 0f;
			state.MaxDuration = 0f;
			var completed = false;
			state.OnComplete = () => completed = true;

			state.OnEnter();

			Assert.IsTrue(completed);
		}

		[Test]
		public void OnEnter_ZeroDuration_SetsProgressToOne()
		{
			var state = new RandomDelayState();
			state.MinDuration = 0f;
			state.MaxDuration = 0f;
			state.OnComplete = () => { };

			state.OnEnter();

			Assert.AreEqual(1f, state.Progress, 0.001f);
		}

		[Test]
		public void OnEnter_NegativeDuration_InvokesComplete()
		{
			var state = new RandomDelayState();
			state.MinDuration = -1f;
			state.MaxDuration = -0.5f;
			var completed = false;
			state.OnComplete = () => completed = true;

			state.OnEnter();

			Assert.IsTrue(completed);
		}

		[Test]
		public void OnEnter_ZeroDuration_NullCallback_DoesNotThrow()
		{
			var state = new RandomDelayState();
			state.MinDuration = 0f;
			state.MaxDuration = 0f;
			state.OnComplete = null;

			Assert.DoesNotThrow(() => state.OnEnter());
		}

		[Test]
		public void OnUpdate_ZeroDuration_ReturnsImmediately()
		{
			var state = new RandomDelayState();
			state.MinDuration = 0f;
			state.MaxDuration = 0f;
			state.OnComplete = () => { };
			state.OnEnter();

			Assert.DoesNotThrow(() => state.OnUpdate());
		}

		[Test]
		public void OnUpdate_PositiveDuration_DoesNotThrow()
		{
			var state = new RandomDelayState();
			state.MinDuration = 1f;
			state.MaxDuration = 2f;
			state.OnEnter();

			Assert.DoesNotThrow(() => state.OnUpdate());
		}

		[Test]
		public void MinDuration_HasDataInAttribute()
		{
			var field = typeof(RandomDelayState).GetField("MinDuration");

			Assert.IsNotNull(field);
			var attr = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attr.Length);
		}

		[Test]
		public void MaxDuration_HasDataInAttribute()
		{
			var field = typeof(RandomDelayState).GetField("MaxDuration");

			Assert.IsNotNull(field);
			var attr = field.GetCustomAttributes(typeof(DataInAttribute), false);
			Assert.AreEqual(1, attr.Length);
		}

		[Test]
		public void SelectedDuration_HasDataOutAttribute()
		{
			var field = typeof(RandomDelayState).GetField("SelectedDuration");

			Assert.IsNotNull(field);
			var attr = field.GetCustomAttributes(typeof(DataOutAttribute), false);
			Assert.AreEqual(1, attr.Length);
		}

		[Test]
		public void OnComplete_HasTransitionAttribute()
		{
			var field = typeof(RandomDelayState).GetField("OnComplete");

			Assert.IsNotNull(field);
			var attr = field.GetCustomAttributes(typeof(TransitionAttribute), false);
			Assert.AreEqual(1, attr.Length);
		}

		[Test]
		public void OnEnter_CalledMultipleTimes_ResetsEachTime()
		{
			var state = new RandomDelayState();
			state.MinDuration = 1f;
			state.MaxDuration = 2f;

			state.OnEnter();
			Assert.AreEqual(0f, state.ElapsedTime, 0.001f);

			state.ElapsedTime = 1.5f;
			state.Progress = 0.8f;

			state.OnEnter();
			Assert.AreEqual(0f, state.ElapsedTime, 0.001f);
			Assert.AreEqual(0f, state.Progress, 0.001f);
			Assert.GreaterOrEqual(state.SelectedDuration, 1f);
			Assert.LessOrEqual(state.SelectedDuration, 2f);
		}
	}
}
