using System;

namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Entry point node for the state machine.
	/// Each state machine has exactly one entry node.
	/// </summary>
	[Serializable]
	public class EntryNode : GraphNode
	{
		public override GraphNodeType NodeType => GraphNodeType.Entry;

		/// <summary>
		/// Initializes a new entry node with an auto-generated ID.
		/// </summary>
		public EntryNode() : base()
		{
			DisplayName = "Entry";
		}

		/// <summary>
		/// Initializes a new entry node with the specified ID.
		/// </summary>
		/// <param name="id">Node ID, or null to auto-generate.</param>
		public EntryNode(string id) : base(id)
		{
			DisplayName = "Entry";
		}
	}
}
