using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.Samples.PatrollingGuard.States
{
	/// <summary>
	/// Guard moves between waypoints on patrol.
	/// </summary>
	[StateInfo(
		"Patrol",
		"Guard moves toward the current waypoint. Transitions to Idle when reached or Alert when target detected.")]
	[StateStyle(Color = StateColors.Green, Icon = NodeIcon.Path)]
	public class PatrolState : State<GuardController>
	{
		[Transition("Reached Waypoint")]
		public Action OnReachedWaypoint;

		[Transition("Target Detected")]
		public Action OnTargetDetected;

		public override void OnEnter()
		{
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

			var waypoint = SharedContext.CurrentWaypoint;
			if (!waypoint) return;

			// Move toward waypoint
			SharedContext.MoveToward(waypoint.position, SharedContext.MoveSpeed);

			// Check if reached
			if (!SharedContext.IsNearPosition(waypoint.position, SharedContext.WaypointReachDistance)) return;
			
			SharedContext.NextWaypoint();
			OnReachedWaypoint?.Invoke();
		}
	}
}
