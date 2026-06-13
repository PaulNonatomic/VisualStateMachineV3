using System;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Stores the state of an open peek preview popup for a sub-state machine node.
	/// </summary>
	[Serializable]
	public class PeekPreviewState
	{
		[SerializeField] private string _nodeId;
		[SerializeField] private Vector2 _position;
		[SerializeField] private Vector2 _panOffset;
		[SerializeField] private float _zoomLevel = 1f;

		/// <summary>
		/// The ID of the sub-state machine node this peek preview belongs to.
		/// </summary>
		public string NodeId
		{
			get => _nodeId;
			set => _nodeId = value;
		}

		/// <summary>
		/// Position of the popup in graph coordinates.
		/// </summary>
		public Vector2 Position
		{
			get => _position;
			set => _position = value;
		}

		/// <summary>
		/// Pan offset within the popup.
		/// </summary>
		public Vector2 PanOffset
		{
			get => _panOffset;
			set => _panOffset = value;
		}

		/// <summary>
		/// Zoom level within the popup.
		/// </summary>
		public float ZoomLevel
		{
			get => _zoomLevel;
			set => _zoomLevel = value;
		}
	}
}
