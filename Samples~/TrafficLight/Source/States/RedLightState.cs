using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.TrafficLight.States
{
	/// <summary>
	/// Activates the red traffic light and waits for the specified duration.
	/// </summary>
	[StateInfo(
		"Activates the red light",
		"Sets the traffic light to red and waits for the configured duration before transitioning.")]
	[StateStyle(Color = StateColors.Red, Icon = NodeIcon.LightBulb)]
	public class RedLightState : State
	{
		[DataIn("TrafficLightGroup")]
		public TrafficLightGroup ActiveGroup;

		[DataIn("Duration")]
		public float Duration = 3f;

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

			ActiveGroup?.SetRed();

			if (Duration <= 0f)
			{
				OnNext?.Invoke();
			}
		}

		public override void OnUpdate()
		{
			if (Duration <= 0f) return;

			// Skip first frame to avoid deltaTime spikes
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
