using UnityEngine;

namespace Nonatomic.VSM.Samples.WorldSpaceGraph
{
	/// <summary>
	/// Simple free-fly camera. Hold right mouse button to look, WASD/QE to move,
	/// Shift to move faster, scroll wheel adjusts move speed. Input is routed through
	/// <see cref="SampleInput"/> so it no-ops gracefully when the legacy Input Manager
	/// is disabled instead of throwing.
	/// </summary>
	public class FlyCameraController : MonoBehaviour
	{
		[SerializeField]
		private float _moveSpeed = 4f;

		[SerializeField]
		private float _fastMultiplier = 3f;

		[SerializeField]
		private float _lookSensitivity = 2.5f;

		[SerializeField]
		private float _scrollSpeedAdjust = 0.5f;

		private float _yaw;
		private float _pitch;
		private bool _initialized;

		private void OnEnable()
		{
			SyncFromTransform();
		}

		private void Update()
		{
			if (!_initialized)
			{
				SyncFromTransform();
			}

			HandleLook();
			HandleMove();
			HandleSpeedAdjust();
		}

		private void SyncFromTransform()
		{
			var euler = transform.eulerAngles;
			_pitch = NormalizePitch(euler.x);
			_yaw = euler.y;
			_initialized = true;
		}

		private void HandleLook()
		{
			if (!SampleInput.GetMouseButton(1))
			{
				return;
			}

			var dx = SampleInput.GetAxis("Mouse X") * _lookSensitivity;
			var dy = SampleInput.GetAxis("Mouse Y") * _lookSensitivity;

			_yaw += dx;
			_pitch = Mathf.Clamp(_pitch - dy, -85f, 85f);

			transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
		}

		private void HandleMove()
		{
			var input = new Vector3(
				GetAxis(KeyCode.D, KeyCode.A),
				GetAxis(KeyCode.E, KeyCode.Q),
				GetAxis(KeyCode.W, KeyCode.S));

			if (input.sqrMagnitude < 0.0001f)
			{
				return;
			}

			var speed = _moveSpeed;
			if (SampleInput.GetKey(KeyCode.LeftShift) || SampleInput.GetKey(KeyCode.RightShift))
			{
				speed *= _fastMultiplier;
			}

			var delta = transform.right * input.x + Vector3.up * input.y + transform.forward * input.z;
			transform.position += delta * speed * Time.deltaTime;
		}

		private void HandleSpeedAdjust()
		{
			var scroll = SampleInput.GetAxis("Mouse ScrollWheel");
			if (Mathf.Abs(scroll) < 0.0001f)
			{
				return;
			}

			_moveSpeed = Mathf.Max(0.5f, _moveSpeed + scroll * _scrollSpeedAdjust * 4f);
		}

		private static float GetAxis(KeyCode positive, KeyCode negative)
		{
			var v = 0f;
			if (SampleInput.GetKey(positive)) v += 1f;
			if (SampleInput.GetKey(negative)) v -= 1f;
			return v;
		}

		private static float NormalizePitch(float pitch)
		{
			pitch %= 360f;
			if (pitch > 180f) pitch -= 360f;
			return Mathf.Clamp(pitch, -85f, 85f);
		}
	}
}
