using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class StateListPanelTests
	{
		private const string PrefsKeyVisible = "VSM.FloatingPanel.StateList.Visible";
		private StateMachineGraph _graph;

		[SetUp]
		public void SetUp()
		{
			EditorPrefs.DeleteKey(PrefsKeyVisible);
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
		}

		[TearDown]
		public void TearDown()
		{
			EditorPrefs.DeleteKey(PrefsKeyVisible);
			if (_graph != null)
			{
				Object.DestroyImmediate(_graph);
			}
		}

		[Test]
		public void StateListPanel_CanBeCreated_WithNullWindow()
		{
			var panel = new StateListPanel(null);

			Assert.IsNotNull(panel);
		}

		[Test]
		public void StateListPanel_HasCorrectPanelId()
		{
			var panel = new StateListPanel(null);

			Assert.IsTrue(panel.ClassListContains("state-list-panel"));
		}

		[Test]
		public void StateListPanel_PopulateFromAsset_AcceptsNullGraph()
		{
			var panel = new StateListPanel(null);

			Assert.DoesNotThrow(() => panel.PopulateFromAsset(null));
		}

		[Test]
		public void StateListPanel_PopulateFromAsset_AcceptsEmptyGraph()
		{
			var panel = new StateListPanel(null);

			Assert.DoesNotThrow(() => panel.PopulateFromAsset(_graph));
		}

		[Test]
		public void StateListPanel_IsVisibleByDefault()
		{
			var panel = new StateListPanel(null);

			Assert.IsTrue(panel.IsVisible);
		}

		[Test]
		public void StateListPanel_CanToggleVisibility()
		{
			var panel = new StateListPanel(null);

			panel.SetVisible(false);

			Assert.IsFalse(panel.IsVisible);
		}

		[Test]
		public void StateListPanel_FiresVisibilityChangedEvent()
		{
			var panel = new StateListPanel(null);
			var eventFired = false;

			panel.OnVisibilityChanged += (p, visible) => eventFired = true;
			panel.SetVisible(false);

			Assert.IsTrue(eventFired);
		}

		[Test]
		public void StateListFilter_EnumHasExpectedValues()
		{
			// Verify the filter enum has all expected values
			Assert.AreEqual(0, (int)StateListFilter.All);
			Assert.AreEqual(1, (int)StateListFilter.States);
			Assert.AreEqual(2, (int)StateListFilter.Navigation);
			Assert.AreEqual(3, (int)StateListFilter.SubStateMachines);
			Assert.AreEqual(4, (int)StateListFilter.Variables);
		}

		[Test]
		public void StateListPanel_PopulateFromAsset_HandlesGraphWithNodes()
		{
			var panel = new StateListPanel(null);

			// Create a graph with an entry node
			_graph.InitializeNewGraph();

			Assert.DoesNotThrow(() => panel.PopulateFromAsset(_graph));
		}

		[Test]
		public void StateListPanel_AutoFrameSearchResult_IsEnabledByDefault()
		{
			EditorPrefs.DeleteKey("VSM.StateList.AutoFrameSearchResult");

			var panel = new StateListPanel(null);

			Assert.IsTrue(panel.AutoFrameSearchResult);
		}

		[Test]
		public void StateListPanel_AutoFrameSearchResult_CanBeDisabled()
		{
			var panel = new StateListPanel(null);

			panel.AutoFrameSearchResult = false;

			Assert.IsFalse(panel.AutoFrameSearchResult);
		}

		[Test]
		public void StateListPanel_AutoFrameSearchResult_PersistsToEditorPrefs()
		{
			EditorPrefs.DeleteKey("VSM.StateList.AutoFrameSearchResult");
			var panel = new StateListPanel(null);

			panel.AutoFrameSearchResult = false;

			Assert.IsFalse(EditorPrefs.GetBool("VSM.StateList.AutoFrameSearchResult", true));
		}

		[Test]
		public void StateListPanel_AutoFrameSearchResult_RestoresFromEditorPrefs()
		{
			EditorPrefs.SetBool("VSM.StateList.AutoFrameSearchResult", false);

			var panel = new StateListPanel(null);

			Assert.IsFalse(panel.AutoFrameSearchResult);

			// Clean up
			EditorPrefs.DeleteKey("VSM.StateList.AutoFrameSearchResult");
		}
	}
}
