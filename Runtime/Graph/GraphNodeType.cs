namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Types of nodes that can appear in a state machine graph.
	/// </summary>
	public enum GraphNodeType
	{
		/// <summary>
		/// A state node that executes logic.
		/// </summary>
		State,

		/// <summary>
		/// The entry point of the state machine.
		/// </summary>
		Entry,

		/// <summary>
		/// An exit point of the state machine.
		/// </summary>
		Exit,

		/// <summary>
		/// A blackboard variable node for data flow.
		/// </summary>
		BlackboardVariable,

		/// <summary>
		/// A nested sub-state machine.
		/// </summary>
		SubStateMachine,

		// Value 5 was the removed Comment node type. PortalOut/PortalIn keep their
		// original serialized integer values (6/7) so existing graph assets that store
		// these node types by integer continue to deserialize correctly.

		/// <summary>
		/// A portal-out node that teleports execution to a matching PortalIn node.
		/// </summary>
		PortalOut = 6,

		/// <summary>
		/// A portal-in node that receives execution from PortalOut nodes.
		/// </summary>
		PortalIn
	}
}
