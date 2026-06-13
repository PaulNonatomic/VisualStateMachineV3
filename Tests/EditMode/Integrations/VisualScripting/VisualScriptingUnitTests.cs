using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Nonatomic.VSM.VisualScripting.Units;
using Unity.VisualScripting;

namespace Nonatomic.VSM.Tests.VisualScripting
{
	[TestFixture]
	public class CompleteStateUnitAttributeTests
	{
		[Test]
		public void HasUnitTitleAttribute_CompleteState()
		{
			var attr = typeof(CompleteStateUnit).GetCustomAttribute<UnitTitleAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Complete State", attr.title);
		}

		[Test]
		public void HasUnitSurtitleAttribute_VSM()
		{
			var attr = typeof(CompleteStateUnit).GetCustomAttribute<UnitSurtitleAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("VSM", attr.surtitle);
		}

		[Test]
		public void HasUnitCategoryAttribute_VSM()
		{
			var attr = typeof(CompleteStateUnit).GetCustomAttribute<UnitCategory>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("VSM", attr.fullName);
		}

		[Test]
		public void IsSealed()
		{
			Assert.IsTrue(typeof(CompleteStateUnit).IsSealed);
		}

		[Test]
		public void ExtendsUnit()
		{
			Assert.IsTrue(typeof(Unit).IsAssignableFrom(typeof(CompleteStateUnit)));
		}

		[Test]
		public void EnterProperty_HasDoNotSerializeAttribute()
		{
			var prop = typeof(CompleteStateUnit).GetProperty("Enter");

			Assert.IsNotNull(prop);
			Assert.IsNotNull(prop.GetCustomAttribute<DoNotSerializeAttribute>());
		}

		[Test]
		public void EnterProperty_IsControlInputType()
		{
			var prop = typeof(CompleteStateUnit).GetProperty("Enter");

			Assert.IsNotNull(prop);
			Assert.AreEqual(typeof(ControlInput), prop.PropertyType);
		}

		[Test]
		public void EnterProperty_HasPrivateSetter()
		{
			var prop = typeof(CompleteStateUnit).GetProperty("Enter");

			Assert.IsNotNull(prop);
			Assert.IsTrue(prop.GetSetMethod(true)?.IsPrivate ?? false);
		}
	}

	[TestFixture]
	public class GetBlackboardVariableUnitAttributeTests
	{
		[Test]
		public void HasUnitTitleAttribute_GetVSMVariable()
		{
			var attr = typeof(GetBlackboardVariableUnit).GetCustomAttribute<UnitTitleAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Get VSM Variable", attr.title);
		}

		[Test]
		public void HasUnitSurtitleAttribute_VSMBlackboard()
		{
			var attr = typeof(GetBlackboardVariableUnit).GetCustomAttribute<UnitSurtitleAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("VSM Blackboard", attr.surtitle);
		}

		[Test]
		public void HasUnitCategoryAttribute_VSMBlackboard()
		{
			var attr = typeof(GetBlackboardVariableUnit).GetCustomAttribute<UnitCategory>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("VSM/Blackboard", attr.fullName);
		}

		[Test]
		public void IsSealed()
		{
			Assert.IsTrue(typeof(GetBlackboardVariableUnit).IsSealed);
		}

		[Test]
		public void ExtendsUnit()
		{
			Assert.IsTrue(typeof(Unit).IsAssignableFrom(typeof(GetBlackboardVariableUnit)));
		}

		[Test]
		public void KeyProperty_HasDoNotSerializeAttribute()
		{
			var prop = typeof(GetBlackboardVariableUnit).GetProperty("Key");

			Assert.IsNotNull(prop);
			Assert.IsNotNull(prop.GetCustomAttribute<DoNotSerializeAttribute>());
		}

		[Test]
		public void KeyProperty_IsValueInputType()
		{
			var prop = typeof(GetBlackboardVariableUnit).GetProperty("Key");

			Assert.IsNotNull(prop);
			Assert.AreEqual(typeof(ValueInput), prop.PropertyType);
		}

		[Test]
		public void ValueProperty_HasDoNotSerializeAttribute()
		{
			var prop = typeof(GetBlackboardVariableUnit).GetProperty("Value");

			Assert.IsNotNull(prop);
			Assert.IsNotNull(prop.GetCustomAttribute<DoNotSerializeAttribute>());
		}

		[Test]
		public void ValueProperty_HasPortLabelHiddenAttribute()
		{
			var prop = typeof(GetBlackboardVariableUnit).GetProperty("Value");

			Assert.IsNotNull(prop);
			Assert.IsNotNull(prop.GetCustomAttribute<PortLabelHiddenAttribute>());
		}

		[Test]
		public void ValueProperty_IsValueOutputType()
		{
			var prop = typeof(GetBlackboardVariableUnit).GetProperty("Value");

			Assert.IsNotNull(prop);
			Assert.AreEqual(typeof(ValueOutput), prop.PropertyType);
		}
	}

	[TestFixture]
	public class SetBlackboardVariableUnitAttributeTests
	{
		[Test]
		public void HasUnitTitleAttribute_SetVSMVariable()
		{
			var attr = typeof(SetBlackboardVariableUnit).GetCustomAttribute<UnitTitleAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("Set VSM Variable", attr.title);
		}

