using NUnit.Framework;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class ConnectionCurveDataTests
	{
		private Connection _connection;

		[SetUp]
		public void SetUp()
		{
			_connection = new Connection();
		}


		[Test]
		public void SubdivisionLevel_Default_IsZero()
		{
			Assert.AreEqual(0, _connection.SubdivisionLevel);
		}

		[Test]
		public void SubdivisionLevel_Set_PersistsValue()
		{
			_connection.SubdivisionLevel = 1;

			Assert.AreEqual(1, _connection.SubdivisionLevel);
		}

		[Test]
		public void SubdivisionLevel_Set_ClampsToMax2()
		{
			_connection.SubdivisionLevel = 5;

			Assert.AreEqual(2, _connection.SubdivisionLevel);
		}

		[Test]
		public void SubdivisionLevel_Set_ClampsToMin0()
		{
			_connection.SubdivisionLevel = -1;

			Assert.AreEqual(0, _connection.SubdivisionLevel);
		}

		[TestCase(0)]
		[TestCase(1)]
		[TestCase(2)]
		public void SubdivisionLevel_ValidValues_PersistCorrectly(int level)
		{
			_connection.SubdivisionLevel = level;

			Assert.AreEqual(level, _connection.SubdivisionLevel);
		}



		[Test]
		public void QuarterPointOffset_Default_IsNull()
		{
			Assert.IsNull(_connection.QuarterPointOffset);
		}

		[Test]
		public void QuarterPointOffset_SetValue_ReturnsValue()
		{
			var offset = new Vector2(10f, 20f);
			_connection.QuarterPointOffset = offset;

			Assert.IsNotNull(_connection.QuarterPointOffset);
			Assert.AreEqual(offset, _connection.QuarterPointOffset.Value);
		}

		[Test]
		public void QuarterPointOffset_SetNull_ReturnsNull()
		{
			_connection.QuarterPointOffset = new Vector2(10f, 20f);
			_connection.QuarterPointOffset = null;

			Assert.IsNull(_connection.QuarterPointOffset);
		}

		[Test]
		public void QuarterPointTangent_Default_IsNull()
		{
			Assert.IsNull(_connection.QuarterPointTangent);
		}

		[Test]
		public void QuarterPointTangent_SetValue_ReturnsValue()
		{
			var tangent = new Vector2(30f, 40f);
			_connection.QuarterPointTangent = tangent;

			Assert.IsNotNull(_connection.QuarterPointTangent);
			Assert.AreEqual(tangent, _connection.QuarterPointTangent.Value);
		}

		[Test]
		public void QuarterPointTangent_SetNull_ReturnsNull()
		{
			_connection.QuarterPointTangent = new Vector2(30f, 40f);
			_connection.QuarterPointTangent = null;

			Assert.IsNull(_connection.QuarterPointTangent);
		}



		[Test]
		public void MidpointOffset_Default_IsNull()
		{
			Assert.IsNull(_connection.MidpointOffset);
		}

		[Test]
		public void MidpointOffset_SetValue_ReturnsValue()
		{
			var offset = new Vector2(50f, 60f);
			_connection.MidpointOffset = offset;

			Assert.IsNotNull(_connection.MidpointOffset);
			Assert.AreEqual(offset, _connection.MidpointOffset.Value);
		}

		[Test]
		public void MidpointOffset_SetNull_ReturnsNull()
		{
			_connection.MidpointOffset = new Vector2(50f, 60f);
			_connection.MidpointOffset = null;

			Assert.IsNull(_connection.MidpointOffset);
		}

		[Test]
		public void MidpointTangent_Default_IsNull()
		{
			Assert.IsNull(_connection.MidpointTangent);
		}

		[Test]
		public void MidpointTangent_SetValue_ReturnsValue()
		{
			var tangent = new Vector2(70f, 80f);
			_connection.MidpointTangent = tangent;

			Assert.IsNotNull(_connection.MidpointTangent);
			Assert.AreEqual(tangent, _connection.MidpointTangent.Value);
		}

		[Test]
		public void MidpointTangent_SetNull_ReturnsNull()
		{
			_connection.MidpointTangent = new Vector2(70f, 80f);
			_connection.MidpointTangent = null;

			Assert.IsNull(_connection.MidpointTangent);
		}



		[Test]
		public void ThreeQuarterPointOffset_Default_IsNull()
		{
			Assert.IsNull(_connection.ThreeQuarterPointOffset);
		}

		[Test]
		public void ThreeQuarterPointOffset_SetValue_ReturnsValue()
		{
			var offset = new Vector2(90f, 100f);
			_connection.ThreeQuarterPointOffset = offset;

			Assert.IsNotNull(_connection.ThreeQuarterPointOffset);
			Assert.AreEqual(offset, _connection.ThreeQuarterPointOffset.Value);
		}

		[Test]
		public void ThreeQuarterPointOffset_SetNull_ReturnsNull()
		{
			_connection.ThreeQuarterPointOffset = new Vector2(90f, 100f);
			_connection.ThreeQuarterPointOffset = null;

			Assert.IsNull(_connection.ThreeQuarterPointOffset);
		}

		[Test]
		public void ThreeQuarterPointTangent_Default_IsNull()
		{
			Assert.IsNull(_connection.ThreeQuarterPointTangent);
		}

		[Test]
		public void ThreeQuarterPointTangent_SetValue_ReturnsValue()
		{
			var tangent = new Vector2(110f, 120f);
			_connection.ThreeQuarterPointTangent = tangent;

			Assert.IsNotNull(_connection.ThreeQuarterPointTangent);
			Assert.AreEqual(tangent, _connection.ThreeQuarterPointTangent.Value);
		}



		[Test]
		public void EighthPointOffset_Default_IsNull()
		{
			Assert.IsNull(_connection.EighthPointOffset);
		}

		[Test]
		public void EighthPointOffset_SetValue_ReturnsValue()
		{
			var offset = new Vector2(1f, 2f);
			_connection.EighthPointOffset = offset;

			Assert.AreEqual(offset, _connection.EighthPointOffset.Value);
		}

		[Test]
		public void EighthPointOffset_SetNull_ReturnsNull()
		{
			_connection.EighthPointOffset = new Vector2(1f, 2f);
			_connection.EighthPointOffset = null;

			Assert.IsNull(_connection.EighthPointOffset);
		}

		[Test]
		public void EighthPointTangent_Default_IsNull()
		{
			Assert.IsNull(_connection.EighthPointTangent);
		}

		[Test]
		public void EighthPointTangent_SetValue_ReturnsValue()
		{
			var tangent = new Vector2(3f, 4f);
			_connection.EighthPointTangent = tangent;

			Assert.AreEqual(tangent, _connection.EighthPointTangent.Value);
		}

		[Test]
		public void ThreeEighthsPointOffset_Default_IsNull()
		{
			Assert.IsNull(_connection.ThreeEighthsPointOffset);
		}

		[Test]
		public void ThreeEighthsPointOffset_SetValue_ReturnsValue()
		{
			var offset = new Vector2(5f, 6f);
			_connection.ThreeEighthsPointOffset = offset;

			Assert.AreEqual(offset, _connection.ThreeEighthsPointOffset.Value);
		}

		[Test]
		public void ThreeEighthsPointTangent_Default_IsNull()
		{
			Assert.IsNull(_connection.ThreeEighthsPointTangent);
		}

		[Test]
		public void ThreeEighthsPointTangent_SetValue_ReturnsValue()
		{
			var tangent = new Vector2(7f, 8f);
			_connection.ThreeEighthsPointTangent = tangent;

			Assert.AreEqual(tangent, _connection.ThreeEighthsPointTangent.Value);
		}

		[Test]
		public void FiveEighthsPointOffset_Default_IsNull()
		{
			Assert.IsNull(_connection.FiveEighthsPointOffset);
		}

		[Test]
		public void FiveEighthsPointOffset_SetValue_ReturnsValue()
		{
			var offset = new Vector2(9f, 10f);
			_connection.FiveEighthsPointOffset = offset;

			Assert.AreEqual(offset, _connection.FiveEighthsPointOffset.Value);
		}

		[Test]
		public void FiveEighthsPointTangent_Default_IsNull()
		{
			Assert.IsNull(_connection.FiveEighthsPointTangent);
		}

		[Test]
		public void FiveEighthsPointTangent_SetValue_ReturnsValue()
		{
			var tangent = new Vector2(11f, 12f);
			_connection.FiveEighthsPointTangent = tangent;

			Assert.AreEqual(tangent, _connection.FiveEighthsPointTangent.Value);
		}

		[Test]
		public void SevenEighthsPointOffset_Default_IsNull()
		{
			Assert.IsNull(_connection.SevenEighthsPointOffset);
		}

		[Test]
		public void SevenEighthsPointOffset_SetValue_ReturnsValue()
		{
			var offset = new Vector2(13f, 14f);
			_connection.SevenEighthsPointOffset = offset;

			Assert.AreEqual(offset, _connection.SevenEighthsPointOffset.Value);
		}

		[Test]
		public void SevenEighthsPointTangent_Default_IsNull()
		{
			Assert.IsNull(_connection.SevenEighthsPointTangent);
		}

		[Test]
		public void SevenEighthsPointTangent_SetValue_ReturnsValue()
		{
			var tangent = new Vector2(15f, 16f);
			_connection.SevenEighthsPointTangent = tangent;

			Assert.AreEqual(tangent, _connection.SevenEighthsPointTangent.Value);
		}



		[Test]
		public void AllLevel1NullableProperties_RoundTrip()
		{
			var offset = new Vector2(100f, 200f);
			var tangent = new Vector2(300f, 400f);

			_connection.QuarterPointOffset = offset;
			_connection.QuarterPointTangent = tangent;
			_connection.MidpointOffset = offset;
			_connection.MidpointTangent = tangent;
			_connection.ThreeQuarterPointOffset = offset;
			_connection.ThreeQuarterPointTangent = tangent;

			Assert.AreEqual(offset, _connection.QuarterPointOffset.Value);
			Assert.AreEqual(tangent, _connection.QuarterPointTangent.Value);
			Assert.AreEqual(offset, _connection.MidpointOffset.Value);
			Assert.AreEqual(tangent, _connection.MidpointTangent.Value);
			Assert.AreEqual(offset, _connection.ThreeQuarterPointOffset.Value);
			Assert.AreEqual(tangent, _connection.ThreeQuarterPointTangent.Value);
		}

		[Test]
		public void AllLevel2NullableProperties_RoundTrip()
		{
			var offset = new Vector2(500f, 600f);
			var tangent = new Vector2(700f, 800f);

			_connection.EighthPointOffset = offset;
			_connection.EighthPointTangent = tangent;
			_connection.ThreeEighthsPointOffset = offset;
			_connection.ThreeEighthsPointTangent = tangent;
			_connection.FiveEighthsPointOffset = offset;
			_connection.FiveEighthsPointTangent = tangent;
			_connection.SevenEighthsPointOffset = offset;
			_connection.SevenEighthsPointTangent = tangent;

			Assert.AreEqual(offset, _connection.EighthPointOffset.Value);
			Assert.AreEqual(tangent, _connection.EighthPointTangent.Value);
			Assert.AreEqual(offset, _connection.ThreeEighthsPointOffset.Value);
			Assert.AreEqual(tangent, _connection.ThreeEighthsPointTangent.Value);
			Assert.AreEqual(offset, _connection.FiveEighthsPointOffset.Value);
			Assert.AreEqual(tangent, _connection.FiveEighthsPointTangent.Value);
			Assert.AreEqual(offset, _connection.SevenEighthsPointOffset.Value);
			Assert.AreEqual(tangent, _connection.SevenEighthsPointTangent.Value);
		}

		[Test]
		public void ClearAllLevel1NullableProperties()
		{
			_connection.QuarterPointOffset = new Vector2(1f, 1f);
			_connection.QuarterPointTangent = new Vector2(2f, 2f);
			_connection.MidpointOffset = new Vector2(3f, 3f);
			_connection.MidpointTangent = new Vector2(4f, 4f);
			_connection.ThreeQuarterPointOffset = new Vector2(5f, 5f);
			_connection.ThreeQuarterPointTangent = new Vector2(6f, 6f);

			_connection.QuarterPointOffset = null;
			_connection.QuarterPointTangent = null;
			_connection.MidpointOffset = null;
			_connection.MidpointTangent = null;
			_connection.ThreeQuarterPointOffset = null;
			_connection.ThreeQuarterPointTangent = null;

			Assert.IsNull(_connection.QuarterPointOffset);
			Assert.IsNull(_connection.QuarterPointTangent);
			Assert.IsNull(_connection.MidpointOffset);
			Assert.IsNull(_connection.MidpointTangent);
			Assert.IsNull(_connection.ThreeQuarterPointOffset);
			Assert.IsNull(_connection.ThreeQuarterPointTangent);
		}

		[Test]
		public void ClearAllLevel2NullableProperties()
		{
			_connection.EighthPointOffset = new Vector2(1f, 1f);
			_connection.EighthPointTangent = new Vector2(2f, 2f);
			_connection.ThreeEighthsPointOffset = new Vector2(3f, 3f);
			_connection.ThreeEighthsPointTangent = new Vector2(4f, 4f);
			_connection.FiveEighthsPointOffset = new Vector2(5f, 5f);
			_connection.FiveEighthsPointTangent = new Vector2(6f, 6f);
			_connection.SevenEighthsPointOffset = new Vector2(7f, 7f);
			_connection.SevenEighthsPointTangent = new Vector2(8f, 8f);

			_connection.EighthPointOffset = null;
			_connection.EighthPointTangent = null;
			_connection.ThreeEighthsPointOffset = null;
			_connection.ThreeEighthsPointTangent = null;
			_connection.FiveEighthsPointOffset = null;
			_connection.FiveEighthsPointTangent = null;
			_connection.SevenEighthsPointOffset = null;
			_connection.SevenEighthsPointTangent = null;

			Assert.IsNull(_connection.EighthPointOffset);
			Assert.IsNull(_connection.EighthPointTangent);
			Assert.IsNull(_connection.ThreeEighthsPointOffset);
			Assert.IsNull(_connection.ThreeEighthsPointTangent);
			Assert.IsNull(_connection.FiveEighthsPointOffset);
			Assert.IsNull(_connection.FiveEighthsPointTangent);
			Assert.IsNull(_connection.SevenEighthsPointOffset);
			Assert.IsNull(_connection.SevenEighthsPointTangent);
		}



		[Test]
		public void DefaultConstructor_GeneratesId()
		{
			Assert.IsNotNull(_connection.Id);
			Assert.IsNotEmpty(_connection.Id);
		}

		[Test]
		public void DefaultConstructor_SetsTransitionType()
		{
			Assert.AreEqual(ConnectionType.Transition, _connection.ConnectionType);
		}

		[Test]
		public void ParameterizedConstructor_SetsAllProperties()
		{
			var connection = new Connection("node1", "portA", "node2", "portB", ConnectionType.Data);

			Assert.IsNotNull(connection.Id);
			Assert.AreEqual("node1", connection.SourceNodeId);
			Assert.AreEqual("portA", connection.SourcePortName);
			Assert.AreEqual("node2", connection.TargetNodeId);
			Assert.AreEqual("portB", connection.TargetPortName);
			Assert.AreEqual(ConnectionType.Data, connection.ConnectionType);
		}

		[Test]
		public void FullConstructor_WithId_UsesProvidedId()
		{
			var connection = new Connection("my-id", "node1", "portA", "node2", "portB");

			Assert.AreEqual("my-id", connection.Id);
		}

		[Test]
		public void FullConstructor_WithNullId_GeneratesId()
		{
			var connection = new Connection(null, "node1", "portA", "node2", "portB");

			Assert.IsNotNull(connection.Id);
			Assert.IsNotEmpty(connection.Id);
		}



		[Test]
		public void IsInstant_ZeroTime_ReturnsTrue()
		{
			_connection.TransitionTime = 0f;

			Assert.IsTrue(_connection.IsInstant);
		}

		[Test]
		public void IsInstant_NegativeTime_ReturnsTrue()
		{
			_connection.TransitionTime = -1f;

			Assert.IsTrue(_connection.IsInstant);
		}

		[Test]
		public void IsInstant_PositiveTime_ReturnsFalse()
		{
			_connection.TransitionTime = 0.5f;

			Assert.IsFalse(_connection.IsInstant);
		}

	}
}
