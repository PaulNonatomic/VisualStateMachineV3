using System;
using NUnit.Framework;
using Nonatomic.VSM.Utils;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class ActionTypeUtilsTests
	{
		[Test]
		public void IsActionType_NullType_ReturnsFalse()
		{
			Assert.IsFalse(ActionTypeUtils.IsActionType(null));
		}

		[Test]
		public void IsActionType_NonGenericAction_ReturnsTrue()
		{
			Assert.IsTrue(ActionTypeUtils.IsActionType(typeof(Action)));
		}

		[Test]
		public void IsActionType_GenericActionWithOneParam_ReturnsTrue()
		{
			Assert.IsTrue(ActionTypeUtils.IsActionType(typeof(Action<int>)));
		}

		[Test]
		public void IsActionType_GenericActionWithTwoParams_ReturnsTrue()
		{
			Assert.IsTrue(ActionTypeUtils.IsActionType(typeof(Action<int, string>)));
		}

		[Test]
		public void IsActionType_GenericActionWithThreeParams_ReturnsTrue()
		{
			Assert.IsTrue(ActionTypeUtils.IsActionType(typeof(Action<int, string, bool>)));
		}

		[Test]
		public void IsActionType_GenericActionWithFourParams_ReturnsTrue()
		{
			Assert.IsTrue(ActionTypeUtils.IsActionType(typeof(Action<int, string, bool, float>)));
		}

		[Test]
		public void IsActionType_StringType_ReturnsFalse()
		{
			Assert.IsFalse(ActionTypeUtils.IsActionType(typeof(string)));
		}

		[Test]
		public void IsActionType_IntType_ReturnsFalse()
		{
			Assert.IsFalse(ActionTypeUtils.IsActionType(typeof(int)));
		}

		[Test]
		public void IsActionType_FuncType_ReturnsFalse()
		{
			Assert.IsFalse(ActionTypeUtils.IsActionType(typeof(Func<int>)));
		}

		[Test]
		public void IsActionType_GenericFuncWithParams_ReturnsFalse()
		{
			Assert.IsFalse(ActionTypeUtils.IsActionType(typeof(Func<int, string>)));
		}

		[Test]
		public void IsActionType_ObjectType_ReturnsFalse()
		{
			Assert.IsFalse(ActionTypeUtils.IsActionType(typeof(object)));
		}

		[Test]
		public void IsActionType_DelegateBaseType_ReturnsFalse()
		{
			Assert.IsFalse(ActionTypeUtils.IsActionType(typeof(Delegate)));
		}
	}
}
