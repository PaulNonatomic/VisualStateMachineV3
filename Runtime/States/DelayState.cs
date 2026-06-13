using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// State that waits for a specified duration before transitioning.
	/// Uses unscaled time by default but can be configured to use scaled time.
	/// </summary>
	[StateInfo(
		"Waits for a duration before continuing",
		"Pauses state machine progression for the specified number of seconds. " +
		"Can use either scaled time (affected by Time.timeScale) or unscaled time " +
		"(real-world seconds). Outputs the actual elapsed time."
	)]
	[StateStyle(Color = StateColors.Cyan, Icon = NodeIcon.Clock)]
	public class DelayState : State
	{
		/// <summary>
		/// Duration to wait in seconds.
		/// </summary>
		[DataIn("Duration")]
		public float Duration = 1f;

		/// <summary>
		/// If true, uses Time.unscaledDeltaTime (ignores Time.timeScale).
		/// </summary>
		[DataIn("Use Unscaled Time")]
		public bool UseUnscaledTime = true;

		/// <summary>
		/// The actual time elapsed (may differ slightly from Duration).
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

		private float _elapsed;
		private bool _started;

		public override void OnEnter()
		{
			_elapsed = 0f;
			_started = false;
			ElapsedTime = 0f;
			Progress = 0f;

			// Handle zero or negative duration
			if (Duration <= 0f)
			{
				ElapsedTime = 0f;
				Progress = 1f;
				OnComplete?.Invoke();
			}
		}

		public override void OnUpdate()
		{
			if (Duration <= 0f) return;

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
			_elapsed += deltaTime;
			ElapsedTime = _elapsed;
			Progress = Mathf.Clamp01(_elapsed / Duration);

			if (_elapsed >= Duration)
			{
				OnComplete?.Invoke();
			}
		}

		public override void OnExit()
		{
			_elapsed = 0f;
			_started = false;
		}
	}
}
