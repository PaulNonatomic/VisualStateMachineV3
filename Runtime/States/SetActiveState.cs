using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// State that enables or disables a GameObject and immediately transitions.
	/// Can target the context GameObject or a specified target.
	/// </summary>
	[StateInfo(
		"Sets a GameObject's active state",
		"Enables or disables a GameObject. If no target is specified, operates on " +
		"the context GameObject. Can be used to show/hide UI elements, enable/disable " +
		"game objects, or control visibility during state transitions."
	)]
	[StateStyle(Color = StateColors.Green, Icon = NodeIcon.Cube)]
	public class SetActiveState : State
	{
		/// <summary>
		/// The target GameObject to enable/disable. If null, uses the context GameObject.
		/// </summary>
		[DataIn("Target")]
		public GameObject Target;

		/// <summary>
		/// Whether to activate (true) or deactivate (false) the target.
		/// </summary>
		[DataIn("Active")]
		public bool Active = true;

		/// <summary>
		/// Transition that fires after setting the active state.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			var target = Target != null ? Target : GameObject;

			if (target != null)
			{
				target.SetActive(Active);
			}
			else
			{
				LogWarning("No target GameObject specified and no context GameObject available");
			}

			OnComplete?.Invoke();
		}
	}
}
