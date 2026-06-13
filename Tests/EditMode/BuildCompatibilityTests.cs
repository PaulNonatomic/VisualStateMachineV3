using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using Nonatomic.VSM.Graph;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nonatomic.VSM.Tests.EditMode
{
	/// <summary>
	/// Tests to validate that the state machine works correctly in builds
	/// across all Unity platforms.
	/// </summary>
	[TestFixture]
	public class BuildCompatibilityTests
	{
		private StateMachineGraph _graph;
		private StateMachineInstance _instance;


		private class SimpleState : State
		{
			public static bool Entered;
			[Transition("Next")] public Action next;
			public override void OnEnter() => Entered = true;
			public static void Reset() => Entered = false;
		}

		private class StateWithAllDataTypes : State
		{
			[DataIn("IntValue")] public int intValue;
			[DataIn("FloatValue")] public float floatValue;
			[DataIn("StringValue")] public string stringValue;
			[DataIn("BoolValue")] public bool boolValue;
			[DataIn("Vector2Value")] public Vector2 vector2Value;
			[DataIn("Vector3Value")] public Vector3 vector3Value;
			[DataIn("ColorValue")] public Color colorValue;
			[DataIn("QuaternionValue")] public Quaternion quaternionValue;

			public static int ReceivedInt;
			public static float ReceivedFloat;
			public static string ReceivedString;
			public static bool ReceivedBool;
			public static Vector2 ReceivedVector2;
			public static Vector3 ReceivedVector3;
			public static Color ReceivedColor;
			public static Quaternion ReceivedQuaternion;

			public override void OnEnter()
			{
				ReceivedInt = intValue;
				ReceivedFloat = floatValue;
				ReceivedString = stringValue;
				ReceivedBool = boolValue;
				ReceivedVector2 = vector2Value;
				ReceivedVector3 = vector3Value;
				ReceivedColor = colorValue;
				ReceivedQuaternion = quaternionValue;
			}

			public static void Reset()
			{
				ReceivedInt = 0;
				ReceivedFloat = 0f;
				ReceivedString = null;
				ReceivedBool = false;
				ReceivedVector2 = Vector2.zero;
				ReceivedVector3 = Vector3.zero;
				ReceivedColor = Color.clear;
				ReceivedQuaternion = Quaternion.identity;
			}
		}

		private class StateWithOutputs : State
		{
			[DataOut("IntOut")] public int intOut;
			[DataOut("FloatOut")] public float floatOut;
			[DataOut("StringOut")] public string stringOut;
			[Transition("Done")] public Action done;

			public override void OnEnter()
			{
				intOut = 42;
				floatOut = 3.14159f;
				stringOut = "Hello World";
			}
		}


		[SetUp]
		public void SetUp()
		{
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			SimpleState.Reset();
			StateWithAllDataTypes.Reset();
		}

		[TearDown]
		public void TearDown()
		{
			_instance?.Stop();
			_instance = null;

			if (_graph != null)
			{
				UnityEngine.Object.DestroyImmediate(_graph);
			}
		}


		[Test]
		public void Serialization_GraphSurvivesSerializationRoundtrip()
		{
			// Setup
			var entry = new EntryNode { Id = "entry" };
			var state = new StateNode
			{
				Id = "state1",
				DisplayName = "Test State",
				StateTypeAssemblyQualifiedName = typeof(SimpleState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(state);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "state1",
				ConnectionType = ConnectionType.Transition
			});

			// Trigger serialization
			var json = JsonUtility.ToJson(_graph);
			Assert.IsNotEmpty(json);

			// Create new graph from JSON
			var newGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			JsonUtility.FromJsonOverwrite(json, newGraph);

			// Verify
			Assert.AreEqual("entry", newGraph.EntryNodeId);
			Assert.IsNotNull(newGraph.GetNode("entry"));
			Assert.IsNotNull(newGraph.GetNode("state1"));

			UnityEngine.Object.DestroyImmediate(newGraph);
		}

		[Test]
		public void Serialization_StateTypeRestoredAfterDeserialization()
		{
			// Setup
			var entry = new EntryNode { Id = "entry" };
			var state = new StateNode
			{
				Id = "state1",
				StateTypeAssemblyQualifiedName = typeof(SimpleState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(state);
			_graph.EntryNodeId = "entry";

			// Simulate serialization roundtrip
			var json = JsonUtility.ToJson(_graph);
			var newGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			JsonUtility.FromJsonOverwrite(json, newGraph);

			// Restore types (happens in OnAfterDeserialize)
			var restoredState = newGraph.GetNode("state1") as StateNode;
			Assert.IsNotNull(restoredState);
			restoredState.RestoreType();

			Assert.IsNotNull(restoredState.StateType);
			Assert.AreEqual(typeof(SimpleState), restoredState.StateType);

			UnityEngine.Object.DestroyImmediate(newGraph);
		}

		[Test]
		public void Serialization_ConnectionsPreservedAfterDeserialization()
		{
			// Setup
			var entry = new EntryNode { Id = "entry" };
			var stateA = new StateNode { Id = "stateA", StateTypeAssemblyQualifiedName = typeof(SimpleState).AssemblyQualifiedName };
			var stateB = new StateNode { Id = "stateB", StateTypeAssemblyQualifiedName = typeof(SimpleState).AssemblyQualifiedName };

			_graph.AddNode(entry);
			_graph.AddNode(stateA);
			_graph.AddNode(stateB);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "stateA",
				ConnectionType = ConnectionType.Transition
			});

			_graph.AddConnection(new Connection
			{
				Id = "c2",
				SourceNodeId = "stateA",
				SourcePortName = "next",
				TargetNodeId = "stateB",
				ConnectionType = ConnectionType.Transition
			});

			// Serialize and deserialize
			var json = JsonUtility.ToJson(_graph);
			var newGraph = ScriptableObject.CreateInstance<StateMachineGraph>();
			JsonUtility.FromJsonOverwrite(json, newGraph);

			// Verify connections
			var connections = newGraph.GetConnectionsFromNode("stateA");
			Assert.IsNotNull(connections);
			Assert.AreEqual(1, connections.Count());
			Assert.AreEqual("stateB", connections.First().TargetNodeId);

			UnityEngine.Object.DestroyImmediate(newGraph);
		}



		[Test]
		public void Serialization_FloatValuesAreLocaleIndependent()
		{
			// Save current culture
			var originalCulture = Thread.CurrentThread.CurrentCulture;

			try
			{
				// Test with a culture that uses comma as decimal separator
				Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

				var state = new StateNode
				{
					Id = "state1",
					StateTypeAssemblyQualifiedName = typeof(StateWithAllDataTypes).AssemblyQualifiedName
				};

				// Set a float property override
				state.SetPropertyOverride("floatValue", 3.14159f, typeof(float));

				// Serialize
				var json = JsonUtility.ToJson(state);

				// Switch to US culture for deserialization
				Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

				// Deserialize
				var newState = new StateNode();
				JsonUtility.FromJsonOverwrite(json, newState);

				// Get the value back
				var overrideValue = newState.GetPropertyOverride("floatValue");
				Assert.IsNotNull(overrideValue);

				var value = overrideValue.GetValue();
				Assert.IsNotNull(value);
				Assert.AreEqual(3.14159f, (float)value, 0.0001f);
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = originalCulture;
			}
		}



		[Test]
		public void TypeResolution_PrimitiveTypesResolveCorrectly()
		{
			// These are the types that need special handling in builds
			var primitiveTypes = new[]
			{
				typeof(int),
				typeof(float),
				typeof(double),
				typeof(bool),
				typeof(string),
				typeof(long),
				typeof(short),
				typeof(byte)
			};

			foreach (var type in primitiveTypes)
			{
				var resolved = Type.GetType(type.AssemblyQualifiedName);
				Assert.IsNotNull(resolved, $"Failed to resolve {type.Name}");
				Assert.AreEqual(type, resolved);
			}
		}

		[Test]
		public void TypeResolution_UnityTypesResolveCorrectly()
		{
			var unityTypes = new[]
			{
				typeof(Vector2),
				typeof(Vector3),
				typeof(Vector4),
				typeof(Quaternion),
				typeof(Color),
				typeof(Rect),
				typeof(Bounds),
				typeof(LayerMask)
			};

			foreach (var type in unityTypes)
			{
				var resolved = Type.GetType(type.AssemblyQualifiedName);
				Assert.IsNotNull(resolved, $"Failed to resolve {type.Name}");
				Assert.AreEqual(type, resolved);
			}
		}

		[Test]
		public void TypeResolution_StateTypeFromAssemblyQualifiedName()
		{
			var typeName = typeof(SimpleState).AssemblyQualifiedName;
			var resolved = Type.GetType(typeName);

			Assert.IsNotNull(resolved);
			Assert.AreEqual(typeof(SimpleState), resolved);
			Assert.IsTrue(typeof(State).IsAssignableFrom(resolved));
		}



		[Test]
		public void StateInstantiation_CanCreateStateViaActivator()
		{
			var stateType = typeof(SimpleState);
			var state = Activator.CreateInstance(stateType) as State;

			Assert.IsNotNull(state);
			Assert.IsInstanceOf<SimpleState>(state);
		}

		[Test]
		public void StateInstantiation_StateHasDefaultConstructor()
		{
			// All State types must have a default constructor for Activator.CreateInstance
			var stateType = typeof(SimpleState);
			var constructor = stateType.GetConstructor(Type.EmptyTypes);

			Assert.IsNotNull(constructor, "State must have a parameterless constructor");
		}



		[Test]
		public void Runtime_InstanceStartsAndEntersFirstState()
		{
			var entry = new EntryNode { Id = "entry" };
			var state = new StateNode
			{
				Id = "state1",
				StateTypeAssemblyQualifiedName = typeof(SimpleState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(state);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "state1",
				ConnectionType = ConnectionType.Transition
			});

			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			Assert.IsTrue(_instance.IsRunning);
			Assert.IsTrue(SimpleState.Entered);
			Assert.IsNotNull(_instance.CurrentState);
			Assert.IsInstanceOf<SimpleState>(_instance.CurrentState);
		}

		[Test]
		public void Runtime_BlackboardWorksWithAllTypes()
		{
			var entry = new EntryNode { Id = "entry" };
			var state = new StateNode
			{
				Id = "state1",
				StateTypeAssemblyQualifiedName = typeof(StateWithAllDataTypes).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(state);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "state1",
				ConnectionType = ConnectionType.Transition
			});

			_instance = new StateMachineInstance(_graph);

			// Set blackboard values before starting
			_instance.Blackboard.Set("IntValue", 42);
			_instance.Blackboard.Set("FloatValue", 3.14f);
			_instance.Blackboard.Set("StringValue", "Test");
			_instance.Blackboard.Set("BoolValue", true);
			_instance.Blackboard.Set("Vector2Value", new Vector2(1, 2));
			_instance.Blackboard.Set("Vector3Value", new Vector3(1, 2, 3));
			_instance.Blackboard.Set("ColorValue", Color.red);
			_instance.Blackboard.Set("QuaternionValue", Quaternion.Euler(45, 90, 0));

			_instance.Start();

			// Verify values were bound
			Assert.AreEqual(42, StateWithAllDataTypes.ReceivedInt);
			Assert.AreEqual(3.14f, StateWithAllDataTypes.ReceivedFloat, 0.001f);
			Assert.AreEqual("Test", StateWithAllDataTypes.ReceivedString);
			Assert.IsTrue(StateWithAllDataTypes.ReceivedBool);
			Assert.AreEqual(new Vector2(1, 2), StateWithAllDataTypes.ReceivedVector2);
			Assert.AreEqual(new Vector3(1, 2, 3), StateWithAllDataTypes.ReceivedVector3);
			Assert.AreEqual(Color.red, StateWithAllDataTypes.ReceivedColor);
		}



		[Test]
		public void EdgeCase_EmptyGraphDoesNotCrash()
		{
			// Graph with no nodes - add entry node but no connections
			var entry = new EntryNode { Id = "entry" };
			_graph.AddNode(entry);
			_graph.EntryNodeId = "entry";

			_instance = new StateMachineInstance(_graph);

			// Expect the error log about missing transition
			LogAssert.Expect(LogType.Error, "Entry node has no outgoing transition");

			// Should not throw
			_instance.Start();

			// Instance created but not running since there's no outgoing transition
			Assert.IsFalse(_instance.IsRunning);
		}

		[Test]
		public void EdgeCase_MissingStateTypeHandledGracefully()
		{
			var entry = new EntryNode { Id = "entry" };
			var state = new StateNode
			{
				Id = "state1",
				// Invalid type name
				StateTypeAssemblyQualifiedName = "NonExistent.Type, NonExistent.Assembly"
			};

			_graph.AddNode(entry);
			_graph.AddNode(state);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "state1",
				ConnectionType = ConnectionType.Transition
			});

			state.RestoreType();

			// Type should be null for invalid type name
			Assert.IsNull(state.StateType);
		}

		[Test]
		public void EdgeCase_NullTransitionActionDoesNotCrash()
		{
			var entry = new EntryNode { Id = "entry" };
			var state = new StateNode
			{
				Id = "state1",
				StateTypeAssemblyQualifiedName = typeof(SimpleState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(state);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "state1",
				ConnectionType = ConnectionType.Transition
			});

			// No connection from state1, so 'next' transition will be null
			_instance = new StateMachineInstance(_graph);
			_instance.Start();

			var currentState = _instance.CurrentState as SimpleState;
			Assert.IsNotNull(currentState);

			// Invoking null action should not crash
			Assert.DoesNotThrow(() => currentState.next?.Invoke());
		}

		[Test]
		public void EdgeCase_StopWhileNotRunningDoesNotCrash()
		{
			var entry = new EntryNode { Id = "entry" };
			_graph.AddNode(entry);
			_graph.EntryNodeId = "entry";

			_instance = new StateMachineInstance(_graph);

			// Stop without starting
			Assert.DoesNotThrow(() => _instance.Stop());
		}

		[Test]
		public void EdgeCase_DoubleStartDoesNotCrash()
		{
			var entry = new EntryNode { Id = "entry" };
			var state = new StateNode
			{
				Id = "state1",
				StateTypeAssemblyQualifiedName = typeof(SimpleState).AssemblyQualifiedName
			};

			_graph.AddNode(entry);
			_graph.AddNode(state);
			_graph.EntryNodeId = "entry";

			_graph.AddConnection(new Connection
			{
				Id = "c1",
				SourceNodeId = "entry",
				TargetNodeId = "state1",
				ConnectionType = ConnectionType.Transition
			});

			_instance = new StateMachineInstance(_graph);

			_instance.Start();
			Assert.DoesNotThrow(() => _instance.Start());
		}

	}
}
