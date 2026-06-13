using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// State that randomly selects between two transitions.
	/// Useful for adding unpredictability to AI behaviors or game events.
	/// </summary>
	[StateInfo(
		"Randomly selects between two paths",
		"Randomly chooses between Path A and Path B based on the configured probability. " +
		"A probability of 0.5 gives equal chance to both paths. Lower values favor Path B, " +
		"higher values favor Path A."
	)]
	[StateStyle(Color = StateColors.Yellow, Icon = NodeIcon.Random)]
	public class RandomBranchState : State
	{
		/// <summary>
		/// Probability of taking Path A (0 to 1).
		/// 0 = always Path B, 1 = always Path A, 0.5 = equal chance.
		/// </summary>
		[DataIn("Probability A")]
		public float ProbabilityA = 0.5f;

		/// <summary>
		/// Outputs which path was taken (true = A, false = B).
		/// </summary>
		[DataOut("Took Path A")]
		public bool TookPathA;

		/// <summary>
		/// Transition for Path A.
		/// </summary>
		[Transition("Path A")]
		public Action OnPathA;

		/// <summary>
		/// Transition for Path B.
		/// </summary>
		[Transition("Path B")]
		public Action OnPathB;

		public override void OnEnter()
		{
			var roll = UnityEngine.Random.value;
			TookPathA = roll < ProbabilityA;

			if (TookPathA)
			{
				OnPathA?.Invoke();
			}
			else
			{
				OnPathB?.Invoke();
			}
		}
	}

	/// <summary>
	/// State that randomly selects between three transitions.
	/// </summary>
	[StateInfo(
		"Randomly selects between three paths",
		"Randomly chooses between Path A, Path B, and Path C based on their configured weights. " +
		"Weights are relative - a weight of 2 is twice as likely as a weight of 1."
	)]
	[StateStyle(Color = StateColors.Yellow, Icon = NodeIcon.Random)]
	public class RandomBranch3State : State
	{
		/// <summary>
		/// Weight for Path A (relative to other weights).
		/// </summary>
		[DataIn("Weight A")]
		public float WeightA = 1f;

		/// <summary>
		/// Weight for Path B (relative to other weights).
		/// </summary>
		[DataIn("Weight B")]
		public float WeightB = 1f;

		/// <summary>
		/// Weight for Path C (relative to other weights).
		/// </summary>
		[DataIn("Weight C")]
		public float WeightC = 1f;

		/// <summary>
		/// Outputs which path was taken (0 = A, 1 = B, 2 = C).
		/// </summary>
		[DataOut("Selected Path")]
		public int SelectedPath;

		/// <summary>
		/// Transition for Path A.
		/// </summary>
		[Transition("Path A")]
		public Action OnPathA;

		/// <summary>
		/// Transition for Path B.
		/// </summary>
		[Transition("Path B")]
		public Action OnPathB;

		/// <summary>
		/// Transition for Path C.
		/// </summary>
		[Transition("Path C")]
		public Action OnPathC;

		public override void OnEnter()
		{
			var totalWeight = WeightA + WeightB + WeightC;
			if (totalWeight <= 0f)
			{
				LogWarning("All weights are zero or negative, defaulting to Path A");
				SelectedPath = 0;
				OnPathA?.Invoke();
				return;
			}

			var roll = UnityEngine.Random.value * totalWeight;

			if (roll < WeightA)
			{
				SelectedPath = 0;
				OnPathA?.Invoke();
			}
			else if (roll < WeightA + WeightB)
			{
				SelectedPath = 1;
				OnPathB?.Invoke();
			}
			else
			{
				SelectedPath = 2;
				OnPathC?.Invoke();
			}
		}
	}
}
