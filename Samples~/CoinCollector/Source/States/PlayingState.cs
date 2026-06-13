using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.CoinCollector.States
{
	/// <summary>
	/// Main gameplay state - player collects coins within time limit.
	/// </summary>
	[StateInfo(
		"Playing",
		"Main gameplay state. Counts down timer and tracks coin collection.")]
	[StateStyle(Color = StateColors.Green, Icon = NodeIcon.Play)]
	public class PlayingState : State<GameController>
	{
		[DataOut("Score")]
		public int Score;

		[DataOut("Time Remaining")]
		public float TimeRemaining;

		[Transition("Check Result")]
		public Action OnCheckResult;

		[Transition("Time Up")]
		public Action OnTimeUp;

		private bool _coinCollectedThisFrame;

		public override void OnEnter()
		{
			SharedContext.ResetGame();
			SharedContext.SetPanelState(showMenu: false, showGame: true, showResult: false);
			SharedContext.ShowMessage("");

			// Subscribe to coin collection
			SharedContext.OnCoinCollected += HandleCoinCollected;
		}

		public override void OnExit()
		{
			// Unsubscribe from events
			SharedContext.OnCoinCollected -= HandleCoinCollected;
		}

		public override void OnUpdate()
		{
			// Update timer
			SharedContext.TimeRemaining -= Time.deltaTime;
			TimeRemaining = SharedContext.TimeRemaining;
			Score = SharedContext.Score;
			SharedContext.UpdateUI();

			// Check win condition (coin collected)
			if (_coinCollectedThisFrame)
			{
				_coinCollectedThisFrame = false;

				if (SharedContext.HasWon)
				{
					OnCheckResult?.Invoke();
					return;
				}
			}

			// Check lose condition (time up)
			if (SharedContext.IsTimeUp)
			{
				OnTimeUp?.Invoke();
			}
		}

		private void HandleCoinCollected(int newScore)
		{
			_coinCollectedThisFrame = true;
		}
	}
}
