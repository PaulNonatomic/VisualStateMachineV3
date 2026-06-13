using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.Samples.PatrollingGuard.States
{
	/// <summary>
	/// Guard returns to patrol route after losing target.
	/// </summary>
	[StateInfo(
		"Return to patrol",
		"Guard moves back to where they left their patrol route.")]
	[StateStyle(Color = StateColors.Cyan, Icon = NodeIcon.Restart)]
	public class ReturnState : State<GuardController>
	{
		[Transition("Resumed Patrol")]
		public Action OnResumedPatrol;

		[Transition("Target Detected")]
		public Action OnTargetDetected;

		public override void OnEnter()
		{
			SharedContext.SetPatrolVisual();
			SharedContext.SetNearestWaypoint();
		}

		public override void OnUpdate()
		{
			if (!SharedContext.IsOnCooldown && SharedContext.CanSeeTarget())
			{
				SharedContext.LastKnownTargetPosition = SharedContext.Target.position;
				OnTargetDetected?.Invoke();
				return;
			}

			var waypoint = SharedContext.CurrentWaypoint;
			if (!waypoint) return;

			// Move toward nearest waypoint
			SharedContext.MoveToward(waypoint.position, SharedContext.MoveSpeed);

			// Check if reached
			if (SharedContext.IsNearPosition(waypoint.position, SharedContext.WaypointReachDistance))
			{
				OnResumedPatrol?.Invoke();
			}
		}
	}
}
