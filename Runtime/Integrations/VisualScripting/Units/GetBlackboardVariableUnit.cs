using Unity.VisualScripting;

namespace Nonatomic.VSM.VisualScripting.Units
{
	/// <summary>
	/// Reads a variable from the VSM blackboard by key.
	/// Only works inside a graph running on a VisualScriptingState.
	/// </summary>
	[UnitTitle("Get VSM Variable")]
	[UnitSurtitle("VSM Blackboard")]
	[UnitCategory("VSM/Blackboard")]
	public sealed class GetBlackboardVariableUnit : Unit
	{
		[DoNotSerialize]
		public ValueInput Key { get; private set; }

		[DoNotSerialize]
		[PortLabelHidden]
		public ValueOutput Value { get; private set; }

		protected override void Definition()
		{
			Key = ValueInput<string>(nameof(Key), string.Empty);
			Value = ValueOutput<object>(nameof(Value), GetValue);
			Requirement(Key, Value);
		}

		private object GetValue(Flow flow)
		{
			var key = flow.GetValue<string>(Key);

			if (string.IsNullOrEmpty(key)) return null;

			var state = VisualScriptingStateUtility.GetState(flow);
			if (state?.Blackboard == null) return null;
			if (!state.Blackboard.HasVariable(key)) return null;

			return state.Blackboard.GetBoxed(key);
		}
	}
}
