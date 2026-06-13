using System;
using NUnit.Framework;
using Nonatomic.VSM.States;

namespace Nonatomic.VSM.Tests.EditMode.States
{
	[TestFixture]
	public class ConditionStateTests
	{

		[Test]
		public void ConditionState_WhenConditionIsTrue_InvokesOnTrue()
		{
			var state = new ConditionState();
			state.Condition = true;
			var trueCalled = false;
			var falseCalled = false;
			state.OnTrue = () => trueCalled = true;
			state.OnFalse = () => falseCalled = true;

			state.OnEnter();

			Assert.IsTrue(trueCalled, "OnTrue should be called when condition is true");
			Assert.IsFalse(falseCalled, "OnFalse should not be called when condition is true");
		}

		[Test]
		public void ConditionState_WhenConditionIsFalse_InvokesOnFalse()
		{
			var state = new ConditionState();
			state.Condition = false;
			var trueCalled = false;
			var falseCalled = false;
			state.OnTrue = () => trueCalled = true;
			state.OnFalse = () => falseCalled = true;

			state.OnEnter();

			Assert.IsFalse(trueCalled, "OnTrue should not be called when condition is false");
			Assert.IsTrue(falseCalled, "OnFalse should be called when condition is false");
		}

		[Test]
		public void ConditionState_WhenInvertIsTrue_InvertsCondition()
		{
			var state = new ConditionState();
			state.Condition = true;
			state.Invert = true;
			var trueCalled = false;
			var falseCalled = false;
			state.OnTrue = () => trueCalled = true;
			state.OnFalse = () => falseCalled = true;

			state.OnEnter();

			Assert.IsFalse(trueCalled, "OnTrue should not be called when condition is inverted from true");
			Assert.IsTrue(falseCalled, "OnFalse should be called when condition is inverted from true");
		}

		[Test]
		public void ConditionState_WhenInvertIsTrueAndConditionIsFalse_InvertsToTrue()
		{
			var state = new ConditionState();
			state.Condition = false;
			state.Invert = true;
			var trueCalled = false;
			var falseCalled = false;
			state.OnTrue = () => trueCalled = true;
			state.OnFalse = () => falseCalled = true;

			state.OnEnter();

			Assert.IsTrue(trueCalled, "OnTrue should be called when condition is inverted from false");
			Assert.IsFalse(falseCalled, "OnFalse should not be called when condition is inverted from false");
		}

		[Test]
		public void ConditionState_SetsResultOutput_ToEvaluatedValue()
		{
			var state = new ConditionState();
			state.Condition = true;
			state.Invert = false;

			state.OnEnter();

			Assert.IsTrue(state.Result, "Result should be true when condition is true and not inverted");
		}

		[Test]
		public void ConditionState_SetsResultOutput_ToInvertedValue()
		{
			var state = new ConditionState();
			state.Condition = true;
			state.Invert = true;

			state.OnEnter();

			Assert.IsFalse(state.Result, "Result should be false when condition is true but inverted");
		}

		[Test]
		public void ConditionState_WithNullTransitions_DoesNotThrow()
		{
			var state = new ConditionState();
			state.Condition = true;
			state.OnTrue = null;
			state.OnFalse = null;

			Assert.DoesNotThrow(() => state.OnEnter());
		}

	}

	[TestFixture]
	public class CompareStateTests
	{

		[Test]
		public void CompareState_Equal_WhenValuesAreEqual_ReturnsTrue()
		{
			var state = new CompareState();
			state.ValueA = 5f;
			state.ValueB = 5f;
			state.Operator = ComparisonOperator.Equal;
			state.Tolerance = 0.0001f;
			var trueCalled = false;
			state.OnTrue = () => trueCalled = true;

			state.OnEnter();

			Assert.IsTrue(trueCalled, "OnTrue should be called when values are equal");
			Assert.IsTrue(state.Result, "Result should be true");
		}

		[Test]
		public void CompareState_Equal_WhenValuesAreWithinTolerance_ReturnsTrue()
		{
			var state = new CompareState();
			state.ValueA = 5f;
			state.ValueB = 5.00005f;
			state.Operator = ComparisonOperator.Equal;
			state.Tolerance = 0.0001f;
			var trueCalled = false;
			state.OnTrue = () => trueCalled = true;

			state.OnEnter();

			Assert.IsTrue(trueCalled, "OnTrue should be called when values are within tolerance");
		}

		[Test]
		public void CompareState_Equal_WhenValuesAreOutsideTolerance_ReturnsFalse()
		{
			var state = new CompareState();
			state.ValueA = 5f;
			state.ValueB = 5.001f;
			state.Operator = ComparisonOperator.Equal;
			state.Tolerance = 0.0001f;
			var falseCalled = false;
			state.OnFalse = () => falseCalled = true;

			state.OnEnter();

			Assert.IsTrue(falseCalled, "OnFalse should be called when values are outside tolerance");
			Assert.IsFalse(state.Result, "Result should be false");
		}



