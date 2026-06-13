using System;
using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Editor.Graph.Renderers;
using Nonatomic.VSM.Editor.Graph.Settings;
using Nonatomic.VSM.Graph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class DefaultEdgeRendererTests
	{
		private DefaultEdgeRenderer _renderer;

		[SetUp]
		public void SetUp()
		{
			_renderer = new DefaultEdgeRenderer();
		}

		[Test]
		public void Type_ReturnsDefault()
		{
			Assert.AreEqual(EdgeRendererType.Default, _renderer.Type);
		}

		[Test]
		public void DisplayName_ReturnsDefaultUnity()
		{
			Assert.AreEqual("Default (Unity)", _renderer.DisplayName);
		}

		[Test]
		public void SupportsCurveCustomization_ReturnsFalse()
		{
			Assert.IsFalse(_renderer.SupportsCurveCustomization);
		}

		[Test]
		public void Settings_ReturnsNull()
		{
			Assert.IsNull(_renderer.Settings);
		}

		[Test]
		public void CreateEdgeControl_ReturnsEdgeControl()
		{
			var control = _renderer.CreateEdgeControl();

			Assert.IsNotNull(control);
			Assert.IsInstanceOf<EdgeControl>(control);
		}

		[Test]
		public void CreateEdgeControl_ReturnsNewInstanceEachCall()
		{
			var control1 = _renderer.CreateEdgeControl();
			var control2 = _renderer.CreateEdgeControl();

			Assert.AreNotSame(control1, control2);
		}

		[Test]
		public void ApplyConnectionData_WithNull_DoesNotThrow()
		{
			var control = new EdgeControl();

			Assert.DoesNotThrow(() => _renderer.ApplyConnectionData(control, null));
		}

		[Test]
		public void ApplyConnectionData_WithConnection_DoesNotThrow()
		{
			var control = new EdgeControl();
			var connection = new Connection();

			Assert.DoesNotThrow(() => _renderer.ApplyConnectionData(control, connection));
		}

		[Test]
		public void GetCurvePoints_ReturnsEmpty()
		{
			var control = new EdgeControl();
			var points = _renderer.GetCurvePoints(control);

			Assert.IsNotNull(points);
			Assert.AreEqual(0, points.Count);
		}

		[Test]
		public void ImplementsIEdgeRenderer()
		{
			Assert.IsInstanceOf<IEdgeRenderer>(_renderer);
		}
	}

	[TestFixture]
	public class WireEdgeRendererTests
	{
		private WireEdgeRenderer _renderer;

		[SetUp]
		public void SetUp()
		{
			_renderer = new WireEdgeRenderer();
		}

		[Test]
		public void Type_ReturnsWire()
		{
			Assert.AreEqual(EdgeRendererType.Wire, _renderer.Type);
		}

		[Test]
		public void DisplayName_ReturnsWireBezier()
		{
			Assert.AreEqual("Wire (Bezier)", _renderer.DisplayName);
		}

		[Test]
		public void SupportsCurveCustomization_ReturnsTrue()
		{
			Assert.IsTrue(_renderer.SupportsCurveCustomization);
		}

		[Test]
		public void Settings_ReturnsWireEdgeSettings()
		{
			Assert.IsNotNull(_renderer.Settings);
			Assert.IsInstanceOf<WireEdgeSettings>(_renderer.Settings);
			Assert.AreSame(WireEdgeSettings.Instance, _renderer.Settings);
		}

		[Test]
		public void CreateEdgeControl_ReturnsWireEdgeControl()
		{
			var control = _renderer.CreateEdgeControl();

			Assert.IsNotNull(control);
			Assert.IsInstanceOf<WireEdgeControl>(control);
		}

		[Test]
		public void CreateEdgeControl_ReturnsNewInstanceEachCall()
		{
			var control1 = _renderer.CreateEdgeControl();
			var control2 = _renderer.CreateEdgeControl();

			Assert.AreNotSame(control1, control2);
		}

		[Test]
		public void ApplyConnectionData_NullControl_DoesNotThrow()
		{
			var connection = new Connection();

			Assert.DoesNotThrow(() => _renderer.ApplyConnectionData(null, connection));
		}

		[Test]
		public void ApplyConnectionData_NullConnection_DoesNotThrow()
		{
			var control = new WireEdgeControl();

			Assert.DoesNotThrow(() => _renderer.ApplyConnectionData(control, null));
		}

		[Test]
		public void ApplyConnectionData_NonWireControl_DoesNotThrow()
		{
			var control = new EdgeControl();
			var connection = new Connection();

			Assert.DoesNotThrow(() => _renderer.ApplyConnectionData(control, connection));
		}

		[Test]
		public void ApplyConnectionData_SyncsMidpointOffset()
		{
			var control = new WireEdgeControl();
			var connection = new Connection();
			connection.MidpointOffset = new Vector2(10f, 20f);

			_renderer.ApplyConnectionData(control, connection);

			Assert.AreEqual(new Vector2(10f, 20f), control.GetMidpointOffset());
		}

		[Test]
		public void ApplyConnectionData_NullMidpointOffset_ClearsOffset()
		{
			var control = new WireEdgeControl();
			control.SetMidpointOffset(new Vector2(10f, 20f));
			var connection = new Connection();
			connection.MidpointOffset = null;

			_renderer.ApplyConnectionData(control, connection);

			Assert.AreEqual(Vector2.zero, control.GetMidpointOffset());
		}

		[Test]
		public void ApplyConnectionData_SyncsQuarterPointOffset()
		{
			var control = new WireEdgeControl();
			var connection = new Connection();
			connection.QuarterPointOffset = new Vector2(5f, 15f);

			_renderer.ApplyConnectionData(control, connection);

			Assert.AreEqual(new Vector2(5f, 15f), control.GetQuarterPointOffset());
		}

		[Test]
		public void ApplyConnectionData_SyncsThreeQuarterPointOffset()
		{
			var control = new WireEdgeControl();
			var connection = new Connection();
			connection.ThreeQuarterPointOffset = new Vector2(30f, 40f);

			_renderer.ApplyConnectionData(control, connection);

			Assert.AreEqual(new Vector2(30f, 40f), control.GetThreeQuarterPointOffset());
		}

		[Test]
		public void ApplyConnectionData_SyncsMidpointTangent()
		{
			var control = new WireEdgeControl();
			var connection = new Connection();
			connection.MidpointTangent = new Vector2(50f, 60f);

			_renderer.ApplyConnectionData(control, connection);

			Assert.AreEqual(new Vector2(50f, 60f), control.GetMidpointTangent());
		}

		[Test]
		public void ApplyConnectionData_SyncsSubdivisionLevel()
		{
			var control = new WireEdgeControl();
			var connection = new Connection();
			connection.SubdivisionLevel = 2;

			_renderer.ApplyConnectionData(control, connection);

			Assert.AreEqual(2, control.GetSubdivisionLevel());
		}

		[Test]
		public void ApplyConnectionData_SyncsLevel2Offsets()
		{
			var control = new WireEdgeControl();
			var connection = new Connection();
			connection.EighthPointOffset = new Vector2(1f, 2f);
			connection.ThreeEighthsPointOffset = new Vector2(3f, 4f);
			connection.FiveEighthsPointOffset = new Vector2(5f, 6f);
			connection.SevenEighthsPointOffset = new Vector2(7f, 8f);

			_renderer.ApplyConnectionData(control, connection);

			Assert.AreEqual(new Vector2(1f, 2f), control.GetEighthPointOffset());
			Assert.AreEqual(new Vector2(3f, 4f), control.GetThreeEighthsPointOffset());
			Assert.AreEqual(new Vector2(5f, 6f), control.GetFiveEighthsPointOffset());
			Assert.AreEqual(new Vector2(7f, 8f), control.GetSevenEighthsPointOffset());
		}

		[Test]
		public void ApplyConnectionData_SyncsLevel2Tangents()
		{
			var control = new WireEdgeControl();
			var connection = new Connection();
			connection.EighthPointTangent = new Vector2(10f, 20f);
			connection.ThreeEighthsPointTangent = new Vector2(30f, 40f);
			connection.FiveEighthsPointTangent = new Vector2(50f, 60f);
			connection.SevenEighthsPointTangent = new Vector2(70f, 80f);

			_renderer.ApplyConnectionData(control, connection);

			Assert.AreEqual(new Vector2(10f, 20f), control.GetEighthPointTangent());
			Assert.AreEqual(new Vector2(30f, 40f), control.GetThreeEighthsPointTangent());
			Assert.AreEqual(new Vector2(50f, 60f), control.GetFiveEighthsPointTangent());
			Assert.AreEqual(new Vector2(70f, 80f), control.GetSevenEighthsPointTangent());
		}

		[Test]
		public void GetCurvePoints_WithWireControl_ReturnsCurvePoints()
		{
			var control = new WireEdgeControl();
			var points = _renderer.GetCurvePoints(control);

			Assert.IsNotNull(points);
			Assert.IsEmpty(points);
		}

		[Test]
		public void GetCurvePoints_WithNonWireControl_ReturnsEmpty()
		{
			var control = new EdgeControl();
			var points = _renderer.GetCurvePoints(control);

			Assert.IsNotNull(points);
			Assert.AreEqual(0, points.Count);
		}

		[Test]
		public void ImplementsIEdgeRenderer()
		{
			Assert.IsInstanceOf<IEdgeRenderer>(_renderer);
		}
	}
}
