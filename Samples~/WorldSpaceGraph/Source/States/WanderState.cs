using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.Samples.WorldSpaceGraph
{
	/// <summary>
	/// The creature picks a random nearby point and walks to it.
	/// </summary>
	[StateInfo(
		Tooltip = "Pick a random point and walk there",
		Description = "Selects a random destination within wander radius on enter, then steps the body toward it each frame. Transitions to Idle on arrival.")]
	[StateStyle(Color = StateColors.LimeGreen)]
	public class WanderState : State<CreatureController>
	{
		[Transition("On Arrived")]
		public Action OnArrived;

		public override void OnEnter()
		{
			if (SharedContext == null) return;
			SharedContext.Target = SharedContext.PickWanderPoint();
		}

		public override void OnUpdate()
		{
			if (SharedContext == null) return;

			SharedContext.StepTowardTarget();
			if (SharedContext.ReachedTarget())
			{
				OnArrived?.Invoke();
			}
		}
	}
}
