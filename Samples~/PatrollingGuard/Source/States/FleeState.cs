using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.Samples.PatrollingGuard.States
{
	[StateInfo(
		"Flee from guard",
		"Thief flees from the guard at increased speed along bezier curves. " +
		"Picks escape destinations away from the guard. " +
		"Transitions back to wander when the guard exits danger range.")]
	[StateStyle(Color = StateColors.Violet, Icon = NodeIcon.Run)]
	public class FleeState : State<ThiefController>
	{
		[Transition("Escaped")]
		public Action OnEscaped;

		public override void OnEnter()
		{
			SharedContext.SetFleeVisual();
			SharedContext.SetTargetSpeed(SharedContext.FleeSpeed);
			SharedContext.InvalidateNextDestination();
		}

		public override void OnUpdate()
		{
			if (!SharedContext.IsGuardInDangerRange())
			{
				OnEscaped?.Invoke();
				return;
			}

			if (SharedContext.IsWallAhead(out var wallNormal))
			{
				var newDest = SharedContext.PickFleeDestination(SharedContext.Transform.position);
				SharedContext.RedirectFromWall(wallNormal, newDest);
			}

			if (SharedContext.NeedsNextDestination)
			{
				// Anchor from the target's actual position so the flee direction
				// is always away from the guard relative to where we ARE, not
				// where the current segment ends.
				var next = SharedContext.PickFleeDestination(SharedContext.Transform.position);
				SharedContext.SetNextDestination(next);
			}

			SharedContext.AdvancePath();
		}
	}
}
