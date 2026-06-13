using System;
using System.Collections.Generic;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.TrafficLight.States
{
	/// <summary>
	/// Sets all traffic light groups to red for a safety clearing phase.
	/// </summary>
	[StateInfo(
		"All red safety phase",
		"Sets all groups to red, providing a safety buffer between direction changes.")]
	[StateStyle(Color = StateColors.Red, Icon = NodeIcon.LightBulb)]
	public class AllRedPhaseState : State
	{
		[DataIn("Groups")]
		public List<TrafficLightGroup> Groups;

		[DataIn("Duration")]
		public float Duration = 1f;

		[DataOut("Elapsed Time")]
		public float ElapsedTime;

		[Transition("Next")]
		public Action OnNext;

		private float _elapsed;
		private bool _started;

		public override void OnEnter()
		{
			_elapsed = 0f;
			_started = false;

			if (Groups != null)
			{
				foreach (var group in Groups)
				{
					group?.SetRed();
				}
			}

			if (Duration <= 0f)
			{
				OnNext?.Invoke();
			}
		}

		public override void OnUpdate()
		{
			if (Duration <= 0f) return;

			if (!_started)
			{
				_started = true;
				return;
			}

			_elapsed += Time.deltaTime;
			ElapsedTime = _elapsed;

			if (_elapsed >= Duration)
			{
				OnNext?.Invoke();
			}
		}
	}
}
