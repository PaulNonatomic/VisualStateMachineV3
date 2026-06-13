using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Samples.AsyncStates
{
	/// <summary>
	/// Demonstrates async/await state lifecycle methods in VSM3.
	///
	/// Features shown:
	/// - OnEnterAsync(CancellationToken) for async initialization and loading
	/// - OnExitAsync(CancellationToken) for async cleanup before leaving a state
	/// - CancellationToken handling when transitions interrupt async work
	/// - Progress reporting during async operations
	///
	/// Setup:
	/// 1. Create a StateMachineGraph with three states:
	///    AsyncInitializeState -> AsyncLoadState -> AsyncCleanupState
	/// 2. Connect AsyncCleanupState "On Done" back to AsyncInitializeState for looping
	/// 3. Add blackboard variables: "Step Count" (int, default 3),
	///    "Step Delay" (float, default 0.5), "Load Duration" (float, default 2),
	///    "Cleanup Duration" (float, default 1)
	/// 4. Assign the graph to this controller
	/// </summary>
	public class AsyncStatesController : MonoBehaviour
	{
		[SerializeField] private StateMachineGraph _graph;

		private StateMachineRunner _runner;

		private void Start()
		{
			_runner = gameObject.AddComponent<StateMachineRunner>();
			_runner.Initialize(_graph);

			_runner.OnStateChanged += (prev, next) =>
			{
				Debug.Log($"[Async] State: {prev?.GetType().Name ?? "None"} -> {next?.GetType().Name ?? "None"}");
			};

			_runner.StartStateMachine();
		}
	}
}
