using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class ConnectionBezierTests
	{
		[Test]
		public void SubdivisionLevel_DefaultValue_IsZero()
		{
			var connection = new Connection();

			Assert.AreEqual(0, connection.SubdivisionLevel);
		}

		[Test]
		public void SubdivisionLevel_SetValidValues_StoresCorrectly()
		{
			var connection = new Connection();

			connection.SubdivisionLevel = 0;
			Assert.AreEqual(0, connection.SubdivisionLevel);

			connection.SubdivisionLevel = 1;
			Assert.AreEqual(1, connection.SubdivisionLevel);

			connection.SubdivisionLevel = 2;
			Assert.AreEqual(2, connection.SubdivisionLevel);
		}

		[Test]
		public void SubdivisionLevel_NegativeValue_ClampsToZero()
		{
			var connection = new Connection();

			connection.SubdivisionLevel = -1;

			Assert.AreEqual(0, connection.SubdivisionLevel);
		}

		[Test]
		public void SubdivisionLevel_ExceedsMax_ClampsToTwo()
		{
			var connection = new Connection();

			connection.SubdivisionLevel = 5;

			Assert.AreEqual(2, connection.SubdivisionLevel);
		}

		[Test]
		public void MidpointOffset_DefaultValue_IsNull()
		{
			var connection = new Connection();

			Assert.IsNull(connection.MidpointOffset);
		}

		[Test]
		public void MidpointOffset_SetValue_StoresCorrectly()
		{
			var connection = new Connection();
			var offset = new Vector2(10f, 20f);

			connection.MidpointOffset = offset;

			Assert.IsNotNull(connection.MidpointOffset);
			Assert.AreEqual(offset, connection.MidpointOffset.Value);
		}

		[Test]
		public void MidpointOffset_SetNull_ClearsValue()
		{
			var connection = new Connection();
			connection.MidpointOffset = new Vector2(10f, 20f);

			connection.MidpointOffset = null;

			Assert.IsNull(connection.MidpointOffset);
		}

		[Test]
		public void MidpointTangent_DefaultValue_IsNull()
		{
			var connection = new Connection();

			Assert.IsNull(connection.MidpointTangent);
		}

		[Test]
		public void MidpointTangent_SetValue_StoresCorrectly()
		{
			var connection = new Connection();
			var tangent = new Vector2(5f, -5f);

			connection.MidpointTangent = tangent;

			Assert.IsNotNull(connection.MidpointTangent);
			Assert.AreEqual(tangent, connection.MidpointTangent.Value);
		}

		[Test]
		public void MidpointTangent_SetNull_ClearsValue()
		{
			var connection = new Connection();
			connection.MidpointTangent = new Vector2(5f, -5f);

			connection.MidpointTangent = null;

			Assert.IsNull(connection.MidpointTangent);
		}

		[Test]
		public void QuarterPointOffset_DefaultValue_IsNull()
		{
			var connection = new Connection();

			Assert.IsNull(connection.QuarterPointOffset);
		}

		[Test]
		public void QuarterPointOffset_SetValue_StoresCorrectly()
		{
			var connection = new Connection();
			var offset = new Vector2(3f, 7f);

			connection.QuarterPointOffset = offset;

			Assert.IsNotNull(connection.QuarterPointOffset);
			Assert.AreEqual(offset, connection.QuarterPointOffset.Value);
		}

		[Test]
		public void QuarterPointTangent_SetAndClear_WorksCorrectly()
		{
			var connection = new Connection();
			var tangent = new Vector2(1f, 2f);

			connection.QuarterPointTangent = tangent;
			Assert.AreEqual(tangent, connection.QuarterPointTangent.Value);

			connection.QuarterPointTangent = null;
			Assert.IsNull(connection.QuarterPointTangent);
		}

		[Test]
		public void ThreeQuarterPointOffset_DefaultValue_IsNull()
		{
			var connection = new Connection();

			Assert.IsNull(connection.ThreeQuarterPointOffset);
		}

		[Test]
		public void ThreeQuarterPointOffset_SetValue_StoresCorrectly()
		{
			var connection = new Connection();
			var offset = new Vector2(-5f, 15f);

			connection.ThreeQuarterPointOffset = offset;

			Assert.IsNotNull(connection.ThreeQuarterPointOffset);
			Assert.AreEqual(offset, connection.ThreeQuarterPointOffset.Value);
		}

		[Test]
		public void ThreeQuarterPointTangent_SetAndClear_WorksCorrectly()
		{
			var connection = new Connection();
			var tangent = new Vector2(8f, -3f);

			connection.ThreeQuarterPointTangent = tangent;
			Assert.AreEqual(tangent, connection.ThreeQuarterPointTangent.Value);

			connection.ThreeQuarterPointTangent = null;
			Assert.IsNull(connection.ThreeQuarterPointTangent);
		}

		[Test]
		public void EighthPointOffset_DefaultValue_IsNull()
		{
			var connection = new Connection();

			Assert.IsNull(connection.EighthPointOffset);
		}

		[Test]
		public void EighthPointOffset_SetValue_StoresCorrectly()
		{
			var connection = new Connection();
			var offset = new Vector2(2f, 4f);

			connection.EighthPointOffset = offset;

			Assert.IsNotNull(connection.EighthPointOffset);
			Assert.AreEqual(offset, connection.EighthPointOffset.Value);
		}

		[Test]
		public void ThreeEighthsPointOffset_SetAndClear_WorksCorrectly()
		{
			var connection = new Connection();
			var offset = new Vector2(6f, 12f);

			connection.ThreeEighthsPointOffset = offset;
			Assert.AreEqual(offset, connection.ThreeEighthsPointOffset.Value);

			connection.ThreeEighthsPointOffset = null;
			Assert.IsNull(connection.ThreeEighthsPointOffset);
		}

		[Test]
		public void FiveEighthsPointOffset_SetAndClear_WorksCorrectly()
		{
			var connection = new Connection();
			var offset = new Vector2(-2f, 8f);

			connection.FiveEighthsPointOffset = offset;
			Assert.AreEqual(offset, connection.FiveEighthsPointOffset.Value);

			connection.FiveEighthsPointOffset = null;
			Assert.IsNull(connection.FiveEighthsPointOffset);
		}

		[Test]
		public void SevenEighthsPointOffset_SetAndClear_WorksCorrectly()
		{
			var connection = new Connection();
			var offset = new Vector2(11f, -4f);

			connection.SevenEighthsPointOffset = offset;
			Assert.AreEqual(offset, connection.SevenEighthsPointOffset.Value);

			connection.SevenEighthsPointOffset = null;
			Assert.IsNull(connection.SevenEighthsPointOffset);
		}

		[Test]
		public void EighthPointTangent_SetAndClear_WorksCorrectly()
		{
			var connection = new Connection();
			var tangent = new Vector2(1f, 1f);

			connection.EighthPointTangent = tangent;
			Assert.AreEqual(tangent, connection.EighthPointTangent.Value);

			connection.EighthPointTangent = null;
			Assert.IsNull(connection.EighthPointTangent);
		}

		[Test]
		public void ThreeEighthsPointTangent_SetAndClear_WorksCorrectly()
		{
			var connection = new Connection();
			var tangent = new Vector2(3f, -1f);

			connection.ThreeEighthsPointTangent = tangent;
			Assert.AreEqual(tangent, connection.ThreeEighthsPointTangent.Value);

			connection.ThreeEighthsPointTangent = null;
			Assert.IsNull(connection.ThreeEighthsPointTangent);
		}

		[Test]
		public void FiveEighthsPointTangent_SetAndClear_WorksCorrectly()
		{
			var connection = new Connection();
			var tangent = new Vector2(-7f, 2f);

			connection.FiveEighthsPointTangent = tangent;
			Assert.AreEqual(tangent, connection.FiveEighthsPointTangent.Value);

			connection.FiveEighthsPointTangent = null;
			Assert.IsNull(connection.FiveEighthsPointTangent);
		}

		[Test]
		public void SevenEighthsPointTangent_SetAndClear_WorksCorrectly()
		{
			var connection = new Connection();
			var tangent = new Vector2(9f, 0f);

			connection.SevenEighthsPointTangent = tangent;
			Assert.AreEqual(tangent, connection.SevenEighthsPointTangent.Value);

			connection.SevenEighthsPointTangent = null;
			Assert.IsNull(connection.SevenEighthsPointTangent);
		}

		[Test]
		public void DefaultConstructor_GeneratesUniqueId()
		{
			var c1 = new Connection();
			var c2 = new Connection();

			Assert.IsNotNull(c1.Id);
			Assert.IsNotNull(c2.Id);
			Assert.AreNotEqual(c1.Id, c2.Id);
		}

		[Test]
		public void DefaultConstructor_SetsTransitionType()
		{
			var connection = new Connection();

			Assert.AreEqual(ConnectionType.Transition, connection.ConnectionType);
		}

		[Test]
		public void ParameterizedConstructor_SetsAllFields()
		{
			var connection = new Connection("srcNode", "srcPort", "tgtNode", "tgtPort", ConnectionType.BlackboardRead);

			Assert.IsNotNull(connection.Id);
			Assert.AreEqual("srcNode", connection.SourceNodeId);
			Assert.AreEqual("srcPort", connection.SourcePortName);
			Assert.AreEqual("tgtNode", connection.TargetNodeId);
			Assert.AreEqual("tgtPort", connection.TargetPortName);
			Assert.AreEqual(ConnectionType.BlackboardRead, connection.ConnectionType);
		}

		[Test]
		public void FullConstructor_WithId_SetsExplicitId()
		{
			var connection = new Connection("myId", "srcNode", "srcPort", "tgtNode", "tgtPort");

			Assert.AreEqual("myId", connection.Id);
		}

		[Test]
		public void FullConstructor_NullId_GeneratesId()
		{
			var connection = new Connection(null, "srcNode", "srcPort", "tgtNode", "tgtPort");

			Assert.IsNotNull(connection.Id);
		}

		[Test]
		public void IsInstant_DefaultTransitionTime_ReturnsTrue()
		{
			var connection = new Connection();

			Assert.IsTrue(connection.IsInstant);
		}

		[Test]
		public void IsInstant_PositiveTransitionTime_ReturnsFalse()
		{
			var connection = new Connection();
			connection.TransitionTime = 0.5f;

			Assert.IsFalse(connection.IsInstant);
		}

		[Test]
		public void IsInstant_ZeroTransitionTime_ReturnsTrue()
		{
			var connection = new Connection();
			connection.TransitionTime = 0f;

			Assert.IsTrue(connection.IsInstant);
		}

		[Test]
		public void AllControlPoints_SetIndependently_DoNotInterfere()
		{
			var connection = new Connection();

			connection.MidpointOffset = new Vector2(1f, 1f);
			connection.QuarterPointOffset = new Vector2(2f, 2f);
			connection.ThreeQuarterPointOffset = new Vector2(3f, 3f);
			connection.EighthPointOffset = new Vector2(4f, 4f);

			Assert.AreEqual(new Vector2(1f, 1f), connection.MidpointOffset.Value);
			Assert.AreEqual(new Vector2(2f, 2f), connection.QuarterPointOffset.Value);
			Assert.AreEqual(new Vector2(3f, 3f), connection.ThreeQuarterPointOffset.Value);
			Assert.AreEqual(new Vector2(4f, 4f), connection.EighthPointOffset.Value);
		}

		[Test]
		public void ClearingOneControlPoint_DoesNotAffectOthers()
		{
			var connection = new Connection();
			connection.MidpointOffset = new Vector2(1f, 1f);
			connection.QuarterPointOffset = new Vector2(2f, 2f);

			connection.MidpointOffset = null;

			Assert.IsNull(connection.MidpointOffset);
			Assert.IsNotNull(connection.QuarterPointOffset);
			Assert.AreEqual(new Vector2(2f, 2f), connection.QuarterPointOffset.Value);
		}
	}
}
