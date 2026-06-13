using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.Door.States
{
	/// <summary>
	/// Door is animating from closed to open.
	/// </summary>
	[StateInfo(
		"Door is opening",
		"Lerps the door rotation from current angle to fully open.")]
	[StateStyle(Color = StateColors.Cyan, Icon = NodeIcon.Play)]
	public class OpeningState : State<DoorController>
	{
		[DataOut("Progress")]
		public float Progress;

		[Transition("Opened")]
		public Action OnOpened;

		public override void OnUpdate()
		{
			var target = SharedContext.OpenAngle;
			var current = SharedContext.CurrentAngle;

			// Lerp toward open
			SharedContext.CurrentAngle = Mathf.Lerp(current, target, Time.deltaTime * SharedContext.OpenSpeed);
			SharedContext.ApplyRotation();

			Progress = Mathf.Clamp01(SharedContext.CurrentAngle / target);

			// Check if fully open (within tolerance)
			if (Mathf.Abs(SharedContext.CurrentAngle - target) < 0.5f)
			{
				SharedContext.CurrentAngle = target;
				SharedContext.ApplyRotation();
				OnOpened?.Invoke();
			}
		}
	}
}
