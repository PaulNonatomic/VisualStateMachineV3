using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// State that waits for a specific key press before transitioning.
	/// </summary>
	[StateInfo(
		"Waits for a key press",
		"Waits until the specified key is pressed before transitioning. " +
		"Can be configured to trigger on key down, key up, or while held."
	)]
	[StateStyle(Color = StateColors.Indigo, Icon = NodeIcon.Command)]
	public class WaitForKeyState : State
	{
		/// <summary>
		/// The key to wait for.
		/// </summary>
		[DataIn("Key")]
		public KeyCode Key = KeyCode.Space;

		/// <summary>
		/// The input mode to use.
		/// </summary>
		[DataIn("Input Mode")]
		public KeyInputMode InputMode = KeyInputMode.Down;

		/// <summary>
		/// Transition that fires when the key condition is met.
		/// </summary>
		[Transition("Pressed")]
		public Action OnPressed;

		public override void OnUpdate()
		{
			bool triggered = InputMode switch
			{
				KeyInputMode.Down => Input.GetKeyDown(Key),
				KeyInputMode.Up => Input.GetKeyUp(Key),
				KeyInputMode.Held => Input.GetKey(Key),
				_ => false
			};

			if (triggered)
			{
				OnPressed?.Invoke();
			}
		}
	}

	/// <summary>
	/// Input mode for key detection.
	/// </summary>
	public enum KeyInputMode
	{
		/// <summary>
		/// Triggers on the frame the key is pressed down.
		/// </summary>
		Down,

		/// <summary>
		/// Triggers on the frame the key is released.
		/// </summary>
		Up,

		/// <summary>
		/// Triggers every frame while the key is held.
		/// </summary>
		Held
	}

	/// <summary>
	/// State that waits for any key press before transitioning.
	/// </summary>
	[StateInfo(
		"Waits for any key press",
		"Waits until any key or mouse button is pressed before transitioning. " +
		"Useful for 'press any key to continue' screens."
	)]
	[StateStyle(Color = StateColors.Indigo, Icon = NodeIcon.Command)]
	public class WaitForAnyKeyState : State
	{
		/// <summary>
		/// Transition that fires when any key is pressed.
		/// </summary>
		[Transition("Pressed")]
		public Action OnPressed;

		public override void OnUpdate()
		{
			if (Input.anyKeyDown)
			{
				OnPressed?.Invoke();
			}
		}
	}

	/// <summary>
	/// State that waits for a mouse button click before transitioning.
	/// </summary>
	[StateInfo(
		"Waits for a mouse button click",
		"Waits until the specified mouse button is clicked before transitioning."
	)]
	[StateStyle(Color = StateColors.Indigo, Icon = NodeIcon.Archery)]
	public class WaitForMouseButtonState : State
	{
		/// <summary>
		/// The mouse button to wait for (0 = left, 1 = right, 2 = middle).
		/// </summary>
		[DataIn("Button")]
		public int Button = 0;

		/// <summary>
		/// The input mode to use.
		/// </summary>
		[DataIn("Input Mode")]
		public KeyInputMode InputMode = KeyInputMode.Down;

		/// <summary>
		/// Outputs the mouse position when clicked.
		/// </summary>
		[DataOut("Mouse Position")]
		public Vector3 MousePosition;

		/// <summary>
		/// Transition that fires when the button condition is met.
		/// </summary>
		[Transition("Clicked")]
		public Action OnClicked;

		public override void OnUpdate()
		{
			bool triggered = InputMode switch
			{
				KeyInputMode.Down => Input.GetMouseButtonDown(Button),
				KeyInputMode.Up => Input.GetMouseButtonUp(Button),
				KeyInputMode.Held => Input.GetMouseButton(Button),
				_ => false
			};

			if (triggered)
			{
				MousePosition = Input.mousePosition;
				OnClicked?.Invoke();
			}
		}
	}
}
