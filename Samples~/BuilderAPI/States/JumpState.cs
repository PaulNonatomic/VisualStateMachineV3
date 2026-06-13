using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.BuilderAPI.States
{
	/// <summary>
	/// Jump state that simulates a simple arc and lands.
	/// </summary>
	[StateInfo(
		"Player is jumping",
		"Simulates a vertical jump arc. Returns to idle when landing.")]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.ArrowUp)]
	public class JumpState : State
	{
		[DataIn("Jump Height")]
		public float JumpHeight = 2f;

		[DataIn("Jump Duration")]
		public float JumpDuration = 0.6f;

		[Transition("On Land")]
		public Action OnLand;

		private float _elapsed;
		private float _startY;

		public override void OnEnter()
		{
			_elapsed = 0f;
			_startY = Transform != null ? Transform.position.y : 0f;
			Log("Entered Jump state");
		}

		public override void OnUpdate()
		{
			_elapsed += Time.deltaTime;
			var t = Mathf.Clamp01(_elapsed / JumpDuration);

			// Parabolic arc: 4h * t * (1 - t) peaks at h when t = 0.5
			var height = 4f * JumpHeight * t * (1f - t);

			if (Transform != null)
			{
				var pos = Transform.position;
				pos.y = _startY + height;
				Transform.position = pos;
			}

			if (t >= 1f)
			{
				if (Transform != null)
				{
					var pos = Transform.position;
					pos.y = _startY;
					Transform.position = pos;
				}

				OnLand?.Invoke();
			}
		}
	}
}
