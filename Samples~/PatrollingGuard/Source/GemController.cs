using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.PatrollingGuard
{
	/// <summary>
	/// Controls a collectible gem. Implements IStateMachineContext to provide
	/// shared context for gem states (idle, follow, return).
	/// </summary>
	public class GemController : MonoBehaviour, IStateMachineContext
	{
		[Header("Follow")]
		[SerializeField] private float _followSpeed = 8f;
		[SerializeField] private float _followSmoothTime = 0.15f;
		[SerializeField] private float _minFollowDistance = 1.2f;

		[Header("Return")]
		[SerializeField] private float _returnSpeed = 12f;
		[SerializeField] private float _returnMinRadius = 3f;
		[SerializeField] private float _returnMaxRadius = 5f;

		[Header("Idle Animation")]
		[SerializeField] private float _bobAmplitude = 0.15f;
		[SerializeField] private float _bobFrequency = 2f;
		[SerializeField] private float _rotationSpeed = 90f;

		[Header("Punch")]
		[SerializeField] private float _punchScale = 0.3f;
		[SerializeField] private float _punchDuration = 0.25f;

		[Header("Audio")]
		[SerializeField] private AudioSource _audioSource;
		[SerializeField] private AudioClip _collectSound;
		[SerializeField] private AudioClip _dropSound;

		private Transform _thief;
		private Vector3 _returnDestination;
		private bool _isCollected;
		private Vector3 _velocity;
		private float _baseY;
		private Vector3 _baseScale;
		private float _punchTimer;

		public GameObject GameObject => gameObject;
		public Transform Transform => transform;
		public Transform Thief => _thief;
		public bool IsCollected => _isCollected;
		public Vector3 ReturnDestination => _returnDestination;

		private void Start()
		{
			_baseY = transform.position.y;
			_baseScale = transform.localScale;
			_returnDestination = transform.position;
		}

		private void Update()
		{
			UpdatePunch();
		}

		public void Collect(Transform thief)
		{
			_thief = thief;
			_isCollected = true;
			_velocity = Vector3.zero;
			_punchTimer = _punchDuration;
			PlaySound(_collectSound);
		}

		public void Drop()
		{
			_thief = null;
			_isCollected = false;
			_returnDestination = PickRandomReturnPosition();
			PlaySound(_dropSound);
		}

		public void MoveTowardThief()
		{
			if (_thief == null) return;

			var thiefPos = _thief.position;
			thiefPos.y = _baseY;

			var offset = transform.position - thiefPos;
			offset.y = 0f;
			var distance = offset.magnitude;

			// If already within min distance, don't move closer
			if (distance <= _minFollowDistance)
			{
				_velocity = Vector3.zero;
				return;
			}

			// Aim for the edge of the min distance sphere
			var target = thiefPos + offset.normalized * _minFollowDistance;

			var position = Vector3.SmoothDamp(
				transform.position, target, ref _velocity, _followSmoothTime, _followSpeed);
			transform.position = position;
		}

		public void MoveTowardReturn()
		{
			transform.position = Vector3.MoveTowards(
				transform.position, _returnDestination, _returnSpeed * Time.deltaTime);
		}

		public bool IsNearPosition(Vector3 position, float threshold)
		{
			return (transform.position - position).sqrMagnitude <= threshold * threshold;
		}

		public void ApplyBobAndSpin()
		{
			var pos = transform.position;
			pos.y = _baseY + Mathf.Sin(Time.time * _bobFrequency) * _bobAmplitude;
			transform.position = pos;

			transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
		}

		private void OnTriggerEnter(Collider other)
		{
			if (_isCollected) return;

			var thief = other.GetComponentInParent<ThiefController>();
			if (thief != null)
			{
				thief.CollectGem(this);
			}
		}

		private void UpdatePunch()
		{
			if (_punchTimer <= 0f) return;

			_punchTimer -= Time.deltaTime;
			var t = Mathf.Clamp01(_punchTimer / _punchDuration);
			var punch = Mathf.Sin(t * Mathf.PI) * t * _punchScale;
			transform.localScale = _baseScale * (1f + punch);
		}

		private void PlaySound(AudioClip clip)
		{
			if (_audioSource != null && clip != null)
			{
				_audioSource.PlayOneShot(clip);
			}
		}

		private Vector3 PickRandomReturnPosition()
		{
			var radius = Random.Range(_returnMinRadius, _returnMaxRadius);
			var angle = Random.Range(0f, 360f);
			var offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
			return new Vector3(offset.x, _baseY, offset.z);
		}
	}
}
