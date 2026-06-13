using System;
using System.Collections;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using Nonatomic.VSM.States;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.PlayMode.States
{
	[TestFixture]
	public class DelayStateTimingTests
	{
		private GameObject _gameObject;
		private StateMachineRunner _runner;
		private StateMachineGraph _graph;

		[SetUp]
		public void SetUp()
		{
			_gameObject = new GameObject("TestRunner");
			_runner = _gameObject.AddComponent<StateMachineRunner>();
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_gameObject != null)
			{
				UnityEngine.Object.Destroy(_gameObject);
			}

			if (_graph != null)
			{
				UnityEngine.Object.DestroyImmediate(_graph);
			}
		}


		[UnityTest]
		public IEnumerator DelayState_WaitsForDuration_BeforeTransitioning()
		{
			SetupDelayGraph(0.2f);
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null; // First frame

			// Should still be in delay state
			Assert.IsInstanceOf<DelayState>(_runner.Instance.CurrentState);

			// Wait for a bit less than the duration
			yield return new WaitForSeconds(0.1f);
			Assert.IsInstanceOf<DelayState>(_runner.Instance.CurrentState, "Should still be delaying");

			// Wait for delay to complete
			yield return new WaitForSeconds(0.2f);

			// Should have transitioned to final state
			Assert.IsInstanceOf<FinalState>(_runner.Instance.CurrentState);
		}

		[UnityTest]
		public IEnumerator DelayState_UpdatesProgress_OverTime()
		{
			SetupDelayGraph(0.5f);
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null; // First frame (skipped in delay state)
			yield return null; // Second frame

			var delayState = _runner.Instance.CurrentState as DelayState;
			Assert.IsNotNull(delayState);

			var initialProgress = delayState.Progress;
			Assert.GreaterOrEqual(initialProgress, 0f);

			yield return new WaitForSeconds(0.2f);

			var midProgress = delayState.Progress;
			Assert.Greater(midProgress, initialProgress, "Progress should increase over time");
			Assert.Less(midProgress, 1f, "Progress should not be complete yet");
		}

		[UnityTest]
		public IEnumerator DelayState_UpdatesElapsedTime_OverTime()
		{
			SetupDelayGraph(0.5f);
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null; // First frame
			yield return null; // Second frame

			var delayState = _runner.Instance.CurrentState as DelayState;
			Assert.IsNotNull(delayState);

			yield return new WaitForSeconds(0.2f);

			Assert.Greater(delayState.ElapsedTime, 0f, "ElapsedTime should be greater than 0");
			Assert.Less(delayState.ElapsedTime, 0.5f, "ElapsedTime should be less than duration");
		}

		[UnityTest]
		public IEnumerator DelayState_WithZeroDuration_CompletesImmediately()
		{
			SetupDelayGraph(0f);
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			// Should immediately transition to final state
			Assert.IsInstanceOf<FinalState>(_runner.Instance.CurrentState);
		}

		[UnityTest]
		public IEnumerator DelayState_ProgressReachesOne_WhenComplete()
		{
			SetupDelayGraph(0.1f);
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return new WaitForSeconds(0.2f);

			// Delay should be complete
			Assert.IsInstanceOf<FinalState>(_runner.Instance.CurrentState);
		}



		[UnityTest]
		public IEnumerator RandomDelayState_WaitsForSelectedDuration()
		{
			SetupRandomDelayGraph(0.2f, 0.3f);
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;

			var randomDelayState = _runner.Instance.CurrentState as RandomDelayState;
			Assert.IsNotNull(randomDelayState);

			// Should have selected a duration
			Assert.GreaterOrEqual(randomDelayState.SelectedDuration, 0.2f);
			Assert.LessOrEqual(randomDelayState.SelectedDuration, 0.3f);

			// Wait for max duration + buffer
			yield return new WaitForSeconds(0.5f);

			// Should have transitioned
			Assert.IsInstanceOf<FinalState>(_runner.Instance.CurrentState);
		}

		[UnityTest]
		public IEnumerator RandomDelayState_UpdatesProgress_OverTime()
		{
			SetupRandomDelayGraph(0.5f, 0.6f);
			_runner.Initialize(_graph);
			_runner.StartStateMachine();

			yield return null;
			yield return null;

			var randomDelayState = _runner.Instance.CurrentState as RandomDelayState;
			Assert.IsNotNull(randomDelayState);

			var initialProgress = randomDelayState.Progress;

			yield return new WaitForSeconds(0.2f);

			var midProgress = randomDelayState.Progress;
			Assert.Greater(midProgress, initialProgress, "Progress should increase over time");
		}



		private void SetupDelayGraph(float duration)
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
				StateTypeAssemblyQualifiedName = typeof(FinalState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(delayNode);
			_graph.AddNode(finalNode);
			_graph.EntryNodeId = "entry";

			// Set the duration via property override
			delayNode.SetPropertyOverride("Duration", duration, typeof(float));

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "delay",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "delay",
				SourcePortName = "OnComplete",
				TargetNodeId = "final",
				ConnectionType = ConnectionType.Transition
			});
		}

		private void SetupRandomDelayGraph(float minDuration, float maxDuration)
		{
			var entry = new EntryNode { Id = "entry" };
			var randomDelayNode = new StateNode
			{
				Id = "randomDelay",
				StateTypeAssemblyQualifiedName = typeof(RandomDelayState).AssemblyQualifiedName
			};
			var finalNode = new StateNode
			{
				Id = "final",
				StateTypeAssemblyQualifiedName = typeof(FinalState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(randomDelayNode);
			_graph.AddNode(finalNode);
			_graph.EntryNodeId = "entry";

			// Set durations via property overrides
			randomDelayNode.SetPropertyOverride("MinDuration", minDuration, typeof(float));
			randomDelayNode.SetPropertyOverride("MaxDuration", maxDuration, typeof(float));

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "randomDelay",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "randomDelay",
				SourcePortName = "OnComplete",
				TargetNodeId = "final",
				ConnectionType = ConnectionType.Transition
			});
		}

	}

	/// <summary>
	/// A simple final state for testing transitions.
	/// </summary>
	public class FinalState : State
	{
		public static bool Entered;

		public override void OnEnter()
		{
			Entered = true;
		}

		public static void Reset()
		{
			Entered = false;
		}
	}
}
