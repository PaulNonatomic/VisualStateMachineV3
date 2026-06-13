using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.PatrollingGuard.States
{
	/// <summary>
	/// Guard is alerted and pauses before chasing.
	/// </summary>
	[StateInfo(
		"Alert",
		"Guard has detected something and pauses briefly before deciding to chase.")]
	[StateStyle(Color = StateColors.Yellow, Icon = NodeIcon.Warning)]
	public class AlertState : State<GuardController>
	{
		[DataIn("Alert Duration")]
		public float AlertDuration = 0.5f;

		[Transition("Chase")]
		public Action OnChase;

		[Transition("Lost Target")]
		public Action OnLostTarget;

		private float _elapsed;

		public override void OnEnter()
		{
			_elapsed = 0f;
			SharedContext.StopMovement();
			SharedContext.SetAlertVisual();

			// Look toward target
			if (SharedContext.Target != null)
			{
				SharedContext.LastKnownTargetPosition = SharedContext.Target.position;
			}
		}

		public override void OnUpdate()
		{
			_elapsed += Time.deltaTime;

			// Rotate toward last known position
			var direction = SharedContext.LastKnownTargetPosition - SharedContext.Transform.position;
			direction.y = 0;

			if (direction.sqrMagnitude > 0.01f)
			{
				var targetRotation = Quaternion.LookRotation(direction);
				SharedContext.Transform.rotation = Quaternion.Slerp(
					SharedContext.Transform.rotation,
					targetRotation,
					SharedContext.RotationSpeed * Time.deltaTime);
			}

			if (_elapsed >= AlertDuration)
			{
				// Check if we can still see target
				if (SharedContext.CanSeeTarget())
				{
					OnChase?.Invoke();
				}
				else
				{
					OnLostTarget?.Invoke();
				}
			}
		}
	}
}
