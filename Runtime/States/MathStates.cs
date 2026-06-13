using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// Adds two integer values.
	/// </summary>
	[StateInfo(
		"Adds two integers",
		"Outputs the sum of A and B as an integer.",
		Category = "Math")]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.PlusCircle)]
	public class AddIntState : State
	{
		[DataIn("A")]
		public int A;

		[DataIn("B")]
		public int B = 1;

		[DataOut("Result")]
		public int Result;

		[Transition]
		public Action OnComplete;

		public override void OnEnter()
		{
			Result = A + B;
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// Subtracts one integer from another.
	/// </summary>
	[StateInfo(
		"Subtracts two integers",
		"Outputs A minus B as an integer.",
		Category = "Math")]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.PlusCircle)]
	public class SubtractIntState : State
	{
		[DataIn("A")]
		public int A;

		[DataIn("B")]
		public int B = 1;

		[DataOut("Result")]
		public int Result;

		[Transition]
		public Action OnComplete;

		public override void OnEnter()
		{
			Result = A - B;
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// Multiplies two integer values.
	/// </summary>
	[StateInfo(
		"Multiplies two integers",
		"Outputs the product of A and B as an integer.",
		Category = "Math")]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.PlusCircle)]
	public class MultiplyIntState : State
	{
		[DataIn("A")]
		public int A;

		[DataIn("B")]
		public int B = 2;

		[DataOut("Result")]
		public int Result;

		[Transition]
		public Action OnComplete;

		public override void OnEnter()
		{
			Result = A * B;
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// Adds two float values.
	/// </summary>
	[StateInfo(
		"Adds two floats",
		"Outputs the sum of A and B as a float.",
		Category = "Math")]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.PlusCircle)]
	public class AddFloatState : State
	{
		[DataIn("A")]
		public float A;

		[DataIn("B")]
		public float B = 1f;

		[DataOut("Result")]
		public float Result;

		[Transition]
		public Action OnComplete;

		public override void OnEnter()
		{
			Result = A + B;
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// Subtracts one float from another.
	/// </summary>
	[StateInfo(
		"Subtracts two floats",
		"Outputs A minus B as a float.",
		Category = "Math")]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.PlusCircle)]
	public class SubtractFloatState : State
	{
		[DataIn("A")]
		public float A;

		[DataIn("B")]
		public float B = 1f;

		[DataOut("Result")]
		public float Result;

		[Transition]
		public Action OnComplete;

		public override void OnEnter()
		{
			Result = A - B;
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// Multiplies two float values.
	/// </summary>
	[StateInfo(
		"Multiplies two floats",
		"Outputs the product of A and B as a float.",
		Category = "Math")]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.PlusCircle)]
	public class MultiplyFloatState : State
	{
		[DataIn("A")]
		public float A;

		[DataIn("B")]
		public float B = 2f;

		[DataOut("Result")]
		public float Result;

		[Transition]
		public Action OnComplete;

		public override void OnEnter()
		{
			Result = A * B;
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// Divides one float by another with division-by-zero protection.
	/// </summary>
	[StateInfo(
		"Divides two floats",
		"Outputs A divided by B. Returns 0 if B is zero.",
		Category = "Math")]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.PlusCircle)]
	public class DivideFloatState : State
	{
		[DataIn("A")]
		public float A;

		[DataIn("B")]
		public float B = 1f;

		[DataOut("Result")]
		public float Result;

		[Transition]
		public Action OnComplete;

		public override void OnEnter()
		{
			Result = Mathf.Approximately(B, 0f) ? 0f : A / B;
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// Computes the modulo (remainder) of two integers.
	/// </summary>
	[StateInfo(
		"Integer modulo",
		"Outputs A mod B. Returns 0 if B is zero.",
		Category = "Math")]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.PlusCircle)]
	public class ModIntState : State
	{
		[DataIn("A")]
		public int A;

		[DataIn("B")]
		public int B = 2;

		[DataOut("Result")]
		public int Result;

		[Transition]
		public Action OnComplete;

		public override void OnEnter()
		{
			Result = B != 0 ? A % B : 0;
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// Clamps an integer between a minimum and maximum value.
	/// </summary>
	[StateInfo(
		"Clamps an integer",
		"Constrains Value to lie between Min and Max.",
		Category = "Math")]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.RulerArrows)]
	public class ClampIntState : State
	{
		[DataIn("Value")]
		public int Value;

		[DataIn("Min")]
		public int Min = 0;

		[DataIn("Max")]
		public int Max = 100;

		[DataOut("Result")]
		public int Result;

		[Transition]
		public Action OnComplete;

		public override void OnEnter()
		{
			Result = Mathf.Clamp(Value, Min, Max);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// Clamps a float between a minimum and maximum value.
	/// </summary>
	[StateInfo(
		"Clamps a float",
		"Constrains Value to lie between Min and Max.",
		Category = "Math")]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.RulerArrows)]
	public class ClampFloatState : State
	{
		[DataIn("Value")]
		public float Value;

		[DataIn("Min")]
		public float Min = 0f;

		[DataIn("Max")]
		public float Max = 1f;

		[DataOut("Result")]
		public float Result;

		[Transition]
		public Action OnComplete;

		public override void OnEnter()
		{
			Result = Mathf.Clamp(Value, Min, Max);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// Returns the minimum of two float values.
	/// </summary>
	[StateInfo(
		"Minimum of two floats",
		"Outputs the smaller of A and B.",
		Category = "Math")]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.ArrowDown)]
	public class MinFloatState : State
	{
		[DataIn("A")]
		public float A;

		[DataIn("B")]
		public float B;

		[DataOut("Result")]
		public float Result;

		[Transition]
		public Action OnComplete;

		public override void OnEnter()
		{
			Result = Mathf.Min(A, B);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// Returns the maximum of two float values.
	/// </summary>
	[StateInfo(
		"Maximum of two floats",
		"Outputs the larger of A and B.",
		Category = "Math")]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.ArrowUp)]
	public class MaxFloatState : State
	{
		[DataIn("A")]
		public float A;

		[DataIn("B")]
		public float B;

		[DataOut("Result")]
		public float Result;

		[Transition]
		public Action OnComplete;

		public override void OnEnter()
		{
			Result = Mathf.Max(A, B);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// Linearly interpolates between two float values.
	/// </summary>
	[StateInfo(
		"Lerps between two floats",
		"Outputs the linear interpolation between A and B at parameter T (0-1).",
		Category = "Math")]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.CurveArray)]
	public class LerpFloatState : State
	{
		[DataIn("A")]
		public float A;

		[DataIn("B")]
		public float B = 1f;

		[DataIn("T")]
		public float T = 0.5f;

		[DataOut("Result")]
		public float Result;

		[Transition]
		public Action OnComplete;

		public override void OnEnter()
		{
			Result = Mathf.Lerp(A, B, T);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// Generates a random integer between Min (inclusive) and Max (exclusive).
	/// </summary>
	[StateInfo(
		"Random integer",
		"Outputs a random integer in the range [Min, Max).",
		Category = "Math")]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.Random)]
	public class RandomIntState : State
	{
		[DataIn("Min")]
		public int Min = 0;

		[DataIn("Max")]
		public int Max = 100;

		[DataOut("Result")]
		public int Result;

		[Transition]
		public Action OnComplete;

		public override void OnEnter()
		{
			Result = UnityEngine.Random.Range(Min, Max);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// Generates a random float between Min (inclusive) and Max (inclusive).
	/// </summary>
	[StateInfo(
		"Random float",
		"Outputs a random float in the range [Min, Max].",
		Category = "Math")]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.Random)]
	public class RandomFloatState : State
	{
		[DataIn("Min")]
		public float Min = 0f;

		[DataIn("Max")]
		public float Max = 1f;

		[DataOut("Result")]
		public float Result;

		[Transition]
		public Action OnComplete;

		public override void OnEnter()
		{
			Result = UnityEngine.Random.Range(Min, Max);
			OnComplete?.Invoke();
		}
	}
}
