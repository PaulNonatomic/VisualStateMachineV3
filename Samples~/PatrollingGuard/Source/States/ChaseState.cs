using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.Samples.PatrollingGuard.States
{
	/// <summary>
	/// Guard chases the target.
	/// </summary>
	[StateInfo(
		"Chase target",
		"Guard pursues the target at increased speed. Transitions to Return if target is lost.")]
	[StateStyle(Color = StateColors.Red, Icon = NodeIcon.Run)]
	public class ChaseState : State<GuardController>
	{
		[Transition("Lost Target")]
		public Action OnLostTarget;

		[Transition("Caught Target")]
		public Action OnCaughtTarget;

		public override void OnEnter()
		{
			SharedContext.SetChaseVisual();
		}

		public override void OnUpdate()
		{
			// Update last known position if we can see target
			if (SharedContext.CanSeeTarget())
			{
				SharedContext.LastKnownTargetPosition = SharedContext.Target.position;
			}

			// Check if target is still in chase range
			if (!SharedContext.IsTargetInChaseRange())
			{
				OnLostTarget?.Invoke();
				return;
			}

			// Move toward target
			SharedContext.MoveToward(SharedContext.Target.position, SharedContext.ChaseSpeed);

			// Check if caught (very close)
			if (SharedContext.IsNearPosition(SharedContext.Target.position, 1f))
			{
				OnCaughtTarget?.Invoke();
			}
		}
	}
}
