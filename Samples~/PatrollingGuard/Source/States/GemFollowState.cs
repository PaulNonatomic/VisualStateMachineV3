using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.Samples.PatrollingGuard.States
{
	/// <summary>
	/// Gem follows the thief with smooth trailing movement.
	/// </summary>
	[StateInfo(
		"Gem Follow",
		"Gem trails behind the thief using smooth damped movement.")]
	[StateStyle(Color = StateColors.LimeGreen, Icon = NodeIcon.Path)]
	public class GemFollowState : State<GemController>
	{
		[Transition("Dropped")]
		public Action OnDropped;

		public override void OnUpdate()
		{
			if (!SharedContext.IsCollected)
			{
				OnDropped?.Invoke();
				return;
			}

			SharedContext.MoveTowardThief();
		}
	}
}
