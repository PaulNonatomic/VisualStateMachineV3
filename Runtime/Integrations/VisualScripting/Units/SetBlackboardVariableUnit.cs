using Unity.VisualScripting;

namespace Nonatomic.VSM.VisualScripting.Units
{
	/// <summary>
	/// Writes a value to the VSM blackboard by key.
	/// Only works inside a graph running on a VisualScriptingState.
	/// </summary>
	[UnitTitle("Set VSM Variable")]
	[UnitSurtitle("VSM Blackboard")]
	[UnitCategory("VSM/Blackboard")]
	public sealed class SetBlackboardVariableUnit : Unit
	{
		[DoNotSerialize]
		public ControlInput Enter { get; private set; }

		[DoNotSerialize]
		public ControlOutput Exit { get; private set; }

		[DoNotSerialize]
		public ValueInput Key { get; private set; }

		[DoNotSerialize]
		public ValueInput Value { get; private set; }

		protected override void Definition()
		{
			Key = ValueInput<string>(nameof(Key), string.Empty);
			Value = ValueInput<object>(nameof(Value));

			Enter = ControlInput(nameof(Enter), Execute);
			Exit = ControlOutput(nameof(Exit));

			Requirement(Key, Enter);
			Requirement(Value, Enter);
			Succession(Enter, Exit);
		}

		private ControlOutput Execute(Flow flow)
		{
			var key = flow.GetValue<string>(Key);
			var value = flow.GetValue<object>(Value);

			if (string.IsNullOrEmpty(key)) return Exit;

			var state = VisualScriptingStateUtility.GetState(flow);
			if (state?.Blackboard == null) return Exit;

			if (state.Blackboard.HasVariable(key))
			{
				state.Blackboard.SetBoxed(key, value, state.Blackboard.GetVariableType(key));
			}

			return Exit;
		}
	}
}
