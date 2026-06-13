using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.BuilderAPI.States
{
	/// <summary>
	/// Idle state that waits for player input to transition.
	/// </summary>
	[StateInfo(
		"Player is idle",
		"Waits for movement or jump input to transition to the appropriate state.")]
	[StateStyle(Color = StateColors.Blue, Icon = NodeIcon.User)]
	public class IdleState : State
	{
		[Transition("On Move")]
		public Action OnMove;

		[Transition("On Jump")]
		public Action OnJump;

		public override void OnEnter()
		{
			Log("Entered Idle state");
		}

		public override void OnUpdate()
		{
			if (Input.GetKeyDown(KeyCode.Space))
			{
				OnJump?.Invoke();
				return;
			}

			var horizontal = Input.GetAxis("Horizontal");
			var vertical = Input.GetAxis("Vertical");

			if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
			{
				OnMove?.Invoke();
			}
		}
	}
}
