using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.Dialogue.States
{
	/// <summary>
	/// NPC tells the player about the town.
	/// Displays the response with typewriter effect, then returns to choices.
	/// </summary>
	[StateInfo(
		"NPC describes the town",
		"Displays town information with typewriter reveal. Press advance key to continue.")]
	[StateStyle(Color = StateColors.Teal, Icon = NodeIcon.InfoCircle)]
	public class TownInfoState : State<DialogueController>
	{
		[Transition("Back to Choices")]
		public Action OnBackToChoices;

		public override void OnEnter()
		{
			var line = SharedContext.TownResponse;
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
				OnBackToChoices?.Invoke();
			}
		}
	}
}
