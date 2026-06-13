using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class WireEdgeControlTests
	{
		private WireEdgeControl _control;
		private WireEdgeSettings _settings;

		[SetUp]
		public void SetUp()
		{
			_settings = WireEdgeSettings.Instance;
			_settings.ResetToDefaults();
			_control = new WireEdgeControl(_settings);
		}


		[Test]
		public void Constructor_WithSettings_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => new WireEdgeControl(_settings));
		}

		[Test]
		public void Constructor_Default_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => new WireEdgeControl());
		}

		[Test]
		public void Constructor_NullSettings_UsesInstanceSettings()
		{
			var control = new WireEdgeControl(null);

			Assert.IsNotNull(control);
		}



		[Test]
		public void GetSubdivisionLevel_Default_ReturnsZero()
		{
			Assert.AreEqual(0, _control.GetSubdivisionLevel());
		}

		[Test]
		public void SetSubdivisionLevel_ValidLevel_PersistsValue()
		{
			_control.SetSubdivisionLevel(1);

			Assert.AreEqual(1, _control.GetSubdivisionLevel());
		}

		[Test]
		public void SetSubdivisionLevel_Level2_PersistsValue()
		{
			_control.SetSubdivisionLevel(2);

			Assert.AreEqual(2, _control.GetSubdivisionLevel());
		}

		[Test]
		public void SetSubdivisionLevel_BelowZero_ClampsToZero()
		{
			_control.SetSubdivisionLevel(-1);

			Assert.AreEqual(0, _control.GetSubdivisionLevel());
		}

		[Test]
		public void SetSubdivisionLevel_AboveTwo_ClampsToTwo()
		{
			_control.SetSubdivisionLevel(5);

			Assert.AreEqual(2, _control.GetSubdivisionLevel());
		}



		[Test]
		public void GetMidpointOffset_Default_ReturnsZero()
		{
			Assert.AreEqual(Vector2.zero, _control.GetMidpointOffset());
		}

		[Test]
		public void SetMidpointOffset_PersistsValue()
		{
			var offset = new Vector2(10f, 20f);
			_control.SetMidpointOffset(offset);

			Assert.AreEqual(offset, _control.GetMidpointOffset());
		}

		[Test]
		public void ClearMidpointOffset_ResetsToZero()
		{
			_control.SetMidpointOffset(new Vector2(10f, 20f));
			_control.ClearMidpointOffset();

			Assert.AreEqual(Vector2.zero, _control.GetMidpointOffset());
		}

		[Test]
		public void GetMidpointTangent_Default_ReturnsZero()
		{
			Assert.AreEqual(Vector2.zero, _control.GetMidpointTangent());
		}

		[Test]
		public void SetMidpointTangent_PersistsValue()
		{
			var tangent = new Vector2(30f, 40f);
			_control.SetMidpointTangent(tangent);

			Assert.AreEqual(tangent, _control.GetMidpointTangent());
		}

		[Test]
		public void ClearMidpointTangent_ResetsToZero()
		{
			_control.SetMidpointTangent(new Vector2(30f, 40f));
			_control.ClearMidpointTangent();

			Assert.AreEqual(Vector2.zero, _control.GetMidpointTangent());
		}



		[Test]
		public void GetQuarterPointOffset_Default_ReturnsZero()
		{
			Assert.AreEqual(Vector2.zero, _control.GetQuarterPointOffset());
		}

		[Test]
		public void SetQuarterPointOffset_PersistsValue()
		{
			var offset = new Vector2(5f, 15f);
			_control.SetQuarterPointOffset(offset);

			Assert.AreEqual(offset, _control.GetQuarterPointOffset());
		}

		[Test]
		public void ClearQuarterPointOffset_ResetsToZero()
		{
			_control.SetQuarterPointOffset(new Vector2(5f, 15f));
			_control.ClearQuarterPointOffset();

			Assert.AreEqual(Vector2.zero, _control.GetQuarterPointOffset());
		}

		[Test]
		public void GetQuarterPointTangent_Default_ReturnsZero()
		{
			Assert.AreEqual(Vector2.zero, _control.GetQuarterPointTangent());
		}

		[Test]
		public void SetQuarterPointTangent_PersistsValue()
		{
			var tangent = new Vector2(25f, 35f);
			_control.SetQuarterPointTangent(tangent);

			Assert.AreEqual(tangent, _control.GetQuarterPointTangent());
		}

		[Test]
		public void ClearQuarterPointTangent_ResetsToZero()
		{
			_control.SetQuarterPointTangent(new Vector2(25f, 35f));
			_control.ClearQuarterPointTangent();

			Assert.AreEqual(Vector2.zero, _control.GetQuarterPointTangent());
		}



		[Test]
		public void GetThreeQuarterPointOffset_Default_ReturnsZero()
		{
			Assert.AreEqual(Vector2.zero, _control.GetThreeQuarterPointOffset());
		}

		[Test]
		public void SetThreeQuarterPointOffset_PersistsValue()
		{
			var offset = new Vector2(50f, 60f);
			_control.SetThreeQuarterPointOffset(offset);

			Assert.AreEqual(offset, _control.GetThreeQuarterPointOffset());
		}

		[Test]
		public void ClearThreeQuarterPointOffset_ResetsToZero()
		{
			_control.SetThreeQuarterPointOffset(new Vector2(50f, 60f));
			_control.ClearThreeQuarterPointOffset();

			Assert.AreEqual(Vector2.zero, _control.GetThreeQuarterPointOffset());
		}

		[Test]
		public void GetThreeQuarterPointTangent_Default_ReturnsZero()
		{
			Assert.AreEqual(Vector2.zero, _control.GetThreeQuarterPointTangent());
		}

		[Test]
		public void SetThreeQuarterPointTangent_PersistsValue()
		{
			var tangent = new Vector2(70f, 80f);
			_control.SetThreeQuarterPointTangent(tangent);

			Assert.AreEqual(tangent, _control.GetThreeQuarterPointTangent());
		}

		[Test]
		public void ClearThreeQuarterPointTangent_ResetsToZero()
		{
			_control.SetThreeQuarterPointTangent(new Vector2(70f, 80f));
			_control.ClearThreeQuarterPointTangent();

			Assert.AreEqual(Vector2.zero, _control.GetThreeQuarterPointTangent());
		}



		[Test]
		public void GetEighthPointOffset_Default_ReturnsZero()
		{
			Assert.AreEqual(Vector2.zero, _control.GetEighthPointOffset());
		}

		[Test]
		public void SetEighthPointOffset_PersistsValue()
		{
			var offset = new Vector2(1f, 2f);
			_control.SetEighthPointOffset(offset);

			Assert.AreEqual(offset, _control.GetEighthPointOffset());
		}

		[Test]
		public void ClearEighthPointOffset_ResetsToZero()
		{
			_control.SetEighthPointOffset(new Vector2(1f, 2f));
			_control.ClearEighthPointOffset();

			Assert.AreEqual(Vector2.zero, _control.GetEighthPointOffset());
		}

		[Test]
		public void GetEighthPointTangent_Default_ReturnsZero()
		{
			Assert.AreEqual(Vector2.zero, _control.GetEighthPointTangent());
		}

		[Test]
		public void SetEighthPointTangent_PersistsValue()
		{
			var tangent = new Vector2(3f, 4f);
			_control.SetEighthPointTangent(tangent);

			Assert.AreEqual(tangent, _control.GetEighthPointTangent());
		}

		[Test]
		public void ClearEighthPointTangent_ResetsToZero()
		{
			_control.SetEighthPointTangent(new Vector2(3f, 4f));
			_control.ClearEighthPointTangent();

			Assert.AreEqual(Vector2.zero, _control.GetEighthPointTangent());
		}



		[Test]
		public void GetThreeEighthsPointOffset_Default_ReturnsZero()
		{
			Assert.AreEqual(Vector2.zero, _control.GetThreeEighthsPointOffset());
		}

		[Test]
		public void SetThreeEighthsPointOffset_PersistsValue()
		{
			var offset = new Vector2(5f, 6f);
			_control.SetThreeEighthsPointOffset(offset);

			Assert.AreEqual(offset, _control.GetThreeEighthsPointOffset());
		}

		[Test]
		public void ClearThreeEighthsPointOffset_ResetsToZero()
		{
			_control.SetThreeEighthsPointOffset(new Vector2(5f, 6f));
			_control.ClearThreeEighthsPointOffset();

			Assert.AreEqual(Vector2.zero, _control.GetThreeEighthsPointOffset());
		}

		[Test]
		public void GetThreeEighthsPointTangent_Default_ReturnsZero()
		{
			Assert.AreEqual(Vector2.zero, _control.GetThreeEighthsPointTangent());
		}

		[Test]
		public void SetThreeEighthsPointTangent_PersistsValue()
		{
			var tangent = new Vector2(7f, 8f);
			_control.SetThreeEighthsPointTangent(tangent);

			Assert.AreEqual(tangent, _control.GetThreeEighthsPointTangent());
		}

		[Test]
		public void ClearThreeEighthsPointTangent_ResetsToZero()
		{
			_control.SetThreeEighthsPointTangent(new Vector2(7f, 8f));
			_control.ClearThreeEighthsPointTangent();

			Assert.AreEqual(Vector2.zero, _control.GetThreeEighthsPointTangent());
		}



		[Test]
		public void GetFiveEighthsPointOffset_Default_ReturnsZero()
		{
			Assert.AreEqual(Vector2.zero, _control.GetFiveEighthsPointOffset());
		}

		[Test]
		public void SetFiveEighthsPointOffset_PersistsValue()
		{
			var offset = new Vector2(9f, 10f);
			_control.SetFiveEighthsPointOffset(offset);

			Assert.AreEqual(offset, _control.GetFiveEighthsPointOffset());
		}

		[Test]
		public void ClearFiveEighthsPointOffset_ResetsToZero()
		{
			_control.SetFiveEighthsPointOffset(new Vector2(9f, 10f));
			_control.ClearFiveEighthsPointOffset();

			Assert.AreEqual(Vector2.zero, _control.GetFiveEighthsPointOffset());
		}

		[Test]
		public void GetFiveEighthsPointTangent_Default_ReturnsZero()
		{
			Assert.AreEqual(Vector2.zero, _control.GetFiveEighthsPointTangent());
		}

		[Test]
		public void SetFiveEighthsPointTangent_PersistsValue()
		{
			var tangent = new Vector2(11f, 12f);
			_control.SetFiveEighthsPointTangent(tangent);

			Assert.AreEqual(tangent, _control.GetFiveEighthsPointTangent());
		}

		[Test]
		public void ClearFiveEighthsPointTangent_ResetsToZero()
		{
			_control.SetFiveEighthsPointTangent(new Vector2(11f, 12f));
			_control.ClearFiveEighthsPointTangent();

			Assert.AreEqual(Vector2.zero, _control.GetFiveEighthsPointTangent());
		}



		[Test]
		public void GetSevenEighthsPointOffset_Default_ReturnsZero()
		{
			Assert.AreEqual(Vector2.zero, _control.GetSevenEighthsPointOffset());
		}

		[Test]
		public void SetSevenEighthsPointOffset_PersistsValue()
		{
			var offset = new Vector2(13f, 14f);
			_control.SetSevenEighthsPointOffset(offset);

			Assert.AreEqual(offset, _control.GetSevenEighthsPointOffset());
		}

		[Test]
		public void ClearSevenEighthsPointOffset_ResetsToZero()
		{
			_control.SetSevenEighthsPointOffset(new Vector2(13f, 14f));
			_control.ClearSevenEighthsPointOffset();

			Assert.AreEqual(Vector2.zero, _control.GetSevenEighthsPointOffset());
		}

		[Test]
		public void GetSevenEighthsPointTangent_Default_ReturnsZero()
		{
			Assert.AreEqual(Vector2.zero, _control.GetSevenEighthsPointTangent());
		}

		[Test]
		public void SetSevenEighthsPointTangent_PersistsValue()
		{
			var tangent = new Vector2(15f, 16f);
			_control.SetSevenEighthsPointTangent(tangent);

			Assert.AreEqual(tangent, _control.GetSevenEighthsPointTangent());
		}

		[Test]
		public void ClearSevenEighthsPointTangent_ResetsToZero()
		{
			_control.SetSevenEighthsPointTangent(new Vector2(15f, 16f));
			_control.ClearSevenEighthsPointTangent();

			Assert.AreEqual(Vector2.zero, _control.GetSevenEighthsPointTangent());
		}



		[Test]
		public void HasCustomOffsets_Default_ReturnsFalse()
		{
			Assert.IsFalse(_control.HasCustomOffsets);
		}

		[Test]
		public void HasCustomOffsets_WithMidpointOffset_ReturnsTrue()
		{
			_control.SetMidpointOffset(new Vector2(10f, 10f));

			Assert.IsTrue(_control.HasCustomOffsets);
		}

		[Test]
		public void HasCustomOffsets_WithMidpointTangent_ReturnsTrue()
		{
			_control.SetMidpointTangent(new Vector2(10f, 10f));

			Assert.IsTrue(_control.HasCustomOffsets);
		}

		[Test]
		public void HasCustomOffsets_WithQuarterPointOffset_ReturnsTrue()
		{
			_control.SetQuarterPointOffset(new Vector2(10f, 10f));

			Assert.IsTrue(_control.HasCustomOffsets);
		}

		[Test]
		public void HasCustomOffsets_WithThreeQuarterPointTangent_ReturnsTrue()
		{
			_control.SetThreeQuarterPointTangent(new Vector2(10f, 10f));

			Assert.IsTrue(_control.HasCustomOffsets);
		}

		[Test]
		public void HasCustomOffsets_WithEighthPointOffset_ReturnsTrue()
		{
			_control.SetEighthPointOffset(new Vector2(10f, 10f));

			Assert.IsTrue(_control.HasCustomOffsets);
		}

		[Test]
		public void HasCustomOffsets_WithThreeEighthsPointTangent_ReturnsTrue()
		{
			_control.SetThreeEighthsPointTangent(new Vector2(10f, 10f));

			Assert.IsTrue(_control.HasCustomOffsets);
		}

		[Test]
		public void HasCustomOffsets_WithFiveEighthsPointOffset_ReturnsTrue()
		{
			_control.SetFiveEighthsPointOffset(new Vector2(10f, 10f));

			Assert.IsTrue(_control.HasCustomOffsets);
		}

		[Test]
		public void HasCustomOffsets_WithSevenEighthsPointTangent_ReturnsTrue()
		{
			_control.SetSevenEighthsPointTangent(new Vector2(10f, 10f));

			Assert.IsTrue(_control.HasCustomOffsets);
		}

		[Test]
		public void HasCustomOffsets_SmallOffset_BelowThreshold_ReturnsFalse()
		{
			_control.SetMidpointOffset(new Vector2(0.05f, 0.05f));

			Assert.IsFalse(_control.HasCustomOffsets);
		}



		[Test]
		public void ClearAllOffsets_ClearsLevel1Offsets()
		{
			_control.SetQuarterPointOffset(new Vector2(10f, 10f));
			_control.SetMidpointOffset(new Vector2(20f, 20f));
			_control.SetThreeQuarterPointOffset(new Vector2(30f, 30f));

			_control.ClearAllOffsets();

			Assert.AreEqual(Vector2.zero, _control.GetQuarterPointOffset());
			Assert.AreEqual(Vector2.zero, _control.GetMidpointOffset());
			Assert.AreEqual(Vector2.zero, _control.GetThreeQuarterPointOffset());
		}

		[Test]
		public void ClearAllOffsets_ClearsLevel1Tangents()
		{
			_control.SetQuarterPointTangent(new Vector2(10f, 10f));
			_control.SetMidpointTangent(new Vector2(20f, 20f));
			_control.SetThreeQuarterPointTangent(new Vector2(30f, 30f));

			_control.ClearAllOffsets();

			Assert.AreEqual(Vector2.zero, _control.GetQuarterPointTangent());
			Assert.AreEqual(Vector2.zero, _control.GetMidpointTangent());
			Assert.AreEqual(Vector2.zero, _control.GetThreeQuarterPointTangent());
		}

		[Test]
		public void ClearAllOffsets_ClearsLevel2Offsets()
		{
			_control.SetEighthPointOffset(new Vector2(1f, 1f));
			_control.SetThreeEighthsPointOffset(new Vector2(2f, 2f));
			_control.SetFiveEighthsPointOffset(new Vector2(3f, 3f));
			_control.SetSevenEighthsPointOffset(new Vector2(4f, 4f));

			_control.ClearAllOffsets();

			Assert.AreEqual(Vector2.zero, _control.GetEighthPointOffset());
			Assert.AreEqual(Vector2.zero, _control.GetThreeEighthsPointOffset());
			Assert.AreEqual(Vector2.zero, _control.GetFiveEighthsPointOffset());
			Assert.AreEqual(Vector2.zero, _control.GetSevenEighthsPointOffset());
		}

		[Test]
		public void ClearAllOffsets_ClearsLevel2Tangents()
		{
			_control.SetEighthPointTangent(new Vector2(1f, 1f));
			_control.SetThreeEighthsPointTangent(new Vector2(2f, 2f));
			_control.SetFiveEighthsPointTangent(new Vector2(3f, 3f));
			_control.SetSevenEighthsPointTangent(new Vector2(4f, 4f));

			_control.ClearAllOffsets();

			Assert.AreEqual(Vector2.zero, _control.GetEighthPointTangent());
			Assert.AreEqual(Vector2.zero, _control.GetThreeEighthsPointTangent());
			Assert.AreEqual(Vector2.zero, _control.GetFiveEighthsPointTangent());
			Assert.AreEqual(Vector2.zero, _control.GetSevenEighthsPointTangent());
		}

		[Test]
		public void ClearAllOffsets_MakesHasCustomOffsetsReturnFalse()
		{
			_control.SetMidpointOffset(new Vector2(100f, 100f));
			_control.SetQuarterPointTangent(new Vector2(50f, 50f));
			_control.SetEighthPointOffset(new Vector2(25f, 25f));

			_control.ClearAllOffsets();

			Assert.IsFalse(_control.HasCustomOffsets);
		}



		[Test]
		public void CurvePoints_IsNotNull()
		{
			Assert.IsNotNull(_control.CurvePoints);
		}

		[Test]
		public void CurvePoints_Default_IsEmpty()
		{
			Assert.AreEqual(0, _control.CurvePoints.Count);
		}



		[Test]
		public void MarkDirty_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => _control.MarkDirty());
		}



		[Test]
		public void SetWireColors_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => _control.SetWireColors(Color.red, Color.blue));
		}

	}
}
