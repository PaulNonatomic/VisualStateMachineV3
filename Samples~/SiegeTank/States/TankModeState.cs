using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.SiegeTank.States
{
	/// <summary>
	/// Tank Mode - Mobile form with lower damage but can move freely.
	/// "Ready to roll out!"
	/// </summary>
	[StateStyle(Color = StateColors.Blue)]
	[StateInfo(
		Tooltip = "Mobile combat mode",
		Description = "Tank can move freely and engage targets with reduced firepower. " +
					  "Press E while stationary to enter Siege Mode.",
		Category = "Combat/Vehicle"
	)]
	public class TankModeState : State<SiegeTank>
	{
		[Transition("On Siege Command")] public Action OnSiegeCommand;

		public override void OnEnter()
		{
			SharedContext.IsSieged = false;
			SharedContext.IsTransforming = false;

			// Apply tank mode stats
			SharedContext.CurrentDamage = SharedContext.Config.tankModeDamage;
			SharedContext.CurrentRange = SharedContext.Config.tankModeRange;
			SharedContext.CurrentFireRate = SharedContext.Config.tankModeFireRate;

			SharedContext.Animator?.SetBool("IsSieged", false);

			Log("Tank Mode - Ready to roll out!");
		}

		public override void OnUpdate()
		{
			HandleMovement();
			HandleCombat();
			CheckSiegeCommand();
		}

		private void HandleMovement()
		{
			if (!SharedContext.HasMoveOrder) return;

			var position = SharedContext.transform.position;
			var destination = SharedContext.MoveDestination;
			var direction = (destination - position).normalized;
			direction.y = 0; // Keep on ground plane

			var distance = Vector3.Distance(position, destination);

			if (distance < 0.5f)
			{
				SharedContext.HasMoveOrder = false;
				return;
			}

			// Rotate towards destination
			if (direction != Vector3.zero)
			{
				var targetRotation = Quaternion.LookRotation(direction);
				SharedContext.transform.rotation = Quaternion.RotateTowards(
					SharedContext.transform.rotation,
					targetRotation,
					SharedContext.Config.rotationSpeed * Time.deltaTime
				);
			}

			// Move forward
			SharedContext.transform.position += SharedContext.transform.forward *
												SharedContext.Config.moveSpeed * Time.deltaTime;
		}

		private void HandleCombat()
		{
			if (SharedContext.CurrentTarget == null) return;

			var distance = Vector3.Distance(
				SharedContext.transform.position,
				SharedContext.CurrentTarget.position
			);

			// Check if target is in range
			if (distance > SharedContext.CurrentRange) return;

			// Check fire rate
			if (Time.time - SharedContext.LastFireTime < SharedContext.CurrentFireRate) return;

			// Fire!
			SharedContext.LastFireTime = Time.time;
			Log($"Tank Mode firing! Damage: {SharedContext.CurrentDamage}");

			// In a real implementation, you'd deal damage here
			// SharedContext.CurrentTarget.GetComponent<IDamageable>()?.TakeDamage(SharedContext.CurrentDamage);
		}

		private void CheckSiegeCommand()
		{
			// In a real game, this would be triggered by player input
			// For now, we'll use a keyboard shortcut for testing
			if (Input.GetKeyDown(KeyCode.E))
			{
				// Can't siege while moving
				if (SharedContext.HasMoveOrder)
				{
					Log("Cannot siege while moving!");
					return;
				}

				OnSiegeCommand?.Invoke();
			}
		}

		public override void OnExit()
		{
			Log("Leaving Tank Mode...");
		}
	}
}
