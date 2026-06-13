using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// State that triggers a debug break, pausing the editor for inspection.
	/// Only active in the editor; does nothing in builds.
	/// </summary>
	[StateInfo(
		"Pauses the editor for debugging",
		"Triggers Debug.Break() which pauses the editor, allowing you to inspect " +
		"the current state of your game. The break only occurs in the editor; in " +
		"builds this state simply logs a message and transitions immediately. " +
		"Useful for debugging state machine flow and inspecting variables."
	)]
	[StateStyle(Color = StateColors.Red, Icon = NodeIcon.Warning)]
	public class DebugBreakState : State
	{
		/// <summary>
		/// Optional message to log before breaking.
		/// </summary>
		[DataIn("Message")]
		public string Message = "Debug break triggered";

		/// <summary>
		/// If true, the break will only trigger once per play session.
		/// Subsequent entries to this state will transition immediately.
		/// </summary>
		[DataIn("Break Once")]
		public bool BreakOnce = false;

		/// <summary>
		/// Transition that fires after the break (when play is resumed).
		/// </summary>
		[Transition("Continue")]
		public Action OnContinue;

		private static readonly System.Collections.Generic.HashSet<string> _triggeredBreakpoints =
			new System.Collections.Generic.HashSet<string>();

		public override void OnEnter()
		{
#if UNITY_EDITOR
			var breakpointKey = $"{StateMachine.InstanceId}_{NodeId}";

			if (BreakOnce && _triggeredBreakpoints.Contains(breakpointKey))
			{
				// Already triggered this breakpoint, skip
				OnContinue?.Invoke();
				return;
			}

			if (!string.IsNullOrEmpty(Message))
			{
				Debug.Log($"[DebugBreak] {Message}", GameObject);
			}

			if (BreakOnce)
			{
				_triggeredBreakpoints.Add(breakpointKey);
			}

			Debug.Break();
#else
			if (!string.IsNullOrEmpty(Message))
			{
				Debug.Log($"[DebugBreak - Skipped in Build] {Message}", GameObject);
			}
#endif
			OnContinue?.Invoke();
		}

		/// <summary>
		/// Clears the record of triggered breakpoints.
		/// Call this to re-enable "Break Once" breakpoints.
		/// </summary>
		public static void ResetBreakpoints()
		{
			_triggeredBreakpoints.Clear();
		}
	}
}
