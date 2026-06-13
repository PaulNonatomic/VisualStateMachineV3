using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Editor.Graph.Settings;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class EdgeSettingsTests
	{
		private EdgeSettings _settings;

		[SetUp]
		public void SetUp()
		{
			_settings = EdgeSettings.Instance;
		}

		[TearDown]
		public void TearDown()
		{
			_settings.ResetToDefaults();
		}

		[Test]
		public void Instance_ReturnsSameInstance()
		{
			var instance1 = EdgeSettings.Instance;
			var instance2 = EdgeSettings.Instance;

			Assert.AreSame(instance1, instance2);
		}

		[Test]
		public void DefaultValues_RendererType_IsWire()
		{
			_settings.ResetToDefaults();

			Assert.AreEqual(EdgeRendererType.Wire, _settings.RendererType);
		}

		[Test]
		public void DefaultValues_EdgeWidth_Is2()
		{
			_settings.ResetToDefaults();

			Assert.AreEqual(2f, _settings.EdgeWidth);
		}

		[Test]
		public void SetRendererType_PersistsValue()
		{
			_settings.RendererType = EdgeRendererType.Default;

			Assert.AreEqual(EdgeRendererType.Default, _settings.RendererType);
		}

		[Test]
		public void SetRendererType_RaisesOnRendererTypeChanged()
		{
			var eventRaised = false;
			_settings.OnRendererTypeChanged += () => eventRaised = true;

			_settings.RendererType = EdgeRendererType.Default;

			Assert.IsTrue(eventRaised);
		}

		[Test]
		public void SetRendererType_SameValue_DoesNotRaiseEvent()
		{
			_settings.ResetToDefaults();
			var eventCount = 0;
			_settings.OnRendererTypeChanged += () => eventCount++;

			_settings.RendererType = _settings.RendererType;

			Assert.AreEqual(0, eventCount);
		}

		[Test]
		public void ResetToDefaults_RestoresRendererType()
		{
			_settings.RendererType = EdgeRendererType.Default;

			_settings.ResetToDefaults();

			Assert.AreEqual(EdgeRendererType.Wire, _settings.RendererType);
		}

		[Test]
		public void ResetToDefaults_RestoresEdgeWidth()
		{
			_settings.EdgeWidth = 10f;

			_settings.ResetToDefaults();

			Assert.AreEqual(2f, _settings.EdgeWidth);
		}

		[Test]
		public void ResetToDefaults_RaisesOnRendererTypeChanged()
		{
			_settings.RendererType = EdgeRendererType.Default;
			var eventRaised = false;
			_settings.OnRendererTypeChanged += () => eventRaised = true;

			_settings.ResetToDefaults();

			Assert.IsTrue(eventRaised);
		}

		[Test]
		public void SetEdgeWidth_PersistsValue()
		{
			_settings.EdgeWidth = 5f;

			Assert.AreEqual(5f, _settings.EdgeWidth);
		}

		[Test]
		public void SaveAndLoad_PersistsRendererType()
		{
			_settings.RendererType = EdgeRendererType.Default;
			_settings.Save();

			_settings.Load();

			Assert.AreEqual(EdgeRendererType.Default, _settings.RendererType);
		}

		[Test]
		public void SaveAndLoad_PersistsEdgeWidth()
		{
			_settings.EdgeWidth = 8f;
			_settings.Save();

			_settings.Load();

			Assert.AreEqual(8f, _settings.EdgeWidth);
		}
	}
}
