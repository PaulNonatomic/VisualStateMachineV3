using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.Blackboard.States
{
	/// <summary>
	/// Reads score configuration from blackboard, accumulates points over time,
	/// and writes the result back. Demonstrates DataIn/DataOut blackboard binding.
	/// </summary>
	[StateInfo(
		"Accumulates score",
		"Reads PointsPerSecond from blackboard, accumulates score, and writes it back via DataOut.")]
	[StateStyle(Color = StateColors.Green, Icon = NodeIcon.LeaderboardStar)]
	public class AccumulateScoreState : State
	{
		[DataIn("Points Per Second")]
		public float PointsPerSecond = 10f;

		[DataIn("Score Multiplier")]
		public float ScoreMultiplier = 1f;

		[DataOut("Score")]
		public int Score;

		[Transition("On Target Reached")]
		public Action OnTargetReached;

		private float _accumulated;
		private int _targetScore;

		public override void OnEnter()
		{
			_accumulated = 0f;

			// Read target score from blackboard directly (not via DataIn)
			_targetScore = Blackboard.Get("Target Score", 100);

			Log($"Accumulating score at {PointsPerSecond * ScoreMultiplier} pts/sec toward target {_targetScore}");
		}

		public override void OnUpdate()
		{
			_accumulated += PointsPerSecond * ScoreMultiplier * Time.deltaTime;
			Score = Mathf.FloorToInt(_accumulated);

			// Write score to blackboard each frame so other systems can react
			Blackboard.Set("Score", Score);

			if (Score >= _targetScore)
			{
				OnTargetReached?.Invoke();
			}
		}
	}
}
