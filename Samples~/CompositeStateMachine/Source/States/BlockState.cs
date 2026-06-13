using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.CompositeStateMachine.States
{
	/// <summary>
	/// Active blocking state - reduces or negates incoming damage.
	/// </summary>
	[StateInfo(
		"Block",
		"Actively blocking. Reduces damage but can be interrupted by heavy attacks.")]
	[StateStyle(Color = StateColors.Blue, Icon = NodeIcon.Shield)]
	public class BlockState : State<CombatController>
	{
		[Transition("Release")]
		public Action OnRelease;

		[Transition("Hit")]
		public Action OnHit;

		[Transition("Timeout")]
		public Action OnTimeout;

		private float _elapsed;

		public override void OnEnter()
		{
			_elapsed = 0f;
			SharedContext.SetBlockVisual();
		}

		public override void OnUpdate()
		{
			_elapsed += Time.deltaTime;

			// Check if hit while blocking
			if (SharedContext.ConsumeHitInput())
			{
				OnHit?.Invoke();
				return;
			}

			// Check for block release
			if (SharedContext.ConsumeDefendRelease())
			{
				OnRelease?.Invoke();
				return;
			}

			// Check for block timeout
			if (_elapsed >= SharedContext.BlockDuration)
			{
				OnTimeout?.Invoke();
			}
		}
	}
}
