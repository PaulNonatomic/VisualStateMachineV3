using System;
using System.Threading;
using System.Threading.Tasks;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.AsyncStates.States
{
	/// <summary>
	/// Performs async initialization with multiple sequential steps.
	/// Demonstrates structured async work with cancellation checkpoints.
	/// </summary>
	[StateInfo(
		"Async initialization",
		"Runs multiple async init steps in sequence with cancellation support between each step.")]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.Command)]
	public class AsyncInitializeState : State
	{
		[DataIn("Step Count")]
		public int StepCount = 3;

		[DataIn("Step Delay")]
		public float StepDelay = 0.5f;

		[DataOut("Steps Completed")]
		public int StepsCompleted;

		[Transition("On Ready")]
		public Action OnReady;

		public override async Task OnEnterAsync(CancellationToken ct)
		{
			StepsCompleted = 0;
			Debug.Log($"[Async] Initializing ({StepCount} steps)...");

			for (var i = 0; i < StepCount; i++)
			{
				ct.ThrowIfCancellationRequested();

				// Simulate async work per step
				var stepElapsed = 0f;
				while (stepElapsed < StepDelay)
				{
					ct.ThrowIfCancellationRequested();
					await Task.Yield();
					stepElapsed += Time.deltaTime;
				}

				StepsCompleted = i + 1;
				Debug.Log($"[Async] Step {StepsCompleted}/{StepCount} complete");
			}

			Debug.Log("[Async] Initialization finished!");
			OnReady?.Invoke();
		}
	}
}
