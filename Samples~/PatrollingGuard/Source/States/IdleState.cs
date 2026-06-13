using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.PatrollingGuard.States
{
	/// <summary>
	/// Guard idles at a waypoint for a random duration.
	/// </summary>
	[StateInfo(
		"Idle at waypoint",
		"Guard waits at the current position for a random duration before continuing patrol.")]
	[StateStyle(Color = StateColors.Blue, Icon = NodeIcon.Clock)]
	public class IdleState : State<GuardController>
	{
		[DataOut("Idle Duration")]
		public float IdleDuration;

		[Transition("Resume Patrol")]
		public Action OnResumePatrol;

		[Transition("Target Detected")]
		public Action OnTargetDetected;

		private float _elapsed;

		public override void OnEnter()
		{
			_elapsed = 0f;
			IdleDuration = UnityEngine.Random.Range(
				SharedContext.MinIdleTime,
				SharedContext.MaxIdleTime);

			SharedContext.StopMovement();
			SharedContext.SetPatrolVisual();
		}

		public override void OnUpdate()
		{
			if (!SharedContext.IsOnCooldown && SharedContext.CanSeeTarget())
			{
				SharedContext.LastKnownTargetPosition = SharedContext.Target.position;
				SharedContext.ReturnPosition = SharedContext.Transform.position;
				OnTargetDetected?.Invoke();
				return;
			}

			_elapsed += Time.deltaTime;

			if (_elapsed >= IdleDuration)
			{
				OnResumePatrol?.Invoke();
			}
		}
	}
}
