using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.Samples.PatrollingGuard.States
{
	/// <summary>
	/// Guard has caught the thief. Signals the thief to drop all gems
	/// and waits for the stun to finish before returning to patrol.
	/// </summary>
	[StateInfo(
		"Caught thief",
		"Guard catches the thief, forcing them to drop gems and pause briefly.")]
	[StateStyle(Color = StateColors.Pink, Icon = NodeIcon.Trophy)]
	public class CaughtState : State<GuardController>
	{
		[Transition("Resume Patrol")]
		public Action OnResumePatrol;

		private ThiefController _thief;

		public override void OnEnter()
		{
			SharedContext.StopMovement();
			SharedContext.PlayCaughtSound();

			_thief = SharedContext.Target?.GetComponent<ThiefController>();
			_thief?.OnCaught();
		}

		public override void OnUpdate()
		{
			if (_thief == null || !_thief.IsStunned)
			{
				OnResumePatrol?.Invoke();
			}
		}
	}
}
