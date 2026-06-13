using System.Collections.Generic;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.PatrollingGuard
{
	/// <summary>
	/// Controls an AI guard that patrols between waypoints and can detect/chase targets.
	/// Implements IStateMachineContext to provide shared context for states.
	/// </summary>
	public class GuardController : MonoBehaviour, IStateMachineContext
	{
		[Header("Movement")]
		[SerializeField] private float _moveSpeed = 3f;
		[SerializeField] private float _chaseSpeed = 5f;
		[SerializeField] private float _rotationSpeed = 5f;
		[SerializeField] private Rigidbody _rigidbody;

		[Header("Patrol")]
		[SerializeField] private List<Transform> _waypoints = new List<Transform>();
		[SerializeField] private float _waypointReachDistance = 0.5f;
		[SerializeField] private float _minIdleTime = 1f;
		[SerializeField] private float _maxIdleTime = 3f;

		[Header("Detection")]
		[SerializeField] private float _detectionRange = 8f;
		[SerializeField] private float _detectionAngle = 60f;
		[SerializeField] private float _chaseDistance = 12f;
		[SerializeField] private float _chaseLeashDistance = 8f;
		[SerializeField] private LayerMask _targetLayer = -1;
		[SerializeField] private Transform _target;

		[Header("Visual Feedback")]
		[SerializeField] private MeshRenderer _stateIndicator;
		[SerializeField] private Color _patrolColor = Color.green;
		[SerializeField] private Color _alertColor = Color.yellow;
		[SerializeField] private Color _chaseColor = Color.red;
		[SerializeField] private float _punchScale = 0.4f;
		[SerializeField] private float _punchDuration = 0.3f;

		[Header("Audio")]
		[SerializeField] private AudioSource _audioSource;
		[SerializeField] private AudioClip _alertSound;
		[SerializeField] private AudioClip _chaseSound;
		[SerializeField] private AudioClip _caughtSound;
		[SerializeField] private AudioClip _turnSound;

		[Header("State Cooldown")]
		[SerializeField] private float _stateCooldown = 0.5f;

		private Vector3 _desiredPosition;
		private Quaternion _desiredRotation;
		private bool _hasDesiredMovement;
		private Vector3 _indicatorBaseScale;
		private float _punchTimer;
		private float _lastStateChangeTime = float.NegativeInfinity;

		private void Awake()
		{
			if (_stateIndicator != null)
			{
				_indicatorBaseScale = _stateIndicator.transform.localScale;
			}

			if (_rigidbody == null) return;

			_rigidbody.isKinematic = false;
			_rigidbody.useGravity = false;
			_rigidbody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
			_rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		}

		private void Update()
		{
			UpdateIndicatorPunch();
		}

		public GameObject GameObject => gameObject;
		public Transform Transform => transform;

		/// <summary>
		/// Movement speed during patrol.
		/// </summary>
		public float MoveSpeed => _moveSpeed;

		/// <summary>
		/// Movement speed when chasing.
		/// </summary>
		public float ChaseSpeed => _chaseSpeed;

		/// <summary>
		/// How fast the guard rotates.
		/// </summary>
		public float RotationSpeed => _rotationSpeed;

		/// <summary>
		/// List of patrol waypoints.
		/// </summary>
		public IReadOnlyList<Transform> Waypoints => _waypoints;

		/// <summary>
		/// Current waypoint index.
		/// </summary>
		public int CurrentWaypointIndex { get; set; }

		/// <summary>
		/// Distance to consider a waypoint reached.
		/// </summary>
		public float WaypointReachDistance => _waypointReachDistance;

		/// <summary>
		/// Minimum idle time at waypoints.
		/// </summary>
		public float MinIdleTime => _minIdleTime;

		/// <summary>
		/// Maximum idle time at waypoints.
		/// </summary>
		public float MaxIdleTime => _maxIdleTime;

		/// <summary>
		/// Detection range in units.
		/// </summary>
		public float DetectionRange => _detectionRange;

		/// <summary>
		/// Half-angle of the detection cone in degrees.
		/// </summary>
		public float DetectionAngle => _detectionAngle;

		/// <summary>
		/// Chase range distance.
		/// </summary>
		public float ChaseDistance => _chaseDistance;

		/// <summary>
		/// Distance at which the guard gives up chasing.
		/// </summary>
		public float ChaseLeashDistance => _chaseLeashDistance;

		/// <summary>
		/// The currently detected/chased target.
		/// </summary>
		public Transform Target => _target;

		/// <summary>
		/// Last known position of the target.
		/// </summary>
		public Vector3 LastKnownTargetPosition { get; set; }

		/// <summary>
		/// Position to return to after losing target.
		/// </summary>
		public Vector3 ReturnPosition { get; set; }

		/// <summary>
		/// Gets the current waypoint transform.
		/// </summary>
		public Transform CurrentWaypoint
		{
			get
			{
				if (_waypoints.Count == 0) return null;
				return _waypoints[CurrentWaypointIndex % _waypoints.Count];
			}
		}

		/// <summary>
		/// Advances to the next waypoint.
		/// </summary>
		public void NextWaypoint()
		{
			if (_waypoints.Count == 0) return;
			CurrentWaypointIndex = (CurrentWaypointIndex + 1) % _waypoints.Count;
			PlaySound(_turnSound);
		}

		/// <summary>
		/// Sets CurrentWaypointIndex to the waypoint nearest to the guard's current position.
		/// </summary>
		public void SetNearestWaypoint()
		{
			if (_waypoints.Count == 0) return;

			var nearestIndex = 0;
			var nearestSqrDist = float.MaxValue;

			for (var i = 0; i < _waypoints.Count; i++)
			{
				if (_waypoints[i] == null) continue;

				var offset = _waypoints[i].position - transform.position;
				offset.y = 0;
				var sqrDist = offset.sqrMagnitude;

				if (sqrDist >= nearestSqrDist) continue;

				nearestSqrDist = sqrDist;
				nearestIndex = i;
			}

			CurrentWaypointIndex = nearestIndex;
		}

		/// <summary>
		/// Checks if the target is within detection range and angle.
		/// </summary>
		public bool CanSeeTarget()
		{
			if (_target == null) return false;

			var directionToTarget = _target.position - transform.position;
			directionToTarget.y = 0;
			var distance = directionToTarget.magnitude;

			if (distance > _detectionRange) return false;

			var angle = Vector3.Angle(transform.forward, directionToTarget);
			if (angle > _detectionAngle) return false;

			return true;
		}

		/// <summary>
		/// Checks if the target is within chase range.
		/// </summary>
		public bool IsTargetInChaseRange()
		{
			if (_target == null) return false;
			var offset = _target.position - transform.position;
			offset.y = 0;
			return offset.sqrMagnitude <= _chaseLeashDistance * _chaseLeashDistance;
		}

		/// <summary>
		/// Moves toward a position at the specified speed.
		/// </summary>
		public void MoveToward(Vector3 targetPosition, float speed)
		{
			var direction = (targetPosition - transform.position);
			direction.y = 0;

			if (!(direction.sqrMagnitude > 0.01f))
			{
				StopMovement();
				return;
			}

			direction.Normalize();
			var step = speed * Time.deltaTime;
			var newPosition = transform.position + direction * step;
			var targetRotation = Quaternion.LookRotation(direction);
			var smoothedRotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);

			if (_rigidbody != null)
			{
				_desiredPosition = newPosition;
				_desiredRotation = smoothedRotation;
				_hasDesiredMovement = true;
			}
			else
			{
				transform.position = newPosition;
				transform.rotation = smoothedRotation;
			}
		}

		private void FixedUpdate()
		{
			if (!_hasDesiredMovement || _rigidbody == null) return;

			_rigidbody.MovePosition(_desiredPosition);
			_rigidbody.MoveRotation(_desiredRotation);
			_hasDesiredMovement = false;
		}

		/// <summary>
		/// Stops horizontal movement by zeroing Rigidbody velocity.
		/// </summary>
		public void StopMovement()
		{
			if (_rigidbody == null) return;

#if UNITY_6000_0_OR_NEWER
			var velocity = _rigidbody.linearVelocity;
			velocity.x = 0f;
			velocity.z = 0f;
			_rigidbody.linearVelocity = velocity;
#else
			var velocity = _rigidbody.velocity;
			velocity.x = 0f;
			velocity.z = 0f;
			_rigidbody.velocity = velocity;
#endif
		}

		/// <summary>
		/// Checks if the guard is close enough to a position.
		/// </summary>
		public bool IsNearPosition(Vector3 position, float threshold)
		{
			var offset = position - transform.position;
			offset.y = 0;
			return offset.sqrMagnitude <= threshold * threshold;
		}

		/// <summary>
		/// Whether a state transition cooldown is active.
		/// </summary>
		public bool IsOnCooldown => Time.time - _lastStateChangeTime < _stateCooldown;

		/// <summary>
		/// Sets the visual indicator color and triggers a Y-axis scale punch.
		/// </summary>
		public void SetStateColor(Color color)
		{
			_lastStateChangeTime = Time.time;

			if (_stateIndicator == null) return;

			_stateIndicator.material.color = color;
			_punchTimer = _punchDuration;
		}

		/// <summary>
		/// Sets patrol color.
		/// </summary>
		public void SetPatrolVisual() => SetStateColor(_patrolColor);

		/// <summary>
		/// Sets alert color and plays alert sound.
		/// </summary>
		public void SetAlertVisual()
		{
			SetStateColor(_alertColor);
			PlaySound(_alertSound);
		}

		/// <summary>
		/// Sets chase color and plays chase sound.
		/// </summary>
		public void SetChaseVisual()
		{
			SetStateColor(_chaseColor);
			PlaySound(_chaseSound);
		}

		/// <summary>
		/// Plays the caught sound effect.
		/// </summary>
		public void PlayCaughtSound()
		{
			PlaySound(_caughtSound);
		}

		private void PlaySound(AudioClip clip)
		{
			if (_audioSource != null && clip != null)
			{
				_audioSource.PlayOneShot(clip);
			}
		}

		private void UpdateIndicatorPunch()
		{
			if (_stateIndicator == null || _punchTimer <= 0f) return;

			_punchTimer -= Time.deltaTime;
			var t = Mathf.Clamp01(_punchTimer / _punchDuration);

			// Damped sine punch: peaks early then settles back to base scale
			var punch = Mathf.Sin(t * Mathf.PI) * t * _punchScale;
			var scale = _indicatorBaseScale;
			scale.y += punch;
			_stateIndicator.transform.localScale = scale;
		}

		private void OnDrawGizmos()
		{
			DrawDetectionArcGizmo();
			DrawChaseRangeGizmos();
			DrawPatrolPathGizmo();
		}

		private void DrawPatrolPathGizmo()
		{
			if (_waypoints == null || _waypoints.Count < 2) return;

			Gizmos.color = Color.cyan;

			for (var i = 0; i < _waypoints.Count; i++)
			{
				var waypoint = _waypoints[i];
				if (waypoint == null) continue;

				Gizmos.DrawWireSphere(waypoint.position, 0.3f);

				var next = _waypoints[(i + 1) % _waypoints.Count];
				if (next == null) continue;

				Gizmos.DrawLine(waypoint.position, next.position);

				// Direction indicator
				var directionPoint = Vector3.Lerp(waypoint.position, next.position, 0.25f);
				Gizmos.DrawSphere(directionPoint, 0.1f);
			}

		}

		private void DrawDetectionArcGizmo()
		{
			var origin = transform.position;
			var arcSegments = 20;
			var angleStep = (_detectionAngle * 2f) / arcSegments;

			// Draw cone edges
			var leftDir = Quaternion.Euler(0, -_detectionAngle, 0) * transform.forward;
			var rightDir = Quaternion.Euler(0, _detectionAngle, 0) * transform.forward;

			Gizmos.color = new Color(1f, 0.9f, 0f, 0.6f);
			Gizmos.DrawLine(origin, origin + leftDir * _detectionRange);
			Gizmos.DrawLine(origin, origin + rightDir * _detectionRange);

			// Draw arc
			var prevPoint = origin + leftDir * _detectionRange;
			for (var i = 1; i <= arcSegments; i++)
			{
				var angle = -_detectionAngle + angleStep * i;
				var dir = Quaternion.Euler(0, angle, 0) * transform.forward;
				var point = origin + dir * _detectionRange;
				Gizmos.DrawLine(prevPoint, point);
				prevPoint = point;
			}

			// Inner fill lines for visual density
			Gizmos.color = new Color(1f, 0.9f, 0f, 0.1f);
			for (var i = 0; i <= arcSegments; i += 2)
			{
				var angle = -_detectionAngle + angleStep * i;
				var dir = Quaternion.Euler(0, angle, 0) * transform.forward;
				Gizmos.DrawLine(origin, origin + dir * _detectionRange);
			}
		}

		private void DrawChaseRangeGizmos()
		{
			var origin = transform.position;
			var circleSegments = 30;
			var angleStep = 360f / circleSegments;

			// Chase leash range
			Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.4f);
			DrawCircleGizmo(origin, _chaseLeashDistance, circleSegments, angleStep);

			// Full chase range (dimmer)
			Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
			DrawCircleGizmo(origin, _chaseDistance, circleSegments, angleStep);
		}

		private static void DrawCircleGizmo(Vector3 origin, float radius, int segments, float angleStep)
		{
			var prevPoint = origin + Vector3.forward * radius;
			for (var i = 1; i <= segments; i++)
			{
				var dir = Quaternion.Euler(0, angleStep * i, 0) * Vector3.forward;
				var point = origin + dir * radius;
				Gizmos.DrawLine(prevPoint, point);
				prevPoint = point;
			}
		}
	}
}
