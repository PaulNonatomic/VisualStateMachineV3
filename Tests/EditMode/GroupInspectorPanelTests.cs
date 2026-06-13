using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class GroupInspectorPanelTests
	{
		private const string VisiblePrefKey = "VSM.FloatingPanel.GroupInspector.Visible";
		private const string PositionXPrefKey = "VSM.FloatingPanel.GroupInspector.X";
		private const string PositionYPrefKey = "VSM.FloatingPanel.GroupInspector.Y";
		private const string WidthPrefKey = "VSM.FloatingPanel.GroupInspector.Width";
		private const string HeightPrefKey = "VSM.FloatingPanel.GroupInspector.Height";
		private const string CollapsedPrefKey = "VSM.FloatingPanel.GroupInspector.Collapsed";

		[SetUp]
		public void SetUp()
		{
			EditorPrefs.DeleteKey(VisiblePrefKey);
			EditorPrefs.DeleteKey(PositionXPrefKey);
			EditorPrefs.DeleteKey(PositionYPrefKey);
			EditorPrefs.DeleteKey(WidthPrefKey);
			EditorPrefs.DeleteKey(HeightPrefKey);
			EditorPrefs.DeleteKey(CollapsedPrefKey);
		}

		[TearDown]
		public void TearDown()
		{
			EditorPrefs.DeleteKey(VisiblePrefKey);
			EditorPrefs.DeleteKey(PositionXPrefKey);
			EditorPrefs.DeleteKey(PositionYPrefKey);
			EditorPrefs.DeleteKey(WidthPrefKey);
			EditorPrefs.DeleteKey(HeightPrefKey);
			EditorPrefs.DeleteKey(CollapsedPrefKey);
		}

		[Test]
		public void GroupInspectorPanel_CanBeCreated_WithNullWindow()
		{
			var panel = new GroupInspectorPanel(null);

			Assert.IsNotNull(panel);
		}

		[Test]
		public void GroupInspectorPanel_HasCorrectCssClass()
		{
			var panel = new GroupInspectorPanel(null);

			Assert.IsTrue(panel.ClassListContains("group-inspector-panel"));
			Assert.IsTrue(panel.ClassListContains("floating-panel"));
		}

		[Test]
		public void GroupInspectorPanel_IsVisibleByDefault()
		{
			var panel = new GroupInspectorPanel(null);

			Assert.IsTrue(panel.IsVisible);
		}

		[Test]
		public void GroupInspectorPanel_CanToggleVisibility()
		{
			var panel = new GroupInspectorPanel(null);

			panel.SetVisible(false);

			Assert.IsFalse(panel.IsVisible);
		}

		[Test]
		public void GroupInspectorPanel_CanCollapse()
		{
			var panel = new GroupInspectorPanel(null);

			panel.SetCollapsed(true);

			Assert.IsTrue(panel.IsCollapsed);
		}

		[Test]
		public void GroupInspectorPanel_ToggleCollapse_FlipsState()
		{
			var panel = new GroupInspectorPanel(null);
			var initialState = panel.IsCollapsed;

			panel.ToggleCollapse();

			Assert.AreNotEqual(initialState, panel.IsCollapsed);
		}

		[Test]
		public void GroupInspectorPanel_ClearInspector_DoesNotThrow()
		{
			var panel = new GroupInspectorPanel(null);

			Assert.DoesNotThrow(() => panel.ClearInspector());
		}

		[Test]
		public void GroupInspectorPanel_ShowGroupInspector_WithNull_DoesNotThrow()
		{
			var panel = new GroupInspectorPanel(null);

			Assert.DoesNotThrow(() => panel.ShowGroupInspector(null));
		}

		[Test]
		public void GroupInspectorPanel_ResetPosition_DoesNotThrow()
		{
			var panel = new GroupInspectorPanel(null);

			Assert.DoesNotThrow(() => panel.ResetPosition());
		}

		[Test]
		public void GroupInspectorPanel_VisibilityChanged_FiresEvent()
		{
			var panel = new GroupInspectorPanel(null);
			var eventFired = false;

			panel.OnVisibilityChanged += (_, _) => eventFired = true;
			panel.SetVisible(false);

			Assert.IsTrue(eventFired);
		}

		[Test]
		public void GroupInspectorPanel_SetVisible_PersistsToEditorPrefs()
		{
			var panel = new GroupInspectorPanel(null);

			panel.SetVisible(false);

			Assert.IsFalse(EditorPrefs.GetBool(VisiblePrefKey, true));
		}

		[Test]
		public void GroupInspectorPanel_RespectsSavedVisibility()
		{
			EditorPrefs.SetBool(VisiblePrefKey, false);

			var panel = new GroupInspectorPanel(null);

			Assert.IsFalse(panel.IsVisible);
		}
	}
}
