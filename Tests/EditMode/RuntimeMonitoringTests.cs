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
	[TestFixture]
	public class RuntimeMonitoringTests
	{
		private StateMachineGraph _graph;
		private StateMachineInstance _instance;
		private GameObject _owner;


		private class MonitorTestStateA : State
		{
			[Transition("Next")] public Action onNext;

			public override void OnEnter() { }
		}

		private class MonitorTestStateB : State
		{
			[Transition("Next")] public Action onNext;
			[Transition("Exit")] public Action onExit;

			public override void OnEnter() { }
		}


		[SetUp]
		public void SetUp()
		{
			// Clear any existing tracked instances
			RuntimeInstanceTracker.ClearAll();

			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			_owner = new GameObject("TestOwner");
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
				StateTypeAssemblyQualifiedName = typeof(MonitorTestStateA).AssemblyQualifiedName
			};
			var stateB = new StateNode
			{
				Id = "stateB",
				DisplayName = "State B",
				StateTypeAssemblyQualifiedName = typeof(MonitorTestStateB).AssemblyQualifiedName
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
				SourcePortName = "Exit",
				TargetNodeId = "exit",
				TargetPortName = "Enter"
			});
		}


		[Test]
		public void Register_WithValidInstance_ReturnsRuntimeInstanceInfo()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var info = RuntimeInstanceTracker.Register(_instance, _graph, _owner);

			Assert.IsNotNull(info);
			Assert.AreEqual(_instance, info.Instance);
			Assert.AreEqual(_graph, info.Graph);
			Assert.AreEqual(_owner, info.Owner);
		}

		[Test]
		public void Register_WithNullInstance_ReturnsNull()
		{
			var info = RuntimeInstanceTracker.Register(null, _graph, _owner);

			Assert.IsNull(info);
		}

		[Test]
		public void Register_SameInstanceTwice_ReturnsSameInfo()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var info1 = RuntimeInstanceTracker.Register(_instance, _graph, _owner);
			var info2 = RuntimeInstanceTracker.Register(_instance, _graph, _owner);

			Assert.AreSame(info1, info2);
		}

		[Test]
		public void Register_FiresOnInstanceRegistered()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			RuntimeInstanceInfo registeredInfo = null;
			RuntimeInstanceTracker.OnInstanceRegistered += info => registeredInfo = info;

			var info = RuntimeInstanceTracker.Register(_instance, _graph, _owner);

			Assert.AreSame(info, registeredInfo);
		}

		[Test]
		public void Unregister_ByInstanceId_RemovesInstance()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var info = RuntimeInstanceTracker.Register(_instance, _graph, _owner);
			var instanceId = info.InstanceId;

			RuntimeInstanceTracker.Unregister(instanceId);

			Assert.IsNull(RuntimeInstanceTracker.GetInstance(instanceId));
		}

		[Test]
		public void Unregister_ByStateMachineInstance_RemovesInstance()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var info = RuntimeInstanceTracker.Register(_instance, _graph, _owner);
			var instanceId = info.InstanceId;

			RuntimeInstanceTracker.Unregister(_instance);

			Assert.IsNull(RuntimeInstanceTracker.GetInstance(instanceId));
		}

		[Test]
		public void Unregister_FiresOnInstanceUnregistered()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var info = RuntimeInstanceTracker.Register(_instance, _graph, _owner);

			RuntimeInstanceInfo unregisteredInfo = null;
			RuntimeInstanceTracker.OnInstanceUnregistered += i => unregisteredInfo = i;

			RuntimeInstanceTracker.Unregister(info.InstanceId);

			Assert.AreSame(info, unregisteredInfo);
		}

		[Test]
		public void GetInstancesForGraph_ReturnsCorrectInstances()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			RuntimeInstanceTracker.Register(_instance, _graph, _owner);

			var instances = RuntimeInstanceTracker.GetInstancesForGraph(_graph).ToList();

			Assert.AreEqual(1, instances.Count);
			Assert.AreEqual(_instance, instances[0].Instance);
		}

		[Test]
		public void GetInstancesForOwner_ReturnsCorrectInstances()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			RuntimeInstanceTracker.Register(_instance, _graph, _owner);

			var instances = RuntimeInstanceTracker.GetInstancesForOwner(_owner).ToList();

			Assert.AreEqual(1, instances.Count);
			Assert.AreEqual(_instance, instances[0].Instance);
		}

		[Test]
		public void GetInstanceForStateMachine_ReturnsCorrectInstance()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var registered = RuntimeInstanceTracker.Register(_instance, _graph, _owner);
			var found = RuntimeInstanceTracker.GetInstanceForStateMachine(_instance);

			Assert.AreSame(registered, found);
		}

		[Test]
		public void ClearAll_RemovesAllInstances()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			RuntimeInstanceTracker.Register(_instance, _graph, _owner);

			RuntimeInstanceTracker.ClearAll();

			Assert.AreEqual(0, RuntimeInstanceTracker.InstanceCount);
		}

		[Test]
		public void ClearAll_FiresOnInstancesCleared()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			RuntimeInstanceTracker.Register(_instance, _graph, _owner);

			var clearedFired = false;
			RuntimeInstanceTracker.OnInstancesCleared += () => clearedFired = true;

			RuntimeInstanceTracker.ClearAll();

			Assert.IsTrue(clearedFired);
		}

		[Test]
		public void HasRunningInstances_ReturnsTrueWhenRunning()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			RuntimeInstanceTracker.Register(_instance, _graph, _owner);

			Assert.IsTrue(RuntimeInstanceTracker.HasRunningInstances());
		}

		[Test]
		public void HasRunningInstances_ReturnsFalseWhenEmpty()
		{
			Assert.IsFalse(RuntimeInstanceTracker.HasRunningInstances());
		}

		[Test]
		public void GetStats_ReturnsCorrectCounts()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			RuntimeInstanceTracker.Register(_instance, _graph, _owner);

			var stats = RuntimeInstanceTracker.GetStats();

			Assert.AreEqual(1, stats.TotalInstances);
			Assert.AreEqual(1, stats.RunningInstances);
			Assert.AreEqual(0, stats.StoppedInstances);
		}

		[Test]
		public void AllInstances_ReturnsAllRegisteredInstances()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			RuntimeInstanceTracker.Register(_instance, _graph, _owner);

			var all = RuntimeInstanceTracker.AllInstances;

			Assert.AreEqual(1, all.Count);
		}

		[Test]
		public void InstanceCount_ReturnsCorrectCount()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			Assert.AreEqual(0, RuntimeInstanceTracker.InstanceCount);

			RuntimeInstanceTracker.Register(_instance, _graph, _owner);

			Assert.AreEqual(1, RuntimeInstanceTracker.InstanceCount);
		}



		[Test]
		public void RuntimeInstanceInfo_InitializesWithCorrectValues()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var info = new RuntimeInstanceInfo(_instance, _graph, _owner);

			Assert.AreEqual(_instance, info.Instance);
			Assert.AreEqual(_graph, info.Graph);
			Assert.AreEqual(_owner, info.Owner);
			Assert.AreEqual(RuntimeInstanceState.Running, info.Status);
			Assert.IsFalse(string.IsNullOrEmpty(info.InstanceId));
		}

		[Test]
		public void RuntimeInstanceInfo_SetStatus_ChangesStatus()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var info = new RuntimeInstanceInfo(_instance, _graph, _owner);

			info.SetStatus(RuntimeInstanceState.Stopped);

			Assert.AreEqual(RuntimeInstanceState.Stopped, info.Status);
		}

		[Test]
		public void RuntimeInstanceInfo_SetStatus_FiresOnStatusChanged()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var info = new RuntimeInstanceInfo(_instance, _graph, _owner);

			RuntimeInstanceInfo changedInfo = null;
			info.OnStatusChanged += i => changedInfo = i;

			info.SetStatus(RuntimeInstanceState.Paused);

			Assert.AreSame(info, changedInfo);
		}

		[Test]
		public void RuntimeInstanceInfo_SetStatus_DoesNotFireWhenUnchanged()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var info = new RuntimeInstanceInfo(_instance, _graph, _owner);

			var eventFired = false;
			info.OnStatusChanged += i => eventFired = true;

			info.SetStatus(RuntimeInstanceState.Running); // Same as initial

			Assert.IsFalse(eventFired);
		}

		[Test]
		public void RuntimeInstanceInfo_TransitionHistory_InitiallyEmpty()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var info = new RuntimeInstanceInfo(_instance, _graph, _owner);

			Assert.AreEqual(0, info.TransitionHistory.Count);
		}

		[Test]
		public void RuntimeInstanceInfo_Dispose_UnsubscribesFromEvents()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var info = new RuntimeInstanceInfo(_instance, _graph, _owner);

			// Should not throw
			Assert.DoesNotThrow(() => info.Dispose());
		}

		[Test]
		public void RuntimeInstanceInfo_GetBlackboardSnapshot_ReturnsEmptyForEmptyBlackboard()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);

			var info = new RuntimeInstanceInfo(_instance, _graph, _owner);

			var snapshot = info.GetBlackboardSnapshot();

			Assert.IsNotNull(snapshot);
			Assert.AreEqual(0, snapshot.Count);
		}

		[Test]
		public void RuntimeInstanceInfo_GetBlackboardSnapshot_ReturnsValues()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Blackboard.Set("testKey", 42);

			var info = new RuntimeInstanceInfo(_instance, _graph, _owner);

			var snapshot = info.GetBlackboardSnapshot();

			Assert.AreEqual(1, snapshot.Count);
			Assert.IsTrue(snapshot.ContainsKey("testKey"));
		}



		[Test]
		public void StateTransitionRecord_GetDuration_ReturnsZeroForNullPrevious()
		{
			var record = new StateTransitionRecord
			{
				Timestamp = DateTime.Now
			};

			var duration = record.GetDuration(null);

			Assert.AreEqual(TimeSpan.Zero, duration);
		}

		[Test]
		public void StateTransitionRecord_GetDuration_ReturnsCorrectDuration()
		{
			var time1 = DateTime.Now;
			var time2 = time1.AddSeconds(5);

			var record1 = new StateTransitionRecord { Timestamp = time1 };
			var record2 = new StateTransitionRecord { Timestamp = time2 };

			var duration = record2.GetDuration(record1);

			Assert.AreEqual(TimeSpan.FromSeconds(5), duration);
		}



		[Test]
		public void RuntimeTrackerStats_DefaultValues()
		{
			var stats = new RuntimeTrackerStats();

			Assert.AreEqual(0, stats.TotalInstances);
			Assert.AreEqual(0, stats.RunningInstances);
			Assert.AreEqual(0, stats.PausedInstances);
			Assert.AreEqual(0, stats.StoppedInstances);
			Assert.AreEqual(0, stats.ErrorInstances);
			Assert.AreEqual(0, stats.UniqueGraphs);
		}



		[Test]
		public void StateChange_FiresOnStateChanged()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var info = RuntimeInstanceTracker.Register(_instance, _graph, _owner);

			RuntimeInstanceInfo changedInfo = null;
			RuntimeInstanceTracker.OnInstanceStateChanged += i => changedInfo = i;

			// Trigger a transition by calling Update (which processes the state machine)
			_instance.Update();

			// The state machine should have started and triggered state change
			Assert.AreSame(info, changedInfo);
		}

		[Test]
		public void GetActiveInstanceForGraph_ReturnsRunningInstance()
		{
			SetupSimpleGraph();
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			RuntimeInstanceTracker.Register(_instance, _graph, _owner);

			var active = RuntimeInstanceTracker.GetActiveInstanceForGraph(_graph);

			Assert.IsNotNull(active);
			Assert.AreEqual(RuntimeInstanceState.Running, active.Status);
		}

		[Test]
		public void GetActiveInstanceForGraph_ReturnsNullForUnknownGraph()
		{
			var otherGraph = ScriptableObject.CreateInstance<StateMachineGraph>();

			try
			{
				var active = RuntimeInstanceTracker.GetActiveInstanceForGraph(otherGraph);
				Assert.IsNull(active);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(otherGraph);
			}
		}

		[Test]
		public void MultipleInstances_SameGraph_AllTracked()
		{
			SetupSimpleGraph();
			var instance1 = new StateMachineInstance(_graph);
			var instance2 = new StateMachineInstance(_graph);

			RuntimeInstanceTracker.Register(instance1, _graph, _owner);
			RuntimeInstanceTracker.Register(instance2, _graph, _owner);

			var instances = RuntimeInstanceTracker.GetInstancesForGraph(_graph).ToList();

			Assert.AreEqual(2, instances.Count);

			// Cleanup
			instance1.Stop();
			instance2.Stop();
		}

		[Test]
		public void UnregisterNonExistent_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => RuntimeInstanceTracker.Unregister("non-existent-id"));
		}

		[Test]
		public void GetInstance_NonExistent_ReturnsNull()
		{
			var result = RuntimeInstanceTracker.GetInstance("non-existent-id");

			Assert.IsNull(result);
		}

		[Test]
		public void GetInstancesForGraph_NullGraph_YieldsEmpty()
		{
			var instances = RuntimeInstanceTracker.GetInstancesForGraph(null).ToList();

			Assert.AreEqual(0, instances.Count);
		}

	}
}
