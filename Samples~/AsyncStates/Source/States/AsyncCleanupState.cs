using System;
using System.Threading;
using System.Threading.Tasks;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.Samples.AsyncStates.States
{
	/// <summary>
	/// Demonstrates async cleanup in OnExitAsync.
	/// Useful for saving data, fading out, or releasing resources before leaving a state.
	/// </summary>
	[StateInfo(
		"Active with async cleanup",
		"Main active state. On exit, performs async cleanup (simulated save/fade).")]
	[StateStyle(Color = StateColors.Green, Icon = NodeIcon.Play)]
	public class AsyncCleanupState : State
	{
		[DataIn("Cleanup Duration")]
		public float CleanupDuration = 1f;

		[Transition("On Done")]
		public Action OnDone;

		private float _timeInState;

		public override void OnEnter()
		{
			_timeInState = 0f;
			Debug.Log("[Async] Active state entered. Press SPACE to trigger exit with async cleanup.");
		}

		public override void OnUpdate()
		{
			_timeInState += Time.deltaTime;

			if (Input.GetKeyDown(KeyCode.Space))
			{
				OnDone?.Invoke();
			}
		}

		public override async Task OnExitAsync(CancellationToken ct)
		{
			Debug.Log($"[Async] Cleaning up after {_timeInState:F1}s in state...");

			var elapsed = 0f;
			while (elapsed < CleanupDuration)
			{
				ct.ThrowIfCancellationRequested();
				await Task.Yield();
				elapsed += Time.deltaTime;
			}

			Debug.Log("[Async] Cleanup complete!");
		}
	}
}
