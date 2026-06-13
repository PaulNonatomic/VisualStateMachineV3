using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.SiegeTank.States
{
	/// <summary>
	/// Unsieging - Transformation from Siege Mode back to Tank Mode.
	/// The tank packs up its siege cannons. Still vulnerable during transformation.
	/// </summary>
	[StateStyle(Color = StateColors.Orange)]
	[StateInfo(
		Tooltip = "Packing up siege cannons",
		Description = "Tank is transforming back to Tank Mode. Cannot move or attack during this " +
					  "1.5 second transformation. Vulnerable to enemy fire.",
		Category = "Combat/Vehicle"
	)]
	public class UnsiegingState : State<SiegeTank>
	{
		[Transition("On Unsiege Complete")] public Action OnUnsiegeComplete;

		private float _transformStartTime;
		private float _transformProgress;

		public override void OnEnter()
		{
			SharedContext.IsTransforming = true;
			_transformStartTime = Time.time;
			_transformProgress = 0f;

			SharedContext.Animator?.SetTrigger("Unsiege");

			Log("Packing up... Returning to mobile configuration!");
		}

		public override void OnUpdate()
		{
			// Calculate transformation progress
			_transformProgress = (Time.time - _transformStartTime) / SharedContext.Config.unsiegeDuration;

			// Lerp stats during transformation
			SharedContext.CurrentDamage = Mathf.Lerp(
				SharedContext.Config.siegeModeDamage,
				SharedContext.Config.tankModeDamage,
				_transformProgress
			);

			SharedContext.CurrentRange = Mathf.Lerp(
				SharedContext.Config.siegeModeRange,
				SharedContext.Config.tankModeRange,
				_transformProgress
			);

			// Check if transformation is complete
			if (_transformProgress >= 1f)
			{
				OnUnsiegeComplete?.Invoke();
			}
		}

		public override void OnExit()
		{
			SharedContext.IsTransforming = false;
			Log("Back to Tank Mode - Ready to move out!");
		}
	}
}
