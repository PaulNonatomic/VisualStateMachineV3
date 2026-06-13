using System;
using System.Reflection;
using NUnit.Framework;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Utils;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class ReflectionCacheTests
	{
		[SetUp]
		public void SetUp()
		{
			ReflectionCache.Clear();
		}


		[Test]
		public void GetFields_ReturnsAllInstanceFields()
		{
			var fields = ReflectionCache.GetFields(typeof(SampleState));

			Assert.IsNotNull(fields);
			Assert.IsTrue(fields.Length > 0);

			var fieldNames = Array.ConvertAll(fields, f => f.Name);
			CollectionAssert.Contains(fieldNames, "OnComplete");
			CollectionAssert.Contains(fieldNames, "InputValue");
			CollectionAssert.Contains(fieldNames, "OutputValue");
			CollectionAssert.Contains(fieldNames, "_privateField");
		}

		[Test]
		public void GetFields_ReturnsSameArrayOnRepeatedCalls()
		{
			var first = ReflectionCache.GetFields(typeof(SampleState));
			var second = ReflectionCache.GetFields(typeof(SampleState));

			Assert.AreSame(first, second);
		}

		[Test]
		public void GetFields_DifferentTypes_ReturnDifferentArrays()
		{
			var sampleFields = ReflectionCache.GetFields(typeof(SampleState));
			var emptyFields = ReflectionCache.GetFields(typeof(EmptyState));

			Assert.AreNotSame(sampleFields, emptyFields);
		}



		[Test]
		public void GetField_ExistingField_ReturnsFieldInfo()
		{
			var field = ReflectionCache.GetField(typeof(SampleState), "OnComplete");

			Assert.IsNotNull(field);
			Assert.AreEqual("OnComplete", field.Name);
			Assert.AreEqual(typeof(Action), field.FieldType);
		}

		[Test]
		public void GetField_PrivateField_ReturnsFieldInfo()
		{
			var field = ReflectionCache.GetField(typeof(SampleState), "_privateField");

			Assert.IsNotNull(field);
			Assert.AreEqual("_privateField", field.Name);
		}

		[Test]
		public void GetField_NonexistentField_ReturnsNull()
		{
			var field = ReflectionCache.GetField(typeof(SampleState), "DoesNotExist");

			Assert.IsNull(field);
		}

		[Test]
		public void GetField_ReturnsSameResultOnRepeatedCalls()
		{
			var first = ReflectionCache.GetField(typeof(SampleState), "OnComplete");
			var second = ReflectionCache.GetField(typeof(SampleState), "OnComplete");

			Assert.AreSame(first, second);
		}

		[Test]
		public void GetField_NullFieldCached_ReturnsNullAgain()
		{
			var first = ReflectionCache.GetField(typeof(SampleState), "Missing");
			var second = ReflectionCache.GetField(typeof(SampleState), "Missing");

			Assert.IsNull(first);
			Assert.IsNull(second);
		}



		[Test]
		public void GetAttribute_FieldWithAttribute_ReturnsAttribute()
		{
			var field = ReflectionCache.GetField(typeof(SampleState), "OnComplete");
			var attr = ReflectionCache.GetAttribute<TransitionAttribute>(field);

			Assert.IsNotNull(attr);
			Assert.AreEqual("Complete", attr.DisplayName);
		}

		[Test]
		public void GetAttribute_FieldWithDataIn_ReturnsAttribute()
		{
			var field = ReflectionCache.GetField(typeof(SampleState), "InputValue");
			var attr = ReflectionCache.GetAttribute<DataInAttribute>(field);

			Assert.IsNotNull(attr);
			Assert.AreEqual("Input", attr.DisplayName);
		}

		[Test]
		public void GetAttribute_FieldWithDataOut_ReturnsAttribute()
		{
			var field = ReflectionCache.GetField(typeof(SampleState), "OutputValue");
			var attr = ReflectionCache.GetAttribute<DataOutAttribute>(field);

			Assert.IsNotNull(attr);
			Assert.AreEqual("Output", attr.DisplayName);
		}

		[Test]
		public void GetAttribute_FieldWithoutAttribute_ReturnsNull()
		{
			var field = ReflectionCache.GetField(typeof(SampleState), "_privateField");
			var attr = ReflectionCache.GetAttribute<TransitionAttribute>(field);

			Assert.IsNull(attr);
		}

		[Test]
		public void GetAttribute_ReturnsSameInstanceOnRepeatedCalls()
		{
			var field = ReflectionCache.GetField(typeof(SampleState), "OnComplete");
			var first = ReflectionCache.GetAttribute<TransitionAttribute>(field);
			var second = ReflectionCache.GetAttribute<TransitionAttribute>(field);

			Assert.AreSame(first, second);
		}

		[Test]
		public void GetAttribute_DifferentAttributeTypes_ReturnCorrectly()
		{
			var field = ReflectionCache.GetField(typeof(SampleState), "OnComplete");

			var transAttr = ReflectionCache.GetAttribute<TransitionAttribute>(field);
			var dataInAttr = ReflectionCache.GetAttribute<DataInAttribute>(field);

			Assert.IsNotNull(transAttr);
			Assert.IsNull(dataInAttr);
		}



		[Test]
		public void GetFieldsWithAttribute_Transition_ReturnsTransitionFields()
		{
			var fields = ReflectionCache.GetFieldsWithAttribute<TransitionAttribute>(typeof(SampleState));

			Assert.AreEqual(1, fields.Length);
			Assert.AreEqual("OnComplete", fields[0].Name);
		}

		[Test]
		public void GetFieldsWithAttribute_DataIn_ReturnsInputFields()
		{
			var fields = ReflectionCache.GetFieldsWithAttribute<DataInAttribute>(typeof(SampleState));

			Assert.AreEqual(1, fields.Length);
			Assert.AreEqual("InputValue", fields[0].Name);
		}

		[Test]
		public void GetFieldsWithAttribute_DataOut_ReturnsOutputFields()
		{
			var fields = ReflectionCache.GetFieldsWithAttribute<DataOutAttribute>(typeof(SampleState));

			Assert.AreEqual(1, fields.Length);
			Assert.AreEqual("OutputValue", fields[0].Name);
		}

		[Test]
		public void GetFieldsWithAttribute_NoMatchingFields_ReturnsEmptyArray()
		{
			var fields = ReflectionCache.GetFieldsWithAttribute<TransitionAttribute>(typeof(EmptyState));

			Assert.IsNotNull(fields);
			Assert.AreEqual(0, fields.Length);
		}

		[Test]
		public void GetFieldsWithAttribute_ReturnsSameArrayOnRepeatedCalls()
		{
			var first = ReflectionCache.GetFieldsWithAttribute<TransitionAttribute>(typeof(SampleState));
			var second = ReflectionCache.GetFieldsWithAttribute<TransitionAttribute>(typeof(SampleState));

			Assert.AreSame(first, second);
		}



		[Test]
		public void Clear_ResetsAllCaches()
		{
			// Populate caches
			var fieldsBefore = ReflectionCache.GetFields(typeof(SampleState));
			var fieldBefore = ReflectionCache.GetField(typeof(SampleState), "OnComplete");
			var attrBefore = ReflectionCache.GetAttribute<TransitionAttribute>(fieldBefore);

			// Clear
			ReflectionCache.Clear();

			// After clear, new calls should produce equivalent but not same-reference arrays
			var fieldsAfter = ReflectionCache.GetFields(typeof(SampleState));
			Assert.AreNotSame(fieldsBefore, fieldsAfter);
			Assert.AreEqual(fieldsBefore.Length, fieldsAfter.Length);
		}



		private class SampleState : Core.State
		{
			[Transition("Complete")]
			public Action OnComplete;

			[DataIn("Input")]
			public float InputValue;

			[DataOut("Output")]
			public string OutputValue;

			private int _privateField;
		}

		private class EmptyState : Core.State
		{
		}

	}
}
