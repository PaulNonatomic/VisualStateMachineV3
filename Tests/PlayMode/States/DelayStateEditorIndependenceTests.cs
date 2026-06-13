using System.Collections;
using NUnit.Framework;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.PlayMode.States
{
	/// <summary>
	/// Tests that verify state machines run independently of editor visualization.
	/// These tests confirm that DelayState works when:
	/// - The runner is not selected in the hierarchy
	/// - The graph is not displayed in the editor
	/// - No editor callbacks are registered
	/// </summary>
	[TestFixture]
	public class DelayStateEditorIndependenceTests
	{
		[SetUp]
		public void SetUp()
		{
			// Ensure no editor callbacks are registered
			StateMachineControllerCallbacks.Reset();
		}

		[TearDown]
		public void TearDown()
		{
			// Clean up any callbacks
			StateMachineControllerCallbacks.Reset();
		}

		[UnityTest]
		public IEnumerator DelayState_RunsWithoutEditorCallbacks()
		{
			// Explicitly ensure no editor callbacks are registered
			StateMachineControllerCallbacks.Reset();

			var gameObject = new GameObject("TestRunner");
			var runner = gameObject.AddComponent<StateMachineRunner>();
			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();

			try
			{
				SetupDelayGraph(graph, 0.2f);
				runner.Initialize(graph);
				runner.StartStateMachine();

				yield return null; // First frame

				// Verify we're in the delay state
				Assert.IsInstanceOf<DelayState>(runner.Instance.CurrentState,
					"Should be in DelayState without editor callbacks");

				// Wait for a bit - should still be delaying
				yield return new WaitForSeconds(0.1f);
				Assert.IsInstanceOf<DelayState>(runner.Instance.CurrentState,
					"Should still be in DelayState");

				// Wait for delay to complete
				yield return new WaitForSeconds(0.2f);

				// Verify transition occurred without editor involvement
				Assert.IsInstanceOf<EditorIndependenceFinalState>(runner.Instance.CurrentState,
					"Should transition to FinalState without editor callbacks");
			}
			finally
			{
				Object.Destroy(gameObject);
				Object.DestroyImmediate(graph);
			}
		}

		[UnityTest]
		public IEnumerator MultipleStateMachines_RunSimultaneously_WithoutEditorSelection()
		{
			// This test proves state machines don't depend on "being selected"
			// by running multiple simultaneously
			var runners = new StateMachineRunner[3];
			var gameObjects = new GameObject[3];
			var graphs = new StateMachineGraph[3];
			var durations = new float[] { 0.15f, 0.2f, 0.25f };

			try
			{
				// Create multiple runners with different delays
				for (int i = 0; i < 3; i++)
				{
					gameObjects[i] = new GameObject($"TestRunner_{i}");
					runners[i] = gameObjects[i].AddComponent<StateMachineRunner>();
					graphs[i] = ScriptableObject.CreateInstance<StateMachineGraph>();
					SetupDelayGraph(graphs[i], durations[i]);
					runners[i].Initialize(graphs[i]);
				}

				// Start all state machines
				foreach (var runner in runners)
				{
					runner.StartStateMachine();
				}

				yield return null;

				// All should be running in DelayState
				for (int i = 0; i < 3; i++)
				{
					Assert.IsInstanceOf<DelayState>(runners[i].Instance.CurrentState,
						$"Runner {i} should be in DelayState");
					Assert.IsTrue(runners[i].Instance.IsRunning,
						$"Runner {i} should be running");
				}

				// Wait for all to complete (longest delay + buffer)
				yield return new WaitForSeconds(0.4f);

				// All should have transitioned independently
				for (int i = 0; i < 3; i++)
				{
					Assert.IsInstanceOf<EditorIndependenceFinalState>(runners[i].Instance.CurrentState,
						$"Runner {i} should have transitioned to FinalState");
				}
			}
			finally
			{
				for (int i = 0; i < 3; i++)
				{
					if (gameObjects[i] != null)
						Object.Destroy(gameObjects[i]);
					if (graphs[i] != null)
						Object.DestroyImmediate(graphs[i]);
				}
			}
		}

		[UnityTest]
		public IEnumerator DelayState_ContinuesRunning_AfterCallbacksCleared()
		{
			// This test verifies that state machines continue running
			// when callbacks are cleared mid-execution (simulating graph window being closed)
			var gameObject = new GameObject("TestRunner");
			var runner = gameObject.AddComponent<StateMachineRunner>();
			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();

			try
			{
				SetupDelayGraph(graph, 0.25f);
				runner.Initialize(graph);
				runner.StartStateMachine();

				yield return null;

				// Verify state machine is running
				Assert.IsInstanceOf<DelayState>(runner.Instance.CurrentState,
					"Should be in DelayState");
				Assert.IsTrue(runner.Instance.IsRunning,
					"State machine should be running");

				// Clear callbacks mid-execution (simulating graph window being closed)
				StateMachineControllerCallbacks.Reset();

				// Verify still running after callbacks cleared
				yield return new WaitForSeconds(0.1f);
				Assert.IsInstanceOf<DelayState>(runner.Instance.CurrentState,
					"Should still be in DelayState after callbacks cleared");
				Assert.IsTrue(runner.Instance.IsRunning,
					"State machine should still be running after callbacks cleared");

				// Wait for delay to complete
				yield return new WaitForSeconds(0.25f);

				// Should have completed despite callbacks being null
				Assert.IsInstanceOf<EditorIndependenceFinalState>(runner.Instance.CurrentState,
					"Should transition to FinalState even with null callbacks");
			}
			finally
			{
				Object.Destroy(gameObject);
				Object.DestroyImmediate(graph);
			}
		}

		[UnityTest]
		public IEnumerator DelayState_ProgressUpdates_WithoutGraphVisualization()
		{
			var gameObject = new GameObject("TestRunner");
			var runner = gameObject.AddComponent<StateMachineRunner>();
			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();

			try
			{
				SetupDelayGraph(graph, 0.5f);
				runner.Initialize(graph);
				runner.StartStateMachine();

				yield return null; // First frame
				yield return null; // Second frame (first-frame skip in DelayState)

				var delayState = runner.Instance.CurrentState as DelayState;
				Assert.IsNotNull(delayState, "Should be in DelayState");

				var progressValues = new float[3];
				progressValues[0] = delayState.Progress;

				yield return new WaitForSeconds(0.15f);
				progressValues[1] = delayState.Progress;

				yield return new WaitForSeconds(0.15f);
				progressValues[2] = delayState.Progress;

				// Verify progress is increasing without any editor visualization
				Assert.Greater(progressValues[1], progressValues[0],
					"Progress should increase over time without editor");
				Assert.Greater(progressValues[2], progressValues[1],
					"Progress should continue increasing without editor");
			}
			finally
			{
				Object.Destroy(gameObject);
				Object.DestroyImmediate(graph);
			}
		}

		[UnityTest]
		public IEnumerator DelayState_ElapsedTimeAccurate_WithoutEditorUpdates()
		{
			var gameObject = new GameObject("TestRunner");
			var runner = gameObject.AddComponent<StateMachineRunner>();
			var graph = ScriptableObject.CreateInstance<StateMachineGraph>();

			try
			{
				SetupDelayGraph(graph, 1f);
				runner.Initialize(graph);
				runner.StartStateMachine();

				yield return null; // First frame
				yield return null; // Second frame

				var delayState = runner.Instance.CurrentState as DelayState;
				Assert.IsNotNull(delayState, "Should be in DelayState");

				var startTime = Time.unscaledTime;

				yield return new WaitForSecondsRealtime(0.25f);

				var actualElapsed = Time.unscaledTime - startTime;

				// Elapsed time should roughly match real time (with some tolerance for frame timing)
				Assert.Greater(delayState.ElapsedTime, 0.15f,
					"ElapsedTime should be tracking without editor");
				Assert.Less(Mathf.Abs(delayState.ElapsedTime - actualElapsed), 0.15f,
					"ElapsedTime should be reasonably accurate without editor");
			}
			finally
			{
				Object.Destroy(gameObject);
				Object.DestroyImmediate(graph);
			}
		}

		private void SetupDelayGraph(StateMachineGraph graph, float duration)
		{
			var entry = new EntryNode { Id = "entry" };
			var delayNode = new StateNode
			{
				Id = "delay",
				StateTypeAssemblyQualifiedName = typeof(DelayState).AssemblyQualifiedName
			};
			var finalNode = new StateNode
			{
				Id = "final",
				StateTypeAssemblyQualifiedName = typeof(EditorIndependenceFinalState).AssemblyQualifiedName
			};

			graph.AddNode(entry);
			graph.AddNode(delayNode);
			graph.AddNode(finalNode);
			graph.EntryNodeId = "entry";

			delayNode.SetPropertyOverride("Duration", duration, typeof(float));

			graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "delay",
				ConnectionType = ConnectionType.Transition
			});

			graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "delay",
				SourcePortName = "OnComplete",
				TargetNodeId = "final",
				ConnectionType = ConnectionType.Transition
			});
		}
	}

	/// <summary>
	/// A simple final state for testing transitions in editor independence tests.
	/// </summary>
	public class EditorIndependenceFinalState : State
	{
		public override void OnEnter()
		{
			// No-op - just marks transition complete
		}
	}
}
