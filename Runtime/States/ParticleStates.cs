using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// State that plays a particle system and immediately transitions.
	/// Does not wait for the particle system to finish.
	/// </summary>
	[StateInfo(
		"Plays a particle system (fire and forget)",
		"Plays a ParticleSystem on the specified component or the context GameObject. " +
		"Transitions immediately without waiting for playback to complete."
	)]
	[StateStyle(Color = StateColors.Violet, Icon = NodeIcon.Sparks)]
	public class PlayParticleState : State
	{
		/// <summary>
		/// The particle system to play. If null, attempts to get one from the context GameObject.
		/// </summary>
		[DataIn("Particle System")]
		public ParticleSystem ParticleSystem;

		/// <summary>
		/// Whether to also play child particle systems.
		/// </summary>
		[DataIn("With Children")]
		public bool WithChildren = true;

		/// <summary>
		/// Transition that fires after starting playback.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		private ParticleSystem _cachedParticleSystem;

		public override void OnEnter()
		{
			var ps = ParticleSystem ?? (_cachedParticleSystem ??= GetComponent<ParticleSystem>());

			if (ps == null)
			{
				LogWarning("No ParticleSystem found");
				OnComplete?.Invoke();
				return;
			}

			ps.Play(WithChildren);
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that plays a particle system and waits for it to finish.
	/// </summary>
	[StateInfo(
		"Plays a particle system and waits",
		"Plays a ParticleSystem and waits for it to finish playing before " +
		"transitioning. Looping particle systems will not auto-complete."
	)]
	[StateStyle(Color = StateColors.Violet, Icon = NodeIcon.Sparks)]
	public class PlayParticleAndWaitState : State
	{
		/// <summary>
		/// The particle system to play. If null, attempts to get one from the context GameObject.
		/// </summary>
		[DataIn("Particle System")]
		public ParticleSystem ParticleSystem;

		/// <summary>
		/// Whether to also play child particle systems.
		/// </summary>
		[DataIn("With Children")]
		public bool WithChildren = true;

		/// <summary>
		/// Outputs the duration of the main particle system module.
		/// </summary>
		[DataOut("Duration")]
		public float Duration;

		/// <summary>
		/// Transition that fires when the particle system finishes playing.
		/// </summary>
		[Transition("Complete")]
		public Action OnComplete;

		private ParticleSystem _cachedParticleSystem;
		private ParticleSystem _particleSystem;

		public override void OnEnter()
		{
			_particleSystem = ParticleSystem ?? (_cachedParticleSystem ??= GetComponent<ParticleSystem>());

			if (_particleSystem == null)
			{
				LogWarning("No ParticleSystem found");
				OnComplete?.Invoke();
				return;
			}

			Duration = _particleSystem.main.duration;
			_particleSystem.Play(WithChildren);
		}

		public override void OnUpdate()
		{
			if (_particleSystem == null || !_particleSystem.isPlaying)
			{
				OnComplete?.Invoke();
			}
		}

		public override void OnExit()
		{
			_particleSystem = null;
		}
	}

	/// <summary>
	/// State that stops a particle system and immediately transitions.
	/// </summary>
	[StateInfo(
		"Stops a particle system",
		"Stops a ParticleSystem with configurable stop behavior. Can either stop " +
		"emitting (letting existing particles finish) or stop and clear all particles."
	)]
	[StateStyle(Color = StateColors.Violet, Icon = NodeIcon.Sparks)]
	public class StopParticleState : State
	{
		/// <summary>
		/// The particle system to stop. If null, attempts to get one from the context GameObject.
		/// </summary>
		[DataIn("Particle System")]
		public ParticleSystem ParticleSystem;

		/// <summary>
		/// Whether to also stop child particle systems.
		/// </summary>
		[DataIn("With Children")]
		public bool WithChildren = true;

		/// <summary>
		/// The stop behavior: StopEmitting lets particles finish, StopEmittingAndClear removes them.
		/// </summary>
		[DataIn("Stop Behavior")]
		public ParticleSystemStopBehavior StopBehavior = ParticleSystemStopBehavior.StopEmitting;

		/// <summary>
		/// Transition that fires after stopping.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		private ParticleSystem _cachedParticleSystem;

		public override void OnEnter()
		{
			var ps = ParticleSystem ?? (_cachedParticleSystem ??= GetComponent<ParticleSystem>());

			if (ps != null)
			{
				ps.Stop(WithChildren, StopBehavior);
			}

			OnComplete?.Invoke();
		}
	}
}
