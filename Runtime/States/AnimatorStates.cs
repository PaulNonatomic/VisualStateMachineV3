using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// State that sets a trigger parameter on an Animator and immediately transitions.
	/// </summary>
	[StateInfo(
		"Sets an Animator trigger parameter",
		"Sets a trigger parameter on the target Animator component. If no Animator is " +
		"specified, attempts to get one from the context GameObject. Triggers are " +
		"automatically reset by Unity after being consumed."
	)]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Film)]
	public class SetAnimatorTriggerState : State
	{
		/// <summary>
		/// The Animator to control. If null, uses the context GameObject's Animator.
		/// </summary>
		[DataIn("Animator")]
		public Animator Animator;

		/// <summary>
		/// The name of the trigger parameter to set.
		/// </summary>
		[DataIn("Trigger Name")]
		public string TriggerName;

		/// <summary>
		/// Transition that fires after setting the trigger.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		private Animator _cachedAnimator;

		public override void OnEnter()
		{
			var animator = Animator ?? (_cachedAnimator ??= GetComponent<Animator>());

			if (animator == null)
			{
				LogWarning("No Animator found");
				OnComplete?.Invoke();
				return;
			}

			if (string.IsNullOrEmpty(TriggerName))
			{
				LogWarning("No trigger name specified");
				OnComplete?.Invoke();
				return;
			}

			animator.SetTrigger(TriggerName);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that sets a bool parameter on an Animator and immediately transitions.
	/// </summary>
	[StateInfo(
		"Sets an Animator bool parameter",
		"Sets a bool parameter on the target Animator component. Useful for controlling " +
		"blend trees or animation states that depend on boolean conditions."
	)]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Film)]
	public class SetAnimatorBoolState : State
	{
		/// <summary>
		/// The Animator to control. If null, uses the context GameObject's Animator.
		/// </summary>
		[DataIn("Animator")]
		public Animator Animator;

		/// <summary>
		/// The name of the bool parameter to set.
		/// </summary>
		[DataIn("Parameter Name")]
		public string ParameterName;

		/// <summary>
		/// The value to set.
		/// </summary>
		[DataIn("Value")]
		public bool Value = true;

		/// <summary>
		/// Transition that fires after setting the parameter.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		private Animator _cachedAnimator;

		public override void OnEnter()
		{
			var animator = Animator ?? (_cachedAnimator ??= GetComponent<Animator>());

			if (animator == null)
			{
				LogWarning("No Animator found");
				OnComplete?.Invoke();
				return;
			}

			if (string.IsNullOrEmpty(ParameterName))
			{
				LogWarning("No parameter name specified");
				OnComplete?.Invoke();
				return;
			}

			animator.SetBool(ParameterName, Value);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that sets a float parameter on an Animator and immediately transitions.
	/// </summary>
	[StateInfo(
		"Sets an Animator float parameter",
		"Sets a float parameter on the target Animator component. Useful for controlling " +
		"blend trees, animation speed, or other float-based animation parameters."
	)]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Film)]
	public class SetAnimatorFloatState : State
	{
		/// <summary>
		/// The Animator to control. If null, uses the context GameObject's Animator.
		/// </summary>
		[DataIn("Animator")]
		public Animator Animator;

		/// <summary>
		/// The name of the float parameter to set.
		/// </summary>
		[DataIn("Parameter Name")]
		public string ParameterName;

		/// <summary>
		/// The value to set.
		/// </summary>
		[DataIn("Value")]
		public float Value;

		/// <summary>
		/// Transition that fires after setting the parameter.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		private Animator _cachedAnimator;

		public override void OnEnter()
		{
			var animator = Animator ?? (_cachedAnimator ??= GetComponent<Animator>());

			if (animator == null)
			{
				LogWarning("No Animator found");
				OnComplete?.Invoke();
				return;
			}

			if (string.IsNullOrEmpty(ParameterName))
			{
				LogWarning("No parameter name specified");
				OnComplete?.Invoke();
				return;
			}

			animator.SetFloat(ParameterName, Value);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that sets an integer parameter on an Animator and immediately transitions.
	/// </summary>
	[StateInfo(
		"Sets an Animator integer parameter",
		"Sets an integer parameter on the target Animator component. Useful for " +
		"controlling state machine indices or discrete animation states."
	)]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Film)]
	public class SetAnimatorIntState : State
	{
		/// <summary>
		/// The Animator to control. If null, uses the context GameObject's Animator.
		/// </summary>
		[DataIn("Animator")]
		public Animator Animator;

		/// <summary>
		/// The name of the integer parameter to set.
		/// </summary>
		[DataIn("Parameter Name")]
		public string ParameterName;

		/// <summary>
		/// The value to set.
		/// </summary>
		[DataIn("Value")]
		public int Value;

		/// <summary>
		/// Transition that fires after setting the parameter.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		private Animator _cachedAnimator;

		public override void OnEnter()
		{
			var animator = Animator ?? (_cachedAnimator ??= GetComponent<Animator>());

			if (animator == null)
			{
				LogWarning("No Animator found");
				OnComplete?.Invoke();
				return;
			}

			if (string.IsNullOrEmpty(ParameterName))
			{
				LogWarning("No parameter name specified");
				OnComplete?.Invoke();
				return;
			}

			animator.SetInteger(ParameterName, Value);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that waits for an Animator to reach a specific state.
	/// </summary>
	[StateInfo(
		"Waits for an Animator state",
		"Waits until the Animator enters or exits a specific animation state. " +
		"Useful for synchronizing state machine transitions with animation playback."
	)]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Film)]
	public class WaitForAnimatorStateState : State
	{
		/// <summary>
		/// The Animator to monitor. If null, uses the context GameObject's Animator.
		/// </summary>
		[DataIn("Animator")]
		public Animator Animator;

		/// <summary>
		/// The name of the animation state to wait for.
		/// </summary>
		[DataIn("State Name")]
		public string StateName;

		/// <summary>
		/// The layer index to check (default is 0).
		/// </summary>
		[DataIn("Layer")]
		public int Layer = 0;

		/// <summary>
		/// If true, waits for the state to be entered.
		/// If false, waits for the state to be exited.
		/// </summary>
		[DataIn("Wait For Entry")]
		public bool WaitForEntry = true;

		/// <summary>
		/// Outputs the normalized time when the condition was met.
		/// </summary>
		[DataOut("Normalized Time")]
		public float NormalizedTime;

		/// <summary>
		/// Transition that fires when the condition is met.
		/// </summary>
		[Transition("Complete")]
		public Action OnComplete;

		private Animator _cachedAnimator;
		private Animator _animator;
		private int _stateHash;
		private bool _wasInState;

		public override void OnEnter()
		{
			_animator = Animator ?? (_cachedAnimator ??= GetComponent<Animator>());

			if (_animator == null)
			{
				LogWarning("No Animator found");
				OnComplete?.Invoke();
				return;
			}

			if (string.IsNullOrEmpty(StateName))
			{
				LogWarning("No state name specified");
				OnComplete?.Invoke();
				return;
			}

			_stateHash = Animator.StringToHash(StateName);
			var stateInfo = _animator.GetCurrentAnimatorStateInfo(Layer);
			_wasInState = stateInfo.shortNameHash == _stateHash;

			// If waiting for entry and already in state, complete immediately
			if (WaitForEntry && _wasInState)
			{
				NormalizedTime = stateInfo.normalizedTime;
				OnComplete?.Invoke();
			}
		}

		public override void OnUpdate()
		{
			if (_animator == null) return;

			var stateInfo = _animator.GetCurrentAnimatorStateInfo(Layer);
			var isInState = stateInfo.shortNameHash == _stateHash;

			if (WaitForEntry)
			{
				// Wait for state to be entered
				if (!_wasInState && isInState)
				{
					NormalizedTime = stateInfo.normalizedTime;
					OnComplete?.Invoke();
				}
			}
			else
			{
				// Wait for state to be exited
				if (_wasInState && !isInState)
				{
					NormalizedTime = stateInfo.normalizedTime;
					OnComplete?.Invoke();
				}
			}

			_wasInState = isInState;
		}

		public override void OnExit()
		{
			_animator = null;
			_stateHash = 0;
			_wasInState = false;
		}
	}

	/// <summary>
	/// State that crossfades to an Animator state and immediately transitions.
	/// </summary>
	[StateInfo(
		"Crossfades to an Animator state",
		"Performs a smooth crossfade transition to the specified animation state. " +
		"Useful for blending between animations without abrupt changes."
	)]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Film)]
	public class CrossFadeAnimatorState : State
	{
		/// <summary>
		/// The Animator to control. If null, uses the context GameObject's Animator.
		/// </summary>
		[DataIn("Animator")]
		public Animator Animator;

		/// <summary>
		/// The name of the animation state to crossfade to.
		/// </summary>
		[DataIn("State Name")]
		public string StateName;

		/// <summary>
		/// The duration of the crossfade transition in seconds.
		/// </summary>
		[DataIn("Transition Duration")]
		public float TransitionDuration = 0.25f;

		/// <summary>
		/// The layer index to crossfade on (default is 0).
		/// </summary>
		[DataIn("Layer")]
		public int Layer = 0;

		/// <summary>
		/// The normalized time offset to start the target state at.
		/// </summary>
		[DataIn("Time Offset")]
		public float TimeOffset = 0f;

		/// <summary>
		/// Transition that fires after starting the crossfade.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		private Animator _cachedAnimator;

		public override void OnEnter()
		{
			var animator = Animator ?? (_cachedAnimator ??= GetComponent<Animator>());

			if (animator == null)
			{
				LogWarning("No Animator found");
				OnComplete?.Invoke();
				return;
			}

			if (string.IsNullOrEmpty(StateName))
			{
				LogWarning("No state name specified");
				OnComplete?.Invoke();
				return;
			}

			animator.CrossFade(StateName, TransitionDuration, Layer, TimeOffset);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that plays an Animator state directly and immediately transitions.
	/// </summary>
	[StateInfo(
		"Plays an Animator state directly",
		"Plays the specified animation state immediately without crossfading. " +
		"Useful for cutscenes or when an instant state change is needed."
	)]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Film)]
	public class PlayAnimatorState : State
	{
		/// <summary>
		/// The Animator to control. If null, uses the context GameObject's Animator.
		/// </summary>
		[DataIn("Animator")]
		public Animator Animator;

		/// <summary>
		/// The name of the animation state to play.
		/// </summary>
		[DataIn("State Name")]
		public string StateName;

		/// <summary>
		/// The layer index to play on (default is 0).
		/// </summary>
		[DataIn("Layer")]
		public int Layer = 0;

		/// <summary>
		/// The normalized time to start playing at.
		/// </summary>
		[DataIn("Normalized Time")]
		public float NormalizedTime = 0f;

		/// <summary>
		/// Transition that fires after starting playback.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		private Animator _cachedAnimator;

		public override void OnEnter()
		{
			var animator = Animator ?? (_cachedAnimator ??= GetComponent<Animator>());

			if (animator == null)
			{
				LogWarning("No Animator found");
				OnComplete?.Invoke();
				return;
			}

			if (string.IsNullOrEmpty(StateName))
			{
				LogWarning("No state name specified");
				OnComplete?.Invoke();
				return;
			}

			animator.Play(StateName, Layer, NormalizedTime);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that reads a float parameter from an Animator and immediately transitions.
	/// </summary>
	[StateInfo(
		"Gets an Animator float parameter",
		"Reads a float parameter from the target Animator and outputs its value. " +
		"Useful for reading blend tree weights or other animation parameters."
	)]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Film)]
	public class GetAnimatorFloatState : State
	{
		/// <summary>
		/// The Animator to read from. If null, uses the context GameObject's Animator.
		/// </summary>
		[DataIn("Animator")]
		public Animator Animator;

		/// <summary>
		/// The name of the float parameter to read.
		/// </summary>
		[DataIn("Parameter Name")]
		public string ParameterName;

		/// <summary>
		/// The current value of the parameter.
		/// </summary>
		[DataOut("Value")]
		public float Value;

		/// <summary>
		/// Transition that fires after reading the parameter.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		private Animator _cachedAnimator;

		public override void OnEnter()
		{
			var animator = Animator ?? (_cachedAnimator ??= GetComponent<Animator>());

			if (animator == null)
			{
				LogWarning("No Animator found");
				OnComplete?.Invoke();
				return;
			}

			if (string.IsNullOrEmpty(ParameterName))
			{
				LogWarning("No parameter name specified");
				OnComplete?.Invoke();
				return;
			}

			Value = animator.GetFloat(ParameterName);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that reads a bool parameter from an Animator and branches on the result.
	/// </summary>
	[StateInfo(
		"Gets an Animator bool and branches",
		"Reads a bool parameter from the target Animator and branches based on its " +
		"value. Similar to ConditionState but reads directly from the Animator."
	)]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Film)]
	public class GetAnimatorBoolState : State
	{
		/// <summary>
		/// The Animator to read from. If null, uses the context GameObject's Animator.
		/// </summary>
		[DataIn("Animator")]
		public Animator Animator;

		/// <summary>
		/// The name of the bool parameter to read.
		/// </summary>
		[DataIn("Parameter Name")]
		public string ParameterName;

		/// <summary>
		/// The current value of the parameter.
		/// </summary>
		[DataOut("Value")]
		public bool Value;

		/// <summary>
		/// Transition that fires when the parameter is true.
		/// </summary>
		[Transition("True")]
		public Action OnTrue;

		/// <summary>
		/// Transition that fires when the parameter is false.
		/// </summary>
		[Transition("False")]
		public Action OnFalse;

		private Animator _cachedAnimator;

		public override void OnEnter()
		{
			var animator = Animator ?? (_cachedAnimator ??= GetComponent<Animator>());

			if (animator == null)
			{
				LogWarning("No Animator found");
				OnFalse?.Invoke();
				return;
			}

			if (string.IsNullOrEmpty(ParameterName))
			{
				LogWarning("No parameter name specified");
				OnFalse?.Invoke();
				return;
			}

			Value = animator.GetBool(ParameterName);

			if (Value)
				OnTrue?.Invoke();
			else
				OnFalse?.Invoke();
		}
	}

	/// <summary>
	/// State that reads an integer parameter from an Animator and immediately transitions.
	/// </summary>
	[StateInfo(
		"Gets an Animator integer parameter",
		"Reads an integer parameter from the target Animator and outputs its value. " +
		"Useful for reading state indices or discrete animation parameters."
	)]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Film)]
	public class GetAnimatorIntState : State
	{
		/// <summary>
		/// The Animator to read from. If null, uses the context GameObject's Animator.
		/// </summary>
		[DataIn("Animator")]
		public Animator Animator;

		/// <summary>
		/// The name of the integer parameter to read.
		/// </summary>
		[DataIn("Parameter Name")]
		public string ParameterName;

		/// <summary>
		/// The current value of the parameter.
		/// </summary>
		[DataOut("Value")]
		public int Value;

		/// <summary>
		/// Transition that fires after reading the parameter.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		private Animator _cachedAnimator;

		public override void OnEnter()
		{
			var animator = Animator ?? (_cachedAnimator ??= GetComponent<Animator>());

			if (animator == null)
			{
				LogWarning("No Animator found");
				OnComplete?.Invoke();
				return;
			}

			if (string.IsNullOrEmpty(ParameterName))
			{
				LogWarning("No parameter name specified");
				OnComplete?.Invoke();
				return;
			}

			Value = animator.GetInteger(ParameterName);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that sets the Animator playback speed and immediately transitions.
	/// </summary>
	[StateInfo(
		"Sets Animator playback speed",
		"Sets the playback speed of the target Animator. A value of 1 is normal speed, " +
		"0 pauses, and values above 1 speed up playback. Useful for slow-motion effects."
	)]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Film)]
	public class SetAnimatorSpeedState : State
	{
		/// <summary>
		/// The Animator to control. If null, uses the context GameObject's Animator.
		/// </summary>
		[DataIn("Animator")]
		public Animator Animator;

		/// <summary>
		/// The playback speed to set.
		/// </summary>
		[DataIn("Speed")]
		public float Speed = 1f;

		/// <summary>
		/// Transition that fires after setting the speed.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		private Animator _cachedAnimator;

		public override void OnEnter()
		{
			var animator = Animator ?? (_cachedAnimator ??= GetComponent<Animator>());

			if (animator == null)
			{
				LogWarning("No Animator found");
				OnComplete?.Invoke();
				return;
			}

			animator.speed = Speed;
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that sets an Animator layer weight and immediately transitions.
	/// </summary>
	[StateInfo(
		"Sets Animator layer weight",
		"Sets the blending weight of an Animator layer. A weight of 0 disables the " +
		"layer, 1 applies it fully. Useful for animation layer blending."
	)]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Film)]
	public class SetAnimatorLayerWeightState : State
	{
		/// <summary>
		/// The Animator to control. If null, uses the context GameObject's Animator.
		/// </summary>
		[DataIn("Animator")]
		public Animator Animator;

		/// <summary>
		/// The layer index to set the weight on.
		/// </summary>
		[DataIn("Layer")]
		public int Layer = 0;

		/// <summary>
		/// The weight to set (0 to 1).
		/// </summary>
		[DataIn("Weight")]
		public float Weight = 1f;

		/// <summary>
		/// Transition that fires after setting the weight.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		private Animator _cachedAnimator;

		public override void OnEnter()
		{
			var animator = Animator ?? (_cachedAnimator ??= GetComponent<Animator>());

			if (animator == null)
			{
				LogWarning("No Animator found");
				OnComplete?.Invoke();
				return;
			}

			animator.SetLayerWeight(Layer, Weight);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that resets a trigger parameter on an Animator and immediately transitions.
	/// </summary>
	[StateInfo(
		"Resets an Animator trigger parameter",
		"Resets a trigger parameter on the target Animator component. Useful for " +
		"cancelling a trigger that hasn't been consumed yet."
	)]
	[StateStyle(Color = StateColors.Orange, Icon = NodeIcon.Film)]
	public class ResetAnimatorTriggerState : State
	{
		/// <summary>
		/// The Animator to control. If null, uses the context GameObject's Animator.
		/// </summary>
		[DataIn("Animator")]
		public Animator Animator;

		/// <summary>
		/// The name of the trigger parameter to reset.
		/// </summary>
		[DataIn("Trigger Name")]
		public string TriggerName;

		/// <summary>
		/// Transition that fires after resetting the trigger.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		private Animator _cachedAnimator;

		public override void OnEnter()
		{
			var animator = Animator ?? (_cachedAnimator ??= GetComponent<Animator>());

			if (animator == null)
			{
				LogWarning("No Animator found");
				OnComplete?.Invoke();
				return;
			}

			if (string.IsNullOrEmpty(TriggerName))
			{
				LogWarning("No trigger name specified");
				OnComplete?.Invoke();
				return;
			}

			animator.ResetTrigger(TriggerName);
			OnComplete?.Invoke();
		}
	}
}
