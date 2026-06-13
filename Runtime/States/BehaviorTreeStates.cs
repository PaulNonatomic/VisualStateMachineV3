using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// Evaluates up to four boolean conditions in order (AND-gate).
	/// All conditions must be true for Success. Fails on the first false condition.
	/// Mirrors the Sequence composite from behavior trees.
	/// </summary>
	[StateInfo(
		"BT Sequence: all conditions must pass",
		"Evaluates up to four boolean conditions in order. Transitions to Success " +
		"only if all active conditions are true. Transitions to Failure on the " +
		"first false condition. Outputs the index of the first failed condition. " +
		"Unused conditions (past the last connected one) default to true."
	)]
	[StateStyle(Color = StateColors.LimeGreen, Icon = NodeIcon.ClipboardCheck)]
	public class SequenceGateState : State
	{
		/// <summary>
		/// First condition to evaluate.
		/// </summary>
		[DataIn("Condition A", Required = true)]
		public bool ConditionA;

		/// <summary>
		/// Second condition. Defaults to true if not connected.
		/// </summary>
		[DataIn("Condition B")]
		public bool ConditionB = true;

		/// <summary>
		/// Third condition. Defaults to true if not connected.
		/// </summary>
		[DataIn("Condition C")]
		public bool ConditionC = true;

		/// <summary>
		/// Fourth condition. Defaults to true if not connected.
		/// </summary>
		[DataIn("Condition D")]
		public bool ConditionD = true;

		/// <summary>
		/// Index of the first failed condition (0-3), or -1 if all passed.
		/// </summary>
		[DataOut("Failed Index")]
		public int FailedIndex;

		/// <summary>
		/// Transition that fires when all conditions are true.
		/// </summary>
		[Transition("Success", Color = StateColors.Green)]
		public Action OnSuccess;

		/// <summary>
		/// Transition that fires when any condition is false.
		/// </summary>
		[Transition("Failure", Color = StateColors.Red)]
		public Action OnFailure;

		public override void OnEnter()
		{
			FailedIndex = -1;

			if (!ConditionA) { FailedIndex = 0; OnFailure?.Invoke(); return; }
			if (!ConditionB) { FailedIndex = 1; OnFailure?.Invoke(); return; }
			if (!ConditionC) { FailedIndex = 2; OnFailure?.Invoke(); return; }
			if (!ConditionD) { FailedIndex = 3; OnFailure?.Invoke(); return; }

			OnSuccess?.Invoke();
		}
	}

	/// <summary>
	/// Evaluates up to four boolean conditions in priority order.
	/// Transitions on the first true condition. Each condition has its own transition.
	/// Mirrors the Selector composite from behavior trees.
	/// </summary>
	[StateInfo(
		"BT Selector: first true condition wins",
		"Evaluates up to four boolean conditions in priority order. Transitions " +
		"on the first true condition via its dedicated output. If no condition " +
		"is true, fires the None transition."
	)]
	[StateStyle(Color = StateColors.LimeGreen, Icon = NodeIcon.Random)]
	public class SelectorGateState : State
	{
		/// <summary>
		/// Highest priority condition.
		/// </summary>
		[DataIn("Condition A")]
		public bool ConditionA;

		/// <summary>
		/// Second priority condition.
		/// </summary>
		[DataIn("Condition B")]
		public bool ConditionB;

		/// <summary>
		/// Third priority condition.
		/// </summary>
		[DataIn("Condition C")]
		public bool ConditionC;

		/// <summary>
		/// Lowest priority condition.
		/// </summary>
		[DataIn("Condition D")]
		public bool ConditionD;

		/// <summary>
		/// Index of the selected condition (0-3), or -1 if none selected.
		/// </summary>
		[DataOut("Selected Index")]
		public int SelectedIndex;

		/// <summary>
		/// Transition for condition A.
		/// </summary>
		[Transition("A")]
		public Action OnA;

		/// <summary>
		/// Transition for condition B.
		/// </summary>
		[Transition("B")]
		public Action OnB;

		/// <summary>
		/// Transition for condition C.
		/// </summary>
		[Transition("C")]
		public Action OnC;

		/// <summary>
		/// Transition for condition D.
		/// </summary>
		[Transition("D")]
		public Action OnD;

		/// <summary>
		/// Transition that fires when no condition is true.
		/// </summary>
		[Transition("None")]
		public Action OnNone;

		public override void OnEnter()
		{
			SelectedIndex = -1;

			if (ConditionA) { SelectedIndex = 0; OnA?.Invoke(); return; }
			if (ConditionB) { SelectedIndex = 1; OnB?.Invoke(); return; }
			if (ConditionC) { SelectedIndex = 2; OnC?.Invoke(); return; }
			if (ConditionD) { SelectedIndex = 3; OnD?.Invoke(); return; }

			OnNone?.Invoke();
		}
	}

	/// <summary>
	/// Counts iterations and branches to Loop or Done.
	/// Mirrors the Repeater decorator from behavior trees.
	/// Connect the Loop transition back to the beginning of the repeated sequence.
	/// </summary>
	[StateInfo(
		"BT Repeater: loops N times",
		"Counts iterations and transitions to Loop until the repeat count is " +
		"reached, then transitions to Done. Set Infinite to true for unlimited " +
		"looping. The counter resets when Done fires. Connect Loop back to the " +
		"start of the sequence to repeat."
	)]
	[StateStyle(Color = StateColors.LimeGreen, Icon = NodeIcon.Restart)]
	public class RepeaterState : State
	{
		/// <summary>
		/// Number of times to repeat before transitioning to Done.
		/// </summary>
		[DataIn("Count")]
		public int RepeatCount = 3;

		/// <summary>
		/// If true, loops forever (ignores RepeatCount).
		/// </summary>
		[DataIn("Infinite")]
		public bool Infinite;

		/// <summary>
		/// Current iteration number (1-based).
		/// </summary>
		[DataOut("Iteration")]
		public int CurrentIteration;

		/// <summary>
		/// Transition that fires for each repetition.
		/// </summary>
		[Transition("Loop")]
		public Action OnLoop;

		/// <summary>
		/// Transition that fires when all repetitions are complete.
		/// </summary>
		[Transition("Done")]
		public Action OnDone;

		private int _count;

		public override void OnEnter()
		{
			_count++;
			CurrentIteration = _count;

			if (Infinite || _count < RepeatCount)
			{
				OnLoop?.Invoke();
			}
			else
			{
				_count = 0;
				OnDone?.Invoke();
			}
		}

		public override void OnReset()
		{
			_count = 0;
			CurrentIteration = 0;
		}
	}

	/// <summary>
	/// Prevents re-entry for a configurable cooldown duration.
	/// Mirrors the Cooldown decorator from behavior trees.
	/// </summary>
	[StateInfo(
		"BT Cooldown: rate limits re-entry",
		"Tracks time since last successful pass-through. If the cooldown has " +
		"elapsed, transitions to Ready and resets the timer. If still cooling " +
		"down, transitions to OnCooldown. Useful for rate-limiting abilities, " +
		"attacks, or any time-gated behavior."
	)]
	[StateStyle(Color = StateColors.LimeGreen, Icon = NodeIcon.Clock)]
	public class CooldownState : State
	{
		/// <summary>
		/// Cooldown duration in seconds.
		/// </summary>
		[DataIn("Duration")]
		public float Duration = 1f;

		/// <summary>
		/// Time remaining on cooldown. Zero when ready.
		/// </summary>
		[DataOut("Remaining")]
		public float Remaining;

		/// <summary>
		/// Transition that fires when the cooldown has elapsed.
		/// </summary>
		[Transition("Ready")]
		public Action OnReady;

		/// <summary>
		/// Transition that fires when still in cooldown.
		/// </summary>
		[Transition("On Cooldown")]
		public Action OnCooldown;

		private float _lastActivation = float.NegativeInfinity;

		public override void OnEnter()
		{
			var elapsed = Time.time - _lastActivation;

			if (elapsed >= Duration)
			{
				_lastActivation = Time.time;
				Remaining = 0f;
				OnReady?.Invoke();
			}
			else
			{
				Remaining = Duration - elapsed;
				OnCooldown?.Invoke();
			}
		}

		public override void OnReset()
		{
			_lastActivation = float.NegativeInfinity;
			Remaining = 0f;
		}
	}

	/// <summary>
	/// Waits and fires a Timeout transition if a configurable duration elapses.
	/// Mirrors the Timeout decorator from behavior trees.
	/// Runs alongside a work path; if that path transitions the machine away before
	/// the duration elapses, the timeout never fires.
	/// </summary>
	[StateInfo(
		"BT Timeout: fails after duration",
		"Waits for the specified duration and fires the Timeout transition when " +
		"time runs out. Use this in parallel paths where one path does work and " +
		"this path enforces a time limit."
	)]
	[StateStyle(Color = StateColors.LimeGreen, Icon = NodeIcon.Hourglass)]
	public class TimeoutState : State
	{
		/// <summary>
		/// Maximum time to wait in seconds.
		/// </summary>
		[DataIn("Duration")]
		public float Duration = 5f;

		/// <summary>
		/// Elapsed time since the timeout started.
		/// </summary>
		[DataOut("Elapsed")]
		public float Elapsed;

		/// <summary>
		/// Progress from 0 to 1.
		/// </summary>
		[DataOut("Progress")]
		public float Progress;

		/// <summary>
		/// Transition that fires when the timeout elapses.
		/// </summary>
		[Transition("Timeout")]
		public Action OnTimeout;

		private float _elapsed;

		public override void OnEnter()
		{
			_elapsed = 0f;
			Elapsed = 0f;
			Progress = 0f;

			if (Duration <= 0f)
			{
				Progress = 1f;
				Elapsed = 0f;
				OnTimeout?.Invoke();
			}
		}

		public override void OnUpdate()
		{
			if (Duration <= 0f) return;

			_elapsed += Time.deltaTime;
			Elapsed = _elapsed;
			Progress = Mathf.Clamp01(_elapsed / Duration);

			if (_elapsed >= Duration)
			{
				OnTimeout?.Invoke();
			}
		}
	}
}
