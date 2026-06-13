using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.SiegeTank.States
{
	/// <summary>
	/// Sieging Up - Transformation from Tank Mode to Siege Mode.
	/// The tank deploys its siege cannons. Vulnerable during transformation.
	/// </summary>
	[StateStyle(Color = StateColors.Yellow)]
	[StateInfo(
		Tooltip = "Deploying siege cannons",
		Description = "Tank is transforming into Siege Mode. Cannot move or attack during this " +
					  "2 second transformation. Vulnerable to enemy fire.",
		Category = "Combat/Vehicle"
	)]
	public class SiegingUpState : State<SiegeTank>
	{
		[Transition("On Siege Complete")] public Action OnSiegeComplete;

		private float _transformStartTime;
		private float _transformProgress;

		public override void OnEnter()
		{
			SharedContext.IsTransforming = true;
			_transformStartTime = Time.time;
			_transformProgress = 0f;

			SharedContext.Animator?.SetTrigger("SiegeUp");

			Log("Sieging up... Deploying siege cannons!");
		}

		public override void OnUpdate()
		{
			// Calculate transformation progress
			_transformProgress = (Time.time - _transformStartTime) / SharedContext.Config.siegeUpDuration;

			// Lerp stats during transformation (optional visual feedback)
			SharedContext.CurrentDamage = Mathf.Lerp(
				SharedContext.Config.tankModeDamage,
				SharedContext.Config.siegeModeDamage,
				_transformProgress
			);

			SharedContext.CurrentRange = Mathf.Lerp(
				SharedContext.Config.tankModeRange,
				SharedContext.Config.siegeModeRange,
				_transformProgress
			);

			// Check if transformation is complete
			if (_transformProgress >= 1f)
			{
				OnSiegeComplete?.Invoke();
			}
		}

		public override void OnExit()
		{
			SharedContext.IsTransforming = false;
			Log("Siege mode transformation complete!");
		}
	}
}
