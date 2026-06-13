using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class ActiveStateIndicatorTests
	{
		[Test]
		public void ActiveStateIndicator_Construction_IsNotActive()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();

			var indicator = new ActiveStateIndicator(owner, border);

			Assert.IsFalse(indicator.IsActive);
		}

		[Test]
		public void ActiveStateIndicator_SetActiveState_True_BecomesActive()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();
			var indicator = new ActiveStateIndicator(owner, border);

			indicator.SetActiveState(true);

			Assert.IsTrue(indicator.IsActive);
		}

		[Test]
		public void ActiveStateIndicator_SetActiveState_False_BecomesInactive()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();
			var indicator = new ActiveStateIndicator(owner, border);

			indicator.SetActiveState(true);
			indicator.SetActiveState(false);

			Assert.IsFalse(indicator.IsActive);
		}

		[Test]
		public void ActiveStateIndicator_PersistOnDeactivate_StaysActiveOnSetFalse()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();
			var indicator = new ActiveStateIndicator(owner, border, persistOnDeactivate: true);

			indicator.SetActiveState(true);
			indicator.SetActiveState(false);

			Assert.IsTrue(indicator.IsActive);
		}

		[Test]
		public void ActiveStateIndicator_PersistOnDeactivate_ForceDeactivate_BecomesInactive()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();
			var indicator = new ActiveStateIndicator(owner, border, persistOnDeactivate: true);

			indicator.SetActiveState(true);
			indicator.ForceDeactivate();

			Assert.IsFalse(indicator.IsActive);
		}

		[Test]
		public void ActiveStateIndicator_ForceDeactivate_WithoutActivation_DoesNotThrow()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();
			var indicator = new ActiveStateIndicator(owner, border);

			Assert.DoesNotThrow(() => indicator.ForceDeactivate());
		}

		[Test]
		public void ActiveStateIndicator_FlashHighlight_DoesNotThrow()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();
			var indicator = new ActiveStateIndicator(owner, border);

			Assert.DoesNotThrow(() => indicator.FlashHighlight());
		}

		[Test]
		public void ActiveStateIndicator_WithDotContainer_DoesNotThrow()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();
			var dot = new UnityEngine.UIElements.VisualElement();

			Assert.DoesNotThrow(() => new ActiveStateIndicator(owner, border, dot));
		}

		[Test]
		public void ActiveStateIndicator_SetActive_AddsCssClass()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();
			var indicator = new ActiveStateIndicator(owner, border);

			indicator.SetActiveState(true);

			Assert.IsTrue(owner.ClassListContains("state-active"));
		}

		[Test]
		public void ActiveStateIndicator_SetInactive_RemovesCssClass()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();
			var indicator = new ActiveStateIndicator(owner, border);

			indicator.SetActiveState(true);
			indicator.SetActiveState(false);

			Assert.IsFalse(owner.ClassListContains("state-active"));
		}

		[Test]
		public void ActiveStateIndicator_Construction_AddsBorderToContainer()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();

			var indicator = new ActiveStateIndicator(owner, border);

			Assert.AreEqual(1, border.childCount);
		}

		[Test]
		public void ActiveStateIndicator_WithDotContainer_AddsDotToContainer()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();
			var dot = new UnityEngine.UIElements.VisualElement();

			var indicator = new ActiveStateIndicator(owner, border, dot);

			Assert.AreEqual(1, dot.childCount);
		}

		[Test]
		public void ActiveStateIndicator_NoDotContainer_DoesNotAddDot()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();
			var dot = new UnityEngine.UIElements.VisualElement();

			var indicator = new ActiveStateIndicator(owner, border, null);

			Assert.AreEqual(0, dot.childCount);
		}

		[Test]
		public void ActiveStateIndicator_SetActiveState_SameValue_NoStateChange()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();
			var indicator = new ActiveStateIndicator(owner, border);

			indicator.SetActiveState(true);
			indicator.SetActiveState(true);

			Assert.IsTrue(indicator.IsActive);
		}

		[Test]
		public void ActiveStateIndicator_ForceDeactivate_RemovesCssClass()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();
			var indicator = new ActiveStateIndicator(owner, border);

			indicator.SetActiveState(true);
			indicator.ForceDeactivate();

			Assert.IsFalse(owner.ClassListContains("state-active"));
		}

		[Test]
		public void ActiveStateIndicator_FlashHighlight_WhenActive_DoesNotThrow()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();
			var indicator = new ActiveStateIndicator(owner, border);

			indicator.SetActiveState(true);

			Assert.DoesNotThrow(() => indicator.FlashHighlight());
		}

		[Test]
		public void ActiveStateIndicator_FlashHighlight_WithDotContainer_DoesNotThrow()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();
			var dot = new UnityEngine.UIElements.VisualElement();
			var indicator = new ActiveStateIndicator(owner, border, dot);

			Assert.DoesNotThrow(() => indicator.FlashHighlight());
		}

		[Test]
		public void ActiveStateIndicator_WithBorderRadius_CreatesSuccessfully()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();

			var indicator = new ActiveStateIndicator(owner, border, null, borderRadius: 8f);

			Assert.IsNotNull(indicator);
			Assert.AreEqual(1, border.childCount);
		}

		[Test]
		public void ActiveStateIndicator_FullLifecycle_ActivateDeactivateForce()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();
			var dot = new UnityEngine.UIElements.VisualElement();
			var indicator = new ActiveStateIndicator(owner, border, dot);

			Assert.IsFalse(indicator.IsActive);

			indicator.SetActiveState(true);
			Assert.IsTrue(indicator.IsActive);
			Assert.IsTrue(owner.ClassListContains("state-active"));

			indicator.SetActiveState(false);
			Assert.IsFalse(indicator.IsActive);
			Assert.IsFalse(owner.ClassListContains("state-active"));

			indicator.FlashHighlight();
			Assert.IsFalse(indicator.IsActive);

			indicator.SetActiveState(true);
			Assert.IsTrue(indicator.IsActive);

			indicator.ForceDeactivate();
			Assert.IsFalse(indicator.IsActive);
			Assert.IsFalse(owner.ClassListContains("state-active"));
		}

		[Test]
		public void ActiveStateIndicator_PersistOnDeactivate_ForceDeactivate_RemovesCssClass()
		{
			var owner = new UnityEngine.UIElements.VisualElement();
			var border = new UnityEngine.UIElements.VisualElement();
			var indicator = new ActiveStateIndicator(owner, border, persistOnDeactivate: true);

			indicator.SetActiveState(true);
			Assert.IsTrue(owner.ClassListContains("state-active"));

			indicator.ForceDeactivate();
			Assert.IsFalse(owner.ClassListContains("state-active"));
		}
	}
}
