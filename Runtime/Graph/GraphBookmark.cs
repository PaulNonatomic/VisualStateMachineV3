using System;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// A saved view location (bookmark) in the graph.
	/// </summary>
	[Serializable]
	public class GraphBookmark
	{
		[SerializeField] private string _id;
		[SerializeField] private string _name;
		[SerializeField] private Vector3 _viewPosition;
		[SerializeField] private Vector3 _viewScale;
		[SerializeField] private float _zoom;
		[SerializeField] private int _slot;
		[SerializeField] private Vector2 _graphPosition;

		public GraphBookmark()
		{
			_id = Guid.NewGuid().ToString();
			_name = "New Bookmark";
			_viewScale = Vector3.one;
			_zoom = 1f;
		}

		public GraphBookmark(string name, Vector3 position, Vector3 scale, float zoom) : this()
		{
			_name = name;
			_viewPosition = position;
			_viewScale = scale;
			_zoom = zoom;
		}

		public string Id
		{
			get => _id;
			set => _id = value;
		}

		public string Name
		{
			get => _name;
			set => _name = value;
		}

		public Vector3 ViewPosition
		{
			get => _viewPosition;
			set => _viewPosition = value;
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

		public int Slot
		{
			get => _slot;
			set => _slot = value;
		}

		/// <summary>
		/// Position of the viewport center in graph coordinates when this bookmark was created.
		/// Used for placing visual markers in the graph.
		/// </summary>
		public Vector2 GraphPosition
		{
			get => _graphPosition;
			set => _graphPosition = value;
		}
	}
}
