using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class ConnectionTests
	{

		[Test]
		public void SourceNodeId_SetAndGet_ReturnsValue()
		{
			var connection = new Connection();

			connection.SourceNodeId = "node-abc";

			Assert.AreEqual("node-abc", connection.SourceNodeId);
		}

		[Test]
		public void SourcePortName_SetAndGet_ReturnsValue()
		{
			var connection = new Connection();

			connection.SourcePortName = "OnComplete";

			Assert.AreEqual("OnComplete", connection.SourcePortName);
		}

		[Test]
		public void TargetNodeId_SetAndGet_ReturnsValue()
		{
			var connection = new Connection();

			connection.TargetNodeId = "node-xyz";

			Assert.AreEqual("node-xyz", connection.TargetNodeId);
		}

		[Test]
		public void TargetPortName_SetAndGet_ReturnsValue()
		{
			var connection = new Connection();

			connection.TargetPortName = "Enter";

			Assert.AreEqual("Enter", connection.TargetPortName);
		}

		[Test]
		public void ConnectionType_SetAndGet_ReturnsValue()
		{
			var connection = new Connection();

			connection.ConnectionType = ConnectionType.Data;

			Assert.AreEqual(ConnectionType.Data, connection.ConnectionType);
		}

		[Test]
		public void TransitionTime_SetAndGet_ReturnsValue()
		{
			var connection = new Connection();

			connection.TransitionTime = 1.5f;

			Assert.AreEqual(1.5f, connection.TransitionTime);
		}

		[Test]
		public void EasingCurve_SetAndGet_ReturnsValue()
		{
			var connection = new Connection();
			var curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

			connection.EasingCurve = curve;

			Assert.IsNotNull(connection.EasingCurve);
			Assert.AreEqual(curve.length, connection.EasingCurve.length);
		}

		[Test]
		public void EasingCurve_DefaultValue_IsNull()
		{
			var connection = new Connection();

			Assert.IsNull(connection.EasingCurve);
		}

		[Test]
		public void Id_SetAndGet_ReturnsValue()
		{
			var connection = new Connection();

			connection.Id = "custom-id";

			Assert.AreEqual("custom-id", connection.Id);
		}



		[Test]
		public void ConnectionType_Data_CanBeAssigned()
		{
			var connection = new Connection("src", "port", "tgt", "port", ConnectionType.Data);

			Assert.AreEqual(ConnectionType.Data, connection.ConnectionType);
		}

		[Test]
		public void ConnectionType_Transition_CanBeAssigned()
		{
			var connection = new Connection("src", "port", "tgt", "port", ConnectionType.Transition);

			Assert.AreEqual(ConnectionType.Transition, connection.ConnectionType);
		}

		[Test]
		public void ConnectionType_BlackboardRead_CanBeAssigned()
		{
			var connection = new Connection("src", "port", "tgt", "port", ConnectionType.BlackboardRead);

			Assert.AreEqual(ConnectionType.BlackboardRead, connection.ConnectionType);
		}

		[Test]
		public void ConnectionType_BlackboardWrite_CanBeAssigned()
		{
			var connection = new Connection("src", "port", "tgt", "port", ConnectionType.BlackboardWrite);

			Assert.AreEqual(ConnectionType.BlackboardWrite, connection.ConnectionType);
		}

		[Test]
		public void ConnectionType_DefaultConstructor_IsTransition()
		{
			var connection = new Connection();

			Assert.AreEqual(ConnectionType.Transition, connection.ConnectionType);
		}

		[Test]
		public void ConnectionType_ParameterizedConstructor_DefaultIsTransition()
		{
			var connection = new Connection("src", "port", "tgt", "port");

			Assert.AreEqual(ConnectionType.Transition, connection.ConnectionType);
		}



		[Test]
		public void DefaultConstructor_GeneratesNonEmptyId()
		{
			var connection = new Connection();

			Assert.IsFalse(string.IsNullOrEmpty(connection.Id));
		}

		[Test]
		public void DefaultConstructor_SourceNodeId_IsNull()
		{
			var connection = new Connection();

			Assert.IsNull(connection.SourceNodeId);
		}

		[Test]
		public void DefaultConstructor_TargetNodeId_IsNull()
		{
			var connection = new Connection();

			Assert.IsNull(connection.TargetNodeId);
		}

		[Test]
		public void DefaultConstructor_TransitionTime_IsZero()
		{
			var connection = new Connection();

			Assert.AreEqual(0f, connection.TransitionTime);
		}

		[Test]
		public void ParameterizedConstructor_StoresAllParameters()
		{
			var connection = new Connection("srcNode", "srcPort", "tgtNode", "tgtPort", ConnectionType.Data);

			Assert.AreEqual("srcNode", connection.SourceNodeId);
			Assert.AreEqual("srcPort", connection.SourcePortName);
			Assert.AreEqual("tgtNode", connection.TargetNodeId);
			Assert.AreEqual("tgtPort", connection.TargetPortName);
			Assert.AreEqual(ConnectionType.Data, connection.ConnectionType);
		}

		[Test]
		public void ParameterizedConstructor_GeneratesUniqueId()
		{
			var c1 = new Connection("src", "port", "tgt", "port");
			var c2 = new Connection("src", "port", "tgt", "port");

			Assert.AreNotEqual(c1.Id, c2.Id);
		}

		[Test]
		public void FullConstructor_WithExplicitId_UsesProvidedId()
		{
			var connection = new Connection("my-custom-id", "src", "port", "tgt", "port");

			Assert.AreEqual("my-custom-id", connection.Id);
		}

		[Test]
		public void FullConstructor_WithNullId_GeneratesId()
		{
			var connection = new Connection(null, "src", "port", "tgt", "port");

			Assert.IsNotNull(connection.Id);
			Assert.IsFalse(string.IsNullOrEmpty(connection.Id));
		}

		[Test]
		public void FullConstructor_StoresPortNames()
		{
			var connection = new Connection("id", "srcNode", "OnExit", "tgtNode", "OnEnter", ConnectionType.BlackboardWrite);

			Assert.AreEqual("srcNode", connection.SourceNodeId);
			Assert.AreEqual("OnExit", connection.SourcePortName);
			Assert.AreEqual("tgtNode", connection.TargetNodeId);
			Assert.AreEqual("OnEnter", connection.TargetPortName);
			Assert.AreEqual(ConnectionType.BlackboardWrite, connection.ConnectionType);
		}

		[Test]
		public void FullConstructor_DefaultType_IsTransition()
		{
			var connection = new Connection("id", "src", "port", "tgt", "port");

			Assert.AreEqual(ConnectionType.Transition, connection.ConnectionType);
		}



		[Test]
		public void IsInstant_NegativeTransitionTime_ReturnsTrue()
		{
			var connection = new Connection();

			connection.TransitionTime = -1f;

			Assert.IsTrue(connection.IsInstant);
		}

		[Test]
		public void IsInstant_SmallPositiveTransitionTime_ReturnsFalse()
		{
			var connection = new Connection();

			connection.TransitionTime = 0.001f;

			Assert.IsFalse(connection.IsInstant);
		}



		[Test]
		public void JsonSerialization_CoreProperties_Preserved()
		{
			var original = new Connection("test-id", "srcNode", "srcPort", "tgtNode", "tgtPort", ConnectionType.BlackboardRead);
			original.TransitionTime = 2.5f;

			var json = JsonUtility.ToJson(original);
			var restored = JsonUtility.FromJson<Connection>(json);

			Assert.AreEqual("test-id", restored.Id);
			Assert.AreEqual("srcNode", restored.SourceNodeId);
			Assert.AreEqual("srcPort", restored.SourcePortName);
			Assert.AreEqual("tgtNode", restored.TargetNodeId);
			Assert.AreEqual("tgtPort", restored.TargetPortName);
			Assert.AreEqual(ConnectionType.BlackboardRead, restored.ConnectionType);
			Assert.AreEqual(2.5f, restored.TransitionTime);
		}

		[Test]
		public void JsonSerialization_SubdivisionLevel_Preserved()
		{
			var original = new Connection();
			original.SubdivisionLevel = 2;

			var json = JsonUtility.ToJson(original);
			var restored = JsonUtility.FromJson<Connection>(json);

			Assert.AreEqual(2, restored.SubdivisionLevel);
		}

		[Test]
		public void JsonSerialization_ControlPoints_Preserved()
		{
			var original = new Connection();
			original.MidpointOffset = new Vector2(10f, 20f);
			original.MidpointTangent = new Vector2(5f, -5f);
			original.QuarterPointOffset = new Vector2(3f, 7f);

			var json = JsonUtility.ToJson(original);
			var restored = JsonUtility.FromJson<Connection>(json);

			Assert.AreEqual(new Vector2(10f, 20f), restored.MidpointOffset.Value);
			Assert.AreEqual(new Vector2(5f, -5f), restored.MidpointTangent.Value);
			Assert.AreEqual(new Vector2(3f, 7f), restored.QuarterPointOffset.Value);
		}

		[Test]
		public void JsonSerialization_NullControlPoints_PreservedAsNull()
		{
			var original = new Connection();

			var json = JsonUtility.ToJson(original);
			var restored = JsonUtility.FromJson<Connection>(json);

			Assert.IsNull(restored.MidpointOffset);
			Assert.IsNull(restored.MidpointTangent);
			Assert.IsNull(restored.QuarterPointOffset);
			Assert.IsNull(restored.ThreeQuarterPointOffset);
		}

		[Test]
		public void JsonSerialization_EasingCurve_Preserved()
		{
			var original = new Connection();
			original.EasingCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

			var json = JsonUtility.ToJson(original);
			var restored = JsonUtility.FromJson<Connection>(json);

			Assert.IsNotNull(restored.EasingCurve);
			Assert.AreEqual(original.EasingCurve.length, restored.EasingCurve.length);
		}

		[Test]
		public void JsonSerialization_AllConnectionTypes_Preserved()
		{
			var types = new[]
			{
				ConnectionType.Data,
				ConnectionType.Transition,
				ConnectionType.BlackboardRead,
				ConnectionType.BlackboardWrite
			};

			foreach (var type in types)
			{
				var original = new Connection("src", "port", "tgt", "port", type);

				var json = JsonUtility.ToJson(original);
				var restored = JsonUtility.FromJson<Connection>(json);

				Assert.AreEqual(type, restored.ConnectionType, $"ConnectionType {type} was not preserved through serialization");
			}
		}



		[Test]
		public void Constructor_NullSourceNodeId_Stores()
		{
			var connection = new Connection(null, null, null, null);

			Assert.IsNull(connection.SourceNodeId);
			Assert.IsNull(connection.SourcePortName);
			Assert.IsNull(connection.TargetNodeId);
			Assert.IsNull(connection.TargetPortName);
		}

		[Test]
		public void Constructor_EmptyStringPorts_Stores()
		{
			var connection = new Connection("", "", "", "");

			Assert.AreEqual("", connection.SourceNodeId);
			Assert.AreEqual("", connection.SourcePortName);
			Assert.AreEqual("", connection.TargetNodeId);
			Assert.AreEqual("", connection.TargetPortName);
		}

		[Test]
		public void TransitionTime_LargeValue_Stores()
		{
			var connection = new Connection();

			connection.TransitionTime = 999999f;

			Assert.AreEqual(999999f, connection.TransitionTime);
		}

		[Test]
		public void Id_OverwriteAutoGenerated_WorksCorrectly()
		{
			var connection = new Connection();
			var originalId = connection.Id;

			connection.Id = "overwritten-id";

			Assert.AreNotEqual(originalId, connection.Id);
			Assert.AreEqual("overwritten-id", connection.Id);
		}

		[Test]
		public void MultipleConnections_IndependentState()
		{
			var c1 = new Connection("src1", "portA", "tgt1", "portB", ConnectionType.Transition);
			var c2 = new Connection("src2", "portC", "tgt2", "portD", ConnectionType.Data);

			c1.TransitionTime = 1.0f;
			c2.TransitionTime = 2.0f;

			Assert.AreEqual("src1", c1.SourceNodeId);
			Assert.AreEqual("src2", c2.SourceNodeId);
			Assert.AreEqual(1.0f, c1.TransitionTime);
			Assert.AreEqual(2.0f, c2.TransitionTime);
			Assert.AreEqual(ConnectionType.Transition, c1.ConnectionType);
			Assert.AreEqual(ConnectionType.Data, c2.ConnectionType);
		}

		[Test]
		public void ConnectionType_CanBeChanged_AfterConstruction()
		{
			var connection = new Connection("src", "port", "tgt", "port", ConnectionType.Transition);

			connection.ConnectionType = ConnectionType.BlackboardRead;

			Assert.AreEqual(ConnectionType.BlackboardRead, connection.ConnectionType);
		}

		[Test]
		public void EasingCurve_SetNull_ClearsValue()
		{
			var connection = new Connection();
			connection.EasingCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

			connection.EasingCurve = null;

			Assert.IsNull(connection.EasingCurve);
		}

	}
}
