using System;
using System.Linq;
using NUnit.Framework;
using Nonatomic.VSM.Editor.Blackboard;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class BlackboardTypeEditorRegistryTests
	{
		private enum TestEnum { A, B, C }

		private class UnregisteredClass { }

		[Test]
		public void GetInfo_Int_ReturnsInfo()
		{
			var info = BlackboardTypeEditorRegistry.GetInfo(typeof(int));

			Assert.IsNotNull(info);
			Assert.AreEqual(typeof(int), info.Type);
		}

		[Test]
		public void GetInfo_Float_ReturnsInfo()
		{
			var info = BlackboardTypeEditorRegistry.GetInfo(typeof(float));

			Assert.IsNotNull(info);
			Assert.AreEqual("Float", info.DisplayName);
		}

		[Test]
		public void GetInfo_Bool_ReturnsInfo()
		{
			var info = BlackboardTypeEditorRegistry.GetInfo(typeof(bool));

			Assert.IsNotNull(info);
			Assert.AreEqual("Bool", info.DisplayName);
		}

		[Test]
		public void GetInfo_String_ReturnsInfo()
		{
			var info = BlackboardTypeEditorRegistry.GetInfo(typeof(string));

			Assert.IsNotNull(info);
			Assert.AreEqual("String", info.DisplayName);
		}

		[Test]
		public void GetInfo_Vector2_ReturnsInfoInMathCategory()
		{
			var info = BlackboardTypeEditorRegistry.GetInfo(typeof(Vector2));

			Assert.IsNotNull(info);
			Assert.AreEqual("Math", info.Category);
		}

		[Test]
		public void GetInfo_Vector3_ReturnsInfoInMathCategory()
		{
			var info = BlackboardTypeEditorRegistry.GetInfo(typeof(Vector3));

			Assert.IsNotNull(info);
			Assert.AreEqual("Math", info.Category);
		}

		[Test]
		public void GetInfo_Color_ReturnsInfo()
		{
			var info = BlackboardTypeEditorRegistry.GetInfo(typeof(Color));

			Assert.IsNotNull(info);
			Assert.AreEqual("Color", info.DisplayName);
		}

		[Test]
		public void GetInfo_GameObject_ReturnsInfoInUnityObjectsCategory()
		{
			var info = BlackboardTypeEditorRegistry.GetInfo(typeof(GameObject));

			Assert.IsNotNull(info);
			Assert.AreEqual("Unity Objects", info.Category);
		}

		[Test]
		public void GetInfo_Null_ReturnsNull()
		{
			var info = BlackboardTypeEditorRegistry.GetInfo(null);

			Assert.IsNull(info);
		}

		[Test]
		public void GetInfo_UnregisteredType_ReturnsNull()
		{
			var info = BlackboardTypeEditorRegistry.GetInfo(typeof(UnregisteredClass));

			Assert.IsNull(info);
		}

		[Test]
		public void GetTypeColor_RegisteredType_ReturnsRegisteredColor()
		{
			var info = BlackboardTypeEditorRegistry.GetInfo(typeof(int));
			var color = BlackboardTypeEditorRegistry.GetTypeColor(typeof(int));

			Assert.AreEqual(info.Color, color);
		}

		[Test]
		public void GetTypeColor_UnregisteredUnityObject_ReturnsLightBlue()
		{
			var expected = new Color(0.4f, 0.7f, 1f);
			var color = BlackboardTypeEditorRegistry.GetTypeColor(typeof(Mesh));

			Assert.AreEqual(expected.r, color.r, 0.001f);
			Assert.AreEqual(expected.g, color.g, 0.001f);
			Assert.AreEqual(expected.b, color.b, 0.001f);
		}

		[Test]
		public void GetTypeColor_Enum_ReturnsSoftGreen()
		{
			var expected = new Color(0.6f, 0.8f, 0.6f);
			var color = BlackboardTypeEditorRegistry.GetTypeColor(typeof(TestEnum));

			Assert.AreEqual(expected.r, color.r, 0.001f);
			Assert.AreEqual(expected.g, color.g, 0.001f);
			Assert.AreEqual(expected.b, color.b, 0.001f);
		}

		[Test]
		public void GetTypeColor_UnknownType_ReturnsGray()
		{
			var expected = new Color(0.5f, 0.5f, 0.5f);
			var color = BlackboardTypeEditorRegistry.GetTypeColor(typeof(UnregisteredClass));

			Assert.AreEqual(expected.r, color.r, 0.001f);
			Assert.AreEqual(expected.g, color.g, 0.001f);
			Assert.AreEqual(expected.b, color.b, 0.001f);
		}

		[Test]
		public void GetTypeColor_Null_ReturnsGray()
		{
			var expected = new Color(0.5f, 0.5f, 0.5f);
			var color = BlackboardTypeEditorRegistry.GetTypeColor(null);

			Assert.AreEqual(expected.r, color.r, 0.001f);
			Assert.AreEqual(expected.g, color.g, 0.001f);
			Assert.AreEqual(expected.b, color.b, 0.001f);
		}

		[Test]
		public void GetDisplayName_Null_ReturnsQuestionMark()
		{
			var name = BlackboardTypeEditorRegistry.GetDisplayName(null);

			Assert.AreEqual("?", name);
		}

		[Test]
		public void GetDisplayName_RegisteredType_ReturnsDisplayName()
		{
			var name = BlackboardTypeEditorRegistry.GetDisplayName(typeof(int));

			Assert.AreEqual("Int", name);
		}

		[Test]
		public void GetDisplayName_UnregisteredType_ReturnsTypeName()
		{
			var name = BlackboardTypeEditorRegistry.GetDisplayName(typeof(UnregisteredClass));

			Assert.AreEqual("UnregisteredClass", name);
		}

		[Test]
		public void GetMenuCategories_FiltersOutShowInMenuFalse()
		{
			var categories = BlackboardTypeEditorRegistry.GetMenuCategories();
			var allItems = categories.SelectMany(g => g).ToList();

			Assert.IsFalse(allItems.Any(i => i.Type == typeof(double)),
				"double has ShowInMenu=false and should be excluded");
			Assert.IsFalse(allItems.Any(i => i.Type == typeof(long)),
				"long has ShowInMenu=false and should be excluded");
		}

		[Test]
		public void GetMenuCategories_IncludesShowInMenuTrue()
		{
			var categories = BlackboardTypeEditorRegistry.GetMenuCategories();
			var allItems = categories.SelectMany(g => g).ToList();

			Assert.IsTrue(allItems.Any(i => i.Type == typeof(int)));
			Assert.IsTrue(allItems.Any(i => i.Type == typeof(float)));
			Assert.IsTrue(allItems.Any(i => i.Type == typeof(bool)));
			Assert.IsTrue(allItems.Any(i => i.Type == typeof(string)));
		}

		[Test]
		public void GetMenuCategories_CategoriesAreOrdered()
		{
			var categories = BlackboardTypeEditorRegistry.GetMenuCategories().ToList();
			var categoryNames = categories.Select(g => g.Key).ToList();

			var primitivesIndex = categoryNames.IndexOf("Primitives");
			var mathIndex = categoryNames.IndexOf("Math");
			var unityObjectsIndex = categoryNames.IndexOf("Unity Objects");

			Assert.IsTrue(primitivesIndex < mathIndex,
				"Primitives should come before Math");
			Assert.IsTrue(mathIndex < unityObjectsIndex,
				"Math should come before Unity Objects");
		}

		[Test]
		public void Register_NullInfo_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => BlackboardTypeEditorRegistry.Register(null));
		}

		[Test]
		public void Register_InfoWithNullType_DoesNotThrow()
		{
			var info = new BlackboardTypeEditorInfo { Type = null, DisplayName = "Test" };

			Assert.DoesNotThrow(() => BlackboardTypeEditorRegistry.Register(info));
		}

		[Test]
		public void Register_CustomType_CanBeRetrieved()
		{
			var info = new BlackboardTypeEditorInfo
			{
				Type = typeof(UnregisteredClass),
				DisplayName = "Custom",
				Category = "Custom",
				Order = 0,
				Color = Color.magenta,
				ShowInMenu = false
			};

			BlackboardTypeEditorRegistry.Register(info);
			var retrieved = BlackboardTypeEditorRegistry.GetInfo(typeof(UnregisteredClass));

			Assert.IsNotNull(retrieved);
			Assert.AreEqual("Custom", retrieved.DisplayName);
			Assert.AreEqual(Color.magenta, retrieved.Color);
		}

		[Test]
		public void TypeInfos_ContainsDefaultTypes()
		{
			var typeInfos = BlackboardTypeEditorRegistry.TypeInfos.ToList();

			Assert.IsTrue(typeInfos.Any(i => i.Type == typeof(int)));
			Assert.IsTrue(typeInfos.Any(i => i.Type == typeof(float)));
			Assert.IsTrue(typeInfos.Any(i => i.Type == typeof(bool)));
			Assert.IsTrue(typeInfos.Any(i => i.Type == typeof(string)));
			Assert.IsTrue(typeInfos.Any(i => i.Type == typeof(Vector2)));
			Assert.IsTrue(typeInfos.Any(i => i.Type == typeof(Vector3)));
			Assert.IsTrue(typeInfos.Any(i => i.Type == typeof(Color)));
			Assert.IsTrue(typeInfos.Any(i => i.Type == typeof(GameObject)));
		}

		[Test]
		public void Int_IsInPrimitivesCategory()
		{
			var info = BlackboardTypeEditorRegistry.GetInfo(typeof(int));

			Assert.AreEqual("Primitives", info.Category);
		}

		[Test]
		public void Double_ShowInMenu_IsFalse()
		{
			var info = BlackboardTypeEditorRegistry.GetInfo(typeof(double));

			Assert.IsNotNull(info);
			Assert.IsFalse(info.ShowInMenu);
		}

		[Test]
		public void Int_ShowInMenu_IsTrue()
		{
			var info = BlackboardTypeEditorRegistry.GetInfo(typeof(int));

			Assert.IsTrue(info.ShowInMenu);
		}
	}
}
