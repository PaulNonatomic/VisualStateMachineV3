using System;
using System.Reflection;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class AttributeTests
	{

		[Test]
		public void DataInAttribute_DefaultConstructor_HasNullDisplayName()
		{
			var attr = new DataInAttribute();

			Assert.IsNull(attr.DisplayName);
		}

		[Test]
		public void DataInAttribute_DefaultConstructor_RequiredIsFalse()
		{
			var attr = new DataInAttribute();

			Assert.IsFalse(attr.Required);
		}

		[Test]
		public void DataInAttribute_DefaultConstructor_TooltipIsNull()
		{
			var attr = new DataInAttribute();

			Assert.IsNull(attr.Tooltip);
		}

		[Test]
		public void DataInAttribute_DisplayNameConstructor_SetsDisplayName()
		{
			var attr = new DataInAttribute("Health");

			Assert.AreEqual("Health", attr.DisplayName);
		}

		[Test]
		public void DataInAttribute_DisplayNameConstructor_RequiredDefaultsFalse()
		{
			var attr = new DataInAttribute("Health");

			Assert.IsFalse(attr.Required);
		}

		[Test]
		public void DataInAttribute_DisplayNameAndRequiredConstructor_SetsBothValues()
		{
			var attr = new DataInAttribute("Health", true);

			Assert.AreEqual("Health", attr.DisplayName);
			Assert.IsTrue(attr.Required);
		}

		[Test]
		public void DataInAttribute_DisplayNameAndRequiredFalse_SetsCorrectly()
		{
			var attr = new DataInAttribute("Speed", false);

			Assert.AreEqual("Speed", attr.DisplayName);
			Assert.IsFalse(attr.Required);
		}

		[Test]
		public void DataInAttribute_TooltipProperty_CanBeSet()
		{
			var attr = new DataInAttribute { Tooltip = "Input health value" };

			Assert.AreEqual("Input health value", attr.Tooltip);
		}

		[Test]
		public void DataInAttribute_AttributeUsage_TargetsFields()
		{
			var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
				typeof(DataInAttribute), typeof(AttributeUsageAttribute));

			Assert.IsNotNull(usage);
			Assert.IsTrue(usage.ValidOn.HasFlag(AttributeTargets.Field));
		}

		[Test]
		public void DataInAttribute_AttributeUsage_DoesNotAllowMultiple()
		{
			var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
				typeof(DataInAttribute), typeof(AttributeUsageAttribute));

			Assert.IsFalse(usage.AllowMultiple);
		}

		[Test]
		public void DataInAttribute_AttributeUsage_IsInherited()
		{
			var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
				typeof(DataInAttribute), typeof(AttributeUsageAttribute));

			Assert.IsTrue(usage.Inherited);
		}



		[Test]
		public void DataOutAttribute_DefaultConstructor_HasNullDisplayName()
		{
			var attr = new DataOutAttribute();

			Assert.IsNull(attr.DisplayName);
		}

		[Test]
		public void DataOutAttribute_DefaultConstructor_TooltipIsNull()
		{
			var attr = new DataOutAttribute();

			Assert.IsNull(attr.Tooltip);
		}

		[Test]
		public void DataOutAttribute_DisplayNameConstructor_SetsDisplayName()
		{
			var attr = new DataOutAttribute("Result");

			Assert.AreEqual("Result", attr.DisplayName);
		}

		[Test]
		public void DataOutAttribute_TooltipProperty_CanBeSet()
		{
			var attr = new DataOutAttribute { Tooltip = "Outputs the result" };

			Assert.AreEqual("Outputs the result", attr.Tooltip);
		}

		[Test]
		public void DataOutAttribute_AttributeUsage_TargetsFields()
		{
			var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
				typeof(DataOutAttribute), typeof(AttributeUsageAttribute));

			Assert.IsNotNull(usage);
			Assert.IsTrue(usage.ValidOn.HasFlag(AttributeTargets.Field));
		}

		[Test]
		public void DataOutAttribute_AttributeUsage_DoesNotAllowMultiple()
		{
			var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
				typeof(DataOutAttribute), typeof(AttributeUsageAttribute));

			Assert.IsFalse(usage.AllowMultiple);
		}



		[Test]
		public void StateInfoAttribute_DefaultConstructor_TooltipIsNull()
		{
			var attr = new StateInfoAttribute();

			Assert.IsNull(attr.Tooltip);
		}

		[Test]
		public void StateInfoAttribute_DefaultConstructor_DescriptionIsNull()
		{
			var attr = new StateInfoAttribute();

			Assert.IsNull(attr.Description);
		}

		[Test]
		public void StateInfoAttribute_DefaultConstructor_CategoryIsNull()
		{
			var attr = new StateInfoAttribute();

			Assert.IsNull(attr.Category);
		}

		[Test]
		public void StateInfoAttribute_DefaultConstructor_HelpUrlIsNull()
		{
			var attr = new StateInfoAttribute();

			Assert.IsNull(attr.HelpUrl);
		}

		[Test]
		public void StateInfoAttribute_TooltipConstructor_SetsTooltip()
		{
			var attr = new StateInfoAttribute("Delays for a duration");

			Assert.AreEqual("Delays for a duration", attr.Tooltip);
		}

		[Test]
		public void StateInfoAttribute_TooltipConstructor_DescriptionRemainsNull()
		{
			var attr = new StateInfoAttribute("Delays for a duration");

			Assert.IsNull(attr.Description);
		}

		[Test]
		public void StateInfoAttribute_TooltipAndDescriptionConstructor_SetsBothValues()
		{
			var attr = new StateInfoAttribute("Combat mode", "Tank can move and fire with reduced damage.");

			Assert.AreEqual("Combat mode", attr.Tooltip);
			Assert.AreEqual("Tank can move and fire with reduced damage.", attr.Description);
		}

		[Test]
		public void StateInfoAttribute_CategoryProperty_SupportsNestedPaths()
		{
			var attr = new StateInfoAttribute { Category = "Combat/Ranged" };

			Assert.AreEqual("Combat/Ranged", attr.Category);
		}

		[Test]
		public void StateInfoAttribute_HelpUrlProperty_CanBeSet()
		{
			var attr = new StateInfoAttribute { HelpUrl = "https://docs.example.com/delay-state" };

			Assert.AreEqual("https://docs.example.com/delay-state", attr.HelpUrl);
		}

		[Test]
		public void StateInfoAttribute_AttributeUsage_TargetsClasses()
		{
			var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
				typeof(StateInfoAttribute), typeof(AttributeUsageAttribute));

			Assert.IsNotNull(usage);
			Assert.IsTrue(usage.ValidOn.HasFlag(AttributeTargets.Class));
		}

		[Test]
		public void StateInfoAttribute_AttributeUsage_DoesNotAllowMultiple()
		{
			var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
				typeof(StateInfoAttribute), typeof(AttributeUsageAttribute));

			Assert.IsFalse(usage.AllowMultiple);
		}

		[Test]
		public void StateInfoAttribute_AttributeUsage_IsInherited()
		{
			var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
				typeof(StateInfoAttribute), typeof(AttributeUsageAttribute));

			Assert.IsTrue(usage.Inherited);
		}



		[Test]
		public void StateStyleAttribute_DefaultConstructor_ColorIsNull()
		{
			var attr = new StateStyleAttribute();

			Assert.IsNull(attr.Color);
		}

		[Test]
		public void StateStyleAttribute_DefaultConstructor_IconIsNull()
		{
			var attr = new StateStyleAttribute();

			Assert.IsNull(attr.Icon);
		}

		[Test]
		public void StateStyleAttribute_ColorConstructor_SetsColor()
		{
			var attr = new StateStyleAttribute("#4A90D9");

			Assert.AreEqual("#4A90D9", attr.Color);
		}

		[Test]
		public void StateStyleAttribute_ColorConstructor_IconRemainsNull()
		{
			var attr = new StateStyleAttribute("#4A90D9");

			Assert.IsNull(attr.Icon);
		}

		[Test]
		public void StateStyleAttribute_ColorAndIconConstructor_SetsBothValues()
		{
			var attr = new StateStyleAttribute("#4A90D9", "clock");

			Assert.AreEqual("#4A90D9", attr.Color);
			Assert.AreEqual("clock", attr.Icon);
		}

		[Test]
		public void StateStyleAttribute_ColorProperty_AcceptsHexFormat()
		{
			var attr = new StateStyleAttribute { Color = "#FF0000" };

			Assert.AreEqual("#FF0000", attr.Color);
		}

		[Test]
		public void StateStyleAttribute_AttributeUsage_TargetsClasses()
		{
			var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
				typeof(StateStyleAttribute), typeof(AttributeUsageAttribute));

			Assert.IsNotNull(usage);
			Assert.IsTrue(usage.ValidOn.HasFlag(AttributeTargets.Class));
		}



		[Test]
		public void StateColors_Grey_IsValidHex()
		{
			Assert.AreEqual("#444444", StateColors.Grey);
		}

		[Test]
		public void StateColors_Red_IsValidHex()
		{
			Assert.AreEqual("#990e23", StateColors.Red);
		}

		[Test]
		public void StateColors_Orange_IsValidHex()
		{
			Assert.AreEqual("#B06101", StateColors.Orange);
		}

		[Test]
		public void StateColors_LimeGreen_IsValidHex()
		{
			Assert.AreEqual("#6d9111", StateColors.LimeGreen);
		}

		[Test]
		public void StateColors_Green_IsValidHex()
		{
			Assert.AreEqual("#116f1c", StateColors.Green);
		}

		[Test]
		public void StateColors_ForestGreen_IsValidHex()
		{
			Assert.AreEqual("#08704a", StateColors.ForestGreen);
		}

		[Test]
		public void StateColors_Teal_IsValidHex()
		{
			Assert.AreEqual("#066670", StateColors.Teal);
		}

		[Test]
		public void StateColors_LightBlue_IsValidHex()
		{
			Assert.AreEqual("#037091", StateColors.LightBlue);
		}

		[Test]
		public void StateColors_Blue_IsValidHex()
		{
			Assert.AreEqual("#084870", StateColors.Blue);
		}

		[Test]
		public void StateColors_Purple_IsValidHex()
		{
			Assert.AreEqual("#4a0e99", StateColors.Purple);
		}

		[Test]
		public void StateColors_Violet_IsValidHex()
		{
			Assert.AreEqual("#740e99", StateColors.Violet);
		}

		[Test]
		public void StateColors_Pink_IsValidHex()
		{
			Assert.AreEqual("#750b55", StateColors.Pink);
		}

		[Test]
		public void StateColors_Cantera_IsValidHex()
		{
			Assert.AreEqual("#551C25", StateColors.Cantera);
		}

		[Test]
		public void StateColors_Dijon_IsValidHex()
		{
			Assert.AreEqual("#a89d46", StateColors.Dijon);
		}

		[Test]
		public void StateColors_Black_IsValidHex()
		{
			Assert.AreEqual("#000000", StateColors.Black);
		}

		[Test]
		public void StateColors_White_IsValidHex()
		{
			Assert.AreEqual("#ffffff", StateColors.White);
		}

		[Test]
		public void StateColors_GrayAlias_EqualsGrey()
		{
			Assert.AreEqual(StateColors.Grey, StateColors.Gray);
		}

		[Test]
		public void StateColors_YellowAlias_EqualsDijon()
		{
			Assert.AreEqual(StateColors.Dijon, StateColors.Yellow);
		}

		[Test]
		public void StateColors_CyanAlias_EqualsTeal()
		{
			Assert.AreEqual(StateColors.Teal, StateColors.Cyan);
		}

		[Test]
		public void StateColors_IndigoAlias_EqualsPurple()
		{
			Assert.AreEqual(StateColors.Purple, StateColors.Indigo);
		}

		[Test]
		public void StateColors_BrownAlias_EqualsCantera()
		{
			Assert.AreEqual(StateColors.Cantera, StateColors.Brown);
		}

		[Test]
		public void StateColors_AllValues_StartWithHash()
		{
			var fields = typeof(StateColors).GetFields(BindingFlags.Public | BindingFlags.Static);

			foreach (var field in fields)
			{
				var value = (string)field.GetValue(null);
				Assert.IsTrue(value.StartsWith("#"),
					$"StateColors.{field.Name} value '{value}' should start with '#'");
			}
		}

		[Test]
		public void StateColors_AllValues_AreSevenCharacters()
		{
			var fields = typeof(StateColors).GetFields(BindingFlags.Public | BindingFlags.Static);

			foreach (var field in fields)
			{
				var value = (string)field.GetValue(null);
				Assert.AreEqual(7, value.Length,
					$"StateColors.{field.Name} value '{value}' should be 7 characters (#RRGGBB)");
			}
		}



		[Test]
		public void TransitionAttribute_DefaultConstructor_DisplayNameIsNull()
		{
			var attr = new TransitionAttribute();

			Assert.IsNull(attr.DisplayName);
		}

		[Test]
		public void TransitionAttribute_DefaultConstructor_TooltipIsNull()
		{
			var attr = new TransitionAttribute();

			Assert.IsNull(attr.Tooltip);
		}

		[Test]
		public void TransitionAttribute_DefaultConstructor_ColorIsNull()
		{
			var attr = new TransitionAttribute();

			Assert.IsNull(attr.Color);
		}

		[Test]
		public void TransitionAttribute_DefaultConstructor_PriorityIsZero()
		{
			var attr = new TransitionAttribute();

			Assert.AreEqual(0, attr.Priority);
		}

		[Test]
		public void TransitionAttribute_DisplayNameConstructor_SetsDisplayName()
		{
			var attr = new TransitionAttribute("OnComplete");

			Assert.AreEqual("OnComplete", attr.DisplayName);
		}

		[Test]
		public void TransitionAttribute_DisplayNameConstructor_ColorRemainsNull()
		{
			var attr = new TransitionAttribute("OnComplete");

			Assert.IsNull(attr.Color);
		}

		[Test]
		public void TransitionAttribute_DisplayNameAndColorConstructor_SetsBothValues()
		{
			var attr = new TransitionAttribute("OnFail", "#FF0000");

			Assert.AreEqual("OnFail", attr.DisplayName);
			Assert.AreEqual("#FF0000", attr.Color);
		}

		[Test]
		public void TransitionAttribute_PriorityProperty_CanBeSet()
		{
			var attr = new TransitionAttribute { Priority = 5 };

			Assert.AreEqual(5, attr.Priority);
		}

		[Test]
		public void TransitionAttribute_TooltipProperty_CanBeSet()
		{
			var attr = new TransitionAttribute { Tooltip = "Fires when the delay completes" };

			Assert.AreEqual("Fires when the delay completes", attr.Tooltip);
		}

		[Test]
		public void TransitionAttribute_AttributeUsage_TargetsFields()
		{
			var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
				typeof(TransitionAttribute), typeof(AttributeUsageAttribute));

			Assert.IsNotNull(usage);
			Assert.IsTrue(usage.ValidOn.HasFlag(AttributeTargets.Field));
		}

		[Test]
		public void TransitionAttribute_AttributeUsage_DoesNotAllowMultiple()
		{
			var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
				typeof(TransitionAttribute), typeof(AttributeUsageAttribute));

			Assert.IsFalse(usage.AllowMultiple);
		}



		[Test]
		public void NodeIcon_Circle_IsNotNullOrEmpty()
		{
			Assert.IsFalse(string.IsNullOrEmpty(NodeIcon.Circle));
		}

		[Test]
		public void NodeIcon_Clock_IsNotNullOrEmpty()
		{
			Assert.IsFalse(string.IsNullOrEmpty(NodeIcon.Clock));
		}

		[Test]
		public void NodeIcon_Play_IsNotNullOrEmpty()
		{
			Assert.IsFalse(string.IsNullOrEmpty(NodeIcon.Play));
		}

		[Test]
		public void NodeIcon_Trophy_IsNotNullOrEmpty()
		{
			Assert.IsFalse(string.IsNullOrEmpty(NodeIcon.Trophy));
		}

		[Test]
		public void NodeIcon_AllConstants_AreNotNullOrEmpty()
		{
			var fields = typeof(NodeIcon).GetFields(BindingFlags.Public | BindingFlags.Static);

			Assert.IsTrue(fields.Length > 0, "NodeIcon should have public static fields");

			foreach (var field in fields)
			{
				var value = (string)field.GetValue(null);
				Assert.IsFalse(string.IsNullOrEmpty(value),
					$"NodeIcon.{field.Name} should not be null or empty");
			}
		}

		[Test]
		public void NodeIcon_HasExpectedMinimumCount()
		{
			var fields = typeof(NodeIcon).GetFields(BindingFlags.Public | BindingFlags.Static);

			Assert.IsTrue(fields.Length >= 70,
				$"NodeIcon should have at least 70 constants, found {fields.Length}");
		}

		[Test]
		public void NodeIcon_Enter_HasExpectedValue()
		{
			Assert.AreEqual("enter", NodeIcon.Enter);
		}

		[Test]
		public void NodeIcon_Exit_HasExpectedValue()
		{
			Assert.AreEqual("exit", NodeIcon.Exit);
		}

		[Test]
		public void NodeIcon_Warning_HasExpectedValue()
		{
			Assert.AreEqual("warning", NodeIcon.Warning);
		}

	}
}
