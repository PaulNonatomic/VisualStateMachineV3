using System;
using UnityEngine;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Base class for all nodes in the visual graph.
	/// </summary>
	[Serializable]
	public abstract class GraphNode
	{
		[SerializeField] private string _id;

		[SerializeField] private Vector2 _position;

		[SerializeField] private string _displayName;

		[SerializeField] private string _comment;

		/// <summary>
		/// Unique identifier for this node.
		/// </summary>
		public string Id
		{
			get => _id;
			set => _id = value;
		}

		/// <summary>
		/// Position in the graph editor.
		/// </summary>
		public Vector2 Position
		{
			get => _position;
			set => _position = value;
		}

		/// <summary>
		/// Display name shown in the graph.
		/// </summary>
		public string DisplayName
		{
			get => _displayName;
			set => _displayName = value;
		}

		/// <summary>
		/// Optional comment for this node.
		/// </summary>
		public string Comment
		{
			get => _comment;
			set => _comment = value;
		}

		/// <summary>
		/// Node type for serialization and identification.
		/// </summary>
		public abstract GraphNodeType NodeType { get; }

		/// <summary>
		/// Initializes a new node with an auto-generated ID.
		/// </summary>
		protected GraphNode()
		{
			_id = Guid.NewGuid().ToString();
		}

		/// <summary>
		/// Initializes a new node with the specified ID.
		/// </summary>
		/// <param name="id">Node ID, or null to auto-generate.</param>
		protected GraphNode(string id)
		{
			_id = id ?? Guid.NewGuid().ToString();
		}
	}
}
