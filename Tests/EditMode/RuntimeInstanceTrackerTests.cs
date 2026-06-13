using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Editor.Runtime;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	/// <summary>
	/// Edge-case and gap-coverage tests for RuntimeInstanceTracker,
	/// RuntimeInstanceInfo, StateTransitionRecord, and RuntimeTrackerStats.
	/// Complements RuntimeMonitoringTests which covers the core happy paths.
	/// </summary>
	[TestFixture]
	public class RuntimeInstanceTrackerTests
	{
		private StateMachineGraph _graph;
		private StateMachineInstance _instance;
		private GameObject _owner;

		private class TrackerTestStateA : State
		{
			[Transition("Next")] public Action onNext;

			public override void OnEnter() { }
		}

		private class TrackerTestStateB : State
		{
			[Transition("Done")] public Action onDone;

			public override void OnEnter() { }
		}

		[SetUp]
		public void SetUp()
		{
			RuntimeInstanceTracker.ClearAll();
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			_owner = new GameObject("TrackerTestOwner");
		}

		[TearDown]
		public void TearDown()
		{
			RuntimeInstanceTracker.ClearAll();

			_instance?.Stop();
			_instance = null;

			if (_graph != null)
			{
				UnityEngine.Object.DestroyImmediate(_graph);
			}

			if (_owner != null)
			{
				UnityEngine.Object.DestroyImmediate(_owner);
			}
		}

		private void SetupSimpleGraph()
		{
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				DisplayName = "State A",
				StateTypeAssemblyQualifiedName = typeof(TrackerTestStateA).AssemblyQualifiedName
			};
			var stateB = new StateNode
			{
				Id = "stateB",
				DisplayName = "State B",
				StateTypeAssemblyQualifiedName = typeof(TrackerTestStateB).AssemblyQualifiedName
			};
			var exit = new ExitNode { Id = "exit" };

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(stateB);
			_graph.AddNode(exit);

			_graph.AddConnection(new Connection
			{
				SourceNodeId = "entry",
				SourcePortName = "Start",
				TargetNodeId = "stateA",
				TargetPortName = "Enter"
			});
			_graph.AddConnection(new Connection
			{
				SourceNodeId = "stateA",
				SourcePortName = "Next",
				TargetNodeId = "stateB",
				TargetPortName = "Enter"
			});
			_graph.AddConnection(new Connection
			{
				SourceNodeId = "stateB",
				SourcePortName = "Done",
				TargetNodeId = "exit",
				TargetPortName = "Enter"
			});
		}

		[Test]
		public void GetStats_WithMixedStatuses_ReturnsCorrectCounts()
		{
			SetupSimpleGraph();

			var instance1 = new StateMachineInstance(_graph);
			instance1.Start();
			RuntimeInstanceTracker.Register(instance1, _graph, _owner);

			var instance2 = new StateMachineInstance(_graph);
			instance2.Start();
			var info2 = RuntimeInstanceTracker.Register(instance2, _graph, _owner);
			info2.SetStatus(RuntimeInstanceState.Paused);

			var instance3 = new StateMachineInstance(_graph);
			instance3.Start();
			var info3 = RuntimeInstanceTracker.Register(instance3, _graph, _owner);
			info3.SetStatus(RuntimeInstanceState.Stopped);

			var stats = RuntimeInstanceTracker.GetStats();

			Assert.AreEqual(3, stats.TotalInstances);
			Assert.AreEqual(1, stats.RunningInstances);
			Assert.AreEqual(1, stats.PausedInstances);
			Assert.AreEqual(1, stats.StoppedInstances);
			Assert.AreEqual(0, stats.ErrorInstances);

			instance1.Stop();
			instance2.Stop();
			instance3.Stop();
		}

		[Test]
		public void GetStats_WithErrorInstance_ReportsError()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var info = RuntimeInstanceTracker.Register(_instance, _graph, _owner);
			info.SetStatus(RuntimeInstanceState.Error);

			var stats = RuntimeInstanceTracker.GetStats();

			Assert.AreEqual(1, stats.ErrorInstances);
			Assert.AreEqual(0, stats.RunningInstances);
		}

		[Test]
		public void GetStats_UniqueGraphs_CountsDistinctGraphs()
		{
			SetupSimpleGraph();

			var graph2 = ScriptableObject.CreateInstance<StateMachineGraph>();
			graph2.AddNode(new EntryNode { Id = "entry2" });

			var instance1 = new StateMachineInstance(_graph);
			var instance2 = new StateMachineInstance(graph2);

			RuntimeInstanceTracker.Register(instance1, _graph, _owner);
			RuntimeInstanceTracker.Register(instance2, graph2, _owner);

			var stats = RuntimeInstanceTracker.GetStats();

			Assert.AreEqual(2, stats.TotalInstances);
			Assert.AreEqual(2, stats.UniqueGraphs);

			instance1.Stop();
			instance2.Stop();
			UnityEngine.Object.DestroyImmediate(graph2);
		}

		[Test]
		public void GetActiveInstanceForGraph_FallsBackToStoppedInstance()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var info = RuntimeInstanceTracker.Register(_instance, _graph, _owner);
			info.SetStatus(RuntimeInstanceState.Stopped);

			var active = RuntimeInstanceTracker.GetActiveInstanceForGraph(_graph);

			Assert.IsNotNull(active);
			Assert.AreEqual(RuntimeInstanceState.Stopped, active.Status);
		}

		[Test]
		public void GetActiveInstanceForGraph_PrefersRunningOverStopped()
		{
			SetupSimpleGraph();

			var stoppedInstance = new StateMachineInstance(_graph);
			stoppedInstance.Start();
			var stoppedInfo = RuntimeInstanceTracker.Register(stoppedInstance, _graph, _owner);
			stoppedInfo.SetStatus(RuntimeInstanceState.Stopped);

			var runningInstance = new StateMachineInstance(_graph);
			runningInstance.Start();
			RuntimeInstanceTracker.Register(runningInstance, _graph, _owner);

			var active = RuntimeInstanceTracker.GetActiveInstanceForGraph(_graph);

			Assert.IsNotNull(active);
			Assert.AreEqual(RuntimeInstanceState.Running, active.Status);
			Assert.AreEqual(runningInstance, active.Instance);

			stoppedInstance.Stop();
			runningInstance.Stop();
		}

		[Test]
		public void GetActiveInstanceForGraph_ReturnsNullWhenOnlyPaused()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var info = RuntimeInstanceTracker.Register(_instance, _graph, _owner);
			info.SetStatus(RuntimeInstanceState.Paused);

			var active = RuntimeInstanceTracker.GetActiveInstanceForGraph(_graph);

			Assert.IsNull(active);
		}

		[Test]
		public void Unregister_CleansUpGraphPathTracking()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var info = RuntimeInstanceTracker.Register(_instance, _graph, _owner);

			RuntimeInstanceTracker.Unregister(info.InstanceId);

			var instances = RuntimeInstanceTracker.GetInstancesForGraph(_graph).ToList();

			Assert.AreEqual(0, instances.Count);
		}

		[Test]
		public void Unregister_OneOfMultiple_LeavesOthers()
		{
			SetupSimpleGraph();
			var instance1 = new StateMachineInstance(_graph);
			var instance2 = new StateMachineInstance(_graph);

			var info1 = RuntimeInstanceTracker.Register(instance1, _graph, _owner);
			RuntimeInstanceTracker.Register(instance2, _graph, _owner);

			RuntimeInstanceTracker.Unregister(info1.InstanceId);

			var instances = RuntimeInstanceTracker.GetInstancesForGraph(_graph).ToList();

			Assert.AreEqual(1, instances.Count);
			Assert.AreEqual(instance2, instances[0].Instance);

			instance1.Stop();
			instance2.Stop();
		}

		[Test]
		public void GetInstancesForOwner_WithMultipleOwners_ReturnsCorrectSubset()
		{
			SetupSimpleGraph();
			var owner2 = new GameObject("SecondOwner");

			var instance1 = new StateMachineInstance(_graph);
			var instance2 = new StateMachineInstance(_graph);

			RuntimeInstanceTracker.Register(instance1, _graph, _owner);
			RuntimeInstanceTracker.Register(instance2, _graph, owner2);

			var ownerInstances = RuntimeInstanceTracker.GetInstancesForOwner(_owner).ToList();
			var owner2Instances = RuntimeInstanceTracker.GetInstancesForOwner(owner2).ToList();

			Assert.AreEqual(1, ownerInstances.Count);
			Assert.AreEqual(instance1, ownerInstances[0].Instance);
			Assert.AreEqual(1, owner2Instances.Count);
			Assert.AreEqual(instance2, owner2Instances[0].Instance);

			instance1.Stop();
			instance2.Stop();
			UnityEngine.Object.DestroyImmediate(owner2);
		}

		[Test]
		public void GetInstancesForOwner_UnknownOwner_ReturnsEmpty()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			RuntimeInstanceTracker.Register(_instance, _graph, _owner);

			var otherOwner = new GameObject("OtherOwner");
			var instances = RuntimeInstanceTracker.GetInstancesForOwner(otherOwner).ToList();

			Assert.AreEqual(0, instances.Count);

			UnityEngine.Object.DestroyImmediate(otherOwner);
		}

		[Test]
		public void TransitionHistory_RecordsTransitions()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var info = RuntimeInstanceTracker.Register(_instance, _graph, _owner);

			_instance.Update();

			Assert.Greater(info.TransitionHistory.Count, 0);
		}

		[Test]
		public void TransitionHistory_RecordContainsTimestamp()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var before = DateTime.Now;
			var info = RuntimeInstanceTracker.Register(_instance, _graph, _owner);
			_instance.Update();
			var after = DateTime.Now;

			if (info.TransitionHistory.Count > 0)
			{
				var record = info.TransitionHistory[info.TransitionHistory.Count - 1];
				Assert.GreaterOrEqual(record.Timestamp, before);
				Assert.LessOrEqual(record.Timestamp, after);
			}
		}

		[Test]
		public void RuntimeInstanceInfo_StartTime_IsSetOnCreation()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var before = DateTime.Now;
			var info = new RuntimeInstanceInfo(_instance, _graph, _owner);
			var after = DateTime.Now;

			Assert.GreaterOrEqual(info.StartTime, before);
			Assert.LessOrEqual(info.StartTime, after);

			info.Dispose();
		}

		[Test]
		public void RuntimeInstanceInfo_InstanceId_IsUnique()
		{
			SetupSimpleGraph();
			var instance1 = new StateMachineInstance(_graph);
			var instance2 = new StateMachineInstance(_graph);

			var info1 = new RuntimeInstanceInfo(instance1, _graph, _owner);
			var info2 = new RuntimeInstanceInfo(instance2, _graph, _owner);

			Assert.AreNotEqual(info1.InstanceId, info2.InstanceId);

			info1.Dispose();
			info2.Dispose();
			instance1.Stop();
			instance2.Stop();
		}

		[Test]
		public void RuntimeInstanceInfo_WithNullInstance_DoesNotThrow()
		{
			Assert.DoesNotThrow(() =>
			{
				var info = new RuntimeInstanceInfo(null, _graph, _owner);
				info.Dispose();
			});
		}

		[Test]
		public void RuntimeInstanceInfo_Dispose_CanBeCalledMultipleTimes()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var info = new RuntimeInstanceInfo(_instance, _graph, _owner);

			Assert.DoesNotThrow(() =>
			{
				info.Dispose();
				info.Dispose();
			});
		}

		[Test]
		public void RuntimeInstanceInfo_SetStatus_ToSameValue_DoesNotFireEvent()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var info = new RuntimeInstanceInfo(_instance, _graph, _owner);
			info.SetStatus(RuntimeInstanceState.Paused);

			var eventFired = false;
			info.OnStatusChanged += _ => eventFired = true;

			info.SetStatus(RuntimeInstanceState.Paused);

			Assert.IsFalse(eventFired);

			info.Dispose();
		}

		[Test]
		public void StateTransitionRecord_PropertiesCanBeSetAndRead()
		{
			var record = new StateTransitionRecord
			{
				Timestamp = DateTime.Now,
				FromNodeId = "node-1",
				FromStateName = "StateA",
				ToNodeId = "node-2",
				ToStateName = "StateB",
				TriggerPortName = "Next"
			};

			Assert.AreEqual("node-1", record.FromNodeId);
			Assert.AreEqual("StateA", record.FromStateName);
			Assert.AreEqual("node-2", record.ToNodeId);
			Assert.AreEqual("StateB", record.ToStateName);
			Assert.AreEqual("Next", record.TriggerPortName);
		}

		[Test]
		public void StateTransitionRecord_BlackboardSnapshot_CanBeSetAndRead()
		{
			var snapshot = new Dictionary<string, string>
			{
				{ "health", "100" },
				{ "speed", "5.5" }
			};

			var record = new StateTransitionRecord
			{
				BlackboardSnapshot = snapshot
			};

			Assert.AreEqual(2, record.BlackboardSnapshot.Count);
			Assert.AreEqual("100", record.BlackboardSnapshot["health"]);
		}

		[Test]
		public void StateTransitionRecord_BlackboardTypes_CanBeSetAndRead()
		{
			var types = new Dictionary<string, string>
			{
				{ "health", "Int32" },
				{ "speed", "Single" }
			};

			var record = new StateTransitionRecord
			{
				BlackboardTypes = types
			};

			Assert.AreEqual(2, record.BlackboardTypes.Count);
			Assert.AreEqual("Int32", record.BlackboardTypes["health"]);
		}

		[Test]
		public void HasRunningInstances_FalseWhenAllPaused()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var info = RuntimeInstanceTracker.Register(_instance, _graph, _owner);
			info.SetStatus(RuntimeInstanceState.Paused);

			Assert.IsFalse(RuntimeInstanceTracker.HasRunningInstances());
		}

		[Test]
		public void HasRunningInstances_FalseWhenAllStopped()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var info = RuntimeInstanceTracker.Register(_instance, _graph, _owner);
			info.SetStatus(RuntimeInstanceState.Stopped);

			Assert.IsFalse(RuntimeInstanceTracker.HasRunningInstances());
		}

		[Test]
		public void RuntimeInstanceState_HasExpectedValues()
		{
			Assert.AreEqual(0, (int)RuntimeInstanceState.Running);
			Assert.AreEqual(1, (int)RuntimeInstanceState.Paused);
			Assert.AreEqual(2, (int)RuntimeInstanceState.Stopped);
			Assert.AreEqual(3, (int)RuntimeInstanceState.Error);
		}

		[Test]
		public void ClearAll_DisposesAllInstances()
		{
			SetupSimpleGraph();
			var instance1 = new StateMachineInstance(_graph);
			var instance2 = new StateMachineInstance(_graph);

			RuntimeInstanceTracker.Register(instance1, _graph, _owner);
			RuntimeInstanceTracker.Register(instance2, _graph, _owner);

			RuntimeInstanceTracker.ClearAll();

			Assert.AreEqual(0, RuntimeInstanceTracker.InstanceCount);
			Assert.AreEqual(0, RuntimeInstanceTracker.AllInstances.Count);

			instance1.Stop();
			instance2.Stop();
		}

		[Test]
		public void Register_WithNullOwner_StillRegisters()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var info = RuntimeInstanceTracker.Register(_instance, _graph, null);

			Assert.IsNotNull(info);
			Assert.IsNull(info.Owner);
		}

		[Test]
		public void Register_WithNullGraph_StillRegisters()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var info = RuntimeInstanceTracker.Register(_instance, null, _owner);

			Assert.IsNotNull(info);
			Assert.IsNull(info.Graph);
		}

		[Test]
		public void Unregister_ByUnknownInstance_DoesNotThrow()
		{
			SetupSimpleGraph();
			var unknownInstance = new StateMachineInstance(_graph);

			Assert.DoesNotThrow(() => RuntimeInstanceTracker.Unregister(unknownInstance));

			unknownInstance.Stop();
		}

		[Test]
		public void GetStats_EmptyTracker_ReturnsAllZeros()
		{
			var stats = RuntimeInstanceTracker.GetStats();

			Assert.AreEqual(0, stats.TotalInstances);
			Assert.AreEqual(0, stats.RunningInstances);
			Assert.AreEqual(0, stats.PausedInstances);
			Assert.AreEqual(0, stats.StoppedInstances);
			Assert.AreEqual(0, stats.ErrorInstances);
			Assert.AreEqual(0, stats.UniqueGraphs);
		}

		[Test]
		public void GetInstancesForGraph_AfterClearAll_ReturnsEmpty()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			RuntimeInstanceTracker.Register(_instance, _graph, _owner);

			RuntimeInstanceTracker.ClearAll();

			var instances = RuntimeInstanceTracker.GetInstancesForGraph(_graph).ToList();

			Assert.AreEqual(0, instances.Count);
		}

		[Test]
		public void Register_MultipleInstancesDifferentGraphs_TracksIndependently()
		{
			SetupSimpleGraph();
			var graph2 = ScriptableObject.CreateInstance<StateMachineGraph>();
			graph2.AddNode(new EntryNode { Id = "entry2" });

			var instance1 = new StateMachineInstance(_graph);
			var instance2 = new StateMachineInstance(graph2);

			RuntimeInstanceTracker.Register(instance1, _graph, _owner);
			RuntimeInstanceTracker.Register(instance2, graph2, _owner);

			var graph1Instances = RuntimeInstanceTracker.GetInstancesForGraph(_graph).ToList();
			var graph2Instances = RuntimeInstanceTracker.GetInstancesForGraph(graph2).ToList();

			Assert.AreEqual(1, graph1Instances.Count);
			Assert.AreEqual(1, graph2Instances.Count);
			Assert.AreEqual(instance1, graph1Instances[0].Instance);
			Assert.AreEqual(instance2, graph2Instances[0].Instance);

			instance1.Stop();
			instance2.Stop();
			UnityEngine.Object.DestroyImmediate(graph2);
		}

		[Test]
		public void RuntimeTrackerStats_FieldsAreWritable()
		{
			var stats = new RuntimeTrackerStats
			{
				TotalInstances = 10,
				RunningInstances = 5,
				PausedInstances = 2,
				StoppedInstances = 2,
				ErrorInstances = 1,
				UniqueGraphs = 3
			};

			Assert.AreEqual(10, stats.TotalInstances);
			Assert.AreEqual(5, stats.RunningInstances);
			Assert.AreEqual(2, stats.PausedInstances);
			Assert.AreEqual(2, stats.StoppedInstances);
			Assert.AreEqual(1, stats.ErrorInstances);
			Assert.AreEqual(3, stats.UniqueGraphs);
		}
	}
}
