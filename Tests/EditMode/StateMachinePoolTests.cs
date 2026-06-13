using System;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Blackboard;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class StateMachinePoolTests
	{
		private StateMachineGraph _graph;
		private StateMachinePool _pool;

		private class PoolTestStateA : State
		{
			public static int EnterCount;
			public static int ExitCount;
			public static int ResetCount;

			[Transition("To B")] public Action toB;

			public override void OnEnter() => EnterCount++;
			public override void OnExit() => ExitCount++;
			public override void OnReset() => ResetCount++;

			public static void ResetCounters()
			{
				EnterCount = 0;
				ExitCount = 0;
				ResetCount = 0;
			}
		}

		private class PoolTestStateB : State
		{
			public static int EnterCount;
			public static int ExitCount;
			public static int ResetCount;

			[Transition("To Exit")] public Action toExit;

			public override void OnEnter() => EnterCount++;
			public override void OnExit() => ExitCount++;
			public override void OnReset() => ResetCount++;

			public static void ResetCounters()
			{
				EnterCount = 0;
				ExitCount = 0;
				ResetCount = 0;
			}
		}

		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			_pool = new StateMachinePool();
			PoolTestStateA.ResetCounters();
			PoolTestStateB.ResetCounters();
			SetupGraph();
		}

		[TearDown]
		public void TearDown()
		{
			_pool.ClearAll();

			if (_graph != null)
			{
				UnityEngine.Object.DestroyImmediate(_graph);
			}
		}

		private void SetupGraph()
		{
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode
			{
				Id = "stateA",
				DisplayName = "State A",
				StateTypeAssemblyQualifiedName = typeof(PoolTestStateA).AssemblyQualifiedName
			};
			var stateB = new StateNode
			{
				Id = "stateB",
				DisplayName = "State B",
				StateTypeAssemblyQualifiedName = typeof(PoolTestStateB).AssemblyQualifiedName
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
				SourcePortName = "toB",
				TargetNodeId = "stateB",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c3",
				SourceNodeId = "stateB",
				SourcePortName = "toExit",
				TargetNodeId = "exit",
				TargetPortName = "in",
				ConnectionType = ConnectionType.Transition
			});
		}

		[Test]
		public void Rent_ReturnsInstance()
		{
			var instance = _pool.Rent(_graph, null);

			Assert.IsNotNull(instance);
			Assert.AreEqual(_graph, instance.Graph);

			_pool.Return(instance);
		}

		[Test]
		public void Rent_NullGraph_ThrowsException()
		{
			Assert.Throws<ArgumentNullException>(() => _pool.Rent(null, null));
		}

		[Test]
		public void Rent_RentedInstanceCanStart()
		{
			var instance = _pool.Rent(_graph, null);

			instance.Start();

			Assert.IsTrue(instance.IsRunning);
			Assert.AreEqual(1, PoolTestStateA.EnterCount);

			instance.Stop();
			_pool.Return(instance);
		}

		[Test]
		public void Return_InstanceIsPooled()
		{
			var instance = _pool.Rent(_graph, null);
			_pool.Return(instance);

			Assert.AreEqual(1, _pool.GetPooledCount(_graph));
		}

		[Test]
		public void Return_StopsRunningInstance()
		{
			var instance = _pool.Rent(_graph, null);
			instance.Start();
			Assert.IsTrue(instance.IsRunning);

			_pool.Return(instance);

			Assert.IsFalse(instance.IsRunning);
		}

		[Test]
		public void Return_CallsOnResetOnStates()
		{
			var instance = _pool.Rent(_graph, null);
			instance.Start();

			_pool.Return(instance);

			Assert.AreEqual(1, PoolTestStateA.ResetCount);
			Assert.AreEqual(1, PoolTestStateB.ResetCount);
		}

		[Test]
		public void Rent_ReusesPooledInstance()
		{
			var instance1 = _pool.Rent(_graph, null);
			var id1 = instance1.Graph;
			_pool.Return(instance1);

			var instance2 = _pool.Rent(_graph, null);

			Assert.AreSame(instance1, instance2);
			Assert.AreEqual(0, _pool.GetPooledCount(_graph));

			_pool.Return(instance2);
		}

		[Test]
		public void Rent_ReusedInstanceCanStartAgain()
		{
			PoolTestStateA.ResetCounters();
			PoolTestStateB.ResetCounters();

			var instance = _pool.Rent(_graph, null);
			instance.Start();
			instance.Stop();
			_pool.Return(instance);

			PoolTestStateA.ResetCounters();
			PoolTestStateB.ResetCounters();

			instance = _pool.Rent(_graph, null);
			instance.Start();

			Assert.IsTrue(instance.IsRunning);
			Assert.AreEqual(1, PoolTestStateA.EnterCount);

			instance.Stop();
			_pool.Return(instance);
		}

		[Test]
		public void Rent_ReusedInstanceGetsNewInstanceId()
		{
			var instance = _pool.Rent(_graph, null);
			var firstId = instance.InstanceId;
			_pool.Return(instance);

			instance = _pool.Rent(_graph, null);
			var secondId = instance.InstanceId;

			Assert.AreNotEqual(firstId, secondId);

			_pool.Return(instance);
		}

		[Test]
		public void Return_ClearsEventSubscribers()
		{
			var instance = _pool.Rent(_graph, null);
			var eventFired = false;
			instance.OnStateChanged += (_, _) => eventFired = true;

			_pool.Return(instance);
			instance = _pool.Rent(_graph, null);
			instance.Start();

			// The old subscriber should not fire
			Assert.IsFalse(eventFired);

			instance.Stop();
			_pool.Return(instance);
		}

		[Test]
		public void Prewarm_CreatesPooledInstances()
		{
			_pool.Prewarm(_graph, 5);

			Assert.AreEqual(5, _pool.GetPooledCount(_graph));
			Assert.AreEqual(5, _pool.GetTotalCreated(_graph));
		}

		[Test]
		public void Prewarm_NullGraph_ThrowsException()
		{
			Assert.Throws<ArgumentNullException>(() => _pool.Prewarm(null, 5));
		}

		[Test]
		public void Prewarm_ZeroCount_DoesNothing()
		{
			_pool.Prewarm(_graph, 0);

			Assert.AreEqual(0, _pool.GetPooledCount(_graph));
		}

		[Test]
		public void SetMaxPoolSize_LimitsPool()
		{
			_pool.SetMaxPoolSize(_graph, 2);

			var instances = new StateMachineInstance[3];
			for (var i = 0; i < 3; i++)
			{
				instances[i] = _pool.Rent(_graph, null);
			}

			for (var i = 0; i < 3; i++)
			{
				_pool.Return(instances[i]);
			}

			// Only 2 should be pooled, third discarded
			Assert.AreEqual(2, _pool.GetPooledCount(_graph));
		}

		[Test]
		public void Clear_RemovesPooledInstances()
		{
			_pool.Prewarm(_graph, 3);
			Assert.AreEqual(3, _pool.GetPooledCount(_graph));

			_pool.Clear(_graph);

			Assert.AreEqual(0, _pool.GetPooledCount(_graph));
		}

		[Test]
		public void ClearAll_RemovesAllPools()
		{
			var graph2 = ScriptableObject.CreateInstance<StateMachineGraph>();

			_pool.Prewarm(_graph, 3);
			_pool.Prewarm(graph2, 2);

			_pool.ClearAll();

			Assert.AreEqual(0, _pool.TotalPooled);

			UnityEngine.Object.DestroyImmediate(graph2);
		}

		[Test]
		public void TotalPooled_ReflectsAllPools()
		{
			var graph2 = ScriptableObject.CreateInstance<StateMachineGraph>();

			_pool.Prewarm(_graph, 3);
			_pool.Prewarm(graph2, 2);

			Assert.AreEqual(5, _pool.TotalPooled);

			_pool.ClearAll();
			UnityEngine.Object.DestroyImmediate(graph2);
		}

		[Test]
		public void OnRented_FiresWithCorrectArgs()
		{
			StateMachineInstance rentedInstance = null;
			var wasNew = false;

			_pool.OnRented += (instance, isNew) =>
			{
				rentedInstance = instance;
				wasNew = isNew;
			};

			var result = _pool.Rent(_graph, null);

			Assert.AreSame(result, rentedInstance);
			Assert.IsTrue(wasNew);

			_pool.Return(result);
		}

		[Test]
		public void OnRented_ReportsNotNewForPooledInstance()
		{
			var instance = _pool.Rent(_graph, null);
			_pool.Return(instance);

			var wasNew = true;
			_pool.OnRented += (_, isNew) => wasNew = isNew;

			_pool.Rent(_graph, null);

			Assert.IsFalse(wasNew);

			_pool.Return(instance);
		}

		[Test]
		public void OnReturned_Fires()
		{
			StateMachineInstance returnedInstance = null;
			_pool.OnReturned += instance => returnedInstance = instance;

			var rented = _pool.Rent(_graph, null);
			_pool.Return(rented);

			Assert.AreSame(rented, returnedInstance);
		}

		[Test]
		public void OnPoolGrew_FiresWhenPoolEmpty()
		{
			StateMachineGraph grewGraph = null;
			var grewSize = 0;

			_pool.OnPoolGrew += (graph, size) =>
			{
				grewGraph = graph;
				grewSize = size;
			};

			var instance = _pool.Rent(_graph, null);

			Assert.AreSame(_graph, grewGraph);
			Assert.AreEqual(1, grewSize);

			_pool.Return(instance);
		}

		[Test]
		public void Return_NullInstance_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => _pool.Return(null));
		}

		[Test]
		public void PrepareForReuse_ResetsBlackboardValues()
		{
			var definition = new BlackboardVariableDefinition
			{
				Key = "health",
				IsExposed = true
			};
			definition.ValueType = typeof(float);
			definition.DefaultValue = 100f;
			_graph.AddBlackboardVariable(definition);

			var instance = _pool.Rent(_graph, null);
			instance.Blackboard.Set("health", 50f);
			Assert.AreEqual(50f, instance.Blackboard.Get<float>("health"));

			_pool.Return(instance);
			instance = _pool.Rent(_graph, null);

			// After reuse, blackboard should be reset to default
			Assert.AreEqual(100f, instance.Blackboard.Get<float>("health"));

			_pool.Return(instance);
		}

		[Test]
		public void MultipleRentReturn_Cycles()
		{
			for (var cycle = 0; cycle < 5; cycle++)
			{
				var instance = _pool.Rent(_graph, null);
				instance.Start();
				Assert.IsTrue(instance.IsRunning);
				instance.Stop();
				_pool.Return(instance);
			}

			Assert.AreEqual(1, _pool.GetPooledCount(_graph));
			Assert.AreEqual(1, _pool.GetTotalCreated(_graph));
		}
	}
}
