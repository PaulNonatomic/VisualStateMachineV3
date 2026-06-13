using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.CompositeStateMachine.States
{
	/// <summary>
	/// Post-attack recovery - vulnerable period after attacking.
	/// </summary>
	[StateInfo(
		"Recovery",
		"Recovering from attack. Character is vulnerable during this phase.")]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Clock)]
	public class RecoveryState : State<CombatController>
	{
		[Transition("Recovered")]
		public Action OnRecovered;

		private float _elapsed;

		public override void OnEnter()
		{
			_elapsed = 0f;
			SharedContext.SetRecoveryVisual();
		}

		public override void OnUpdate()
		{
			_elapsed += Time.deltaTime;

			if (_elapsed >= SharedContext.AttackRecoveryTime)
			{
				OnRecovered?.Invoke();
			}
		}
	}
}
