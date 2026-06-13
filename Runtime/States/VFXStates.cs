#if VSM_VFX_GRAPH
using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;
using UnityEngine.VFX;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// State that plays a VFX Graph effect and immediately transitions.
	/// Requires the Visual Effect Graph package (com.unity.visualeffectgraph).
	/// </summary>
	[StateInfo(
		"Plays a VFX Graph effect (fire and forget)",
		"Plays a Visual Effect on the specified VisualEffect component or the " +
		"context GameObject. Transitions immediately without waiting for completion."
	)]
	[StateStyle(Color = StateColors.Violet, Icon = NodeIcon.Sparks)]
	public class PlayVFXState : State
	{
		/// <summary>
		/// The VisualEffect component to play. If null, attempts to get one from the context GameObject.
		/// </summary>
		[DataIn("Visual Effect")]
		public VisualEffect VisualEffect;

		/// <summary>
		/// Transition that fires after starting playback.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		private VisualEffect _cachedVisualEffect;

		public override void OnEnter()
		{
			var vfx = VisualEffect ?? (_cachedVisualEffect ??= GetComponent<VisualEffect>());

			if (vfx == null)
			{
				LogWarning("No VisualEffect found");
				OnComplete?.Invoke();
				return;
			}

			vfx.Play();
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that stops a VFX Graph effect and immediately transitions.
	/// Requires the Visual Effect Graph package (com.unity.visualeffectgraph).
	/// </summary>
	[StateInfo(
		"Stops a VFX Graph effect",
		"Stops a Visual Effect on the specified VisualEffect component. " +
		"Transitions immediately after stopping."
	)]
	[StateStyle(Color = StateColors.Violet, Icon = NodeIcon.Sparks)]
	public class StopVFXState : State
	{
		/// <summary>
		/// The VisualEffect component to stop. If null, attempts to get one from the context GameObject.
		/// </summary>
		[DataIn("Visual Effect")]
		public VisualEffect VisualEffect;

		/// <summary>
		/// Transition that fires after stopping.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		private VisualEffect _cachedVisualEffect;

		public override void OnEnter()
		{
			var vfx = VisualEffect ?? (_cachedVisualEffect ??= GetComponent<VisualEffect>());

			if (vfx != null)
			{
				vfx.Stop();
			}

			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that sets a float property on a VFX Graph effect and immediately transitions.
	/// Requires the Visual Effect Graph package (com.unity.visualeffectgraph).
	/// </summary>
	[StateInfo(
		"Sets a VFX Graph float property",
		"Sets a float property on the specified VisualEffect component by name. " +
		"Useful for controlling VFX parameters like size, speed, or intensity."
	)]
	[StateStyle(Color = StateColors.Violet, Icon = NodeIcon.Sparks)]
	public class SetVFXFloatState : State
	{
		/// <summary>
		/// The VisualEffect component to modify. If null, attempts to get one from the context GameObject.
		/// </summary>
		[DataIn("Visual Effect")]
		public VisualEffect VisualEffect;

		/// <summary>
		/// The name of the float property to set.
		/// </summary>
		[DataIn("Property Name")]
		public string PropertyName;

		/// <summary>
		/// The float value to set.
		/// </summary>
		[DataIn("Value")]
		public float Value;

		/// <summary>
		/// Transition that fires after setting the property.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		private VisualEffect _cachedVisualEffect;

		public override void OnEnter()
		{
			var vfx = VisualEffect ?? (_cachedVisualEffect ??= GetComponent<VisualEffect>());

			if (vfx == null)
			{
				LogWarning("No VisualEffect found");
				OnComplete?.Invoke();
				return;
			}

			if (string.IsNullOrEmpty(PropertyName))
			{
				LogWarning("No property name specified");
				OnComplete?.Invoke();
				return;
			}

			vfx.SetFloat(PropertyName, Value);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that sends a named event to a VFX Graph effect and immediately transitions.
	/// Requires the Visual Effect Graph package (com.unity.visualeffectgraph).
	/// </summary>
	[StateInfo(
		"Sends an event to a VFX Graph effect",
		"Sends a named event to the specified VisualEffect component. " +
		"VFX Graph events can trigger spawn bursts, state changes, or other behaviors " +
		"defined in the graph."
	)]
	[StateStyle(Color = StateColors.Violet, Icon = NodeIcon.Sparks)]
	public class SendVFXEventState : State
	{
		/// <summary>
		/// The VisualEffect component to send the event to. If null, attempts to get one from the context GameObject.
		/// </summary>
		[DataIn("Visual Effect")]
		public VisualEffect VisualEffect;

		/// <summary>
		/// The name of the event to send.
		/// </summary>
		[DataIn("Event Name")]
		public string EventName;

		/// <summary>
		/// Transition that fires after sending the event.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		private VisualEffect _cachedVisualEffect;

		public override void OnEnter()
		{
			var vfx = VisualEffect ?? (_cachedVisualEffect ??= GetComponent<VisualEffect>());

			if (vfx == null)
			{
				LogWarning("No VisualEffect found");
				OnComplete?.Invoke();
				return;
			}

			if (string.IsNullOrEmpty(EventName))
			{
				LogWarning("No event name specified");
				OnComplete?.Invoke();
				return;
			}

			vfx.SendEvent(EventName);
			OnComplete?.Invoke();
		}
	}
}
#endif
