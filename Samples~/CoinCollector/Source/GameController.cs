using System;
using Nonatomic.VSM.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Nonatomic.VSM.Samples.CoinCollector
{
	/// <summary>
	/// Main game controller for the Coin Collector sample.
	/// Manages game state, scoring, and UI updates.
	/// </summary>
	public class GameController : MonoBehaviour, IStateMachineContext
	{
		[Header("Game Settings")]
		[SerializeField] private int _coinsToWin = 5;
		[SerializeField] private float _gameTime = 30f;

		[Header("UI References")]
		[SerializeField] private Text _scoreText;
		[SerializeField] private Text _timerText;
		[SerializeField] private Text _messageText;
		[SerializeField] private GameObject _menuPanel;
		[SerializeField] private GameObject _gamePanel;
		[SerializeField] private GameObject _resultPanel;

		public GameObject GameObject => gameObject;
		public Transform Transform => transform;

		/// <summary>
		/// Current score (coins collected).
		/// </summary>
		public int Score { get; set; }

		/// <summary>
		/// Number of coins needed to win.
		/// </summary>
		public int CoinsToWin => _coinsToWin;

		/// <summary>
		/// Total game time in seconds.
		/// </summary>
		public float GameTime => _gameTime;

		/// <summary>
		/// Time remaining in the current game.
		/// </summary>
		public float TimeRemaining { get; set; }

		/// <summary>
		/// Event fired when a coin is collected.
		/// </summary>
		public event Action<int> OnCoinCollected;

		private void Awake()
		{
			// Find UI elements if not assigned
			if (_scoreText == null)
				_scoreText = GameObject.Find("ScoreText")?.GetComponent<Text>();
			if (_timerText == null)
				_timerText = GameObject.Find("TimerText")?.GetComponent<Text>();
			if (_messageText == null)
				_messageText = GameObject.Find("MessageText")?.GetComponent<Text>();
		}

		/// <summary>
		/// Called by CoinPickup when a coin is collected.
		/// </summary>
		public void CollectCoin()
		{
			Score++;
			OnCoinCollected?.Invoke(Score);
			UpdateUI();
		}

		/// <summary>
		/// Resets the game state for a new game.
		/// </summary>
		public void ResetGame()
		{
			Score = 0;
			TimeRemaining = _gameTime;
			UpdateUI();

			// Re-enable all coins
			var coins = FindObjectsByType<CoinPickup>(FindObjectsSortMode.None);
			foreach (var coin in coins)
			{
				coin.gameObject.SetActive(true);
			}
		}

		/// <summary>
		/// Updates the UI to reflect current game state.
		/// </summary>
		public void UpdateUI()
		{
			if (_scoreText != null)
				_scoreText.text = $"Coins: {Score}/{_coinsToWin}";

			if (_timerText != null)
				_timerText.text = $"Time: {Mathf.CeilToInt(TimeRemaining)}";
		}

		/// <summary>
		/// Shows a message in the UI.
		/// </summary>
		public void ShowMessage(string message)
		{
			if (_messageText != null)
				_messageText.text = message;
		}

		/// <summary>
		/// Shows/hides UI panels based on game state.
		/// </summary>
		public void SetPanelState(bool showMenu, bool showGame, bool showResult)
		{
			if (_menuPanel != null)
				_menuPanel.SetActive(showMenu);
			if (_gamePanel != null)
				_gamePanel.SetActive(showGame);
			if (_resultPanel != null)
				_resultPanel.SetActive(showResult);
		}

		/// <summary>
		/// Checks if the win condition is met.
		/// </summary>
		public bool HasWon => Score >= _coinsToWin;

		/// <summary>
		/// Checks if time has run out.
		/// </summary>
		public bool IsTimeUp => TimeRemaining <= 0f;
	}
}
