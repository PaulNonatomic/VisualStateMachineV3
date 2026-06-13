using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Samples.Blackboard
{
	/// <summary>
	/// Demonstrates VSM3's Blackboard system for data flow between states.
	///
	/// Features shown:
	/// - Blackboard variables bound to [DataIn]/[DataOut] ports via graph connections
	/// - Exposed variables with per-instance overrides in the Inspector
	/// - Blackboard.Subscribe for reactive updates
	/// - Direct Blackboard.Get/Set from code
	///
	/// Setup:
	/// 1. Create a StateMachineGraph with an AccumulateScoreState and DisplayResultState
	/// 2. Add blackboard variables: "Points Per Second" (float), "Score Multiplier" (float),
	///    "Target Score" (int), "Score" (int), "Player Name" (string)
	/// 3. Expose "Points Per Second", "Score Multiplier", "Target Score", and "Player Name"
	/// 4. Create blackboard Get nodes and connect them to the state DataIn ports
	/// 5. Assign the graph to this controller and add multiple instances with different overrides
	/// </summary>
	public class BlackboardController : MonoBehaviour
	{
		[SerializeField] private StateMachineGraph _graph;

		private StateMachineRunner _runner;

		private void Start()
		{
			_runner = gameObject.AddComponent<StateMachineRunner>();
			_runner.Initialize(_graph);

			// Subscribe to blackboard changes for reactive UI updates
			_runner.Blackboard.Subscribe<int>("Score", (oldValue, newValue) =>
			{
				if (newValue % 25 == 0 && newValue > 0)
				{
					Debug.Log($"[Blackboard] Score milestone: {newValue}");
				}
			});

			_runner.OnStateChanged += (prev, next) =>
			{
				Debug.Log($"[Blackboard] State: {prev?.GetType().Name ?? "None"} -> {next?.GetType().Name ?? "None"}");
			};

			_runner.StartStateMachine();
		}

		private void Update()
		{
			// Demonstrate setting blackboard values from external code.
			// Press M to toggle a 2x score multiplier.
			if (Input.GetKeyDown(KeyCode.M))
			{
				var current = _runner.GetBlackboardValue("Score Multiplier", 1f);
				var next = current > 1f ? 1f : 2f;
				_runner.SetBlackboardValue("Score Multiplier", next);
				Debug.Log($"[Blackboard] Multiplier set to {next}x");
			}
		}
	}
}
