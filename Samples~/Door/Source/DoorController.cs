using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.Door
{
	/// <summary>
	/// Controls a door that can be opened and closed by pressing E.
	/// Implements IStateMachineContext to provide shared context for states.
	/// </summary>
	public class DoorController : MonoBehaviour, IStateMachineContext
	{
		[Header("Door Settings")]
		[SerializeField] private Transform _doorPivot;
		[SerializeField] private float _openAngle = 90f;
		[SerializeField] private float _openSpeed = 2f;
		[SerializeField] private float _autoCloseDelay = 5f;

		[Header("Interaction")]
		[SerializeField] private float _interactionRange = 5f;
		[SerializeField] private KeyCode _interactKey = KeyCode.E;

		public GameObject GameObject => gameObject;
		public Transform Transform => transform;

		/// <summary>
		/// The pivot point of the door for rotation.
		/// </summary>
		public Transform DoorPivot => _doorPivot != null ? _doorPivot : transform;

		/// <summary>
		/// The target angle when fully open.
		/// </summary>
		public float OpenAngle => _openAngle;

		/// <summary>
		/// How fast the door opens/closes (multiplier for Lerp).
		/// </summary>
		public float OpenSpeed => _openSpeed;

		/// <summary>
		/// Time before the door automatically closes when open.
		/// </summary>
		public float AutoCloseDelay => _autoCloseDelay;

		/// <summary>
		/// Current rotation of the door (0 = closed, OpenAngle = fully open).
		/// </summary>
		public float CurrentAngle { get; set; }

		/// <summary>
		/// Checks if the player is in range and pressing the interact key.
		/// </summary>
		public bool IsInteractPressed()
		{
			if (!Input.GetKeyDown(_interactKey))
				return false;

			// Simple range check from camera
			var camera = Camera.main;
			if (camera == null)
				return false;

			var distance = Vector3.Distance(camera.transform.position, transform.position);
			return distance <= _interactionRange;
		}

		/// <summary>
		/// Applies the current angle to the door pivot.
		/// </summary>
		public void ApplyRotation()
		{
			if (DoorPivot != null)
			{
				DoorPivot.localRotation = Quaternion.Euler(0f, CurrentAngle, 0f);
			}
		}

		private void OnDrawGizmosSelected()
		{
			// Draw interaction range
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(transform.position, _interactionRange);
		}
	}
}
