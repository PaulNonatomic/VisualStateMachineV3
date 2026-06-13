using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.Dialogue.States
{
	/// <summary>
	/// Presents dialogue choices to the player.
	/// Each choice triggers a different transition.
	/// </summary>
	[StateInfo(
		"Presents dialogue choices",
		"Displays three choices. Press 1, 2, or 3 to select. " +
		"Each choice has its own transition output.")]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Menu)]
	public class ChoiceState : State<DialogueController>
	{
		[DataOut("Selected Index")]
		public int SelectedIndex;

		[DataOut("Selected Text")]
		public string SelectedText;

		[Transition("Choice A")]
		public Action OnChoiceA;

		[Transition("Choice B")]
		public Action OnChoiceB;

		[Transition("Choice C")]
		public Action OnChoiceC;

		public override void OnEnter()
		{
			SelectedIndex = -1;
			SelectedText = null;

			SharedContext.ShowChoices(
				SharedContext.ChoicePrompt,
				SharedContext.ChoiceA,
				SharedContext.ChoiceB,
				SharedContext.ChoiceC);
		}

		public override void OnUpdate()
		{
			if (Input.GetKeyDown(KeyCode.Alpha1))
			{
				SelectChoice(0, SharedContext.ChoiceA, OnChoiceA);
			}
			else if (Input.GetKeyDown(KeyCode.Alpha2))
			{
				SelectChoice(1, SharedContext.ChoiceB, OnChoiceB);
			}
			else if (Input.GetKeyDown(KeyCode.Alpha3))
			{
				SelectChoice(2, SharedContext.ChoiceC, OnChoiceC);
			}
		}

		private void SelectChoice(int index, string text, Action transition)
		{
			SelectedIndex = index;
			SelectedText = text;
			transition?.Invoke();
		}
	}
}
