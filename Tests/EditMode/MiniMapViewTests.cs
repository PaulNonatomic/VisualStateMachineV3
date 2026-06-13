using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph;
using UnityEditor;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class MiniMapViewTests
	{
		private const string PrefsKeyVisible = "VSM.MiniMap.Visible";

		[SetUp]
		public void SetUp()
		{
			// Clear any existing pref to start fresh
			EditorPrefs.DeleteKey(PrefsKeyVisible);
		}

		[TearDown]
		public void TearDown()
		{
			// Clean up prefs after tests
			EditorPrefs.DeleteKey(PrefsKeyVisible);
		}

		[Test]
		public void MiniMapView_CanBeCreated_WithNullGraphView()
		{
			var miniMap = new MiniMapView(null);

			Assert.IsNotNull(miniMap);
		}

		[Test]
		public void MiniMapView_IsVisibleByDefault()
		{
			var miniMap = new MiniMapView(null);

			Assert.IsTrue(miniMap.IsVisible);
		}

		[Test]
		public void MiniMapView_CanToggleVisibility()
		{
			var miniMap = new MiniMapView(null);

			miniMap.IsVisible = false;

			Assert.IsFalse(miniMap.IsVisible);
		}

		[Test]
		public void MiniMapView_ToggleVisibility_FlipsState()
		{
			var miniMap = new MiniMapView(null);
			var initialState = miniMap.IsVisible;

			miniMap.ToggleVisibility();

			Assert.AreNotEqual(initialState, miniMap.IsVisible);
		}

		[Test]
		public void MiniMapView_SavesVisibilityToEditorPrefs()
		{
			var miniMap = new MiniMapView(null);

			miniMap.IsVisible = false;

			Assert.IsFalse(EditorPrefs.GetBool(PrefsKeyVisible, true));
		}

		[Test]
		public void MiniMapView_RestoresVisibilityFromEditorPrefs()
		{
			// Set pref to hidden before creating
			EditorPrefs.SetBool(PrefsKeyVisible, false);

			var miniMap = new MiniMapView(null);

			Assert.IsFalse(miniMap.IsVisible);
		}

		[Test]
		public void MiniMapView_FiresVisibilityChangedEvent_WhenVisibilityChanges()
		{
			var miniMap = new MiniMapView(null);
			var eventFired = false;
			var newVisibility = true;

			miniMap.OnVisibilityChanged += visible =>
			{
				eventFired = true;
				newVisibility = visible;
			};

			miniMap.IsVisible = false;

			Assert.IsTrue(eventFired);
			Assert.IsFalse(newVisibility);
		}

		[Test]
		public void MiniMapView_DoesNotFireVisibilityChangedEvent_WhenVisibilityUnchanged()
		{
			var miniMap = new MiniMapView(null);
			var eventFired = false;

			miniMap.OnVisibilityChanged += _ => eventFired = true;

			// Set to same value (already visible)
			miniMap.IsVisible = true;

			Assert.IsFalse(eventFired);
		}

		[Test]
		public void MiniMapView_HasCorrectName()
		{
			var miniMap = new MiniMapView(null);

			Assert.AreEqual("mini-map-container", miniMap.name);
		}
	}
}
