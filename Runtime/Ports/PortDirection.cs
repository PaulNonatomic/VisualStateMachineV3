namespace Nonatomic.VSM.Ports
{
	/// <summary>
	/// The direction of data flow for a port.
	/// </summary>
	public enum PortDirection
	{
		/// <summary>
		/// Port receives data from external sources.
		/// </summary>
		Input,

		/// <summary>
		/// Port provides data to external consumers.
		/// </summary>
		Output,

		/// <summary>
		/// Special output port that triggers state transitions.
		/// </summary>
		Transition
	}
}
