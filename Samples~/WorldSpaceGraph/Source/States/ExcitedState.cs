using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.WorldSpaceGraph
{
	/// <summary>
	/// The creature jiggles in place after being poked.
	/// </summary>
	[StateInfo(
		Tooltip = "Jiggle in place when poked",
		Description = "Plays a brief jiggle animation, then transitions to Idle.")]
	[StateStyle(Color = StateColors.Orange)]
	public class ExcitedState : State<CreatureController>
	{
		[DataIn("Settle Duration")]
		public float SettleDuration = 1.25f;

		[Transition("On Settled")]
		public Action OnSettled;

		private float _enterTime;
		private Vector3 _baseScale;

		public override void OnEnter()
		{
			_enterTime = Time.time;
			if (SharedContext != null)
			{
				_baseScale = SharedContext.Body.localScale;
			}
		}

		public override void OnUpdate()
		{
			if (SharedContext == null) return;

			var elapsed = Time.time - _enterTime;
			var jiggle = 1f + Mathf.Sin(elapsed * 30f) * 0.12f;
			SharedContext.Body.localScale = _baseScale * jiggle;

			if (elapsed >= SettleDuration)
			{
				SharedContext.Body.localScale = _baseScale;
				OnSettled?.Invoke();
			}
		}

		public override void OnExit()
		{
			if (SharedContext != null)
			{
				SharedContext.Body.localScale = _baseScale;
			}
		}
	}
}
