using System.Reflection;
using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class StateSearchWindowFilterTests
	{
		private MethodInfo _getBuiltInStateCategory;
		private MethodInfo _formatNamespaceDisplay;
		private MethodInfo _isGenericNamespacePart;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			var type = typeof(StateSearchWindow);
			var flags = BindingFlags.Static | BindingFlags.NonPublic;

			_getBuiltInStateCategory = type.GetMethod("GetBuiltInStateCategory", flags);
			_formatNamespaceDisplay = type.GetMethod("FormatNamespaceDisplay", flags);
			_isGenericNamespacePart = type.GetMethod("IsGenericNamespacePart", flags);

			Assert.IsNotNull(_getBuiltInStateCategory, "GetBuiltInStateCategory method not found");
			Assert.IsNotNull(_formatNamespaceDisplay, "FormatNamespaceDisplay method not found");
			Assert.IsNotNull(_isGenericNamespacePart, "IsGenericNamespacePart method not found");
		}

		private string GetBuiltInStateCategory(string typeName)
		{
			return (string)_getBuiltInStateCategory.Invoke(null, new object[] { typeName });
		}

		private string FormatNamespaceDisplay(string ns)
		{
			return (string)_formatNamespaceDisplay.Invoke(null, new object[] { ns });
		}

		private bool IsGenericNamespacePart(string part)
		{
			return (bool)_isGenericNamespacePart.Invoke(null, new object[] { part });
		}


		[Test]
		public void GetBuiltInStateCategory_ConditionState_ReturnsLogic()
		{
			Assert.AreEqual("Logic", GetBuiltInStateCategory("BoolConditionState"));
		}

		[Test]
		public void GetBuiltInStateCategory_CompareState_ReturnsLogic()
		{
			Assert.AreEqual("Logic", GetBuiltInStateCategory("IntCompareState"));
		}

		[Test]
		public void GetBuiltInStateCategory_RandomBranchState_ReturnsLogic()
		{
			Assert.AreEqual("Logic", GetBuiltInStateCategory("RandomBranchState"));
		}

		[Test]
		public void GetBuiltInStateCategory_RandomOnly_ReturnsOther()
		{
			Assert.AreEqual("Other", GetBuiltInStateCategory("RandomState"));
		}



		[Test]
		public void GetBuiltInStateCategory_DelayState_ReturnsTiming()
		{
			Assert.AreEqual("Timing", GetBuiltInStateCategory("DelayState"));
		}

		[Test]
		public void GetBuiltInStateCategory_WaitState_ReturnsTiming()
		{
			Assert.AreEqual("Timing", GetBuiltInStateCategory("WaitForSecondsState"));
		}



		[Test]
		public void GetBuiltInStateCategory_SpawnState_ReturnsGameObject()
		{
			Assert.AreEqual("GameObject", GetBuiltInStateCategory("SpawnPrefabState"));
		}

		[Test]
		public void GetBuiltInStateCategory_DestroyState_ReturnsGameObject()
		{
			Assert.AreEqual("GameObject", GetBuiltInStateCategory("DestroyObjectState"));
		}

		[Test]
		public void GetBuiltInStateCategory_TransformState_ReturnsGameObject()
		{
			Assert.AreEqual("GameObject", GetBuiltInStateCategory("MoveTransformState"));
		}

		[Test]
		public void GetBuiltInStateCategory_AnimatorState_ReturnsGameObject()
		{
			Assert.AreEqual("GameObject", GetBuiltInStateCategory("SetAnimatorTriggerState"));
		}

		[Test]
		public void GetBuiltInStateCategory_SetActiveState_ReturnsGameObject()
		{
			Assert.AreEqual("GameObject", GetBuiltInStateCategory("SetActiveState"));
		}



		[Test]
		public void GetBuiltInStateCategory_AudioState_ReturnsAudio()
		{
			Assert.AreEqual("Audio", GetBuiltInStateCategory("PlayAudioState"));
		}



		[Test]
		public void GetBuiltInStateCategory_LogState_ReturnsDebug()
		{
			Assert.AreEqual("Debug", GetBuiltInStateCategory("LogMessageState"));
		}

		[Test]
		public void GetBuiltInStateCategory_DebugState_ReturnsDebug()
		{
			Assert.AreEqual("Debug", GetBuiltInStateCategory("DebugBreakState"));
		}

		[Test]
		public void GetBuiltInStateCategory_InvokeState_ReturnsDebug()
		{
			Assert.AreEqual("Debug", GetBuiltInStateCategory("InvokeEventState"));
		}



		[Test]
		public void GetBuiltInStateCategory_UnknownName_ReturnsOther()
		{
			Assert.AreEqual("Other", GetBuiltInStateCategory("MyCustomState"));
		}

		[Test]
		public void GetBuiltInStateCategory_EmptyName_ReturnsOther()
		{
			Assert.AreEqual("Other", GetBuiltInStateCategory(""));
		}

		[Test]
		public void GetBuiltInStateCategory_SimpleState_ReturnsOther()
		{
			Assert.AreEqual("Other", GetBuiltInStateCategory("IdleState"));
		}



		[Test]
		public void FormatNamespaceDisplay_Null_ReturnsCustomStates()
		{
			Assert.AreEqual("Custom States", FormatNamespaceDisplay(null));
		}

		[Test]
		public void FormatNamespaceDisplay_Empty_ReturnsCustomStates()
		{
			Assert.AreEqual("Custom States", FormatNamespaceDisplay(""));
		}

		[Test]
		public void FormatNamespaceDisplay_Global_ReturnsCustomStates()
		{
			Assert.AreEqual("Custom States", FormatNamespaceDisplay("Global"));
		}



		[Test]
		public void FormatNamespaceDisplay_SinglePart_ReturnsThatPart()
		{
			Assert.AreEqual("MyProject", FormatNamespaceDisplay("MyProject"));
		}

		[Test]
		public void FormatNamespaceDisplay_TwoParts_ReturnsLastPart()
		{
			Assert.AreEqual("AI", FormatNamespaceDisplay("MyProject.AI"));
		}

		[Test]
		public void FormatNamespaceDisplay_ThreeParts_ReturnsLastPart()
		{
			Assert.AreEqual("Combat", FormatNamespaceDisplay("MyProject.AI.Combat"));
		}



		[Test]
		public void FormatNamespaceDisplay_LastPartIsStates_ReturnsProjectPart()
		{
			Assert.AreEqual("MyProject", FormatNamespaceDisplay("MyProject.States"));
		}

		[Test]
		public void FormatNamespaceDisplay_LastPartIsCore_ReturnsProjectPart()
		{
			Assert.AreEqual("MyProject", FormatNamespaceDisplay("MyProject.Core"));
		}

		[Test]
		public void FormatNamespaceDisplay_LastPartIsScripts_ReturnsProjectPart()
		{
			Assert.AreEqual("MyProject", FormatNamespaceDisplay("MyProject.Scripts"));
		}

		[Test]
		public void FormatNamespaceDisplay_LastPartIsCommon_ReturnsProjectPart()
		{
			Assert.AreEqual("MyProject", FormatNamespaceDisplay("MyProject.Common"));
		}

		[Test]
		public void FormatNamespaceDisplay_LastPartIsShared_ReturnsProjectPart()
		{
			Assert.AreEqual("MyProject", FormatNamespaceDisplay("MyProject.Shared"));
		}

		[Test]
		public void FormatNamespaceDisplay_LastPartIsBase_ReturnsProjectPart()
		{
			Assert.AreEqual("MyProject", FormatNamespaceDisplay("MyProject.Base"));
		}

		[Test]
		public void FormatNamespaceDisplay_LastPartIsComponents_ReturnsProjectPart()
		{
			Assert.AreEqual("MyProject", FormatNamespaceDisplay("MyProject.Components"));
		}

		[Test]
		public void FormatNamespaceDisplay_LastPartIsBehaviours_ReturnsProjectPart()
		{
			Assert.AreEqual("MyProject", FormatNamespaceDisplay("MyProject.Behaviours"));
		}

		[Test]
		public void FormatNamespaceDisplay_LastPartIsBehaviors_ReturnsProjectPart()
		{
			Assert.AreEqual("MyProject", FormatNamespaceDisplay("MyProject.Behaviors"));
		}



		[Test]
		public void FormatNamespaceDisplay_TwoGenericParts_ReturnsThirdFromEnd()
		{
			Assert.AreEqual("MyProject", FormatNamespaceDisplay("MyProject.Samples.States"));
		}

		[Test]
		public void FormatNamespaceDisplay_ProjectAndGenericParts_GoesDeeper()
		{
			Assert.AreEqual("Combat", FormatNamespaceDisplay("Company.Combat.Core.States"));
		}



		[Test]
		public void FormatNamespaceDisplay_StatesLowercase_ReturnsProjectPart()
		{
			Assert.AreEqual("MyProject", FormatNamespaceDisplay("MyProject.states"));
		}

		[Test]
		public void FormatNamespaceDisplay_StatesUppercase_ReturnsProjectPart()
		{
			Assert.AreEqual("MyProject", FormatNamespaceDisplay("MyProject.STATES"));
		}



		[TestCase("States", true)]
		[TestCase("State", true)]
		[TestCase("Scripts", true)]
		[TestCase("Core", true)]
		[TestCase("Common", true)]
		[TestCase("Shared", true)]
		[TestCase("Base", true)]
		[TestCase("Components", true)]
		[TestCase("Behaviours", true)]
		[TestCase("Behaviors", true)]
		public void IsGenericNamespacePart_GenericParts_ReturnsTrue(string part, bool expected)
		{
			Assert.AreEqual(expected, IsGenericNamespacePart(part));
		}

		[TestCase("MyProject")]
		[TestCase("Combat")]
		[TestCase("AI")]
		[TestCase("Player")]
		[TestCase("Enemy")]
		[TestCase("UI")]
		public void IsGenericNamespacePart_NonGenericParts_ReturnsFalse(string part)
		{
			Assert.IsFalse(IsGenericNamespacePart(part));
		}

		[Test]
		public void IsGenericNamespacePart_CaseInsensitive_States()
		{
			Assert.IsTrue(IsGenericNamespacePart("states"));
			Assert.IsTrue(IsGenericNamespacePart("STATES"));
			Assert.IsTrue(IsGenericNamespacePart("States"));
		}

		[Test]
		public void IsGenericNamespacePart_CaseInsensitive_Core()
		{
			Assert.IsTrue(IsGenericNamespacePart("core"));
			Assert.IsTrue(IsGenericNamespacePart("CORE"));
		}

	}
}
