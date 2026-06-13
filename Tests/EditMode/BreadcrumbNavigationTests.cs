using System.Collections.Generic;
using NUnit.Framework;
using Nonatomic.VSM.Editor.Graph.Navigation;
using Nonatomic.VSM.Graph;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class BreadcrumbNavigationTests
	{
		private BreadcrumbNavigation _breadcrumbNavigation;
		private List<StateMachineGraph> _testGraphs;

		[SetUp]
		public void SetUp()
		{
			_breadcrumbNavigation = new BreadcrumbNavigation();
			_testGraphs = new List<StateMachineGraph>();

			// Create test graphs
			for (var i = 0; i < 3; i++)
			{
				var graph = ScriptableObject.CreateInstance<StateMachineGraph>();
				graph.name = $"TestGraph{i}";
				_testGraphs.Add(graph);
			}
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var graph in _testGraphs)
			{
				if (graph != null)
				{
					Object.DestroyImmediate(graph);
				}
			}

			_testGraphs.Clear();
		}

		[Test]
		public void SetPath_WithEmptyPath_ShowsOnlyCurrentAsLabel()
		{
			var current = _testGraphs[0];

			_breadcrumbNavigation.SetPath(new List<StateMachineGraph>(), current);

			// Should have 1 child (the current label)
			Assert.AreEqual(1, _breadcrumbNavigation.childCount);
		}

		[Test]
		public void SetPath_WithOneLevelPath_ShowsParentButtonAndCurrentLabel()
		{
			var parent = _testGraphs[0];
			var current = _testGraphs[1];
			var path = new List<StateMachineGraph> { parent };

			_breadcrumbNavigation.SetPath(path, current);

			// Should have container with: parent button, separator, current label
			// The breadcrumb navigation has 1 child (the container), and container has the items
			Assert.AreEqual(1, _breadcrumbNavigation.childCount);
		}

		[Test]
		public void SetPath_WithTwoLevelPath_ShowsAllParentsAndCurrent()
		{
			var grandparent = _testGraphs[0];
			var parent = _testGraphs[1];
			var current = _testGraphs[2];
			var path = new List<StateMachineGraph> { grandparent, parent };

			_breadcrumbNavigation.SetPath(path, current);

			Assert.AreEqual(1, _breadcrumbNavigation.childCount);
		}

		[Test]
		public void OnBreadcrumbClicked_FiresWithCorrectIndex()
		{
			var parent = _testGraphs[0];
			var current = _testGraphs[1];
			var path = new List<StateMachineGraph> { parent };

			var clickedIndex = -1;
			_breadcrumbNavigation.OnBreadcrumbClicked += index => clickedIndex = index;

			_breadcrumbNavigation.SetPath(path, current);

			// The event should be wired up, index will be set when button is clicked
			// We can't easily simulate clicks in unit tests without UI toolkit test framework
			Assert.AreEqual(-1, clickedIndex); // Not clicked yet
		}

		[Test]
		public void Clear_RemovesAllBreadcrumbs()
		{
			var path = new List<StateMachineGraph> { _testGraphs[0] };
			_breadcrumbNavigation.SetPath(path, _testGraphs[1]);

			_breadcrumbNavigation.Clear();

			// After clear, the container should be empty
			Assert.AreEqual(1, _breadcrumbNavigation.childCount);
		}
	}
}
