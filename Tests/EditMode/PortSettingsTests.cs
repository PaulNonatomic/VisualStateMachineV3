using System;
using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph.Settings;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class PortSettingsTests
	{
		private PortSettings _settings;

		[SetUp]
		public void SetUp()
		{
			_settings = PortSettings.Instance;
		}

		[TearDown]
		public void TearDown()
		{
			_settings.ResetToDefaults();
		}

		[Test]
		public void Instance_ReturnsSameInstance()
		{
			var instance1 = PortSettings.Instance;
			var instance2 = PortSettings.Instance;

			Assert.AreSame(instance1, instance2);
		}

		[Test]
		public void DefaultValues_ColorMode_IsTypeBased()
		{
			_settings.ResetToDefaults();

			Assert.AreEqual(PortColorMode.TypeBased, _settings.ColorMode);
		}

		[Test]
		public void DefaultValues_ShowTypeIcons_IsTrue()
		{
			_settings.ResetToDefaults();

			Assert.IsTrue(_settings.ShowTypeIcons);
		}

		[Test]
		public void SetColorMode_PersistsValue()
		{
			_settings.ColorMode = PortColorMode.StateBased;

			Assert.AreEqual(PortColorMode.StateBased, _settings.ColorMode);
		}

		[Test]
		public void SetColorMode_SurvivesLoadCycle()
		{
			_settings.ColorMode = PortColorMode.StateBased;
			_settings.Load();

			Assert.AreEqual(PortColorMode.StateBased, _settings.ColorMode);
		}

		[Test]
		public void SetShowTypeIcons_PersistsValue()
		{
			_settings.ShowTypeIcons = false;

			Assert.IsFalse(_settings.ShowTypeIcons);
		}

		[Test]
		public void SetShowTypeIcons_SurvivesLoadCycle()
		{
			_settings.ShowTypeIcons = false;
			_settings.Load();

			Assert.IsFalse(_settings.ShowTypeIcons);
		}

		[Test]
		public void SetColorMode_FiresOnSettingsChanged()
		{
			var fired = false;
			_settings.OnSettingsChanged += () => fired = true;

			_settings.ColorMode = PortColorMode.StateBased;

			Assert.IsTrue(fired);
			_settings.OnSettingsChanged -= () => fired = true;
		}

		[Test]
		public void SetColorMode_SameValue_DoesNotFireEvent()
		{
			_settings.ResetToDefaults();
			var fired = false;
			_settings.OnSettingsChanged += () => fired = true;

			_settings.ColorMode = PortColorMode.TypeBased;

			Assert.IsFalse(fired);
			_settings.OnSettingsChanged -= () => fired = true;
		}

		[Test]
		public void SetShowTypeIcons_FiresOnSettingsChanged()
		{
			var fired = false;
			_settings.OnSettingsChanged += () => fired = true;

			_settings.ShowTypeIcons = false;

			Assert.IsTrue(fired);
			_settings.OnSettingsChanged -= () => fired = true;
		}

		[Test]
		public void SetShowTypeIcons_SameValue_DoesNotFireEvent()
		{
			_settings.ResetToDefaults();
			var fired = false;
			_settings.OnSettingsChanged += () => fired = true;

			_settings.ShowTypeIcons = true;

			Assert.IsFalse(fired);
			_settings.OnSettingsChanged -= () => fired = true;
		}

		[Test]
		public void ResetToDefaults_RestoresColorMode()
		{
			_settings.ColorMode = PortColorMode.StateBased;
			_settings.ResetToDefaults();

			Assert.AreEqual(PortColorMode.TypeBased, _settings.ColorMode);
		}

		[Test]
		public void ResetToDefaults_RestoresShowTypeIcons()
		{
			_settings.ShowTypeIcons = false;
			_settings.ResetToDefaults();

			Assert.IsTrue(_settings.ShowTypeIcons);
		}

		[Test]
		public void ResetToDefaults_FiresOnSettingsChanged()
		{
			var fired = false;
			_settings.OnSettingsChanged += () => fired = true;

			_settings.ResetToDefaults();

			Assert.IsTrue(fired);
			_settings.OnSettingsChanged -= () => fired = true;
		}

		[Test]
		public void ResetToDefaults_PersistsAfterLoad()
		{
			_settings.ColorMode = PortColorMode.StateBased;
			_settings.ShowTypeIcons = false;
			_settings.ResetToDefaults();
			_settings.Load();

			Assert.AreEqual(PortColorMode.TypeBased, _settings.ColorMode);
			Assert.IsTrue(_settings.ShowTypeIcons);
		}

		[Test]
		public void MultipleChanges_AllPersist()
		{
			_settings.ColorMode = PortColorMode.StateBased;
			_settings.ShowTypeIcons = false;
			_settings.Load();

			Assert.AreEqual(PortColorMode.StateBased, _settings.ColorMode);
			Assert.IsFalse(_settings.ShowTypeIcons);
		}
	}
}
