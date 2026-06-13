using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.WorldSpaceGraph
{
	/// <summary>
	/// The creature stands still until enough time passes to wander, or until the
	/// player pokes it. Time-based transition uses <see cref="IdleDuration"/>.
	/// </summary>
	[StateInfo(
		Tooltip = "Stand still and breathe",
		Description = "Holds position and waits. Transitions to Wander after IdleDuration seconds, or to Excited if poked.")]
	[StateStyle(Color = StateColors.Blue)]
	public class IdleState : State<CreatureController>
	{
		[DataIn("Idle Duration")]
		public float IdleDuration = 3f;

		[Transition("On Bored")]
		public Action OnBored;

		[Transition("On Poked")]
		public Action OnPoked;

		private float _enterTime;

		public override void OnEnter()
		{
			_enterTime = Time.time;
			if (SharedContext != null)
			{
				SharedContext.Target = SharedContext.Body.position;
			}
		}

		public override void OnUpdate()
		{
			if (SharedContext != null && SharedContext.Poked)
			{
				SharedContext.Poked = false;
				OnPoked?.Invoke();
				return;
			}

			if (Time.time - _enterTime >= IdleDuration)
			{
				OnBored?.Invoke();
			}
		}
	}
}
