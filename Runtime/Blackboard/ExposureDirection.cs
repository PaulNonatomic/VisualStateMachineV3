namespace Nonatomic.VSM.Blackboard
{
	/// <summary>
	/// Controls how a blackboard variable is exposed when its graph is used as a sub-graph.
	/// </summary>
	public enum ExposureDirection
	{
		/// <summary>
		/// Variable is not exposed on the sub-state machine node.
		/// </summary>
		None,

		/// <summary>
		/// Variable appears as an input port — parent writes, child reads.
		/// </summary>
		Input,

		/// <summary>
		/// Variable appears as an output port — child writes, parent reads.
		/// </summary>
		Output,

		/// <summary>
		/// Variable appears as both input and output ports.
		/// </summary>
		InputOutput
	}
}
