using System;
using System.Collections.Generic;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Layout
{
	/// <summary>
	/// Tracks node sizes and triggers repositioning when a node's size changes
	/// due to collapse/expand operations.
	/// </summary>
	public class NodeSizeTracker : IDisposable
	{
		private readonly Node _node;
		private readonly StateMachineGraph _graph;
		private readonly Dictionary<string, Node> _nodeViews;
		private readonly Action _markDirtyCallback;

		private float _previousWidth;
		private bool _isInitialized;
		private bool _isDisposed;

		// Width stabilization: wait for width to stop changing before tracking
		private float _lastCheckedWidth;
		private int _stableFrameCount;
		private int _initAttempts;
		private const int RequiredStableFrames = 3;
		private const int MaxInitAttempts = 60;

		// Threshold to prevent repositioning from tiny floating-point changes
		private const float SizeChangeThreshold = 5f;

		/// <summary>
		/// Creates a new size tracker for a node.
		/// </summary>
		/// <param name="node">The node to track</param>
		/// <param name="graph">The state machine graph data</param>
		/// <param name="nodeViews">Dictionary mapping node IDs to their visual views</param>
		/// <param name="markDirtyCallback">Callback to mark the graph asset as dirty after changes</param>
		public NodeSizeTracker(
			Node node,
			StateMachineGraph graph,
			Dictionary<string, Node> nodeViews,
			Action markDirtyCallback = null)
		{
			_node = node;
			_graph = graph;
			_nodeViews = nodeViews;
			_markDirtyCallback = markDirtyCallback;

			// Register for geometry changes
			_node.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

			// Initialize the previous width once the node has layout
			_node.schedule.Execute(InitializePreviousWidth);
		}

		/// <summary>
		/// Waits for the node width to stabilize before enabling tracking.
		/// Width must remain unchanged across multiple consecutive frames to ensure
		/// layout settling (inline field visibility, port rendering, font loading)
		/// has completed. Without this, false-positive width changes on load trigger
		/// repositioning and cause cumulative node position drift.
		/// </summary>
		private void InitializePreviousWidth()
		{
			if (_isDisposed) return;

			var currentWidth = _node.resolvedStyle.width;

			// Safety limit: if width never stabilizes, accept current width
			if (++_initAttempts > MaxInitAttempts)
			{
				if (!float.IsNaN(currentWidth) && currentWidth > 0)
				{
					_previousWidth = currentWidth;
				}
				_isInitialized = true;
				return;
			}

			// If width is still invalid, try again next frame
			if (float.IsNaN(currentWidth) || currentWidth <= 0)
			{
				_node.schedule.Execute(InitializePreviousWidth);
				return;
			}

			// Check if width has stabilized (unchanged between consecutive frames)
			if (Mathf.Abs(currentWidth - _lastCheckedWidth) < 1f)
			{
				_stableFrameCount++;
				if (_stableFrameCount >= RequiredStableFrames)
				{
					_previousWidth = currentWidth;
					_isInitialized = true;
					return;
				}
			}
			else
			{
				_stableFrameCount = 0;
			}

			_lastCheckedWidth = currentWidth;
			_node.schedule.Execute(InitializePreviousWidth);
		}

		/// <summary>
		/// Handles geometry change events from the node.
		/// </summary>
		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			if (_isDisposed || !_isInitialized) return;
			if (EditorApplication.isPlayingOrWillChangePlaymode) return;

			var currentWidth = _node.resolvedStyle.width;

			// Skip if width is invalid
			if (float.IsNaN(currentWidth) || currentWidth <= 0) return;

			// Calculate the change
			var widthDelta = currentWidth - _previousWidth;

			// Only reposition if the change is significant (avoids jitter from minor layout adjustments)
			if (Mathf.Abs(widthDelta) >= SizeChangeThreshold)
			{
				// Trigger repositioning
				NodeRepositionService.OnNodeSizeChanged(
					_node,
					_previousWidth,
					_graph,
					_nodeViews);

				// Mark the graph as dirty so changes are saved
				_markDirtyCallback?.Invoke();
			}

			// Update the tracked width
			_previousWidth = currentWidth;
		}

		/// <summary>
		/// Manually forces an update of the tracked width without triggering repositioning.
		/// Call this after programmatic position changes to prevent feedback loops.
		/// </summary>
		public void SyncWidth()
		{
			if (_isDisposed) return;

			var currentWidth = _node.resolvedStyle.width;
			if (!float.IsNaN(currentWidth) && currentWidth > 0)
			{
				_previousWidth = currentWidth;
			}
		}

		/// <summary>
		/// Disposes of the tracker and unregisters callbacks.
		/// </summary>
		public void Dispose()
		{
			if (_isDisposed) return;
			_isDisposed = true;

			_node?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
		}
	}
}
