using Nonatomic.VSM.Editor.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph
{
	/// <summary>
	/// Floating panel that displays a debug timeline of state transitions with
	/// blackboard snapshots, diff inspection, and replay controls.
	/// </summary>
	public class DebugTimelinePanel : FloatingPanel
	{
		private readonly StateMachineGraphWindow _window;
		private readonly TransitionTimelineView _timelineView;
		private readonly ReplayController _replayController;

		private RuntimeInstanceInfo _trackedInstance;
		private Label _statusLabel;
		private Label _speedLabel;
		private Label _positionLabel;
		private Button _playPauseButton;
		private SliderInt _scrubber;
		private VisualElement _controlBar;
		private VisualElement _scrubberBar;

		public ReplayController ReplayController => _replayController;

		public DebugTimelinePanel(StateMachineGraphWindow window)
			: base("DebugTimeline", "Debug Timeline", new Vector2(10, 420), new Vector2(360, 450))
		{
			_window = window;
			_replayController = new ReplayController();

			AddToClassList("debug-timeline-panel");

			// Status bar showing live/paused/replay mode
			var statusBar = new VisualElement();
			statusBar.style.flexDirection = FlexDirection.Row;
			statusBar.style.justifyContent = Justify.SpaceBetween;
			statusBar.style.alignItems = Align.Center;
			statusBar.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
			statusBar.style.paddingLeft = 8;
			statusBar.style.paddingRight = 8;
			statusBar.style.paddingTop = 4;
			statusBar.style.paddingBottom = 4;

			_statusLabel = new Label("LIVE");
			_statusLabel.style.color = new Color(0.2f, 0.78f, 0.2f);
			_statusLabel.style.fontSize = 10;
			_statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			statusBar.Add(_statusLabel);

			var statusRight = new VisualElement();
			statusRight.style.flexDirection = FlexDirection.Row;
			statusRight.style.alignItems = Align.Center;

			_speedLabel = new Label("1x");
			_speedLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
			_speedLabel.style.fontSize = 10;
			_speedLabel.style.marginRight = 8;
			_speedLabel.style.cursor = new UnityEngine.UIElements.Cursor();
			_speedLabel.RegisterCallback<MouseDownEvent>(_ =>
			{
				_replayController.CycleSpeed();
				_speedLabel.text = FormatSpeed(_replayController.PlaybackSpeed);
			});
			statusRight.Add(_speedLabel);

			var exportButton = CreateSmallButton("\u2193", "Export transition history to JSON file");
			exportButton.clicked += () =>
			{
				if (_trackedInstance != null)
				{
					TransitionHistoryExporter.ExportToFile(_trackedInstance);
				}
				else
				{
					Debug.LogWarning("[VSM] Cannot export: no runtime instance is currently tracked.");
				}
			};
			statusRight.Add(exportButton);

			statusBar.Add(statusRight);
			ContentContainer.Add(statusBar);

			// Replay control bar
			_controlBar = new VisualElement();
			_controlBar.style.flexDirection = FlexDirection.Row;
			_controlBar.style.alignItems = Align.Center;
			_controlBar.style.justifyContent = Justify.SpaceBetween;
			_controlBar.style.paddingLeft = 8;
			_controlBar.style.paddingRight = 8;
			_controlBar.style.paddingTop = 2;
			_controlBar.style.paddingBottom = 2;
			_controlBar.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
			_controlBar.style.borderBottomWidth = 1;
			_controlBar.style.borderBottomColor = new Color(0.14f, 0.14f, 0.14f);

			var controlsLeft = new VisualElement();
			controlsLeft.style.flexDirection = FlexDirection.Row;
			controlsLeft.style.alignItems = Align.Center;

			var stepBackButton = CreateSmallButton("|\u25C4", "Step backward");
			stepBackButton.clicked += () => _replayController.StepBackward();
			controlsLeft.Add(stepBackButton);

			_playPauseButton = CreateSmallButton("\u25BA", "Play / Pause replay");
			_playPauseButton.clicked += () =>
			{
				if (_replayController.Mode == ReplayMode.Playing)
				{
					_replayController.Pause();
				}
				else
				{
					_replayController.Play();
				}
			};
			controlsLeft.Add(_playPauseButton);

			var stepForwardButton = CreateSmallButton("\u25BA|", "Step forward");
			stepForwardButton.clicked += () => _replayController.StepForward();
			controlsLeft.Add(stepForwardButton);

			_controlBar.Add(controlsLeft);

			var liveButton = new Button(() => _replayController.ReturnToLive());
			liveButton.text = "\u25CF LIVE";
			liveButton.tooltip = "Return to live mode";
			liveButton.style.fontSize = 10;
			liveButton.style.height = 18;
			liveButton.style.paddingLeft = 4;
			liveButton.style.paddingRight = 4;
			liveButton.style.paddingTop = 0;
			liveButton.style.paddingBottom = 0;
			liveButton.style.backgroundColor = Color.clear;
			liveButton.style.color = new Color(0.2f, 0.78f, 0.2f);
			liveButton.style.borderTopWidth = 0;
			liveButton.style.borderBottomWidth = 0;
			liveButton.style.borderLeftWidth = 0;
			liveButton.style.borderRightWidth = 0;
			_controlBar.Add(liveButton);

			ContentContainer.Add(_controlBar);

			// Scrubber bar
			_scrubberBar = new VisualElement();
			_scrubberBar.style.flexDirection = FlexDirection.Row;
			_scrubberBar.style.alignItems = Align.Center;
			_scrubberBar.style.paddingLeft = 8;
			_scrubberBar.style.paddingRight = 8;
			_scrubberBar.style.paddingTop = 2;
			_scrubberBar.style.paddingBottom = 2;
			_scrubberBar.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
			_scrubberBar.style.borderBottomWidth = 1;
			_scrubberBar.style.borderBottomColor = new Color(0.14f, 0.14f, 0.14f);

			_scrubber = new SliderInt(0, 0);
			_scrubber.style.flexGrow = 1;
			_scrubber.style.marginRight = 8;
			_scrubber.RegisterValueChangedCallback(evt =>
			{
				if (_replayController.Mode != ReplayMode.Playing)
				{
					_replayController.GoToIndex(evt.newValue);
				}
			});
			_scrubberBar.Add(_scrubber);

			_positionLabel = new Label("0/0");
			_positionLabel.style.fontSize = 10;
			_positionLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
			_positionLabel.style.width = 40;
			_positionLabel.style.unityTextAlign = TextAnchor.MiddleRight;
			_scrubberBar.Add(_positionLabel);

			ContentContainer.Add(_scrubberBar);

			// Embed the timeline view
			_timelineView = new TransitionTimelineView();
			_timelineView.OnNavigateToState += OnTimelineNavigateToState;
			ContentContainer.Add(_timelineView);

			// Wire up replay controller events
			_replayController.OnReplayPositionChanged += OnReplayPositionChanged;
			_replayController.OnModeChanged += OnReplayModeChanged;

			// Start hidden by default
			SetVisible(false);
		}

		/// <summary>
		/// Sets the runtime instance to track in the timeline.
		/// </summary>
		public void SetInstance(RuntimeInstanceInfo instance)
		{
			if (_trackedInstance != null)
			{
				_trackedInstance.OnStateChanged -= OnInstanceStateChanged;
				_trackedInstance.OnStatusChanged -= OnInstanceStatusChanged;
			}

			_trackedInstance = instance;
			_replayController.SetInstance(instance);
			_timelineView.SetInstance(instance);

			if (instance != null)
			{
				instance.OnStateChanged += OnInstanceStateChanged;
				instance.OnStatusChanged += OnInstanceStatusChanged;
				UpdateScrubber();
				UpdateStatusLabel();
			}
			else
			{
				_statusLabel.text = "NO INSTANCE";
				_statusLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
				UpdateScrubber();
			}
		}

		/// <summary>
		/// Updates the timeline with the latest transitions.
		/// </summary>
		public void RefreshTimeline()
		{
			if (_replayController.Mode == ReplayMode.Live)
			{
				_timelineView.RefreshTimeline();
				UpdateScrubber();
			}
		}

		private void OnInstanceStateChanged(RuntimeInstanceInfo info)
		{
			RefreshTimeline();
		}

		private void OnInstanceStatusChanged(RuntimeInstanceInfo info)
		{
			UpdateStatusLabel();

			if (info.Status == RuntimeInstanceState.Stopped)
			{
				RefreshTimeline();
			}
		}

		private void OnReplayPositionChanged(StateTransitionRecord record)
		{
			if (record == null) return;

			// Highlight the state in the graph
			if (!string.IsNullOrEmpty(record.ToNodeId))
			{
				_window.GraphView?.HighlightActiveState(record.ToNodeId);
			}

			// Select and scroll to the record in the timeline
			_timelineView.SelectRecord(record);
			_timelineView.ScrollToRecord(record);

			UpdateScrubber();
		}

		private void OnReplayModeChanged(ReplayMode mode)
		{
			UpdateStatusLabel();
			UpdatePlayPauseButton();

			if (mode == ReplayMode.Live)
			{
				_timelineView.RefreshTimeline();
				UpdateScrubber();
			}
		}

		private void OnTimelineNavigateToState(string nodeId)
		{
			if (string.IsNullOrEmpty(nodeId)) return;

			_window.GraphView?.HighlightActiveState(nodeId);

			// If user clicks a timeline entry directly while in Live mode, enter Paused
			if (_replayController.Mode == ReplayMode.Live)
			{
				_replayController.Pause();
			}
		}

		private void UpdateStatusLabel()
		{
			if (_trackedInstance == null)
			{
				_statusLabel.text = "NO INSTANCE";
				_statusLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
				return;
			}

			switch (_replayController.Mode)
			{
				case ReplayMode.Live:
					switch (_trackedInstance.Status)
					{
						case RuntimeInstanceState.Running:
							_statusLabel.text = "LIVE";
							_statusLabel.style.color = new Color(0.2f, 0.78f, 0.2f);
							break;
						case RuntimeInstanceState.Stopped:
							_statusLabel.text = "STOPPED";
							_statusLabel.style.color = new Color(0.78f, 0.2f, 0.2f);
							break;
						case RuntimeInstanceState.Paused:
							_statusLabel.text = "PAUSED";
							_statusLabel.style.color = new Color(0.78f, 0.59f, 0.2f);
							break;
						case RuntimeInstanceState.Error:
							_statusLabel.text = "ERROR";
							_statusLabel.style.color = new Color(0.78f, 0.2f, 0.2f);
							break;
					}
					break;
				case ReplayMode.Paused:
					_statusLabel.text = "REPLAY \u275A\u275A";
					_statusLabel.style.color = new Color(0.78f, 0.59f, 0.2f);
					break;
				case ReplayMode.Playing:
					_statusLabel.text = "REPLAY \u25BA";
					_statusLabel.style.color = new Color(0.2f, 0.7f, 0.86f);
					break;
			}
		}

		private void UpdatePlayPauseButton()
		{
			if (_playPauseButton == null) return;
			_playPauseButton.text = _replayController.Mode == ReplayMode.Playing
				? "\u275A\u275A"
				: "\u25BA";
		}

		private void UpdateScrubber()
		{
			var count = _replayController.HistoryCount;
			if (count <= 0)
			{
				_scrubber.highValue = 0;
				_scrubber.SetValueWithoutNotify(0);
				_positionLabel.text = "0/0";
				return;
			}

			_scrubber.highValue = count - 1;
			_scrubber.SetValueWithoutNotify(_replayController.CurrentIndex);
			_positionLabel.text = $"{_replayController.CurrentIndex + 1}/{count}";
		}

		private static Button CreateSmallButton(string text, string tooltip)
		{
			var button = new Button();
			button.text = text;
			button.tooltip = tooltip;
			button.style.width = 24;
			button.style.height = 18;
			button.style.marginLeft = 2;
			button.style.marginRight = 2;
			button.style.paddingLeft = 0;
			button.style.paddingRight = 0;
			button.style.paddingTop = 0;
			button.style.paddingBottom = 0;
			button.style.fontSize = 10;
			button.style.unityTextAlign = TextAnchor.MiddleCenter;
			button.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
			button.style.borderTopWidth = 1;
			button.style.borderBottomWidth = 1;
			button.style.borderLeftWidth = 1;
			button.style.borderRightWidth = 1;
			button.style.borderTopColor = new Color(0.35f, 0.35f, 0.35f);
			button.style.borderBottomColor = new Color(0.35f, 0.35f, 0.35f);
			button.style.borderLeftColor = new Color(0.35f, 0.35f, 0.35f);
			button.style.borderRightColor = new Color(0.35f, 0.35f, 0.35f);
			button.style.borderTopLeftRadius = 3;
			button.style.borderTopRightRadius = 3;
			button.style.borderBottomLeftRadius = 3;
			button.style.borderBottomRightRadius = 3;
			return button;
		}

		private static string FormatSpeed(float speed)
		{
			if (speed < 1f)
			{
				return $"{speed:F1}x";
			}
			return $"{speed:F0}x";
		}

		protected override void AddTitleBarButtons(VisualElement container)
		{
			// Title bar buttons are now part of the control bar
		}
	}
}
