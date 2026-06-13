using NUnit.Framework;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Editor.Debugging;
using Nonatomic.VSM.Editor.Runtime;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class BreakpointManagerEvaluateConditionTests
	{
		private StateMachineGraph _graph;
		private StateMachineInstance _instance;
		private RuntimeInstanceInfo _info;
		private GameObject _owner;

		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();

			var entry = new EntryNode { Id = "entry" };
			_graph.AddNode(entry);
			_graph.EntryNodeId = "entry";

			_instance = new StateMachineInstance(_graph);

			// Set blackboard values directly on the instance's blackboard
			_instance.Blackboard.Set("health", 100);
			_instance.Blackboard.Set("speed", 5.5f);
			_instance.Blackboard.Set("isAlive", true);
			_instance.Blackboard.Set("name", "Player");
			_instance.Blackboard.Set("score", 99.5);

			_owner = new GameObject("TestOwner");
			_info = new RuntimeInstanceInfo(_instance, _graph, _owner);
		}

		[TearDown]
		public void TearDown()
		{
			_instance?.Stop();
			_instance = null;

			if (_graph != null) Object.DestroyImmediate(_graph);
			if (_owner != null) Object.DestroyImmediate(_owner);
		}


		[Test]
		public void EvaluateCondition_NullCondition_ReturnsTrue()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition(null, _info));
		}

		[Test]
		public void EvaluateCondition_EmptyCondition_ReturnsTrue()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("", _info));
		}

		[Test]
		public void EvaluateCondition_WhitespaceCondition_ReturnsTrue()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("   ", _info));
		}

		[Test]
		public void EvaluateCondition_NullInfo_ReturnsFalse()
		{
			Assert.IsFalse(BreakpointManager.EvaluateCondition("health == 100", null));
		}



		[Test]
		public void EvaluateCondition_NegationOfTrue_ReturnsFalse()
		{
			Assert.IsFalse(BreakpointManager.EvaluateCondition("!isAlive", _info));
		}

		[Test]
		public void EvaluateCondition_NegationOfFalse_ReturnsTrue()
		{
			_instance.Blackboard.Set("isAlive", false);

			Assert.IsTrue(BreakpointManager.EvaluateCondition("!isAlive", _info));
		}

		[Test]
		public void EvaluateCondition_NegationOfNonexistentVariable_ReturnsTrue()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("!nonExistent", _info));
		}

		[Test]
		public void EvaluateCondition_NegationWithWhitespace_Works()
		{
			Assert.IsFalse(BreakpointManager.EvaluateCondition("! isAlive", _info));
		}



		[Test]
		public void EvaluateCondition_IntEquals_True()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("health == 100", _info));
		}

		[Test]
		public void EvaluateCondition_IntEquals_False()
		{
			Assert.IsFalse(BreakpointManager.EvaluateCondition("health == 50", _info));
		}

		[Test]
		public void EvaluateCondition_IntNotEquals_True()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("health != 50", _info));
		}

		[Test]
		public void EvaluateCondition_IntNotEquals_False()
		{
			Assert.IsFalse(BreakpointManager.EvaluateCondition("health != 100", _info));
		}

		[Test]
		public void EvaluateCondition_IntGreaterThan_True()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("health > 50", _info));
		}

		[Test]
		public void EvaluateCondition_IntGreaterThan_False()
		{
			Assert.IsFalse(BreakpointManager.EvaluateCondition("health > 100", _info));
		}

		[Test]
		public void EvaluateCondition_IntLessThan_True()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("health < 200", _info));
		}

		[Test]
		public void EvaluateCondition_IntLessThan_False()
		{
			Assert.IsFalse(BreakpointManager.EvaluateCondition("health < 100", _info));
		}

		[Test]
		public void EvaluateCondition_IntGreaterOrEqual_EqualValue()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("health >= 100", _info));
		}

		[Test]
		public void EvaluateCondition_IntLessOrEqual_EqualValue()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("health <= 100", _info));
		}



		[Test]
		public void EvaluateCondition_FloatEquals_True()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("speed == 5.5", _info));
		}

		[Test]
		public void EvaluateCondition_FloatNotEquals_True()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("speed != 3.0", _info));
		}

		[Test]
		public void EvaluateCondition_FloatGreaterThan_True()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("speed > 5.0", _info));
		}

		[Test]
		public void EvaluateCondition_FloatLessThan_True()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("speed < 6.0", _info));
		}



		[Test]
		public void EvaluateCondition_DoubleEquals_True()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("score == 99.5", _info));
		}

		[Test]
		public void EvaluateCondition_DoubleGreaterThan_True()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("score > 50.0", _info));
		}



		[Test]
		public void EvaluateCondition_BoolEqualsTrue_True()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("isAlive == True", _info));
		}

		[Test]
		public void EvaluateCondition_BoolEqualsFalse_False()
		{
			Assert.IsFalse(BreakpointManager.EvaluateCondition("isAlive == False", _info));
		}

		[Test]
		public void EvaluateCondition_BoolNotEqualsTrue_False()
		{
			Assert.IsFalse(BreakpointManager.EvaluateCondition("isAlive != True", _info));
		}

		[Test]
		public void EvaluateCondition_BoolGreaterThan_ReturnsFalse()
		{
			// Boolean comparisons only support == and !=
			Assert.IsFalse(BreakpointManager.EvaluateCondition("isAlive > True", _info));
		}



		[Test]
		public void EvaluateCondition_StringEquals_True()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("name == Player", _info));
		}

		[Test]
		public void EvaluateCondition_StringEquals_False()
		{
			Assert.IsFalse(BreakpointManager.EvaluateCondition("name == Enemy", _info));
		}

		[Test]
		public void EvaluateCondition_StringNotEquals_True()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("name != Enemy", _info));
		}

		[Test]
		public void EvaluateCondition_StringGreaterThan_ReturnsFalse()
		{
			// String comparisons only support == and !=
			Assert.IsFalse(BreakpointManager.EvaluateCondition("name > Player", _info));
		}



		[Test]
		public void EvaluateCondition_TruthyBoolTrue_ReturnsTrue()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("isAlive", _info));
		}

		[Test]
		public void EvaluateCondition_TruthyBoolFalse_ReturnsFalse()
		{
			_instance.Blackboard.Set("isAlive", false);

			Assert.IsFalse(BreakpointManager.EvaluateCondition("isAlive", _info));
		}

		[Test]
		public void EvaluateCondition_TruthyIntNonZero_ReturnsTrue()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("health", _info));
		}

		[Test]
		public void EvaluateCondition_TruthyIntZero_ReturnsFalse()
		{
			_instance.Blackboard.Set("health", 0);

			Assert.IsFalse(BreakpointManager.EvaluateCondition("health", _info));
		}

		[Test]
		public void EvaluateCondition_TruthyFloatNonZero_ReturnsTrue()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("speed", _info));
		}

		[Test]
		public void EvaluateCondition_TruthyFloatZero_ReturnsFalse()
		{
			_instance.Blackboard.Set("speed", 0f);

			Assert.IsFalse(BreakpointManager.EvaluateCondition("speed", _info));
		}

		[Test]
		public void EvaluateCondition_TruthyNonEmptyString_ReturnsTrue()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("name", _info));
		}

		[Test]
		public void EvaluateCondition_TruthyEmptyString_ReturnsFalse()
		{
			_instance.Blackboard.Set("name", "");

			Assert.IsFalse(BreakpointManager.EvaluateCondition("name", _info));
		}

		[Test]
		public void EvaluateCondition_TruthyNonexistentVariable_ReturnsFalse()
		{
			Assert.IsFalse(BreakpointManager.EvaluateCondition("nonExistent", _info));
		}



		[Test]
		public void EvaluateCondition_NonexistentVariable_Comparison_ReturnsFalse()
		{
			Assert.IsFalse(BreakpointManager.EvaluateCondition("nonExistent == 42", _info));
		}

		[Test]
		public void EvaluateCondition_ConditionWithExtraWhitespace_Works()
		{
			Assert.IsTrue(BreakpointManager.EvaluateCondition("  health  ==  100  ", _info));
		}

		[Test]
		public void EvaluateCondition_NullBoxedValue_EqualsNull_ReturnsTrue()
		{
			_instance.Blackboard.Set<string>("name", null);

			Assert.IsTrue(BreakpointManager.EvaluateCondition("name == null", _info));
		}

		[Test]
		public void EvaluateCondition_NullBoxedValue_EqualsEmpty_ReturnsTrue()
		{
			_instance.Blackboard.Set<string>("name", null);

			Assert.IsTrue(BreakpointManager.EvaluateCondition("name ==", _info));
		}

		[Test]
		public void EvaluateCondition_UnknownOperator_FallsThrough()
		{
			// A condition with no valid operator is treated as a truthy check
			Assert.IsTrue(BreakpointManager.EvaluateCondition("health", _info));
		}

	}

	[TestFixture]
	public class BreakpointManagerShouldBreakTests
	{
		private StateMachineGraph _graph;
		private StateMachineInstance _instance;
		private RuntimeInstanceInfo _info;
		private GameObject _owner;

		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();

			var entry = new EntryNode { Id = "entry" };
			_graph.AddNode(entry);
			_graph.EntryNodeId = "entry";

			_instance = new StateMachineInstance(_graph);
			_owner = new GameObject("TestOwner");
			_info = new RuntimeInstanceInfo(_instance, _graph, _owner);
		}

		[TearDown]
		public void TearDown()
		{
			_instance?.Stop();
			_instance = null;

			if (_graph != null) Object.DestroyImmediate(_graph);
			if (_owner != null) Object.DestroyImmediate(_owner);
		}

		[Test]
		public void EvaluateCondition_ConditionalWithEmptyCondition_ReturnsTrue()
		{
			// Empty condition is always true
			Assert.IsTrue(BreakpointManager.EvaluateCondition("", _info));
		}

		[Test]
		public void EvaluateCondition_ConditionalWithNullBlackboard_ReturnsFalse()
		{
			// Instance with no blackboard setup - use null info
			Assert.IsFalse(BreakpointManager.EvaluateCondition("health == 100", null));
		}
	}

	[TestFixture]
	public class BreakpointManagerGlobalEnabledTests
	{
		private bool _originalEnabled;

		[SetUp]
		public void SetUp()
		{
			_originalEnabled = BreakpointManager.GlobalEnabled;
		}

		[TearDown]
		public void TearDown()
		{
			BreakpointManager.GlobalEnabled = _originalEnabled;
		}

		[Test]
		public void GlobalEnabled_SetTrue_FiresEvent()
		{
			BreakpointManager.GlobalEnabled = false;
			var fired = false;
			void Handler(bool value) => fired = true;
			BreakpointManager.OnGlobalEnabledChanged += Handler;

			BreakpointManager.GlobalEnabled = true;

			Assert.IsTrue(fired);
			BreakpointManager.OnGlobalEnabledChanged -= Handler;
		}

		[Test]
		public void GlobalEnabled_SetSameValue_DoesNotFireEvent()
		{
			var currentValue = BreakpointManager.GlobalEnabled;
			var fired = false;
			void Handler(bool value) => fired = true;
			BreakpointManager.OnGlobalEnabledChanged += Handler;

			BreakpointManager.GlobalEnabled = currentValue;

			Assert.IsFalse(fired);
			BreakpointManager.OnGlobalEnabledChanged -= Handler;
		}

		[Test]
		public void GlobalEnabled_EventPassesNewValue()
		{
			BreakpointManager.GlobalEnabled = true;
			var receivedValue = true;
			void Handler(bool value) => receivedValue = value;
			BreakpointManager.OnGlobalEnabledChanged += Handler;

			BreakpointManager.GlobalEnabled = false;

			Assert.IsFalse(receivedValue);
			BreakpointManager.OnGlobalEnabledChanged -= Handler;
		}
	}
}
