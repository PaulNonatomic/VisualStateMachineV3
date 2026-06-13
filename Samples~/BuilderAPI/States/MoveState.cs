using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.BuilderAPI.States
{
	/// <summary>
	/// Movement state that moves the context transform based on input.
	/// </summary>
	[StateInfo(
		"Player is moving",
		"Reads movement input and translates the context transform. Transitions to idle when input stops.")]
	[StateStyle(Color = StateColors.Green, Icon = NodeIcon.Run)]
	public class MoveState : State
	{
		[DataIn("Speed")]
		public float Speed = 5f;

		[DataOut("Distance")]
		public float DistanceTraveled;

		[Transition("On Stop")]
		public Action OnStop;

		[Transition("On Jump")]
		public Action OnJump;

		private float _distanceTraveled;

		public override void OnEnter()
		{
			_distanceTraveled = 0f;
			Log("Entered Move state");
		}

		public override void OnUpdate()
		{
			if (Input.GetKeyDown(KeyCode.Space))
			{
				OnJump?.Invoke();
				return;
			}

			var horizontal = Input.GetAxis("Horizontal");
			var vertical = Input.GetAxis("Vertical");
			var input = new Vector3(horizontal, 0f, vertical);

			if (input.magnitude < 0.1f)
			{
				OnStop?.Invoke();
				return;
			}

			if (Transform != null)
			{
				var movement = input.normalized * Speed * Time.deltaTime;
				Transform.position += movement;
				_distanceTraveled += movement.magnitude;
			}
		}

		public override void OnExit()
		{
			DistanceTraveled = _distanceTraveled;
		}
	}
}
