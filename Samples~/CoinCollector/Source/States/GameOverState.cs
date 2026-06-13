using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.CoinCollector.States
{
	/// <summary>
	/// Game over state - time ran out before collecting all coins.
	/// </summary>
	[StateInfo(
		"Game Over",
		"Displays game over message. Press Space to return to menu.")]
	[StateStyle(Color = StateColors.Red, Icon = NodeIcon.Xmark)]
	public class GameOverState : State<GameController>
	{
		[DataIn("Final Score")]
		public int FinalScore;

		[Transition("Restart")]
		public Action OnRestart;

		public override void OnEnter()
		{
			SharedContext.SetPanelState(showMenu: false, showGame: false, showResult: true);
			SharedContext.ShowMessage($"TIME'S UP!\nCoins: {SharedContext.Score}/{SharedContext.CoinsToWin}\n\nPress SPACE to try again");
		}

		public override void OnUpdate()
		{
			if (Input.GetKeyDown(KeyCode.Space))
			{
				OnRestart?.Invoke();
			}
		}
	}
}
