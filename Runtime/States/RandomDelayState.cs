using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// State that waits for a random duration within a specified range before transitioning.
	/// Useful for adding variation to timing-based behaviors.
	/// </summary>
	[StateInfo(
		"Waits for a random duration before continuing",
		"Pauses state machine progression for a random number of seconds between " +
		"MinDuration and MaxDuration. The actual duration is determined when the " +
		"state is entered and output for use by subsequent states."
	)]
	[StateStyle(Color = StateColors.Cyan, Icon = NodeIcon.Hourglass)]
	public class RandomDelayState : State
	{
		/// <summary>
		/// Minimum duration to wait in seconds.
		/// </summary>
		[DataIn("Min Duration")]
		public float MinDuration = 0.5f;

		/// <summary>
		/// Maximum duration to wait in seconds.
		/// </summary>
		[DataIn("Max Duration")]
		public float MaxDuration = 2f;

		/// <summary>
		/// If true, uses Time.unscaledDeltaTime (ignores Time.timeScale).
		/// </summary>
		[DataIn("Use Unscaled Time")]
		public bool UseUnscaledTime = true;

		/// <summary>
		/// The randomly selected duration for this execution.
		/// </summary>
		[DataOut("Selected Duration")]
		public float SelectedDuration;

		/// <summary>
		/// The actual time elapsed.
		/// </summary>
		[DataOut("Elapsed Time")]
		public float ElapsedTime;

		/// <summary>
		/// Current progress from 0 to 1.
		/// </summary>
		[DataOut("Progress")]
		public float Progress;

		/// <summary>
		/// Transition that fires when the delay completes.
		/// </summary>
		[Transition("Complete")]
		public Action OnComplete;

		private bool _started;

		public override void OnEnter()
		{
			// Ensure min <= max
			var min = Mathf.Min(MinDuration, MaxDuration);
			var max = Mathf.Max(MinDuration, MaxDuration);

			SelectedDuration = UnityEngine.Random.Range(min, max);
			ElapsedTime = 0f;
			Progress = 0f;
			_started = false;

			// Handle zero or negative duration
			if (SelectedDuration <= 0f)
			{
				ElapsedTime = 0f;
				Progress = 1f;
				OnComplete?.Invoke();
			}
		}

		public override void OnUpdate()
		{
			if (SelectedDuration <= 0f) return;

			// Skip the first frame to avoid large delta time values
			// that can occur when the state machine starts during Awake()
			if (!_started)
			{
				_started = true;
				return;
			}

			// Accumulate elapsed time using delta time
			// Cap delta time to avoid issues with editor pauses or long frames
			var deltaTime = UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
			deltaTime = Mathf.Min(deltaTime, 0.1f); // Cap at 100ms to handle editor pauses
			ElapsedTime += deltaTime;
			Progress = Mathf.Clamp01(ElapsedTime / SelectedDuration);

			if (ElapsedTime >= SelectedDuration)
			{
				OnComplete?.Invoke();
			}
		}

		public override void OnExit()
		{
			_started = false;
		}
	}
}
