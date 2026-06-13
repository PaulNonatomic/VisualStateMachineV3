using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.CompositeStateMachine.States
{
	/// <summary>
	/// Attack windup - preparing to strike.
	/// </summary>
	[StateInfo(
		"Windup",
		"Preparing attack. Character pulls back weapon before striking.")]
	[StateStyle(Color = StateColors.Yellow, Icon = NodeIcon.ArrowLeft)]
	public class WindupState : State<CombatController>
	{
		[Transition("Ready")]
		public Action OnReady;

		private float _elapsed;

		public override void OnEnter()
		{
			_elapsed = 0f;
			SharedContext.SetWindupVisual();
		}

		public override void OnUpdate()
		{
			_elapsed += Time.deltaTime;

			if (_elapsed >= SharedContext.AttackWindupTime)
			{
				OnReady?.Invoke();
			}
		}
	}
}
