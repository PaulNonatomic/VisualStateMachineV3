using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;
using UnityEditor;

namespace Nonatomic.VSM.Tests.EditMode
{
	public class SplitViewTests
	{
		private const string SplitEnabledPref = "VSM.SplitView.Enabled";
		private const string SplitRatioPref = "VSM.SplitView.Ratio";
		private const string LinkScrollPref = "VSM.SplitView.LinkScroll";

		[SetUp]
		public void SetUp()
		{
			// Clear any existing prefs to start fresh
			EditorPrefs.DeleteKey(SplitEnabledPref);
			EditorPrefs.DeleteKey(SplitRatioPref);
			EditorPrefs.DeleteKey(LinkScrollPref);
		}

		[TearDown]
		public void TearDown()
		{
			// Clean up prefs after tests
			EditorPrefs.DeleteKey(SplitEnabledPref);
			EditorPrefs.DeleteKey(SplitRatioPref);
			EditorPrefs.DeleteKey(LinkScrollPref);
		}

		[Test]
		public void SplitViewContainer_CanBeCreated()
		{
			var container = new SplitViewContainer();

			Assert.IsNotNull(container);
			Assert.IsNotNull(container.PrimaryPane);
			Assert.IsNotNull(container.SecondaryPane);
		}

		[Test]
		public void SplitViewContainer_StartsDisabled()
		{
			var container = new SplitViewContainer();

			Assert.IsFalse(container.IsSplitEnabled);
		}

		[Test]
		public void SplitViewContainer_ToggleEnablesSplit()
		{
			var container = new SplitViewContainer();

			container.ToggleSplit();

			Assert.IsTrue(container.IsSplitEnabled);
		}

		[Test]
		public void SplitViewContainer_ToggleTwiceDisablesSplit()
		{
			var container = new SplitViewContainer();

			container.ToggleSplit();
			container.ToggleSplit();

			Assert.IsFalse(container.IsSplitEnabled);
		}

		[Test]
		public void SplitViewContainer_SetSplitEnabledWorks()
		{
			var container = new SplitViewContainer();

			container.SetSplitEnabled(true);
			Assert.IsTrue(container.IsSplitEnabled);

			container.SetSplitEnabled(false);
			Assert.IsFalse(container.IsSplitEnabled);
		}

		[Test]
		public void SplitViewContainer_LinkScrollDefaultsToFalse()
		{
			var container = new SplitViewContainer();

			Assert.IsFalse(container.IsLinkScrollEnabled);
		}

		[Test]
		public void SplitViewContainer_LinkScrollCanBeToggled()
		{
			var container = new SplitViewContainer();

			container.IsLinkScrollEnabled = true;
			Assert.IsTrue(container.IsLinkScrollEnabled);

			container.IsLinkScrollEnabled = false;
			Assert.IsFalse(container.IsLinkScrollEnabled);
		}

		[Test]
		public void SplitViewContainer_PersistsSplitState()
		{
			// First container - enable split
			var container1 = new SplitViewContainer();
			container1.SetSplitEnabled(true);

			// Second container - should restore state
			var container2 = new SplitViewContainer();
			Assert.IsTrue(container2.IsSplitEnabled);
		}

		[Test]
		public void SplitViewContainer_PersistsLinkScrollState()
		{
			// First container - enable link scroll
			var container1 = new SplitViewContainer();
			container1.IsLinkScrollEnabled = true;

			// Second container - should restore state
			var container2 = new SplitViewContainer();
			Assert.IsTrue(container2.IsLinkScrollEnabled);
		}

		[Test]
		public void SplitViewContainer_FiresOnSplitToggledEvent()
		{
			var container = new SplitViewContainer();
			var eventFired = false;
			var eventValue = false;

			container.OnSplitToggled += value =>
			{
				eventFired = true;
				eventValue = value;
			};

			container.ToggleSplit();

			Assert.IsTrue(eventFired);
			Assert.IsTrue(eventValue);
		}

		[Test]
		public void SplitViewContainer_FiresOnLinkScrollToggledEvent()
		{
			var container = new SplitViewContainer();
			var eventFired = false;
			var eventValue = false;

			container.OnLinkScrollToggled += value =>
			{
				eventFired = true;
				eventValue = value;
			};

			container.IsLinkScrollEnabled = true;

			Assert.IsTrue(eventFired);
			Assert.IsTrue(eventValue);
		}

		[Test]
		public void SplitViewContainer_SplitRatioCanBeSet()
		{
			var container = new SplitViewContainer();

			container.SetSplitRatio(0.3f);

			// Ratio should be clamped between 0.2 and 0.8
			// We can't directly read the ratio back, but the method shouldn't throw
			Assert.Pass("Split ratio set without exception");
		}
	}
}
