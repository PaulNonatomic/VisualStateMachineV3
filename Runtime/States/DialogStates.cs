using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// State that displays dialog text and waits for the player to continue.
	/// Text can be fed from localization systems via DataIn ports.
	/// </summary>
	[StateInfo(
		"Displays dialog and waits to continue",
		"Outputs dialog text and speaker name for external UI to display. " +
		"Waits for a key press to advance. Connect localized strings " +
		"via DataIn ports from blackboard variables."
	)]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Note)]
	public class DialogState : State
	{
		/// <summary>
		/// The dialog text to display. Can be a localized string from the blackboard.
		/// </summary>
		[DataIn("Text", Required = true)]
		public string Text;

		/// <summary>
		/// The name of the speaker.
		/// </summary>
		[DataIn("Speaker")]
		public string Speaker;

		/// <summary>
		/// Optional portrait sprite for the speaker.
		/// </summary>
		[DataIn("Portrait")]
		public Sprite Portrait;

		/// <summary>
		/// The key to press to advance the dialog.
		/// </summary>
		[DataIn("Advance Key")]
		public KeyCode AdvanceKey = KeyCode.Space;

		/// <summary>
		/// Optional event fired when dialog is shown. Can be used to notify a dialog UI system.
		/// </summary>
		[DataIn("On Show")]
		public UnityEvent<string> OnShow;

		/// <summary>
		/// Outputs the dialog text for downstream states.
		/// </summary>
		[DataOut("Text")]
		public string OutputText;

		/// <summary>
		/// Outputs the speaker name for downstream states.
		/// </summary>
		[DataOut("Speaker")]
		public string OutputSpeaker;

		/// <summary>
		/// Transition that fires when the player advances the dialog.
		/// </summary>
		[Transition("Continue")]
		public Action OnContinue;

		public override void OnEnter()
		{
			OutputText = Text;
			OutputSpeaker = Speaker;

			if (string.IsNullOrEmpty(Text))
			{
				LogWarning("No dialog text specified");
				OnContinue?.Invoke();
				return;
			}

			OnShow?.Invoke(Text);
		}

		public override void OnUpdate()
		{
			if (Input.GetKeyDown(AdvanceKey))
			{
				OnContinue?.Invoke();
			}
		}
	}

	/// <summary>
	/// State that displays dialog text and automatically advances after a delay.
	/// </summary>
	[StateInfo(
		"Displays dialog and auto-advances",
		"Outputs dialog text for external UI to display and automatically " +
		"transitions after a configurable delay. Useful for narration, " +
		"tutorials, or timed dialog sequences."
	)]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Note)]
	public class TimedDialogState : State
	{
		/// <summary>
		/// The dialog text to display.
		/// </summary>
		[DataIn("Text", Required = true)]
		public string Text;

		/// <summary>
		/// The name of the speaker.
		/// </summary>
		[DataIn("Speaker")]
		public string Speaker;

		/// <summary>
		/// Optional portrait sprite for the speaker.
		/// </summary>
		[DataIn("Portrait")]
		public Sprite Portrait;

		/// <summary>
		/// How long to display the dialog before advancing (in seconds).
		/// </summary>
		[DataIn("Duration")]
		public float Duration = 3f;

		/// <summary>
		/// Whether to allow skipping via key press.
		/// </summary>
		[DataIn("Allow Skip")]
		public bool AllowSkip = true;

		/// <summary>
		/// The key to press to skip the dialog.
		/// </summary>
		[DataIn("Skip Key")]
		public KeyCode SkipKey = KeyCode.Space;

		/// <summary>
		/// Optional event fired when dialog is shown.
		/// </summary>
		[DataIn("On Show")]
		public UnityEvent<string> OnShow;

		/// <summary>
		/// Outputs the dialog text for downstream states.
		/// </summary>
		[DataOut("Text")]
		public string OutputText;

		/// <summary>
		/// Outputs the speaker name for downstream states.
		/// </summary>
		[DataOut("Speaker")]
		public string OutputSpeaker;

		/// <summary>
		/// Current progress from 0 to 1.
		/// </summary>
		[DataOut("Progress")]
		public float Progress;

		/// <summary>
		/// Transition that fires when dialog time expires or is skipped.
		/// </summary>
		[Transition("Continue")]
		public Action OnContinue;

		private float _elapsed;

		public override void OnEnter()
		{
			OutputText = Text;
			OutputSpeaker = Speaker;
			_elapsed = 0f;
			Progress = 0f;

			if (string.IsNullOrEmpty(Text))
			{
				LogWarning("No dialog text specified");
				OnContinue?.Invoke();
				return;
			}

			OnShow?.Invoke(Text);

			if (Duration <= 0f)
			{
				Progress = 1f;
				OnContinue?.Invoke();
			}
		}

		public override void OnUpdate()
		{
			if (Duration <= 0f) return;

			if (AllowSkip && Input.GetKeyDown(SkipKey))
			{
				Progress = 1f;
				OnContinue?.Invoke();
				return;
			}

			_elapsed += Time.deltaTime;
			Progress = Mathf.Clamp01(_elapsed / Duration);

			if (_elapsed >= Duration)
			{
				OnContinue?.Invoke();
			}
		}
	}

	/// <summary>
	/// State that presents dialog choices and waits for selection.
	/// </summary>
	[StateInfo(
		"Presents dialog choices",
		"Displays up to four dialog choices and waits for the player to select one. " +
		"Each choice has its own transition. Empty choices are hidden. " +
		"Connect localized strings via DataIn ports."
	)]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Menu)]
	public class DialogChoiceState : State
	{
		/// <summary>
		/// Text for choice A.
		/// </summary>
		[DataIn("Choice A", Required = true)]
		public string ChoiceA;

		/// <summary>
		/// Text for choice B. Leave empty to hide.
		/// </summary>
		[DataIn("Choice B")]
		public string ChoiceB;

		/// <summary>
		/// Text for choice C. Leave empty to hide.
		/// </summary>
		[DataIn("Choice C")]
		public string ChoiceC;

		/// <summary>
		/// Text for choice D. Leave empty to hide.
		/// </summary>
		[DataIn("Choice D")]
		public string ChoiceD;

		/// <summary>
		/// Optional event fired when choices are shown. Passes the number of visible choices.
		/// </summary>
		[DataIn("On Show")]
		public UnityEvent<int> OnShow;

		/// <summary>
		/// Outputs the index of the selected choice (0-3).
		/// </summary>
		[DataOut("Selected Index")]
		public int SelectedIndex;

		/// <summary>
		/// Outputs the text of the selected choice.
		/// </summary>
		[DataOut("Selected Text")]
		public string SelectedText;

		/// <summary>
		/// Transition that fires when choice A is selected.
		/// </summary>
		[Transition("Choice A")]
		public Action OnChoiceA;

		/// <summary>
		/// Transition that fires when choice B is selected.
		/// </summary>
		[Transition("Choice B")]
		public Action OnChoiceB;

		/// <summary>
		/// Transition that fires when choice C is selected.
		/// </summary>
		[Transition("Choice C")]
		public Action OnChoiceC;

		/// <summary>
		/// Transition that fires when choice D is selected.
		/// </summary>
		[Transition("Choice D")]
		public Action OnChoiceD;

		private int _choiceCount;

		public override void OnEnter()
		{
			SelectedIndex = -1;
			SelectedText = null;

			if (string.IsNullOrEmpty(ChoiceA))
			{
				LogWarning("No choices specified");
				return;
			}

			_choiceCount = 1;

			if (!string.IsNullOrEmpty(ChoiceB))
			{
				_choiceCount = 2;

				if (!string.IsNullOrEmpty(ChoiceC))
				{
					_choiceCount = 3;

					if (!string.IsNullOrEmpty(ChoiceD))
					{
						_choiceCount = 4;
					}
				}
			}

			OnShow?.Invoke(_choiceCount);
		}

		public override void OnUpdate()
		{
			if (Input.GetKeyDown(KeyCode.Alpha1) && _choiceCount >= 1)
			{
				SelectChoice(0, ChoiceA, OnChoiceA);
			}
			else if (Input.GetKeyDown(KeyCode.Alpha2) && _choiceCount >= 2)
			{
				SelectChoice(1, ChoiceB, OnChoiceB);
			}
			else if (Input.GetKeyDown(KeyCode.Alpha3) && _choiceCount >= 3)
			{
				SelectChoice(2, ChoiceC, OnChoiceC);
			}
			else if (Input.GetKeyDown(KeyCode.Alpha4) && _choiceCount >= 4)
			{
				SelectChoice(3, ChoiceD, OnChoiceD);
			}
		}

		private void SelectChoice(int index, string text, Action transition)
		{
			SelectedIndex = index;
			SelectedText = text;
			transition?.Invoke();
		}
	}

	/// <summary>
	/// State that clears/hides the dialog UI and immediately transitions.
	/// </summary>
	[StateInfo(
		"Clears the dialog display",
		"Fires a clear event to notify the dialog UI to hide, then immediately transitions. " +
		"Place this after dialog sequences to clean up the UI."
	)]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Erase)]
	public class ClearDialogState : State
	{
		/// <summary>
		/// Event fired to notify external UI to clear/hide the dialog.
		/// </summary>
		[DataIn("On Clear")]
		public UnityEvent OnClear;

		/// <summary>
		/// Transition that fires after clearing.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			OnClear?.Invoke();
			OnComplete?.Invoke();
		}
	}
}
