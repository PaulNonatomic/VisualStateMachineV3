using UnityEngine;

namespace Nonatomic.VSM.Samples.PatrollingGuard
{
	/// <summary>
	/// Rotates the transform to always face the main camera.
	/// </summary>
	public class Billboard : MonoBehaviour
	{
		private Transform _camera;

		private void Start()
		{
			var cam = Camera.main;
			if (cam != null) _camera = cam.transform;
		}

		private void LateUpdate()
		{
			if (_camera == null) return;

			transform.rotation = Quaternion.LookRotation(transform.position - _camera.position);
		}
	}
}
