using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.CompositeStateMachine.States
{
	/// <summary>
	/// Stagger state - character was hit and is temporarily stunned.
	/// </summary>
	[StateInfo(
		"Stagger",
		"Hit while blocking. Character is stunned briefly before recovering.")]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.Warning)]
	public class StaggerState : State<CombatController>
	{
		[Transition("Recovered")]
		public Action OnRecovered;

		private float _elapsed;

		public override void OnEnter()
		{
			_elapsed = 0f;
			SharedContext.SetStaggerVisual();
		}

		public override void OnUpdate()
		{
			_elapsed += Time.deltaTime;

			if (_elapsed >= SharedContext.StaggerDuration)
			{
				OnRecovered?.Invoke();
			}
		}
	}
}
