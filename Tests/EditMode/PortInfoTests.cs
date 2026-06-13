using System;
using System.Collections.Generic;
using NUnit.Framework;
using Nonatomic.VSM.Ports;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class PortInfoTests
	{

		[Test]
		public void InputPortInfo_DefaultConstructor_DirectionIsInput()
		{
			var port = new InputPortInfo();

			Assert.AreEqual(PortDirection.Input, port.Direction);
		}

		[Test]
		public void InputPortInfo_FieldNameConstructor_SetsFieldAndType()
		{
			var port = new InputPortInfo("_health", typeof(int));

			Assert.AreEqual("_health", port.FieldName);
			Assert.AreEqual(typeof(int), port.DataType);
		}

		[Test]
		public void InputPortInfo_FieldNameConstructor_GeneratesDisplayName()
		{
			var port = new InputPortInfo("_healthPoints", typeof(int));

			Assert.IsNotNull(port.DisplayName);
			Assert.IsFalse(string.IsNullOrEmpty(port.DisplayName));
		}

		[Test]
		public void InputPortInfo_FullConstructor_SetsAllProperties()
		{
			var port = new InputPortInfo("_speed", typeof(float), "Speed", true, "Movement speed");

			Assert.AreEqual("_speed", port.FieldName);
			Assert.AreEqual(typeof(float), port.DataType);
			Assert.AreEqual("Speed", port.DisplayName);
			Assert.IsTrue(port.IsRequired);
			Assert.AreEqual("Movement speed", port.Tooltip);
		}

		[Test]
		public void InputPortInfo_FullConstructor_NullDisplayName_UsesFormattedFieldName()
		{
			var port = new InputPortInfo("_speed", typeof(float), null, false);

			Assert.IsNotNull(port.DisplayName);
			Assert.IsFalse(string.IsNullOrEmpty(port.DisplayName));
		}

		[Test]
		public void InputPortInfo_IsRequired_DefaultIsFalse()
		{
			var port = new InputPortInfo();

			Assert.IsFalse(port.IsRequired);
		}

		[Test]
		public void InputPortInfo_IsRequired_SetAndGet()
		{
			var port = new InputPortInfo();

			port.IsRequired = true;

			Assert.IsTrue(port.IsRequired);
		}

		[Test]
		public void InputPortInfo_DefaultValueJson_SetAndGet()
		{
			var port = new InputPortInfo();

			port.DefaultValueJson = "{\"value\": 42}";

			Assert.AreEqual("{\"value\": 42}", port.DefaultValueJson);
		}

		[Test]
		public void InputPortInfo_DefaultValue_SetAndGet()
		{
			var port = new InputPortInfo();

			port.DefaultValue = 42;

			Assert.AreEqual(42, port.DefaultValue);
		}



		[Test]
		public void OutputPortInfo_DefaultConstructor_DirectionIsOutput()
		{
			var port = new OutputPortInfo();

			Assert.AreEqual(PortDirection.Output, port.Direction);
		}

		[Test]
		public void OutputPortInfo_FieldNameConstructor_SetsFieldAndType()
		{
			var port = new OutputPortInfo("_result", typeof(string));

			Assert.AreEqual("_result", port.FieldName);
			Assert.AreEqual(typeof(string), port.DataType);
		}

		[Test]
		public void OutputPortInfo_FieldNameConstructor_GeneratesDisplayName()
		{
			var port = new OutputPortInfo("_damageResult", typeof(float));

			Assert.IsNotNull(port.DisplayName);
			Assert.IsFalse(string.IsNullOrEmpty(port.DisplayName));
		}

		[Test]
		public void OutputPortInfo_FullConstructor_SetsAllProperties()
		{
			var port = new OutputPortInfo("_output", typeof(bool), "Output Flag", "Whether the operation succeeded");

			Assert.AreEqual("_output", port.FieldName);
			Assert.AreEqual(typeof(bool), port.DataType);
			Assert.AreEqual("Output Flag", port.DisplayName);
			Assert.AreEqual("Whether the operation succeeded", port.Tooltip);
		}

		[Test]
		public void OutputPortInfo_FullConstructor_NullDisplayName_UsesFormattedFieldName()
		{
			var port = new OutputPortInfo("_output", typeof(bool), null);

			Assert.IsNotNull(port.DisplayName);
			Assert.IsFalse(string.IsNullOrEmpty(port.DisplayName));
		}



		[Test]
		public void TransitionPortInfo_DefaultConstructor_DirectionIsTransition()
		{
			var port = new TransitionPortInfo();

			Assert.AreEqual(PortDirection.Transition, port.Direction);
		}

		[Test]
		public void TransitionPortInfo_FieldNameConstructor_SetsFieldAndActionType()
		{
			var port = new TransitionPortInfo("OnComplete");

			Assert.AreEqual("OnComplete", port.FieldName);
			Assert.AreEqual(typeof(Action), port.DataType);
		}

		[Test]
		public void TransitionPortInfo_FieldNameConstructor_GeneratesDisplayName()
		{
			var port = new TransitionPortInfo("OnComplete");

			Assert.IsNotNull(port.DisplayName);
			Assert.IsFalse(string.IsNullOrEmpty(port.DisplayName));
		}

		[Test]
		public void TransitionPortInfo_FullConstructor_SetsAllProperties()
		{
			var port = new TransitionPortInfo("OnFail", "Failure", "#FF0000", "Triggered on failure", 5);

			Assert.AreEqual("OnFail", port.FieldName);
			Assert.AreEqual(typeof(Action), port.DataType);
			Assert.AreEqual("Failure", port.DisplayName);
			Assert.AreEqual("#FF0000", port.ColorHint);
			Assert.AreEqual("Triggered on failure", port.Tooltip);
			Assert.AreEqual(5, port.Priority);
		}

		[Test]
		public void TransitionPortInfo_FullConstructor_NullDisplayName_UsesFormattedFieldName()
		{
			var port = new TransitionPortInfo("OnComplete", null);

			Assert.IsNotNull(port.DisplayName);
			Assert.IsFalse(string.IsNullOrEmpty(port.DisplayName));
		}

		[Test]
		public void TransitionPortInfo_ColorHint_DefaultIsNull()
		{
			var port = new TransitionPortInfo();

			Assert.IsNull(port.ColorHint);
		}

		[Test]
		public void TransitionPortInfo_Priority_DefaultIsZero()
		{
			var port = new TransitionPortInfo();

			Assert.AreEqual(0, port.Priority);
		}

		[Test]
		public void TransitionPortInfo_GetColor_NullColorHint_ReturnsWhite()
		{
			var port = new TransitionPortInfo();

			Assert.AreEqual(Color.white, port.GetColor());
		}

		[Test]
		public void TransitionPortInfo_GetColor_EmptyColorHint_ReturnsWhite()
		{
			var port = new TransitionPortInfo();
			port.ColorHint = "";

			Assert.AreEqual(Color.white, port.GetColor());
		}

		[Test]
		public void TransitionPortInfo_GetColor_ValidHex_ReturnsParsedColor()
		{
			var port = new TransitionPortInfo();
			port.ColorHint = "#FF0000";

			var color = port.GetColor();

			Assert.AreEqual(1f, color.r, 0.01f);
			Assert.AreEqual(0f, color.g, 0.01f);
			Assert.AreEqual(0f, color.b, 0.01f);
		}

		[Test]
		public void TransitionPortInfo_GetColor_InvalidHex_ReturnsWhite()
		{
			var port = new TransitionPortInfo();
			port.ColorHint = "not-a-color";

			Assert.AreEqual(Color.white, port.GetColor());
		}

		[Test]
		public void TransitionPortInfo_TryGetColor_NullColorHint_ReturnsFalse()
		{
			var port = new TransitionPortInfo();

			Assert.IsFalse(port.TryGetColor(out var color));
			Assert.AreEqual(Color.white, color);
		}

		[Test]
		public void TransitionPortInfo_TryGetColor_EmptyColorHint_ReturnsFalse()
		{
			var port = new TransitionPortInfo();
			port.ColorHint = "";

			Assert.IsFalse(port.TryGetColor(out _));
		}

		[Test]
		public void TransitionPortInfo_TryGetColor_InvalidHex_ReturnsFalse()
		{
			var port = new TransitionPortInfo();
			port.ColorHint = "not-a-color";

			Assert.IsFalse(port.TryGetColor(out _));
		}

		[Test]
		public void TransitionPortInfo_TryGetColor_ValidHex_ReturnsTrueAndParsedColor()
		{
			var port = new TransitionPortInfo();
			port.ColorHint = "#00FF00";

			Assert.IsTrue(port.TryGetColor(out var color));
			Assert.AreEqual(0f, color.r, 0.01f);
			Assert.AreEqual(1f, color.g, 0.01f);
			Assert.AreEqual(0f, color.b, 0.01f);
		}



		[Test]
		public void PortInfo_FieldName_SetAndGet()
		{
			var port = new InputPortInfo();

			port.FieldName = "_testField";

			Assert.AreEqual("_testField", port.FieldName);
		}

		[Test]
		public void PortInfo_DisplayName_SetAndGet()
		{
			var port = new InputPortInfo();

			port.DisplayName = "Test Display";

			Assert.AreEqual("Test Display", port.DisplayName);
		}

		[Test]
		public void PortInfo_Tooltip_SetAndGet()
		{
			var port = new InputPortInfo();

			port.Tooltip = "A helpful tooltip";

			Assert.AreEqual("A helpful tooltip", port.Tooltip);
		}

		[Test]
		public void PortInfo_Category_SetAndGet()
		{
			var port = new OutputPortInfo();

			port.Category = "Combat";

			Assert.AreEqual("Combat", port.Category);
		}

		[Test]
		public void PortInfo_DataType_SetAndGet()
		{
			var port = new InputPortInfo();

			port.DataType = typeof(float);

			Assert.AreEqual(typeof(float), port.DataType);
		}

		[Test]
		public void PortInfo_DataType_SetAlsoSetsAssemblyQualifiedName()
		{
			var port = new InputPortInfo();

			port.DataType = typeof(int);

			Assert.IsNotNull(port.DataTypeAssemblyQualifiedName);
			Assert.IsTrue(port.DataTypeAssemblyQualifiedName.Contains("Int32"));
		}

		[Test]
		public void PortInfo_DataType_SetNull_ClearsAssemblyQualifiedName()
		{
			var port = new InputPortInfo();
			port.DataType = typeof(int);

			port.DataType = null;

			Assert.IsNull(port.DataTypeAssemblyQualifiedName);
		}

		[Test]
		public void PortInfo_CacheTypeName_StoresAssemblyQualifiedName()
		{
			var port = new InputPortInfo();
			port.DataType = typeof(string);

			port.CacheTypeName();

			Assert.IsNotNull(port.DataTypeAssemblyQualifiedName);
			Assert.IsTrue(port.DataTypeAssemblyQualifiedName.Contains("String"));
		}

		[Test]
		public void PortInfo_RestoreType_RestoresFromCachedName()
		{
			var port = new InputPortInfo();
			port.DataType = typeof(float);
			port.CacheTypeName();
			var cachedName = port.DataTypeAssemblyQualifiedName;

			// Simulate deserialization by creating a new port with only the assembly name
			var restoredPort = new InputPortInfo();
			restoredPort.DataTypeAssemblyQualifiedName = cachedName;
			restoredPort.RestoreType();

			Assert.AreEqual(typeof(float), restoredPort.DataType);
		}

		[Test]
		public void PortInfo_RestoreType_EmptyName_DoesNotThrow()
		{
			var port = new InputPortInfo();
			port.DataTypeAssemblyQualifiedName = "";

			Assert.DoesNotThrow(() => port.RestoreType());
			Assert.IsNull(port.DataType);
		}

		[Test]
		public void PortInfo_RestoreType_NullName_DoesNotThrow()
		{
			var port = new InputPortInfo();
			port.DataTypeAssemblyQualifiedName = null;

			Assert.DoesNotThrow(() => port.RestoreType());
		}



		[Test]
		public void GetFriendlyTypeName_Int_ReturnsInt()
		{
			var port = new InputPortInfo("_val", typeof(int));

			Assert.AreEqual("int", port.GetFriendlyTypeName());
		}

		[Test]
		public void GetFriendlyTypeName_Float_ReturnsFloat()
		{
			var port = new InputPortInfo("_val", typeof(float));

			Assert.AreEqual("float", port.GetFriendlyTypeName());
		}

		[Test]
		public void GetFriendlyTypeName_Bool_ReturnsBool()
		{
			var port = new InputPortInfo("_val", typeof(bool));

			Assert.AreEqual("bool", port.GetFriendlyTypeName());
		}

		[Test]
		public void GetFriendlyTypeName_String_ReturnsString()
		{
			var port = new InputPortInfo("_val", typeof(string));

			Assert.AreEqual("string", port.GetFriendlyTypeName());
		}

		[Test]
		public void GetFriendlyTypeName_Double_ReturnsDouble()
		{
			var port = new InputPortInfo("_val", typeof(double));

			Assert.AreEqual("double", port.GetFriendlyTypeName());
		}

		[Test]
		public void GetFriendlyTypeName_Long_ReturnsLong()
		{
			var port = new InputPortInfo("_val", typeof(long));

			Assert.AreEqual("long", port.GetFriendlyTypeName());
		}

		[Test]
		public void GetFriendlyTypeName_NullType_ReturnsUnknown()
		{
			var port = new InputPortInfo();

			Assert.AreEqual("Unknown", port.GetFriendlyTypeName());
		}

		[Test]
		public void GetFriendlyTypeName_CustomClass_ReturnsClassName()
		{
			var port = new InputPortInfo("_val", typeof(Vector3));

			Assert.AreEqual("Vector3", port.GetFriendlyTypeName());
		}

		[Test]
		public void GetFriendlyTypeName_GenericType_ReturnsFormattedName()
		{
			var port = new InputPortInfo("_val", typeof(List<int>));

			Assert.AreEqual("List<int>", port.GetFriendlyTypeName());
		}

		[Test]
		public void GetFriendlyTypeName_GenericWithCustomType_ReturnsFormattedName()
		{
			var port = new InputPortInfo("_val", typeof(List<Vector3>));

			Assert.AreEqual("List<Vector3>", port.GetFriendlyTypeName());
		}

		[Test]
		public void GetFriendlyTypeName_Dictionary_ReturnsFormattedName()
		{
			var port = new InputPortInfo("_val", typeof(Dictionary<string, int>));

			Assert.AreEqual("Dictionary<string, int>", port.GetFriendlyTypeName());
		}

	}
}
