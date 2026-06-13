using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.Samples.PatrollingGuard.States
{
	/// <summary>
	/// Gem bobs and spins at its position, waiting to be collected.
	/// </summary>
	[StateInfo(
		"Gem Idle",
		"Gem sits at its position, gently bobbing and spinning, waiting to be collected.")]
	[StateStyle(Color = StateColors.Dijon, Icon = NodeIcon.Sparks)]
	public class GemIdleState : State<GemController>
	{
		[Transition("Collected")]
		public Action OnCollected;

		public override void OnUpdate()
		{
			if (SharedContext.IsCollected)
			{
				OnCollected?.Invoke();
				return;
			}

			SharedContext.ApplyBobAndSpin();
		}
	}
}
