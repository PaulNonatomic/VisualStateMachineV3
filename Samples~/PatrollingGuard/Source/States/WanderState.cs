using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.Samples.PatrollingGuard.States
{
	[StateInfo(
		"Wander",
		"Thief meanders smoothly along bezier curves between random destinations. " +
		"Pre-picks the next waypoint at the midpoint for continuous flow. " +
		"Transitions to Flee when the guard is detected nearby.")]
	[StateStyle(Color = StateColors.LimeGreen, Icon = NodeIcon.Spiral)]
	public class WanderState : State<ThiefController>
	{
		[Transition("Guard Detected")]
		public Action OnGuardDetected;

		public override void OnEnter()
		{
			SharedContext.SetWanderVisual();
			SharedContext.SetTargetSpeed(SharedContext.WanderSpeed);

			if (!SharedContext.PathInitialized)
			{
				var firstDest = SharedContext.PickRandomDestination(SharedContext.Transform.position);
				SharedContext.InitializePath(firstDest);
			}
		}

		public override void OnUpdate()
		{
			if (SharedContext.IsGuardNearby())
			{
				OnGuardDetected?.Invoke();
				return;
			}

			if (SharedContext.IsWallAhead(out var wallNormal))
			{
				var newDest = SharedContext.PickRandomDestination(SharedContext.Transform.position);
				SharedContext.RedirectFromWall(wallNormal, newDest);
			}

			if (SharedContext.NeedsNextDestination)
			{
				var anchor = SharedContext.CurrentDestination;
				var next = SharedContext.PickRandomDestination(anchor);
				SharedContext.SetNextDestination(next);
			}

			SharedContext.AdvancePath();
		}
	}
}
