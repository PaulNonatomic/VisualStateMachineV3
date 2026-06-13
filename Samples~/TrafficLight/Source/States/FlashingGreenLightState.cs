using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.TrafficLight.States
{
	/// <summary>
	/// Flashes the green light on and off as a warning before transitioning.
	/// Used in Japanese traffic light sequences before the yellow phase.
	/// </summary>
	[StateInfo(
		"Flashes the green light",
		"Blinks the green light on and off at a configurable interval, then transitions after the duration.")]
	[StateStyle(Color = StateColors.Green, Icon = NodeIcon.LightBulbOn)]
	public class FlashingGreenLightState : State
	{
		private const string ActiveGroupKey = "ActiveGroup";

		[DataIn("Duration")]
		public float Duration = 2f;

		[DataIn("Flash Interval")]
		public float FlashInterval = 0.25f;

		[DataOut("Elapsed Time")]
		public float ElapsedTime;

		[Transition("Next")]
		public Action OnNext;

		private TrafficLightGroup _activeGroup;
		private float _elapsed;
		private float _flashTimer;
		private bool _greenOn;
		private bool _started;

		public override void OnEnter()
		{
			_elapsed = 0f;
			_flashTimer = 0f;
			_greenOn = true;
			_started = false;

			Blackboard?.TryGet(ActiveGroupKey, out _activeGroup);
			_activeGroup?.SetGreen();

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

			var deltaTime = Time.deltaTime;

			_elapsed += deltaTime;
			ElapsedTime = _elapsed;

			_flashTimer += deltaTime;
			if (_flashTimer >= FlashInterval)
			{
				_flashTimer -= FlashInterval;
				_greenOn = !_greenOn;
				_activeGroup?.SetGreenLight(_greenOn);
			}

			if (_elapsed >= Duration)
			{
				OnNext?.Invoke();
			}
		}
	}
}
