using System;
using Nonatomic.VSM.Attributes;
using Nonatomic.VSM.Core;
using UnityEngine;
using UnityEngine.Playables;

namespace Nonatomic.VSM.States
{
	/// <summary>
	/// State that plays a Timeline (or any PlayableAsset) and waits for it to complete.
	/// Optionally references a PlayableDirector; if none is provided, one is found
	/// or created on the context GameObject.
	/// </summary>
	[StateInfo(
		"Plays a Timeline and waits for completion",
		"Plays a PlayableAsset (such as a TimelineAsset) using a PlayableDirector. " +
		"Waits for playback to finish before transitioning. Outputs elapsed time " +
		"and progress. If no Director is provided, uses or creates one on the " +
		"context GameObject."
	)]
	[StateStyle(Color = StateColors.Pink, Icon = NodeIcon.Film)]
	public class PlayTimelineState : State
	{
		/// <summary>
		/// The playable asset to play. Typically a TimelineAsset.
		/// </summary>
		[DataIn("Asset", Required = true)]
		public PlayableAsset Asset;

		/// <summary>
		/// Optional PlayableDirector. If null, one is found or created on the context GameObject.
		/// </summary>
		[DataIn("Director")]
		public PlayableDirector Director;

		/// <summary>
		/// What happens when the playable finishes: None (stop), Hold (freeze on last frame), Loop.
		/// </summary>
		[DataIn("Wrap Mode")]
		public DirectorWrapMode WrapMode = DirectorWrapMode.None;

		/// <summary>
		/// Playback speed multiplier.
		/// </summary>
		[DataIn("Speed")]
		public float Speed = 1f;

		/// <summary>
		/// Elapsed playback time in seconds.
		/// </summary>
		[DataOut("Elapsed Time")]
		public float ElapsedTime;

		/// <summary>
		/// Playback progress from 0 to 1.
		/// </summary>
		[DataOut("Progress")]
		public float Progress;

		/// <summary>
		/// Transition that fires when playback completes.
		/// </summary>
		[Transition("Complete")]
		public Action OnComplete;

		private PlayableDirector _director;
		private bool _completed;
		private bool _ownedDirector;

		public override void OnEnter()
		{
			if (Asset == null)
			{
				LogWarning("No playable asset assigned");
				OnComplete?.Invoke();
				return;
			}

			_completed = false;
			ElapsedTime = 0f;
			Progress = 0f;

			_director = ResolveDirector();
			if (_director == null)
			{
				LogWarning("No PlayableDirector available");
				OnComplete?.Invoke();
				return;
			}

			_director.playableAsset = Asset;
			_director.extrapolationMode = WrapMode;
			_director.time = 0;
			_director.stopped += HandleStopped;

			if (_director.playableGraph.IsValid())
			{
				_director.playableGraph.GetRootPlayable(0).SetSpeed(Speed);
			}

			_director.Play();
		}

		public override void OnUpdate()
		{
			if (_director == null) return;

			ElapsedTime = (float)_director.time;

			var duration = _director.duration;
			Progress = duration > 0 ? Mathf.Clamp01((float)(_director.time / duration)) : 0f;

			if (_completed)
			{
				OnComplete?.Invoke();
			}
		}

		public override void OnExit()
		{
			if (_director != null)
			{
				_director.stopped -= HandleStopped;
			}

			if (_ownedDirector && _director != null)
			{
				UnityEngine.Object.Destroy(_director);
			}

			_director = null;
			_ownedDirector = false;
		}

		private void HandleStopped(PlayableDirector director)
		{
			_completed = true;
		}

		private PlayableDirector ResolveDirector()
		{
			if (Director != null) return Director;

			var existing = GetComponent<PlayableDirector>();
			if (existing != null) return existing;

			if (GameObject == null) return null;

			_ownedDirector = true;
			return GameObject.AddComponent<PlayableDirector>();
		}
	}

