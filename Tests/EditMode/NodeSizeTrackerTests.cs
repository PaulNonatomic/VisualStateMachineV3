using System;
using System.Collections.Generic;
using NUnit.Framework;
using Nonatomic.VSM.Editor.Layout;
using Nonatomic.VSM.Graph;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Nonatomic.VSM.Tests.EditMode
{
	[TestFixture]
	public class NodeSizeTrackerTests
	{
		private Node _node;
		private StateMachineGraph _graph;
		private Dictionary<string, Node> _nodeViews;

		[SetUp]
		public void SetUp()
		{
			_node = new Node();
			_graph = ScriptableObject.CreateInstance<StateMachineGraph>();
			_nodeViews = new Dictionary<string, Node>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_graph != null)
			{
				UnityEngine.Object.DestroyImmediate(_graph);
			}
		}

		[Test]
		public void Constructor_DoesNotThrow()
		{
			Assert.DoesNotThrow(() =>
			{
				using var tracker = new NodeSizeTracker(_node, _graph, _nodeViews);
			});
		}

		[Test]
		public void Constructor_WithCallback_DoesNotThrow()
		{
			var callbackCalled = false;

			Assert.DoesNotThrow(() =>
			{
				using var tracker = new NodeSizeTracker(_node, _graph, _nodeViews, () => callbackCalled = true);
			});

			Assert.IsFalse(callbackCalled, "Callback should not be called during construction");
		}

		[Test]
		public void Dispose_CanBeCalledMultipleTimes()
		{
			var tracker = new NodeSizeTracker(_node, _graph, _nodeViews);

			Assert.DoesNotThrow(() =>
			{
				tracker.Dispose();
				tracker.Dispose();
			});
		}

		[Test]
		public void ImplementsIDisposable()
		{
			var tracker = new NodeSizeTracker(_node, _graph, _nodeViews);

			Assert.IsInstanceOf<IDisposable>(tracker);
			tracker.Dispose();
		}

		[Test]
		public void SyncWidth_BeforeLayout_DoesNotThrow()
		{
			using var tracker = new NodeSizeTracker(_node, _graph, _nodeViews);

			Assert.DoesNotThrow(() => tracker.SyncWidth());
		}

		[Test]
		public void SyncWidth_AfterDispose_DoesNotThrow()
		{
			var tracker = new NodeSizeTracker(_node, _graph, _nodeViews);
			tracker.Dispose();

			Assert.DoesNotThrow(() => tracker.SyncWidth());
		}
	}
}
