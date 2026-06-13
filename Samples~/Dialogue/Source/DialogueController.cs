using Nonatomic.VSM.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Nonatomic.VSM.Samples.Dialogue
{
	/// <summary>
	/// Controls the dialogue UI and provides shared context for dialogue states.
	/// Manages a dialogue panel with speaker name, dialogue text, choice buttons,
	/// and an interaction prompt.
	/// </summary>
	public class DialogueController : MonoBehaviour, IStateMachineContext
	{
		[Header("Interaction")]
		[SerializeField] private KeyCode _interactKey = KeyCode.E;
		[SerializeField] private KeyCode _advanceKey = KeyCode.Space;
		[SerializeField] private float _interactionRange = 4f;

		[Header("Typewriter")]
		[SerializeField] private float _charactersPerSecond = 30f;

		[Header("Dialogue Content")]
		[SerializeField] private DialogueLine _greeting = new ("Tavern Keeper", "Welcome, traveller! What brings you to my humble tavern?");
		[SerializeField] private string _choicePrompt = "What would you like to ask?";
		[SerializeField] private string _choiceA = "Tell me about the town";
		[SerializeField] private string _choiceB = "Got any work for me?";
		[SerializeField] private string _choiceC = "Goodbye";
		[SerializeField] private DialogueLine _townResponse = new ("Tavern Keeper", "This is Millhaven, a quiet trading town. We get merchants from all over. The market square is just north of here.");
		[SerializeField] private DialogueLine _workResponse = new ("Tavern Keeper", "Actually, yes. Rats have been getting into my cellar again. Clear them out and I will make it worth your while.");
		[SerializeField] private DialogueLine _farewell = new ("Tavern Keeper", "Safe travels, friend. You are always welcome here.");

		[Header("UI References")]
		[SerializeField] private GameObject _dialoguePanel;
		[SerializeField] private Text _speakerText;
		[SerializeField] private Text _dialogueText;
		[SerializeField] private Text _promptText;
		[SerializeField] private GameObject _choicePanel;
		[SerializeField] private Text _choicePromptText;
		[SerializeField] private Text _choiceAText;
		[SerializeField] private Text _choiceBText;
		[SerializeField] private Text _choiceCText;
		[SerializeField] private Text _continueHint;

		public GameObject GameObject => gameObject;
		public Transform Transform => transform;

		public DialogueLine Greeting => _greeting;
		public string ChoicePrompt => _choicePrompt;
		public string ChoiceA => _choiceA;
		public string ChoiceB => _choiceB;
		public string ChoiceC => _choiceC;
		public DialogueLine TownResponse => _townResponse;
		public DialogueLine WorkResponse => _workResponse;
		public DialogueLine Farewell => _farewell;

		public KeyCode AdvanceKey => _advanceKey;
		public string FullText { get; private set; }
		public float TypewriterProgress { get; private set; }
		public bool IsTypewriterComplete => string.IsNullOrEmpty(FullText) || TypewriterProgress >= FullText.Length;

		/// <summary>
		/// Checks if the player is in range and pressing the interact key.
		/// </summary>
		public bool IsInteractPressed()
		{
			if (!Input.GetKeyDown(_interactKey)) return false;

			var camera = Camera.main;
			if (camera == null) return false;

			var distance = Vector3.Distance(camera.transform.position, transform.position);
			return distance <= _interactionRange;
		}

		/// <summary>
		/// Checks if the advance key was pressed this frame.
		/// </summary>
		public bool IsAdvancePressed()
		{
			return Input.GetKeyDown(_advanceKey);
		}

		/// <summary>
		/// Shows the dialogue panel with speaker name and starts the typewriter on the text.
		/// </summary>
		public void ShowDialogue(string speaker, string text)
		{
			if (_dialoguePanel != null) _dialoguePanel.SetActive(true);
			if (_choicePanel != null) _choicePanel.SetActive(false);
			if (_continueHint != null) _continueHint.gameObject.SetActive(false);
			if (_speakerText != null) _speakerText.text = speaker ?? "";

			FullText = text;
			TypewriterProgress = 0f;
			UpdateTypewriterDisplay();
		}

		/// <summary>
		/// Advances the typewriter by delta time. Returns true when complete.
		/// </summary>
		public bool AdvanceTypewriter(float deltaTime)
		{
			if (IsTypewriterComplete) return true;

			TypewriterProgress += _charactersPerSecond * deltaTime;
			UpdateTypewriterDisplay();

			if (!IsTypewriterComplete) return false;

			if (_continueHint != null) _continueHint.gameObject.SetActive(true);
			return true;
		}

		/// <summary>
		/// Completes the typewriter instantly, showing full text.
		/// </summary>
		public void CompleteTypewriter()
		{
			if (string.IsNullOrEmpty(FullText)) return;

			TypewriterProgress = FullText.Length;
			UpdateTypewriterDisplay();
			if (_continueHint != null) _continueHint.gameObject.SetActive(true);
		}

		/// <summary>
		/// Shows the choice panel with three options.
		/// </summary>
		public void ShowChoices(string prompt, string choiceA, string choiceB, string choiceC)
		{
			if (_dialoguePanel != null) _dialoguePanel.SetActive(true);
			if (_choicePanel != null) _choicePanel.SetActive(true);
			if (_continueHint != null) _continueHint.gameObject.SetActive(false);

			if (_speakerText != null) _speakerText.text = "";
			if (_dialogueText != null) _dialogueText.text = "";
			if (_choicePromptText != null) _choicePromptText.text = prompt;
			if (_choiceAText != null) _choiceAText.text = $"[1] {choiceA}";
			if (_choiceBText != null) _choiceBText.text = $"[2] {choiceB}";
			if (_choiceCText != null) _choiceCText.text = $"[3] {choiceC}";
		}

		/// <summary>
		/// Hides all dialogue UI.
		/// </summary>
		public void HideDialogue()
		{
			if (_dialoguePanel != null) _dialoguePanel.SetActive(false);
			if (_choicePanel != null) _choicePanel.SetActive(false);
		}

		/// <summary>
		/// Shows the interaction prompt.
		/// </summary>
		public void ShowPrompt(string text)
		{
			if (_promptText != null)
			{
				_promptText.gameObject.SetActive(true);
				_promptText.text = text;
			}
		}

		/// <summary>
		/// Hides the interaction prompt.
		/// </summary>
		public void HidePrompt()
		{
			if (_promptText != null) _promptText.gameObject.SetActive(false);
		}

		private void UpdateTypewriterDisplay()
		{
			if (_dialogueText == null || string.IsNullOrEmpty(FullText)) return;

			var visibleCount = Mathf.Clamp(Mathf.FloorToInt(TypewriterProgress), 0, FullText.Length);
			_dialogueText.text = FullText.Substring(0, visibleCount);
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(transform.position, _interactionRange);
		}
	}
}
