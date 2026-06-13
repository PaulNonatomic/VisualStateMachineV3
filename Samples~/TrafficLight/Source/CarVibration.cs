using UnityEngine;

namespace Nonatomic.VSM.Samples.TrafficLight
{
	public class CarVibration : MonoBehaviour
	{
		[Header("Position Vibration")]
		[SerializeField] private float _positionIntensity = 0.002f;
		[SerializeField] private float _positionFrequency = 30f;

		[Header("Rotation Vibration")]
		[SerializeField] private float _rotationIntensity = 0.15f;
		[SerializeField] private float _rotationFrequency = 25f;

		[Header("Engine Pulse")]
		[SerializeField] private float _pulseFrequency = 12f;
		[SerializeField] private float _pulseDepth = 0.6f;

		[Header("Randomness")]
		[SerializeField] private float _jitterSpeed = 8f;
		[SerializeField] [Range(0f, 1f)] private float _jitterAmount = 0.3f;

		private Vector3 _originalLocalPosition;
		private Quaternion _originalLocalRotation;
		private float _timeOffset;

		private void Awake()
		{
			_originalLocalPosition = transform.localPosition;
			_originalLocalRotation = transform.localRotation;
			_timeOffset = Random.Range(0f, 100f);
		}

		private void Update()
		{
			var t = Time.time + _timeOffset;
			var pulse = 1f + Mathf.Sin(t * _pulseFrequency * Mathf.PI * 2f) * _pulseDepth;
			var jitter = 1f + (Mathf.PerlinNoise(t * _jitterSpeed, _timeOffset) - 0.5f) * 2f * _jitterAmount;
			var envelope = pulse * jitter;

			ApplyPositionVibration(t, envelope);
			ApplyRotationVibration(t, envelope);
		}

		private void ApplyPositionVibration(float t, float envelope)
		{
			var x = (Mathf.PerlinNoise(t * _positionFrequency, 0f) - 0.5f) * 2f;
			var y = (Mathf.PerlinNoise(0f, t * _positionFrequency * 1.1f) - 0.5f) * 2f;
			var z = (Mathf.PerlinNoise(t * _positionFrequency * 0.9f, 50f) - 0.5f) * 2f;

			var offset = new Vector3(x, y, z) * (_positionIntensity * envelope);
			transform.localPosition = _originalLocalPosition + offset;
		}

		private void ApplyRotationVibration(float t, float envelope)
		{
			var pitch = (Mathf.PerlinNoise(t * _rotationFrequency, 100f) - 0.5f) * 2f;
			var yaw = (Mathf.PerlinNoise(200f, t * _rotationFrequency * 0.8f) - 0.5f) * 2f;
			var roll = (Mathf.PerlinNoise(t * _rotationFrequency * 1.2f, 300f) - 0.5f) * 2f;

			var rotationOffset = new Vector3(pitch, yaw, roll) * (_rotationIntensity * envelope);
			transform.localRotation = _originalLocalRotation * Quaternion.Euler(rotationOffset);
		}

		private void OnDisable()
		{
			transform.localPosition = _originalLocalPosition;
			transform.localRotation = _originalLocalRotation;
		}
	}
}