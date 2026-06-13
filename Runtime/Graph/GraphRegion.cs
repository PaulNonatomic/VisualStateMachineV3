using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Represents a visual region/area for organizing nodes in the graph.
	/// Regions are purely organizational and don't affect state machine execution.
	/// </summary>
	[Serializable]
	public class GraphRegion
	{
		[SerializeField] private string _id;
		[SerializeField] private string _title;
		[SerializeField] private Rect _bounds;
		[SerializeField] private Color _color;
		[SerializeField] private List<string> _containedNodeIds = new List<string>();

		/// <summary>
		/// Unique identifier for this region.
		/// </summary>
		public string Id
		{
			get => _id;
			set => _id = value;
		}

		/// <summary>
		/// Display title for the region.
		/// </summary>
		public string Title
		{
			get => _title;
			set => _title = value;
		}

		/// <summary>
		/// Position and size of the region in graph coordinates.
		/// </summary>
		public Rect Bounds
		{
			get => _bounds;
			set => _bounds = value;
		}

		/// <summary>
		/// Background color of the region.
		/// </summary>
		public Color Color
		{
			get => _color;
			set => _color = value;
		}

		/// <summary>
		/// IDs of nodes contained within this region.
		/// </summary>
		public List<string> ContainedNodeIds => _containedNodeIds;

		/// <summary>
		/// Adds a node ID to this region's contained nodes.
		/// </summary>
		public void AddNode(string nodeId)
		{
			if (string.IsNullOrEmpty(nodeId)) return;
			if (!_containedNodeIds.Contains(nodeId))
			{
				_containedNodeIds.Add(nodeId);
			}
		}

		/// <summary>
		/// Removes a node ID from this region's contained nodes.
		/// </summary>
		public void RemoveNode(string nodeId)
		{
			_containedNodeIds.Remove(nodeId);
		}

		/// <summary>
		/// Checks if this region contains the specified node.
		/// </summary>
		public bool ContainsNode(string nodeId)
		{
			return _containedNodeIds.Contains(nodeId);
		}

		/// <summary>
		/// Clears all contained node IDs.
		/// </summary>
		public void ClearNodes()
		{
			_containedNodeIds.Clear();
		}

		public GraphRegion()
		{
			_id = Guid.NewGuid().ToString();
			_title = "New Group";
			_bounds = new Rect(0, 0, 300, 200);
			_color = new Color(0.3f, 0.3f, 0.4f, 0.3f);
		}

		public GraphRegion(string title, Rect bounds) : this()
		{
			_title = title;
			_bounds = bounds;
		}

		public GraphRegion(string title, Rect bounds, Color color) : this(title, bounds)
		{
			_color = color;
		}
	}
}
