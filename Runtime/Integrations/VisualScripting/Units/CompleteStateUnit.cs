using Unity.VisualScripting;

namespace Nonatomic.VSM.VisualScripting.Units
{
	/// <summary>
	/// Triggers the Done transition on the parent VisualScriptingState,
	/// causing the state machine to advance to the next state.
	/// </summary>
	[UnitTitle("Complete State")]
	[UnitSurtitle("VSM")]
	[UnitCategory("VSM")]
	public sealed class CompleteStateUnit : Unit
	{
		[DoNotSerialize]
		public ControlInput Enter { get; private set; }

		protected override void Definition()
		{
			Enter = ControlInput(nameof(Enter), Execute);
		}

		private ControlOutput Execute(Flow flow)
		{
			var state = VisualScriptingStateUtility.GetState(flow);
			state?.CompleteState();
			return null;
		}
	}
}
