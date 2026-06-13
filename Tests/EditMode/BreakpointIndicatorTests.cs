using NUnit.Framework;
using Nonatomic.VSM.Editor.Debugging;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class BreakpointIndicatorTests
	{
		private VisualElement _container;
		private BreakpointIndicator _indicator;

		[SetUp]
		public void SetUp()
		{
			_container = new VisualElement();
			_indicator = new BreakpointIndicator(_container);
		}

		[Test]
		public void Constructor_AddsDotToContainer()
		{
			Assert.AreEqual(1, _container.childCount);
		}

		[Test]
		public void Constructor_DotIsHiddenByDefault()
		{
			Assert.IsFalse(_indicator.IsVisible);
		}

		[Test]
		public void Show_SetsIsVisibleTrue()
		{
			_indicator.Show(enabled: true, conditional: false);

			Assert.IsTrue(_indicator.IsVisible);
		}

		[Test]
		public void Show_Enabled_NonConditional_ShowsDot()
		{
			_indicator.Show(enabled: true, conditional: false);

			var dot = _container[0];
			Assert.AreEqual(DisplayStyle.Flex, dot.style.display.value);
		}

		[Test]
		public void Show_Disabled_ShowsDot()
		{
			_indicator.Show(enabled: false, conditional: false);

			Assert.IsTrue(_indicator.IsVisible);
		}

		[Test]
		public void Show_Conditional_ShowsDot()
		{
			_indicator.Show(enabled: true, conditional: true);

			Assert.IsTrue(_indicator.IsVisible);
		}

		[Test]
		public void Hide_SetsIsVisibleFalse()
		{
			_indicator.Show(enabled: true, conditional: false);
			_indicator.Hide();

			Assert.IsFalse(_indicator.IsVisible);
		}

		[Test]
		public void Hide_WhenAlreadyHidden_RemainsFalse()
		{
			_indicator.Hide();

			Assert.IsFalse(_indicator.IsVisible);
		}

		[Test]
		public void ShowThenHide_CyclesVisibility()
		{
			Assert.IsFalse(_indicator.IsVisible);

			_indicator.Show(enabled: true, conditional: false);
			Assert.IsTrue(_indicator.IsVisible);

			_indicator.Hide();
			Assert.IsFalse(_indicator.IsVisible);

			_indicator.Show(enabled: false, conditional: true);
			Assert.IsTrue(_indicator.IsVisible);
		}

		[Test]
		public void MultipleShowCalls_RemainsVisible()
		{
			_indicator.Show(enabled: true, conditional: false);
			_indicator.Show(enabled: false, conditional: true);
			_indicator.Show(enabled: true, conditional: true);

			Assert.IsTrue(_indicator.IsVisible);
		}
	}
}
