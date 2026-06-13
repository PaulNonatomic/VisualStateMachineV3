using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.Dialogue.States
{
	/// <summary>
	/// NPC says goodbye. Shows farewell text, then returns to idle.
	/// </summary>
	[StateInfo(
		"NPC says goodbye",
		"Displays farewell dialogue, then transitions back to idle after the player advances.")]
	[StateStyle(Color = StateColors.Red, Icon = NodeIcon.Exit)]
	public class FarewellState : State<DialogueController>
	{
		[Transition("End Dialogue")]
		public Action OnEndDialogue;

		public override void OnEnter()
		{
			var line = SharedContext.Farewell;
			SharedContext.ShowDialogue(line.Speaker, line.Text);
		}

		public override void OnUpdate()
		{
			SharedContext.AdvanceTypewriter(Time.deltaTime);

			if (!SharedContext.IsAdvancePressed()) return;

			if (!SharedContext.IsTypewriterComplete)
			{
				SharedContext.CompleteTypewriter();
			}
			else
			{
				OnEndDialogue?.Invoke();
			}
		}
	}
}
