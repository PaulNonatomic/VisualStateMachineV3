using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.CoinCollector.States
{
	/// <summary>
	/// Victory state - player collected all coins.
	/// </summary>
	[StateInfo(
		"Win!",
		"Displays victory message. Press Space to return to menu.")]
	[StateStyle(Color = StateColors.Yellow, Icon = NodeIcon.Trophy)]
	public class WinState : State<GameController>
	{
		[DataIn("Final Score")]
		public int FinalScore;

		[Transition("Restart")]
		public Action OnRestart;

		public override void OnEnter()
		{
			SharedContext.SetPanelState(showMenu: false, showGame: false, showResult: true);
			SharedContext.ShowMessage($"YOU WIN!\nCoins: {SharedContext.Score}\nTime: {Mathf.CeilToInt(SharedContext.TimeRemaining)}s remaining\n\nPress SPACE to play again");
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
