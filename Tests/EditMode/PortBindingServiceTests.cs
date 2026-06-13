using System;
using System.Collections.Generic;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Binding;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class PortBindingServiceTests
	{
		private PortBindingService _bindingService;
		private Blackboard.Blackboard _blackboard;
		private StateMachineGraph _graph;


		private class InputState : State
		{
			[DataIn("Speed")] public float speed;

			[DataIn("Target")] public Vector3 target;

			[DataIn] public string name;
		}

		private class OutputState : State
		{
			[DataOut("Result")] public float result = 42f;

			[DataOut("Position")] public Vector3 position = new Vector3(1, 2, 3);

			[DataOut] public string message = "Hello";
		}

		private class TransitionState : State
		{
			[Transition("On Complete")] public Action onComplete;

			[Transition("On Fail")] public Action onFail;
		}

		private class MixedState : State
		{
			[DataIn("InputValue")] public int inputValue;

			[DataOut("OutputValue")] public int outputValue;

			[Transition("On Done")] public Action onDone;

			public override void OnEnter()
			{
				outputValue = inputValue * 2;
			}
		}


		[SetUp]
		public void SetUp()
		{
			_blackboard = new Blackboard.Blackboard();
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			var stateInstances = new Dictionary<string, State>();
			_bindingService = new PortBindingService(_graph, _blackboard, stateInstances);
		}

		[TearDown]
		public void TearDown()
		{
			if (_graph != null)
			{
				UnityEngine.Object.DestroyImmediate(_graph);
			}

			_bindingService = null;
			_blackboard = null;
		}


		[Test]
		public void BindInputs_NullState_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => _bindingService.BindInputs(null));
		}

		[Test]
		public void BindInputs_StateWithoutNodeId_DoesNotThrow()
		{
			var state = new InputState();
			Assert.DoesNotThrow(() => _bindingService.BindInputs(state));
		}

		[Test]
		public void BindInputs_StateWithoutNodeId_LeavesFieldsUnchanged()
		{
			var state = new InputState();
			state.speed = 5f;
			_blackboard.Set("speed", 10f);

			_bindingService.BindInputs(state);

			// Without a NodeId and graph connections, binding doesn't occur
			Assert.AreEqual(5f, state.speed);
		}



		[Test]
		public void BindOutputs_NullState_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => _bindingService.BindOutputs(null));
		}

		[Test]
		public void BindOutputs_StateWithoutNodeId_DoesNotThrow()
		{
			var state = new OutputState();
			Assert.DoesNotThrow(() => _bindingService.BindOutputs(state));
		}

		[Test]
		public void BindOutputs_StateWithoutNodeId_DoesNotWriteToBlackboard()
		{
			var state = new OutputState();

			_bindingService.BindOutputs(state);

			// Without a NodeId and graph connections, output doesn't write to blackboard
			Assert.IsFalse(_blackboard.HasKey("result"));
		}



		[Test]
		public void BindTransitions_NullState_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => _bindingService.BindTransitions(null, _ => { }));
		}

		[Test]
		public void BindTransitions_StateWithoutNodeId_DoesNotThrow()
		{
			var state = new TransitionState();
			Assert.DoesNotThrow(() => _bindingService.BindTransitions(state, _ => { }));
		}

		[Test]
		public void BindTransitions_NullCallback_DoesNotThrow()
		{
			var state = new TransitionState();
			Assert.DoesNotThrow(() => _bindingService.BindTransitions(state, null));
		}

		[Test]
		public void BindTransitions_StateWithoutNodeId_LeavesTransitionsNull()
		{
			var state = new TransitionState();

			_bindingService.BindTransitions(state, _ => { });

			// Without a NodeId and graph connections, transitions aren't bound
			Assert.IsNull(state.onComplete);
			Assert.IsNull(state.onFail);
		}



		private class TypeConversionState : State
		{
			[DataIn] public int intValue;
			[DataIn] public float floatValue;
			[DataIn] public double doubleValue;
			[DataIn] public string stringValue;
			[DataIn] public long longValue;
		}

		private class SourceFloatState : State
		{
			[DataOut] public float outputFloat = 42.5f;
		}

		private class SourceIntState : State
		{
			[DataOut] public int outputInt = 100;
		}

		private class SourceStringState : State
		{
			[DataOut] public string outputString = "123";
		}

		[Test]
		public void BindInputs_CompatibleTypeConversion_FloatToInt_ConvertsValue()
		{
			// Arrange
			var sourceState = new SourceFloatState { outputFloat = 42.7f };
			sourceState.NodeId = "source-node";
			var targetState = new TypeConversionState();
			targetState.NodeId = "target-node";

			var stateInstances = new Dictionary<string, State>
			{
				{ "source-node", sourceState },
				{ "target-node", targetState }
			};

			var sourceNode = new StateNode(typeof(SourceFloatState)) { Id = "source-node" };
			var targetNode = new StateNode(typeof(TypeConversionState)) { Id = "target-node" };
			_graph.AddNode(sourceNode);
			_graph.AddNode(targetNode);

			var connection = new Connection(
				"source-node", "outputFloat",
				"target-node", "intValue",
				ConnectionType.Data);
			_graph.AddConnection(connection);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindInputs(targetState);

			// Assert - float 42.7 should convert to int 42
			Assert.AreEqual(42, targetState.intValue);
		}

		[Test]
		public void BindInputs_CompatibleTypeConversion_IntToFloat_ConvertsValue()
		{
			// Arrange
			var sourceState = new SourceIntState { outputInt = 100 };
			sourceState.NodeId = "source-node";
			var targetState = new TypeConversionState();
			targetState.NodeId = "target-node";

			var stateInstances = new Dictionary<string, State>
			{
				{ "source-node", sourceState },
				{ "target-node", targetState }
			};

			var sourceNode = new StateNode(typeof(SourceIntState)) { Id = "source-node" };
			var targetNode = new StateNode(typeof(TypeConversionState)) { Id = "target-node" };
			_graph.AddNode(sourceNode);
			_graph.AddNode(targetNode);

			var connection = new Connection(
				"source-node", "outputInt",
				"target-node", "floatValue",
				ConnectionType.Data);
			_graph.AddConnection(connection);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindInputs(targetState);

			// Assert
			Assert.AreEqual(100f, targetState.floatValue);
		}

		[Test]
		public void BindInputs_IncompatibleTypeConversion_LogsErrorAndDoesNotThrow()
		{
			// Arrange - string "hello" cannot convert to int
			var sourceState = new SourceStringState { outputString = "not-a-number" };
			sourceState.NodeId = "source-node";
			var targetState = new TypeConversionState();
			targetState.intValue = 999; // Set a known value
			targetState.NodeId = "target-node";

			var stateInstances = new Dictionary<string, State>
			{
				{ "source-node", sourceState },
				{ "target-node", targetState }
			};

			var sourceNode = new StateNode(typeof(SourceStringState)) { Id = "source-node" };
			var targetNode = new StateNode(typeof(TypeConversionState)) { Id = "target-node" };
			_graph.AddNode(sourceNode);
			_graph.AddNode(targetNode);

			var connection = new Connection(
				"source-node", "outputString",
				"target-node", "intValue",
				ConnectionType.Data);
			_graph.AddConnection(connection);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act & Assert - should not throw, should log error
			Assert.DoesNotThrow(() => service.BindInputs(targetState));

			// Original value should be unchanged due to conversion failure
			Assert.AreEqual(999, targetState.intValue);
		}



		[Test]
		public void BindInputs_NonExistentTargetField_LogsWarningAndDoesNotThrow()
		{
			// Arrange
			var sourceState = new SourceIntState { outputInt = 42 };
			sourceState.NodeId = "source-node";
			var targetState = new InputState();
			targetState.NodeId = "target-node";

			var stateInstances = new Dictionary<string, State>
			{
				{ "source-node", sourceState },
				{ "target-node", targetState }
			};

			var sourceNode = new StateNode(typeof(SourceIntState)) { Id = "source-node" };
			var targetNode = new StateNode(typeof(InputState)) { Id = "target-node" };
			_graph.AddNode(sourceNode);
			_graph.AddNode(targetNode);

			// Connection to field that doesn't exist on InputState
			var connection = new Connection(
				"source-node", "outputInt",
				"target-node", "nonExistentField",
				ConnectionType.Data);
			_graph.AddConnection(connection);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act & Assert - should not throw
			Assert.DoesNotThrow(() => service.BindInputs(targetState));
		}

		[Test]
		public void BindOutputs_NonExistentSourceField_DoesNotThrow()
		{
			// Arrange
			var state = new OutputState();
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(OutputState)) { Id = "state-node" };
			var varNode = new BlackboardVariableNode("testVar", typeof(float), BlackboardNodeMode.Set)
			{
				Id = "var-node"
			};
			_graph.AddNode(stateNode);
			_graph.AddNode(varNode);

			// Connection from field that doesn't exist
			var connection = new Connection(
				"state-node", "nonExistentOutput",
				"var-node", "value",
				ConnectionType.BlackboardWrite);
			_graph.AddConnection(connection);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act & Assert - should not throw
			Assert.DoesNotThrow(() => service.BindOutputs(state));
		}



		private class NullableOutputState : State
		{
			[DataOut] public string nullableOutput = null;
		}

		[Test]
		public void BindInputs_SourceFieldIsNull_DoesNotSetTargetField()
		{
			// Arrange
			var sourceState = new NullableOutputState { nullableOutput = null };
			sourceState.NodeId = "source-node";
			var targetState = new InputState { name = "original" };
			targetState.NodeId = "target-node";

			var stateInstances = new Dictionary<string, State>
			{
				{ "source-node", sourceState },
				{ "target-node", targetState }
			};

			var sourceNode = new StateNode(typeof(NullableOutputState)) { Id = "source-node" };
			var targetNode = new StateNode(typeof(InputState)) { Id = "target-node" };
			_graph.AddNode(sourceNode);
			_graph.AddNode(targetNode);

			var connection = new Connection(
				"source-node", "nullableOutput",
				"target-node", "name",
				ConnectionType.Data);
			_graph.AddConnection(connection);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindInputs(targetState);

			// Assert - original value should be preserved when source is null
			Assert.AreEqual("original", targetState.name);
		}

		[Test]
		public void BindInputs_BlackboardVariableIsNull_DoesNotSetTargetField()
		{
			// Arrange
			_blackboard.Set<string>("nullVar", null);

			var targetState = new InputState { name = "default-name" };
			targetState.NodeId = "target-node";

			var stateInstances = new Dictionary<string, State> { { "target-node", targetState } };

			var targetNode = new StateNode(typeof(InputState)) { Id = "target-node" };
			var varNode = new BlackboardVariableNode("nullVar", typeof(string), BlackboardNodeMode.Get)
			{
				Id = "var-node"
			};
			_graph.AddNode(targetNode);
			_graph.AddNode(varNode);

			var connection = new Connection(
				"var-node", "value",
				"target-node", "name",
				ConnectionType.BlackboardRead);
			_graph.AddConnection(connection);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindInputs(targetState);

			// Assert - original value should be preserved
			Assert.AreEqual("default-name", targetState.name);
		}



		[Test]
		public void BindInputs_BlackboardVariableUndefined_DoesNotSetField()
		{
			// Arrange - no variable set in blackboard
			var targetState = new InputState { speed = 99f };
			targetState.NodeId = "target-node";

			var stateInstances = new Dictionary<string, State> { { "target-node", targetState } };

			var targetNode = new StateNode(typeof(InputState)) { Id = "target-node" };
			var varNode = new BlackboardVariableNode("undefinedVar", typeof(float), BlackboardNodeMode.Get)
			{
				Id = "var-node"
			};
			_graph.AddNode(targetNode);
			_graph.AddNode(varNode);

			var connection = new Connection(
				"var-node", "value",
				"target-node", "speed",
				ConnectionType.BlackboardRead);
			_graph.AddConnection(connection);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindInputs(targetState);

			// Assert - original value preserved when blackboard variable doesn't exist
			Assert.AreEqual(99f, targetState.speed);
		}

		[Test]
		public void BindInputs_BlackboardVariableDefined_SetsFieldValue()
		{
			// Arrange
			_blackboard.Set("playerSpeed", 25.5f);

			var targetState = new InputState();
			targetState.NodeId = "target-node";

			var stateInstances = new Dictionary<string, State> { { "target-node", targetState } };

			var targetNode = new StateNode(typeof(InputState)) { Id = "target-node" };
			var varNode = new BlackboardVariableNode("playerSpeed", typeof(float), BlackboardNodeMode.Get)
			{
				Id = "var-node"
			};
			_graph.AddNode(targetNode);
			_graph.AddNode(varNode);

			var connection = new Connection(
				"var-node", "value",
				"target-node", "speed",
				ConnectionType.BlackboardRead);
			_graph.AddConnection(connection);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindInputs(targetState);

			// Assert
			Assert.AreEqual(25.5f, targetState.speed);
		}



		private class MultiTransitionState : State
		{
			[Transition("Complete")] public Action onComplete;
			[Transition] public Action onFinish;
			[Transition("Done")] public Action _onDone;
		}

		[Test]
		public void BindTransitions_MatchesByFieldName_BindsCorrectTransition()
		{
			// Arrange
			var state = new MultiTransitionState();
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(MultiTransitionState)) { Id = "state-node" };
			var targetNode = new StateNode(typeof(InputState)) { Id = "target-node" };
			_graph.AddNode(stateNode);
			_graph.AddNode(targetNode);

			// Use field name directly
			var connection = new Connection(
				"state-node", "onComplete",
				"target-node", "entry",
				ConnectionType.Transition);
			_graph.AddConnection(connection);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);
			string transitionedTo = null;

			// Act
			service.BindTransitions(state, nodeId => transitionedTo = nodeId);

			// Assert
			Assert.IsNotNull(state.onComplete);
			state.onComplete.Invoke();
			Assert.AreEqual("target-node", transitionedTo);
		}

		[Test]
		public void BindTransitions_MatchesByDisplayName_FallsBackToDisplayNameMatch()
		{
			// Arrange
			var state = new MultiTransitionState();
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(MultiTransitionState)) { Id = "state-node" };
			var targetNode = new StateNode(typeof(InputState)) { Id = "target-node" };
			_graph.AddNode(stateNode);
			_graph.AddNode(targetNode);

			// Use display name (attribute value "Done" for field _onDone)
			var connection = new Connection(
				"state-node", "Done",
				"target-node", "entry",
				ConnectionType.Transition);
			_graph.AddConnection(connection);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);
			string transitionedTo = null;

			// Act
			service.BindTransitions(state, nodeId => transitionedTo = nodeId);

			// Assert
			Assert.IsNotNull(state._onDone);
			state._onDone.Invoke();
			Assert.AreEqual("target-node", transitionedTo);
		}

		[Test]
		public void BindTransitions_NoMatchingField_LeavesTransitionNull()
		{
			// Arrange
			var state = new TransitionState();
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(TransitionState)) { Id = "state-node" };
			var targetNode = new StateNode(typeof(InputState)) { Id = "target-node" };
			_graph.AddNode(stateNode);
			_graph.AddNode(targetNode);

			// Connection with non-matching port name
			var connection = new Connection(
				"state-node", "nonExistentTransition",
				"target-node", "entry",
				ConnectionType.Transition);
			_graph.AddConnection(connection);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindTransitions(state, _ => { });

			// Assert - transitions should remain null
			Assert.IsNull(state.onComplete);
			Assert.IsNull(state.onFail);
		}

		[Test]
		public void BindTransitions_WithTransitionTime_BindsTransition()
		{
			// Arrange
			var state = new TransitionState();
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(TransitionState)) { Id = "state-node" };
			var targetNode = new StateNode(typeof(InputState)) { Id = "target-node" };
			_graph.AddNode(stateNode);
			_graph.AddNode(targetNode);

			var connection = new Connection(
				"state-node", "onComplete",
				"target-node", "entry",
				ConnectionType.Transition)
			{
				TransitionTime = 1.5f
			};
			_graph.AddConnection(connection);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);
			string transitionedTo = null;

			// Act
			service.BindTransitions(state, nodeId => transitionedTo = nodeId);

			// Assert - transition should be bound even with transition time
			Assert.IsNotNull(state.onComplete);
			state.onComplete.Invoke();
			Assert.AreEqual("target-node", transitionedTo);
		}



		[Test]
		public void BindInputs_PropertyOverrideForUnconnectedPort_AppliesOverride()
		{
			// Arrange
			var state = new InputState();
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(InputState)) { Id = "state-node" };
			stateNode.SetPropertyOverride("speed", 77.7f, typeof(float));
			_graph.AddNode(stateNode);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindInputs(state);

			// Assert
			Assert.AreEqual(77.7f, state.speed, 0.001f);
		}

		[Test]
		public void BindInputs_PropertyOverrideForConnectedPort_IgnoresOverride()
		{
			// Arrange
			_blackboard.Set("connectedSpeed", 50f);

			var state = new InputState();
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(InputState)) { Id = "state-node" };
			stateNode.SetPropertyOverride("speed", 99.9f, typeof(float)); // This should be ignored
			_graph.AddNode(stateNode);

			var varNode = new BlackboardVariableNode("connectedSpeed", typeof(float), BlackboardNodeMode.Get)
			{
				Id = "var-node"
			};
			_graph.AddNode(varNode);

			// Connect to the same field that has an override
			var connection = new Connection(
				"var-node", "value",
				"state-node", "speed",
				ConnectionType.BlackboardRead);
			_graph.AddConnection(connection);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindInputs(state);

			// Assert - connection value takes precedence over override
			Assert.AreEqual(50f, state.speed);
		}

		[Test]
		public void BindInputs_PropertyOverrideWithNullValue_DoesNotApply()
		{
			// Arrange
			var state = new InputState { name = "original" };
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(InputState)) { Id = "state-node" };
			stateNode.SetPropertyOverride("name", null, typeof(string));
			_graph.AddNode(stateNode);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindInputs(state);

			// Assert - null override should not change original value
			Assert.AreEqual("original", state.name);
		}

		[Test]
		public void BindInputs_MultiplePropertyOverrides_AppliesAllUnconnected()
		{
			// Arrange
			var state = new InputState();
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(InputState)) { Id = "state-node" };
			stateNode.SetPropertyOverride("speed", 10f, typeof(float));
			stateNode.SetPropertyOverride("target", new Vector3(1, 2, 3), typeof(Vector3));
			stateNode.SetPropertyOverride("name", "TestName", typeof(string));
			_graph.AddNode(stateNode);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindInputs(state);

			// Assert - all overrides should be applied
			Assert.AreEqual(10f, state.speed);
			Assert.AreEqual(new Vector3(1, 2, 3), state.target);
			Assert.AreEqual("TestName", state.name);
		}



		private class CacheTestState1 : State
		{
			[DataIn] public int value1;
		}

		private class CacheTestState2 : State
		{
			[DataIn] public int value1;
		}

		[Test]
		public void BindInputs_MultipleStatesWithSameFieldName_UsesSeparateCacheEntries()
		{
			// Arrange - create two different state types with same field name
			var sourceState1 = new SourceIntState { outputInt = 10 };
			sourceState1.NodeId = "source1";
			var sourceState2 = new SourceIntState { outputInt = 20 };
			sourceState2.NodeId = "source2";
			var targetState1 = new CacheTestState1();
			targetState1.NodeId = "target1";
			var targetState2 = new CacheTestState2();
			targetState2.NodeId = "target2";

			var stateInstances = new Dictionary<string, State>
			{
				{ "source1", sourceState1 },
				{ "source2", sourceState2 },
				{ "target1", targetState1 },
				{ "target2", targetState2 }
			};

			_graph.AddNode(new StateNode(typeof(SourceIntState)) { Id = "source1" });
			_graph.AddNode(new StateNode(typeof(SourceIntState)) { Id = "source2" });
			_graph.AddNode(new StateNode(typeof(CacheTestState1)) { Id = "target1" });
			_graph.AddNode(new StateNode(typeof(CacheTestState2)) { Id = "target2" });

			_graph.AddConnection(new Connection(
				"source1", "outputInt",
				"target1", "value1",
				ConnectionType.Data));
			_graph.AddConnection(new Connection(
				"source2", "outputInt",
				"target2", "value1",
				ConnectionType.Data));

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindInputs(targetState1);
			service.BindInputs(targetState2);

			// Assert - both states should have correct values
			Assert.AreEqual(10, targetState1.value1);
			Assert.AreEqual(20, targetState2.value1);
		}

		[Test]
		public void BindInputs_SameStateMultipleTimes_UsesCachedField()
		{
			// Arrange
			_blackboard.Set("speed1", 10f);

			var state = new InputState();
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(InputState)) { Id = "state-node" };
			var varNode = new BlackboardVariableNode("speed1", typeof(float), BlackboardNodeMode.Get)
			{
				Id = "var-node"
			};
			_graph.AddNode(stateNode);
			_graph.AddNode(varNode);

			_graph.AddConnection(new Connection(
				"var-node", "value",
				"state-node", "speed",
				ConnectionType.BlackboardRead));

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act - bind multiple times (should use cache)
			service.BindInputs(state);
			Assert.AreEqual(10f, state.speed);

			_blackboard.Set("speed1", 20f);
			service.BindInputs(state);
			Assert.AreEqual(20f, state.speed);

			_blackboard.Set("speed1", 30f);
			service.BindInputs(state);
			Assert.AreEqual(30f, state.speed);
		}



		[Test]
		public void BindOutputs_WritesToBlackboardVariable()
		{
			// Arrange
			var state = new OutputState { result = 123.456f };
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(OutputState)) { Id = "state-node" };
			var varNode = new BlackboardVariableNode("outputResult", typeof(float), BlackboardNodeMode.Set)
			{
				Id = "var-node"
			};
			_graph.AddNode(stateNode);
			_graph.AddNode(varNode);

			_graph.AddConnection(new Connection(
				"state-node", "result",
				"var-node", "value",
				ConnectionType.BlackboardWrite));

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindOutputs(state);

			// Assert
			Assert.IsTrue(_blackboard.HasKey("outputResult"));
			Assert.AreEqual(123.456f, _blackboard.Get<float>("outputResult"), 0.001f);
		}

		[Test]
		public void BindOutputs_MissingBlackboardVariableNode_DoesNotThrow()
		{
			// Arrange
			var state = new OutputState();
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(OutputState)) { Id = "state-node" };
			_graph.AddNode(stateNode);

			// Connection to non-existent node
			_graph.AddConnection(new Connection(
				"state-node", "result",
				"nonexistent-var-node", "value",
				ConnectionType.BlackboardWrite));

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act & Assert
			Assert.DoesNotThrow(() => service.BindOutputs(state));
		}



		[Test]
		public void BindInputs_DataConnection_MissingSourceState_DoesNotSetField()
		{
			// Arrange
			var targetState = new InputState { speed = 42f };
			targetState.NodeId = "target-node";

			// stateInstances does NOT contain source state
			var stateInstances = new Dictionary<string, State>
			{
				{ "target-node", targetState }
			};

			_graph.AddNode(new StateNode(typeof(SourceFloatState)) { Id = "source-node" });
			_graph.AddNode(new StateNode(typeof(InputState)) { Id = "target-node" });

			_graph.AddConnection(new Connection(
				"source-node", "outputFloat",
				"target-node", "speed",
				ConnectionType.Data));

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindInputs(targetState);

			// Assert - original value preserved
			Assert.AreEqual(42f, targetState.speed);
		}

		[Test]
		public void BindInputs_DataConnection_MissingSourceField_DoesNotSetField()
		{
			// Arrange
			var sourceState = new SourceIntState();
			sourceState.NodeId = "source-node";
			var targetState = new InputState { speed = 42f };
			targetState.NodeId = "target-node";

			var stateInstances = new Dictionary<string, State>
			{
				{ "source-node", sourceState },
				{ "target-node", targetState }
			};

			_graph.AddNode(new StateNode(typeof(SourceIntState)) { Id = "source-node" });
			_graph.AddNode(new StateNode(typeof(InputState)) { Id = "target-node" });

			// Connection with non-existent source field
			_graph.AddConnection(new Connection(
				"source-node", "nonExistentField",
				"target-node", "speed",
				ConnectionType.Data));

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindInputs(targetState);

			// Assert - original value preserved
			Assert.AreEqual(42f, targetState.speed);
		}



		[Test]
		public void Constructor_NullGraph_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() =>
				new PortBindingService(null, _blackboard, new Dictionary<string, State>()));
		}

		[Test]
		public void Constructor_NullBlackboard_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() =>
				new PortBindingService(_graph, null, new Dictionary<string, State>()));
		}

		[Test]
		public void Constructor_NullStateInstances_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() =>
				new PortBindingService(_graph, _blackboard, null));
		}



		private class NonConvertibleOutputState : State
		{
			[DataOut] public Vector3 vector = new Vector3(1, 2, 3);
		}

		private class BoolInputState : State
		{
			[DataIn] public bool flag;
		}

		[Test]
		public void BindInputs_NonConvertibleTypes_DoesNotThrowAndPreservesOriginal()
		{
			// Vector3 cannot be Convert.ChangeType'd to bool
			var sourceState = new NonConvertibleOutputState();
			sourceState.NodeId = "source-node";
			var targetState = new BoolInputState { flag = true };
			targetState.NodeId = "target-node";

			var stateInstances = new Dictionary<string, State>
			{
				{ "source-node", sourceState },
				{ "target-node", targetState }
			};

			_graph.AddNode(new StateNode(typeof(NonConvertibleOutputState)) { Id = "source-node" });
			_graph.AddNode(new StateNode(typeof(BoolInputState)) { Id = "target-node" });

			_graph.AddConnection(new Connection(
				"source-node", "vector",
				"target-node", "flag",
				ConnectionType.Data));

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Should log error but not throw
			Assert.DoesNotThrow(() => service.BindInputs(targetState));
		}

		[Test]
		public void BindTransitions_NullCallbackWithConnections_DoesNotThrowOnBind()
		{
			// BindTransitions with null callback should not throw during binding
			var state = new TransitionState();
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(TransitionState)) { Id = "state-node" };
			var targetNode = new StateNode(typeof(InputState)) { Id = "target-node" };
			_graph.AddNode(stateNode);
			_graph.AddNode(targetNode);

			var connection = new Connection(
				"state-node", "onComplete",
				"target-node", "entry",
				ConnectionType.Transition);
			_graph.AddConnection(connection);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Binding with null callback should not throw
			Assert.DoesNotThrow(() => service.BindTransitions(state, null));

			// The transition field should be bound (even though callback is null)
			Assert.IsNotNull(state.onComplete);
		}

		[Test]
		public void BindTransitions_NullCallbackWithConnections_ThrowsOnInvoke()
		{
			// When bound with null callback, invoking the transition should throw NullReferenceException
			var state = new TransitionState();
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(TransitionState)) { Id = "state-node" };
			var targetNode = new StateNode(typeof(InputState)) { Id = "target-node" };
			_graph.AddNode(stateNode);
			_graph.AddNode(targetNode);

			var connection = new Connection(
				"state-node", "onComplete",
				"target-node", "entry",
				ConnectionType.Transition);
			_graph.AddConnection(connection);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);
			service.BindTransitions(state, null);

			// Invoking should throw since the callback closure captures null
			Assert.Throws<NullReferenceException>(() => state.onComplete.Invoke());
		}

		[Test]
		public void BindTransitions_MultipleConnectionsFromSameNode_BindsAll()
		{
			var state = new TransitionState();
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(TransitionState)) { Id = "state-node" };
			var target1 = new StateNode(typeof(InputState)) { Id = "target-1" };
			var target2 = new StateNode(typeof(InputState)) { Id = "target-2" };
			_graph.AddNode(stateNode);
			_graph.AddNode(target1);
			_graph.AddNode(target2);

			_graph.AddConnection(new Connection(
				"state-node", "onComplete",
				"target-1", "entry",
				ConnectionType.Transition));
			_graph.AddConnection(new Connection(
				"state-node", "onFail",
				"target-2", "entry",
				ConnectionType.Transition));

			var service = new PortBindingService(_graph, _blackboard, stateInstances);
			var transitions = new List<string>();

			service.BindTransitions(state, nodeId => transitions.Add(nodeId));

			Assert.IsNotNull(state.onComplete);
			Assert.IsNotNull(state.onFail);

			state.onComplete.Invoke();
			state.onFail.Invoke();

			Assert.AreEqual(2, transitions.Count);
			Assert.AreEqual("target-1", transitions[0]);
			Assert.AreEqual("target-2", transitions[1]);
		}

		[Test]
		public void BindOutputs_MultipleBlackboardWrites_WritesAll()
		{
			var state = new OutputState { result = 99.9f, message = "world" };
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			_graph.AddNode(new StateNode(typeof(OutputState)) { Id = "state-node" });
			_graph.AddNode(new BlackboardVariableNode("resultVar", typeof(float), BlackboardNodeMode.Set) { Id = "var1" });
			_graph.AddNode(new BlackboardVariableNode("messageVar", typeof(string), BlackboardNodeMode.Set) { Id = "var2" });

			_graph.AddConnection(new Connection(
				"state-node", "result",
				"var1", "value",
				ConnectionType.BlackboardWrite));
			_graph.AddConnection(new Connection(
				"state-node", "message",
				"var2", "value",
				ConnectionType.BlackboardWrite));

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			service.BindOutputs(state);

			Assert.AreEqual(99.9f, _blackboard.Get<float>("resultVar"), 0.001f);
			Assert.AreEqual("world", _blackboard.Get<string>("messageVar"));
		}

		[Test]
		public void BindInputs_EmptyNodeId_DoesNotBind()
		{
			var state = new InputState { speed = 5f };
			state.NodeId = "";

			_bindingService.BindInputs(state);

			Assert.AreEqual(5f, state.speed);
		}

		[Test]
		public void BindOutputs_EmptyNodeId_DoesNotWrite()
		{
			var state = new OutputState { result = 42f };
			state.NodeId = "";

			_bindingService.BindOutputs(state);

			Assert.IsFalse(_blackboard.HasKey("result"));
		}

		[Test]
		public void BindTransitions_EmptyNodeId_DoesNotBind()
		{
			var state = new TransitionState();
			state.NodeId = "";

			_bindingService.BindTransitions(state, _ => { });

			Assert.IsNull(state.onComplete);
			Assert.IsNull(state.onFail);
		}

		[Test]
		public void BindInputs_PropertyOverrideNonExistentField_DoesNotThrow()
		{
			var state = new InputState();
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(InputState)) { Id = "state-node" };
			stateNode.SetPropertyOverride("nonExistentField", 42, typeof(int));
			_graph.AddNode(stateNode);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			Assert.DoesNotThrow(() => service.BindInputs(state));
		}

		[Test]
		public void BindInputs_BlackboardReadWithMissingSourceNode_DoesNotThrow()
		{
			var targetState = new InputState { speed = 77f };
			targetState.NodeId = "target-node";

			var stateInstances = new Dictionary<string, State> { { "target-node", targetState } };

			_graph.AddNode(new StateNode(typeof(InputState)) { Id = "target-node" });

			// Connection from a non-existent variable node
			_graph.AddConnection(new Connection(
				"missing-var-node", "value",
				"target-node", "speed",
				ConnectionType.BlackboardRead));

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			Assert.DoesNotThrow(() => service.BindInputs(targetState));
			Assert.AreEqual(77f, targetState.speed, "Original value should be preserved");
		}

		private class UnattributedState : State
		{
			public float speed;
			public string name;
			public Action onDone;
		}

		[Test]
		public void BindInputs_FieldWithoutDataInAttribute_DoesNotBind()
		{
			// Arrange - UnattributedState.speed has no [DataIn] attribute
			_blackboard.Set("attackSpeed", 999f);

			var targetState = new UnattributedState { speed = 5f };
			targetState.NodeId = "target-node";

			var stateInstances = new Dictionary<string, State> { { "target-node", targetState } };

			var targetNode = new StateNode(typeof(UnattributedState)) { Id = "target-node" };
			var varNode = new BlackboardVariableNode("attackSpeed", typeof(float), BlackboardNodeMode.Get)
			{
				Id = "var-node"
			};
			_graph.AddNode(targetNode);
			_graph.AddNode(varNode);

			_graph.AddConnection(new Connection(
				"var-node", "value",
				"target-node", "speed",
				ConnectionType.BlackboardRead));

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindInputs(targetState);

			// Assert - field without [DataIn] should not receive data
			Assert.AreEqual(5f, targetState.speed, "Field without [DataIn] should not be written to");
		}

		[Test]
		public void BindOutputs_FieldWithoutDataOutAttribute_DoesNotWriteToBlackboard()
		{
			// Arrange - UnattributedState.speed has no [DataOut] attribute
			var state = new UnattributedState { speed = 42f };
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			_graph.AddNode(new StateNode(typeof(UnattributedState)) { Id = "state-node" });
			_graph.AddNode(new BlackboardVariableNode("outputVar", typeof(float), BlackboardNodeMode.Set) { Id = "var-node" });

			_graph.AddConnection(new Connection(
				"state-node", "speed",
				"var-node", "value",
				ConnectionType.BlackboardWrite));

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindOutputs(state);

			// Assert - field without [DataOut] should not write to blackboard
			Assert.IsFalse(_blackboard.HasKey("outputVar"), "Field without [DataOut] should not write to blackboard");
		}

		[Test]
		public void BindTransitions_FieldWithoutTransitionAttribute_DoesNotBind()
		{
			// Arrange - UnattributedState.onDone is an Action but has no [Transition] attribute
			var state = new UnattributedState();
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(UnattributedState)) { Id = "state-node" };
			var targetNode = new StateNode(typeof(InputState)) { Id = "target-node" };
			_graph.AddNode(stateNode);
			_graph.AddNode(targetNode);

			var connection = new Connection(
				"state-node", "onDone",
				"target-node", "entry",
				ConnectionType.Transition);
			_graph.AddConnection(connection);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindTransitions(state, _ => { });

			// Assert - Action field without [Transition] should remain null
			Assert.IsNull(state.onDone, "Action field without [Transition] should not be bound");
		}

		[Test]
		public void BindInputs_PropertyOverrideOnFieldWithoutDataIn_DoesNotApply()
		{
			// Arrange - UnattributedState.speed has no [DataIn] attribute
			var state = new UnattributedState { speed = 5f };
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(UnattributedState)) { Id = "state-node" };
			stateNode.SetPropertyOverride("speed", 999f, typeof(float));
			_graph.AddNode(stateNode);

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindInputs(state);

			// Assert - override should not apply to field without [DataIn]
			Assert.AreEqual(5f, state.speed, "Property override should not apply to field without [DataIn]");
		}

		[Test]
		public void BindInputs_DataConnectionFieldWithoutDataIn_DoesNotBind()
		{
			// Arrange - direct data connection to field without [DataIn]
			var sourceState = new SourceIntState { outputInt = 100 };
			sourceState.NodeId = "source-node";
			var targetState = new UnattributedState { speed = 5f };
			targetState.NodeId = "target-node";

			var stateInstances = new Dictionary<string, State>
			{
				{ "source-node", sourceState },
				{ "target-node", targetState }
			};

			_graph.AddNode(new StateNode(typeof(SourceIntState)) { Id = "source-node" });
			_graph.AddNode(new StateNode(typeof(UnattributedState)) { Id = "target-node" });

			_graph.AddConnection(new Connection(
				"source-node", "outputInt",
				"target-node", "speed",
				ConnectionType.Data));

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// Act
			service.BindInputs(targetState);

			// Assert
			Assert.AreEqual(5f, targetState.speed, "Data connection should not write to field without [DataIn]");
		}

		[Test]
		public void BindTransitions_ReBindOverwritesPreviousBinding()
		{
			var state = new TransitionState();
			state.NodeId = "state-node";

			var stateInstances = new Dictionary<string, State> { { "state-node", state } };

			var stateNode = new StateNode(typeof(TransitionState)) { Id = "state-node" };
			var targetNode = new StateNode(typeof(InputState)) { Id = "target-node" };
			_graph.AddNode(stateNode);
			_graph.AddNode(targetNode);

			_graph.AddConnection(new Connection(
				"state-node", "onComplete",
				"target-node", "entry",
				ConnectionType.Transition));

			var service = new PortBindingService(_graph, _blackboard, stateInstances);

			// First bind
			var firstTarget = "";
			service.BindTransitions(state, nodeId => firstTarget = nodeId);
			state.onComplete.Invoke();
			Assert.AreEqual("target-node", firstTarget);

			// Re-bind with different callback
			var secondTarget = "";
			service.BindTransitions(state, nodeId => secondTarget = nodeId);
			state.onComplete.Invoke();
			Assert.AreEqual("target-node", secondTarget);
			Assert.AreEqual("target-node", firstTarget, "First callback should not be affected by re-bind");
		}


	}
}