		[Test]
		public void CompareState_NotEqual_WhenValuesAreDifferent_ReturnsTrue()
		{
			var state = new CompareState();
			state.ValueA = 5f;
			state.ValueB = 10f;
			state.Operator = ComparisonOperator.NotEqual;
			var trueCalled = false;
			state.OnTrue = () => trueCalled = true;

			state.OnEnter();

			Assert.IsTrue(trueCalled, "OnTrue should be called when values are not equal");
			Assert.IsTrue(state.Result);
		}

		[Test]
		public void CompareState_NotEqual_WhenValuesAreEqual_ReturnsFalse()
		{
			var state = new CompareState();
			state.ValueA = 5f;
			state.ValueB = 5f;
			state.Operator = ComparisonOperator.NotEqual;
			var falseCalled = false;
			state.OnFalse = () => falseCalled = true;

			state.OnEnter();

			Assert.IsTrue(falseCalled, "OnFalse should be called when values are equal");
			Assert.IsFalse(state.Result);
		}



		[Test]
		public void CompareState_GreaterThan_WhenAIsGreater_ReturnsTrue()
		{
			var state = new CompareState();
			state.ValueA = 10f;
			state.ValueB = 5f;
			state.Operator = ComparisonOperator.GreaterThan;
			var trueCalled = false;
			state.OnTrue = () => trueCalled = true;

			state.OnEnter();

			Assert.IsTrue(trueCalled);
			Assert.IsTrue(state.Result);
		}

		[Test]
		public void CompareState_GreaterThan_WhenAIsLess_ReturnsFalse()
		{
			var state = new CompareState();
			state.ValueA = 5f;
			state.ValueB = 10f;
			state.Operator = ComparisonOperator.GreaterThan;
			var falseCalled = false;
			state.OnFalse = () => falseCalled = true;

			state.OnEnter();

			Assert.IsTrue(falseCalled);
			Assert.IsFalse(state.Result);
		}

		[Test]
		public void CompareState_GreaterThan_WhenEqual_ReturnsFalse()
		{
			var state = new CompareState();
			state.ValueA = 5f;
			state.ValueB = 5f;
			state.Operator = ComparisonOperator.GreaterThan;
			var falseCalled = false;
			state.OnFalse = () => falseCalled = true;

			state.OnEnter();

			Assert.IsTrue(falseCalled);
			Assert.IsFalse(state.Result);
		}



		[Test]
		public void CompareState_GreaterOrEqual_WhenAIsGreater_ReturnsTrue()
		{
			var state = new CompareState();
			state.ValueA = 10f;
			state.ValueB = 5f;
			state.Operator = ComparisonOperator.GreaterOrEqual;

			state.OnEnter();

			Assert.IsTrue(state.Result);
		}

		[Test]
		public void CompareState_GreaterOrEqual_WhenEqual_ReturnsTrue()
		{
			var state = new CompareState();
			state.ValueA = 5f;
			state.ValueB = 5f;
			state.Operator = ComparisonOperator.GreaterOrEqual;

			state.OnEnter();

			Assert.IsTrue(state.Result);
		}

		[Test]
		public void CompareState_GreaterOrEqual_WhenAIsLess_ReturnsFalse()
		{
			var state = new CompareState();
			state.ValueA = 5f;
			state.ValueB = 10f;
			state.Operator = ComparisonOperator.GreaterOrEqual;

			state.OnEnter();

			Assert.IsFalse(state.Result);
		}



		[Test]
		public void CompareState_LessThan_WhenAIsLess_ReturnsTrue()
		{
			var state = new CompareState();
			state.ValueA = 5f;
			state.ValueB = 10f;
			state.Operator = ComparisonOperator.LessThan;

			state.OnEnter();

			Assert.IsTrue(state.Result);
		}

		[Test]
		public void CompareState_LessThan_WhenAIsGreater_ReturnsFalse()
		{
			var state = new CompareState();
			state.ValueA = 10f;
			state.ValueB = 5f;
			state.Operator = ComparisonOperator.LessThan;

			state.OnEnter();

			Assert.IsFalse(state.Result);
		}

		[Test]
		public void CompareState_LessThan_WhenEqual_ReturnsFalse()
		{
			var state = new CompareState();
			state.ValueA = 5f;
			state.ValueB = 5f;
			state.Operator = ComparisonOperator.LessThan;

			state.OnEnter();

			Assert.IsFalse(state.Result);
		}



		[Test]
		public void CompareState_LessOrEqual_WhenAIsLess_ReturnsTrue()
		{
			var state = new CompareState();
			state.ValueA = 5f;
			state.ValueB = 10f;
			state.Operator = ComparisonOperator.LessOrEqual;

			state.OnEnter();

			Assert.IsTrue(state.Result);
		}

		[Test]
		public void CompareState_LessOrEqual_WhenEqual_ReturnsTrue()
		{
			var state = new CompareState();
			state.ValueA = 5f;
			state.ValueB = 5f;
			state.Operator = ComparisonOperator.LessOrEqual;

			state.OnEnter();

			Assert.IsTrue(state.Result);
		}

		[Test]
		public void CompareState_LessOrEqual_WhenAIsGreater_ReturnsFalse()
		{
			var state = new CompareState();
			state.ValueA = 10f;
			state.ValueB = 5f;
			state.Operator = ComparisonOperator.LessOrEqual;

			state.OnEnter();

			Assert.IsFalse(state.Result);
		}



