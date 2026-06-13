using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.Dialogue.States
{
	/// <summary>
	/// NPC greets the player with a typewriter text reveal.
	/// Press Space to skip the typewriter or advance to choices.
	/// </summary>
	[StateInfo(
		"NPC greets the player",
		"Displays the greeting dialogue line with a typewriter effect. " +
		"Press the advance key to skip or continue.")]
	[StateStyle(Color = StateColors.Green, Icon = NodeIcon.PostSolid)]
	public class GreetingState : State<DialogueController>
	{
		[Transition("Continue")]
		public Action OnContinue;

		public override void OnEnter()
		{
			var line = SharedContext.Greeting;
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
				OnContinue?.Invoke();
			}
		}
	}
}
