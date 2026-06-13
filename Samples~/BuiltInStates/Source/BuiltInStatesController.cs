using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Samples.BuiltInStates
{
	/// <summary>
	/// Demonstrates composing a state machine entirely from VSM3's built-in states,
	/// without writing any custom state classes.
	///
	/// Features shown:
	/// - LogState for console output at each step
	/// - DelayState and RandomDelayState for timing
	/// - ConditionState for boolean branching
	/// - CompareState for numeric comparisons
	/// - RandomBranchState for probability-based paths
	/// - RepeaterState for looping a fixed number of times
	///
	/// Setup:
	/// 1. Create a StateMachineGraph using only built-in states (no custom C# needed)
	/// 2. Suggested flow:
	///
	///    Entry -> LogState("Starting sequence")
	///          -> RepeaterState(RepeatCount=3)
	///              -> [Loop] -> RandomBranchState(ProbabilityA=0.5)
	///                  -> [Path A] -> LogState("Took path A")
	///                              -> DelayState(Duration=1)
	///                              -> PortalOut(Ch 0)
	///                  -> [Path B] -> LogState("Took path B")
	///                              -> RandomDelayState(Min=0.5, Max=2)
	///                              -> PortalOut(Ch 0)
	///              -> PortalIn(Ch 0) -> back to RepeaterState
	///              -> [Done] -> LogState("Sequence complete!")
	///                        -> ConditionState(Condition bound to "Should Restart" blackboard var)
	///                            -> [True]  -> PortalOut(Ch 1)
	///                            -> [False] -> LogState("Finished")
	///                                       -> Exit
	///              -> PortalIn(Ch 1) -> back to Entry connection
	///
	/// 3. Add blackboard variables:
	///    - "Should Restart" (bool, default false, exposed)
	///
	/// 4. This graph requires NO custom C# code beyond this controller.
	///    All logic is handled by built-in states and graph connections.
	/// </summary>
	public class BuiltInStatesController : MonoBehaviour
	{
		[SerializeField] private StateMachineGraph _graph;

		private StateMachineRunner _runner;

		private void Start()
		{
			_runner = gameObject.AddComponent<StateMachineRunner>();
			_runner.Initialize(_graph);

			_runner.OnStateChanged += (prev, next) =>
			{
				Debug.Log($"[BuiltIn] {prev?.GetType().Name ?? "None"} -> {next?.GetType().Name ?? "None"}");
			};

			_runner.StartStateMachine();
		}
	}
}
