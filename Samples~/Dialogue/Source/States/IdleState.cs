using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.Samples.Dialogue.States
{
	/// <summary>
	/// NPC is idle, waiting for the player to start a conversation.
	/// Shows an interaction prompt when the player is in range.
	/// </summary>
	[StateInfo(
		"NPC is idle",
		"Waits for the player to press the interact key to begin dialogue.")]
	[StateStyle(Color = StateColors.Blue, Icon = NodeIcon.User)]
	public class IdleState : State<DialogueController>
	{
		[Transition("Talk")]
		public Action OnTalk;

		public override void OnEnter()
		{
			SharedContext.HideDialogue();
			SharedContext.ShowPrompt("Press E to talk");
		}

		public override void OnUpdate()
		{
			if (SharedContext.IsInteractPressed())
			{
				SharedContext.HidePrompt();
				OnTalk?.Invoke();
			}
		}

		public override void OnExit()
		{
			SharedContext.HidePrompt();
		}
	}
}
