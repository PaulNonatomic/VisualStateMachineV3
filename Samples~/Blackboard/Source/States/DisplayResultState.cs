using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.Blackboard.States
{
	/// <summary>
	/// Reads the final score from blackboard and displays it.
	/// Demonstrates reading blackboard values written by a previous state.
	/// </summary>
	[StateInfo(
		"Displays the result",
		"Reads Score from blackboard (written by AccumulateScoreState) and logs the final result.")]
	[StateStyle(Color = StateColors.Yellow, Icon = NodeIcon.Trophy)]
	public class DisplayResultState : State
	{
		[DataIn("Score")]
		public int FinalScore;

		[DataIn("Player Name")]
		public string PlayerName = "Player";

		[Transition("On Restart")]
		public Action OnRestart;

		public override void OnEnter()
		{
			Debug.Log($"[Blackboard] {PlayerName} reached target with score: {FinalScore}!");
		}

		public override void OnUpdate()
		{
			if (Input.GetKeyDown(KeyCode.R))
			{
				// Reset score in blackboard before restarting
				Blackboard.Set("Score", 0);
				OnRestart?.Invoke();
			}
		}
	}
}