	/// <summary>
	/// State that pauses a playing timeline and immediately transitions.
	/// </summary>
	[StateInfo(
		"Pauses timeline playback",
		"Pauses the specified PlayableDirector and immediately transitions. " +
		"Use ResumeTimelineState to resume playback later."
	)]
	[StateStyle(Color = StateColors.Pink, Icon = NodeIcon.Film)]
	public class PauseTimelineState : State
	{
		/// <summary>
		/// The PlayableDirector to pause.
		/// </summary>
		[DataIn("Director", Required = true)]
		public PlayableDirector Director;

		/// <summary>
		/// Transition that fires after pausing.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			if (Director == null)
			{
				LogWarning("No PlayableDirector specified");
				OnComplete?.Invoke();
				return;
			}

			Director.Pause();
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that resumes a paused timeline and immediately transitions.
	/// </summary>
	[StateInfo(
		"Resumes timeline playback",
		"Resumes the specified PlayableDirector and immediately transitions. " +
		"Optionally sets a new playback speed."
	)]
	[StateStyle(Color = StateColors.Pink, Icon = NodeIcon.Film)]
	public class ResumeTimelineState : State
	{
		/// <summary>
		/// The PlayableDirector to resume.
		/// </summary>
		[DataIn("Director", Required = true)]
		public PlayableDirector Director;

		/// <summary>
		/// Playback speed multiplier applied on resume.
		/// </summary>
		[DataIn("Speed")]
		public float Speed = 1f;

		/// <summary>
		/// Transition that fires after resuming.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			if (Director == null)
			{
				LogWarning("No PlayableDirector specified");
				OnComplete?.Invoke();
				return;
			}

			if (Director.playableGraph.IsValid())
			{
				Director.playableGraph.GetRootPlayable(0).SetSpeed(Speed);
			}

			Director.Resume();
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that stops a playing timeline and immediately transitions.
	/// </summary>
	[StateInfo(
		"Stops timeline playback",
		"Stops the specified PlayableDirector and immediately transitions. " +
		"Playback position is reset."
	)]
	[StateStyle(Color = StateColors.Pink, Icon = NodeIcon.Film)]
	public class StopTimelineState : State
	{
		/// <summary>
		/// The PlayableDirector to stop.
		/// </summary>
		[DataIn("Director", Required = true)]
		public PlayableDirector Director;

		/// <summary>
		/// Transition that fires after stopping.
		/// </summary>
		[Transition("Done")]
		public Action OnComplete;

		public override void OnEnter()
		{
			if (Director == null)
			{
				LogWarning("No PlayableDirector specified");
				OnComplete?.Invoke();
				return;
			}

			Director.Stop();
			OnComplete?.Invoke();
		}
	}

	/// <summary>
	/// State that waits for a timeline to reach a specific playback time.
	/// </summary>
	[StateInfo(
		"Waits for a playback time to be reached",
		"Monitors a PlayableDirector and transitions when the current playback " +
		"time reaches or exceeds the target time. Useful for triggering events " +
		"at specific points during cutscenes."
	)]
	[StateStyle(Color = StateColors.Pink, Icon = NodeIcon.Clock)]
	public class WaitForPlaybackTimeState : State
	{
		/// <summary>
		/// The PlayableDirector to monitor.
		/// </summary>
		[DataIn("Director", Required = true)]
		public PlayableDirector Director;

		/// <summary>
		/// The target time in seconds to wait for.
		/// </summary>
		[DataIn("Target Time")]
		public float TargetTime;

		/// <summary>
		/// Current playback time.
		/// </summary>
		[DataOut("Current Time")]
		public float CurrentTime;

		/// <summary>
		/// Transition that fires when the target time is reached.
		/// </summary>
		[Transition("Reached")]
		public Action OnReached;

		public override void OnEnter()
		{
			CurrentTime = 0f;

			if (Director == null)
			{
				LogWarning("No PlayableDirector specified");
				OnReached?.Invoke();
				return;
			}
		}

		public override void OnUpdate()
		{
			if (Director == null) return;

			CurrentTime = (float)Director.time;

			if (CurrentTime >= TargetTime)
			{
				OnReached?.Invoke();
			}
		}
	}
}
