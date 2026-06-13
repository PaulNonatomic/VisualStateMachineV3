using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// State that destroys a GameObject and immediately transitions.
	/// </summary>
	[StateInfo(
		"Destroys a GameObject",
		"Destroys the target GameObject. If no target is specified, destroys the " +
		"context GameObject (which will also stop the state machine). Can optionally " +
		"delay destruction by a specified number of seconds."
	)]
	[StateStyle(Color = StateColors.Red, Icon = NodeIcon.Trash)]
	public class DestroyState : State
	{
		/// <summary>
		/// The GameObject to destroy. If null, destroys the context GameObject.
		/// </summary>
		[DataIn("Target")]
		public GameObject Target;

		/// <summary>
		/// Delay before destruction in seconds. 0 = immediate.
		/// </summary>
		[DataIn("Delay")]
		public float Delay = 0f;

		/// <summary>
		/// Transition that fires after initiating destruction.
		/// Note: This fires immediately, even if destruction is delayed.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			var target = Target != null ? Target : GameObject;

			if (target == null)
			{
				LogWarning("No target GameObject to destroy");
				OnComplete?.Invoke();
				return;
			}

			if (Delay > 0f)
			{
				UnityEngine.Object.Destroy(target, Delay);
			}
			else
			{
				UnityEngine.Object.Destroy(target);
			}

			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that destroys a GameObject immediately using DestroyImmediate.
	/// Use with caution - prefer DestroyState for runtime use.
	/// </summary>
	[StateInfo(
		"Destroys a GameObject immediately",
		"Destroys the target GameObject using DestroyImmediate. This is mainly " +
		"useful in editor scripts. For runtime use, prefer DestroyState instead. " +
		"Warning: DestroyImmediate can cause issues if used incorrectly."
	)]
	[StateStyle(Color = StateColors.Red, Icon = NodeIcon.Trash)]
	public class DestroyImmediateState : State
	{
		/// <summary>
		/// The GameObject to destroy. If null, destroys the context GameObject.
		/// </summary>
		[DataIn("Target")]
		public GameObject Target;

		/// <summary>
		/// Transition that fires after destruction.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			var target = Target != null ? Target : GameObject;

			if (target == null)
			{
				LogWarning("No target GameObject to destroy");
				OnComplete?.Invoke();
				return;
			}

			UnityEngine.Object.DestroyImmediate(target);
			OnComplete?.Invoke();
		}
	}
}
