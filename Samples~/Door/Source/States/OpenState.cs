using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.Door.States
{
	/// <summary>
	/// Door is fully open, waiting for interaction or auto-close timeout.
	/// </summary>
	[StateInfo(
		"Door is open",
		"Waits for the player to close the door or auto-closes after a delay.")]
	[StateStyle(Color = StateColors.Green, Icon = NodeIcon.Unlock)]
	public class OpenState : State<DoorController>
	{
		[DataIn("Auto Close")]
		public bool AutoClose = true;

		[DataOut("Time Open")]
		public float TimeOpen;

		[Transition("Close")]
		public Action OnClose;

		private float _timeInState;

		public override void OnEnter()
		{
			_timeInState = 0f;

			// Ensure door is fully open
			SharedContext.CurrentAngle = SharedContext.OpenAngle;
			SharedContext.ApplyRotation();
		}

		public override void OnUpdate()
		{
			_timeInState += Time.deltaTime;
			TimeOpen = _timeInState;

			// Check for manual close
			if (SharedContext.IsInteractPressed())
			{
				OnClose?.Invoke();
				return;
			}

			// Check for auto-close
			if (AutoClose && _timeInState >= SharedContext.AutoCloseDelay)
			{
				OnClose?.Invoke();
			}
		}
	}
}
