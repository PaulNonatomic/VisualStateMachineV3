using System;
using UnityEditor;

namespace Nonatomic.VSM.Editor.Runtime
{
	/// <summary>
	/// Replay mode for the transition timeline.
	/// </summary>
	public enum ReplayMode
	{
		Live,
		Paused,
		Playing
	}

	/// <summary>
	/// Controls replay navigation through recorded transition history.
	/// Manages playback position, stepping, auto-play with speed control,
	/// and mode transitions between live monitoring and replay.
	/// </summary>
	public class ReplayController : IDisposable
	{
		public event Action<StateTransitionRecord> OnReplayPositionChanged;
		public event Action<ReplayMode> OnModeChanged;

		public ReplayMode Mode { get; private set; } = ReplayMode.Live;
		public int CurrentIndex { get; private set; }
		public float PlaybackSpeed { get; private set; } = 1f;

		public StateTransitionRecord CurrentRecord
		{
			get
			{
				if (_instance == null) return null;
				var history = _instance.TransitionHistory;
				if (history.Count == 0 || CurrentIndex < 0 || CurrentIndex >= history.Count)
				{
					return null;
				}
				return history[CurrentIndex];
			}
		}

		public int HistoryCount => _instance?.TransitionHistory.Count ?? 0;

		private RuntimeInstanceInfo _instance;
		private double _lastStepTime;

		private static readonly float[] SpeedOptions = { 0.5f, 1f, 2f, 4f };

		public void SetInstance(RuntimeInstanceInfo instance)
		{
			if (Mode == ReplayMode.Playing)
			{
				StopAutoPlay();
			}

			_instance = instance;
			CurrentIndex = instance?.TransitionHistory.Count - 1 ?? 0;
			if (CurrentIndex < 0) CurrentIndex = 0;
			SetMode(ReplayMode.Live);
		}

		public void StepForward()
		{
			if (_instance == null) return;
			var history = _instance.TransitionHistory;
			if (history.Count == 0) return;

			if (CurrentIndex < history.Count - 1)
			{
				CurrentIndex++;
				SetMode(ReplayMode.Paused);
				OnReplayPositionChanged?.Invoke(CurrentRecord);
			}
		}

		public void StepBackward()
		{
			if (_instance == null) return;
			var history = _instance.TransitionHistory;
			if (history.Count == 0) return;

			if (CurrentIndex > 0)
			{
				CurrentIndex--;
				SetMode(ReplayMode.Paused);
				OnReplayPositionChanged?.Invoke(CurrentRecord);
			}
		}

		public void GoToIndex(int index)
		{
			if (_instance == null) return;
			var history = _instance.TransitionHistory;
			if (history.Count == 0) return;

			var clamped = Math.Max(0, Math.Min(index, history.Count - 1));
			if (clamped == CurrentIndex) return;

			CurrentIndex = clamped;

			if (Mode == ReplayMode.Live)
			{
				SetMode(ReplayMode.Paused);
			}

			OnReplayPositionChanged?.Invoke(CurrentRecord);
		}

		public void Play()
		{
			if (_instance == null) return;
			var history = _instance.TransitionHistory;
			if (history.Count == 0) return;

			// If at the end, restart from beginning
			if (CurrentIndex >= history.Count - 1)
			{
				CurrentIndex = 0;
				OnReplayPositionChanged?.Invoke(CurrentRecord);
			}

			_lastStepTime = EditorApplication.timeSinceStartup;
			SetMode(ReplayMode.Playing);
			EditorApplication.update -= OnEditorUpdate;
			EditorApplication.update += OnEditorUpdate;
		}

		public void Pause()
		{
			StopAutoPlay();
			SetMode(ReplayMode.Paused);
		}

		public void ReturnToLive()
		{
			StopAutoPlay();

			if (_instance != null)
			{
				CurrentIndex = _instance.TransitionHistory.Count - 1;
				if (CurrentIndex < 0) CurrentIndex = 0;
			}

			SetMode(ReplayMode.Live);
		}

		public void CycleSpeed()
		{
			var currentIdx = Array.IndexOf(SpeedOptions, PlaybackSpeed);
			if (currentIdx < 0) currentIdx = 1; // Default to 1x
			var nextIdx = (currentIdx + 1) % SpeedOptions.Length;
			PlaybackSpeed = SpeedOptions[nextIdx];
		}

		public void Dispose()
		{
			StopAutoPlay();
			_instance = null;
		}

		private void OnEditorUpdate()
		{
			if (_instance == null || Mode != ReplayMode.Playing)
			{
				StopAutoPlay();
				return;
			}

			var history = _instance.TransitionHistory;
			if (CurrentIndex >= history.Count - 1)
			{
				Pause();
				return;
			}

			var currentRecord = history[CurrentIndex];
			var nextRecord = history[CurrentIndex + 1];
			var recordDuration = (nextRecord.Timestamp - currentRecord.Timestamp).TotalSeconds;

			// Minimum step interval to prevent instant skipping for zero-duration transitions
			if (recordDuration < 0.05)
			{
				recordDuration = 0.05;
			}

			var scaledDuration = recordDuration / PlaybackSpeed;
			var elapsed = EditorApplication.timeSinceStartup - _lastStepTime;

			if (elapsed >= scaledDuration)
			{
				CurrentIndex++;
				_lastStepTime = EditorApplication.timeSinceStartup;
				OnReplayPositionChanged?.Invoke(CurrentRecord);

				if (CurrentIndex >= history.Count - 1)
				{
					Pause();
				}
			}
		}

		private void StopAutoPlay()
		{
			EditorApplication.update -= OnEditorUpdate;
		}

		private void SetMode(ReplayMode mode)
		{
			if (Mode == mode) return;
			Mode = mode;
			OnModeChanged?.Invoke(mode);
		}
	}
}
