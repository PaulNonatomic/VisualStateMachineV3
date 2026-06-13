using System;
using System.Threading;
using System.Threading.Tasks;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.AsyncStates.States
{
	/// <summary>
	/// Simulates an asynchronous loading operation with progress reporting.
	/// Demonstrates OnEnterAsync with CancellationToken support.
	/// </summary>
	[StateInfo(
		"Async loading",
		"Simulates an async load with progress. Supports cancellation if a transition interrupts the load.")]
	[StateStyle(Color = StateColors.Teal, Icon = NodeIcon.Download)]
	public class AsyncLoadState : State
	{
		[DataIn("Load Duration")]
		public float LoadDuration = 2f;

		[DataOut("Progress")]
		public float Progress;

		[Transition("On Loaded")]
		public Action OnLoaded;

		[Transition("On Failed")]
		public Action OnFailed;

		public override async Task OnEnterAsync(CancellationToken ct)
		{
			Progress = 0f;
			Debug.Log("[Async] Loading started...");

			var elapsed = 0f;

			while (elapsed < LoadDuration)
			{
				ct.ThrowIfCancellationRequested();

				// Simulate incremental loading
				await Task.Yield();
				elapsed += Time.deltaTime;
				Progress = Mathf.Clamp01(elapsed / LoadDuration);

				// Write progress to blackboard for external monitoring
				Blackboard.Set("Load Progress", Progress);
			}

			Progress = 1f;
			Blackboard.Set("Load Progress", 1f);
			Debug.Log("[Async] Loading complete!");

			OnLoaded?.Invoke();
		}

		public override void OnExit()
		{
			if (Progress < 1f)
			{
				Debug.Log("[Async] Loading was cancelled before completion");
			}
		}
	}
}
