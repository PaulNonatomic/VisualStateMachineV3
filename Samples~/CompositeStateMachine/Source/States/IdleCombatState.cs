using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.Samples.CompositeStateMachine.States
{
	/// <summary>
	/// Idle combat state - waiting for player input.
	/// </summary>
	[StateInfo(
		"Idle Combat",
		"Waiting for combat input. Attack with left mouse, defend with right mouse.")]
	[StateStyle(Color = StateColors.Green, Icon = NodeIcon.User)]
	public class IdleCombatState : State<CombatController>
	{
		[Transition("Attack")]
		public Action OnAttack;

		[Transition("Defend")]
		public Action OnDefend;

		public override void OnEnter()
		{
			SharedContext.SetIdleVisual();
		}

		public override void OnUpdate()
		{
			if (SharedContext.ConsumeAttackInput())
			{
				OnAttack?.Invoke();
				return;
			}

			if (SharedContext.ConsumeDefendInput())
			{
				OnDefend?.Invoke();
			}
		}
	}
}
