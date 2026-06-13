using System.Collections.Generic;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.PatrollingGuard
{
	public class ThiefController : MonoBehaviour, IStateMachineContext
	{
		[Header("Movement")]
		[SerializeField] private float _wanderSpeed = 2f;
		[SerializeField] private float _fleeSpeed = 6f;
		[SerializeField] private float _speedAcceleration = 8f;
		[SerializeField] private float _speedDeceleration = 3f;
		[SerializeField] private float _rotationSpeed = 5f;
		[SerializeField] private Rigidbody _rigidbody;

		[Header("Path")]
		[SerializeField] private float _destinationDistance = 8f;
		[SerializeField] private float _minDestinationDistance = 2f;
		[SerializeField] private float _nextPickThreshold = 0.5f;

		[Header("Detection")]
		[SerializeField] private float _awarenessRange = 6f;
		[SerializeField] private float _awarenessAngle = 90f;
		[SerializeField] private float _dangerRange = 10f;
		[SerializeField] private Transform _guard;

		[Header("Bounds")]
		[SerializeField] private float _areaBoundsRadius = 13f;
		[SerializeField] private Vector3 _areaCenter = Vector3.zero;

		[Header("Wall Detection")]
		[SerializeField] private LayerMask _wallLayerMask = ~0;
		[SerializeField] private float _wallDetectionDistance = 4f;
		[SerializeField] private float _rayOriginHeight = 0.5f;

		[Header("Visual Feedback")]
		[SerializeField] private MeshRenderer _stateIndicator;
		[SerializeField] private Color _wanderColor = Color.green;
		[SerializeField] private Color _fleeColor = Color.red;
		[SerializeField] private Color _caughtColor = Color.white;
		[SerializeField] private float _punchScale = 0.5f;
		[SerializeField] private float _punchDuration = 0.3f;

		[Header("Caught")]
		[SerializeField] private float _stunDuration = 1f;

		[Header("Audio")]
		[SerializeField] private AudioSource _audioSource;
		[SerializeField] private AudioClip _fleeSound;
		[SerializeField] private AudioClip _caughtSound;
		[SerializeField] private AudioClip _newPathSound;

		private Vector3 _p0;
		private Vector3 _p1;
		private Vector3 _p2;
		private float _t;
		private float _currentArcLength;
		private float _currentSpeed;
		private float _targetSpeed;
		private Vector3 _nextDestination;
		private bool _hasNextDestination;
		private bool _needsNextDestination;
		private bool _pathInitialized;

		private readonly List<GemController> _collectedGems = new List<GemController>();

		private Vector3 _desiredPosition;
		private Quaternion _desiredRotation;
		private bool _hasDesiredMovement;
		private Vector3 _indicatorBaseScale;
		private float _punchTimer;
		private float _stunTimer;
		private bool _isStunned;

		private const float MinArcLength = 0.5f;
		private const int ArcLengthSamples = 8;

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
			UpdateStun();
		}

		public GameObject GameObject => gameObject;
		public Transform Transform => transform;
		public float WanderSpeed => _wanderSpeed;
		public float FleeSpeed => _fleeSpeed;
		public Transform Guard => _guard;
		public float AwarenessRange => _awarenessRange;
		public float AwarenessAngle => _awarenessAngle;
		public float DangerRange => _dangerRange;
		public bool PathInitialized => _pathInitialized;
		public bool NeedsNextDestination => _needsNextDestination;
		public Vector3 CurrentDestination => _p2;
		public bool IsStunned => _isStunned;
		public float StunDuration => _stunDuration;

		public bool IsGuardNearby()
		{
			if (!IsGuardActive()) return false;

			var direction = _guard.position - transform.position;
			direction.y = 0;

			if (direction.sqrMagnitude > _awarenessRange * _awarenessRange) return false;

			var angle = Vector3.Angle(transform.forward, direction);
			return angle <= _awarenessAngle;
		}

		public bool IsGuardInDangerRange()
		{
			if (!IsGuardActive()) return false;
			var offset = _guard.position - transform.position;
			offset.y = 0;
			return offset.sqrMagnitude <= _dangerRange * _dangerRange;
		}

		private bool IsGuardActive()
		{
			return _guard != null && _guard.gameObject.activeInHierarchy;
		}

		/// <summary>
		/// Sets up the first bezier segment as a straight line from the current
		/// position to the first destination, using the midpoint as the control
		/// point so speed along the curve is uniform from t=0.
		/// </summary>
		public void InitializePath(Vector3 firstDestination)
		{
			var pos = transform.position;
			firstDestination.y = pos.y;

			_p0 = pos;
			_p1 = (pos + firstDestination) * 0.5f;
			_p1.y = pos.y;
			_p2 = firstDestination;
			_t = 0f;
			_currentSpeed = _targetSpeed;
			_currentArcLength = ApproximateArcLength(_p0, _p1, _p2);
			_hasNextDestination = false;
			_needsNextDestination = false;
			_pathInitialized = true;
		}

		/// <summary>
		/// Advances along the current bezier segment, applying position and rotation.
		/// Signals NeedsNextDestination when the pick threshold is crossed.
		/// </summary>
		public void AdvancePath()
		{
			if (!_pathInitialized) return;

			RampSpeed();

			var deltaT = (_currentSpeed * Time.deltaTime) / Mathf.Max(_currentArcLength, MinArcLength);
			_t = Mathf.Min(_t + deltaT, 1f);

			if (_t >= _nextPickThreshold && !_hasNextDestination && !_needsNextDestination)
			{
				_needsNextDestination = true;
			}

			var position = EvaluateQuadraticBezier(_p0, _p1, _p2, _t);
			position.y = transform.position.y;

			var tangent = EvaluateQuadraticBezierTangent(_p0, _p1, _p2, _t);
			tangent.y = 0f;

			ApplyMovement(position, tangent);

			if (_t >= 1f)
			{
				ShiftToNextSegment();
			}
		}

		public void SetTargetSpeed(float speed)
		{
			_targetSpeed = speed;
		}

		/// <summary>
		/// Pre-sets the next waypoint for when the current segment completes.
		/// </summary>
		public void SetNextDestination(Vector3 destination)
		{
			destination.y = transform.position.y;
			_nextDestination = destination;
			_hasNextDestination = true;
			_needsNextDestination = false;
			PlaySound(_newPathSound);
		}

		/// <summary>
		/// Clears the pre-picked destination so the active state can re-pick
		/// with its own strategy (e.g. after a Wander→Flee transition).
		/// </summary>
		public void InvalidateNextDestination()
		{
			_hasNextDestination = false;
		}

		/// <summary>
		/// Raycasts along the bezier tangent direction to detect walls ahead.
		/// </summary>
		public bool IsWallAhead(out Vector3 wallNormal)
		{
			var tangent = EvaluateQuadraticBezierTangent(_p0, _p1, _p2, _t);
			tangent.y = 0f;

			if (tangent.sqrMagnitude < 0.001f)
			{
				wallNormal = Vector3.zero;
				return false;
			}

			tangent.Normalize();
			var rayOrigin = transform.position + Vector3.up * _rayOriginHeight;

			if (Physics.Raycast(rayOrigin, tangent, out var hit, _wallDetectionDistance, _wallLayerMask))
			{
				// Ignore the guard and self — only react to static geometry
				if (hit.transform == transform || hit.transform == _guard)
				{
					wallNormal = Vector3.zero;
					return false;
				}

				wallNormal = hit.normal;
				wallNormal.y = 0f;
				wallNormal.Normalize();
				return true;
			}

			wallNormal = Vector3.zero;
			return false;
		}

		/// <summary>
		/// Interrupts the current segment to avoid a wall.
		/// Reflects the current tangent off the wall normal to create a smooth redirect.
		/// </summary>
		public void RedirectFromWall(Vector3 wallNormal, Vector3 newDestination)
		{
			var currentPos = transform.position;
			newDestination.y = currentPos.y;

			var tangent = EvaluateQuadraticBezierTangent(_p0, _p1, _p2, _t);
			tangent.y = 0f;
			tangent.Normalize();

			var reflected = Vector3.Reflect(tangent, wallNormal);
			reflected.y = 0f;
			reflected.Normalize();

			var controlPoint = currentPos + reflected * (_destinationDistance * 0.3f);

			_p0 = currentPos;
			_p1 = controlPoint;
			_p2 = newDestination;
			_t = 0f;
			_currentArcLength = ApproximateArcLength(_p0, _p1, _p2);
			_hasNextDestination = false;
			_needsNextDestination = false;
		}

		/// <summary>
		/// Picks a random point within the bounds area that is at least
		/// _minDestinationDistance from the anchor. Uses the inner 85% of the
		/// bounds so destinations stay away from the boundary edge.
		/// </summary>
		public Vector3 PickRandomDestination(Vector3 anchor)
		{
			for (var attempt = 0; attempt < 10; attempt++)
			{
				var randomCircle = Random.insideUnitCircle;
				var candidate = _areaCenter
					+ new Vector3(randomCircle.x, 0f, randomCircle.y) * (_areaBoundsRadius * 0.85f);
				candidate.y = transform.position.y;

				if (Vector3.Distance(candidate, anchor) >= _minDestinationDistance)
				{
					return candidate;
				}
			}

			var toCenter = _areaCenter - anchor;
			toCenter.y = 0f;
			var fallbackDir = toCenter.sqrMagnitude > 0.01f ? toCenter.normalized : Vector3.forward;
			var fallback = anchor + fallbackDir * _destinationDistance;
			fallback.y = transform.position.y;
			return ClampToBounds(fallback);
		}

		/// <summary>
		/// Picks a destination biased away from the guard with slight random jitter.
		/// </summary>
		public Vector3 PickFleeDestination(Vector3 anchor)
		{
			if (_guard == null) return PickRandomDestination(anchor);

			var awayFromGuard = anchor - _guard.position;
			awayFromGuard.y = 0f;

			if (awayFromGuard.sqrMagnitude < 0.01f) return PickRandomDestination(anchor);

			awayFromGuard.Normalize();
			var jitter = Random.Range(-30f, 30f);
			var fleeDir = Quaternion.Euler(0f, jitter, 0f) * awayFromGuard;
			var candidate = ClampToBounds(anchor + fleeDir * _destinationDistance);

			if (Vector3.Distance(candidate, anchor) >= _minDestinationDistance) return candidate;

			var perpendicular = Vector3.Cross(awayFromGuard, Vector3.up).normalized;
			return ClampToBounds(anchor + perpendicular * _destinationDistance);
		}

		public Vector3 ClampToBounds(Vector3 position)
		{
			var offset = position - _areaCenter;
			offset.y = 0f;

			if (offset.magnitude > _areaBoundsRadius)
			{
				offset = offset.normalized * _areaBoundsRadius;
			}

			return new Vector3(
				_areaCenter.x + offset.x,
				transform.position.y,
				_areaCenter.z + offset.z);
		}

		public void StopMovement()
		{
			if (!_rigidbody) return;

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

		public void SetWanderVisual() => SetStateColor(_wanderColor);

		public void SetFleeVisual()
		{
			SetStateColor(_fleeColor);
			PlaySound(_fleeSound);
		}

		public void CollectGem(GemController gem)
		{
			if (gem.IsCollected) return;
			gem.Collect(transform);
			_collectedGems.Add(gem);
		}

		public void DropAllGems()
		{
			foreach (var gem in _collectedGems)
			{
				if (gem != null) gem.Drop();
			}
			_collectedGems.Clear();
		}

		public void OnCaught()
		{
			DropAllGems();
			StopMovement();
			_isStunned = true;
			_stunTimer = _stunDuration;
			_punchTimer = _punchDuration;
			SetStateColor(_caughtColor);
			PlaySound(_caughtSound);
		}

		private void OnCollisionStay(Collision collision)
		{
			if (!_pathInitialized) return;
			if (collision.transform == _guard) return;

			var contact = collision.GetContact(0);
			var wallNormal = contact.normal;
			wallNormal.y = 0f;

			if (wallNormal.sqrMagnitude < 0.001f) return;

			wallNormal.Normalize();
			var newDest = PickRandomDestination(transform.position);
			RedirectFromWall(wallNormal, newDest);
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
			var punch = Mathf.Sin(t * Mathf.PI) * t * _punchScale;
			var scale = _indicatorBaseScale;
			scale.y += punch;
			_stateIndicator.transform.localScale = scale;
		}

		private void UpdateStun()
		{
			if (!_isStunned) return;

			_stunTimer -= Time.deltaTime;
			if (_stunTimer <= 0f)
			{
				_isStunned = false;
			}
		}

		private void RampSpeed()
		{
			var rate = _currentSpeed < _targetSpeed ? _speedAcceleration : _speedDeceleration;
			_currentSpeed = Mathf.MoveTowards(_currentSpeed, _targetSpeed, rate * Time.deltaTime);
		}

		private void ShiftToNextSegment()
		{
			if (_hasNextDestination)
			{
				// Carry the exit tangent direction into the new segment's control point
				// so the curve continues smoothly without a sharp corner.
				var exitDirection = (_p2 - _p1).normalized;
				var distToNext = Vector3.Distance(_p2, _nextDestination);

				_p0 = _p2;
				_p1 = _p2 + exitDirection * (distToNext * 0.33f);
				_p1.y = _p0.y;
				_p2 = _nextDestination;
				_t = 0f;
				_currentArcLength = ApproximateArcLength(_p0, _p1, _p2);
				_hasNextDestination = false;
				_needsNextDestination = false;
			}
			else
			{
				// Hold at destination until a new one is provided.
				// Keep _t at 1 so AdvancePath re-enters here next frame.
				_needsNextDestination = true;
			}
		}

		private void FixedUpdate()
		{
			if (!_hasDesiredMovement || _rigidbody == null) return;

			_rigidbody.MovePosition(_desiredPosition);
			_rigidbody.MoveRotation(_desiredRotation);
			_hasDesiredMovement = false;
		}

		private void ApplyMovement(Vector3 position, Vector3 tangent)
		{
			var rotation = transform.rotation;

			if (tangent.sqrMagnitude > 0.001f)
			{
				var targetRotation = Quaternion.LookRotation(tangent.normalized);
				rotation = Quaternion.Slerp(
					transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
			}

			if (_rigidbody != null)
			{
				_desiredPosition = position;
				_desiredRotation = rotation;
				_hasDesiredMovement = true;
			}
			else
			{
				transform.position = position;
				transform.rotation = rotation;
			}
		}

		private void SetStateColor(Color color)
		{
			if (_stateIndicator != null)
			{
				_stateIndicator.material.color = color;
			}
		}

		private static Vector3 EvaluateQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
		{
			var u = 1f - t;
			return u * u * p0 + 2f * u * t * p1 + t * t * p2;
		}

		private static Vector3 EvaluateQuadraticBezierTangent(Vector3 p0, Vector3 p1, Vector3 p2, float t)
		{
			return 2f * (1f - t) * (p1 - p0) + 2f * t * (p2 - p1);
		}

		private static float ApproximateArcLength(Vector3 p0, Vector3 p1, Vector3 p2)
		{
			var length = 0f;
			var prev = p0;

			for (var i = 1; i <= ArcLengthSamples; i++)
			{
				var sampleT = (float)i / ArcLengthSamples;
				var point = EvaluateQuadraticBezier(p0, p1, p2, sampleT);
				length += Vector3.Distance(prev, point);
				prev = point;
			}

			return Mathf.Max(length, MinArcLength);
		}

		private void OnDrawGizmos()
		{
			DrawAreaBoundsGizmo();
			DrawDetectionGizmos();
			DrawBezierPathGizmo();
		}

		private void DrawAreaBoundsGizmo()
		{
			var center = _areaCenter;
			center.y = transform.position.y;

			Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
			DrawCircleGizmo(center, _areaBoundsRadius);
		}

		private void DrawDetectionGizmos()
		{
			var origin = transform.position;

			DrawAwarenessWedgeGizmo(origin);

			Gizmos.color = new Color(1f, 0.2f, 0f, 0.3f);
			DrawCircleGizmo(origin, _dangerRange);
		}

		private void DrawAwarenessWedgeGizmo(Vector3 origin)
		{
			var arcSegments = 20;
			var angleStep = (_awarenessAngle * 2f) / arcSegments;

			var leftDir = Quaternion.Euler(0, -_awarenessAngle, 0) * transform.forward;
			var rightDir = Quaternion.Euler(0, _awarenessAngle, 0) * transform.forward;

			Gizmos.color = new Color(1f, 0.9f, 0f, 0.5f);
			Gizmos.DrawLine(origin, origin + leftDir * _awarenessRange);
			Gizmos.DrawLine(origin, origin + rightDir * _awarenessRange);

			var prevPoint = origin + leftDir * _awarenessRange;
			for (var i = 1; i <= arcSegments; i++)
			{
				var angle = -_awarenessAngle + angleStep * i;
				var dir = Quaternion.Euler(0, angle, 0) * transform.forward;
				var point = origin + dir * _awarenessRange;
				Gizmos.DrawLine(prevPoint, point);
				prevPoint = point;
			}

			Gizmos.color = new Color(1f, 0.9f, 0f, 0.1f);
			for (var i = 0; i <= arcSegments; i += 2)
			{
				var angle = -_awarenessAngle + angleStep * i;
				var dir = Quaternion.Euler(0, angle, 0) * transform.forward;
				Gizmos.DrawLine(origin, origin + dir * _awarenessRange);
			}
		}

		private void DrawBezierPathGizmo()
		{
			if (!Application.isPlaying || !_pathInitialized) return;

			Gizmos.color = Color.yellow;
			var prev = _p0;
			for (var i = 1; i <= 20; i++)
			{
				var sampleT = (float)i / 20;
				var point = EvaluateQuadraticBezier(_p0, _p1, _p2, sampleT);
				Gizmos.DrawLine(prev, point);
				prev = point;
			}

			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(_p1, 0.3f);
			Gizmos.color = Color.magenta;
			Gizmos.DrawWireSphere(_p2, 0.3f);

			if (_hasNextDestination)
			{
				Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
				Gizmos.DrawWireSphere(_nextDestination, 0.25f);
			}

			// Wall detection ray
			var tangent = EvaluateQuadraticBezierTangent(_p0, _p1, _p2, _t);
			tangent.y = 0f;
			if (tangent.sqrMagnitude > 0.001f)
			{
				tangent.Normalize();
				var rayOrigin = transform.position + Vector3.up * _rayOriginHeight;

				if (Physics.Raycast(rayOrigin, tangent, out var hit, _wallDetectionDistance, _wallLayerMask))
				{
					Gizmos.color = Color.red;
					Gizmos.DrawLine(rayOrigin, hit.point);
				}
				else
				{
					Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
					Gizmos.DrawLine(rayOrigin, rayOrigin + tangent * _wallDetectionDistance);
				}
			}
		}

		private static void DrawCircleGizmo(Vector3 origin, float radius)
		{
			var segments = 30;
			var angleStep = 360f / segments;
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
