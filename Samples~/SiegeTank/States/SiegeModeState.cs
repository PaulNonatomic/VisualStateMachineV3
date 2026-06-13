using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.SiegeTank.States
{
	/// <summary>
	/// Siege Mode - Immobile but devastating firepower.
	/// High damage, long range, but cannot move and has minimum range.
	/// "In position."
	/// </summary>
	[StateStyle(Color = StateColors.Red)]
	[StateInfo(
		Tooltip = "Artillery mode - maximum firepower",
		Description = "Tank is deployed in Siege Mode with devastating long-range firepower. " +
					  "Cannot move. Has a minimum range blind spot. Press E to return to Tank Mode.",
		Category = "Combat/Vehicle"
	)]
	public class SiegeModeState : State<SiegeTank>
	{
		[Transition("On Unsiege Command")] public Action OnUnsiegeCommand;

		public override void OnEnter()
		{
			SharedContext.IsSieged = true;
			SharedContext.IsTransforming = false;

			// Apply siege mode stats - the big guns!
			SharedContext.CurrentDamage = SharedContext.Config.siegeModeDamage;
			SharedContext.CurrentRange = SharedContext.Config.siegeModeRange;
			SharedContext.CurrentFireRate = SharedContext.Config.siegeModeFireRate;

			// Cancel any move orders - we're locked in position
			SharedContext.HasMoveOrder = false;

			SharedContext.Animator?.SetBool("IsSieged", true);

			Log("Siege Mode - In position! Ready to unleash hell!");
		}

		public override void OnUpdate()
		{
			HandleSiegeCombat();
			CheckUnsiegeCommand();
		}

		private void HandleSiegeCombat()
		{
			if (SharedContext.CurrentTarget == null) return;

			var distance = Vector3.Distance(
				SharedContext.transform.position,
				SharedContext.CurrentTarget.position
			);

			// Check minimum range - siege tanks can't hit targets too close
			if (distance < SharedContext.Config.siegeModeMinRange)
			{
				// Target too close! This is the siege tank's weakness
				return;
			}

			// Check maximum range
			if (distance > SharedContext.CurrentRange) return;

			// Check fire rate (slower in siege mode but hits harder)
			if (Time.time - SharedContext.LastFireTime < SharedContext.CurrentFireRate) return;

			// FIRE THE SIEGE CANNONS!
			SharedContext.LastFireTime = Time.time;
			Log($"SIEGE CANNON FIRING! Massive damage: {SharedContext.CurrentDamage}!");

			// In a real implementation:
			// - Play siege cannon sound effect
			// - Spawn explosion visual at target
			// - Deal splash damage in area
			// SharedContext.CurrentTarget.GetComponent<IDamageable>()?.TakeDamage(SharedContext.CurrentDamage);
		}

		private void CheckUnsiegeCommand()
		{
			// Check for unsiege command (same key as siege for toggle)
			if (Input.GetKeyDown(KeyCode.E))
			{
				OnUnsiegeCommand?.Invoke();
			}
		}

		public override void OnExit()
		{
			Log("Leaving Siege Mode...");
		}
	}
}
