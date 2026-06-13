using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// State that plays an audio clip and immediately transitions.
	/// Does not wait for the audio to finish.
	/// </summary>
	[StateInfo(
		"Plays an audio clip (fire and forget)",
		"Plays an audio clip using AudioSource.PlayClipAtPoint or on a specified " +
		"AudioSource. Transitions immediately without waiting for playback to complete."
	)]
	[StateStyle(Color = StateColors.Pink, Icon = NodeIcon.BroadcastRight)]
	public class PlayAudioState : State
	{
		/// <summary>
		/// The audio clip to play.
		/// </summary>
		[DataIn("Clip", Required = true)]
		public AudioClip Clip;

		/// <summary>
		/// Optional AudioSource to play on. If null, uses PlayClipAtPoint.
		/// </summary>
		[DataIn("Audio Source")]
		public AudioSource AudioSource;

		/// <summary>
		/// Volume to play at (0 to 1).
		/// </summary>
		[DataIn("Volume")]
		public float Volume = 1f;

		/// <summary>
		/// Position to play at (used when AudioSource is null).
		/// If not set, uses the context GameObject's position.
		/// </summary>
		[DataIn("Position")]
		public Vector3? Position;

		/// <summary>
		/// Transition that fires after starting playback.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			if (Clip == null)
			{
				LogWarning("No audio clip specified");
				OnComplete?.Invoke();
				return;
			}

			if (AudioSource != null)
			{
				AudioSource.clip = Clip;
				AudioSource.volume = Volume;
				AudioSource.Play();
			}
			else
			{
				var pos = Position ?? (Transform != null ? Transform.position : Vector3.zero);
				UnityEngine.AudioSource.PlayClipAtPoint(Clip, pos, Volume);
			}

			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that plays an audio clip and waits for it to finish.
	/// </summary>
	[StateInfo(
		"Plays an audio clip and waits",
		"Plays an audio clip on a specified AudioSource and waits for playback to " +
		"complete before transitioning. Requires an AudioSource component."
	)]
	[StateStyle(Color = StateColors.Pink, Icon = NodeIcon.BroadcastRight)]
	public class PlayAudioAndWaitState : State
	{
		/// <summary>
		/// The audio clip to play.
		/// </summary>
		[DataIn("Clip", Required = true)]
		public AudioClip Clip;

		/// <summary>
		/// The AudioSource to play on. If null, attempts to get one from the context GameObject.
		/// </summary>
		[DataIn("Audio Source")]
		public AudioSource AudioSource;

		/// <summary>
		/// Volume to play at (0 to 1).
		/// </summary>
		[DataIn("Volume")]
		public float Volume = 1f;

		/// <summary>
		/// Outputs the duration of the clip in seconds.
		/// </summary>
		[DataOut("Duration")]
		public float Duration;

		/// <summary>
		/// Transition that fires when playback completes.
		/// </summary>
		[Transition("Complete")]
		public Action OnComplete;

		private AudioSource _cachedAudioSource;
		private AudioSource _audioSource;

		public override void OnEnter()
		{
			if (Clip == null)
			{
				LogWarning("No audio clip specified");
				OnComplete?.Invoke();
				return;
			}

			_audioSource = AudioSource ?? (_cachedAudioSource ??= GetComponent<AudioSource>());

			if (_audioSource == null)
			{
				LogWarning("No AudioSource found");
				OnComplete?.Invoke();
				return;
			}

			Duration = Clip.length;
			_audioSource.clip = Clip;
			_audioSource.volume = Volume;
			_audioSource.Play();
		}

		public override void OnUpdate()
		{
			if (_audioSource == null || !_audioSource.isPlaying)
			{
				OnComplete?.Invoke();
			}
		}

		public override void OnExit()
		{
			_audioSource = null;
		}
	}

	/// <summary>
	/// State that stops audio playback on an AudioSource.
	/// </summary>
	[StateInfo(
		"Stops audio playback",
		"Stops audio playback on the specified AudioSource and immediately transitions."
	)]
	[StateStyle(Color = StateColors.Pink, Icon = NodeIcon.BroadcastLeft)]
	public class StopAudioState : State
	{
		/// <summary>
		/// The AudioSource to stop. If null, attempts to get one from the context GameObject.
		/// </summary>
		[DataIn("Audio Source")]
		public AudioSource AudioSource;

		/// <summary>
		/// Transition that fires after stopping.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		private AudioSource _cachedAudioSource;

		public override void OnEnter()
		{
			var audioSource = AudioSource ?? (_cachedAudioSource ??= GetComponent<AudioSource>());

			if (audioSource != null)
			{
				audioSource.Stop();
			}

			OnComplete?.Invoke();
		}
	}
}
