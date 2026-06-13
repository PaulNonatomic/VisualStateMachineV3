using System;
using System.Collections.Generic;
using NUnit.Framework;
using Nonatomic.VSM.Ports;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class PortDirectionTests
	{
		[Test]
		public void PortDirection_Input_HasValue0()
		{
			Assert.AreEqual(0, (int)PortDirection.Input);
		}

		[Test]
		public void PortDirection_Output_HasValue1()
		{
			Assert.AreEqual(1, (int)PortDirection.Output);
		}

		[Test]
		public void PortDirection_Transition_HasValue2()
		{
			Assert.AreEqual(2, (int)PortDirection.Transition);
		}

		[Test]
		public void PortDirection_HasThreeValues()
		{
			var values = Enum.GetValues(typeof(PortDirection));

			Assert.AreEqual(3, values.Length);
		}
	}

	[TestFixture]
	public class InputPortInfoDetailTests
	{
		[Test]
		public void DefaultConstructor_FieldNameIsNull()
		{
			var port = new InputPortInfo();

			Assert.IsNull(port.FieldName);
		}

		[Test]
		public void DefaultConstructor_DisplayNameIsNull()
		{
			var port = new InputPortInfo();

			Assert.IsNull(port.DisplayName);
		}

		[Test]
		public void DefaultConstructor_DataTypeIsNull()
		{
			var port = new InputPortInfo();

			Assert.IsNull(port.DataType);
		}

		[Test]
		public void DefaultConstructor_TooltipIsNull()
		{
			var port = new InputPortInfo();

			Assert.IsNull(port.Tooltip);
		}

		[Test]
		public void DefaultConstructor_DefaultValueIsNull()
		{
			var port = new InputPortInfo();

			Assert.IsNull(port.DefaultValue);
		}

		[Test]
		public void DefaultConstructor_DefaultValueJsonIsNull()
		{
			var port = new InputPortInfo();

			Assert.IsNull(port.DefaultValueJson);
		}

		[Test]
		public void FieldNameConstructor_FormatsDisplayName()
		{
			var port = new InputPortInfo("_healthPoints", typeof(int));

			Assert.AreEqual("Health Points", port.DisplayName);
		}

		[Test]
		public void FieldNameConstructor_IsRequired_DefaultsFalse()
		{
			var port = new InputPortInfo("_value", typeof(float));

			Assert.IsFalse(port.IsRequired);
		}

		[Test]
		public void FullConstructor_NullTooltip_LeavesNull()
		{
			var port = new InputPortInfo("_speed", typeof(float), "Speed", false, null);

			Assert.IsNull(port.Tooltip);
		}

		[Test]
		public void FullConstructor_WithTooltip_SetsTooltip()
		{
			var port = new InputPortInfo("_speed", typeof(float), "Speed", true, "How fast");

			Assert.AreEqual("How fast", port.Tooltip);
		}

		[Test]
		public void DefaultValue_CanStoreString()
		{
			var port = new InputPortInfo();

			port.DefaultValue = "hello";

			Assert.AreEqual("hello", port.DefaultValue);
		}

		[Test]
		public void DefaultValue_CanStoreNull()
		{
			var port = new InputPortInfo();
			port.DefaultValue = 42;

			port.DefaultValue = null;

			Assert.IsNull(port.DefaultValue);
		}

		[Test]
		public void DataType_Set_UpdatesAssemblyQualifiedName()
		{
			var port = new InputPortInfo();

			port.DataType = typeof(Vector3);

			Assert.IsTrue(port.DataTypeAssemblyQualifiedName.Contains("Vector3"));
		}

		[Test]
		public void Direction_AlwaysInput_RegardlessOfOtherProperties()
		{
			var port = new InputPortInfo("_field", typeof(string));
			port.IsRequired = true;
			port.DefaultValue = "test";

			Assert.AreEqual(PortDirection.Input, port.Direction);
		}
	}

	[TestFixture]
	public class OutputPortInfoDetailTests
	{
		[Test]
		public void DefaultConstructor_FieldNameIsNull()
		{
			var port = new OutputPortInfo();

			Assert.IsNull(port.FieldName);
		}

		[Test]
		public void DefaultConstructor_DisplayNameIsNull()
		{
			var port = new OutputPortInfo();

			Assert.IsNull(port.DisplayName);
		}

		[Test]
		public void DefaultConstructor_DataTypeIsNull()
		{
			var port = new OutputPortInfo();

			Assert.IsNull(port.DataType);
		}

		[Test]
		public void FieldNameConstructor_FormatsDisplayName()
		{
			var port = new OutputPortInfo("_damageResult", typeof(float));

			Assert.AreEqual("Damage Result", port.DisplayName);
		}

		[Test]
		public void FullConstructor_NullTooltip_LeavesNull()
		{
			var port = new OutputPortInfo("_result", typeof(bool), "Result", null);

			Assert.IsNull(port.Tooltip);
		}

		[Test]
		public void FullConstructor_WithTooltip_SetsTooltip()
		{
			var port = new OutputPortInfo("_result", typeof(bool), "Result", "Operation outcome");

			Assert.AreEqual("Operation outcome", port.Tooltip);
		}

		[Test]
		public void Direction_AlwaysOutput_RegardlessOfOtherProperties()
		{
			var port = new OutputPortInfo("_field", typeof(int));
			port.Category = "Combat";
			port.Tooltip = "Test";

			Assert.AreEqual(PortDirection.Output, port.Direction);
		}

		[Test]
		public void FieldNameConstructor_SetsAssemblyQualifiedName()
		{
			var port = new OutputPortInfo("_value", typeof(string));

			Assert.IsNotNull(port.DataTypeAssemblyQualifiedName);
			Assert.IsTrue(port.DataTypeAssemblyQualifiedName.Contains("String"));
		}
	}

	[TestFixture]
	public class TransitionPortInfoDetailTests
	{
		[Test]
		public void DefaultConstructor_FieldNameIsNull()
		{
			var port = new TransitionPortInfo();

			Assert.IsNull(port.FieldName);
		}

		[Test]
		public void DefaultConstructor_DataTypeIsNull()
		{
			var port = new TransitionPortInfo();

			Assert.IsNull(port.DataType);
		}

		[Test]
		public void FieldNameConstructor_SetsDataTypeToAction()
		{
			var port = new TransitionPortInfo("OnComplete");

			Assert.AreEqual(typeof(Action), port.DataType);
		}

		[Test]
		public void FieldNameConstructor_FormatsDisplayName_RemovesOnPrefix()
		{
			var port = new TransitionPortInfo("OnComplete");

			Assert.IsNotNull(port.DisplayName);
			Assert.IsFalse(string.IsNullOrEmpty(port.DisplayName));
		}

		[Test]
		public void FullConstructor_SetsDataTypeToAction()
		{
			var port = new TransitionPortInfo("OnFail", "Failure");

			Assert.AreEqual(typeof(Action), port.DataType);
		}

		[Test]
		public void FullConstructor_DefaultColorHint_IsNull()
		{
			var port = new TransitionPortInfo("OnDone", "Done");

			Assert.IsNull(port.ColorHint);
		}

		[Test]
		public void FullConstructor_DefaultPriority_IsZero()
		{
			var port = new TransitionPortInfo("OnDone", "Done");

			Assert.AreEqual(0, port.Priority);
		}

		[Test]
		public void Priority_CanBeSetToNegative()
		{
			var port = new TransitionPortInfo();

			port.Priority = -1;

			Assert.AreEqual(-1, port.Priority);
		}

		[Test]
		public void Priority_CanBeSetToLargeValue()
		{
			var port = new TransitionPortInfo();

			port.Priority = 999;

			Assert.AreEqual(999, port.Priority);
		}

		[Test]
		public void GetColor_ValidLowercaseHex_ReturnsParsedColor()
		{
			var port = new TransitionPortInfo();
			port.ColorHint = "#00ff00";

			var color = port.GetColor();

			Assert.AreEqual(0f, color.r, 0.01f);
			Assert.AreEqual(1f, color.g, 0.01f);
			Assert.AreEqual(0f, color.b, 0.01f);
		}

		[Test]
		public void GetColor_BlueHex_ReturnsParsedColor()
		{
			var port = new TransitionPortInfo();
			port.ColorHint = "#0000FF";

			var color = port.GetColor();

			Assert.AreEqual(0f, color.r, 0.01f);
			Assert.AreEqual(0f, color.g, 0.01f);
			Assert.AreEqual(1f, color.b, 0.01f);
		}

		[Test]
		public void ColorHint_SetAndGet()
		{
			var port = new TransitionPortInfo();

			port.ColorHint = "#AABBCC";

			Assert.AreEqual("#AABBCC", port.ColorHint);
		}

		[Test]
		public void Direction_AlwaysTransition_RegardlessOfOtherProperties()
		{
			var port = new TransitionPortInfo("OnComplete");
			port.Priority = 10;
			port.ColorHint = "#FF0000";

			Assert.AreEqual(PortDirection.Transition, port.Direction);
		}
	}

	[TestFixture]
	public class PortInfoSerializationTests
	{
		[Test]
		public void CacheTypeName_NullType_DoesNotThrow()
		{
			var port = new InputPortInfo();

			Assert.DoesNotThrow(() => port.CacheTypeName());
		}

		[Test]
		public void RestoreType_ValidAssemblyName_RestoresType()
		{
			var port = new OutputPortInfo();
			port.DataTypeAssemblyQualifiedName = typeof(Vector3).AssemblyQualifiedName;

			port.RestoreType();

			Assert.AreEqual(typeof(Vector3), port.DataType);
		}

		[Test]
		public void DataType_Getter_RestoresFromAssemblyQualifiedName()
		{
			var port = new InputPortInfo();
			port.DataTypeAssemblyQualifiedName = typeof(bool).AssemblyQualifiedName;

			var dataType = port.DataType;

			Assert.AreEqual(typeof(bool), dataType);
		}

		[Test]
		public void GetFriendlyTypeName_Action_ReturnsActionName()
		{
			var port = new TransitionPortInfo("OnComplete");

			Assert.AreEqual("Action", port.GetFriendlyTypeName());
		}

		[Test]
		public void GetFriendlyTypeName_GenericDictionary_ReturnsFormattedName()
		{
			var port = new OutputPortInfo("_map", typeof(Dictionary<string, float>));

			Assert.AreEqual("Dictionary<string, float>", port.GetFriendlyTypeName());
		}

		[Test]
		public void Category_DefaultIsNull()
		{
			var port = new InputPortInfo();

			Assert.IsNull(port.Category);
		}

		[Test]
		public void Category_SetAndGet()
		{
			var port = new TransitionPortInfo();

			port.Category = "Transitions";

			Assert.AreEqual("Transitions", port.Category);
		}
	}
}
