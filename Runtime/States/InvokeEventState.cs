using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// State that invokes a UnityEvent and immediately transitions.
	/// Useful for triggering external systems or callbacks from the state machine.
	/// </summary>
	[StateInfo(
		"Invokes a UnityEvent",
		"Invokes a configurable UnityEvent, allowing you to trigger external " +
		"methods, animations, sounds, or other Unity systems from your state machine. " +
		"The event is invoked immediately on state entry, then transitions to the next state."
	)]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.BroadcastRight)]
	public class InvokeEventState : State
	{
		/// <summary>
		/// The event to invoke when entering this state.
		/// </summary>
		[DataIn("Event")]
		public UnityEvent Event;

		/// <summary>
		/// Transition that fires after invoking the event.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			Event?.Invoke();
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that invokes a UnityEvent with a string parameter.
	/// </summary>
	[StateInfo(
		"Invokes a UnityEvent with a string parameter",
		"Invokes a UnityEvent that accepts a string parameter. Useful for triggering " +
		"methods that need text data like playing named animations or sending messages."
	)]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.BroadcastRight)]
	public class InvokeStringEventState : State
	{
		/// <summary>
		/// The event to invoke when entering this state.
		/// </summary>
		[DataIn("Event")]
		public UnityEvent<string> Event;

		/// <summary>
		/// The string value to pass to the event.
		/// </summary>
		[DataIn("Value")]
		public string Value;

		/// <summary>
		/// Transition that fires after invoking the event.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			Event?.Invoke(Value);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that invokes a UnityEvent with a float parameter.
	/// </summary>
	[StateInfo(
		"Invokes a UnityEvent with a float parameter",
		"Invokes a UnityEvent that accepts a float parameter. Useful for setting " +
		"values like volume, speed, or other numeric properties."
	)]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.BroadcastRight)]
	public class InvokeFloatEventState : State
	{
		/// <summary>
		/// The event to invoke when entering this state.
		/// </summary>
		[DataIn("Event")]
		public UnityEvent<float> Event;

		/// <summary>
		/// The float value to pass to the event.
		/// </summary>
		[DataIn("Value")]
		public float Value;

		/// <summary>
		/// Transition that fires after invoking the event.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			Event?.Invoke(Value);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that invokes a UnityEvent with an int parameter.
	/// </summary>
	[StateInfo(
		"Invokes a UnityEvent with an int parameter",
		"Invokes a UnityEvent that accepts an int parameter. Useful for setting " +
		"indices, counts, or other integer values."
	)]
	[StateStyle(Color = StateColors.Purple, Icon = NodeIcon.BroadcastRight)]
	public class InvokeIntEventState : State
	{
		/// <summary>
		/// The event to invoke when entering this state.
		/// </summary>
		[DataIn("Event")]
		public UnityEvent<int> Event;

		/// <summary>
		/// The int value to pass to the event.
		/// </summary>
		[DataIn("Value")]
		public int Value;

		/// <summary>
		/// Transition that fires after invoking the event.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			Event?.Invoke(Value);
			OnComplete?.Invoke();
		}
	}
}
