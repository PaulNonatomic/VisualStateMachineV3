using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Stores graph view state (zoom, pan position).
	/// </summary>
	[Serializable]
	public class GraphViewData
	{
		[SerializeField] private Vector3 _viewPosition;

		[SerializeField] private Vector3 _viewScale = Vector3.one;

		[SerializeField] private float _zoom = 1f;

		[SerializeField] private bool _hasBeenSet;

		[SerializeField] private List<PeekPreviewState> _openPeekPreviews = new List<PeekPreviewState>();

		[SerializeField] private List<GraphBookmark> _bookmarks = new List<GraphBookmark>();

		[SerializeField] private List<Breakpoint> _breakpoints = new List<Breakpoint>();

		public Vector3 ViewPosition
		{
			get => _viewPosition;
			set
			{
				_viewPosition = value;
				_hasBeenSet = true;
			}
		}

		public Vector3 ViewScale
		{
			get => _viewScale;
			set => _viewScale = value;
		}

		public float Zoom
		{
			get => _zoom;
			set => _zoom = value;
		}

		/// <summary>
		/// Returns true if the view position has been explicitly set (graph has been opened before).
		/// </summary>
		public bool HasBeenSet => _hasBeenSet;

		/// <summary>
		/// States of open peek preview popups for sub-state machine nodes.
		/// </summary>
		public List<PeekPreviewState> OpenPeekPreviews => _openPeekPreviews;

		/// <summary>
		/// Adds or updates a peek preview state for a node.
		/// </summary>
		public void SetPeekPreviewState(string nodeId, Vector2 position, Vector2 panOffset, float zoomLevel)
		{
			var existingIndex = _openPeekPreviews.FindIndex(p => p.NodeId == nodeId);
			var state = new PeekPreviewState
			{
				NodeId = nodeId,
				Position = position,
				PanOffset = panOffset,
				ZoomLevel = zoomLevel
			};

			if (existingIndex >= 0)
			{
				_openPeekPreviews[existingIndex] = state;
			}
			else
			{
				_openPeekPreviews.Add(state);
			}
		}

		/// <summary>
		/// Removes a peek preview state for a node.
		/// </summary>
		public void RemovePeekPreviewState(string nodeId)
		{
			_openPeekPreviews.RemoveAll(p => p.NodeId == nodeId);
		}

		/// <summary>
		/// Gets the peek preview state for a node, if it exists.
		/// </summary>
		public PeekPreviewState GetPeekPreviewState(string nodeId)
		{
			return _openPeekPreviews.Find(p => p.NodeId == nodeId);
		}

		/// <summary>
		/// List of saved view bookmarks.
		/// </summary>
		public IReadOnlyList<GraphBookmark> Bookmarks => _bookmarks;

		/// <summary>
		/// Adds a new bookmark.
		/// </summary>
		public void AddBookmark(GraphBookmark bookmark)
		{
			_bookmarks.Add(bookmark);
		}

		/// <summary>
		/// Removes a bookmark by ID.
		/// </summary>
		public void RemoveBookmark(string bookmarkId)
		{
			_bookmarks.RemoveAll(b => b.Id == bookmarkId);
		}

		/// <summary>
		/// Updates an existing bookmark.
		/// </summary>
		public void UpdateBookmark(GraphBookmark bookmark)
		{
			var index = _bookmarks.FindIndex(b => b.Id == bookmark.Id);
			if (index >= 0)
			{
				_bookmarks[index] = bookmark;
			}
		}

		/// <summary>
		/// Gets a bookmark by ID.
		/// </summary>
		public GraphBookmark GetBookmark(string bookmarkId)
		{
			return _bookmarks.Find(b => b.Id == bookmarkId);
		}

		/// <summary>
		/// All breakpoints in this graph.
		/// </summary>
		public IReadOnlyList<Breakpoint> Breakpoints => _breakpoints;

		/// <summary>
		/// Adds a breakpoint to the graph.
		/// </summary>
		public void AddBreakpoint(Breakpoint breakpoint)
		{
			if (breakpoint == null) return;

			var existingIndex = _breakpoints.FindIndex(b => b.Id == breakpoint.Id);
			if (existingIndex >= 0)
			{
				_breakpoints[existingIndex] = breakpoint;
			}
			else
			{
				_breakpoints.Add(breakpoint);
			}
		}

		/// <summary>
		/// Removes a breakpoint by ID.
		/// </summary>
		public void RemoveBreakpoint(string breakpointId)
		{
			_breakpoints.RemoveAll(b => b.Id == breakpointId);
		}

		/// <summary>
		/// Removes all breakpoints for a specific node.
		/// </summary>
		public void RemoveBreakpointsForNode(string nodeId)
		{
			_breakpoints.RemoveAll(b => b.NodeId == nodeId);
		}

		/// <summary>
		/// Gets the breakpoint for a specific node, if one exists.
		/// </summary>
		public Breakpoint GetBreakpointForNode(string nodeId)
		{
			return _breakpoints.Find(b => b.NodeId == nodeId);
		}

		/// <summary>
		/// Gets the breakpoint for one specific transition (edge), identified by its target node,
		/// source node, and source port. Multiple transitions into the same node each get their own.
		/// </summary>
		public Breakpoint GetTransitionBreakpoint(string targetNodeId, string sourceNodeId, string transitionPortName)
		{
			return _breakpoints.Find(b => b.Type == BreakpointType.OnTransition
				&& b.NodeId == targetNodeId
				&& b.SourceNodeId == sourceNodeId
				&& b.TransitionPortName == transitionPortName);
		}

		/// <summary>
		/// Gets the node-entry breakpoint (OnEntry or Conditional) for a node, if one exists.
		/// </summary>
		public Breakpoint GetNodeEntryBreakpoint(string nodeId)
		{
			return _breakpoints.Find(b => b.NodeId == nodeId && b.Type != BreakpointType.OnTransition);
		}

		/// <summary>
		/// Gets a breakpoint by its unique id.
		/// </summary>
		public Breakpoint GetBreakpointById(string breakpointId)
		{
			return _breakpoints.Find(b => b.Id == breakpointId);
		}

		/// <summary>
		/// Checks if a node has an enabled breakpoint.
		/// </summary>
		public bool HasEnabledBreakpoint(string nodeId)
		{
			var breakpoint = GetBreakpointForNode(nodeId);
			return breakpoint != null && breakpoint.Enabled;
		}

		/// <summary>
		/// Toggles a breakpoint on a node. Returns the breakpoint if one was added, null if removed.
		/// </summary>
		public Breakpoint ToggleBreakpoint(string nodeId)
		{
			var existing = GetNodeEntryBreakpoint(nodeId);
			if (existing != null)
			{
				_breakpoints.Remove(existing);
				return null;
			}

			var breakpoint = new Breakpoint(nodeId);
			_breakpoints.Add(breakpoint);
			return breakpoint;
		}

		/// <summary>
		/// Removes all breakpoints from the graph.
		/// </summary>
		public void ClearAllBreakpoints()
		{
			_breakpoints.Clear();
		}
	}
}
