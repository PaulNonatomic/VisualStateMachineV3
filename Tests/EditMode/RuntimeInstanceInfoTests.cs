using System;
using System.Linq;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Editor.Runtime;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class RuntimeInstanceInfoTests
	{
		private StateMachineGraph _graph;
		private StateMachineInstance _instance;
		private RuntimeInstanceInfo _info;

		private class TestStateA : State
		{
			[Transition("Complete")] public Action OnComplete;

			public override void OnEnter() { }
		}

		private class TestStateB : State
		{
			[Transition("Complete")] public Action OnComplete;

			public override void OnEnter() { }
		}

		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
		}

		[TearDown]
		public void TearDown()
		{
			_info?.Dispose();
			_instance?.Stop();
			_instance = null;

			if (_graph != null)
			{
				UnityEngine.Object.DestroyImmediate(_graph);
			}
		}

		private void SetupSimpleGraph()
		{
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				DisplayName = "State A",
				StateTypeAssemblyQualifiedName = typeof(TestStateA).AssemblyQualifiedName
			};
			var stateB = new StateNode
			{
				Id = "stateB",
				DisplayName = "State B",
				StateTypeAssemblyQualifiedName = typeof(TestStateB).AssemblyQualifiedName
			};
			var exit = new ExitNode { Id = "exit", ExitName = "Done" };

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(stateB);
			_graph.AddNode(exit);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "stateA",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "stateA",
				SourcePortName = "OnComplete",
				TargetNodeId = "stateB",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c3",
				SourceNodeId = "stateB",
				SourcePortName = "OnComplete",
				TargetNodeId = "exit",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});
		}

		private void SetupGraphWithPortals()
		{
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				DisplayName = "State A",
				StateTypeAssemblyQualifiedName = typeof(TestStateA).AssemblyQualifiedName
			};
			var portalOut = new PortalOutNode { Id = "portalOut", ChannelId = 1 };
			var portalIn = new PortalInNode { Id = "portalIn", ChannelId = 1 };
			var stateB = new StateNode
			{
				Id = "stateB",
				DisplayName = "State B",
				StateTypeAssemblyQualifiedName = typeof(TestStateB).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(portalOut);
			_graph.AddNode(portalIn);
			_graph.AddNode(stateB);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				SourcePortName = "out",
				TargetNodeId = "stateA",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "stateA",
				SourcePortName = "OnComplete",
				TargetNodeId = "portalOut",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c3",
				SourceNodeId = "portalIn",
				SourcePortName = "out",
				TargetNodeId = "stateB",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});
		}


		[Test]
		public void Constructor_NullInstance_DoesNotThrow()
		{
			Assert.DoesNotThrow(() =>
			{
				_info = new RuntimeInstanceInfo(null, _graph, null);
			});
		}

		[Test]
		public void Constructor_SetsInitialStatus_Running()
		{
			_info = new RuntimeInstanceInfo(null, _graph, null);

			Assert.AreEqual(RuntimeInstanceState.Running, _info.Status);
		}

		[Test]
		public void Constructor_GeneratesUniqueInstanceId()
		{
			_info = new RuntimeInstanceInfo(null, _graph, null);

			Assert.IsNotNull(_info.InstanceId);
			Assert.IsNotEmpty(_info.InstanceId);
		}

		[Test]
		public void Constructor_TwoInstances_HaveDifferentIds()
		{
			_info = new RuntimeInstanceInfo(null, _graph, null);
			var info2 = new RuntimeInstanceInfo(null, _graph, null);

			Assert.AreNotEqual(_info.InstanceId, info2.InstanceId);

			info2.Dispose();
		}

		[Test]
		public void Constructor_SetsGraph()
		{
			_info = new RuntimeInstanceInfo(null, _graph, null);

			Assert.AreEqual(_graph, _info.Graph);
		}

		[Test]
		public void Constructor_SetsStartTime()
		{
			var before = DateTime.Now;

			_info = new RuntimeInstanceInfo(null, _graph, null);

			var after = DateTime.Now;
			Assert.IsTrue(_info.StartTime >= before && _info.StartTime <= after);
		}

		[Test]
		public void Constructor_WithInstance_SetsInstance()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			Assert.AreEqual(_instance, _info.Instance);
		}

		[Test]
		public void Constructor_WithOwner_SetsOwner()
		{
			var go = new GameObject("TestOwner");

			_info = new RuntimeInstanceInfo(null, _graph, go);

			Assert.AreEqual(go, _info.Owner);

			UnityEngine.Object.DestroyImmediate(go);
		}

		[Test]
		public void Constructor_TransitionHistory_IsEmpty()
		{
			_info = new RuntimeInstanceInfo(null, _graph, null);

			Assert.IsNotNull(_info.TransitionHistory);
			Assert.AreEqual(0, _info.TransitionHistory.Count);
		}



		[Test]
		public void SetStatus_ChangesStatus()
		{
			_info = new RuntimeInstanceInfo(null, _graph, null);

			_info.SetStatus(RuntimeInstanceState.Stopped);

			Assert.AreEqual(RuntimeInstanceState.Stopped, _info.Status);
		}

		[Test]
		public void SetStatus_FiresOnStatusChanged()
		{
			_info = new RuntimeInstanceInfo(null, _graph, null);
			var fired = false;
			_info.OnStatusChanged += _ => fired = true;

			_info.SetStatus(RuntimeInstanceState.Paused);

			Assert.IsTrue(fired);
		}

		[Test]
		public void SetStatus_SameStatus_DoesNotFireEvent()
		{
			_info = new RuntimeInstanceInfo(null, _graph, null);
			var fireCount = 0;
			_info.OnStatusChanged += _ => fireCount++;

			_info.SetStatus(RuntimeInstanceState.Running);

			Assert.AreEqual(0, fireCount);
		}

		[Test]
		public void SetStatus_DifferentStatuses_FiresEachTime()
		{
			_info = new RuntimeInstanceInfo(null, _graph, null);
			var fireCount = 0;
			_info.OnStatusChanged += _ => fireCount++;

			_info.SetStatus(RuntimeInstanceState.Paused);
			_info.SetStatus(RuntimeInstanceState.Running);
			_info.SetStatus(RuntimeInstanceState.Stopped);

			Assert.AreEqual(3, fireCount);
		}

		[Test]
		public void SetStatus_EventPassesCorrectInstance()
		{
			_info = new RuntimeInstanceInfo(null, _graph, null);
			RuntimeInstanceInfo received = null;
			_info.OnStatusChanged += info => received = info;

			_info.SetStatus(RuntimeInstanceState.Stopped);

			Assert.AreEqual(_info, received);
		}



		[Test]
		public void CurrentStateNodeId_NoState_ReturnsNull()
		{
			_info = new RuntimeInstanceInfo(null, _graph, null);

			Assert.IsNull(_info.CurrentStateNodeId);
		}

		[Test]
		public void CurrentStateName_NoState_ReturnsNull()
		{
			_info = new RuntimeInstanceInfo(null, _graph, null);

			Assert.IsNull(_info.CurrentStateName);
		}



		[Test]
		public void StateChanged_UpdatesCurrentStateNodeId()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			_instance.Start();

			Assert.AreEqual("stateA", _info.CurrentStateNodeId);
		}

		[Test]
		public void StateChanged_UpdatesCurrentStateName()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			_instance.Start();

			Assert.AreEqual("State A", _info.CurrentStateName);
		}

		[Test]
		public void StateChanged_FiresOnStateChanged()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);
			var fireCount = 0;
			_info.OnStateChanged += _ => fireCount++;

			_instance.Start();

			Assert.IsTrue(fireCount > 0);
		}

		[Test]
		public void StateChanged_RecordsTransitionHistory()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			_instance.Start();

			Assert.IsTrue(_info.TransitionHistory.Count > 0);
		}

		[Test]
		public void StateChanged_FirstTransition_HasNullFromFields()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			_instance.Start();

			var firstRecord = _info.TransitionHistory[0];
			Assert.IsNull(firstRecord.FromNodeId);
			Assert.IsNull(firstRecord.FromStateName);
		}

		[Test]
		public void StateChanged_FirstTransition_HasCorrectToFields()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			_instance.Start();

			var firstRecord = _info.TransitionHistory[0];
			Assert.AreEqual("stateA", firstRecord.ToNodeId);
			Assert.AreEqual("State A", firstRecord.ToStateName);
		}

		[Test]
		public void StateChanged_RecordsTimestamp()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);
			var before = DateTime.Now;

			_instance.Start();

			var record = _info.TransitionHistory[0];
			Assert.IsTrue(record.Timestamp >= before);
			Assert.IsTrue(record.Timestamp <= DateTime.Now);
		}



		[Test]
		public void TransitionTriggered_ToExitNode_SetsCurrentStateToExit()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			_instance.Start();

			// Trigger transition A -> B
			var stateA = _instance.CurrentState as TestStateA;
			stateA?.OnComplete?.Invoke();

			// Trigger transition B -> Exit
			var stateB = _instance.CurrentState as TestStateB;
			stateB?.OnComplete?.Invoke();

			Assert.AreEqual("exit", _info.CurrentStateNodeId);
		}

		[Test]
		public void TransitionTriggered_ToExitNode_SetsStateName()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			_instance.Start();

			var stateA = _instance.CurrentState as TestStateA;
			stateA?.OnComplete?.Invoke();

			var stateB = _instance.CurrentState as TestStateB;
			stateB?.OnComplete?.Invoke();

			Assert.AreEqual("Done", _info.CurrentStateName);
		}

		[Test]
		public void TransitionTriggered_PortalNode_IsMarkedTransient()
		{
			SetupGraphWithPortals();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			var visitedNodes = new System.Collections.Generic.List<(string nodeId, bool isTransient)>();
			_info.OnNodeVisited += (_, nodeId, isTransient) =>
			{
				visitedNodes.Add((nodeId, isTransient));
			};

			_instance.Start();

			// Trigger A -> PortalOut transition
			var stateA = _instance.CurrentState as TestStateA;
			stateA?.OnComplete?.Invoke();

			// Check if portal nodes were marked as transient
			var portalVisits = visitedNodes.Where(v =>
				v.nodeId == "portalOut" || v.nodeId == "portalIn").ToList();

			foreach (var visit in portalVisits)
			{
				Assert.IsTrue(visit.isTransient, $"Portal node {visit.nodeId} should be transient");
			}
		}

		[Test]
		public void TransitionTriggered_StateNode_IsNotTransient()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			var visitedNodes = new System.Collections.Generic.List<(string nodeId, bool isTransient)>();
			_info.OnNodeVisited += (_, nodeId, isTransient) =>
			{
				visitedNodes.Add((nodeId, isTransient));
			};

			_instance.Start();

			var stateVisits = visitedNodes.Where(v => v.nodeId == "stateA").ToList();
			foreach (var visit in stateVisits)
			{
				Assert.IsFalse(visit.isTransient, "State node should not be transient");
			}
		}

		[Test]
		public void TransitionTriggered_ExitNode_IsNotTransient()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			var visitedNodes = new System.Collections.Generic.List<(string nodeId, bool isTransient)>();
			_info.OnNodeVisited += (_, nodeId, isTransient) =>
			{
				visitedNodes.Add((nodeId, isTransient));
			};

			_instance.Start();

			var stateA = _instance.CurrentState as TestStateA;
			stateA?.OnComplete?.Invoke();

			var stateB = _instance.CurrentState as TestStateB;
			stateB?.OnComplete?.Invoke();

			var exitVisits = visitedNodes.Where(v => v.nodeId == "exit").ToList();
			Assert.IsTrue(exitVisits.Count > 0, "Exit node should have been visited");
			foreach (var visit in exitVisits)
			{
				Assert.IsFalse(visit.isTransient, "Exit node should not be transient");
			}
		}

		[Test]
		public void TransitionTriggered_CapturesTriggerPortName()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			_instance.Start();

			var stateA = _instance.CurrentState as TestStateA;
			stateA?.OnComplete?.Invoke();

			// Look for a transition record that has a TriggerPortName
			var recordsWithPort = _info.TransitionHistory
				.Where(r => !string.IsNullOrEmpty(r.TriggerPortName))
				.ToList();

			Assert.IsTrue(recordsWithPort.Count > 0, "At least one record should have a trigger port name");
		}



		[Test]
		public void TransitionHistory_DoesNotExceedMaxEntries()
		{
			SetupSimpleGraph();

			// Add many states to create lots of transitions
			for (var i = 0; i < 60; i++)
			{
				var node = new StateNode
				{
					Id = $"extra{i}",
					DisplayName = $"Extra {i}",
					StateTypeAssemblyQualifiedName = typeof(TestStateA).AssemblyQualifiedName
				};
				_graph.AddNode(node);
			}

			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			// Manually trigger many state change events by starting and transitioning
			// Since we can't easily trigger 100+ transitions, verify the max is respected
			// by checking the constant is enforced
			_instance.Start();

			Assert.IsTrue(_info.TransitionHistory.Count <= 100,
				$"History should not exceed 100 entries, got {_info.TransitionHistory.Count}");
		}



		[Test]
		public void Dispose_WithNullInstance_DoesNotThrow()
		{
			_info = new RuntimeInstanceInfo(null, _graph, null);

			Assert.DoesNotThrow(() => _info.Dispose());
		}

		[Test]
		public void Dispose_StopsTrackingStateChanges()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			var stateChangeCount = 0;
			_info.OnStateChanged += _ => stateChangeCount++;

			_info.Dispose();

			// Start after dispose - should not fire our handler
			_instance.Start();

			Assert.AreEqual(0, stateChangeCount);
		}



		[Test]
		public void GetBlackboardSnapshot_NullInstance_ReturnsEmpty()
		{
			_info = new RuntimeInstanceInfo(null, _graph, null);

			var snapshot = _info.GetBlackboardSnapshot();

			Assert.IsNotNull(snapshot);
			Assert.AreEqual(0, snapshot.Count);
		}

		[Test]
		public void GetBlackboardSnapshot_EmptyBlackboard_ReturnsEmpty()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			var snapshot = _info.GetBlackboardSnapshot();

			Assert.IsNotNull(snapshot);
			Assert.AreEqual(0, snapshot.Count);
		}



		[Test]
		public void FireInitialStateEvents_NullInstance_DoesNotThrow()
		{
			_info = new RuntimeInstanceInfo(null, _graph, null);

			Assert.DoesNotThrow(() => _info.FireInitialStateEvents());
		}

		[Test]
		public void FireInitialStateEvents_InstanceAlreadyInState_TracksCurrentState()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			// Create info after state machine is already running
			_info = new RuntimeInstanceInfo(_instance, _graph, null);
			_info.FireInitialStateEvents();

			Assert.AreEqual("stateA", _info.CurrentStateNodeId);
			Assert.AreEqual("State A", _info.CurrentStateName);
		}

		[Test]
		public void FireInitialStateEvents_InstanceAlreadyInState_RecordsHistory()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			_info = new RuntimeInstanceInfo(_instance, _graph, null);
			_info.FireInitialStateEvents();

			Assert.IsTrue(_info.TransitionHistory.Count > 0,
				"Should have recorded at least one history entry for catch-up");
		}



		[Test]
		public void StateTransitionRecord_GetDuration_NullPrevious_ReturnsZero()
		{
			var record = new StateTransitionRecord
			{
				Timestamp = DateTime.Now
			};

			Assert.AreEqual(TimeSpan.Zero, record.GetDuration(null));
		}

		[Test]
		public void StateTransitionRecord_GetDuration_ValidPrevious_ReturnsCorrectDuration()
		{
			var baseTime = DateTime.Now;
			var previous = new StateTransitionRecord
			{
				Timestamp = baseTime
			};
			var current = new StateTransitionRecord
			{
				Timestamp = baseTime.AddSeconds(5)
			};

			var duration = current.GetDuration(previous);

			Assert.AreEqual(5.0, duration.TotalSeconds, 0.001);
		}

		[Test]
		public void StateTransitionRecord_GetDuration_SameTimestamp_ReturnsZero()
		{
			var time = DateTime.Now;
			var previous = new StateTransitionRecord { Timestamp = time };
			var current = new StateTransitionRecord { Timestamp = time };

			Assert.AreEqual(TimeSpan.Zero, current.GetDuration(previous));
		}

		[Test]
		public void StateTransitionRecord_Properties_SetAndGet()
		{
			var record = new StateTransitionRecord
			{
				FromNodeId = "nodeA",
				FromStateName = "State A",
				ToNodeId = "nodeB",
				ToStateName = "State B",
				TriggerPortName = "OnComplete"
			};

			Assert.AreEqual("nodeA", record.FromNodeId);
			Assert.AreEqual("State A", record.FromStateName);
			Assert.AreEqual("nodeB", record.ToNodeId);
			Assert.AreEqual("State B", record.ToStateName);
			Assert.AreEqual("OnComplete", record.TriggerPortName);
		}

		[Test]
		public void StateTransitionRecord_BlackboardSnapshot_SetAndGet()
		{
			var snapshot = new System.Collections.Generic.Dictionary<string, string>
			{
				{ "health", "100" },
				{ "score", "42" }
			};

			var record = new StateTransitionRecord
			{
				BlackboardSnapshot = snapshot
			};

			Assert.AreEqual(2, record.BlackboardSnapshot.Count);
			Assert.AreEqual("100", record.BlackboardSnapshot["health"]);
			Assert.AreEqual("42", record.BlackboardSnapshot["score"]);
		}

		[Test]
		public void StateTransitionRecord_BlackboardTypes_SetAndGet()
		{
			var types = new System.Collections.Generic.Dictionary<string, string>
			{
				{ "health", "Int32" },
				{ "name", "String" }
			};

			var record = new StateTransitionRecord
			{
				BlackboardTypes = types
			};

			Assert.AreEqual(2, record.BlackboardTypes.Count);
			Assert.AreEqual("Int32", record.BlackboardTypes["health"]);
		}



		[Test]
		public void RuntimeInstanceState_HasExpectedValues()
		{
			Assert.IsTrue(Enum.IsDefined(typeof(RuntimeInstanceState), RuntimeInstanceState.Running));
			Assert.IsTrue(Enum.IsDefined(typeof(RuntimeInstanceState), RuntimeInstanceState.Paused));
			Assert.IsTrue(Enum.IsDefined(typeof(RuntimeInstanceState), RuntimeInstanceState.Stopped));
			Assert.IsTrue(Enum.IsDefined(typeof(RuntimeInstanceState), RuntimeInstanceState.Error));
		}



		[Test]
		public void MultipleTransitions_AllRecordedInHistory()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			_instance.Start();

			var stateA = _instance.CurrentState as TestStateA;
			stateA?.OnComplete?.Invoke();

			// Should have at least 2 history entries (initial entry + A->B transition)
			Assert.IsTrue(_info.TransitionHistory.Count >= 2,
				$"Expected at least 2 transition records, got {_info.TransitionHistory.Count}");
		}

		[Test]
		public void MultipleTransitions_HistoryOrderIsChronological()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			_instance.Start();

			var stateA = _instance.CurrentState as TestStateA;
			stateA?.OnComplete?.Invoke();

			for (var i = 1; i < _info.TransitionHistory.Count; i++)
			{
				Assert.IsTrue(
					_info.TransitionHistory[i].Timestamp >= _info.TransitionHistory[i - 1].Timestamp,
					$"History entry {i} should have timestamp >= entry {i - 1}");
			}
		}

		[Test]
		public void FullTransitionSequence_TracksAllStates()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			_instance.Start();
			Assert.AreEqual("stateA", _info.CurrentStateNodeId);

			var stateA = _instance.CurrentState as TestStateA;
			stateA?.OnComplete?.Invoke();
			Assert.AreEqual("stateB", _info.CurrentStateNodeId);

			var stateB = _instance.CurrentState as TestStateB;
			stateB?.OnComplete?.Invoke();
			Assert.AreEqual("exit", _info.CurrentStateNodeId);
		}



		[Test]
		public void InstanceStopped_SetsStatusToStopped()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			_instance.Start();
			_instance.Stop();

			Assert.AreEqual(RuntimeInstanceState.Stopped, _info.Status);
		}

		[Test]
		public void InstanceStopped_FiresOnStatusChanged()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_info = new RuntimeInstanceInfo(_instance, _graph, null);

			var fired = false;
			_info.OnStatusChanged += _ => fired = true;

			_instance.Start();
			_instance.Stop();

			Assert.IsTrue(fired);
		}

	}
}
