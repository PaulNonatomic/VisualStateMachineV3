using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.CompositeStateMachine.States
{
	/// <summary>
	/// Attack strike - the damaging part of the attack.
	/// </summary>
	[StateInfo(
		"Strike",
		"Executing attack strike. This is the damaging portion of the attack.")]
	[StateStyle(Color = StateColors.Red, Icon = NodeIcon.ArrowRight)]
	public class StrikeState : State<CombatController>
	{
		[Transition("Complete")]
		public Action OnComplete;

		private float _elapsed;

		public override void OnEnter()
		{
			_elapsed = 0f;
			SharedContext.SetStrikeVisual();
		}

		public override void OnUpdate()
		{
			_elapsed += Time.deltaTime;

			if (_elapsed >= SharedContext.AttackStrikeTime)
			{
				OnComplete?.Invoke();
			}
		}
	}
}
