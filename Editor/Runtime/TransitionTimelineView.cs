using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Runtime
{
	/// <summary>
	/// Visual timeline of state transitions with blackboard snapshot inspection and diff view.
	/// </summary>
	public class TransitionTimelineView : VisualElement
	{
		public event Action<string> OnNavigateToState;

		private RuntimeInstanceInfo _instance;
		private ScrollView _timelineScroll;
		private VisualElement _detailsPanel;
		private StateTransitionRecord _selectedRecord;
		private StateTransitionRecord _diffAnchorRecord;
		private bool _isDiffMode;
		private VisualElement _selectedElement;
		private VisualElement _anchorElement;
		private readonly System.Collections.Generic.Dictionary<StateTransitionRecord, VisualElement> _recordToElement
			= new System.Collections.Generic.Dictionary<StateTransitionRecord, VisualElement>();

		public TransitionTimelineView()
		{
			style.flexGrow = 1;
			BuildLayout();
		}

		public void SetInstance(RuntimeInstanceInfo instance)
		{
			_instance = instance;
			_selectedRecord = null;
			_diffAnchorRecord = null;
			_isDiffMode = false;
			RefreshTimeline();
		}

		public void RefreshTimeline()
		{
			_timelineScroll.Clear();
			_detailsPanel.Clear();
			_recordToElement.Clear();

			if (_instance == null)
			{
				_timelineScroll.Add(CreateEmptyLabel("No instance selected"));
				return;
			}

			var history = _instance.TransitionHistory;
			if (history.Count == 0)
			{
				_timelineScroll.Add(CreateEmptyLabel("No transitions yet"));
				return;
			}

			// Show most recent first
			for (var i = history.Count - 1; i >= 0; i--)
			{
				var record = history[i];
				var previousRecord = i > 0 ? history[i - 1] : null;
				var isLatest = i == history.Count - 1;
				var entry = CreateTimelineEntry(record, previousRecord, isLatest);
				_recordToElement[record] = entry;
				_timelineScroll.Add(entry);
			}
		}

		/// <summary>
		/// Programmatically selects a record in the timeline, applying selection styling
		/// and showing the blackboard snapshot.
		/// </summary>
		public void SelectRecord(StateTransitionRecord record)
		{
			if (record == null) return;
			if (!_recordToElement.TryGetValue(record, out var entry)) return;

			HandleEntryClick(entry, record);
		}

		/// <summary>
		/// Scrolls the timeline to make the specified record visible.
		/// </summary>
		public void ScrollToRecord(StateTransitionRecord record)
		{
			if (record == null) return;
			if (!_recordToElement.TryGetValue(record, out var entry)) return;

			entry.schedule.Execute(() =>
			{
				_timelineScroll.ScrollTo(entry);
			});
		}

		private void BuildLayout()
		{
			// Toolbar
			var toolbar = new VisualElement();
			toolbar.AddToClassList("timeline-toolbar");
			toolbar.style.flexDirection = FlexDirection.Row;
			toolbar.style.justifyContent = Justify.SpaceBetween;
			toolbar.style.alignItems = Align.Center;
			toolbar.style.paddingLeft = 8;
			toolbar.style.paddingRight = 8;
			toolbar.style.paddingTop = 4;
			toolbar.style.paddingBottom = 4;
			toolbar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
			toolbar.style.borderBottomWidth = 1;
			toolbar.style.borderBottomColor = new Color(0.14f, 0.14f, 0.14f);

			var titleLabel = new Label("Transition Timeline");
			titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			toolbar.Add(titleLabel);

			var diffToggle = new Button(() => ToggleDiffMode())
			{
				text = "Diff Mode",
				tooltip = "Compare blackboard state between two transitions. Right-click a transition to set the anchor, then click another to compare."
			};
			diffToggle.style.fontSize = 10;
			diffToggle.style.height = 18;
			toolbar.Add(diffToggle);

			Add(toolbar);

			// Timeline scroll area
			_timelineScroll = new ScrollView(ScrollViewMode.Vertical);
			_timelineScroll.style.flexGrow = 1;
			_timelineScroll.style.maxHeight = 250;
			Add(_timelineScroll);

			// Details panel (shows snapshot or diff when entry is selected)
			_detailsPanel = new VisualElement();
			_detailsPanel.style.borderTopWidth = 1;
			_detailsPanel.style.borderTopColor = new Color(0.14f, 0.14f, 0.14f);
			Add(_detailsPanel);
		}

		private VisualElement CreateTimelineEntry(
			StateTransitionRecord record,
			StateTransitionRecord previousRecord,
			bool isLatest)
		{
			var entry = new VisualElement();
			entry.AddToClassList("timeline-entry");
			entry.style.flexDirection = FlexDirection.Row;
			entry.style.alignItems = Align.Center;
			entry.style.paddingLeft = 8;
			entry.style.paddingRight = 8;
			entry.style.paddingTop = 4;
			entry.style.paddingBottom = 4;
			entry.style.borderBottomWidth = 1;
			entry.style.borderBottomColor = new Color(0.2f, 0.2f, 0.2f);

			// Status dot
			var dot = new VisualElement();
			dot.style.width = 8;
			dot.style.height = 8;
			dot.style.borderTopLeftRadius = 4;
			dot.style.borderTopRightRadius = 4;
			dot.style.borderBottomLeftRadius = 4;
			dot.style.borderBottomRightRadius = 4;
			dot.style.marginRight = 8;
			dot.style.backgroundColor = isLatest
				? new Color(0.2f, 0.78f, 0.2f)
				: new Color(0.4f, 0.4f, 0.4f);
			entry.Add(dot);

			// Timestamp
			var timestamp = new Label(record.Timestamp.ToString("HH:mm:ss.fff"));
			timestamp.style.width = 80;
			timestamp.style.color = new Color(0.47f, 0.47f, 0.47f);
			timestamp.style.fontSize = 10;
			entry.Add(timestamp);

			// Transition label
			var fromName = record.FromStateName ?? "Start";
			var toName = record.ToStateName ?? "Unknown";
			var transitionLabel = new Label($"{fromName} \u2192 {toName}");
			transitionLabel.style.flexGrow = 1;
			transitionLabel.style.color = new Color(0.78f, 0.78f, 0.78f);
			if (isLatest)
			{
				transitionLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			}
			entry.Add(transitionLabel);

			// Duration badge
			if (previousRecord != null)
			{
				var duration = record.GetDuration(previousRecord);
				var durationText = FormatDuration(duration);
				var durationBadge = new Label(durationText);
				durationBadge.style.color = new Color(0.47f, 0.7f, 0.47f);
				durationBadge.style.fontSize = 10;
				durationBadge.style.marginLeft = 4;
				durationBadge.style.paddingLeft = 4;
				durationBadge.style.paddingRight = 4;
				durationBadge.style.paddingTop = 1;
				durationBadge.style.paddingBottom = 1;
				durationBadge.style.backgroundColor = new Color(0.2f, 0.4f, 0.2f, 0.3f);
				durationBadge.style.borderTopLeftRadius = 3;
				durationBadge.style.borderTopRightRadius = 3;
				durationBadge.style.borderBottomLeftRadius = 3;
				durationBadge.style.borderBottomRightRadius = 3;
				entry.Add(durationBadge);
			}

			// Port name badge
			if (!string.IsNullOrEmpty(record.TriggerPortName))
			{
				var portBadge = new Label(record.TriggerPortName);
				portBadge.style.color = new Color(0.59f, 0.7f, 0.86f);
				portBadge.style.fontSize = 10;
				portBadge.style.marginLeft = 4;
				portBadge.style.paddingLeft = 4;
				portBadge.style.paddingRight = 4;
				portBadge.style.paddingTop = 1;
				portBadge.style.paddingBottom = 1;
				portBadge.style.backgroundColor = new Color(0.2f, 0.3f, 0.47f, 0.3f);
				portBadge.style.borderTopLeftRadius = 3;
				portBadge.style.borderTopRightRadius = 3;
				portBadge.style.borderBottomLeftRadius = 3;
				portBadge.style.borderBottomRightRadius = 3;
				entry.Add(portBadge);
			}

			// Hover effect
			entry.RegisterCallback<MouseEnterEvent>(_ =>
			{
				if (entry != _selectedElement && entry != _anchorElement)
				{
					entry.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
				}
			});
			entry.RegisterCallback<MouseLeaveEvent>(_ =>
			{
				if (entry != _selectedElement && entry != _anchorElement)
				{
					entry.style.backgroundColor = StyleKeyword.Null;
				}
			});

			// Click handler
			entry.RegisterCallback<MouseDownEvent>(evt =>
			{
				if (evt.button == 0)
				{
					HandleEntryClick(entry, record);
				}
				else if (evt.button == 1)
				{
					HandleEntryRightClick(entry, record);
				}
			});

			return entry;
		}

		private void HandleEntryClick(VisualElement entry, StateTransitionRecord record)
		{
			if (_isDiffMode && _diffAnchorRecord != null && _diffAnchorRecord != record)
			{
				ShowDiffView(_diffAnchorRecord, record);
				_isDiffMode = false;
				_diffAnchorRecord = null;
				if (_anchorElement != null)
				{
					_anchorElement.style.backgroundColor = StyleKeyword.Null;
					_anchorElement.style.borderLeftWidth = 0;
					_anchorElement = null;
				}
				return;
			}

			// Deselect previous
			if (_selectedElement != null)
			{
				_selectedElement.style.backgroundColor = StyleKeyword.Null;
				_selectedElement.style.borderLeftWidth = 0;
			}

			_selectedRecord = record;
			_selectedElement = entry;

			// Highlight selection
			entry.style.backgroundColor = new Color(0.18f, 0.25f, 0.33f);
			entry.style.borderLeftWidth = 3;
			entry.style.borderLeftColor = new Color(0.27f, 0.75f, 1f);

			ShowSnapshotView(record);

			if (!string.IsNullOrEmpty(record.ToNodeId))
			{
				OnNavigateToState?.Invoke(record.ToNodeId);
			}
		}

		private void HandleEntryRightClick(VisualElement entry, StateTransitionRecord record)
		{
			// Clear previous anchor
			if (_anchorElement != null)
			{
				_anchorElement.style.backgroundColor = StyleKeyword.Null;
				_anchorElement.style.borderLeftWidth = 0;
			}

			_diffAnchorRecord = record;
			_anchorElement = entry;
			_isDiffMode = true;

			// Highlight anchor
			entry.style.backgroundColor = new Color(0.25f, 0.22f, 0.14f);
			entry.style.borderLeftWidth = 3;
			entry.style.borderLeftColor = new Color(1f, 0.7f, 0.2f);

			_detailsPanel.Clear();
			var hint = new Label("Click another entry to see the diff");
			hint.style.color = new Color(0.6f, 0.6f, 0.6f);
			hint.style.paddingLeft = 12;
			hint.style.paddingTop = 8;
			hint.style.paddingBottom = 8;
			_detailsPanel.Add(hint);
		}

		private void ShowSnapshotView(StateTransitionRecord record)
		{
			_detailsPanel.Clear();

			if (record.BlackboardSnapshot == null || record.BlackboardSnapshot.Count == 0)
			{
				var noVars = new Label("No blackboard variables at this point");
				noVars.style.color = new Color(0.5f, 0.5f, 0.5f);
				noVars.style.paddingLeft = 12;
				noVars.style.paddingTop = 8;
				_detailsPanel.Add(noVars);
				return;
			}

			var header = new Label("Blackboard Snapshot");
			header.style.unityFontStyleAndWeight = FontStyle.Bold;
			header.style.paddingLeft = 12;
			header.style.paddingTop = 8;
			header.style.paddingBottom = 4;
			_detailsPanel.Add(header);

			var scroll = new ScrollView(ScrollViewMode.Vertical);
			scroll.style.maxHeight = 200;

			foreach (var kvp in record.BlackboardSnapshot)
			{
				var row = new VisualElement();
				row.style.flexDirection = FlexDirection.Row;
				row.style.paddingLeft = 12;
				row.style.paddingRight = 12;
				row.style.paddingTop = 2;
				row.style.paddingBottom = 2;

				var keyLabel = new Label(kvp.Key);
				keyLabel.style.width = 120;
				keyLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
				row.Add(keyLabel);

				var typeName = record.BlackboardTypes != null &&
					record.BlackboardTypes.TryGetValue(kvp.Key, out var tn)
					? tn
					: "";
				if (!string.IsNullOrEmpty(typeName))
				{
					var typeLabel = new Label($"({typeName})");
					typeLabel.style.width = 60;
					typeLabel.style.color = new Color(0.4f, 0.4f, 0.4f);
					typeLabel.style.fontSize = 10;
					row.Add(typeLabel);
				}

				var valueLabel = new Label(kvp.Value ?? "null");
				valueLabel.style.flexGrow = 1;
				valueLabel.style.color = new Color(0.6f, 0.78f, 1f);
				row.Add(valueLabel);

				scroll.Add(row);
			}

			_detailsPanel.Add(scroll);
		}

		private void ShowDiffView(StateTransitionRecord recordA, StateTransitionRecord recordB)
		{
			_detailsPanel.Clear();

			var diff = BlackboardDiffUtility.ComputeDiff(
				recordA.BlackboardSnapshot,
				recordB.BlackboardSnapshot);

			// Diff header
			var header = new VisualElement();
			header.style.flexDirection = FlexDirection.Row;
			header.style.justifyContent = Justify.SpaceBetween;
			header.style.paddingLeft = 12;
			header.style.paddingRight = 12;
			header.style.paddingTop = 8;
			header.style.paddingBottom = 4;
			header.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);

			var headerLabel = new Label("Blackboard Diff");
			headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			header.Add(headerLabel);

			var fromTo = new Label(
				$"{recordA.ToStateName ?? "?"} \u2192 {recordB.ToStateName ?? "?"}");
			fromTo.style.color = new Color(0.5f, 0.5f, 0.5f);
			fromTo.style.fontSize = 10;
			header.Add(fromTo);

			_detailsPanel.Add(header);

			if (!diff.HasChanges)
			{
				var noChanges = new Label("No blackboard changes between these transitions");
				noChanges.style.color = new Color(0.5f, 0.5f, 0.5f);
				noChanges.style.paddingLeft = 12;
				noChanges.style.paddingTop = 4;
				_detailsPanel.Add(noChanges);
				return;
			}

			var scroll = new ScrollView(ScrollViewMode.Vertical);
			scroll.style.maxHeight = 200;

			foreach (var entry in diff.Changed)
			{
				scroll.Add(CreateDiffRow(entry.Key, entry.OldValue, entry.NewValue,
					new Color(0.78f, 0.59f, 0.2f, 0.15f)));
			}

			foreach (var entry in diff.Added)
			{
				scroll.Add(CreateDiffRow(entry.Key, null, entry.NewValue,
					new Color(0.2f, 0.59f, 0.2f, 0.15f)));
			}

			foreach (var entry in diff.Removed)
			{
				scroll.Add(CreateDiffRow(entry.Key, entry.OldValue, null,
					new Color(0.78f, 0.2f, 0.2f, 0.15f)));
			}

			_detailsPanel.Add(scroll);
		}

		private VisualElement CreateDiffRow(string key, string oldValue, string newValue, Color bgColor)
		{
			var row = new VisualElement();
			row.style.flexDirection = FlexDirection.Row;
			row.style.paddingLeft = 12;
			row.style.paddingRight = 12;
			row.style.paddingTop = 2;
			row.style.paddingBottom = 2;
			row.style.backgroundColor = bgColor;

			var keyLabel = new Label(key);
			keyLabel.style.width = 120;
			keyLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
			row.Add(keyLabel);

			if (oldValue != null && newValue != null)
			{
				var oldLabel = new Label(oldValue);
				oldLabel.style.color = new Color(0.78f, 0.47f, 0.47f);
				oldLabel.style.flexGrow = 1;
				row.Add(oldLabel);

				var arrow = new Label("\u2192");
				arrow.style.marginLeft = 4;
				arrow.style.marginRight = 4;
				arrow.style.color = new Color(0.47f, 0.47f, 0.47f);
				row.Add(arrow);

				var newLabel = new Label(newValue);
				newLabel.style.color = new Color(0.47f, 0.78f, 0.47f);
				newLabel.style.flexGrow = 1;
				row.Add(newLabel);
			}
			else if (newValue != null)
			{
				var addedLabel = new Label($"+ {newValue}");
				addedLabel.style.color = new Color(0.47f, 0.78f, 0.47f);
				addedLabel.style.flexGrow = 1;
				row.Add(addedLabel);
			}
			else if (oldValue != null)
			{
				var removedLabel = new Label($"- {oldValue}");
				removedLabel.style.color = new Color(0.78f, 0.47f, 0.47f);
				removedLabel.style.flexGrow = 1;
				row.Add(removedLabel);
			}

			return row;
		}

		private void ToggleDiffMode()
		{
			_isDiffMode = !_isDiffMode;

			if (!_isDiffMode)
			{
				if (_anchorElement != null)
				{
					_anchorElement.style.backgroundColor = StyleKeyword.Null;
					_anchorElement.style.borderLeftWidth = 0;
					_anchorElement = null;
				}
				_diffAnchorRecord = null;
				_detailsPanel.Clear();
			}
			else
			{
				_detailsPanel.Clear();
				var hint = new Label("Right-click an entry to set as diff anchor, then click another to compare");
				hint.style.color = new Color(0.6f, 0.6f, 0.6f);
				hint.style.paddingLeft = 12;
				hint.style.paddingTop = 8;
				hint.style.paddingBottom = 8;
				_detailsPanel.Add(hint);
			}
		}

		private Label CreateEmptyLabel(string text)
		{
			var label = new Label(text);
			label.style.color = new Color(0.5f, 0.5f, 0.5f);
			label.style.paddingLeft = 12;
			label.style.paddingTop = 8;
			return label;
		}

		private static string FormatDuration(TimeSpan duration)
		{
			if (duration.TotalMilliseconds < 1)
			{
				return "<1ms";
			}
			if (duration.TotalSeconds < 1)
			{
				return $"{duration.TotalMilliseconds:F0}ms";
			}
			if (duration.TotalMinutes < 1)
			{
				return $"{duration.TotalSeconds:F1}s";
			}
			return $"{duration.TotalMinutes:F1}m";
		}
	}
}