		[Test]
		public void CompareState_WithNegativeValues_ComparesCorrectly()
		{
			var state = new CompareState();
			state.ValueA = -5f;
			state.ValueB = -10f;
			state.Operator = ComparisonOperator.GreaterThan;

			state.OnEnter();

			Assert.IsTrue(state.Result, "-5 should be greater than -10");
		}

		[Test]
		public void CompareState_WithZeroValues_ComparesCorrectly()
		{
			var state = new CompareState();
			state.ValueA = 0f;
			state.ValueB = 0f;
			state.Operator = ComparisonOperator.Equal;

			state.OnEnter();

			Assert.IsTrue(state.Result);
		}

		[Test]
		public void CompareState_WithNullTransitions_DoesNotThrow()
		{
			var state = new CompareState();
			state.ValueA = 5f;
			state.ValueB = 10f;
			state.Operator = ComparisonOperator.Equal;
			state.OnTrue = null;
			state.OnFalse = null;

			Assert.DoesNotThrow(() => state.OnEnter());
		}

	}

	[TestFixture]
	public class CompareStringStateTests
	{

		[Test]
		public void CompareStringState_WhenStringsAreEqual_InvokesOnEqual()
		{
			var state = new CompareStringState();
			state.ValueA = "Hello";
			state.ValueB = "Hello";
			var equalCalled = false;
			state.OnEqual = () => equalCalled = true;

			state.OnEnter();

			Assert.IsTrue(equalCalled);
			Assert.IsTrue(state.Result);
		}

		[Test]
		public void CompareStringState_WhenStringsAreDifferent_InvokesOnNotEqual()
		{
			var state = new CompareStringState();
			state.ValueA = "Hello";
			state.ValueB = "World";
			var notEqualCalled = false;
			state.OnNotEqual = () => notEqualCalled = true;

			state.OnEnter();

			Assert.IsTrue(notEqualCalled);
			Assert.IsFalse(state.Result);
		}



		[Test]
		public void CompareStringState_WhenCaseDiffers_AndIgnoreCaseIsFalse_ReturnsNotEqual()
		{
			var state = new CompareStringState();
			state.ValueA = "Hello";
			state.ValueB = "hello";
			state.IgnoreCase = false;
			var notEqualCalled = false;
			state.OnNotEqual = () => notEqualCalled = true;

			state.OnEnter();

			Assert.IsTrue(notEqualCalled, "Should be not equal when case differs and IgnoreCase is false");
			Assert.IsFalse(state.Result);
		}

		[Test]
		public void CompareStringState_WhenCaseDiffers_AndIgnoreCaseIsTrue_ReturnsEqual()
		{
			var state = new CompareStringState();
			state.ValueA = "Hello";
			state.ValueB = "hello";
			state.IgnoreCase = true;
			var equalCalled = false;
			state.OnEqual = () => equalCalled = true;

			state.OnEnter();

			Assert.IsTrue(equalCalled, "Should be equal when case differs but IgnoreCase is true");
			Assert.IsTrue(state.Result);
		}

		[Test]
		public void CompareStringState_MixedCase_IgnoreCase_ReturnsEqual()
		{
			var state = new CompareStringState();
			state.ValueA = "HeLLo WoRLd";
			state.ValueB = "hello world";
			state.IgnoreCase = true;

			state.OnEnter();

			Assert.IsTrue(state.Result);
		}



		[Test]
		public void CompareStringState_EmptyStrings_AreEqual()
		{
			var state = new CompareStringState();
			state.ValueA = "";
			state.ValueB = "";

			state.OnEnter();

			Assert.IsTrue(state.Result);
		}

		[Test]
		public void CompareStringState_NullStrings_AreEqual()
		{
			var state = new CompareStringState();
			state.ValueA = null;
			state.ValueB = null;

			state.OnEnter();

			Assert.IsTrue(state.Result);
		}

		[Test]
		public void CompareStringState_NullAndEmpty_AreNotEqual()
		{
			var state = new CompareStringState();
			state.ValueA = null;
			state.ValueB = "";

			state.OnEnter();

			Assert.IsFalse(state.Result);
		}

		[Test]
		public void CompareStringState_WhitespaceStrings_ComparedCorrectly()
		{
			var state = new CompareStringState();
			state.ValueA = "  ";
			state.ValueB = "  ";

			state.OnEnter();

			Assert.IsTrue(state.Result);
		}

		[Test]
		public void CompareStringState_DifferentWhitespace_AreNotEqual()
		{
			var state = new CompareStringState();
			state.ValueA = " ";
			state.ValueB = "  ";

			state.OnEnter();

			Assert.IsFalse(state.Result);
		}

		[Test]
		public void CompareStringState_WithNullTransitions_DoesNotThrow()
		{
			var state = new CompareStringState();
			state.ValueA = "test";
			state.ValueB = "test";
			state.OnEqual = null;
			state.OnNotEqual = null;

			Assert.DoesNotThrow(() => state.OnEnter());
		}

	}
}
