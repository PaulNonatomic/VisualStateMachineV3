using System;
using UnityEngine;

namespace Nonatomic.VSM.Samples.SiegeTank
{
	/// <summary>
	/// Configuration for siege tank stats.
	/// Serializable class that can be edited in the inspector.
	/// </summary>
	[Serializable]
	public class SiegeTankConfig
	{
		[Header("Tank Mode")] [Tooltip("Damage dealt per shot in Tank Mode.")]
		public float tankModeDamage = 15f;

		[Tooltip("Attack range in Tank Mode.")]
		public float tankModeRange = 7f;

		[Tooltip("Time between shots in Tank Mode (seconds).")]
		public float tankModeFireRate = 1f;

		[Tooltip("Movement speed in units per second.")]
		public float moveSpeed = 4f;

		[Tooltip("Rotation speed in degrees per second.")]
		public float rotationSpeed = 120f;

		[Header("Siege Mode")] [Tooltip("Damage dealt per shot in Siege Mode.")]
		public float siegeModeDamage = 70f;

		[Tooltip("Attack range in Siege Mode.")]
		public float siegeModeRange = 13f;

		[Tooltip("Time between shots in Siege Mode (seconds).")]
		public float siegeModeFireRate = 2.5f;

		[Tooltip("Minimum attack range in Siege Mode - cannot hit targets closer than this.")]
		public float siegeModeMinRange = 2f;

		[Header("Transformation")] [Tooltip("Time to transform from Tank Mode to Siege Mode (seconds).")]
		public float siegeUpDuration = 2f;

		[Tooltip("Time to transform from Siege Mode to Tank Mode (seconds).")]
		public float unsiegeDuration = 1.5f;
	}
}
