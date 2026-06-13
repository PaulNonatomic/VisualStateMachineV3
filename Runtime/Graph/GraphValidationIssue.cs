namespace Nonatomic.VSM.Graph
{
	/// <summary>
	/// A validation issue found in the graph.
	/// </summary>
	public class GraphValidationIssue
	{
		/// <summary>
		/// Severity level of this issue.
		/// </summary>
		public GraphValidationSeverity Severity { get; }

		/// <summary>
		/// Description of the validation issue.
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// ID of the node related to this issue, or null if graph-wide.
		/// </summary>
		public string NodeId { get; }

		/// <summary>
		/// Initializes a new validation issue.
		/// </summary>
		/// <param name="severity">Severity level.</param>
		/// <param name="message">Description of the issue.</param>
		/// <param name="nodeId">Related node ID, or null.</param>
		public GraphValidationIssue(GraphValidationSeverity severity, string message, string nodeId)
		{
			Severity = severity;
			Message = message;
			NodeId = nodeId;
		}
	}

	/// <summary>
	/// Severity of a validation issue.
	/// </summary>
	public enum GraphValidationSeverity
	{
		/// <summary>
		/// Informational note that does not affect functionality.
		/// </summary>
		Info,

		/// <summary>
		/// Potential problem that may cause unexpected behavior.
		/// </summary>
		Warning,

		/// <summary>
		/// Critical problem that prevents correct execution.
		/// </summary>
		Error
	}
}
