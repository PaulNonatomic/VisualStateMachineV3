using UnityEngine;

namespace Nonatomic.VSM.Samples.CoinCollector
{
	/// <summary>
	/// A collectible coin that notifies the GameController when collected.
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class CoinPickup : MonoBehaviour
	{
		[SerializeField] private float _rotationSpeed = 90f;
		[SerializeField] private float _bobSpeed = 2f;
		[SerializeField] private float _bobHeight = 0.25f;

		private Vector3 _startPosition;
		private GameController _gameController;

		private void Start()
		{
			_startPosition = transform.position;
			_gameController = FindFirstObjectByType<GameController>();

			// Ensure we have a trigger collider
			var collider = GetComponent<Collider>();
			if (collider != null)
				collider.isTrigger = true;
		}

		private void Update()
		{
			// Rotate the coin
			transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);

			// Bob up and down
			var newY = _startPosition.y + Mathf.Sin(Time.time * _bobSpeed) * _bobHeight;
			transform.position = new Vector3(transform.position.x, newY, transform.position.z);
		}

		private void OnTriggerEnter(Collider other)
		{
			// Check if the player collected us
			if (other.CompareTag("Player") || other.GetComponent<Camera>() != null)
			{
				Collect();
			}
		}

		private void Collect()
		{
			if (_gameController != null)
			{
				_gameController.CollectCoin();
			}

			// Disable the coin (could also destroy or play VFX)
			gameObject.SetActive(false);
		}
	}
}
