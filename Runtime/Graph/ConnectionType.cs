namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// Types of connections between nodes in the graph.
	/// </summary>
	public enum ConnectionType
	{
		/// <summary>
		/// Data connection - transfers values between ports.
		/// </summary>
		Data,

		/// <summary>
		/// Transition connection - defines state flow.
		/// </summary>
		Transition,

		/// <summary>
		/// Blackboard read - connects blackboard variable to input port.
		/// </summary>
		BlackboardRead,

		/// <summary>
		/// Blackboard write - connects output port to blackboard variable.
		/// </summary>
		BlackboardWrite
	}
}
