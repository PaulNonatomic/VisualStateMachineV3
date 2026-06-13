using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.Samples.PatrollingGuard.States
{
	/// <summary>
	/// Gem tweens quickly back to a random position near center, then returns to idle.
	/// </summary>
	[StateInfo(
		"Gem Return",
		"Gem tweens quickly back to a random position near center, then resumes idle.")]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Restart)]
	public class GemReturnState : State<GemController>
	{
		[Transition("Returned")]
		public Action OnReturned;

		public override void OnUpdate()
		{
			SharedContext.MoveTowardReturn();

			if (SharedContext.IsNearPosition(SharedContext.ReturnDestination, 0.1f))
			{
				OnReturned?.Invoke();
			}
		}
	}
}
