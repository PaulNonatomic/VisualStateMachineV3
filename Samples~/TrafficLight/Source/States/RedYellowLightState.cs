using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.TrafficLight.States
{
	/// <summary>
	/// Activates both red and yellow traffic lights simultaneously.
	/// Used in UK and German traffic light sequences as a "get ready" phase before green.
	/// </summary>
	[StateInfo(
		"Activates the red and yellow lights",
		"Sets the traffic light to red+yellow and waits for the configured duration before transitioning.")]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.LightBulb)]
	public class RedYellowLightState : State
	{
		[DataIn("TrafficLightGroup")]
		public TrafficLightGroup ActiveGroup;

		[DataIn("Duration")]
		public float Duration = 2f;

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

			ActiveGroup?.SetRedYellow();

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
