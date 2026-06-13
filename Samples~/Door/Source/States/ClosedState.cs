using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.Samples.Door.States
{
	/// <summary>
	/// Door is fully closed and waiting for interaction.
	/// </summary>
	[StateInfo(
		"Door is closed",
		"Waits for the player to press the interact key to begin opening.")]
	[StateStyle(Color = StateColors.Blue, Icon = NodeIcon.Lock)]
	public class ClosedState : State<DoorController>
	{
		[Transition("Open")]
		public Action OnOpen;

		public override void OnEnter()
		{
			// Ensure door is fully closed
			SharedContext.CurrentAngle = 0f;
			SharedContext.ApplyRotation();
		}

		public override void OnUpdate()
		{
			if (SharedContext.IsInteractPressed())
			{
				OnOpen?.Invoke();
			}
		}
	}
}