		[Test]
		public void HasUnitSurtitleAttribute_VSMBlackboard()
		{
			var attr = typeof(SetBlackboardVariableUnit).GetCustomAttribute<UnitSurtitleAttribute>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("VSM Blackboard", attr.surtitle);
		}

		[Test]
		public void HasUnitCategoryAttribute_VSMBlackboard()
		{
			var attr = typeof(SetBlackboardVariableUnit).GetCustomAttribute<UnitCategory>();

			Assert.IsNotNull(attr);
			Assert.AreEqual("VSM/Blackboard", attr.fullName);
		}

		[Test]
		public void IsSealed()
		{
			Assert.IsTrue(typeof(SetBlackboardVariableUnit).IsSealed);
		}

		[Test]
		public void ExtendsUnit()
		{
			Assert.IsTrue(typeof(Unit).IsAssignableFrom(typeof(SetBlackboardVariableUnit)));
		}

		[Test]
		public void EnterProperty_HasDoNotSerializeAttribute()
		{
			var prop = typeof(SetBlackboardVariableUnit).GetProperty("Enter");

			Assert.IsNotNull(prop);
			Assert.IsNotNull(prop.GetCustomAttribute<DoNotSerializeAttribute>());
		}

		[Test]
		public void EnterProperty_IsControlInputType()
		{
			var prop = typeof(SetBlackboardVariableUnit).GetProperty("Enter");

			Assert.IsNotNull(prop);
			Assert.AreEqual(typeof(ControlInput), prop.PropertyType);
		}

		[Test]
		public void ExitProperty_HasDoNotSerializeAttribute()
		{
			var prop = typeof(SetBlackboardVariableUnit).GetProperty("Exit");

			Assert.IsNotNull(prop);
			Assert.IsNotNull(prop.GetCustomAttribute<DoNotSerializeAttribute>());
		}

		[Test]
		public void ExitProperty_IsControlOutputType()
		{
			var prop = typeof(SetBlackboardVariableUnit).GetProperty("Exit");

			Assert.IsNotNull(prop);
			Assert.AreEqual(typeof(ControlOutput), prop.PropertyType);
		}

		[Test]
		public void KeyProperty_HasDoNotSerializeAttribute()
		{
			var prop = typeof(SetBlackboardVariableUnit).GetProperty("Key");

			Assert.IsNotNull(prop);
			Assert.IsNotNull(prop.GetCustomAttribute<DoNotSerializeAttribute>());
		}

		[Test]
		public void KeyProperty_IsValueInputType()
		{
			var prop = typeof(SetBlackboardVariableUnit).GetProperty("Key");

			Assert.IsNotNull(prop);
			Assert.AreEqual(typeof(ValueInput), prop.PropertyType);
		}

		[Test]
		public void ValueProperty_HasDoNotSerializeAttribute()
		{
			var prop = typeof(SetBlackboardVariableUnit).GetProperty("Value");

			Assert.IsNotNull(prop);
			Assert.IsNotNull(prop.GetCustomAttribute<DoNotSerializeAttribute>());
		}

		[Test]
		public void ValueProperty_IsValueInputType()
		{
			var prop = typeof(SetBlackboardVariableUnit).GetProperty("Value");

			Assert.IsNotNull(prop);
			Assert.AreEqual(typeof(ValueInput), prop.PropertyType);
		}

		[Test]
		public void HasFourPortProperties()
		{
			var props = typeof(SetBlackboardVariableUnit)
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(p => p.GetCustomAttribute<DoNotSerializeAttribute>() != null)
				.ToArray();

			Assert.AreEqual(4, props.Length);
		}
	}

	[TestFixture]
	public class VisualScriptingStateUtilityTests
	{
		private Type _utilityType;

		[SetUp]
		public void SetUp()
		{
			_utilityType = typeof(CompleteStateUnit).Assembly.GetType(
				"Nonatomic.VSM.VisualScripting.Units.VisualScriptingStateUtility");
		}

		[Test]
		public void TypeExists()
		{
			Assert.IsNotNull(_utilityType);
		}

		[Test]
		public void IsInternalClass()
		{
			Assert.IsTrue(_utilityType.IsNotPublic);
		}

		[Test]
		public void IsStaticClass()
		{
			Assert.IsTrue(_utilityType.IsAbstract && _utilityType.IsSealed);
		}

		[Test]
		public void HasGetStateMethod()
		{
			var method = _utilityType.GetMethod("GetState", BindingFlags.Public | BindingFlags.Static);

			Assert.IsNotNull(method);
		}

		[Test]
		public void GetStateMethod_AcceptsFlowParameter()
		{
			var method = _utilityType.GetMethod("GetState", BindingFlags.Public | BindingFlags.Static);
			var parameters = method.GetParameters();

			Assert.AreEqual(1, parameters.Length);
			Assert.AreEqual(typeof(Flow), parameters[0].ParameterType);
		}

		[Test]
		public void GetStateMethod_ReturnsVisualScriptingState()
		{
			var method = _utilityType.GetMethod("GetState", BindingFlags.Public | BindingFlags.Static);

			Assert.AreEqual(typeof(Nonatomic.VSM.VisualScripting.VisualScriptingState), method.ReturnType);
		}

		[Test]
		public void GetStateMethod_NullFlow_ReturnsNull()
		{
			var method = _utilityType.GetMethod("GetState", BindingFlags.Public | BindingFlags.Static);
			var result = method.Invoke(null, new object[] { null });

			Assert.IsNull(result);
		}
	}
}
