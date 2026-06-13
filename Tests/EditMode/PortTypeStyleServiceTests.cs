using System;
using NUnit.Framework;
using Nonatomic.VSM.Editor.Ports;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class PortTypeStyleServiceTests
	{
		private enum TestEnum { A, B }

		[Test]
		public void GetTypeColor_Null_ReturnsDefaultGray()
		{
			var color = PortTypeStyleService.GetTypeColor(null);

			Assert.AreEqual(0.6f, color.r, 0.001f);
			Assert.AreEqual(0.6f, color.g, 0.001f);
			Assert.AreEqual(0.6f, color.b, 0.001f);
		}

		[Test]
		public void GetTypeColor_Action_ReturnsTransitionColor()
		{
			var expected = PortTypeStyleService.GetTransitionColor();
			var color = PortTypeStyleService.GetTypeColor(typeof(Action));

			Assert.AreEqual(expected, color);
		}

		[Test]
		public void GetTypeColor_GenericAction_ReturnsTransitionColor()
		{
			var expected = PortTypeStyleService.GetTransitionColor();
			var color = PortTypeStyleService.GetTypeColor(typeof(Action<int>));

			Assert.AreEqual(expected, color);
		}

		[Test]
		public void GetTypeColor_Int_ReturnsRegistryColor()
		{
			var color = PortTypeStyleService.GetTypeColor(typeof(int));

			Assert.AreNotEqual(new Color(0.6f, 0.6f, 0.6f), color,
				"Should return a specific color, not the default gray");
		}

		[Test]
		public void GetTransitionColor_ReturnsLightGray()
		{
			var color = PortTypeStyleService.GetTransitionColor();

			Assert.AreEqual(0.8f, color.r, 0.001f);
			Assert.AreEqual(0.8f, color.g, 0.001f);
			Assert.AreEqual(0.8f, color.b, 0.001f);
		}

		[Test]
		public void GetFlowColor_ReturnsWhiteish()
		{
			var color = PortTypeStyleService.GetFlowColor();

			Assert.AreEqual(0.9f, color.r, 0.001f);
			Assert.AreEqual(0.9f, color.g, 0.001f);
			Assert.AreEqual(0.9f, color.b, 0.001f);
		}

		[Test]
		public void GetTypeCssClass_Null_ReturnsObject()
		{
			var cssClass = PortTypeStyleService.GetTypeCssClass(null);

			Assert.AreEqual("port-type-object", cssClass);
		}

		[Test]
		public void GetTypeCssClass_Int_ReturnsIntClass()
		{
			var cssClass = PortTypeStyleService.GetTypeCssClass(typeof(int));

			Assert.AreEqual("port-type-int", cssClass);
		}

		[Test]
		public void GetTypeCssClass_Long_ReturnsIntClass()
		{
			var cssClass = PortTypeStyleService.GetTypeCssClass(typeof(long));

			Assert.AreEqual("port-type-int", cssClass);
		}

		[Test]
		public void GetTypeCssClass_Float_ReturnsFloatClass()
		{
			var cssClass = PortTypeStyleService.GetTypeCssClass(typeof(float));

			Assert.AreEqual("port-type-float", cssClass);
		}

		[Test]
		public void GetTypeCssClass_Double_ReturnsFloatClass()
		{
			var cssClass = PortTypeStyleService.GetTypeCssClass(typeof(double));

			Assert.AreEqual("port-type-float", cssClass);
		}

		[Test]
		public void GetTypeCssClass_Bool_ReturnsBoolClass()
		{
			var cssClass = PortTypeStyleService.GetTypeCssClass(typeof(bool));

			Assert.AreEqual("port-type-bool", cssClass);
		}

		[Test]
		public void GetTypeCssClass_String_ReturnsStringClass()
		{
			var cssClass = PortTypeStyleService.GetTypeCssClass(typeof(string));

			Assert.AreEqual("port-type-string", cssClass);
		}

		[Test]
		public void GetTypeCssClass_Vector2_ReturnsVectorClass()
		{
			var cssClass = PortTypeStyleService.GetTypeCssClass(typeof(Vector2));

			Assert.AreEqual("port-type-vector", cssClass);
		}

		[Test]
		public void GetTypeCssClass_Vector3_ReturnsVectorClass()
		{
			var cssClass = PortTypeStyleService.GetTypeCssClass(typeof(Vector3));

			Assert.AreEqual("port-type-vector", cssClass);
		}

		[Test]
		public void GetTypeCssClass_Quaternion_ReturnsVectorClass()
		{
			var cssClass = PortTypeStyleService.GetTypeCssClass(typeof(Quaternion));

			Assert.AreEqual("port-type-vector", cssClass);
		}

		[Test]
		public void GetTypeCssClass_UnknownType_ReturnsObjectClass()
		{
			var cssClass = PortTypeStyleService.GetTypeCssClass(typeof(DateTime));

			Assert.AreEqual("port-type-object", cssClass);
		}

		[Test]
		public void GetTypeIcon_Null_ReturnsNull()
		{
			var icon = PortTypeStyleService.GetTypeIcon(null);

			Assert.IsNull(icon);
		}

		[Test]
		public void GetTypeIcon_Enum_ReturnsNonNull()
		{
			var icon = PortTypeStyleService.GetTypeIcon(typeof(TestEnum));

			// EditorGUIUtility.IconContent may return null in batch mode
			// but should work in interactive editor
			Assert.IsNotNull(icon);
		}
	}
}
