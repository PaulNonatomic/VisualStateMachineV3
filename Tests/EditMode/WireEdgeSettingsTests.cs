using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class WireEdgeSettingsTests
	{
		private WireEdgeSettings _settings;

		[SetUp]
		public void SetUp()
		{
			_settings = WireEdgeSettings.Instance;
		}

		[TearDown]
		public void TearDown()
		{
			_settings.ResetToDefaults();
		}

		[Test]
		public void Instance_ReturnsSameInstance()
		{
			var instance1 = WireEdgeSettings.Instance;
			var instance2 = WireEdgeSettings.Instance;

			Assert.AreSame(instance1, instance2);
		}

		[Test]
		public void DefaultValues_ControlPointSpacing_Is150()
		{
			_settings.ResetToDefaults();

			Assert.AreEqual(150f, _settings.ControlPointSpacing);
		}

		[Test]
		public void DefaultValues_BackwardLoopRadius_Is50()
		{
			_settings.ResetToDefaults();

			Assert.AreEqual(50f, _settings.BackwardLoopRadius);
		}

		[Test]
		public void DefaultValues_BackwardLoopOffset_Is80()
		{
			_settings.ResetToDefaults();

			Assert.AreEqual(80f, _settings.BackwardLoopOffset);
		}

		[Test]
		public void DefaultValues_SplineTension_Is05()
		{
			_settings.ResetToDefaults();

			Assert.AreEqual(0.5f, _settings.SplineTension);
		}

		[Test]
		public void DefaultValues_SamplesPerSegment_Is10()
		{
			_settings.ResetToDefaults();

			Assert.AreEqual(10, _settings.SamplesPerSegment);
		}

		[Test]
		public void DefaultValues_PortExitDistance_Is40()
		{
			_settings.ResetToDefaults();

			Assert.AreEqual(40f, _settings.PortExitDistance);
		}

		[Test]
		public void DefaultValues_EdgeWidth_Is2()
		{
			_settings.ResetToDefaults();

			Assert.AreEqual(2f, _settings.EdgeWidth);
		}

		[Test]
		public void SetControlPointSpacing_PersistsValue()
		{
			_settings.ControlPointSpacing = 200f;

			Assert.AreEqual(200f, _settings.ControlPointSpacing);
		}

		[Test]
		public void SetBackwardLoopRadius_PersistsValue()
		{
			_settings.BackwardLoopRadius = 75f;

			Assert.AreEqual(75f, _settings.BackwardLoopRadius);
		}

		[Test]
		public void SetSplineTension_PersistsValue()
		{
			_settings.SplineTension = 0.8f;

			Assert.AreEqual(0.8f, _settings.SplineTension);
		}

		[Test]
		public void SetSamplesPerSegment_PersistsValue()
		{
			_settings.SamplesPerSegment = 20;

			Assert.AreEqual(20, _settings.SamplesPerSegment);
		}

		[Test]
		public void ResetToDefaults_RestoresAllProperties()
		{
			_settings.ControlPointSpacing = 999f;
			_settings.BackwardLoopRadius = 999f;
			_settings.BackwardLoopOffset = 999f;
			_settings.SplineTension = 0.9f;
			_settings.SamplesPerSegment = 99;
			_settings.PortExitDistance = 999f;
			_settings.EdgeWidth = 99f;

			_settings.ResetToDefaults();

			Assert.AreEqual(150f, _settings.ControlPointSpacing);
			Assert.AreEqual(50f, _settings.BackwardLoopRadius);
			Assert.AreEqual(80f, _settings.BackwardLoopOffset);
			Assert.AreEqual(0.5f, _settings.SplineTension);
			Assert.AreEqual(10, _settings.SamplesPerSegment);
			Assert.AreEqual(40f, _settings.PortExitDistance);
			Assert.AreEqual(2f, _settings.EdgeWidth);
		}

		[Test]
		public void SaveAndLoad_PersistsControlPointSpacing()
		{
			_settings.ControlPointSpacing = 250f;
			_settings.Save();
			_settings.ControlPointSpacing = 0f;

			_settings.Load();

			Assert.AreEqual(250f, _settings.ControlPointSpacing);
		}

		[Test]
		public void SaveAndLoad_PersistsBackwardLoopRadius()
		{
			_settings.BackwardLoopRadius = 100f;
			_settings.Save();
			_settings.BackwardLoopRadius = 0f;

			_settings.Load();

			Assert.AreEqual(100f, _settings.BackwardLoopRadius);
		}

		[Test]
		public void SaveAndLoad_PersistsSplineTension()
		{
			_settings.SplineTension = 0.75f;
			_settings.Save();
			_settings.SplineTension = 0f;

			_settings.Load();

			Assert.AreEqual(0.75f, _settings.SplineTension);
		}

		[Test]
		public void SaveAndLoad_PersistsSamplesPerSegment()
		{
			_settings.SamplesPerSegment = 25;
			_settings.Save();
			_settings.SamplesPerSegment = 0;

			_settings.Load();

			Assert.AreEqual(25, _settings.SamplesPerSegment);
		}

		[Test]
		public void SaveAndLoad_PersistsEdgeWidth()
		{
			_settings.EdgeWidth = 5f;
			_settings.Save();
			_settings.EdgeWidth = 0f;

			_settings.Load();

			Assert.AreEqual(5f, _settings.EdgeWidth);
		}

		[Test]
		public void SaveAndLoad_PersistsPortExitDistance()
		{
			_settings.PortExitDistance = 60f;
			_settings.Save();
			_settings.PortExitDistance = 0f;

			_settings.Load();

			Assert.AreEqual(60f, _settings.PortExitDistance);
		}

		[Test]
		public void ImplementsIEdgeRendererSettings()
		{
			Assert.IsInstanceOf<Nonatomic.VSM.Editor.Graph.Settings.IEdgeRendererSettings>(_settings);
		}
	}
}
