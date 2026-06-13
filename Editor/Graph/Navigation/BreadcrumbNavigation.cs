using System;
using System.Collections.Generic;
using System.Linq;
using Nonatomic.VSM.Graph;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.VSM.Editor.Graph.Navigation
{
	/// <summary>
	/// Breadcrumb navigation for traversing sub-state machine hierarchy.
	/// </summary>
	public class BreadcrumbNavigation : VisualElement
	{
		public event Action<int> OnBreadcrumbClicked;
		public event Action<StateMachineGraph> OnRecentGraphSelected;

		private readonly List<BreadcrumbItem> _breadcrumbs = new List<BreadcrumbItem>();
		private readonly VisualElement _container;
		private readonly Button _historyButton;

		// Recently viewed history (static to persist across instances)
		private static readonly List<RecentGraphEntry> _recentHistory = new List<RecentGraphEntry>();
		private const int MaxHistorySize = 10;

		public BreadcrumbNavigation()
		{
			AddToClassList("breadcrumb-navigation");

			style.flexDirection = FlexDirection.Row;
			style.alignItems = Align.Center;
			style.paddingLeft = 8;
			style.paddingRight = 8;
			style.paddingTop = 4;
			style.paddingBottom = 4;

			// History dropdown button
			_historyButton = new Button(ShowHistoryMenu)
			{
				text = "▼",
				tooltip = "Recently viewed graphs"
			};
			_historyButton.AddToClassList("breadcrumb-history-button");
			_historyButton.style.width = 20;
			_historyButton.style.height = 20;
			_historyButton.style.fontSize = 8;
			_historyButton.style.paddingLeft = 0;
			_historyButton.style.paddingRight = 0;
			_historyButton.style.paddingTop = 0;
			_historyButton.style.paddingBottom = 0;
			_historyButton.style.marginRight = 4;
			_historyButton.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
			_historyButton.style.borderTopLeftRadius = 3;
			_historyButton.style.borderTopRightRadius = 3;
			_historyButton.style.borderBottomLeftRadius = 3;
			_historyButton.style.borderBottomRightRadius = 3;
			Add(_historyButton);

			_container = new VisualElement();
			_container.style.flexDirection = FlexDirection.Row;
			_container.style.alignItems = Align.Center;
			Add(_container);
		}

		/// <summary>
		/// Records a graph as recently viewed.
		/// </summary>
		public static void AddToHistory(StateMachineGraph graph)
		{
			if (graph == null) return;

			var assetPath = AssetDatabase.GetAssetPath(graph);
			if (string.IsNullOrEmpty(assetPath)) return;

			// Remove existing entry for this graph
			_recentHistory.RemoveAll(e => e.AssetPath == assetPath);

			// Add to front of list
			_recentHistory.Insert(0, new RecentGraphEntry
			{
				AssetPath = assetPath,
				GraphName = graph.name,
				ViewedAt = DateTime.Now
			});

			// Trim to max size
			while (_recentHistory.Count > MaxHistorySize)
			{
				_recentHistory.RemoveAt(_recentHistory.Count - 1);
			}
		}

		/// <summary>
		/// Gets the recent history entries.
		/// </summary>
		public static IReadOnlyList<RecentGraphEntry> GetRecentHistory()
		{
			return _recentHistory;
		}

		private void ShowHistoryMenu()
		{
			var menu = new GenericMenu();

			if (_recentHistory.Count == 0)
			{
				menu.AddDisabledItem(new GUIContent("No recent graphs"));
			}
			else
			{
				foreach (var entry in _recentHistory)
				{
					var capturedEntry = entry;
					menu.AddItem(
						new GUIContent(entry.GraphName),
						false,
						() => LoadGraphFromHistory(capturedEntry)
					);
				}

				menu.AddSeparator("");
				menu.AddItem(new GUIContent("Clear History"), false, () => _recentHistory.Clear());
			}

			menu.ShowAsContext();
		}

		private void LoadGraphFromHistory(RecentGraphEntry entry)
		{
			var graph = AssetDatabase.LoadAssetAtPath<StateMachineGraph>(entry.AssetPath);
			if (graph != null)
			{
				OnRecentGraphSelected?.Invoke(graph);
			}
		}

		public void SetPath(IReadOnlyList<StateMachineGraph> path, StateMachineGraph current)
		{
			_breadcrumbs.Clear();
			_container.Clear();

			// Add path items (parent graphs in the navigation stack)
			for (var i = 0; i < path.Count; i++)
			{
				if (i > 0)
				{
					AddSeparator();
				}

				var graph = path[i];
				var button = CreateBreadcrumbButton(graph.name, i, false);
				_container.Add(button);
			}

			// Add current item (non-clickable, we're already here)
			if (current != null)
			{
				if (path.Count > 0)
				{
					AddSeparator();
				}

				var currentLabel = new Label(current.name);
				currentLabel.AddToClassList("breadcrumb-current");
				currentLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
				currentLabel.style.color = new Color(0.9f, 0.9f, 0.9f);
				_container.Add(currentLabel);
			}
		}

		private Button CreateBreadcrumbButton(string text, int index, bool isRoot)
		{
			var button = new Button(() => OnBreadcrumbClicked?.Invoke(index))
			{
				text = text
			};

			button.AddToClassList("breadcrumb-button");
			button.style.backgroundColor = Color.clear;
			button.style.borderTopWidth = 0;
			button.style.borderBottomWidth = 0;
			button.style.borderLeftWidth = 0;
			button.style.borderRightWidth = 0;
			button.style.paddingLeft = 4;
			button.style.paddingRight = 4;
			button.style.color = new Color(0.6f, 0.8f, 1f);

			button.RegisterCallback<MouseEnterEvent>(evt => { button.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f); });

			button.RegisterCallback<MouseLeaveEvent>(evt => { button.style.backgroundColor = Color.clear; });

			return button;
		}

		private void AddSeparator()
		{
			var separator = new Label(">");
			separator.AddToClassList("breadcrumb-separator");
			separator.style.marginLeft = 4;
			separator.style.marginRight = 4;
			separator.style.color = new Color(0.5f, 0.5f, 0.5f);
			_container.Add(separator);
		}

		public new void Clear()
		{
			_breadcrumbs.Clear();
			_container.Clear();
		}
	}

	/// <summary>
	/// Represents a single breadcrumb item.
	/// </summary>
	public class BreadcrumbItem
	{
		public string Name { get; set; }
		public StateMachineGraph Graph { get; set; }
		public int Depth { get; set; }
	}

	/// <summary>
	/// An entry in the recent graph history.
	/// </summary>
	public class RecentGraphEntry
	{
		public string AssetPath { get; set; }
		public string GraphName { get; set; }
		public DateTime ViewedAt { get; set; }
	}
}
