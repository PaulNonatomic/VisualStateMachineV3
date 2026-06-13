using System.Collections;
using NUnit.Framework;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.PlayMode.States
{
	[TestFixture]
	public class RandomDelayStateTimingTests
	{
		[UnityTest]
		public IEnumerator RandomDelayState_CompletesAfterDuration()
		{
			var state = new RandomDelayState();
			state.MinDuration = 0.2f;
			state.MaxDuration = 0.2f;
			state.UseUnscaledTime = true;
			var completed = false;
			state.OnComplete = () => completed = true;

			state.OnEnter();

			Assert.IsFalse(completed, "Should not complete immediately");

			// Simulate frames by calling OnUpdate over time
			var elapsed = 0f;
			var maxTime = 1f;
			while (!completed && elapsed < maxTime)
			{
				state.OnUpdate();
				elapsed += Time.unscaledDeltaTime;
				yield return null;
			}

			Assert.IsTrue(completed, "Should complete after duration elapses");
		}

		[UnityTest]
		public IEnumerator RandomDelayState_ProgressIncreasesOverTime()
		{
			var state = new RandomDelayState();
			state.MinDuration = 0.5f;
			state.MaxDuration = 0.5f;
			state.UseUnscaledTime = true;
			state.OnComplete = () => { };

			state.OnEnter();

			// Skip the first frame (state skips first frame internally)
			state.OnUpdate();
			yield return null;

			var previousProgress = state.Progress;

			// Run a few more frames
			for (var i = 0; i < 5; i++)
			{
				state.OnUpdate();
				yield return null;
			}

			Assert.Greater(state.Progress, previousProgress,
				"Progress should increase over time");
			Assert.GreaterOrEqual(state.Progress, 0f, "Progress should not be negative");
			Assert.LessOrEqual(state.Progress, 1f, "Progress should not exceed 1");
		}

		[UnityTest]
		public IEnumerator RandomDelayState_ElapsedTimeIncreasesOverTime()
		{
			var state = new RandomDelayState();
			state.MinDuration = 1f;
			state.MaxDuration = 1f;
			state.UseUnscaledTime = true;
			state.OnComplete = () => { };

			state.OnEnter();

			Assert.AreEqual(0f, state.ElapsedTime, 0.001f, "ElapsedTime should start at 0");

			// Skip first frame
			state.OnUpdate();
			yield return null;

			// Run a few more frames to accumulate time
			for (var i = 0; i < 5; i++)
			{
				state.OnUpdate();
				yield return null;
			}

			Assert.Greater(state.ElapsedTime, 0f, "ElapsedTime should increase over frames");
		}

		[UnityTest]
		public IEnumerator RandomDelayState_SkipsFirstFrame()
		{
			var state = new RandomDelayState();
			state.MinDuration = 0.01f;
			state.MaxDuration = 0.01f;
			state.UseUnscaledTime = true;
			var completed = false;
			state.OnComplete = () => completed = true;

			state.OnEnter();

			// First OnUpdate should be skipped (no progress)
			state.OnUpdate();
			yield return null;

			// Even with a very short duration, the first frame should be skipped
			// so ElapsedTime should still be 0 after first OnUpdate
			// The completion may happen on the second OnUpdate
			Assert.AreEqual(0f, state.ElapsedTime, 0.001f,
				"ElapsedTime should be 0 after first frame (skipped)");
		}

		[UnityTest]
		public IEnumerator RandomDelayState_ZeroDuration_CompletesImmediately()
		{
			var state = new RandomDelayState();
			state.MinDuration = 0f;
			state.MaxDuration = 0f;
			var completed = false;
			state.OnComplete = () => completed = true;

			state.OnEnter();

			yield return null;

			Assert.IsTrue(completed, "Zero duration should complete immediately in OnEnter");
			Assert.AreEqual(1f, state.Progress, 0.001f, "Progress should be 1 for zero duration");
		}

		[UnityTest]
		public IEnumerator RandomDelayState_ProgressReachesOneOnCompletion()
		{
			var state = new RandomDelayState();
			state.MinDuration = 0.1f;
			state.MaxDuration = 0.1f;
			state.UseUnscaledTime = true;
			var completed = false;
			state.OnComplete = () => completed = true;

			state.OnEnter();

			var elapsed = 0f;
			var maxTime = 2f;
			while (!completed && elapsed < maxTime)
			{
				state.OnUpdate();
				elapsed += Time.unscaledDeltaTime;
				yield return null;
			}

			Assert.IsTrue(completed, "Should have completed");
			Assert.AreEqual(1f, state.Progress, 0.001f,
				"Progress should be 1.0 when completed");
		}

		[UnityTest]
		public IEnumerator RandomDelayState_SelectedDuration_WithinRange()
		{
			for (var i = 0; i < 20; i++)
			{
				var state = new RandomDelayState();
				state.MinDuration = 0.5f;
				state.MaxDuration = 1.5f;

				state.OnEnter();

				Assert.GreaterOrEqual(state.SelectedDuration, 0.5f,
					$"Iteration {i}: SelectedDuration should be >= MinDuration");
				Assert.LessOrEqual(state.SelectedDuration, 1.5f,
					$"Iteration {i}: SelectedDuration should be <= MaxDuration");
			}

			yield return null;
		}

		[UnityTest]
		public IEnumerator RandomDelayState_OnExit_ResetsStartedFlag()
		{
			var state = new RandomDelayState();
			state.MinDuration = 0.5f;
			state.MaxDuration = 0.5f;
			state.UseUnscaledTime = true;
			state.OnComplete = () => { };

			state.OnEnter();

			// Run a couple frames
			state.OnUpdate();
			yield return null;
			state.OnUpdate();
			yield return null;

			var progressBeforeExit = state.ElapsedTime;
			Assert.Greater(progressBeforeExit, 0f, "Should have accumulated time");

			state.OnExit();

			// Re-enter: should reset and skip first frame again
			state.OnEnter();
			Assert.AreEqual(0f, state.ElapsedTime, 0.001f,
				"ElapsedTime should reset on re-enter");

			state.OnUpdate();
			Assert.AreEqual(0f, state.ElapsedTime, 0.001f,
				"First frame should be skipped again after re-enter");

			yield return null;
		}

		[UnityTest]
		public IEnumerator RandomDelayState_SwappedMinMax_StillCompletes()
		{
			var state = new RandomDelayState();
			state.MinDuration = 0.3f;
			state.MaxDuration = 0.1f; // Swapped: min > max
			state.UseUnscaledTime = true;
			var completed = false;
			state.OnComplete = () => completed = true;

			state.OnEnter();

			// Duration should be between 0.1 and 0.3 regardless of order
			Assert.GreaterOrEqual(state.SelectedDuration, 0.1f);
			Assert.LessOrEqual(state.SelectedDuration, 0.3f);

			var elapsed = 0f;
			var maxTime = 2f;
			while (!completed && elapsed < maxTime)
			{
				state.OnUpdate();
				elapsed += Time.unscaledDeltaTime;
				yield return null;
			}

			Assert.IsTrue(completed, "Should complete even with swapped min/max");

			yield return null;
		}
	}
}
