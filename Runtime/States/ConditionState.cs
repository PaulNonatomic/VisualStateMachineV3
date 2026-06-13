using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// State that branches based on a boolean condition.
	/// </summary>
	[StateInfo(
		"Branches based on a condition",
		"Evaluates a boolean condition and transitions to either the True or False path. " +
		"The condition can be set via data input port from blackboard variables or other states."
	)]
	[StateStyle(Color = StateColors.Yellow, Icon = NodeIcon.CheckCircle)]
	public class ConditionState : State
	{
		/// <summary>
		/// The condition to evaluate.
		/// </summary>
		[DataIn("Condition")]
		public bool Condition;

		/// <summary>
		/// Inverts the condition if true.
		/// </summary>
		[DataIn("Invert")]
		public bool Invert = false;

		/// <summary>
		/// Outputs the evaluated result (after inversion if applicable).
		/// </summary>
		[DataOut("Result")]
		public bool Result;

		/// <summary>
		/// Transition that fires when condition is true.
		/// </summary>
		[Transition("True", Color = StateColors.Green)]
		public Action OnTrue;

		/// <summary>
		/// Transition that fires when condition is false.
		/// </summary>
		[Transition("False", Color = StateColors.Red)]
		public Action OnFalse;

		public override void OnEnter()
		{
			Result = Invert ? !Condition : Condition;

			if (Result)
			{
				OnTrue?.Invoke();
			}
			else
			{
				OnFalse?.Invoke();
			}
		}
	}

	/// <summary>
	/// State that compares two numeric values and branches accordingly.
	/// </summary>
	[StateInfo(
		"Compares two numbers",
		"Compares two float values using the specified comparison operator and " +
		"transitions based on the result."
	)]
	[StateStyle(Color = StateColors.Yellow, Icon = NodeIcon.CheckCircle)]
	public class CompareState : State
	{
		/// <summary>
		/// The first value to compare.
		/// </summary>
		[DataIn("Value A")]
		public float ValueA;

		/// <summary>
		/// The second value to compare.
		/// </summary>
		[DataIn("Value B")]
		public float ValueB;

		/// <summary>
		/// The comparison operator to use.
		/// </summary>
		[DataIn("Operator")]
		public ComparisonOperator Operator = ComparisonOperator.Equal;

		/// <summary>
		/// Tolerance for equality comparisons (to handle float imprecision).
		/// </summary>
		[DataIn("Tolerance")]
		public float Tolerance = 0.0001f;

		/// <summary>
		/// Outputs the comparison result.
		/// </summary>
		[DataOut("Result")]
		public bool Result;

		/// <summary>
		/// Transition that fires when comparison is true.
		/// </summary>
		[Transition("True", Color = StateColors.Green)]
		public Action OnTrue;

		/// <summary>
		/// Transition that fires when comparison is false.
		/// </summary>
		[Transition("False", Color = StateColors.Red)]
		public Action OnFalse;

		public override void OnEnter()
		{
			Result = Operator switch
			{
				ComparisonOperator.Equal => Math.Abs(ValueA - ValueB) <= Tolerance,
				ComparisonOperator.NotEqual => Math.Abs(ValueA - ValueB) > Tolerance,
				ComparisonOperator.GreaterThan => ValueA > ValueB,
				ComparisonOperator.GreaterOrEqual => ValueA >= ValueB,
				ComparisonOperator.LessThan => ValueA < ValueB,
				ComparisonOperator.LessOrEqual => ValueA <= ValueB,
				_ => false
			};

			if (Result)
			{
				OnTrue?.Invoke();
			}
			else
			{
				OnFalse?.Invoke();
			}
		}
	}

	/// <summary>
	/// Comparison operators for numeric comparisons.
	/// </summary>
	public enum ComparisonOperator
	{
		Equal,
		NotEqual,
		GreaterThan,
		GreaterOrEqual,
		LessThan,
		LessOrEqual
	}

	/// <summary>
	/// State that branches based on whether the graph is running as a sub-graph.
	/// When the graph has a parent state machine, the "Yes" transition fires.
	/// When running standalone, the "No" transition fires.
	/// </summary>
	[StateInfo(
		"Branches based on sub-graph context",
		"Checks whether this graph is running as a sub-state machine within a parent graph. " +
		"Use this to make graphs that behave differently when standalone vs embedded — " +
		"for example, looping when standalone but exiting when used as a sub-graph."
	)]
	[StateStyle(Color = StateColors.Yellow, Icon = NodeIcon.Share)]
	public class IsSubGraphBranchState : State
	{
		[DataOut("Result")]
		public bool Result;

		[Transition("Yes", Color = StateColors.Green)]
		public Action OnYes;

		[Transition("No", Color = StateColors.Red)]
		public Action OnNo;

		public override void OnEnter()
		{
			Result = StateMachine?.Parent != null;

			if (Result)
			{
				OnYes?.Invoke();
			}
			else
			{
				OnNo?.Invoke();
			}
		}
	}

	/// <summary>
	/// State that compares two strings and branches accordingly.
	/// </summary>
	[StateInfo(
		"Compares two strings",
		"Compares two string values and transitions based on whether they match."
	)]
	[StateStyle(Color = StateColors.Yellow, Icon = NodeIcon.CheckCircle)]
	public class CompareStringState : State
	{
		/// <summary>
		/// The first string to compare.
		/// </summary>
		[DataIn("Value A")]
		public string ValueA;

		/// <summary>
		/// The second string to compare.
		/// </summary>
		[DataIn("Value B")]
		public string ValueB;

		/// <summary>
		/// If true, ignores case when comparing.
		/// </summary>
		[DataIn("Ignore Case")]
		public bool IgnoreCase = false;

		/// <summary>
		/// Outputs the comparison result.
		/// </summary>
		[DataOut("Result")]
		public bool Result;

		/// <summary>
		/// Transition that fires when strings are equal.
		/// </summary>
		[Transition("Equal", Color = StateColors.Green)]
		public Action OnEqual;

		/// <summary>
		/// Transition that fires when strings are not equal.
		/// </summary>
		[Transition("Not Equal", Color = StateColors.Red)]
		public Action OnNotEqual;

		public override void OnEnter()
		{
			var comparison = IgnoreCase
				? StringComparison.OrdinalIgnoreCase
				: StringComparison.Ordinal;

			Result = string.Equals(ValueA, ValueB, comparison);

			if (Result)
			{
				OnEqual?.Invoke();
			}
			else
			{
				OnNotEqual?.Invoke();
			}
		}
	}
}
