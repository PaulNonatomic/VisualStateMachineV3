using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.CoinCollector.States
{
	/// <summary>
	/// Main menu state, waits for player to start the game.
	/// </summary>
	[StateInfo(
		"Main menu",
		"Displays the main menu and waits for the player to press Space to start.")]
	[StateStyle(Color = StateColors.Blue, Icon = NodeIcon.Home)]
	public class MenuState : State<GameController>
	{
		[Transition("Start")]
		public Action OnStart;

		public override void OnEnter()
		{
			SharedContext.SetPanelState(showMenu: true, showGame: false, showResult: false);
			SharedContext.ShowMessage("Press SPACE to Start");
		}

		public override void OnUpdate()
		{
			if (Input.GetKeyDown(KeyCode.Space))
			{
				OnStart?.Invoke();
			}
		}
	}
}
