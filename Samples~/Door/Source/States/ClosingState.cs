using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.Door.States
{
	/// <summary>
	/// Door is animating from open to closed.
	/// </summary>
	[StateInfo(
		"Door is closing",
		"Lerps the door rotation from current angle to fully closed.")]
	[StateStyle(Color = StateColors.Cyan, Icon = NodeIcon.Play)]
	public class ClosingState : State<DoorController>
	{
		[DataOut("Progress")]
		public float Progress;

		[Transition("Closed")]
		public Action OnClosed;

		public override void OnUpdate()
		{
			var current = SharedContext.CurrentAngle;

			// Lerp toward closed
			SharedContext.CurrentAngle = Mathf.Lerp(current, 0f, Time.deltaTime * SharedContext.OpenSpeed);
			SharedContext.ApplyRotation();

			Progress = 1f - Mathf.Clamp01(SharedContext.CurrentAngle / SharedContext.OpenAngle);

			// Check if fully closed (within tolerance)
			if (Mathf.Abs(SharedContext.CurrentAngle) < 0.5f)
			{
				SharedContext.CurrentAngle = 0f;
				SharedContext.ApplyRotation();
				OnClosed?.Invoke();
			}
		}
	}
}
